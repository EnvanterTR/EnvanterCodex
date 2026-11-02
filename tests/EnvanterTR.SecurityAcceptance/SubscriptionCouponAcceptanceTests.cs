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
    public async Task Platform_coupon_management_is_platform_only_validated_and_audited()
    {
        var code = $"ADMIN_{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        var plan = new SubscriptionPlan
        {
            Code = $"cp-{Guid.NewGuid():N}", Name = "Coupon Cloud plan", DeploymentKind = "cloud",
            MonthlyPrice = 100m, AnnualPrice = 1000m, Currency = "TRY", IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.PlatformAdministrators.AnyAsync(x => x.UserId == _userId))
                db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        SaveSubscriptionCouponRequest Request(string reason, decimal value = 15m, bool active = true) => new(
            code, "Percent", value, "TRY", plan.Id, DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(30), 20, 2, active, reason);

        using (var noCsrf = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/coupons",
                   JsonContent.Create(Request("Güzellik kampanyası"))))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using (var invalidValue = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/coupons",
                   JsonContent.Create(Request("Geçersiz indirim değeri", 150m)), token, cookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidValue.StatusCode);
        using (var invalidCode = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/coupons",
                   JsonContent.Create(Request("Geçersiz kupon kodu") with { Code = "BAD-" }), token, cookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidCode.StatusCode);
        using (var companyAdmin = await SendAsUserAsync(HttpMethod.Post, "/api/platform/coupons", _userId, _companyId,
                   JsonContent.Create(Request("Platform erişim sınaması")), token, cookie))
            Assert.Equal(HttpStatusCode.Forbidden, companyAdmin.StatusCode);

        using var created = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/coupons",
            JsonContent.Create(Request("Yaz dönemi müşteri kampanyası")), token, cookie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdPayload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var couponId = createdPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(code, createdPayload.RootElement.GetProperty("code").GetString());

        using (var duplicate = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/coupons",
                   JsonContent.Create(Request("Yinelenen promosyon kodu")), token, cookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using (var listed = await SendAsPlatformAdminAsync("/api/platform/coupons"))
        {
            Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
            using var listPayload = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
            var result = listPayload.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == couponId);
            Assert.Equal(plan.Name, result.GetProperty("planName").GetString());
            Assert.Equal(0, result.GetProperty("redemptionCount").GetInt32());
            Assert.True(result.GetProperty("isActive").GetBoolean());
        }

        using var updated = await SendAsPlatformAdminRequestAsync(HttpMethod.Put, $"/api/platform/coupons/{couponId}",
            JsonContent.Create(Request("Kampanya süresi sona erdi", 15m, active: false)), token, cookie);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var coupon = await verifyDb.SubscriptionCoupons.SingleAsync(x => x.Id == couponId);
        Assert.False(coupon.IsActive);
        Assert.Equal(1, await verifyDb.PlatformAuditEntries.CountAsync(x => x.TargetId == couponId &&
            x.Action == "SubscriptionCouponCreated"));
        Assert.Equal(1, await verifyDb.PlatformAuditEntries.CountAsync(x => x.TargetId == couponId &&
            x.Action == "SubscriptionCouponUpdated"));
    }
}
