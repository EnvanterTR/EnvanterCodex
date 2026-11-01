using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Checkout_expiration_marks_only_overdue_unpaid_orders_and_releases_initialization_leases()
    {
        var now = DateTimeOffset.UtcNow;
        var plan = new SubscriptionPlan
        {
            Code = $"expiry-{Guid.NewGuid():N}"[..18], Name = "Expiration acceptance", DeploymentKind = "cloud",
            MonthlyPrice = 100m, AnnualPrice = 1000m, Currency = "TRY", IsActive = true, IsPublic = false
        };
        var orders = new[]
        {
            CreateOrder("Pending", now.AddMinutes(-1)),
            CreateOrder("PaymentStarted", now.AddMinutes(-1)),
            CreateOrder("Pending", now.AddHours(24)),
            CreateOrder("PaymentStarted", now.AddHours(24)),
            CreateOrder("PaymentReview", now.AddMinutes(-1)),
            CreateOrder("ProvisioningFailed", now.AddMinutes(-1))
        };

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            foreach (var order in orders) order.PlanId = plan.Id;
            db.CompanyCheckoutOrders.AddRange(orders);
            await db.SaveChangesAsync();
        }

        int expired;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            expired = await CompanyCheckoutOrderExpiration.ExpireAsync(db, now, CancellationToken.None);
            Assert.Equal(0, await CompanyCheckoutOrderExpiration.ExpireAsync(db, now, CancellationToken.None));
            var persisted = await db.CompanyCheckoutOrders.AsNoTracking()
                .Where(order => orders.Select(item => item.Id).Contains(order.Id)).ToDictionaryAsync(order => order.Id);
            Assert.Equal("Expired", persisted[orders[0].Id].Status);
            Assert.Equal("Expired", persisted[orders[1].Id].Status);
            Assert.Null(persisted[orders[0].Id].InitializationLeaseUntil);
            Assert.Null(persisted[orders[1].Id].InitializationLeaseUntil);
            Assert.Equal("Pending", persisted[orders[2].Id].Status);
            Assert.Equal("PaymentStarted", persisted[orders[3].Id].Status);
            Assert.Equal("PaymentReview", persisted[orders[4].Id].Status);
            Assert.Equal("ProvisioningFailed", persisted[orders[5].Id].Status);
        }
        Assert.Equal(2, expired);

        static CompanyCheckoutOrder CreateOrder(string status, DateTimeOffset expiresAt)
        {
            var id = Guid.NewGuid();
            return new CompanyCheckoutOrder
            {
                Id = id, CompanyName = $"Expiry test {id:N}", Slug = $"expiry-{id:N}"[..19],
                ContactEmail = $"contact-{id:N}@example.invalid", OwnerName = "Expiry Test",
                OwnerEmail = $"owner-{id:N}@example.invalid", PlanId = Guid.NewGuid(), TermDays = 365,
                SubtotalAmount = 1000m, DiscountAmount = 0m, Amount = 1000m, Currency = "TRY",
                Status = status, CreatedAt = expiresAt.AddHours(-25), ExpiresAt = expiresAt,
                InitializationLeaseUntil = status == "Pending" ? expiresAt.AddMinutes(5) : null
            };
        }
    }
}
