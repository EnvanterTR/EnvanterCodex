using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class PendingDeliveryReportEndpoints
{
    private const int MaximumExportRows = 50_000;
    private sealed class PendingDeliveryRow
    {
        public string OrderType { get; init; } = string.Empty;
        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string Supplier { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateOnly OrderDate { get; init; }
        public DateOnly? ExpectedOn { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public int QuantityReceived { get; init; }
        public int Remaining { get; init; }
        public decimal UnitCost { get; init; }
    }

    public static void MapPendingDeliveryReportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/reports/pending-deliveries", async ([FromQuery] string? q, [FromQuery] string? state,
            [FromQuery] DateOnly? asOf, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Validate(q, state, out var errors)) return Results.ValidationProblem(errors);
            var effectiveDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (effectiveDate < new DateOnly(2000, 1, 1) || effectiveDate > new DateOnly(2100, 12, 31))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asOf"] = ["Rapor tarihi 2000 ile 2100 arasında olmalıdır."] });

            var all = BuildQuery(db, companyId);
            var summary = new
            {
                overdueOrders = await all.Where(x => x.ExpectedOn.HasValue && x.ExpectedOn.Value < effectiveDate)
                    .Select(x => x.OrderId).Distinct().CountAsync(ct),
                overdueLines = await all.CountAsync(x => x.ExpectedOn.HasValue && x.ExpectedOn.Value < effectiveDate, ct),
                undatedLines = await all.CountAsync(x => !x.ExpectedOn.HasValue, ct),
                remainingUnits = await all.SumAsync(x => (int?)x.Remaining, ct) ?? 0
            };
            var filtered = ApplyFilters(all, q, state, effectiveDate);
            var total = await filtered.CountAsync(ct);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 200);
            var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page == 0 ? 1 : page, 1, pageCount);
            var items = await filtered.OrderBy(x => x.ExpectedOn ?? DateOnly.MaxValue).ThenByDescending(x => x.OrderDate)
                .ThenBy(x => x.OrderNumber).ThenBy(x => x.ItemName)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { asOf = effectiveDate, state = state ?? "all", items, total, page, pageSize, pageCount, summary });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/pending-deliveries/export.csv", async ([FromQuery] string? q, [FromQuery] string? state,
            [FromQuery] DateOnly? asOf, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            if (!Validate(q, state, out var errors)) return Results.ValidationProblem(errors);
            var effectiveDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (effectiveDate < new DateOnly(2000, 1, 1) || effectiveDate > new DateOnly(2100, 12, 31))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["asOf"] = ["Rapor tarihi 2000 ile 2100 arasında olmalıdır."] });
            var query = ApplyFilters(BuildQuery(db, companyId), q, state, effectiveDate);
            if (await query.CountAsync(ct) > MaximumExportRows)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = [$"Dışa aktarma en fazla {MaximumExportRows:N0} sipariş kalemini destekler."] });
            var rows = await query.OrderBy(x => x.ExpectedOn ?? DateOnly.MaxValue).ThenByDescending(x => x.OrderDate)
                .ThenBy(x => x.OrderNumber).ThenBy(x => x.ItemName).ToListAsync(ct);
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                var trimmed = value.TrimStart();
                var formula = trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@';
                return "\"" + (formula ? "'" + value : value).Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("Sipariş türü;Sipariş no;Tedarikçi;Durum;Sipariş tarihi;Beklenen teslim;Para birimi;Kalem;Sipariş adedi;Teslim alınan;Kalan;Birim bedeli\r\n");
            foreach (var row in rows)
            {
                csv.AppendJoin(';', Cell(row.OrderType), Cell(row.OrderNumber), Cell(row.Supplier), Cell(row.Status),
                    Cell(row.OrderDate.ToString("yyyy-MM-dd")), Cell(row.ExpectedOn?.ToString("yyyy-MM-dd")), Cell(row.Currency),
                    Cell(row.ItemName), row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.QuantityReceived.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.UnitCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-bekleyen-teslimatlar-{effectiveDate:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");
    }

    private static IQueryable<PendingDeliveryRow> BuildQuery(AppDbContext db, Guid companyId)
    {
        var assets = from order in db.AssetPurchaseOrders.AsNoTracking()
                     join line in db.AssetPurchaseOrderLines.AsNoTracking()
                         on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                     where order.CompanyId == companyId && order.Status != "Cancelled" && line.Quantity > line.QuantityReceived
                     select new
                     {
                         OrderType = "Varlık", OrderId = order.Id, order.OrderNumber, order.Supplier, order.Status,
                         order.OrderDate, order.ExpectedOn, order.Currency, ItemName = line.Name, line.Quantity,
                         line.QuantityReceived, Remaining = line.Quantity - line.QuantityReceived, line.UnitCost
                     };

        var accessories = from order in db.AccessoryPurchaseOrders.AsNoTracking()
                          join line in db.AccessoryPurchaseOrderLines.AsNoTracking()
                              on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                          join accessory in db.Accessories.AsNoTracking()
                              on new { line.CompanyId, Id = line.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                          where order.CompanyId == companyId && order.Status != "Cancelled" && line.Quantity > line.QuantityReceived
                          select new
                          {
                              OrderType = "Aksesuar", OrderId = order.Id, order.OrderNumber, order.Supplier, order.Status,
                              order.OrderDate, order.ExpectedOn, order.Currency, ItemName = accessory.Name, line.Quantity,
                              line.QuantityReceived, Remaining = line.Quantity - line.QuantityReceived, line.UnitCost
                          };

        var stocks = from order in db.StockPurchaseOrders.AsNoTracking()
                     join line in db.StockPurchaseOrderLines.AsNoTracking()
                         on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                     join consumable in db.Consumables.AsNoTracking()
                         on new { line.CompanyId, Id = line.ConsumableId } equals new { consumable.CompanyId, Id = (Guid?)consumable.Id } into consumables
                     from consumable in consumables.DefaultIfEmpty()
                     join component in db.InventoryComponents.AsNoTracking()
                         on new { line.CompanyId, Id = line.ComponentId } equals new { component.CompanyId, Id = (Guid?)component.Id } into components
                     from component in components.DefaultIfEmpty()
                     where order.CompanyId == companyId && order.Status != "Cancelled" && line.Quantity > line.QuantityReceived
                     select new
                     {
                         OrderType = line.ConsumableId.HasValue ? "Sarf" : "Bileşen",
                         OrderId = order.Id,
                         order.OrderNumber,
                         order.Supplier,
                         order.Status,
                         order.OrderDate,
                         order.ExpectedOn,
                         order.Currency,
                         ItemName = line.ConsumableId.HasValue
                             ? consumable != null ? consumable.Name : "Silinmiş sarf malzemesi"
                             : component != null ? component.Name : "Silinmiş bileşen",
                         line.Quantity,
                         line.QuantityReceived,
                         Remaining = line.Quantity - line.QuantityReceived,
                         line.UnitCost
                     };

        // Keep each UNION input as a server-translatable anonymous projection. EF Core
        // needs the set operation before the presentation DTO member initialization.
        return assets.Concat(accessories).Concat(stocks)
            .Select(x => new PendingDeliveryRow
            {
                OrderType = x.OrderType,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                Supplier = x.Supplier,
                Status = x.Status,
                OrderDate = x.OrderDate,
                ExpectedOn = x.ExpectedOn,
                Currency = x.Currency,
                ItemName = x.ItemName,
                Quantity = x.Quantity,
                QuantityReceived = x.QuantityReceived,
                Remaining = x.Remaining,
                UnitCost = x.UnitCost
            });
    }

    private static IQueryable<PendingDeliveryRow> ApplyFilters(IQueryable<PendingDeliveryRow> query, string? q,
        string? state, DateOnly asOf)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.OrderNumber.Contains(term) || x.Supplier.Contains(term) || x.ItemName.Contains(term));
        }
        return state switch
        {
            "overdue" => query.Where(x => x.ExpectedOn.HasValue && x.ExpectedOn.Value < asOf),
            "due30" => query.Where(x => x.ExpectedOn.HasValue && x.ExpectedOn.Value >= asOf && x.ExpectedOn.Value <= asOf.AddDays(30)),
            "undated" => query.Where(x => !x.ExpectedOn.HasValue),
            _ => query
        };
    }

    private static bool Validate(string? q, string? state, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        if (q?.Trim().Length > 120) errors["q"] = ["Arama 120 karakteri aşamaz."];
        if (!string.IsNullOrWhiteSpace(state) && state is not ("all" or "overdue" or "due30" or "undated"))
            errors["state"] = ["Durum all, overdue, due30 veya undated olmalıdır."];
        return errors.Count == 0;
    }

    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
}
