using System.Net;
using System.Net.Http.Json;
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mfa_setup_enable_and_password_change_preserve_current_cookie_scope_and_expiry(bool platform)
    {
        const string password = "Synthetic-mfa-cookie-Password-43!";
        const string newPassword = "Synthetic-mfa-cookie-NewPassword-44!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            if (platform)
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
                await db.SaveChangesAsync();
            }
        }
        using var cookieFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                    ["Storage:CompanyMediaPath"] = _mediaPath
                }));
            builder.ConfigureTestServices(services => services.PostConfigure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.Zero));
        });
        using var client = cookieFactory.CreateClient();
        async Task<HttpResponseMessage> PostAsync(string path, object body)
        {
            using var csrf = await client.GetAsync("/api/session/csrf");
            using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            request.Headers.Add("X-CSRF-TOKEN", csrfBody.RootElement.GetProperty("requestToken").GetString());
            return await client.SendAsync(request);
        }
        using var login = await PostAsync("/api/session/login", new { email, password, platformAdminOnly = platform,
            companyId = platform ? (Guid?)null : _companyId });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var options = cookieFactory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        var ticket = options.TicketDataFormat.Unprotect(login.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(options.Cookie.Name + "=", StringComparison.Ordinal)).Split(';', 2)[0].Split('=', 2)[1])!;
        var originalExpiry = ticket.Properties.ExpiresUtc;
        var sessionId = ticket.Principal.FindFirst("session_id")!.Value;
        void AssertPreservedCookie(HttpResponseMessage response)
        {
            // Security-stamp validation can append a renewal cookie before the
            // explicit sign-in cookie. The final same-name cookie is effective.
            var nextTicket = options.TicketDataFormat.Unprotect(response.Headers.GetValues("Set-Cookie")
                .Last(value => value.StartsWith(options.Cookie.Name + "=", StringComparison.Ordinal)).Split(';', 2)[0].Split('=', 2)[1])!;
            Assert.NotNull(nextTicket);
            Assert.Equal(originalExpiry, nextTicket.Properties.ExpiresUtc);
            Assert.Equal(sessionId, nextTicket.Principal.FindFirst("session_id")!.Value);
            Assert.Equal(platform ? null : _companyId.ToString(), nextTicket.Principal.FindFirst("company_id")?.Value);
            Assert.Equal(platform, nextTicket.Principal.HasClaim("platform_admin", "true"));
            Assert.False(nextTicket.Principal.HasClaim(claim => claim.Type == "platform_reauth_at"));
        }
        using var setup = await PostAsync("/api/session/2fa/setup", new { currentPassword = password });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        AssertPreservedCookie(setup);
        using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        var secret = setupBody.RootElement.GetProperty("secret").GetString()!;
        using var enable = await PostAsync("/api/session/2fa/enable", new { code = GenerateAuthenticatorCode(secret) });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        AssertPreservedCookie(enable);
        using var enabledStatus = await client.GetAsync("/api/session/2fa");
        Assert.Equal(HttpStatusCode.OK, enabledStatus.StatusCode);
        using var enabledBody = JsonDocument.Parse(await enabledStatus.Content.ReadAsStringAsync());
        Assert.True(enabledBody.RootElement.GetProperty("enabled").GetBoolean());
        using var changed = await PostAsync("/api/session/password/change", new { currentPassword = password,
            newPassword, confirmPassword = newPassword, twoFactorCode = GenerateAuthenticatorCode(secret) });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        AssertPreservedCookie(changed);
        using var sessionResponse = await client.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        using var disabled = await PostAsync("/api/session/2fa/disable", new { currentPassword = newPassword,
            code = GenerateAuthenticatorCode(secret) });
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        AssertPreservedCookie(disabled);
        using var disabledStatus = await client.GetAsync("/api/session/2fa");
        Assert.Equal(HttpStatusCode.OK, disabledStatus.StatusCode);
        using var disabledBody = JsonDocument.Parse(await disabledStatus.Content.ReadAsStringAsync());
        Assert.False(disabledBody.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enabled_mfa_requires_a_code_on_each_fresh_device_even_when_remembered(bool platform)
    {
        const string password = "Synthetic-fresh-device-Password-43!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        string secret;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
            secret = (await users.GetAuthenticatorKeyAsync(user))!;
            Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
            if (platform)
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
                await db.SaveChangesAsync();
            }
        }
        using var cookieFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                    ["Storage:CompanyMediaPath"] = _mediaPath
                }));
        });
        using var firstDevice = cookieFactory.CreateClient();
        using var secondDevice = cookieFactory.CreateClient();
        foreach (var device in new[] { firstDevice, secondDevice })
        {
            using var csrf = await device.GetAsync("/api/session/csrf");
            using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
            var token = csrfBody.RootElement.GetProperty("requestToken").GetString()!;
            using var passwordOnly = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
            {
                Content = JsonContent.Create(new { email, password, rememberMe = true,
                    platformAdminOnly = platform, companyId = platform ? (Guid?)null : _companyId })
            };
            passwordOnly.Headers.Add("X-CSRF-TOKEN", token);
            using var challenge = await device.SendAsync(passwordOnly);
            Assert.Equal(HttpStatusCode.Conflict, challenge.StatusCode);
            using var challengeBody = JsonDocument.Parse(await challenge.Content.ReadAsStringAsync());
            Assert.True(challengeBody.RootElement.GetProperty("twoFactorRequired").GetBoolean());
            Assert.DoesNotContain(challenge.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
                cookie => cookie.StartsWith("EnvanterTR.Session=", StringComparison.Ordinal));
            using var notSignedIn = await device.GetAsync("/api/session");
            Assert.Equal(HttpStatusCode.Unauthorized, notSignedIn.StatusCode);
            using var verifiedLogin = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
            {
                Content = JsonContent.Create(new { email, password, rememberMe = true,
                    platformAdminOnly = platform, companyId = platform ? (Guid?)null : _companyId,
                    twoFactorCode = GenerateAuthenticatorCode(secret) })
            };
            verifiedLogin.Headers.Add("X-CSRF-TOKEN", token);
            using var verified = await device.SendAsync(verifiedLogin);
            Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
            using var signedIn = await device.GetAsync("/api/session");
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        }
    }

    [Theory]
    [InlineData(true, false, 720, false, 8)]
    [InlineData(true, true, 720, false, 720)]
    [InlineData(false, false, 720, false, 24)]
    [InlineData(false, true, 720, false, 720)]
    [InlineData(false, false, 6, false, 6)]
    [InlineData(false, true, 6, false, 6)]
    [InlineData(false, false, 6, true, 6)]
    [InlineData(false, true, 720, true, 720)]
    public async Task Login_cookie_and_server_expiry_match_platform_and_company_policy(
        bool platform, bool rememberMe, int policyHours, bool enrollMfa, int expectedHours)
    {
        const string password = "Synthetic-session-duration-Password-43!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        string? secret = null;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(item => item.Id == _companyId);
            company.SecuritySessionDurationHours = policyHours;
            company.SecurityRequireMfa = enrollMfa;
            if (platform)
            {
                db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
                Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
                secret = await users.GetAuthenticatorKeyAsync(user);
                Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
            }
            await db.SaveChangesAsync();
        }

        using var cookieFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                    ["Storage:CompanyMediaPath"] = _mediaPath
                }));
        });
        using var client = cookieFactory.CreateClient();
        using var csrf = await client.GetAsync("/api/session/csrf");
        using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfBody.RootElement.GetProperty("requestToken").GetString()!;
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = JsonContent.Create(new { email, password, rememberMe,
                companyId = platform ? (Guid?)null : _companyId, platformAdminOnly = platform,
                twoFactorCode = secret is null ? null : GenerateAuthenticatorCode(secret) })
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", token);
        using var login = await client.SendAsync(loginRequest);
        HttpResponseMessage? enrollment = null;
        try
        {
            if (enrollMfa)
            {
                Assert.Equal(HttpStatusCode.Conflict, login.StatusCode);
                using var challengeBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
                var ticket = challengeBody.RootElement.GetProperty("setupTicket").GetString();
                using var setupRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/company-mfa/setup")
                    { Content = JsonContent.Create(new { ticket }) };
                setupRequest.Headers.Add("X-CSRF-TOKEN", token);
                using var setup = await client.SendAsync(setupRequest);
                Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
                using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
                using var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/company-mfa/verify")
                    { Content = JsonContent.Create(new { ticket, code = GenerateAuthenticatorCode(setupBody.RootElement.GetProperty("secret").GetString()!) }) };
                verifyRequest.Headers.Add("X-CSRF-TOKEN", token);
                enrollment = await client.SendAsync(verifyRequest);
            }
            var signedIn = enrollment ?? login;
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
            var options = cookieFactory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(IdentityConstants.ApplicationScheme);
            var cookie = signedIn.Headers.GetValues("Set-Cookie")
                .Single(value => value.StartsWith(options.Cookie.Name + "=", StringComparison.Ordinal));
            Assert.Equal(rememberMe, cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
            var protectedTicket = cookie.Split(';', 2)[0].Split('=', 2)[1];
            var ticketData = options.TicketDataFormat.Unprotect(protectedTicket);
            Assert.NotNull(ticketData);
            Assert.Equal(rememberMe, ticketData.Properties.IsPersistent);
            Assert.False(ticketData.Properties.AllowRefresh);
            var sessionId = Guid.Parse(ticketData.Principal.FindFirst("session_id")!.Value);

            await using var verifyScope = _factory.Services.CreateAsyncScope();
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.UserSessions.SingleAsync(item => item.Id == sessionId);
            Assert.Equal(TimeSpan.FromHours(expectedHours), session.ExpiresAt - session.CreatedAt);
            Assert.InRange(Math.Abs((ticketData.Properties.ExpiresUtc!.Value - session.ExpiresAt).TotalSeconds), 0, 1);
            Assert.Equal(platform ? (Guid?)null : _companyId, session.CompanyId);
            using var authenticated = await client.GetAsync("/api/session");
            Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
            if (authenticated.Headers.TryGetValues("Set-Cookie", out var refreshedCookies))
            {
                var refreshedCookie = refreshedCookies.LastOrDefault(value => value.StartsWith(options.Cookie.Name + "=", StringComparison.Ordinal));
                if (refreshedCookie is not null)
                {
                    var refreshedTicket = options.TicketDataFormat.Unprotect(refreshedCookie.Split(';', 2)[0].Split('=', 2)[1]);
                    Assert.NotNull(refreshedTicket);
                    Assert.Equal(ticketData.Properties.ExpiresUtc, refreshedTicket.Properties.ExpiresUtc);
                    Assert.Equal(rememberMe, refreshedTicket.Properties.IsPersistent);
                }
            }

            session.CreatedAt = DateTimeOffset.UtcNow.AddDays(-31);
            session.LastSeenAt = session.CreatedAt;
            session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            using var expired = await client.GetAsync("/api/session");
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        }
        finally
        {
            enrollment?.Dispose();
        }
    }
}
