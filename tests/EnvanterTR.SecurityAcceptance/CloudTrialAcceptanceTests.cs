using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
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
    public async Task Cloud_trial_expired_email_proof_and_withdrawn_plan_create_no_accounts()
    {
        var plan = await SeedTrialPlan();
        await using var factory = EnabledTrialFactory(); using var client = factory.CreateClient();
        await ConfigureTrialCsrf(client);
        var email = $"expired-trial-{Guid.NewGuid():N}@example.invalid";
        var slug = $"expired-{Guid.NewGuid():N}";
        using (var sent = await client.PostAsJsonAsync("/api/trials/request",
            new RequestCloudTrial("Expired proof", slug, "Trial owner", email, plan.Id, true, CloudTrialEndpoints.NoticeVersion)))
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var token = LastTrialToken();
        var protector = factory.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("EnvanterTR.CloudTrial.Email.v1").ToTimeLimitedDataProtector();
        var expired = protector.Protect(protector.Unprotect(token), DateTimeOffset.UtcNow.AddMinutes(-1));
        using (var denied = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(expired)))
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Users.AnyAsync(user => user.Email == email));
            Assert.False(await db.Companies.AnyAsync(company => company.Slug == slug));
            await db.SubscriptionPlans.Where(item => item.Id == plan.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsPublic, false));
        }
        using (var denied = await client.PostAsJsonAsync("/api/trials/confirm", new ConfirmCloudTrial(token)))
            Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        await using var finalScope = factory.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await finalDb.Users.AnyAsync(user => user.Email == email));
        Assert.False(await finalDb.Companies.AnyAsync(company => company.Slug == slug));
    }

    [Fact]
    public async Task Concurrent_cloud_trial_confirmation_is_idempotent_without_extending_trial()
    {
        var plan = await SeedTrialPlan();
        await using var factory = EnabledTrialFactory();
        using var first = factory.CreateClient(); using var second = factory.CreateClient();
        await ConfigureTrialCsrf(first); await ConfigureTrialCsrf(second);
        var email = $"concurrent-trial-{Guid.NewGuid():N}@example.invalid";
        var slug = $"concurrent-{Guid.NewGuid():N}";
        using (var sent = await first.PostAsJsonAsync("/api/trials/request",
            new RequestCloudTrial("Concurrent trial", slug, "Trial owner", email, plan.Id, true, CloudTrialEndpoints.NoticeVersion)))
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var proof = new ConfirmCloudTrial(LastTrialToken());
        var responses = await Task.WhenAll(first.PostAsJsonAsync("/api/trials/confirm", proof),
            second.PostAsJsonAsync("/api/trials/confirm", proof));
        var companyIds = new List<Guid>(); var endDates = new List<DateTimeOffset>();
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                companyIds.Add(json.RootElement.GetProperty("companyId").GetGuid());
                endDates.Add(json.RootElement.GetProperty("endsAt").GetDateTimeOffset());
            }
        }
        Assert.Equal(companyIds[0], companyIds[1]); Assert.Equal(endDates[0], endDates[1]);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = Assert.Single(await db.Companies.Where(item => item.Slug == slug).ToListAsync());
        var subscription = Assert.Single(await db.TenantSubscriptions.Where(item => item.CompanyId == company.Id).ToListAsync());
        Assert.Equal(TimeSpan.FromDays(15), subscription.EndsAt - subscription.StartsAt);
        Assert.Equal(1, await db.Users.CountAsync(item => item.Email == email));
        Assert.Equal(1, await db.CompanyMemberships.CountAsync(item => item.CompanyId == company.Id));
        Assert.Equal(1, await db.AuditEntries.CountAsync(item => item.CompanyId == company.Id && item.Action == "CloudTrialStarted"));
    }

    [Fact]
    public async Task Concurrent_distinct_cloud_trials_for_one_email_grant_only_one_trial()
    {
        var plan = await SeedTrialPlan();
        await using var factory = EnabledTrialFactory();
        using var first = factory.CreateClient(); using var second = factory.CreateClient();
        await ConfigureTrialCsrf(first); await ConfigureTrialCsrf(second);
        var email = $"one-trial-{Guid.NewGuid():N}@example.invalid";
        var request = new RequestCloudTrial("One trial", $"one-{Guid.NewGuid():N}", "Trial owner", email,
            plan.Id, true, CloudTrialEndpoints.NoticeVersion);
        using (var sent = await first.PostAsJsonAsync("/api/trials/request", request))
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var firstProof = new ConfirmCloudTrial(LastTrialToken());
        var otherSlug = $"other-{Guid.NewGuid():N}";
        using (var sent = await second.PostAsJsonAsync("/api/trials/request", request with { Slug = otherSlug }))
            Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        var secondProof = new ConfirmCloudTrial(LastTrialToken());
        var responses = await Task.WhenAll(first.PostAsJsonAsync("/api/trials/confirm", firstProof),
            second.PostAsJsonAsync("/api/trials/confirm", secondProof));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = await db.Users.SingleAsync(item => item.Email == email);
        Assert.Equal(1, await db.CompanyMemberships.CountAsync(item => item.UserId == owner.Id));
        Assert.Equal(1, await db.AuditEntries.CountAsync(item => item.ActorUserId == owner.Id && item.Action == "CloudTrialStarted"));
        Assert.Equal(1, await db.Companies.CountAsync(item => item.Slug == request.Slug || item.Slug == otherSlug));
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
