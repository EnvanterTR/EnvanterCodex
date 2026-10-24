using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Platform_demo_provisioning_rejects_company_administrator()
    {
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var body = new ProvisionCompanyRequest("Synthetic demo", $"demo-{Guid.NewGuid():N}", null,
            "Synthetic owner", "demo@example.invalid", Guid.NewGuid(), 15, null, false, true,
            "Requested demonstration acceptance");
        using var denied = await SendAsUserAsync(HttpMethod.Post, "/api/platform/companies", _userId, _companyId,
            JsonContent.Create(body), token, cookie);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Platform_demo_provisioning_uses_shared_service_preserves_owner_and_records_no_payment(bool newOwner)
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var plan = new SubscriptionPlan { Code = $"demo-{Guid.NewGuid():N}", Name = "Synthetic demo plan",
            DeploymentKind = "cloud", IsActive = true, MaxLoginUsers = 1 };
        string email;
        string? stamp, passwordHash;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Id == _viewerUserId);
            email = newOwner ? $"demo-{Guid.NewGuid():N}@example.invalid" : owner.Email!;
            stamp = owner.SecurityStamp;
            passwordHash = owner.PasswordHash;
            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
        }
        var slug = $"demo-{Guid.NewGuid():N}";
        var body = new ProvisionCompanyRequest("Synthetic demo company", slug, email, "Synthetic owner", email,
            plan.Id, 15, null, false, IsDemo: true, Note: "Requested demonstration acceptance");
        using var created = await ImpersonationPost(client, "/api/platform/companies", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var payload = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("isDemo").GetBoolean());
        Assert.Equal("Trial", payload.GetProperty("status").GetString());
        var companyId = payload.GetProperty("id").GetGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(item => item.Id == companyId);
            Assert.Equal(slug, company.Slug);
            Assert.True(company.SecurityRequireMfa);
            var ownerMembership = await db.CompanyMemberships.SingleAsync(item => item.CompanyId == companyId);
            Assert.Equal("owner", ownerMembership.RoleKey);
            var owner = await db.Users.SingleAsync(item => item.Id == ownerMembership.UserId);
            Assert.Equal(!newOwner, owner.EmailConfirmed);
            if (!newOwner)
            {
                Assert.Equal(_viewerUserId, owner.Id);
                Assert.Equal(stamp, owner.SecurityStamp);
                Assert.Equal(passwordHash, owner.PasswordHash);
                Assert.Equal("viewer", await db.CompanyMemberships.Where(item => item.CompanyId == _companyId &&
                    item.UserId == owner.Id).Select(item => item.RoleKey).SingleAsync());
            }
            var subscription = await db.TenantSubscriptions.SingleAsync(item => item.CompanyId == companyId);
            Assert.True(subscription.IsDemo);
            Assert.Equal("Trial", subscription.Status);
            Assert.Null(subscription.PaymentReference);
            Assert.Equal(TimeSpan.FromDays(15), subscription.EndsAt - subscription.StartsAt);
            Assert.False(await db.CompanyCheckoutOrders.AnyAsync(item => item.CompanyId == companyId));
            Assert.Single(await db.PlatformAuditEntries.Where(item => item.CompanyId == companyId &&
                item.ActorUserId == _userId && item.Action == "DemoCompanyProvisioned").ToListAsync());
            Assert.False(await db.PlatformAuditEntries.AnyAsync(item => item.CompanyId == companyId &&
                item.Action == "PaidCompanyProvisioned"));
        }
        using var duplicate = await ImpersonationPost(client, "/api/platform/companies", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var timeline = await client.GetFromJsonAsync<JsonElement>(
            $"/api/platform/members/{payload.GetProperty("ownerId").GetGuid()}/timeline");
        Assert.Contains(timeline.GetProperty("items").EnumerateArray(), item =>
            item.GetProperty("title").GetString() == "Demo şirket hesabı oluşturuldu");
    }

    [Fact]
    public async Task Platform_demo_provisioning_rejects_paid_proof_missing_reason_and_excess_duration()
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var slug = $"demo-{Guid.NewGuid():N}";
        var plan = new SubscriptionPlan { Code = $"demo-{Guid.NewGuid():N}", Name = "Demo validation",
            DeploymentKind = "cloud", IsActive = true };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
        }
        var validShape = new ProvisionCompanyRequest("Synthetic demo", slug, null, "Synthetic owner",
            $"demo-{Guid.NewGuid():N}@example.invalid", plan.Id, 15, null, false, true, "Requested demonstration acceptance");
        foreach (var invalid in new[] { validShape with { Note = "" }, validShape with { TermDays = 61 },
            validShape with { PaymentVerified = true }, validShape with { PaymentReference = "fake-paid-proof" },
            validShape with { IsDemo = false } })
        {
            using var denied = await ImpersonationPost(client, "/api/platform/companies", invalid);
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        }
        await using var check = factory.Services.CreateAsyncScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<AppDbContext>().Companies.AnyAsync(item => item.Slug == slug));
    }

    [Fact]
    public async Task Platform_demo_provisioning_still_requires_platform_mfa()
    {
        var setup = await ImpersonationActor(mfa: false, stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var body = new ProvisionCompanyRequest("Synthetic demo", $"demo-{Guid.NewGuid():N}", null,
            "Synthetic owner", "demo@example.invalid", Guid.NewGuid(), 15, null, false, true,
            "Requested demonstration acceptance");
        using var denied = await ImpersonationPost(client, "/api/platform/companies", body);
        Assert.Equal((HttpStatusCode)428, denied.StatusCode);
    }

    [Fact]
    public async Task Platform_demo_provisioning_still_requires_csrf_after_fresh_mfa_login()
    {
        var setup = await ImpersonationActor(stepUp: false);
        using var factory = setup.Factory;
        using var client = setup.Client;
        var plan = new SubscriptionPlan { Code = $"demo-{Guid.NewGuid():N}", Name = "Demo CSRF",
            DeploymentKind = "cloud", IsActive = true };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
        }
        var body = new ProvisionCompanyRequest("Synthetic demo", $"demo-{Guid.NewGuid():N}", null,
            "Synthetic owner", $"demo-{Guid.NewGuid():N}@example.invalid", plan.Id, 15, null, false, true,
            "Requested demonstration acceptance");
        using var missingCsrf = await client.PostAsJsonAsync("/api/platform/companies", body);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }
}
