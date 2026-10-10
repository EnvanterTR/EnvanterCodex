using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class StockPurchaseOrderEndpoints
{
    public static RouteGroupBuilder MapStockPurchaseOrderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/stock-orders/export.csv", async ([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? itemType,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool? overdueOnly, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(itemType) && itemType is not ("Consumable" or "Component"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["itemType"] = ["Kalem türü Sarf veya Bileşen olmalıdır."] });
            var query = from order in db.StockPurchaseOrders.AsNoTracking()
                        join line in db.StockPurchaseOrderLines.AsNoTracking() on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                        where order.CompanyId == companyId
                        select new { order, line };
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.order.Status == status.Trim());
            if (from is not null) query = query.Where(x => x.order.OrderDate >= from);
            if (to is not null) query = query.Where(x => x.order.OrderDate <= to);
            if (itemType == "Consumable") query = query.Where(x => x.line.ConsumableId != null);
            if (itemType == "Component") query = query.Where(x => x.line.ComponentId != null);
            if (overdueOnly == true)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                query = query.Where(x => (x.order.Status == "Ordered" || x.order.Status == "PartiallyReceived") && x.order.ExpectedOn != null && x.order.ExpectedOn < today);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.order.OrderNumber.Contains(term) || x.order.Supplier.Contains(term) ||
                    (x.order.Notes != null && x.order.Notes.Contains(term)) ||
                    (x.line.ConsumableId != null && db.Consumables.Any(i => i.CompanyId == companyId && i.Id == x.line.ConsumableId &&
                        (i.Name.Contains(term) || (i.ItemNumber != null && i.ItemNumber.Contains(term))))) ||
                    (x.line.ComponentId != null && db.InventoryComponents.Any(i => i.CompanyId == companyId && i.Id == x.line.ComponentId &&
                        (i.Name.Contains(term) || (i.ItemNumber != null && i.ItemNumber.Contains(term))))));
            }
            const int exportLimit = 50_000;
            if (await query.CountAsync(ct) > exportLimit)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = [$"Dışa aktarma en fazla {exportLimit:N0} sipariş satırını destekler. Sonuçları arama veya durum filtresiyle daraltın."] });
            var rows = await query.OrderByDescending(x => x.order.OrderDate).ThenBy(x => x.order.OrderNumber)
                .Select(x => new
                {
                    x.order.OrderNumber, x.order.Supplier, x.order.Status, x.order.OrderDate, x.order.ExpectedOn, x.order.Currency,
                    ItemType = x.line.ConsumableId != null ? "Sarf" : "Bileşen",
                    ItemName = x.line.ConsumableId != null ? db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.line.ConsumableId).Select(i => i.Name).FirstOrDefault() :
                        db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.line.ComponentId).Select(i => i.Name).FirstOrDefault(),
                    ItemNumber = x.line.ConsumableId != null ? db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.line.ConsumableId).Select(i => i.ItemNumber).FirstOrDefault() :
                        db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.line.ComponentId).Select(i => i.ItemNumber).FirstOrDefault(),
                    x.line.Quantity, x.line.QuantityReceived, Remaining = x.line.Quantity - x.line.QuantityReceived, x.line.UnitCost,
                    TotalCost = x.line.Quantity * x.line.UnitCost,
                    Subtotal = db.StockPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.order.Id)
                        .Sum(l => (decimal?)(l.Quantity * l.UnitCost)) ?? 0m,
                    x.order.TaxRatePercent, x.order.DiscountAmount, x.order.FreightAmount, x.line.Notes
                }).ToListAsync(ct);
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                var trimmed = value.TrimStart();
                var formula = trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@');
                return "\"" + (formula ? "'" + value : value).Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("Sipariş No;Tedarikçi;Durum;Sipariş Tarihi;Beklenen Tarih;Para Birimi;Kalem Türü;Kalem;Stok Kodu;Sipariş Adedi;Teslim Alınan;Kalan;Birim Bedeli;Satır Toplamı;Ara Toplam;İskonto;Vergi Oranı %;Vergi;Nakliye;Genel Toplam;Satır Notu\r\n");
            foreach (var row in rows)
            {
                var taxAmount = CalculateTax(row.Subtotal, row.DiscountAmount, row.TaxRatePercent);
                var grandTotal = CalculateTotal(row.Subtotal, row.DiscountAmount, taxAmount, row.FreightAmount);
                csv.AppendLine(string.Join(';', new[]
            {
                Cell(row.OrderNumber), Cell(row.Supplier), Cell(row.Status), Cell(row.OrderDate.ToString("yyyy-MM-dd")), Cell(row.ExpectedOn?.ToString("yyyy-MM-dd")), Cell(row.Currency),
                Cell(row.ItemType), Cell(row.ItemName), Cell(row.ItemNumber), row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.QuantityReceived.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.UnitCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), row.TotalCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.Subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), row.DiscountAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.TaxRatePercent.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), taxAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.FreightAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), grandTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(row.Notes)
            }));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-sarf-bilesen-siparisleri-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/stock-orders", async ([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? itemType,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool? overdueOnly,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(itemType) && itemType is not ("Consumable" or "Component"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["itemType"] = ["Kalem türü Sarf veya Bileşen olmalıdır."] });
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.StockPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (from is not null) query = query.Where(x => x.OrderDate >= from);
            if (to is not null) query = query.Where(x => x.OrderDate <= to);
            if (itemType == "Consumable") query = query.Where(x => db.StockPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id && l.ConsumableId != null));
            if (itemType == "Component") query = query.Where(x => db.StockPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id && l.ComponentId != null));
            if (overdueOnly == true)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                query = query.Where(x => (x.Status == "Ordered" || x.Status == "PartiallyReceived") && x.ExpectedOn != null && x.ExpectedOn < today);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.OrderNumber.Contains(term) || x.Supplier.Contains(term) || (x.Notes != null && x.Notes.Contains(term)) ||
                    db.StockPurchaseOrderLines.Any(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id &&
                        ((l.ConsumableId != null && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == l.ConsumableId &&
                            (c.Name.Contains(term) || (c.ItemNumber != null && c.ItemNumber.Contains(term))))) ||
                         (l.ComponentId != null && db.InventoryComponents.Any(c => c.CompanyId == companyId && c.Id == l.ComponentId &&
                            (c.Name.Contains(term) || (c.ItemNumber != null && c.ItemNumber.Contains(term))))))));
            }
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency,
                    lineCount = db.StockPurchaseOrderLines.Count(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id),
                    quantity = db.StockPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.Quantity) ?? 0,
                    quantityReceived = db.StockPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.QuantityReceived) ?? 0,
                    subtotal = db.StockPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (decimal?)(l.Quantity * l.UnitCost)) ?? 0m,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount }).ToListAsync(ct);
            var items = rows.Select(x =>
            {
                var taxAmount = CalculateTax(x.subtotal, x.DiscountAmount, x.TaxRatePercent);
                return new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency, x.lineCount, x.quantity,
                    x.quantityReceived, x.subtotal, x.TaxRatePercent, x.DiscountAmount, x.FreightAmount, taxAmount,
                    totalCost = CalculateTotal(x.subtotal, x.DiscountAmount, taxAmount, x.FreightAmount) };
            }).ToArray();
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization("asset.read");

        api.MapGet("/stock-orders/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var order = await db.StockPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency, x.Notes,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            var lines = await db.StockPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, itemType = x.ConsumableId != null ? "Consumable" : "Component",
                    itemId = x.ConsumableId ?? x.ComponentId!.Value,
                    itemName = x.ConsumableId != null ? db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.ConsumableId).Select(i => i.Name).FirstOrDefault() :
                        db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.ComponentId).Select(i => i.Name).FirstOrDefault(),
                    itemNumber = x.ConsumableId != null ? db.Consumables.Where(i => i.CompanyId == companyId && i.Id == x.ConsumableId).Select(i => i.ItemNumber).FirstOrDefault() :
                        db.InventoryComponents.Where(i => i.CompanyId == companyId && i.Id == x.ComponentId).Select(i => i.ItemNumber).FirstOrDefault(),
                    x.Quantity, x.QuantityReceived, remaining = x.Quantity - x.QuantityReceived, x.UnitCost, totalCost = x.Quantity * x.UnitCost, x.Notes }).ToListAsync(ct);
            var attachments = await db.StockPurchaseOrderAttachments.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            var subtotal = lines.Sum(x => x.totalCost);
            var taxAmount = CalculateTax(subtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(lines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, subtotal);
            var pricedLines = lines.Select(line => new { line.Id, line.itemType, line.itemId, line.itemName, line.itemNumber,
                line.Quantity, line.QuantityReceived, line.remaining, line.UnitCost, line.totalCost, line.Notes, landedUnitCost = landedUnitCosts[line.Id] }).ToArray();
            return Results.Ok(new { order, lines = pricedLines, subtotal, taxableAmount = subtotal - order.DiscountAmount, taxAmount,
                totalCost = CalculateTotal(subtotal, order.DiscountAmount, taxAmount, order.FreightAmount), quantity = lines.Sum(x => x.Quantity),
                quantityReceived = lines.Sum(x => x.QuantityReceived), attachments, maxAttachmentCount = 30, maxAttachmentSizeBytes = 10 * 1024 * 1024 });
        }).RequireAuthorization("asset.read");

        api.MapPost("/stock-orders", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var errors = new Dictionary<string, string[]>();
            var supplier = Clean(request.Supplier); var number = Clean(request.OrderNumber); var currency = request.Currency?.Trim().ToUpperInvariant() ?? "";
            var lines = request.Items ?? [];
            if (supplier is null || supplier.Length > 200) errors["supplier"] = ["Tedarikçi zorunludur ve 200 karakteri aşamaz."];
            if (number?.Length > 80) errors["orderNumber"] = ["Sipariş numarası 80 karakteri aşamaz."];
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) errors["currency"] = ["Üç harfli para birimi girin (TRY, EUR, USD)."];
            var taxRatePercent = request.TaxRatePercent ?? 0m;
            var discountAmount = request.DiscountAmount ?? 0m;
            var freightAmount = request.FreightAmount ?? 0m;
            if (taxRatePercent is < 0 or > 100) errors["taxRatePercent"] = ["Vergi oranı %0 ile %100 arasında olmalıdır."];
            if (discountAmount is < 0 or > 1000000000000000m) errors["discountAmount"] = ["İskonto 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (freightAmount is < 0 or > 1000000000000000m) errors["freightAmount"] = ["Nakliye 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (request.OrderDate == default || request.ExpectedOn is DateOnly expected && expected < request.OrderDate) errors["dates"] = ["Sipariş tarihi zorunludur; beklenen tarih daha önce olamaz."];
            if (request.Notes?.Length > 2000) errors["notes"] = ["Not en fazla 2.000 karakter olabilir."];
            if (lines.Length is < 1 or > 100 || lines.Any(x => ((x.ConsumableId is null) == (x.ComponentId is null)) ||
                (x.ConsumableId == Guid.Empty) || (x.ComponentId == Guid.Empty) || x.Quantity is < 1 or > 100000 || x.UnitCost is < 0 or > 1000000000m || x.Notes?.Length > 500) ||
                lines.Select(x => (x.ConsumableId, x.ComponentId)).Distinct().Count() != lines.Length)
                errors["items"] = ["1–100 farklı sarf/bileşen seçin; adet 1–100.000, birim maliyet 0–1.000.000.000 aralığında olmalı."];
            if (lines.All(x => x.Quantity is >= 1 and <= 100000 && x.UnitCost is >= 0 and <= 1000000000m))
            {
                var subtotal = lines.Sum(x => x.Quantity * x.UnitCost);
                if (subtotal > 1000000000000000m) errors["items"] = ["Sipariş ara toplamı 1.000.000.000.000.000 tutarını aşamaz."];
                if (discountAmount > subtotal) errors["discountAmount"] = ["İskonto sipariş ara toplamından fazla olamaz."];
            }
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var consumableIds = lines.Where(x => x.ConsumableId.HasValue).Select(x => x.ConsumableId!.Value).Distinct().ToArray();
            var componentIds = lines.Where(x => x.ComponentId.HasValue).Select(x => x.ComponentId!.Value).Distinct().ToArray();
            if (consumableIds.Length > 0 && await db.Consumables.CountAsync(x => x.CompanyId == companyId && consumableIds.Contains(x.Id), ct) != consumableIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Sarf kalemlerinden biri bu şirkette bulunamadı."] });
            if (componentIds.Length > 0 && await db.InventoryComponents.CountAsync(x => x.CompanyId == companyId && componentIds.Contains(x.Id), ct) != componentIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Bileşen kalemlerinden biri bu şirkette bulunamadı."] });

            var supplierEntry = await EnsureSupplierAsync(db, companyId, supplier!, ct);
            if (!supplierEntry.Active) return Results.Conflict(new { message = "Tedarikçi arşivlenmiş; önce tedarikçi kataloğundan geri alın." });
            number ??= $"ST-{request.OrderDate:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var order = new StockPurchaseOrder { CompanyId = companyId, OrderNumber = number, Supplier = supplierEntry.Name, SupplierId = supplierEntry.Id,
                Status = "Ordered", OrderDate = request.OrderDate, ExpectedOn = request.ExpectedOn, Currency = currency,
                TaxRatePercent = taxRatePercent, DiscountAmount = discountAmount, FreightAmount = freightAmount,
                Notes = Clean(request.Notes), CreatedByUserId = actorId };
            db.StockPurchaseOrders.Add(order);
            foreach (var line in lines) db.StockPurchaseOrderLines.Add(new StockPurchaseOrderLine { CompanyId = companyId, PurchaseOrderId = order.Id,
                ConsumableId = line.ConsumableId, ComponentId = line.ComponentId, Quantity = line.Quantity, UnitCost = line.UnitCost, Notes = Clean(line.Notes) });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "StockPurchaseOrder", TargetId = order.Id,
                Action = "stock-order.created", Details = $"{order.OrderNumber} · {lines.Length} kalem · vergi %{taxRatePercent:0.##} · iskonto {discountAmount:0.00} · nakliye {freightAmount:0.00} {currency}" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu sipariş numarası şirkette zaten kullanılıyor." }); }
            return Results.Created($"/api/stock-orders/{order.Id}", new { order.Id, order.OrderNumber });
        }).RequireAuthorization("asset.write");

        api.MapPost("/stock-orders/{id:guid}/receive", async (Guid id, ReceiptRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.LineId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1–100 farklı sipariş satırı ve her satır için 1–100.000 arası miktar girin."] });
            if (request.ReceivedOn == default || request.Notes?.Length > 1000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim tarihi zorunludur; not en fazla 1.000 karakter olabilir."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.StockPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"StockPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Cancelled" or "Received") return Results.Conflict(new { message = "Bu sipariş artık teslim alınamaz." });
            if (request.ReceivedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim tarihi sipariş tarihinden önce olamaz."] });
            var lineIds = items.Select(x => x.LineId).ToArray();
            var lines = await db.StockPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırlarından biri bulunamadı; stok değişmedi." });
            var allOrderLines = await db.StockPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
            var orderSubtotal = allOrderLines.Sum(x => x.Quantity * x.UnitCost);
            var taxAmount = CalculateTax(orderSubtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(allOrderLines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, orderSubtotal);
            var consumableIds = lines.Where(x => x.ConsumableId.HasValue).Select(x => x.ConsumableId!.Value).Distinct().Order().ToArray();
            var componentIds = lines.Where(x => x.ComponentId.HasValue).Select(x => x.ComponentId!.Value).Distinct().Order().ToArray();
            var consumables = consumableIds.Length == 0 ? [] : await db.Consumables.FromSqlInterpolated($"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({consumableIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
            var components = componentIds.Length == 0 ? [] : await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({componentIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
            if (consumables.Count != consumableIds.Length || components.Count != componentIds.Length) return Results.Conflict(new { message = "Sipariş stoğu bulunamadı; hiçbir kayıt değişmedi." });
            foreach (var line in lines)
            {
                var quantity = items.Single(x => x.LineId == line.Id).Quantity;
                if (quantity > line.Quantity - line.QuantityReceived) return Results.Conflict(new { message = "Teslim alınan miktar kalan miktarı aşamaz; hiçbir stok değişmedi." });
                var total = line.ConsumableId is Guid consumableId ? consumables.Single(x => x.Id == consumableId).TotalQuantity : components.Single(x => x.Id == line.ComponentId).TotalQuantity;
                if ((long)total + quantity > 100000) return Results.Conflict(new { message = "Toplam stok 100.000 adedi aşamaz; hiçbir stok değişmedi." });
            }
            var note = Clean(request.Notes); var received = 0;
            foreach (var line in lines)
            {
                var quantity = items.Single(x => x.LineId == line.Id).Quantity; received += quantity; line.QuantityReceived += quantity;
                var landedUnitCost = landedUnitCosts[line.Id];
                var details = $"Sipariş {order.OrderNumber} · teslim alma {request.ReceivedOn:yyyy-MM-dd}" + (note is null ? "" : $" · {note}");
                if (line.ConsumableId is Guid consumableId)
                {
                    var item = consumables.Single(x => x.Id == consumableId); item.TotalQuantity += quantity; item.Supplier = order.Supplier; item.SupplierId = order.SupplierId;
                    item.UnitCost = landedUnitCost; item.Currency = order.Currency; item.PurchaseDate = request.ReceivedOn; item.UpdatedAt = DateTimeOffset.UtcNow;
                    db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id, ActorUserId = actorId, MovementType = "Received", Quantity = quantity, Notes = details });
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Consumable", TargetId = item.Id, Action = "stock-order.received", Details = $"{order.OrderNumber} · {quantity} adet · birim maliyet {landedUnitCost:0.00} {order.Currency}" });
                }
                else
                {
                    var item = components.Single(x => x.Id == line.ComponentId); item.TotalQuantity += quantity; item.Supplier = order.Supplier; item.SupplierId = order.SupplierId;
                    item.UnitCost = landedUnitCost; item.Currency = order.Currency; item.PurchaseDate = request.ReceivedOn; item.UpdatedAt = DateTimeOffset.UtcNow;
                    db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = item.Id, ActorUserId = actorId, MovementType = "Received", Quantity = quantity, Notes = details });
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "InventoryComponent", TargetId = item.Id, Action = "stock-order.received", Details = $"{order.OrderNumber} · {quantity} adet · birim maliyet {landedUnitCost:0.00} {order.Currency}" });
                }
            }
            var all = await db.StockPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id).ToListAsync(ct);
            var totalReceived = all.Sum(x => x.QuantityReceived) + received; var totalOrdered = all.Sum(x => x.Quantity);
            order.Status = totalReceived == totalOrdered ? "Received" : "PartiallyReceived"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "StockPurchaseOrder", TargetId = id,
                Action = "stock-order.receipt", Details = $"{received} adet teslim alındı; toplam {totalReceived}/{totalOrdered}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { order.Status, receivedQuantity = received, orderQuantityReceived = totalReceived, orderQuantity = totalOrdered });
        }).RequireAuthorization("asset.write");

        api.MapPost("/stock-orders/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.StockPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"StockPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Received" or "Cancelled") return Results.Conflict(new { message = "Tamamlanmış veya iptal edilmiş sipariş değiştirilemez." });
            order.Status = "Cancelled"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "StockPurchaseOrder", TargetId = id, Action = "stock-order.cancelled", Details = order.OrderNumber });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static async Task<(Guid? Id, string Name, bool Active)> EnsureSupplierAsync(AppDbContext db, Guid companyId, string name, CancellationToken ct)
    {
        var normalized = name.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        var existing = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.NormalizedName == normalized, ct);
        if (existing is null)
        {
            existing = new SupplierCatalogEntry { CompanyId = companyId, Name = name, NormalizedName = normalized };
            db.SupplierCatalogEntries.Add(existing);
            return (existing.Id, existing.Name, true);
        }
        return (existing.Id, existing.Name, existing.IsActive);
    }
    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool Actor(ClaimsPrincipal user, out Guid userId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static decimal CalculateTax(decimal subtotal, decimal discount, decimal taxRatePercent) =>
        Math.Round((subtotal - discount) * taxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
    private static decimal CalculateTotal(decimal subtotal, decimal discount, decimal tax, decimal freight) => subtotal - discount + tax + freight;
    private static Dictionary<Guid, decimal> CalculateLandedUnitCosts(IReadOnlyList<(Guid Id, int Quantity, decimal UnitCost)> lines, decimal discount, decimal tax, decimal freight, decimal subtotal)
    {
        var costs = new Dictionary<Guid, decimal>(lines.Count);
        var allocatedDiscount = 0m; var allocatedTax = 0m; var allocatedFreight = 0m;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index]; var isLast = index == lines.Count - 1;
            var lineSubtotal = line.Quantity * line.UnitCost;
            var discountShare = isLast ? discount - allocatedDiscount : Allocate(discount, lineSubtotal, subtotal);
            var taxShare = isLast ? tax - allocatedTax : Allocate(tax, lineSubtotal, subtotal);
            var freightShare = isLast ? freight - allocatedFreight : Allocate(freight, lineSubtotal, subtotal);
            allocatedDiscount += discountShare; allocatedTax += taxShare; allocatedFreight += freightShare;
            costs[line.Id] = Math.Round((lineSubtotal - discountShare + taxShare + freightShare) / line.Quantity, 2, MidpointRounding.AwayFromZero);
        }
        return costs;

        static decimal Allocate(decimal amount, decimal lineSubtotal, decimal orderSubtotal) => orderSubtotal == 0m
            ? 0m
            : Math.Round(amount * lineSubtotal / orderSubtotal, 2, MidpointRounding.AwayFromZero);
    }
    private sealed record CreateRequest(string? OrderNumber, string? Supplier, DateOnly OrderDate, DateOnly? ExpectedOn,
        string? Currency, decimal? TaxRatePercent, decimal? DiscountAmount, decimal? FreightAmount, string? Notes, CreateLine[]? Items);
    private sealed record CreateLine(Guid? ConsumableId, Guid? ComponentId, int Quantity, decimal UnitCost, string? Notes);
    private sealed record ReceiptRequest(ReceiptLine[]? Items, DateOnly ReceivedOn, string? Notes);
    private sealed record ReceiptLine(Guid LineId, int Quantity);
}
