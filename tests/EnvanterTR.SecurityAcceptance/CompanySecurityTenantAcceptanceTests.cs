using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Company_security_policy_ignores_forged_foreign_company_selectors()
    {
        var now = DateTimeOffset.UtcNow;
        var sessionAId = Guid.NewGuid();
        var sessionBId = Guid.NewGuid();
        var createdAt = now.AddHours(-2);
        var originalExpiry = now.AddDays(20);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var companyA = await db.Companies.SingleAsync(item => item.Id == _companyId);
            companyA.SecurityPasswordMinimumLength = 13;
            companyA.SecuritySessionDurationHours = 80;
            companyA.SecurityRequireMfa = false;
            var companyB = await db.Companies.SingleAsync(item => item.Id == _otherCompanyId);
            companyB.SecurityPasswordMinimumLength = 21;
            companyB.SecuritySessionDurationHours = 96;
            companyB.SecurityRequireMfa = true;
            db.CompanyMemberships.Add(new CompanyMembership
            {
                CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin"
            });
            db.UserSessions.AddRange(
                new UserSession
                {
                    Id = sessionAId, UserId = _userId, CompanyId = _companyId, PlatformAdmin = false,
                    CreatedAt = createdAt, LastSeenAt = createdAt, ExpiresAt = originalExpiry
                },
                new UserSession
                {
                    Id = sessionBId, UserId = _userId, CompanyId = _otherCompanyId, PlatformAdmin = false,
                    CreatedAt = createdAt, LastSeenAt = createdAt, ExpiresAt = originalExpiry
                });
            await db.SaveChangesAsync();
        }

        using (var legitimateB = await SendAsUserAsync(HttpMethod.Get, "/api/company-security", _userId, _otherCompanyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, legitimateB.StatusCode);
            using var payload = JsonDocument.Parse(await legitimateB.Content.ReadAsStringAsync());
            Assert.Equal(21, payload.RootElement.GetProperty("passwordMinimumLength").GetInt32());
            Assert.Equal(96, payload.RootElement.GetProperty("sessionDurationHours").GetInt32());
            Assert.True(payload.RootElement.GetProperty("requireMfa").GetBoolean());
        }

        using (var forgedRead = new HttpRequestMessage(HttpMethod.Get,
                   $"/api/company-security?companyId={_otherCompanyId:D}"))
        {
            forgedRead.Headers.Add("X-Acceptance-User", _userId.ToString());
            forgedRead.Headers.Add("X-Acceptance-Company", _companyId.ToString());
            forgedRead.Headers.Add("X-Acceptance-Role", "admin");
            forgedRead.Headers.Add("X-Company-Id", _otherCompanyId.ToString());
            using var response = await _client!.SendAsync(forgedRead);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(13, payload.RootElement.GetProperty("passwordMinimumLength").GetInt32());
            Assert.Equal(80, payload.RootElement.GetProperty("sessionDurationHours").GetInt32());
            Assert.False(payload.RootElement.GetProperty("requireMfa").GetBoolean());
        }

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _userId, _companyId, role: "admin");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var forgedWrite = new HttpRequestMessage(HttpMethod.Put,
                   $"/api/company-security?companyId={_otherCompanyId:D}")
               {
                   Content = JsonContent.Create(new { passwordMinimumLength = 18, sessionDurationHours = 36, requireMfa = false })
               })
        {
            forgedWrite.Headers.Add("X-Acceptance-User", _userId.ToString());
            forgedWrite.Headers.Add("X-Acceptance-Company", _companyId.ToString());
            forgedWrite.Headers.Add("X-Acceptance-Role", "admin");
            forgedWrite.Headers.Add("X-Company-Id", _otherCompanyId.ToString());
            forgedWrite.Headers.Add("X-CSRF-TOKEN", requestToken);
            forgedWrite.Headers.Add("Cookie", antiforgeryCookie);
            using var response = await _client!.SendAsync(forgedWrite);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        await using var verificationScope = _factory!.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var companyAPolicy = await verificationDb.Companies.AsNoTracking().Where(item => item.Id == _companyId)
            .Select(item => new { item.SecurityPasswordMinimumLength, item.SecuritySessionDurationHours, item.SecurityRequireMfa })
            .SingleAsync();
        var companyBPolicy = await verificationDb.Companies.AsNoTracking().Where(item => item.Id == _otherCompanyId)
            .Select(item => new { item.SecurityPasswordMinimumLength, item.SecuritySessionDurationHours, item.SecurityRequireMfa })
            .SingleAsync();
        Assert.Equal(18, companyAPolicy.SecurityPasswordMinimumLength);
        Assert.Equal(36, companyAPolicy.SecuritySessionDurationHours);
        Assert.False(companyAPolicy.SecurityRequireMfa);
        Assert.Equal(21, companyBPolicy.SecurityPasswordMinimumLength);
        Assert.Equal(96, companyBPolicy.SecuritySessionDurationHours);
        Assert.True(companyBPolicy.SecurityRequireMfa);

        var sessionA = await verificationDb.UserSessions.AsNoTracking().SingleAsync(item => item.Id == sessionAId);
        var sessionB = await verificationDb.UserSessions.AsNoTracking().SingleAsync(item => item.Id == sessionBId);
        var expectedSessionAExpiry = ToPostgresMicrosecondPrecision(createdAt.AddHours(36));
        // PostgreSQL timestamptz stores microsecond precision (10 .NET ticks).
        Assert.Equal(expectedSessionAExpiry, sessionA.ExpiresAt);
        Assert.Equal(ToPostgresMicrosecondPrecision(originalExpiry), sessionB.ExpiresAt);
        Assert.Null(sessionB.RevokedAt);
        Assert.Single(await verificationDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
            entry.Action == "CompanySecurityPolicyChanged").ToListAsync());
        Assert.Empty(await verificationDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _otherCompanyId &&
            entry.Action == "CompanySecurityPolicyChanged").ToListAsync());
    }

    private static DateTimeOffset ToPostgresMicrosecondPrecision(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % 10, value.Offset);
}
