namespace EnvanterTR.Web.Data;

public sealed record CreateAssetRequest(
    string AssetTag,
    string Name,
    Guid CategoryId,
    Guid? BrandId,
    Guid? ModelId,
    string? SerialNumber,
    string? Location,
    DateOnly? PurchaseDate,
    DateOnly? WarrantyExpiresAt,
    decimal? PurchaseCost,
    string? Currency,
    bool? IsRequestable = null);

public sealed record UpdateAssetRequest(
    string AssetTag,
    string Name,
    Guid CategoryId,
    Guid? BrandId,
    Guid? ModelId,
    string? SerialNumber,
    string? Location,
    DateOnly? PurchaseDate,
    DateOnly? WarrantyExpiresAt,
    decimal? PurchaseCost,
    string? Currency,
    DateTimeOffset ExpectedUpdatedAt,
    bool? IsRequestable = null);

public sealed record CreateLookupRequest(string Name, Guid? BrandId = null);
public sealed record RenameLookupRequest(string Name);
public sealed record RenameDirectoryEntryRequest(string CurrentName, string NewName, bool Merge = false);
public sealed record LocationCatalogRequest(string? Name, Guid? ParentLocationId = null);
public sealed record SetLocationDefaultRequest(bool IsDefault);
public sealed record SetLocationManagerRequest(Guid? EmployeeId);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string DisplayName,
    string? Email,
    string? Department,
    Guid? ManagerEmployeeId = null,
    string? JobTitle = null,
    string? Phone = null,
    string? Location = null,
    DateOnly? StartedOn = null);

public sealed record UpdateEmployeeRequest(
    string EmployeeNumber,
    string DisplayName,
    string? Email,
    string? Department,
    string? JobTitle = null,
    string? Phone = null,
    string? Location = null,
    DateOnly? StartedOn = null);

public sealed record SetEmployeeManagerRequest(Guid? ManagerEmployeeId);
public sealed record LinkCompanyMemberEmployeeRequest(Guid? EmployeeId);

public sealed record AssignAssetRequest(Guid EmployeeId, string? Note);
public sealed record ReturnAssetRequest(string? Note);
public sealed record ChangeAssetStatusRequest(string Status, string? Note);
public sealed record BulkChangeAssetStatusRequest(Guid[] AssetIds, string Status, string? Note);
public sealed record BulkAssignAssetsRequest(Guid[] AssetIds, Guid EmployeeId, string? Note);
public sealed record BulkReturnAssetsRequest(Guid[] AssetIds, string? Note);
public sealed record BulkTransferAssetsRequest(Guid[] AssetIds, string TargetLocation, string? Notes);
public sealed record BulkRecordAssetAuditRequest(Guid[] AssetIds, DateOnly AuditDate, string? Notes, DateOnly? NextAuditOn = null);
public sealed record SetAssetAuditScheduleRequest(DateOnly? NextAuditOn);
public sealed record SetAssetParentRequest(Guid? ParentAssetId, DateTimeOffset ExpectedUpdatedAt);
public sealed record SelectedAssetExportRequest(Guid[] AssetIds);
public sealed record SetEmployeeActiveRequest(bool IsActive);
public sealed record UpdateEmployeePrivateNotesRequest(string? Notes);

public sealed record AssetListItem(
    Guid Id,
    string AssetTag,
    string Name,
    string Status,
    string? SerialNumber,
    string? Location,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? ModelId,
    string? ModelName,
    string? AssignedEmployeeName,
    Guid? AssignedEmployeeId,
    DateOnly? WarrantyExpiresAt,
    DateTimeOffset UpdatedAt,
    Guid? PhotoAttachmentId = null,
    Guid? LocationCatalogId = null);
