using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class TenantPlanFeatures
{
    public const string Reports = "reports";
    public const string CustomerApi = "api";
    public const string Maintenance = "maintenance";
    public static bool IsSupported(string featureKey) =>
        string.Equals(featureKey, Reports, StringComparison.Ordinal) ||
        string.Equals(featureKey, CustomerApi, StringComparison.Ordinal) ||
        string.Equals(featureKey, Maintenance, StringComparison.Ordinal);

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

    public static bool RequiresMaintenance(PathString path)
    {
        if (path.StartsWithSegments("/api/maintenances") || path.StartsWithSegments("/api/reports/maintenances"))
            return true;
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is { Length: >= 4 } &&
               string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[1], "assets", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(segments[2], out _) &&
               string.Equals(segments[3], "maintenances", StringComparison.OrdinalIgnoreCase);
    }

    public static IResult FeatureUnavailable(string featureKey) => Results.Json(new
    {
        code = "plan_feature_unavailable",
        feature = featureKey,
        message = "Bu özellik şirketinizin etkin Cloud planında bulunmuyor. Plan seçenekleri için şirket yöneticinizle iletişime geçin."
    }, statusCode: StatusCodes.Status402PaymentRequired);
}
