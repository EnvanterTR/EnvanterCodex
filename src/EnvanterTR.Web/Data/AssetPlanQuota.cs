using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnvanterTR.Web.Data;

public static class AssetPlanQuota
{
    public const string ErrorCode = "asset_quota_exceeded";
    private const string DatabaseMessage = "asset_quota_exceeded";

    public static IResult ExceededResult() => Results.Json(new
    {
        code = ErrorCode,
        message = "Bu işlem planınızdaki varlık sınırını aşar. Planınızı yükseltin veya mevcut varlık kayıtlarını arşivleyin."
    }, statusCode: StatusCodes.Status402PaymentRequired);

    public static bool IsExceeded(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException { SqlState: "P0001", MessageText: DatabaseMessage }) return true;
        return false;
    }

    public static async Task<int?> GetLimitAsync(AppDbContext db, Guid companyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await (from subscription in db.TenantSubscriptions.AsNoTracking()
                      join plan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
                      where subscription.CompanyId == companyId
                          && (subscription.Status == "Active" || subscription.Status == "Trial")
                          && subscription.StartsAt <= now && subscription.EndsAt > now
                      select plan.MaxAssets).SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<int> GetUsageAsync(AppDbContext db, Guid companyId, CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking().Where(company => company.Id == companyId)
            .Select(company => company.AssetCount).SingleAsync(cancellationToken);
}
