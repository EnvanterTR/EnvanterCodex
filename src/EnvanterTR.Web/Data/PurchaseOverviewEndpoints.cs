using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class PurchaseOverviewEndpoints
{
    public static RouteGroupBuilder MapPurchaseOverviewEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/purchasing/orders", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] string? type, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            [FromQuery] int? page, [FromQuery] int? pageSize, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            if (from.HasValue && to.HasValue && from > to) return Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (type is not (null or "" or "asset" or "accessory" or "stock" or "license")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["type"] = ["Geçerli tür: asset, accessory, stock, license."] });
            if (status is not (null or "" or "Ordered" or "PartiallyReceived" or "Received" or "Cancelled" or "Recorded")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Sipariş durumu geçersiz."] });

            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var queries = BuildQuery(db, companyId, allowedLocations, q?.Trim(), status, type, from, to);
            var assetCount = await queries.Assets.CountAsync(ct);
            var accessoryCount = await queries.Accessories.CountAsync(ct);
            var stockCount = await queries.Stock.CountAsync(ct);
            var licenseCount = await queries.Licenses.CountAsync(ct);
            var total = assetCount + accessoryCount + stockCount + licenseCount;
            var requestedPage = page.GetValueOrDefault(1);
            var size = pageSize.GetValueOrDefault(25);
            if (requestedPage < 1 || requestedPage > 1000 || size is not (25 or 50 or 100))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["page"] = ["Sayfa 1–1000, sayfa boyutu 25, 50 veya 100 olmalıdır."] });
            var skip = checked((requestedPage - 1) * size);
            var pageItems = await OrderedOrders(queries).Skip(skip).Take(size).ToArrayAsync(ct);
            var assetIds = pageItems.Where(x => x.Type == "asset").Select(x => x.Id).ToArray();
            var accessoryIds = pageItems.Where(x => x.Type == "accessory").Select(x => x.Id).ToArray();
            var stockIds = pageItems.Where(x => x.Type == "stock").Select(x => x.Id).ToArray();
            var licenseIds = pageItems.Where(x => x.Type == "license").Select(x => x.Id).ToArray();
            var assetTotals = await db.AssetPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && assetIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var accessoryTotals = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var stockTotals = await db.StockPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && stockIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var licenseTotals = await db.SoftwareLicensePurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && licenseIds.Contains(x.Id))
                .Select(x => new PurchaseOrderLineAggregate(x.Id, 1, x.SeatsPurchased, 0, x.UnitCost.HasValue ? x.UnitCost.Value * x.SeatsPurchased : 0m, x.UnitCost.HasValue, false, false)).ToDictionaryAsync(x => x.Id, ct);
            var items = pageItems.Select(x => WithTotals(x, x.Type switch
            {
                "asset" => assetTotals.GetValueOrDefault(x.Id),
                "accessory" => accessoryTotals.GetValueOrDefault(x.Id),
                "stock" => stockTotals.GetValueOrDefault(x.Id),
                _ => licenseTotals.GetValueOrDefault(x.Id)
            })).ToArray();
            return Results.Ok(new { items, page = requestedPage, pageSize = size, total, totalPages = (int)Math.Ceiling(total / (double)size) });
        }).RequireAuthorization("asset.read");

        api.MapGet("/purchasing/orders/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] string? type, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            if (from.HasValue && to.HasValue && from > to) return Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (type is not (null or "" or "asset" or "accessory" or "stock" or "license") ||
                status is not (null or "" or "Ordered" or "PartiallyReceived" or "Received" or "Cancelled" or "Recorded"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["filter"] = ["Sipariş türü veya durumu geçersiz."] });
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var queries = BuildQuery(db, companyId, allowedLocations, q?.Trim(), status, type, from, to);
            var assetCount = await queries.Assets.CountAsync(ct);
            var accessoryCount = await queries.Accessories.CountAsync(ct);
            var stockCount = await queries.Stock.CountAsync(ct);
            var licenseCount = await queries.Licenses.CountAsync(ct);
            var total = assetCount + accessoryCount + stockCount + licenseCount;
            if (total > 10_000) return Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Dışa aktarım sınırı aşıldı", detail: "Filtre en fazla 10.000 sipariş döndürebilir. Tarih, tür veya durum filtresini daraltıp yeniden deneyin.");
            var rows = await OrderedOrders(queries).ToListAsync(ct);
            var assetIds = rows.Where(x => x.Type == "asset").Select(x => x.Id).ToArray();
            var accessoryIds = rows.Where(x => x.Type == "accessory").Select(x => x.Id).ToArray();
            var stockIds = rows.Where(x => x.Type == "stock").Select(x => x.Id).ToArray();
            var licenseIds = rows.Where(x => x.Type == "license").Select(x => x.Id).ToArray();
            var assetTotals = await db.AssetPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && assetIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var accessoryTotals = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var stockTotals = await db.StockPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && stockIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId).Select(g => new PurchaseOrderLineAggregate(g.Key, g.Count(), g.Sum(x => x.Quantity), g.Sum(x => x.QuantityReceived), g.Sum(x => x.UnitCost * x.Quantity))).ToDictionaryAsync(x => x.Id, ct);
            var licenseTotals = await db.SoftwareLicensePurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && licenseIds.Contains(x.Id))
                .Select(x => new PurchaseOrderLineAggregate(x.Id, 1, x.SeatsPurchased, 0, x.UnitCost.HasValue ? x.UnitCost.Value * x.SeatsPurchased : 0m, x.UnitCost.HasValue, false, false)).ToDictionaryAsync(x => x.Id, ct);
            var output = new StringBuilder("Tür;Sipariş no;Tedarikçi;Durum;Sipariş tarihi;Beklenen teslim;Para birimi;Kalem;Sipariş miktarı;Teslim alınan;Kalan;Ara toplam;İskonto;Vergi;Nakliye;Genel toplam\r\n");
            foreach (var raw in rows)
            {
                var x = WithTotals(raw, raw.Type switch
                {
                    "asset" => assetTotals.GetValueOrDefault(raw.Id),
                    "accessory" => accessoryTotals.GetValueOrDefault(raw.Id),
                    "stock" => stockTotals.GetValueOrDefault(raw.Id),
                    _ => licenseTotals.GetValueOrDefault(raw.Id)
                });
                output.AppendJoin(';', new[] { Csv(x.TypeLabel), Csv(x.OrderNumber), Csv(x.Supplier), Csv(x.StatusLabel), Csv(x.OrderDate.ToString("yyyy-MM-dd")), Csv(x.ExpectedOn?.ToString("yyyy-MM-dd") ?? ""), Csv(x.Currency), Csv(x.ItemCount.ToString()), Csv(x.Quantity.ToString()), Csv(x.QuantityReceived?.ToString() ?? ""), Csv(x.RemainingQuantity?.ToString() ?? ""), Csv(x.Subtotal?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? ""), Csv(x.DiscountAmount?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? ""), Csv(x.TaxAmount?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? ""), Csv(x.FreightAmount?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? ""), Csv(x.Total?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? "") });
                output.Append("\r\n");
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(output.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", "satinalma-siparisleri.csv");
        }).RequireAuthorization("asset.read");

        return api;
    }

    private static PurchaseOrderQueries BuildQuery(AppDbContext db, Guid companyId, HashSet<Guid>? allowedLocations, string? term,
        string? status, string? type, DateOnly? from, DateOnly? to)
    {
        IQueryable<AssetPurchaseOrder> assets = db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
        IQueryable<AccessoryPurchaseOrder> accessories = db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
        IQueryable<StockPurchaseOrder> stock = db.StockPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
        var visibleLicenseIds = db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId)
            .LicensesInLocationScope(db, companyId, allowedLocations).Select(x => x.Id);
        IQueryable<SoftwareLicensePurchaseOrder> licenses = db.SoftwareLicensePurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && !x.IsArchived && visibleLicenseIds.Contains(x.LicenseId));
        assets = assets.InLocationScope(db, companyId, allowedLocations);
        accessories = accessories.InLocationScope(db, companyId, allowedLocations);
        stock = stock.InLocationScope(db, companyId, allowedLocations);
        if (type is "asset") { accessories = accessories.Where(_ => false); stock = stock.Where(_ => false); licenses = licenses.Where(_ => false); }
        else if (type is "accessory") { assets = assets.Where(_ => false); stock = stock.Where(_ => false); licenses = licenses.Where(_ => false); }
        else if (type is "stock") { assets = assets.Where(_ => false); accessories = accessories.Where(_ => false); licenses = licenses.Where(_ => false); }
        else if (type is "license") { assets = assets.Where(_ => false); accessories = accessories.Where(_ => false); stock = stock.Where(_ => false); }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var selected = status.Trim();
            if (selected == "Recorded") { assets = assets.Where(_ => false); accessories = accessories.Where(_ => false); stock = stock.Where(_ => false); }
            else licenses = licenses.Where(_ => false);
            if (selected != "Recorded") { assets = assets.Where(x => x.Status == selected); accessories = accessories.Where(x => x.Status == selected); stock = stock.Where(x => x.Status == selected); }
        }
        if (from.HasValue) { var selected = from.Value; assets = assets.Where(x => x.OrderDate >= selected); accessories = accessories.Where(x => x.OrderDate >= selected); stock = stock.Where(x => x.OrderDate >= selected); licenses = licenses.Where(x => (x.OrderDate ?? DateOnly.FromDateTime(x.CreatedAt.UtcDateTime)) >= selected); }
        if (to.HasValue) { var selected = to.Value; assets = assets.Where(x => x.OrderDate <= selected); accessories = accessories.Where(x => x.OrderDate <= selected); stock = stock.Where(x => x.OrderDate <= selected); licenses = licenses.Where(x => (x.OrderDate ?? DateOnly.FromDateTime(x.CreatedAt.UtcDateTime)) <= selected); }
        if (!string.IsNullOrWhiteSpace(term))
        {
            var selected = term.Trim();
            assets = assets.Where(x => x.OrderNumber.Contains(selected) || x.Supplier.Contains(selected) || (x.Notes != null && x.Notes.Contains(selected)) || db.AssetPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id && l.Name.Contains(selected)));
            accessories = accessories.Where(x => x.OrderNumber.Contains(selected) || x.Supplier.Contains(selected) || (x.Notes != null && x.Notes.Contains(selected)) || db.AccessoryPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id && db.Accessories.Any(a => a.CompanyId == companyId && a.Id == l.AccessoryId && a.Name.Contains(selected))));
            stock = stock.Where(x => x.OrderNumber.Contains(selected) || x.Supplier.Contains(selected) || (x.Notes != null && x.Notes.Contains(selected)) || db.StockPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id && ((l.ConsumableId.HasValue && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == l.ConsumableId && c.Name.Contains(selected))) || (l.ComponentId.HasValue && db.InventoryComponents.Any(c => c.CompanyId == companyId && c.Id == l.ComponentId && c.Name.Contains(selected))))));
            licenses = licenses.Where(x => x.OrderNumber.Contains(selected) || (x.Notes != null && x.Notes.Contains(selected)) ||
                db.SupplierCatalogEntries.Any(s => s.CompanyId == companyId && s.Id == x.SupplierId && s.Name.Contains(selected)) ||
                db.SoftwareLicenses.Any(l => l.CompanyId == companyId && l.Id == x.LicenseId && l.Name.Contains(selected)));
        }
        var assetRows = assets.Select(x => new PurchaseOrderProjection { Id = x.Id, Type = "asset", OrderNumber = x.OrderNumber,
            Supplier = x.Supplier, Status = x.Status, OrderDate = x.OrderDate, ExpectedOn = x.ExpectedOn, Currency = x.Currency,
            RelatedId = null, DiscountAmount = x.DiscountAmount, TaxRatePercent = x.TaxRatePercent, FreightAmount = x.FreightAmount });
        var accessoryRows = accessories.Select(x => new PurchaseOrderProjection { Id = x.Id, Type = "accessory", OrderNumber = x.OrderNumber,
            Supplier = x.Supplier, Status = x.Status, OrderDate = x.OrderDate, ExpectedOn = x.ExpectedOn, Currency = x.Currency,
            RelatedId = null, DiscountAmount = x.DiscountAmount, TaxRatePercent = x.TaxRatePercent, FreightAmount = x.FreightAmount });
        var stockRows = stock.Select(x => new PurchaseOrderProjection { Id = x.Id, Type = "stock", OrderNumber = x.OrderNumber,
            Supplier = x.Supplier, Status = x.Status, OrderDate = x.OrderDate, ExpectedOn = x.ExpectedOn, Currency = x.Currency,
            RelatedId = null, DiscountAmount = x.DiscountAmount, TaxRatePercent = x.TaxRatePercent, FreightAmount = x.FreightAmount });
        var licenseRows = licenses.Select(x => new PurchaseOrderProjection { Id = x.Id, RelatedId = x.LicenseId, Type = "license", OrderNumber = x.OrderNumber,
            Supplier = db.SupplierCatalogEntries.Where(s => s.CompanyId == companyId && s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? "Tedarikçi kaydı yok",
            Status = "Recorded", OrderDate = x.OrderDate ?? DateOnly.FromDateTime(x.CreatedAt.UtcDateTime), ExpectedOn = null, Currency = x.Currency,
            DiscountAmount = 0m, TaxRatePercent = 0m, FreightAmount = 0m });
        return new PurchaseOrderQueries(assetRows, accessoryRows, stockRows, licenseRows);
    }

    private static PurchaseOrderOverviewItem WithTotals(PurchaseOrderProjection x, PurchaseOrderLineAggregate? lines)
    {
        var aggregate = lines ?? new PurchaseOrderLineAggregate(x.Id, 0, 0, 0, 0m, true);
        var detailedCostsTracked = aggregate.DetailedCostsTracked;
        var taxable = Math.Max(0m, aggregate.Subtotal - x.DiscountAmount);
        var tax = Math.Round(taxable * x.TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        return new PurchaseOrderOverviewItem(x.Id, x.Type, TypeLabel(x.Type), x.OrderNumber, x.Supplier, x.Status, StatusLabel(x.Status), x.OrderDate, x.ExpectedOn,
            x.Currency, aggregate.ItemCount, aggregate.Quantity, aggregate.ReceiptTracked ? aggregate.QuantityReceived : null,
            aggregate.ReceiptTracked ? Math.Max(0, aggregate.Quantity - aggregate.QuantityReceived) : null,
            aggregate.IsPriced ? aggregate.Subtotal : null, detailedCostsTracked ? x.DiscountAmount : null, detailedCostsTracked ? tax : null,
            detailedCostsTracked ? x.FreightAmount : null,
            aggregate.IsPriced ? detailedCostsTracked ? taxable + tax + x.FreightAmount : aggregate.Subtotal : null, x.RelatedId);
    }

    private static string TypeLabel(string type) => type switch { "asset" => "Varlık", "accessory" => "Aksesuar", "license" => "Yazılım lisansı", _ => "Sarf / bileşen" };
    private static string StatusLabel(string status) => status switch { "Ordered" => "Sipariş edildi", "PartiallyReceived" => "Kısmi teslim", "Received" => "Tamamlandı", "Cancelled" => "İptal edildi", "Recorded" => "Kaydedildi", _ => status };
    private static string Csv(string value) => "\"" + Sanitize(value).Replace("\"", "\"\"") + "\"";
    private static string Sanitize(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@') ||
            value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n') ? "'" + value : value;
    }
    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static IOrderedQueryable<PurchaseOrderProjection> OrderedOrders(PurchaseOrderQueries queries) =>
        queries.Assets.Concat(queries.Accessories).Concat(queries.Stock).Concat(queries.Licenses)
            .OrderByDescending(x => x.OrderDate).ThenBy(x => x.Type).ThenBy(x => x.OrderNumber).ThenBy(x => x.Id);

    private sealed class PurchaseOrderProjection
    {
        public Guid Id { get; init; }
        public Guid? RelatedId { get; init; }
        public required string Type { get; init; }
        public required string OrderNumber { get; init; }
        public required string Supplier { get; init; }
        public required string Status { get; init; }
        public DateOnly OrderDate { get; init; }
        public DateOnly? ExpectedOn { get; init; }
        public required string Currency { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal TaxRatePercent { get; init; }
        public decimal FreightAmount { get; init; }
    }
    private sealed record PurchaseOrderQueries(IQueryable<PurchaseOrderProjection> Assets, IQueryable<PurchaseOrderProjection> Accessories,
        IQueryable<PurchaseOrderProjection> Stock, IQueryable<PurchaseOrderProjection> Licenses);
    private sealed record PurchaseOrderLineAggregate(Guid Id, int ItemCount, int Quantity, int QuantityReceived, decimal Subtotal,
        bool IsPriced = true, bool ReceiptTracked = true, bool DetailedCostsTracked = true);
    private sealed record PurchaseOrderOverviewItem(Guid Id, string Type, string TypeLabel, string OrderNumber, string Supplier, string Status,
        string StatusLabel, DateOnly OrderDate, DateOnly? ExpectedOn, string Currency, int ItemCount, int Quantity, int? QuantityReceived,
        int? RemainingQuantity, decimal? Subtotal, decimal? DiscountAmount, decimal? TaxAmount, decimal? FreightAmount, decimal? Total, Guid? RelatedId);
}
