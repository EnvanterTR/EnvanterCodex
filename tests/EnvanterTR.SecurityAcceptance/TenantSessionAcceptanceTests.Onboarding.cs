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
    private async Task<CompanyOnboardingEndpoints.OnboardingState> OnboardingStateAsync()
    {
        using var response = await SendAsAdminAsync("/api/company/onboarding");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        return (await response.Content.ReadFromJsonAsync<CompanyOnboardingEndpoints.OnboardingState>())!;
    }

    private async Task<(string Token, string Cookie)> OnboardingCsrfAsync()
    {
        using var response = await SendAsAdminAsync("/api/session/csrf");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (json.RootElement.GetProperty("requestToken").GetString()!, response.Headers.GetValues("Set-Cookie")
            .Select(v => v.Split(';', 2)[0]).First(v => v.Contains("Antiforgery", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Onboarding_five_steps_use_real_tenant_records_and_parallel_completion_is_persistent_once()
    {
        var initial = await OnboardingStateAsync();
        Assert.Equal(new[] { "profile", "location", "employee", "asset", "assignment" }, initial.Steps.Select(s => s.Key));
        Assert.False(initial.Ready); Assert.Null(initial.CompletedAt);
        var csrf = await OnboardingCsrfAsync();
        using (var incomplete = await SendAsRoleAsync(HttpMethod.Post, "/api/company/onboarding/complete", "admin",
            requestToken: csrf.Token, antiforgeryCookie: csrf.Cookie)) Assert.Equal(HttpStatusCode.Conflict, incomplete.StatusCode);

        string assetName; string? passwordHash;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(c => c.Id == _companyId); company.ContactEmail = "setup@example.invalid";
            db.CompanyLocations.Add(new CompanyLocation { CompanyId = _companyId, Name = "Setup office", NormalizedName = "SETUP OFFICE" });
            var asset = await db.Assets.SingleAsync(a => a.Id == _ownAssetId); assetName = asset.Name;
            asset.Status = AssetStatuses.Deployed; asset.AssignedEmployeeId = _ownEmployeeId;
            passwordHash = await db.Users.Where(u => u.Id == _userId).Select(u => u.PasswordHash).SingleAsync();
            await db.SaveChangesAsync();
        }
        var ready = await OnboardingStateAsync(); Assert.Equal(5, ready.ReadyCount); Assert.True(ready.Ready); Assert.Null(ready.CompletedAt);
        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => SendAsRoleAsync(HttpMethod.Post,
            "/api/company/onboarding/complete", "admin", requestToken: csrf.Token, antiforgeryCookie: csrf.Cookie)));
        var completed = new List<DateTimeOffset?>();
        foreach (var response in responses)
            using (response) { Assert.Equal(HttpStatusCode.OK, response.StatusCode); completed.Add((await response.Content.ReadFromJsonAsync<CompanyOnboardingEndpoints.OnboardingState>())!.CompletedAt); }
        Assert.All(completed, at => Assert.NotNull(at)); Assert.Single(completed.Distinct());
        Assert.Equal(completed[0], (await OnboardingStateAsync()).CompletedAt);
        await using var final = _factory!.Services.CreateAsyncScope(); var verify = final.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await verify.AuditEntries.SingleAsync(a => a.CompanyId == _companyId && a.Action == "CompanyOnboardingCompleted");
        Assert.Equal(_userId, audit.ActorUserId); Assert.Equal(_companyId, audit.TargetId);
        Assert.False(await verify.AuditEntries.AnyAsync(a => a.CompanyId == _otherCompanyId && a.Action == "CompanyOnboardingCompleted"));
        Assert.Equal(assetName, await verify.Assets.Where(a => a.Id == _ownAssetId).Select(a => a.Name).SingleAsync());
        Assert.Equal(passwordHash, await verify.Users.Where(u => u.Id == _userId).Select(u => u.PasswordHash).SingleAsync());
    }

    [Fact]
    public async Task Onboarding_does_not_count_inactive_archived_deleted_or_other_tenant_records()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Employees.Where(e => e.CompanyId == _companyId).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false));
            await db.Assets.Where(a => a.CompanyId == _companyId).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AssetStatuses.Archived));
            var category = await db.AssetCategories.FirstAsync(c => c.CompanyId == _companyId);
            db.Assets.Add(new Asset { CompanyId = _companyId, AssetTag = "SETUP-DELETED", Name = "Deleted", CategoryId = category.Id,
                Status = AssetStatuses.Deployed, AssignedEmployeeId = _ownEmployeeId, DeletedAt = DateTimeOffset.UtcNow });
            db.CompanyLocations.Add(new CompanyLocation { CompanyId = _companyId, Name = "Inactive", NormalizedName = "INACTIVE", IsActive = false });
            db.CompanyLocations.Add(new CompanyLocation { CompanyId = _otherCompanyId, Name = "Other office", NormalizedName = "OTHER OFFICE" });
            var other = await db.Assets.SingleAsync(a => a.Id == _otherCompanyAssetId); other.Status = AssetStatuses.Deployed; other.AssignedEmployeeId = _otherCompanyEmployeeId;
            await db.SaveChangesAsync();
        }
        var state = await OnboardingStateAsync(); Assert.Equal(0, state.ReadyCount); Assert.All(state.Steps, s => Assert.False(s.Ready));
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Assets.Where(a => a.Id == _ownAssetId).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AssetStatuses.Deployed).SetProperty(a => a.AssignedEmployeeId, _ownEmployeeId));
        }
        Assert.False((await OnboardingStateAsync()).Steps.Single(s => s.Key == "assignment").Ready);
    }

    [Fact]
    public async Task Onboarding_requires_membership_administration_and_csrf_and_ignores_supplied_company_id()
    {
        using (var anonymous = await _client!.GetAsync("/api/company/onboarding")) Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post })
        {
            var path = "/api/company/onboarding" + (method == HttpMethod.Post ? "/complete" : "");
            using var viewer = await SendAsUserAsync(method, path, _viewerUserId, _companyId, role: "viewer");
            Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
            using var forged = await SendAsUserAsync(method, path, _userId, _otherCompanyId);
            Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        }
        using (var noCsrf = await SendAsRoleAsync(HttpMethod.Post, "/api/company/onboarding/complete", "admin")) Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using (var queryOverride = await SendAsAdminAsync($"/api/company/onboarding?companyId={_otherCompanyId}"))
        {
            Assert.Equal(HttpStatusCode.OK, queryOverride.StatusCode);
            var state = (await queryOverride.Content.ReadFromJsonAsync<CompanyOnboardingEndpoints.OnboardingState>())!;
            Assert.Equal((await OnboardingStateAsync()).ReadyCount, state.ReadyCount);
        }
    }
}
