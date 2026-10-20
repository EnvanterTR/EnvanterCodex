using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal static class AssetContractLocationAccess
{
    public static IQueryable<AssetContract> InLocationScope(this IQueryable<AssetContract> contracts,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return contracts;

        var visibleAssignments = db.SoftwareLicenseAssignments.AsNoTracking().Where(assignment =>
            assignment.CompanyId == companyId && (assignment.EmployeeId.HasValue || assignment.AssetId.HasValue) &&
            (!assignment.EmployeeId.HasValue || db.Employees.Any(employee => employee.CompanyId == companyId &&
                employee.Id == assignment.EmployeeId && employee.LocationCatalogId.HasValue &&
                allowedLocations.Contains(employee.LocationCatalogId.Value))) &&
            (!assignment.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId &&
                asset.Id == assignment.AssetId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                allowedLocations.Contains(asset.LocationCatalogId.Value))));

        return contracts.Where(contract =>
            (db.AssetContractAssets.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id) ||
             db.AssetContractLicenses.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id)) &&
            !db.AssetContractAssets.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id &&
                !db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == link.AssetId &&
                    asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                    allowedLocations.Contains(asset.LocationCatalogId.Value))) &&
            !db.AssetContractLicenses.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id &&
                !visibleAssignments.Any(assignment => assignment.LicenseId == link.LicenseId)));
    }

    public static IQueryable<AssetContract> InEmployeeScope(this IQueryable<AssetContract> contracts,
        AppDbContext db, Guid companyId, EmployeeAccessScope employeeScope)
    {
        if (!employeeScope.IsRestricted) return contracts;

        var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId));
        var visibleAssignments = db.SoftwareLicenseAssignments.AsNoTracking().Where(assignment =>
            assignment.CompanyId == companyId && (assignment.EmployeeId.HasValue || assignment.AssetId.HasValue) &&
            (!assignment.EmployeeId.HasValue || visibleEmployees.Any(employee => employee.Id == assignment.EmployeeId)) &&
            (!assignment.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId &&
                asset.Id == assignment.AssetId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                employeeScope.AllowedLocations != null && employeeScope.AllowedLocations.Contains(asset.LocationCatalogId.Value))));

        return contracts.Where(contract =>
            (db.AssetContractAssets.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id) ||
             db.AssetContractLicenses.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id)) &&
            !db.AssetContractAssets.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id &&
                !db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == link.AssetId &&
                    asset.DeletedAt == null && asset.LocationCatalogId.HasValue && employeeScope.AllowedLocations != null &&
                    employeeScope.AllowedLocations.Contains(asset.LocationCatalogId.Value))) &&
            !db.AssetContractLicenses.Any(link => link.CompanyId == companyId && link.ContractId == contract.Id &&
                !visibleAssignments.Any(assignment => assignment.LicenseId == link.LicenseId)));
    }

    public static IQueryable<Asset> AssetsInLocationScope(this IQueryable<Asset> assets, Guid companyId,
        HashSet<Guid>? allowedLocations) => allowedLocations is null ? assets : assets.Where(asset =>
        asset.CompanyId == companyId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
        allowedLocations.Contains(asset.LocationCatalogId.Value));

    public static IQueryable<SoftwareLicense> LicensesInLocationScope(this IQueryable<SoftwareLicense> licenses,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return licenses;
        return licenses.Where(license => db.SoftwareLicenseAssignments.Any(assignment =>
            assignment.CompanyId == companyId && assignment.LicenseId == license.Id &&
            (assignment.EmployeeId.HasValue || assignment.AssetId.HasValue) &&
            (!assignment.EmployeeId.HasValue || db.Employees.Any(employee => employee.CompanyId == companyId &&
                employee.Id == assignment.EmployeeId && employee.LocationCatalogId.HasValue &&
                allowedLocations.Contains(employee.LocationCatalogId.Value))) &&
            (!assignment.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId &&
                asset.Id == assignment.AssetId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                allowedLocations.Contains(asset.LocationCatalogId.Value)))));
    }

    public static IQueryable<SoftwareLicense> LicensesInEmployeeScope(this IQueryable<SoftwareLicense> licenses,
        AppDbContext db, Guid companyId, EmployeeAccessScope employeeScope)
    {
        if (!employeeScope.IsRestricted) return licenses;
        var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId));
        return licenses.Where(license => db.SoftwareLicenseAssignments.Any(assignment =>
            assignment.CompanyId == companyId && assignment.LicenseId == license.Id &&
            (assignment.EmployeeId.HasValue || assignment.AssetId.HasValue) &&
            (!assignment.EmployeeId.HasValue || visibleEmployees.Any(employee => employee.Id == assignment.EmployeeId)) &&
            (!assignment.AssetId.HasValue || db.Assets.Any(asset => asset.CompanyId == companyId &&
                asset.Id == assignment.AssetId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
                employeeScope.AllowedLocations != null && employeeScope.AllowedLocations.Contains(asset.LocationCatalogId.Value)))));
    }

    public static Task<bool> CanAccessAsync(AppDbContext db, Guid companyId, Guid contractId,
        EmployeeAccessScope employeeScope, CancellationToken ct) =>
        db.AssetContracts.AsNoTracking().Where(contract => contract.CompanyId == companyId && contract.Id == contractId)
            .InEmployeeScope(db, companyId, employeeScope).AnyAsync(ct);
}
