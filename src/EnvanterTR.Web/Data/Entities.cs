namespace EnvanterTR.Web.Data;

public sealed class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? ContactEmail { get; set; }
    public string? BrandLogoPath { get; set; }
    public string BrandPrimaryColor { get; set; } = "#1769E0";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string DeploymentKind { get; set; }
    public int? MaxLoginUsers { get; set; }
    public int? MaxAssets { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public decimal OneTimePrice { get; set; }
    public bool SupportsOfflineUse { get; set; }
    public int OfflineGraceDays { get; set; } = 30;
    public string? FeatureFlagsJson { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class TenantSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public required string Status { get; set; }
    public bool IsDemo { get; set; }
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndsAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PlatformAdministrator
{
    public Guid UserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LocalLicenseGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public required string LicenseKeyHash { get; set; }
    public required string SignedPayload { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset OfflineGraceUntil { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PublicSiteContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ContentKey { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public string? MediaPath { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PlatformAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorUserId { get; set; }
    public Guid? CompanyId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

// Employee records are inventory subjects; they are deliberately not login accounts.
public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string EmployeeNumber { get; set; }
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
}

public sealed class AssetBrand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
}

public sealed class AssetModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? BrandId { get; set; }
    public required string Name { get; set; }
}

public sealed class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string AssetTag { get; set; }
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public string Status { get; set; } = AssetStatuses.Available;
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string EventType { get; set; }
    public string? Details { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class AssetStatuses
{
    public const string Available = "Available";
    public const string Deployed = "Deployed";
    public const string Maintenance = "Maintenance";
    public const string Archived = "Archived";
}

public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public required string Action { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
