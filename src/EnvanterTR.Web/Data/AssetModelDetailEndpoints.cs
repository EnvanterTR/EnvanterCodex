using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetModelDetailEndpoints
{
    public static RouteGroupBuilder MapAssetModelDetailEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/models/{id:guid}/details", async (Guid id, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId))
                return Results.Forbid();

            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Forbid();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, userId, ct);
            var allowedLocations = employeeScope.AllowedLocations;
            var visibleEmployees = employeeScope.Apply(db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId));

            var assetQuery = db.Assets.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.ModelId == id && x.DeletedAt == null);
            if (allowedLocations is not null)
            {
                assetQuery = assetQuery.Where(x => x.LocationCatalogId.HasValue && allowedLocations.Contains(x.LocationCatalogId.Value));
                if (!await assetQuery.AnyAsync(ct)) return Results.NotFound();
            }

            var model = await db.AssetModels.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ModelNumber,
                    x.BrandId,
                    BrandName = db.AssetBrands.Where(b => b.CompanyId == companyId && b.Id == x.BrandId)
                        .Select(b => b.Name).FirstOrDefault(),
                    x.CategoryId,
                    CategoryName = db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId)
                        .Select(c => c.Name).FirstOrDefault(),
                    HasPhoto = x.PhotoFileName != null,
                    x.DepreciationMonths,
                    x.DepreciationMinimum,
                    x.DepreciationMinimumType,
                    x.DepreciationMethod
                })
                .SingleOrDefaultAsync(ct);

            if (model is null) return Results.NotFound();

            var statusCounts = await assetQuery.GroupBy(x => x.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(ct);
            var assetTotal = await assetQuery.CountAsync(ct);
            var assets = await assetQuery
                .OrderBy(x => x.AssetTag)
                .Take(100)
                .Select(x => new
                {
                    x.Id,
                    x.AssetTag,
                    x.Name,
                    x.Status,
                    x.SerialNumber,
                    Location = allowedLocations == null
                        ? x.Location
                        : db.CompanyLocations.Where(location => location.CompanyId == companyId &&
                            location.Id == x.LocationCatalogId).Select(location => location.Name).FirstOrDefault(),
                    EmployeeName = visibleEmployees.Where(e => e.Id == x.AssignedEmployeeId)
                        .Select(e => e.DisplayName).FirstOrDefault(),
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var orderQuery = db.AssetPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId &&
                db.AssetPurchaseOrderLines.Any(line => line.CompanyId == companyId &&
                    line.PurchaseOrderId == order.Id && line.ModelId == id));
            orderQuery = orderQuery.InLocationScope(db, companyId, allowedLocations);
            var orderTotal = await orderQuery.CountAsync(ct);
            var orders = await orderQuery.OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.CreatedAt).Take(25)
                .Select(order => new
                {
                    order.Id,
                    order.OrderNumber,
                    order.Supplier,
                    order.Status,
                    order.OrderDate,
                    order.ExpectedOn,
                    order.Currency,
                    Quantity = db.AssetPurchaseOrderLines.Where(line => line.CompanyId == companyId &&
                        line.PurchaseOrderId == order.Id && line.ModelId == id).Sum(line => (int?)line.Quantity) ?? 0,
                    QuantityReceived = db.AssetPurchaseOrderLines.Where(line => line.CompanyId == companyId &&
                        line.PurchaseOrderId == order.Id && line.ModelId == id).Sum(line => (int?)line.QuantityReceived) ?? 0
                })
                .ToListAsync(ct);

            var history = allowedLocations is null
                ? await db.AuditEntries.AsNoTracking()
                    .Where(entry => entry.CompanyId == companyId && entry.TargetType == "AssetModel" && entry.TargetId == id)
                    .OrderByDescending(entry => entry.OccurredAt).Take(50)
                    .Select(entry => new { entry.Action, entry.Details, entry.OccurredAt })
                    .ToListAsync(ct)
                : [];

            return Results.Ok(new
            {
                model,
                summary = new { assetTotal, statuses = statusCounts },
                assets,
                assetTotal,
                assetListLimit = 100,
                orders,
                orderTotal,
                orderListLimit = 25,
                history
            });
        }).RequireAuthorization("asset.read");

        return api;
    }
}
