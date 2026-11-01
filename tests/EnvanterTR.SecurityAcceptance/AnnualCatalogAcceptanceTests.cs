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
    public async Task Annual_catalog_drafts_are_authorized_csrf_protected_idempotent_and_preserve_existing_plans()
    {
        using (var forbidden = await SendAsAdminAsync("/api/platform/plans/annual-catalog"))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var firstCode = AnnualCloudCatalog.Tiers[0].Code;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            db.SubscriptionPlans.Add(new SubscriptionPlan { Code = firstCode, Name = "Existing manual plan", DeploymentKind = "cloud", AnnualPrice = 777m, MaxLoginUsers = 3, IsPublic = false });
            await db.SaveChangesAsync();
        }
        using var preview = await SendAsPlatformAdminAsync("/api/platform/plans/annual-catalog");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        using var previewPayload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
        Assert.Equal(15, previewPayload.RootElement.GetProperty("trialDays").GetInt32());
        Assert.False(previewPayload.RootElement.GetProperty("taxIncluded").GetBoolean());
        Assert.Equal(8, previewPayload.RootElement.GetProperty("plans").GetArrayLength());
        Assert.All(AnnualCloudCatalog.Tiers, tier => Assert.Equal(tier.MonthlyEquivalent * 12, tier.AnnualPrice));
        using (var missingCsrf = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/plans/annual-catalog/drafts", JsonContent.Create(new { changeReason = "Approved annual catalog" })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var csrf = await SendAsPlatformAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0]).First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        Task<HttpResponseMessage> Create(string reason) => SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/plans/annual-catalog/drafts", JsonContent.Create(new { changeReason = reason }), token, cookie);
        using (var invalidReason = await Create("")) Assert.Equal(HttpStatusCode.BadRequest, invalidReason.StatusCode);
        using (var created = await Create("Approved annual commercial catalog"))
        {
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Assert.Equal(7, payload.RootElement.GetProperty("created").GetArrayLength());
            Assert.False(payload.RootElement.GetProperty("published").GetBoolean());
        }
        using (var repeated = await Create("Repeat annual catalog safely"))
        {
            Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
            using var payload = JsonDocument.Parse(await repeated.Content.ReadAsStringAsync());
            Assert.Equal(0, payload.RootElement.GetProperty("created").GetArrayLength());
        }
        await using var finalScope = _factory.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var codes = AnnualCloudCatalog.Tiers.Select(tier => tier.Code).ToArray();
        var saved = await finalDb.SubscriptionPlans.Where(plan => codes.Contains(plan.Code)).ToListAsync();
        Assert.Equal(8, saved.Count);
        Assert.All(saved, plan => { Assert.False(plan.IsPublic); Assert.Equal(0m, plan.MonthlyPrice); });
        Assert.Equal(777m, saved.Single(plan => plan.Code == firstCode).AnnualPrice);
        Assert.Equal(3, saved.Single(plan => plan.Code == firstCode).MaxLoginUsers);
        foreach (var tier in AnnualCloudCatalog.Tiers.Skip(1))
        {
            var plan = saved.Single(item => item.Code == tier.Code);
            Assert.Equal(tier.Users, plan.MaxLoginUsers);
            Assert.Equal(tier.AnnualPrice, plan.AnnualPrice);
        }
        Assert.Equal(7, await finalDb.PlatformAuditEntries.CountAsync(entry => entry.Action == "AnnualCatalogDraftCreated"));
    }
}
