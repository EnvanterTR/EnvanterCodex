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
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyDepartment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ParentLocationId { get; set; }
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
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public Guid? LocationCatalogId { get; set; }
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
}

public sealed class AssetModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? BrandId { get; set; }
    public required string Name { get; set; }
}

public sealed class AssetCustomFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
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
    public Guid? AssignedEmployeeId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyExpiresAt { get; set; }
    public DateOnly? NextAuditOn { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRequestable { get; set; }
    public string? CustomFieldsJson { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
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

public sealed class AssetMaintenance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
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
    public string? Notes { get; set; }
    public bool IsRequestable { get; set; }
    public string? PhotoFileName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public Guid AccessoryId { get; set; }
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

public sealed class AssetPurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public required string Name { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
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
    public int Seats { get; set; }
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public bool AutoRenew { get; set; }
    public bool RequiresEulaAcceptance { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public DateTimeOffset? RevokedAt { get; set; }
    public string? Notes { get; set; }
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
