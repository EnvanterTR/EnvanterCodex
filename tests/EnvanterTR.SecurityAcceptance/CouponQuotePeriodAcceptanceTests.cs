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
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("inactive")]
    public async Task Public_coupon_quote_rejects_unavailable_periods_without_creating_orders_or_changing_company_data(string state)
    {
        var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var plan = new SubscriptionPlan
        {
            Code = $"period-{Guid.NewGuid():N}", Name = "Annual quote period acceptance",
            DeploymentKind = "cloud", MonthlyPrice = 0m, AnnualPrice = 2400m,
            Currency = "TRY", IsActive = true, IsPublic = true
        };
        var coupon = new SubscriptionCoupon
        {
            Code = $"PERIOD_{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            DiscountType = "Percent", DiscountValue = 10m, Currency = "TRY", PlanId = plan.Id,
            StartsAt = state == "future" ? now.AddDays(1) : now.AddDays(-2),
            EndsAt = state == "expired" ? now.AddDays(-1) : now.AddDays(2),
            IsActive = state != "inactive", CreatedByUserId = _userId
        };
        string companyBefore;
        int assetsBefore;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.SubscriptionCoupons.Add(coupon);
            await db.SaveChangesAsync();
            companyBefore = JsonSerializer.Serialize(await db.Companies.AsNoTracking().SingleAsync(x => x.Id == _companyId));
            assetsBefore = await db.Assets.IgnoreQueryFilters().CountAsync(x => x.CompanyId == _companyId);
        }

        using var client = _factory.CreateClient();
        using var rejected = await client.PostAsJsonAsync("/api/checkout/quote",
            new CheckoutCouponQuoteRequest(plan.Id, "annual", coupon.Code.ToLowerInvariant()));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        using var error = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        Assert.True(error.RootElement.GetProperty("errors").TryGetProperty("couponCode", out _));

        using var regular = await client.PostAsJsonAsync("/api/checkout/quote",
            new CheckoutCouponQuoteRequest(plan.Id, "annual", null));
        Assert.Equal(HttpStatusCode.OK, regular.StatusCode);
        using var quote = JsonDocument.Parse(await regular.Content.ReadAsStringAsync());
        Assert.Equal(2400m, quote.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal(0m, quote.RootElement.GetProperty("discount").GetDecimal());

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verifyDb.CompanyCheckoutOrders.AnyAsync(x => x.PlanId == plan.Id));
        Assert.Equal(companyBefore, JsonSerializer.Serialize(await verifyDb.Companies.AsNoTracking().SingleAsync(x => x.Id == _companyId)));
        Assert.Equal(assetsBefore, await verifyDb.Assets.IgnoreQueryFilters().CountAsync(x => x.CompanyId == _companyId));
        var persisted = await verifyDb.SubscriptionCoupons.AsNoTracking().SingleAsync(x => x.Id == coupon.Id);
        Assert.Equal(coupon.StartsAt, persisted.StartsAt);
        Assert.Equal(coupon.EndsAt, persisted.EndsAt);
        Assert.Equal(coupon.IsActive, persisted.IsActive);
    }
}
