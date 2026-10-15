using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class TenantPlanFeatures
{
    public const string Reports = "reports";
    public static bool IsSupported(string featureKey) => string.Equals(featureKey, Reports, StringComparison.Ordinal);

    public static async Task<bool> IsEnabledAsync(Guid companyId, string featureKey,
        AppDbContext db, CancellationToken cancellationToken)
    {
        var plan = await (from subscription in db.TenantSubscriptions.AsNoTracking()
                          join subscriptionPlan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals subscriptionPlan.Id
                          where subscription.CompanyId == companyId
                          orderby subscription.StartsAt descending, subscription.CreatedAt descending
                          select new { subscriptionPlan.DeploymentKind, subscriptionPlan.FeatureFlagsJson })
            .FirstOrDefaultAsync(cancellationToken);

        // Plans created before feature enforcement and self-hosted installations stay compatible.
        if (plan is null || !string.Equals(plan.DeploymentKind, "cloud", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(plan.FeatureFlagsJson)) return true;

        try
        {
            using var document = JsonDocument.Parse(plan.FeatureFlagsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(featureKey, out var value)) return true;
            return value.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            // Invalid plan metadata must not silently grant a paid feature.
            return false;
        }
    }

    public static bool RequiresReports(PathString path) =>
        path.StartsWithSegments("/api/reports") || path.StartsWithSegments("/api/assets/custom-reports");

    public static IResult FeatureUnavailable(string featureKey) => Results.Json(new
    {
        code = "plan_feature_unavailable",
        feature = featureKey,
        message = "Bu özellik şirketinizin etkin Cloud planında bulunmuyor. Plan seçenekleri için şirket yöneticinizle iletişime geçin."
    }, statusCode: StatusCodes.Status402PaymentRequired);
}
