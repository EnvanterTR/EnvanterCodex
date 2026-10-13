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
public sealed record EditSubscriptionRequest(Guid PlanId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Note);
public sealed record PlatformInviteMemberRequest(Guid CompanyId, string FullName, string Email, string RoleKey);
public sealed record PublicDemoRequest(string CompanyName, string ContactName, string Email, string? Phone,
    string? JobTitle, string? TeamSize, string? AssetCount, string? Message, bool Consent, string? Website);
public sealed record UpdatePlatformDemoRequest(string Status, Guid? AssignedAdminUserId, DateTimeOffset? FollowUpAt,
    Guid? CompanyId, string? Note);
public sealed record ProvisionCompanyRequest(string CompanyName, string Slug, string? ContactEmail,
    string OwnerName, string OwnerEmail, Guid PlanId, int TermDays, string PaymentReference, bool PaymentVerified);
public sealed record SetCompanyActiveRequest(bool IsActive, string? Note);
public sealed record SaveSiteContentRequest(string Title, string? Body, string? MediaPath, bool IsPublished);
