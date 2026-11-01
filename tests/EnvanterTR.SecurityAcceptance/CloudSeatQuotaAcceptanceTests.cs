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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cloud_plan_user_quota_is_public_and_enforced_without_removing_existing_members(bool platform)
    {
        var plan = new SubscriptionPlan { Code = $"seat-{Guid.NewGuid():N}", Name = "Seat acceptance", DeploymentKind = "cloud", MaxLoginUsers = 1, IsActive = true, IsPublic = true };
        int existingMembers;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (platform) db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            existingMembers = await db.CompanyMemberships.CountAsync(item => item.CompanyId == _companyId);
            db.SubscriptionPlans.Add(plan);
            db.TenantSubscriptions.Add(new TenantSubscription { CompanyId = _companyId, PlanId = plan.Id, Status = "Active", StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), EndsAt = DateTimeOffset.UtcNow.AddDays(30) });
            await db.SaveChangesAsync();
        }
        using var plansResponse = await _client!.GetAsync("/api/plans");
        using var plans = JsonDocument.Parse(await plansResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, plans.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == plan.Id).GetProperty("maxLoginUsers").GetInt32());
        using var csrf = platform ? await SendAsPlatformAdminAsync("/api/session/csrf") : await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0]).First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var email = $"seat-{Guid.NewGuid():N}@example.invalid";
        Task<HttpResponseMessage> Invite(string address) => platform
            ? SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/members/invite",
                JsonContent.Create(new { companyId = _companyId, fullName = "Seat acceptance", email = address, roleKey = "viewer" }), token, cookie)
            : SendAsUserAsync(HttpMethod.Post, "/api/members/invite", _userId, _companyId,
                JsonContent.Create(new { fullName = "Seat acceptance", email = address, roleKey = "viewer" }), token, cookie, role: "admin");
        using (var denied = await Invite(email)) Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Users.AnyAsync(user => user.Email == email));
            await db.SubscriptionPlans.Where(item => item.Id == plan.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.MaxLoginUsers, existingMembers + 1));
        }
        using (var admitted = await Invite(email)) Assert.Equal(HttpStatusCode.Created, admitted.StatusCode);
        using (var full = await Invite($"other-{email}")) Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(existingMembers + 1, await db.CompanyMemberships.CountAsync(item => item.CompanyId == _companyId));
        }
    }
}
