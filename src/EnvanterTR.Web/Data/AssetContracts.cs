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
    bool? IsRequestable = null,
    Dictionary<string, string>? CustomFields = null,
    bool? IsByod = null,
    Guid? DefaultLocationCatalogId = null,
    string? Notes = null);

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
    bool? IsRequestable = null,
    Dictionary<string, string>? CustomFields = null,
    bool? IsByod = null,
    Guid? DefaultLocationCatalogId = null,
    string? Notes = null);

public sealed record AssetCustomFieldDefinitionRequest(
    string Key,
    string Name,
    string FieldType,
    string[]? Options,
    bool IsRequired,
    int SortOrder = 0);

public sealed record CreateLookupRequest(string Name, Guid? BrandId = null, int? DepreciationMonths = null,
    decimal? DepreciationMinimum = null, string? DepreciationMinimumType = null, string? DepreciationMethod = null,
    Guid? CategoryId = null, string? Website = null, string? SupportUrl = null, string? WarrantyUrl = null,
    string? SupportPhone = null, string? SupportEmail = null, string? ModelNumber = null);
public sealed record SetModelDepreciationRequest(int? Months, decimal? Minimum, string? MinimumType, string? Method);
public sealed record RenameLookupRequest(string Name, string? Website = null, string? SupportUrl = null,
    string? WarrantyUrl = null, string? SupportPhone = null, string? SupportEmail = null, bool UpdateBrandDetails = false,
    string? ModelNumber = null);
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

public sealed record AssignAssetRequest(Guid EmployeeId, string? Note, DateOnly? ExpectedReturnOn = null);
public sealed record ReturnAssetRequest(string? Note);
public sealed record ChangeAssetStatusRequest(string Status, string? Note);
public sealed record AssetTrashRequest(string? Note);
public sealed record BulkChangeAssetStatusRequest(Guid[] AssetIds, string Status, string? Note);
public sealed record BulkAssignAssetsRequest(Guid[] AssetIds, Guid EmployeeId, string? Note, DateOnly? ExpectedReturnOn = null);
public sealed record BulkReturnAssetsRequest(Guid[] AssetIds, string? Note);
public sealed record BulkTransferAssetsRequest(Guid[] AssetIds, string TargetLocation, string? Notes);
public sealed record BulkRecordAssetAuditRequest(Guid[] AssetIds, DateOnly AuditDate, string? Notes, DateOnly? NextAuditOn = null);
public sealed record SetAssetAuditScheduleRequest(DateOnly? NextAuditOn);
public sealed record SetAssetParentRequest(Guid? ParentAssetId, DateTimeOffset ExpectedUpdatedAt);
public sealed record AddAssetNoteRequest(string? Note);
public sealed record SelectedAssetExportRequest(Guid[] AssetIds);
public sealed record SetEmployeeActiveRequest(bool IsActive);
public sealed record BulkSetEmployeeActiveRequest(Guid[]? EmployeeIds, bool IsActive);
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
    Guid? LocationCatalogId = null,
    bool IsByod = false,
    DateOnly? ExpectedReturnOn = null,
    string? ModelPhotoUrl = null,
    string? ModelNumber = null,
    string? EmployeeNumber = null,
    string? DefaultLocation = null,
    string? CompanyName = null,
    decimal? PurchaseCost = null,
    string Currency = "TRY",
    decimal? CurrentValue = null);
