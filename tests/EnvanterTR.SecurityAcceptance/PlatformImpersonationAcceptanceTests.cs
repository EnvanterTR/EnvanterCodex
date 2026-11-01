using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private const string ImpersonationPassword = "Synthetic-Support-Actor-47!";

    private static async Task<HttpResponseMessage> ImpersonationPost(HttpClient client, string path, object body)
    {
        using var csrf = await client.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        using var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        message.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(message);
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid OriginalId, string RootCookie)>
        ImpersonationActor(string role = "viewer", bool mfa = true, bool stepUp = true)
    {
        string email, secret;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(db.Database.HasPendingModelChanges());
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var actor = (await users.FindByIdAsync(_userId.ToString()))!;
            actor.DisplayName = "Support Actor";
            Assert.True((await users.UpdateAsync(actor)).Succeeded);
            Assert.True((await users.AddPasswordAsync(actor, ImpersonationPassword)).Succeeded);
            Assert.True((await users.ResetAuthenticatorKeyAsync(actor)).Succeeded);
            secret = (await users.GetAuthenticatorKeyAsync(actor))!;
            Assert.True((await users.SetTwoFactorEnabledAsync(actor, mfa)).Succeeded);
            email = actor.Email!;
            var target = (await users.FindByIdAsync(_viewerUserId.ToString()))!;
            target.EmailConfirmed = true; target.DisplayName = "Customer Account";
            Assert.True((await users.UpdateAsync(target)).Succeeded);
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _viewerUserId)
                .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, role));
            await db.SaveChangesAsync();
        }
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                ["Storage:CompanyMediaPath"] = _mediaPath, ["TenantRouting:RootDomain"] = "envantertr.com"
            }));
            builder.ConfigureTestServices(services => services.PostConfigure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero));
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var login = await ImpersonationPost(client, "/api/session/login", new
        { email, password = ImpersonationPassword, platformAdminOnly = true, twoFactorCode = mfa ? GenerateAuthenticatorCode(secret) : null });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var rootCookie = ResponseCookie(login, "EnvanterTR.Session");
        if (stepUp)
        {
            using var verified = await ImpersonationPost(client, "/api/session/platform-reauth", new
            { password = ImpersonationPassword, code = GenerateAuthenticatorCode(secret) });
            Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
            rootCookie = ResponseCookie(verified, "EnvanterTR.Session");
        }
        var ticket = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme).TicketDataFormat.Unprotect(rootCookie.Split('=', 2)[1])!;
        return (factory, client, Guid.Parse(ticket.Principal.FindFirstValue("session_id")!), rootCookie);
    }

    private static string ResponseCookie(HttpResponseMessage response, string name) => response.Headers.GetValues("Set-Cookie")
        .Last(value => value.StartsWith(name + "=", StringComparison.Ordinal)).Split(';', 2)[0];
    private async Task<HttpResponseMessage> BeginImpersonation(HttpClient client) => await ImpersonationPost(client,
        $"/api/platform/companies/{_companyId}/impersonations", new StartPlatformImpersonation(_viewerUserId, "Synthetic customer support request"));

    [Fact]
    public async Task Impersonation_uses_customer_role_scope_records_actor_and_restores_original_session()
    {
        var setup = await ImpersonationActor("owner");
        using var factory = setup.Factory; using var client = setup.Client;
        DateTimeOffset originalExpiry;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            originalExpiry = (await db.UserSessions.SingleAsync(session => session.Id == setup.OriginalId)).ExpiresAt;
            db.CompanyMemberships.Add(new CompanyMembership { UserId = _viewerUserId, CompanyId = _otherCompanyId, RoleKey = "owner" });
            await db.SaveChangesAsync();
        }
        using var started = await BeginImpersonation(client);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var cookies = started.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(PlatformImpersonation.ReturnCookieName + "="));
        Assert.Contains("httponly", cookies, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookies, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ResponseCookie(started, PlatformImpersonation.ReturnCookieName).Split('=', 2)[1], await started.Content.ReadAsStringAsync());
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.Equal(_viewerUserId.ToString(), session.GetProperty("userId").GetString());
        Assert.False(session.GetProperty("platformAdmin").GetBoolean());
        Assert.Equal("owner", session.GetProperty("role").GetString());
        Assert.Equal("Support Actor", session.GetProperty("impersonation").GetProperty("actorName").GetString());
        Assert.Single(session.GetProperty("availableCompanies").EnumerateArray());
        using var rootDenied = await client.GetAsync("/api/platform/overview");
        Assert.Equal(HttpStatusCode.Forbidden, rootDenied.StatusCode);
        using var otherDenied = await client.GetAsync($"/api/assets/{_otherCompanyAssetId}");
        Assert.Equal(HttpStatusCode.NotFound, otherDenied.StatusCode);
        var categoryName = "Impersonated write " + Guid.NewGuid().ToString("N");
        using var created = await ImpersonationPost(client, "/api/accessory-categories", new { name = categoryName });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Guid impersonationId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(_companyId, (await db.AccessoryCategories.SingleAsync(category => category.Name == categoryName)).CompanyId);
            var support = await db.UserSessions.SingleAsync(item => item.ImpersonatorSessionId == setup.OriginalId);
            impersonationId = support.Id;
            Assert.False(support.PlatformAdmin);
            Assert.Equal(_viewerUserId, support.UserId);
            Assert.True(support.ExpiresAt <= support.CreatedAt.AddMinutes(15));
            Assert.Equal(64, support.ImpersonationReturnTokenHash!.Length);
            Assert.Contains(await db.PlatformAuditEntries.Where(entry => entry.TargetId == support.Id).ToListAsync(),
                entry => entry.ActorUserId == _userId && entry.Action == "ImpersonationRequest" && entry.Details!.Contains("/api/accessory-categories"));
        }
        using var stopped = await ImpersonationPost(client, PlatformImpersonation.StopPath, new { });
        Assert.Equal(HttpStatusCode.OK, stopped.StatusCode);
        Assert.Equal("https://envantertr.com/platform-admin.html", (await stopped.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("platformUrl").GetString());
        var restored = await client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.True(restored.GetProperty("platformAdmin").GetBoolean());
        Assert.Equal(_userId.ToString(), restored.GetProperty("userId").GetString());
        Assert.Equal(JsonValueKind.Null, restored.GetProperty("impersonation").ValueKind);
        using var needsNewStepUp = await ImpersonationPost(client, $"/api/platform/companies/{_companyId}/impersonations",
            new StartPlatformImpersonation(_viewerUserId, "Another support request"));
        Assert.Equal((HttpStatusCode)428, needsNewStepUp.StatusCode);
        await using var finalScope = factory.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ended = await finalDb.UserSessions.SingleAsync(item => item.Id == impersonationId);
        Assert.NotNull(ended.RevokedAt); Assert.NotNull(ended.ImpersonationEndedAt); Assert.Null(ended.ImpersonationReturnTokenHash);
        Assert.Equal(originalExpiry, (await finalDb.UserSessions.SingleAsync(item => item.Id == setup.OriginalId)).ExpiresAt);
        Assert.Equal(1, await finalDb.PlatformAuditEntries.CountAsync(entry => entry.TargetId == ended.Id && entry.Action == "ImpersonationStarted"));
        Assert.Equal(1, await finalDb.PlatformAuditEntries.CountAsync(entry => entry.TargetId == ended.Id && entry.Action == "ImpersonationEnded"));
    }

    [Fact]
    public async Task Impersonation_does_not_elevate_a_customer_viewer_or_allow_nested_platform_access()
    {
        var setup = await ImpersonationActor();
        using var factory = setup.Factory; using var client = setup.Client;
        using var started = await BeginImpersonation(client); Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        using var own = await client.GetAsync($"/api/assets/{_ownAssetId}"); Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        using var write = await ImpersonationPost(client, "/api/accessory-categories", new { name = "Must not exist" });
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        using var nested = await BeginImpersonation(client); Assert.Equal(HttpStatusCode.Forbidden, nested.StatusCode);
        using var exit = await ImpersonationPost(client, "/api/session/logout", new { });
        Assert.Equal(HttpStatusCode.OK, exit.StatusCode);
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/session")).GetProperty("platformAdmin").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Impersonation_requires_platform_mfa_and_recent_reauthentication(bool mfa)
    {
        var setup = await ImpersonationActor(mfa: mfa, stepUp: false);
        using var factory = setup.Factory; using var originalClient = setup.Client;
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var cookie = setup.RootCookie;
        if (mfa)
        {
            // A fresh MFA login now supplies the same proof as explicit reauthentication.
            // Exercise an expired proof rather than misclassifying that fresh login.
            var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(IdentityConstants.ApplicationScheme);
            var ticket = options.TicketDataFormat.Unprotect(cookie.Split('=', 2)[1])!;
            var identity = (ClaimsIdentity)ticket.Principal.Identity!;
            foreach (var claim in identity.FindAll("platform_reauth_at").ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new Claim("platform_reauth_at", DateTimeOffset.UtcNow.AddMinutes(-11)
                .ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
            cookie = "EnvanterTR.Session=" + options.TicketDataFormat.Protect(ticket);
        }
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        using var denied = await BeginImpersonation(client);
        Assert.Equal((HttpStatusCode)428, denied.StatusCode);
        Assert.Contains(mfa ? "platform_reauth_required" : "platform_mfa_required", await denied.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("removed")]
    [InlineData("stamp")]
    [InlineData("mfa")]
    public async Task Impersonation_stops_when_the_original_administrator_authority_changes(string kind)
    {
        var setup = await ImpersonationActor();
        using var factory = setup.Factory; using var client = setup.Client;
        using var started = await BeginImpersonation(client); Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (kind == "revoked") await db.UserSessions.Where(session => session.Id == setup.OriginalId).ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow));
            if (kind == "removed") await db.PlatformAdministrators.Where(admin => admin.UserId == _userId).ExecuteDeleteAsync();
            if (kind == "stamp") await db.Users.Where(user => user.Id == _userId).ExecuteUpdateAsync(update => update.SetProperty(user => user.SecurityStamp, Guid.NewGuid().ToString()));
            if (kind == "mfa") await db.Users.Where(user => user.Id == _userId).ExecuteUpdateAsync(update => update.SetProperty(user => user.TwoFactorEnabled, false));
        }
        using var denied = await client.GetAsync($"/api/assets/{_ownAssetId}"); Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var returnDenied = await client.PostAsJsonAsync(PlatformImpersonation.StopPath, new { });
        Assert.NotEqual(HttpStatusCode.OK, returnDenied.StatusCode);
    }

    [Fact]
    public async Task Impersonation_return_requires_secret_and_rejects_consumed_cookie_replay()
    {
        var setup = await ImpersonationActor();
        using var factory = setup.Factory; using var client = setup.Client;
        using var started = await BeginImpersonation(client); Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var supportCookie = ResponseCookie(started, "EnvanterTR.Session");
        var returnCookie = ResponseCookie(started, PlatformImpersonation.ReturnCookieName);
        using var raw = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        raw.DefaultRequestHeaders.Add("Cookie", supportCookie + "; " + returnCookie);
        using var csrf = await raw.GetAsync("/api/session/csrf"); Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var csrfCookie = csrf.Headers.GetValues("Set-Cookie").Last(value => value.StartsWith(".AspNetCore.Antiforgery.")).Split(';', 2)[0];
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        async Task<HttpResponseMessage> Return(string? secret)
        {
            raw.DefaultRequestHeaders.Remove("Cookie");
            raw.DefaultRequestHeaders.Add("Cookie", supportCookie + "; " + csrfCookie + (secret is null ? "" : "; " + secret));
            using var request = new HttpRequestMessage(HttpMethod.Post, PlatformImpersonation.StopPath) { Content = JsonContent.Create(new { }) };
            request.Headers.Add("X-CSRF-TOKEN", token);
            return await raw.SendAsync(request);
        }
        using var missing = await Return(null); Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);
        using var wrong = await Return(PlatformImpersonation.ReturnCookieName + "=" + new string('A', 43));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        using var valid = await Return(returnCookie); Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var replay = await Return(returnCookie); Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
    }

    [Theory]
    [InlineData("membership")]
    [InlineData("company")]
    [InlineData("session")]
    public async Task Impersonation_can_return_without_customer_membership_or_company_access(string kind)
    {
        var setup = await ImpersonationActor();
        using var factory = setup.Factory; using var client = setup.Client;
        using var started = await BeginImpersonation(client); Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var exitCsrf = (await client.GetFromJsonAsync<JsonElement>("/api/session/csrf")).GetProperty("requestToken").GetString();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (kind == "membership") await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _viewerUserId).ExecuteDeleteAsync();
            if (kind == "company") await db.Companies.Where(company => company.Id == _companyId).ExecuteUpdateAsync(update => update.SetProperty(company => company.IsActive, false));
            if (kind == "session") await db.UserSessions.Where(session => session.ImpersonatorSessionId == setup.OriginalId).ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, DateTimeOffset.UtcNow));
        }
        using var exitRequest = new HttpRequestMessage(HttpMethod.Post, PlatformImpersonation.StopPath) { Content = JsonContent.Create(new { }) };
        exitRequest.Headers.Add("X-CSRF-TOKEN", exitCsrf);
        using var stopped = await client.SendAsync(exitRequest);
        Assert.Equal(HttpStatusCode.OK, stopped.StatusCode);
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/session")).GetProperty("platformAdmin").GetBoolean());
    }
}
