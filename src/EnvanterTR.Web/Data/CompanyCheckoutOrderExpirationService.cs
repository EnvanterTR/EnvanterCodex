using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CompanyCheckoutOrderExpiration
{
    public static Task<int> ExpireAsync(AppDbContext db, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.CompanyCheckoutOrders
            .Where(order => (order.Status == "Pending" || order.Status == "PaymentStarted") && order.ExpiresAt <= now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(order => order.Status, "Expired")
                .SetProperty(order => order.InitializationLeaseUntil, (DateTimeOffset?)null), cancellationToken);
}

public sealed class CompanyCheckoutOrderExpirationWorker(IServiceScopeFactory scopeFactory,
    ILogger<CompanyCheckoutOrderExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var expiredCount = await CompanyCheckoutOrderExpiration.ExpireAsync(db, DateTimeOffset.UtcNow, stoppingToken);
                if (expiredCount > 0)
                    logger.LogInformation("Expired {CheckoutOrderCount} unpaid checkout orders.", expiredCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogError("Checkout order expiration cycle failed ({ErrorType}).", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
