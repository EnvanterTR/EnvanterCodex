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
    string? Currency);

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
    DateTimeOffset ExpectedUpdatedAt);

public sealed record CreateLookupRequest(string Name, Guid? BrandId = null);
public sealed record RenameLookupRequest(string Name);
public sealed record RenameDirectoryEntryRequest(string CurrentName, string NewName, bool Merge = false);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string DisplayName,
    string? Email,
    string? Department,
    Guid? ManagerEmployeeId = null);

public sealed record SetEmployeeManagerRequest(Guid? ManagerEmployeeId);

public sealed record AssignAssetRequest(Guid EmployeeId, string? Note);
public sealed record ReturnAssetRequest(string? Note);
public sealed record ChangeAssetStatusRequest(string Status, string? Note);
public sealed record BulkChangeAssetStatusRequest(Guid[] AssetIds, string Status, string? Note);
public sealed record BulkAssignAssetsRequest(Guid[] AssetIds, Guid EmployeeId, string? Note);
public sealed record BulkReturnAssetsRequest(Guid[] AssetIds, string? Note);
public sealed record BulkTransferAssetsRequest(Guid[] AssetIds, string TargetLocation, string? Notes);
public sealed record SelectedAssetExportRequest(Guid[] AssetIds);
public sealed record SetEmployeeActiveRequest(bool IsActive);

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
    DateTimeOffset UpdatedAt);
