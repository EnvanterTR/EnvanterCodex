namespace EnvanterTR.Web.Data;

public sealed class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? ContactEmail { get; set; }
    public string? BrandLogoPath { get; set; }
    public string BrandPrimaryColor { get; set; } = "#1769E0";
    public string? LabelPrintSettingsJson { get; set; }
    public string? AssetStatusLabelsJson { get; set; }
    public int SecurityPasswordMinimumLength { get; set; } = 12;
    public int SecuritySessionDurationHours { get; set; } = 720;
    public bool SecurityRequireMfa { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }
    public string CustomerLifecycleStage { get; set; } = "Unclassified";
    public Guid? PlatformOwnerUserId { get; set; }
    public DateTimeOffset? PlatformNextFollowUpAt { get; set; }
    public int AssetCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyMfaEnrollmentChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string TicketHash { get; set; }
    public bool RememberMe { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SetupStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class CompanyApiToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string TokenPrefix { get; set; }
    public required string TokenHash { get; set; }
    public required string ScopesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class SupplierCatalogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public string? LogoFileName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetContract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? ContractNumber { get; set; }
    public string? ContractType { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? Cost { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetContractAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid AssetId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetContractLicense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid LicenseId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetContractAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public string? Sha256 { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyDepartment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ParentCompanyUnitId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Code { get; set; }
    public string UnitType { get; set; } = CompanyUnitTypes.Organizational;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class CompanyUnitTypes
{
    public const string Organizational = "organizational";
    public const string LegalEntity = "legal_entity";
    public const string Branch = "branch";
    public static readonly string[] All = [Organizational, LegalEntity, Branch];
}

public sealed class CompanyLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ParentLocationId { get; set; }
    public Guid? CompanyUnitId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public bool IsDefault { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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

public sealed class SubscriptionCoupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public string? Currency { get; set; }
    public Guid? PlanId { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int? MaxRedemptions { get; set; }
    public int? MaxRedemptionsPerCustomer { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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

public sealed class CompanyCheckoutOrder
{
    public Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public required string Slug { get; set; }
    public required string ContactEmail { get; set; }
    public required string OwnerName { get; set; }
    public required string OwnerEmail { get; set; }
    public Guid PlanId { get; set; }
    public int TermDays { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public Guid? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public required string Status { get; set; }
    public string? CheckoutTokenHash { get; set; }
    public string? CheckoutPageUrlProtected { get; set; }
    public string? PaymentReference { get; set; }
    public Guid? CompanyId { get; set; }
    public string? DataProcessingNoticeVersion { get; set; }
    // Immutable purchase context; deliberately not cleared if a company/account is deleted.
    public Guid? TargetCompanyId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset? DataProcessingAcknowledgedAt { get; set; }
    public DateTimeOffset? InitializationLeaseUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
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

public sealed class PlatformDemoRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CompanyName { get; set; }
    public required string ContactName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? TeamSize { get; set; }
    public string? AssetCount { get; set; }
    public string? Message { get; set; }
    public required string Source { get; set; }
    public string Status { get; set; } = "New";
    public DateTimeOffset ConsentAt { get; set; } = DateTimeOffset.UtcNow;
    public required string ConsentNoticeVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? AssignedAdminUserId { get; set; }
    public DateTimeOffset? FollowUpAt { get; set; }
    public DateTimeOffset? LastContactedAt { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTimeOffset? DemoGrantedAt { get; set; }
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

public sealed class PlatformAuditRetentionPolicy
{
    public int Id { get; set; } = 1;
    public int? RetentionDays { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public Guid? CompanyUnitId { get; set; }
    public DateOnly? StartedOn { get; set; }
    public string? PrivateNotes { get; set; }
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
    public string? Website { get; set; }
    public string? SupportUrl { get; set; }
    public string? WarrantyUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
    public string? LogoFileName { get; set; }
}

public sealed class AssetModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public required string Name { get; set; }
    public string? ModelNumber { get; set; }
    public string? PhotoFileName { get; set; }
    public int? DepreciationMonths { get; set; }
    public decimal? DepreciationMinimum { get; set; }
    public string DepreciationMinimumType { get; set; } = "amount";
    public string DepreciationMethod { get; set; } = "linear";
}

public sealed class AssetModelAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ModelId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetCustomFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ModelId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string GroupName { get; set; } = "Genel";
    public required string FieldType { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public Guid? ParentAssetId { get; set; }
    public string Status { get; set; } = AssetStatuses.Available;
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public Guid? DefaultLocationCatalogId { get; set; }
    public Guid? AssignedEmployeeId { get; set; }
    public DateOnly? ExpectedReturnOn { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateOnly? InsuranceExpiresAt { get; set; }
    public DateOnly? NextAuditOn { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRequestable { get; set; }
    public bool IsByod { get; set; }
    public string? Notes { get; set; }
    public string? CustomFieldsJson { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public sealed class AssetReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RequestNote { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SavedAssetSearch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string FiltersJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SavedAssetImportMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Family { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string MappingJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomAssetReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string DefinitionJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DashboardPreference
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string VisibleWidgetsJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OperationalNotificationPreference
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string EnabledKindsJson { get; set; }
    public required string EmailEnabledKindsJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OperationalNotificationEmailDigest
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public string? LastError { get; set; }
}

public sealed class PlatformFollowUpEmailDigest
{
    public Guid UserId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public string? LastError { get; set; }
}

public sealed class OperationalNotificationState
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public required string Kind { get; set; }
    public Guid RecordId { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
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

public sealed class AssetAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? AssetEventId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EmployeeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EmployeeHandoverDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public required string DocumentNumber { get; set; }
    public required string SnapshotJson { get; set; }
    public required string SnapshotSha256 { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientSignaturePngBase64 { get; set; }
    public string? RecipientSignatureSha256 { get; set; }
    public DateTimeOffset? RecipientSignedAt { get; set; }
    public string? IssuerName { get; set; }
    public string? IssuerSignaturePngBase64 { get; set; }
    public string? IssuerSignatureSha256 { get; set; }
    public DateTimeOffset? IssuerSignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EmployeeHandoverEmailAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public required string RecipientEmail { get; set; }
    public required string SnapshotSha256 { get; set; }
    public required string PdfSha256 { get; set; }
    public string Status { get; set; } = "Sending";
    public string? ErrorType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class AssetMaintenance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public required string MaintenanceType { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Description { get; set; }
    public DateOnly? PlannedOn { get; set; }
    public DateOnly StartedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string Status { get; set; } = "InProgress";
    public decimal? Cost { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public string? CompletionNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetMaintenanceAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaintenanceId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Accessory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ModelNumber { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public string? ItemNumber { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public int MinimumQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public string? Notes { get; set; }
    public bool IsRequestable { get; set; }
    public string? PhotoFileName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? BrandId { get; set; }
    public required string Name { get; set; }
    public string? PhotoFileName { get; set; }
}

public sealed class AccessoryCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AccessoryId { get; set; }
    public string TargetType { get; set; } = "Employee";
    public Guid? EmployeeId { get; set; }
    public Guid? AssetId { get; set; }
    public string? Location { get; set; }
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CheckedOutAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastReturnedAt { get; set; }
}

public sealed class AccessoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AccessoryId { get; set; }
    public Guid? AssignmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryKit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryKitLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AccessoryKitId { get; set; }
    public required string ItemType { get; set; }
    public Guid? AccessoryId { get; set; }
    public Guid? ConsumableId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? ComponentId { get; set; }
    public int Quantity { get; set; }
}

public sealed class AccessoryPurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string OrderNumber { get; set; }
    public required string Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string Status { get; set; } = "Ordered";
    public string ApprovalStatus { get; set; } = "NotRequired";
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ApprovalNote { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedOn { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal TaxRatePercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryPurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid AccessoryId { get; set; }
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityReturned { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StockPurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string OrderNumber { get; set; }
    public required string Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string Status { get; set; } = "Ordered";
    public string ApprovalStatus { get; set; } = "NotRequired";
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ApprovalNote { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedOn { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal TaxRatePercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StockPurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? ConsumableId { get; set; }
    public Guid? ComponentId { get; set; }
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityReturned { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetPurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string OrderNumber { get; set; }
    public required string Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string Status { get; set; } = "Ordered";
    public string ApprovalStatus { get; set; } = "NotRequired";
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ApprovalNote { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedOn { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal TaxRatePercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyPurchaseBudget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int BudgetYear { get; set; }
    public required string Currency { get; set; }
    public decimal Amount { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetPurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityReturned { get; set; }
    public decimal UnitCost { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetPurchaseOrderAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PurchaseOrderReturn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string OrderType { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public required string ReturnNumber { get; set; }
    public DateOnly ReturnedOn { get; set; }
    public required string Reason { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PurchaseOrderReturnLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderReturnId { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? AssetId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StockPurchaseOrderAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessoryPurchaseOrderAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Consumable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }
    public string? ModelNumber { get; set; }
    public string? ItemNumber { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public int MinimumQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public string? Notes { get; set; }
    public bool IsRequestable { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConsumableMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ConsumableId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class InventoryRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public required string ItemType { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RequestNote { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? KitReturnedAt { get; set; }
    public string? KitReturnNote { get; set; }
}

public sealed class InventoryRequestKitFulfillmentLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryRequestId { get; set; }
    public required string ItemType { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public Guid? AccessoryAssignmentId { get; set; }
    public Guid? ComponentAllocationId { get; set; }
    public Guid? AssetId { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
}

public sealed class CompanyTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public Guid? AssignedEmployeeId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public required string Subject { get; set; }
    public required string Category { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SupportTicketMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public bool IsSupportReply { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SupportTicketEmailOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? MessageId { get; set; }
    public required string RecipientEmail { get; set; }
    public bool IsSupportMailbox { get; set; }
    public required string EventType { get; set; }
    public string? StatusSnapshot { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorType { get; set; }
}

public sealed class SoftwareLicense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Publisher { get; set; }
    public string? ProductVersion { get; set; }
    public string LicenseType { get; set; } = "Subscription";
    public string? LicenseReference { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string? PurchaseOrderReference { get; set; }
    public int Seats { get; set; }
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public bool AutoRenew { get; set; }
    public bool RequiresEulaAcceptance { get; set; }
    public bool IsArchived { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SoftwareLicensePurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid LicenseId { get; set; }
    public Guid? SupplierId { get; set; }
    public required string OrderNumber { get; set; }
    public DateOnly? OrderDate { get; set; }
    public int SeatsPurchased { get; set; } = 1;
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AssetInstalledSoftware
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public required string Fingerprint { get; set; }
    public required string Name { get; set; }
    public string? Publisher { get; set; }
    public string? Version { get; set; }
    public string? Architecture { get; set; }
    public DateOnly? InstallDate { get; set; }
    public required string Source { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsPresent { get; set; } = true;
}

public sealed class SoftwareLicenseAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid LicenseId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StorageFileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public bool IsEulaDocument { get; set; }
    public bool IsCurrentEula { get; set; }
    public string? EulaVersion { get; set; }
    public string? Sha256 { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SoftwareLicenseAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid LicenseId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EulaAcceptedAt { get; set; }
    public Guid? EulaAcceptedByUserId { get; set; }
    public Guid? EulaAttachmentId { get; set; }
    public string? EulaVersion { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class SoftwareLicenseEmployeeEulaAcceptance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid LicenseId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public required string AcceptedByEmailSnapshot { get; set; }
    public Guid EulaAttachmentId { get; set; }
    public required string EulaVersion { get; set; }
    public required string EulaSha256 { get; set; }
    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SoftwareLicenseEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid LicenseId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string EventType { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class InventoryComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }
    public string? ModelNumber { get; set; }
    public string? ItemNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Supplier { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
    public int MinimumQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ComponentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ComponentId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public int Quantity { get; set; }
    public int RemovedQuantity { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastRemovedAt { get; set; }
}

public sealed class ComponentMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ComponentId { get; set; }
    public Guid? AllocationId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class AssetStatuses
{
    public const string Available = "Available";
    public const string Deployed = "Deployed";
    public const string Pending = "Pending";
    public const string Maintenance = "Maintenance";
    public const string Unusable = "Unusable";
    public const string Archived = "Archived";
    public const string ReturnedToSupplier = "ReturnedToSupplier";
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
