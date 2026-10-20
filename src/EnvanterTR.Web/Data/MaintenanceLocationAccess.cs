using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal static class MaintenanceLocationAccess
{
    // A maintenance record belongs to an asset. Readers who can see that asset may
    // read its maintenance history; employee identity is redacted by each projection
    // when the responsible employee is outside the reader's employee scope.
    public static IQueryable<AssetMaintenance> InAssetReadScope(this IQueryable<AssetMaintenance> maintenances,
        AppDbContext db, Guid companyId, EmployeeAccessScope employeeScope)
    {
        if (!employeeScope.IsRestricted) return maintenances;

        var scoped = maintenances.Where(maintenance => db.Assets.Any(asset => asset.CompanyId == companyId &&
            asset.Id == maintenance.AssetId && asset.DeletedAt == null &&
            (asset.LocationCatalogId.HasValue
                ? db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == asset.LocationCatalogId.Value &&
                    (employeeScope.AllowedLocations == null || employeeScope.AllowedLocations.Contains(location.Id)) &&
                    (employeeScope.AllowedCompanyUnits == null || !location.CompanyUnitId.HasValue || employeeScope.AllowedCompanyUnits.Contains(location.CompanyUnitId.Value)))
                : !employeeScope.RestrictLocations && employeeScope.AllowedCompanyUnits == null)));

        // Unit-restricted readers may only see maintenance assigned to employees in
        // their unit scope. Location-only readers can inspect the visible asset's
        // history, with out-of-scope employee identity redacted by the projection.
        if (!employeeScope.RestrictCompanyUnits) return scoped;
        var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId));
        return scoped.Where(maintenance => !maintenance.ResponsibleEmployeeId.HasValue ||
            visibleEmployees.Any(employee => employee.Id == maintenance.ResponsibleEmployeeId.Value));
    }

    public static IQueryable<AssetMaintenance> InEmployeeScope(this IQueryable<AssetMaintenance> maintenances,
        AppDbContext db, Guid companyId, EmployeeAccessScope employeeScope)
    {
        if (!employeeScope.IsRestricted) return maintenances;

        var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId));
        return maintenances.Where(maintenance =>
            db.Assets.Any(asset => asset.CompanyId == companyId && asset.Id == maintenance.AssetId && asset.DeletedAt == null &&
                (asset.LocationCatalogId.HasValue
                    ? db.CompanyLocations.Any(location => location.CompanyId == companyId && location.Id == asset.LocationCatalogId.Value &&
                        (employeeScope.AllowedLocations == null || employeeScope.AllowedLocations.Contains(location.Id)) &&
                        (employeeScope.AllowedCompanyUnits == null || !location.CompanyUnitId.HasValue || employeeScope.AllowedCompanyUnits.Contains(location.CompanyUnitId.Value)))
                    : !employeeScope.RestrictLocations && employeeScope.AllowedCompanyUnits == null)) &&
            (!maintenance.ResponsibleEmployeeId.HasValue || visibleEmployees.Any(employee =>
                employee.Id == maintenance.ResponsibleEmployeeId.Value)));
    }

    public static IQueryable<AssetMaintenance> InLocationScope(this IQueryable<AssetMaintenance> maintenances,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return maintenances;
        return maintenances.Where(maintenance => db.Assets.Any(asset => asset.CompanyId == companyId &&
            asset.Id == maintenance.AssetId && asset.DeletedAt == null && asset.LocationCatalogId.HasValue &&
            allowedLocations.Contains(asset.LocationCatalogId.Value)));
    }

    public static Task<bool> CanAccessAsync(AppDbContext db, Guid companyId, Guid maintenanceId,
        EmployeeAccessScope employeeScope, CancellationToken ct) =>
        db.AssetMaintenances.AsNoTracking().Where(maintenance => maintenance.CompanyId == companyId && maintenance.Id == maintenanceId)
            .InEmployeeScope(db, companyId, employeeScope).AnyAsync(ct);

    public static Task<bool> CanReadAsync(AppDbContext db, Guid companyId, Guid maintenanceId,
        EmployeeAccessScope employeeScope, CancellationToken ct) =>
        db.AssetMaintenances.AsNoTracking().Where(maintenance => maintenance.CompanyId == companyId && maintenance.Id == maintenanceId)
            .InAssetReadScope(db, companyId, employeeScope).AnyAsync(ct);
}
