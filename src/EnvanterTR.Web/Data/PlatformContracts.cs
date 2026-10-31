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
    int SortOrder,
    string? ChangeReason);

public sealed record SaveSubscriptionCouponRequest(
    string Code, string DiscountType, decimal DiscountValue, string Currency, Guid? PlanId,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, int? MaxRedemptions,
    int? MaxRedemptionsPerCustomer, bool IsActive, string? ChangeReason);

public sealed record GrantDemoRequest(Guid PlanId, int Days, string? Note);
public sealed record ExtendSubscriptionRequest(DateTimeOffset EndsAt, string? Note);
public sealed record EditSubscriptionRequest(Guid PlanId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Note);
public sealed record PlatformInviteMemberRequest(Guid CompanyId, string FullName, string Email, string RoleKey);
public sealed record UpdatePlatformMemberRoleRequest(string RoleKey, string? Note);
public sealed record RemovePlatformMembershipRequest(string? Note);
public sealed record PlatformMemberSecurityActionRequest(string Action, string? Note, int? LockoutDays);
public sealed record TransferCompanyOwnershipRequest(string NewOwnerEmail, string FormerOwnerRoleKey, string? Note,
    bool ConfirmTransfer);
public sealed record PlatformMemberTimelineEvent(DateTimeOffset OccurredAt, string EventType, string Title,
    string Description, string? CompanyName, string? PaymentReference = null, decimal? Amount = null, string? Currency = null);
public sealed record PublicDemoRequest(string CompanyName, string ContactName, string Email, string? Phone,
    string? JobTitle, string? TeamSize, string? AssetCount, string? Message, bool Consent, string? Website);
public sealed record UpdatePlatformDemoRequest(string Status, Guid? AssignedAdminUserId, DateTimeOffset? FollowUpAt,
    Guid? CompanyId, string? Note);
public sealed record RecordCheckoutPaymentFollowUpRequest(string Outcome, DateTimeOffset? NextFollowUpAt, string Note);
public sealed record ProvisionCompanyRequest(string CompanyName, string Slug, string? ContactEmail,
    string OwnerName, string OwnerEmail, Guid PlanId, int TermDays, string? PaymentReference, bool PaymentVerified,
    bool IsDemo = false, string? Note = null);
public sealed record SetCompanyActiveRequest(bool IsActive, string? Note);
public sealed record DeleteCompanyRequest(string ConfirmSlug, string? Note);
public sealed record RestoreCompanyRequest(string? Note);
public sealed record SetCompaniesActiveRequest(Guid[] CompanyIds, bool IsActive, string? Note);
public sealed record SetCompaniesPlanRequest(Guid[] CompanyIds, Guid PlanId, string? Note);
public sealed record UpdatePlatformCustomerLifecycleRequest(string Stage, Guid? AssignedAdminUserId,
    DateTimeOffset? NextFollowUpAt, string? Note);
public sealed record SaveSiteContentRequest(string Title, string? Body, string? MediaPath, bool IsPublished);
