using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal static class PurchaseOrderLocationAccess
{
    public static IQueryable<StockPurchaseOrder> InLocationScope(this IQueryable<StockPurchaseOrder> orders,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return orders;
        return orders.Where(order =>
            db.StockPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id) &&
            !db.StockPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id &&
                ((line.ConsumableId.HasValue && !db.Consumables.Any(item => item.CompanyId == companyId && item.Id == line.ConsumableId &&
                    item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value))) ||
                 (line.ComponentId.HasValue && !db.InventoryComponents.Any(item => item.CompanyId == companyId && item.Id == line.ComponentId &&
                    item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value))) ||
                 (!line.ConsumableId.HasValue && !line.ComponentId.HasValue))));
    }

    public static IQueryable<AccessoryPurchaseOrder> InLocationScope(this IQueryable<AccessoryPurchaseOrder> orders,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return orders;
        return orders.Where(order =>
            db.AccessoryPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id) &&
            !db.AccessoryPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id &&
                !db.Accessories.Any(item => item.CompanyId == companyId && item.Id == line.AccessoryId &&
                    item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value))));
    }

    public static IQueryable<AssetPurchaseOrder> InLocationScope(this IQueryable<AssetPurchaseOrder> orders,
        AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations)
    {
        if (allowedLocations is null) return orders;
        return orders.Where(order =>
            db.AssetPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id) &&
            !db.AssetPurchaseOrderLines.Any(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id &&
                (!line.LocationCatalogId.HasValue || !allowedLocations.Contains(line.LocationCatalogId.Value))) &&
            !db.Assets.Any(asset => asset.CompanyId == companyId && asset.PurchaseOrderId == order.Id &&
                (!asset.LocationCatalogId.HasValue || !allowedLocations.Contains(asset.LocationCatalogId.Value))));
    }

    public static async Task<bool> CanAccessStockOrderAsync(AppDbContext db, Guid companyId, Guid orderId,
        HashSet<Guid>? allowedLocations, CancellationToken cancellationToken) =>
        await db.StockPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId && order.Id == orderId)
            .InLocationScope(db, companyId, allowedLocations).AnyAsync(cancellationToken);

    public static async Task<bool> CanAccessAccessoryOrderAsync(AppDbContext db, Guid companyId, Guid orderId,
        HashSet<Guid>? allowedLocations, CancellationToken cancellationToken) =>
        await db.AccessoryPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId && order.Id == orderId)
            .InLocationScope(db, companyId, allowedLocations).AnyAsync(cancellationToken);

    public static async Task<bool> CanAccessAssetOrderAsync(AppDbContext db, Guid companyId, Guid orderId,
        HashSet<Guid>? allowedLocations, CancellationToken cancellationToken) =>
        await db.AssetPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId && order.Id == orderId)
            .InLocationScope(db, companyId, allowedLocations).AnyAsync(cancellationToken);

    public static async Task<bool> CanAccessStockItemsAsync(AppDbContext db, Guid companyId, Guid[] consumableIds,
        Guid[] componentIds, HashSet<Guid>? allowedLocations, CancellationToken cancellationToken)
    {
        if (allowedLocations is null) return true;
        var distinctConsumables = consumableIds.Distinct().ToArray();
        var distinctComponents = componentIds.Distinct().ToArray();
        return (distinctConsumables.Length == 0 || await db.Consumables.CountAsync(item => item.CompanyId == companyId &&
                   distinctConsumables.Contains(item.Id) && item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value), cancellationToken) == distinctConsumables.Length) &&
               (distinctComponents.Length == 0 || await db.InventoryComponents.CountAsync(item => item.CompanyId == companyId &&
                   distinctComponents.Contains(item.Id) && item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value), cancellationToken) == distinctComponents.Length);
    }

    public static async Task<bool> CanAccessAccessoriesAsync(AppDbContext db, Guid companyId, Guid[] accessoryIds,
        HashSet<Guid>? allowedLocations, CancellationToken cancellationToken)
    {
        if (allowedLocations is null) return true;
        var ids = accessoryIds.Distinct().ToArray();
        return ids.Length > 0 && await db.Accessories.CountAsync(item => item.CompanyId == companyId && ids.Contains(item.Id) &&
            item.LocationCatalogId.HasValue && allowedLocations.Contains(item.LocationCatalogId.Value), cancellationToken) == ids.Length;
    }
}
