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
    private static async Task<HttpResponseMessage> CompanyDeletionSend(HttpClient client, HttpMethod method,
        string path, object body)
    {
        using var csrf = await client.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Company_deletion_is_recoverable_preserves_shared_identity_and_other_tenant_sessions_and_records()
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var slug = $"delete-{Guid.NewGuid():N}";
        string? passwordHash, stamp;
        var now = DateTimeOffset.UtcNow;
        var targetSession = new UserSession { UserId = _viewerUserId, CompanyId = _companyId, ExpiresAt = now.AddHours(1) };
        var otherSession = new UserSession { UserId = _viewerUserId, CompanyId = _otherCompanyId, ExpiresAt = now.AddHours(1) };
        var token = new CompanyApiToken { CompanyId = _companyId, UserId = _viewerUserId, Name = "Synthetic target",
            TokenPrefix = "test", TokenHash = Guid.NewGuid().ToString("N"), ScopesJson = "[]" };
        var otherToken = new CompanyApiToken { CompanyId = _otherCompanyId, UserId = _viewerUserId, Name = "Synthetic other",
            TokenPrefix = "test", TokenHash = Guid.NewGuid().ToString("N"), ScopesJson = "[]" };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Companies.SingleAsync(company => company.Id == _companyId)).Slug = slug;
            var owner = await db.Users.SingleAsync(account => account.Id == _viewerUserId);
            passwordHash = owner.PasswordHash; stamp = owner.SecurityStamp;
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _viewerUserId, RoleKey = "viewer" });
            db.UserSessions.AddRange(targetSession, otherSession);
            db.CompanyApiTokens.AddRange(token, otherToken);
            await db.SaveChangesAsync();
        }
        var body = new DeleteCompanyRequest(slug, "Synthetic demo isolation acceptance");
        using var deleted = await CompanyDeletionSend(client, HttpMethod.Delete, $"/api/platform/companies/{_companyId}", body);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.False((await deleted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("billingChanged").GetBoolean());
        using var duplicate = await CompanyDeletionSend(client, HttpMethod.Delete, $"/api/platform/companies/{_companyId}", body);
        Assert.True((await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("unchanged").GetBoolean());
        var visible = await client.GetFromJsonAsync<JsonElement>($"/api/platform/companies?q={slug}");
        Assert.Equal(0, visible.GetProperty("total").GetInt32());
        var trash = await client.GetFromJsonAsync<JsonElement>($"/api/platform/companies?deleted=true&q={slug}");
        Assert.Equal(1, trash.GetProperty("total").GetInt32());
        using var activation = await CompanyDeletionSend(client, HttpMethod.Patch, $"/api/platform/companies/{_companyId}/active",
            new SetCompanyActiveRequest(true, "Must not bypass company trash"));
        Assert.Equal(HttpStatusCode.Conflict, activation.StatusCode);
        using var bulkActivation = await CompanyDeletionSend(client, HttpMethod.Patch, "/api/platform/companies/bulk-status",
            new SetCompaniesActiveRequest([_companyId, _otherCompanyId], true, "Must not partially activate trash"));
        Assert.Equal(HttpStatusCode.Conflict, bulkActivation.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.NotNull((await db.Companies.SingleAsync(company => company.Id == _companyId)).DeletedAt);
            Assert.False((await db.Companies.SingleAsync(company => company.Id == _companyId)).IsActive);
            Assert.True((await db.Companies.SingleAsync(company => company.Id == _otherCompanyId)).IsActive);
            Assert.NotNull((await db.UserSessions.SingleAsync(session => session.Id == targetSession.Id)).RevokedAt);
            Assert.Null((await db.UserSessions.SingleAsync(session => session.Id == otherSession.Id)).RevokedAt);
            Assert.Null((await db.UserSessions.SingleAsync(session => session.Id == setup.OriginalId)).RevokedAt);
            Assert.NotNull((await db.CompanyApiTokens.SingleAsync(item => item.Id == token.Id)).RevokedAt);
            Assert.Null((await db.CompanyApiTokens.SingleAsync(item => item.Id == otherToken.Id)).RevokedAt);
            var owner = await db.Users.SingleAsync(account => account.Id == _viewerUserId);
            Assert.Equal(passwordHash, owner.PasswordHash); Assert.Equal(stamp, owner.SecurityStamp);
            Assert.True(await db.CompanyMemberships.AnyAsync(member => member.UserId == _viewerUserId && member.CompanyId == _companyId));
            Assert.True(await db.CompanyMemberships.AnyAsync(member => member.UserId == _viewerUserId && member.CompanyId == _otherCompanyId));
            Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _ownAssetId));
            Assert.True(await db.Assets.AnyAsync(asset => asset.Id == _otherCompanyAssetId));
            Assert.Single(await db.PlatformAuditEntries.Where(entry => entry.CompanyId == _companyId && entry.Action == "CompanySoftDeleted").ToListAsync());
        }
        using var restored = await ImpersonationPost(client, $"/api/platform/companies/{_companyId}/restore",
            new RestoreCompanyRequest("Synthetic restoration acceptance"));
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(item => item.Id == _companyId);
            Assert.Null(company.DeletedAt); Assert.False(company.IsActive);
            Assert.NotNull((await db.UserSessions.SingleAsync(session => session.Id == targetSession.Id)).RevokedAt);
            Assert.NotNull((await db.CompanyApiTokens.SingleAsync(item => item.Id == token.Id)).RevokedAt);
            Assert.Single(await db.PlatformAuditEntries.Where(entry => entry.CompanyId == _companyId && entry.Action == "CompanyRestored").ToListAsync());
        }
    }

    [Fact]
    public async Task Company_deletion_rejects_missing_reason_slug_confirmation_and_csrf()
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory; using var client = setup.Client;
        var slug = $"delete-{Guid.NewGuid():N}";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Companies.SingleAsync(company => company.Id == _companyId)).Slug = slug;
            await db.SaveChangesAsync();
        }
        foreach (var invalid in new[] { new DeleteCompanyRequest(slug, ""), new DeleteCompanyRequest("wrong-slug", "Required reason entered") })
        {
            using var denied = await CompanyDeletionSend(client, HttpMethod.Delete, $"/api/platform/companies/{_companyId}", invalid);
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        }
        using var missing = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/companies/{_companyId}")
            { Content = JsonContent.Create(new DeleteCompanyRequest(slug, "Required reason entered")) };
        using var missingCsrf = await client.SendAsync(missing);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await using var check = factory.Services.CreateAsyncScope();
        var company = await check.ServiceProvider.GetRequiredService<AppDbContext>().Companies.SingleAsync(item => item.Id == _companyId);
        Assert.Null(company.DeletedAt); Assert.True(company.IsActive);
    }

    [Fact]
    public async Task Company_deletion_requires_platform_mfa()
    {
        var setup = await ImpersonationActor(mfa: false, stepUp: false);
        using var factory = setup.Factory; using var client = setup.Client;
        using var denied = await CompanyDeletionSend(client, HttpMethod.Delete, $"/api/platform/companies/{_companyId}",
            new DeleteCompanyRequest("unused", "Required reason entered"));
        Assert.Equal((HttpStatusCode)428, denied.StatusCode);
    }

    [Fact]
    public async Task Company_deletion_rejects_company_administrator()
    {
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var denied = await SendAsUserAsync(HttpMethod.Delete, $"/api/platform/companies/{_companyId}", _userId, _companyId,
            JsonContent.Create(new DeleteCompanyRequest("unused", "Required reason entered")), token, cookie);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
