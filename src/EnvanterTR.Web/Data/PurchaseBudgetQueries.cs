using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

internal static class PurchaseBudgetQueries
{
    public static async Task<decimal> CommittedPurchaseAmountAsync(AppDbContext db, Guid companyId, int year, string currency,
        CancellationToken ct, Guid? exceptAssetOrderId = null, Guid? exceptAccessoryOrderId = null, Guid? exceptStockOrderId = null)
    {
        var assetOrders = await db.AssetPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId &&
                order.OrderDate.Year == year && order.Currency == currency && order.Status != "Cancelled" && order.ApprovalStatus != "Rejected" &&
                (exceptAssetOrderId == null || order.Id != exceptAssetOrderId))
            .Select(order => new { order.DiscountAmount, order.TaxRatePercent, order.FreightAmount,
                Subtotal = db.AssetPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id)
                    .Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m })
            .ToListAsync(ct);
        var accessoryOrders = await db.AccessoryPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId &&
                order.OrderDate.Year == year && order.Currency == currency && order.Status != "Cancelled" && order.ApprovalStatus != "Rejected" &&
                (exceptAccessoryOrderId == null || order.Id != exceptAccessoryOrderId))
            .Select(order => new { order.DiscountAmount, order.TaxRatePercent, order.FreightAmount,
                Subtotal = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id)
                    .Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m })
            .ToListAsync(ct);
        var stockOrders = await db.StockPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId &&
                order.OrderDate.Year == year && order.Currency == currency && order.Status != "Cancelled" && order.ApprovalStatus != "Rejected" &&
                (exceptStockOrderId == null || order.Id != exceptStockOrderId))
            .Select(order => new { order.DiscountAmount, order.TaxRatePercent, order.FreightAmount,
                Subtotal = db.StockPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == order.Id)
                    .Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m })
            .ToListAsync(ct);

        return assetOrders.Sum(order => CalculateTotal(order.Subtotal, order.DiscountAmount,
                   CalculateTax(order.Subtotal, order.DiscountAmount, order.TaxRatePercent), order.FreightAmount))
            + accessoryOrders.Sum(order => CalculateTotal(order.Subtotal, order.DiscountAmount,
                CalculateTax(order.Subtotal, order.DiscountAmount, order.TaxRatePercent), order.FreightAmount))
            + stockOrders.Sum(order => CalculateTotal(order.Subtotal, order.DiscountAmount,
                CalculateTax(order.Subtotal, order.DiscountAmount, order.TaxRatePercent), order.FreightAmount));
    }

    public static decimal CalculateTax(decimal subtotal, decimal discount, decimal taxRatePercent) =>
        Math.Round((subtotal - discount) * taxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateTotal(decimal subtotal, decimal discount, decimal tax, decimal freight) => subtotal - discount + tax + freight;

}
