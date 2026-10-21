using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    private WebApplicationFactory<Program> EnabledTrialFactory() => _factory!.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["CloudTrials:Enabled"] = "true", ["TenantRouting:RootDomain"] = "envantertr.com" })));

    private async Task<SubscriptionPlan> SeedTrialPlan()
    {
        var plan = new SubscriptionPlan { Code = $"trial-{Guid.NewGuid():N}", Name = "Annual trial acceptance", DeploymentKind = "cloud", MaxLoginUsers = 1, AnnualPrice = 2400m, MonthlyPrice = 0m, IsActive = true, IsPublic = true, Currency = "TRY" };
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SubscriptionPlans.Add(plan); await db.SaveChangesAsync(); return plan;
    }

    private async Task ConfigureTrialCsrf(HttpClient client)
    {
        using var csrf = await client.GetAsync("/api/session/csrf");
        using var payload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", payload.RootElement.GetProperty("requestToken").GetString());
    }

    private string LastTrialToken()
    {
        var body = WebUtility.HtmlDecode(DecodeMimeBody(_smtpCapture.Messages.Last()));
        var match = Regex.Match(body, "href=\"([^\"]*trial\\.html[^\"]*)\"");
        Assert.True(match.Success, "Verification mail must contain a trial confirmation link.");
        var url = new Uri(match.Groups[1].Value);
        Assert.Equal(string.Empty, url.Query);
        return Uri.UnescapeDataString(url.Fragment["#token=".Length..]);
    }

    [Fact]
    public async Task Verified_cloud_trial_creates_exactly_fifteen_days_once_and_requires_mfa()
    {
        var plan = await SeedTrialPlan();
        await using var factory = EnabledTrialFactory(); using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var email = $"trial-{Guid.NewGuid():N}@example.invalid"; var slug = $"trial-{Guid.NewGuid():N}";
        var request = new RequestCloudTrial("Trial company", slug, "Trial owner", email, plan.Id, true, CloudTrialEndpoints.NoticeVersion);
        using (var sent = await client.PostAsJsonAsync("/api/trials/request", request)) Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Companies.AnyAsync(company => company.Slug == slug));
        var token = LastTrialToken(); Guid companyId;
        using (var confirmed = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token)))
        {
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            Assert.Equal("no-store", confirmed.Headers.CacheControl?.ToString());
            using var payload = JsonDocument.Parse(await confirmed.Content.ReadAsStringAsync());
            companyId = payload.RootElement.GetProperty("companyId").GetGuid();
            Assert.Equal($"https://{slug}.envantertr.com/", payload.RootElement.GetProperty("portalUrl").GetString());
            Assert.True(payload.RootElement.GetProperty("mfaRequired").GetBoolean());
            Assert.Contains("#email=", payload.RootElement.GetProperty("setupUrl").GetString());
        }
        using (var repeated = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token))) Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var duplicate = request with { Slug = $"again-{Guid.NewGuid():N}" };
        using (var sentAgain = await client.PostAsJsonAsync("/api/trials/request", duplicate)) Assert.Equal(HttpStatusCode.Accepted, sentAgain.StatusCode);
        using (var denied = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(LastTrialToken()))) Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        await using var finalScope = factory.Services.CreateAsyncScope();
        var db = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = await db.Companies.SingleAsync(item => item.Id == companyId);
        Assert.True(company.SecurityRequireMfa);
        var subscription = await db.TenantSubscriptions.SingleAsync(item => item.CompanyId == companyId);
        Assert.Equal("Trial", subscription.Status); Assert.False(subscription.IsDemo);
        Assert.Equal(TimeSpan.FromDays(15), subscription.EndsAt - subscription.StartsAt);
        Assert.Single(await db.CompanyMemberships.Where(member => member.CompanyId == companyId && member.RoleKey == "owner").ToListAsync());
        Assert.Equal(1, await db.AuditEntries.CountAsync(entry => entry.CompanyId == companyId && entry.Action == "CloudTrialStarted"));
    }

    [Fact]
    public async Task Verified_cloud_trial_preserves_existing_owner_credentials_and_rejects_tampered_proof()
    {
        var plan = await SeedTrialPlan();
        var email = $"existing-trial-{Guid.NewGuid():N}@example.invalid";
        Guid ownerId;
        string passwordHash, securityStamp;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var owner = new EnvanterUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Existing owner" };
            Assert.True((await users.CreateAsync(owner, "Acceptance-only!92Aa")).Succeeded);
            ownerId = owner.Id; passwordHash = owner.PasswordHash!; securityStamp = owner.SecurityStamp!;
        }
        await using var factory = EnabledTrialFactory(); using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var slug = $"existing-{Guid.NewGuid():N}";
        using (var sent = await client.PostAsJsonAsync("/api/trials/request",
            new RequestCloudTrial("Existing owner trial", slug, "Different submitted name", email, plan.Id, true, CloudTrialEndpoints.NoticeVersion)))
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var token = LastTrialToken();
        using (var tampered = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial("X" + token[1..])))
            Assert.Equal(HttpStatusCode.BadRequest, tampered.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Companies.AnyAsync(company => company.Slug == slug));
        using (var confirmed = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token)))
        {
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            using var payload = JsonDocument.Parse(await confirmed.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("setupUrl").ValueKind);
        }
        await using var finalScope = factory.Services.CreateAsyncScope();
        var db = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Users.SingleAsync(user => user.Id == ownerId);
        Assert.Equal(passwordHash, saved.PasswordHash); Assert.Equal(securityStamp, saved.SecurityStamp);
        Assert.True(saved.EmailConfirmed); Assert.Equal("Existing owner", saved.DisplayName);
        var company = await db.Companies.SingleAsync(company => company.Slug == slug);
        Assert.Equal(ownerId, (await db.CompanyMemberships.SingleAsync(member => member.CompanyId == company.Id)).UserId);
    }

    [Fact]
    public async Task Cloud_trial_defaults_closed_requires_csrf_and_rejects_unverified_tokens()
    {
        using var status = await _client!.GetAsync("/api/trials/status");
        using var statusPayload = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        Assert.False(statusPayload.RootElement.GetProperty("available").GetBoolean());
        await using var factory = EnabledTrialFactory(); using var client = factory.CreateClient();
        using (var missingCsrf = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial("invalid")))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await ConfigureTrialCsrf(client);
        using (var invalidToken = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial("invalid")))
            Assert.Equal(HttpStatusCode.BadRequest, invalidToken.StatusCode);
    }
}
