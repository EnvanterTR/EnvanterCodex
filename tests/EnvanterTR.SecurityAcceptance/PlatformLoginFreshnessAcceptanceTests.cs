using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fresh_platform_password_and_mfa_login_authorizes_consecutive_writes_without_another_code(bool recovery)
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var cookie = setup.RootCookie;
        if (recovery)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var actor = (await users.FindByIdAsync(_userId.ToString()))!;
            var codes = (await users.GenerateNewTwoFactorRecoveryCodesAsync(actor, 2))!.ToArray();
            using var login = await ImpersonationPost(client, "/api/session/login", new
            { email = actor.Email, password = ImpersonationPassword, platformAdminOnly = true, twoFactorCode = codes[0] });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            cookie = ResponseCookie(login, "EnvanterTR.Session");
            await using var recoveryCheck = factory.Services.CreateAsyncScope();
            var freshUsers = recoveryCheck.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var freshActor = (await freshUsers.FindByIdAsync(_userId.ToString()))!;
            Assert.Equal(1, await freshUsers.CountRecoveryCodesAsync(freshActor));
        }
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookie.Split('=', 2)[1])!;
        var proof = long.Parse(ticket.Principal.FindFirstValue("platform_reauth_at")!,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(proof, DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Assert.Null(ticket.Principal.FindFirstValue("company_id"));
        var planCodes = Enumerable.Range(0, 2).Select(index => $"mfa-{_userId:N}-{index}").ToArray();
        foreach (var planCode in planCodes)
        {
            using var write = await ImpersonationPost(client, "/api/platform/plans",
                new SavePlanRequest(planCode, "Fresh login acceptance", "Synthetic isolated plan", "cloud", 1, null,
                    0m, 2400m, 0m, "TRY", false, 0, "{}", false, true, 0,
                    "Synthetic fresh-login consecutive-write acceptance"));
            Assert.Equal(HttpStatusCode.Created, write.StatusCode);
        }
        await using var check = factory.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.SubscriptionPlans.CountAsync(plan => planCodes.Contains(plan.Code)));
        Assert.Equal(2, await db.PlatformAuditEntries.CountAsync(entry => entry.ActorUserId == _userId &&
            entry.Action == "PlanCreated"));
        Assert.False(await db.PlatformAuditEntries.AnyAsync(entry => entry.ActorUserId == _userId &&
            entry.Action == "PlatformReauthenticated"));
        var refreshed = await client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.True(refreshed.GetProperty("platformAdmin").GetBoolean());
    }

    [Fact]
    public async Task Company_login_by_a_platform_administrator_does_not_grant_fresh_platform_write_proof()
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        string email, code;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var actor = (await users.FindByIdAsync(_userId.ToString()))!;
            email = actor.Email!;
            code = GenerateAuthenticatorCode((await users.GetAuthenticatorKeyAsync(actor))!);
        }
        using var login = await ImpersonationPost(client, "/api/session/login", new
        { email, password = ImpersonationPassword, companyId = _companyId, twoFactorCode = code });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = ResponseCookie(login, "EnvanterTR.Session");
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookie.Split('=', 2)[1])!;
        Assert.Equal(_companyId.ToString(), ticket.Principal.FindFirstValue("company_id"));
        Assert.Null(ticket.Principal.FindFirstValue("platform_reauth_at"));
        using var denied = await ImpersonationPost(client, "/api/platform/plans/annual-catalog/drafts",
            new { changeReason = "Company login must not authorize platform writes" });
        Assert.Equal((HttpStatusCode)428, denied.StatusCode);
    }
}
