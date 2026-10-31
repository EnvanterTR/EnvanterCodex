using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false, "none")]
    [InlineData(true, "none")]
    [InlineData(false, "revoked-session")]
    [InlineData(false, "suspended-company")]
    [InlineData(false, "removed-membership")]
    public async Task Wrong_tenant_host_preserves_only_still_valid_shared_sessions(bool platformContext, string invalidation)
    {
        const string password = "Synthetic.Tenant-Continuity-42!";
        string email, ownSlug, foreignSlug;
        string? secondFactor = null;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            email = user.Email!;
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ownSlug = (await db.Companies.SingleAsync(x => x.Id == _companyId)).Slug;
            foreignSlug = (await db.Companies.SingleAsync(x => x.Id == _otherCompanyId)).Slug;
            if (platformContext)
            {
                db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
                await db.SaveChangesAsync();
                Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
                Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
                secondFactor = GenerateAuthenticatorCode((await users.GetAuthenticatorKeyAsync(user))!);
            }
        }
        using var realIdentity = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                ["Storage:CompanyMediaPath"] = _mediaPath,
                ["TenantRouting:RootDomain"] = "envantertr.com",
                ["TenantRouting:CookieDomain"] = ".envantertr.com"
            }));
        });
        using var browser = realIdentity.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(platformContext ? "https://envantertr.com" : $"https://{ownSlug}.envantertr.com"), AllowAutoRedirect = false
        });
        using var csrf = await browser.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = JsonContent.Create(new { email, password, companyId = platformContext ? (Guid?)null : _companyId,
                rememberMe = true, platformAdminOnly = platformContext, twoFactorCode = secondFactor })
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", csrfBody.RootElement.GetProperty("requestToken").GetString());
        using var login = await browser.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("EnvanterTR.Session=", StringComparison.Ordinal) && value.Contains("domain=.envantertr.com", StringComparison.OrdinalIgnoreCase));
        using var ownBefore = await browser.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.OK, ownBefore.StatusCode);

        if (invalidation != "none")
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            switch (invalidation)
            {
                case "revoked-session":
                    await db.UserSessions.Where(x => x.UserId == _userId).ExecuteUpdateAsync(x =>
                        x.SetProperty(s => s.RevokedAt, DateTimeOffset.UtcNow));
                    break;
                case "suspended-company":
                    await db.Companies.Where(x => x.Id == _companyId).ExecuteUpdateAsync(x => x.SetProperty(c => c.IsActive, false));
                    break;
                case "removed-membership":
                    await db.CompanyMemberships.Where(x => x.CompanyId == _companyId && x.UserId == _userId).ExecuteDeleteAsync();
                    break;
            }
        }

        using var foreign = await browser.GetAsync($"https://{foreignSlug}.envantertr.com/api/assets");
        Assert.Equal(HttpStatusCode.Unauthorized, foreign.StatusCode);
        var rewritesSessionCookie = foreign.Headers.TryGetValues("Set-Cookie", out var foreignCookies) &&
            foreignCookies.Any(x => x.StartsWith("EnvanterTR.Session=", StringComparison.Ordinal));
        Assert.Equal(invalidation != "none", rewritesSessionCookie);
        using var foreignWrite = await browser.PutAsJsonAsync($"https://{foreignSlug}.envantertr.com/api/assets/{Guid.NewGuid()}",
            new { name = "Must not be written" });
        Assert.Equal(HttpStatusCode.Unauthorized, foreignWrite.StatusCode);
        Assert.False(foreignWrite.Headers.TryGetValues("Set-Cookie", out var writeCookies) &&
            writeCookies.Any(x => x.StartsWith("EnvanterTR.Session=", StringComparison.Ordinal)));
        using var ownAfter = await browser.GetAsync("/api/session");
        if (invalidation != "none")
        {
            Assert.Equal(HttpStatusCode.Unauthorized, ownAfter.StatusCode);
            return;
        }
        Assert.Equal(HttpStatusCode.OK, ownAfter.StatusCode);
        using var ownBody = JsonDocument.Parse(await ownAfter.Content.ReadAsStringAsync());
        if (platformContext)
        {
            Assert.True(ownBody.RootElement.GetProperty("platformAdmin").GetBoolean());
            Assert.Equal(JsonValueKind.Null, ownBody.RootElement.GetProperty("companyId").ValueKind);
        }
        else Assert.Equal(_companyId, ownBody.RootElement.GetProperty("companyId").GetGuid());
    }
}
