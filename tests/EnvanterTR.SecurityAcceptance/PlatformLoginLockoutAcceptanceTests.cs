using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Platform_login_locks_after_five_failed_factors_and_rejects_correct_password_during_five_minute_lock(bool secondFactor)
    {
        const string password = "Synthetic-Lockout-Password-43!";
        string email;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            email = user.Email!;
            Assert.True((await users.AddPasswordAsync(user, password)).Succeeded);
            Assert.True((await users.SetLockoutEnabledAsync(user, true)).Succeeded);
            if (secondFactor)
            {
                Assert.True((await users.ResetAuthenticatorKeyAsync(user)).Succeeded);
                Assert.True((await users.SetTwoFactorEnabledAsync(user, true)).Succeeded);
            }
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                    ["Storage:CompanyMediaPath"] = _mediaPath
                }));
        });
        using var client = factory.CreateClient();
        using var csrf = await client.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfBody = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var csrfToken = csrfBody.RootElement.GetProperty("requestToken").GetString()!;
        var firstAttempt = DateTimeOffset.UtcNow;
        DateTimeOffset? lockedUntil = null;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
            {
                Content = JsonContent.Create(new { email,
                    password = secondFactor ? password : "wrong-synthetic-password", platformAdminOnly = true,
                    twoFactorCode = secondFactor ? "INVALID-SYNTHETIC-RECOVERY" : null })
            };
            request.Headers.Add("X-CSRF-TOKEN", csrfToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(secondFactor
                ? attempt == 5 ? HttpStatusCode.Locked : HttpStatusCode.BadRequest
                : HttpStatusCode.Unauthorized, response.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = (await users.FindByIdAsync(_userId.ToString()))!;
            Assert.Equal(attempt == 5, await users.IsLockedOutAsync(user));
            if (attempt < 5) Assert.Equal(attempt, user.AccessFailedCount);
            else lockedUntil = user.LockoutEnd;
        }
        Assert.NotNull(lockedUntil);
        Assert.InRange(lockedUntil!.Value, firstAttempt.AddMinutes(5), DateTimeOffset.UtcNow.AddMinutes(5));
        using var correctRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = JsonContent.Create(new { email, password, platformAdminOnly = true })
        };
        correctRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        using var correctResponse = await client.SendAsync(correctRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, correctResponse.StatusCode);
        using var protectedResponse = await client.GetAsync("/api/platform/overview");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyUsers = verifyScope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
        var persisted = (await verifyUsers.FindByIdAsync(_userId.ToString()))!;
        Assert.Equal(lockedUntil, persisted.LockoutEnd);
        Assert.True(await verifyUsers.IsLockedOutAsync(persisted));
    }
}
