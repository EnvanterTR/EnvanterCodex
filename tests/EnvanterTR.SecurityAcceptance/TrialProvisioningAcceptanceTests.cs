using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Platform_can_provision_free_trial_without_claiming_payment()
    {
        var plan = new SubscriptionPlan { Code = $"pilot-{Guid.NewGuid():N}", Name = "Pilot acceptance", DeploymentKind = "cloud", IsActive = true, IsPublic = true };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        using var csrf = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var token = csrf.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrfResponse.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0])
            .First(x => x.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var slug = $"pilot-{Guid.NewGuid():N}";
        var email = $"pilot-{Guid.NewGuid():N}@example.invalid";
        foreach (var invalid in new[] {
            new { mode = "trial", days = 365, paid = false, reference = (string?)null },
            new { mode = "trial", days = 15, paid = true, reference = (string?)null },
            new { mode = "trial", days = 15, paid = false, reference = (string?)"claimed-payment" },
            new { mode = "unknown", days = 15, paid = false, reference = (string?)null } })
        {
            using var denied = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/companies",
                JsonContent.Create(new { companyName = "Invalid pilot", slug, ownerName = "Pilot owner", ownerEmail = email,
                    planId = plan.Id, termDays = invalid.days, provisioningMode = invalid.mode,
                    paymentVerified = invalid.paid, paymentReference = invalid.reference }), token, cookie);
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        }
        using (var denied = await SendAsRoleAsync(HttpMethod.Post, "/api/platform/companies", "admin",
            JsonContent.Create(new { companyName = "Unauthorized pilot", slug, ownerName = "Pilot owner", ownerEmail = email,
                planId = plan.Id, termDays = 15, provisioningMode = "trial", paymentVerified = false }), token, cookie))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var response = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/companies",
            JsonContent.Create(new { companyName = "Free pilot acceptance", slug, ownerName = "Pilot owner",
                ownerEmail = email, planId = plan.Id,
                termDays = 15, provisioningMode = "trial", paymentVerified = false }), token, cookie);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Trial", payload.RootElement.GetProperty("status").GetString());
        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = await verifyDb.Companies.SingleAsync(x => x.Slug == slug);
        var subscription = await verifyDb.TenantSubscriptions.SingleAsync(x => x.CompanyId == company.Id);
        Assert.True(subscription.IsDemo);
        Assert.Null(subscription.PaymentReference);
        Assert.Equal(15, (subscription.EndsAt - subscription.StartsAt).Days);
        Assert.True(company.SecurityRequireMfa);
        Assert.Equal("owner", (await verifyDb.CompanyMemberships.SingleAsync(x => x.CompanyId == company.Id)).RoleKey);
        Assert.False(await verifyDb.CompanyCheckoutOrders.AnyAsync(x => x.CompanyId == company.Id));
        Assert.False(await verifyDb.PlatformAuditEntries.AnyAsync(x => x.TargetId == company.Id && x.Action == "PaidCompanyProvisioned"));
        Assert.True(await verifyDb.PlatformAuditEntries.AnyAsync(x => x.TargetId == company.Id && x.Action == "TrialCompanyProvisioned"));

        // Complete the actual invitation and mandatory first-login MFA protocol.
        var invitation = _smtpCapture.Messages.Last(x => x.Contains($"To: {email}", StringComparison.OrdinalIgnoreCase));
        var (invitationEmail, invitationToken) = ExtractAccountLink(invitation);
        const string password = "Pilot.Acceptance-Password-47!";
        using var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        using var loginCsrf = await loginClient.GetAsync("/api/session/csrf");
        using var loginCsrfPayload = JsonDocument.Parse(await loginCsrf.Content.ReadAsStringAsync());
        var loginToken = loginCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var loginCookie = loginCsrf.Headers.GetValues("Set-Cookie").Select(x => x.Split(';', 2)[0])
            .First(x => x.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using (var accepted = await SendAnonymousEndpointAsync(loginClient, "/api/session/password/reset",
            JsonContent.Create(new { email = invitationEmail, token = invitationToken, newPassword = password }), loginToken, loginCookie))
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var challenge = await SendAnonymousLoginAsync(loginClient, email, password, null, loginToken, loginCookie, company.Id);
        Assert.Equal(HttpStatusCode.Conflict, challenge.StatusCode);
        using var challengeBody = JsonDocument.Parse(await challenge.Content.ReadAsStringAsync());
        Assert.True(challengeBody.RootElement.GetProperty("companyMfaSetupRequired").GetBoolean());
        var ticket = challengeBody.RootElement.GetProperty("setupTicket").GetString()!;
        using var setup = await SendAnonymousEndpointAsync(loginClient, "/api/session/company-mfa/setup",
            JsonContent.Create(new { ticket }), loginToken, loginCookie);
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        using var verified = await SendAnonymousEndpointAsync(loginClient, "/api/session/company-mfa/verify",
            JsonContent.Create(new { ticket, code = GenerateAuthenticatorCode(setupBody.RootElement.GetProperty("secret").GetString()!) }), loginToken, loginCookie);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        using var verifiedBody = JsonDocument.Parse(await verified.Content.ReadAsStringAsync());
        Assert.True(verifiedBody.RootElement.GetProperty("authenticated").GetBoolean());
        Assert.Equal(company.Id, verifiedBody.RootElement.GetProperty("companyId").GetGuid());

        Assert.Contains(verified.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("EnvanterTR.Session=", StringComparison.Ordinal));
        var ownerId = await verifyDb.CompanyMemberships.Where(member => member.CompanyId == company.Id)
            .Select(member => member.UserId).SingleAsync();
        using var onboarding = await SendAsUserAsync(HttpMethod.Get, "/api/onboarding/status", ownerId, company.Id,
            role: "owner");
        Assert.Equal(HttpStatusCode.OK, onboarding.StatusCode);
        using var onboardingBody = JsonDocument.Parse(await onboarding.Content.ReadAsStringAsync());
        Assert.Equal(5, onboardingBody.RootElement.GetProperty("totalSteps").GetInt32());
        Assert.Equal(1, onboardingBody.RootElement.GetProperty("completedSteps").GetInt32());
        var steps = onboardingBody.RootElement.GetProperty("steps").EnumerateArray().ToArray();
        Assert.Equal(new[] { "company", "branding", "import", "members", "firstAsset" },
            steps.Select(step => step.GetProperty("key").GetString()).ToArray());
        Assert.True(steps[0].GetProperty("complete").GetBoolean());
        Assert.False(steps[1].GetProperty("complete").GetBoolean());
        Assert.False(steps[2].GetProperty("complete").GetBoolean());
        Assert.False(steps[3].GetProperty("complete").GetBoolean());
        Assert.False(steps[4].GetProperty("complete").GetBoolean());
    }
}
