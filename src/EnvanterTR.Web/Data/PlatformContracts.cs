namespace EnvanterTR.Web.Data;

public sealed record SavePlanRequest(
    string Code,
    string Name,
    string? Description,
    string DeploymentKind,
    int? MaxLoginUsers,
    int? MaxAssets,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    decimal OneTimePrice,
    string Currency,
    bool SupportsOfflineUse,
    int OfflineGraceDays,
    string? FeatureFlagsJson,
    bool IsPublic,
    bool IsActive,
    int SortOrder);

public sealed record GrantDemoRequest(Guid PlanId, int Days, string? Note);
public sealed record ExtendSubscriptionRequest(int Days, string? Note);
public sealed record ProvisionCompanyRequest(string CompanyName, string Slug, string? ContactEmail,
    string OwnerName, string OwnerEmail, Guid PlanId, int TermDays, string PaymentReference, bool PaymentVerified);
public sealed record SetCompanyActiveRequest(bool IsActive, string? Note);
public sealed record SaveSiteContentRequest(string Title, string? Body, string? MediaPath, bool IsPublished);
