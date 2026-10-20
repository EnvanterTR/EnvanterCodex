using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryPurchaseOrderEndpoints
{
    public static RouteGroupBuilder MapAccessoryPurchaseOrderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/accessory-orders/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] bool overdueOnly = false, [FromQuery] DateOnly? asOf = null) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (from is not null && to is not null && from.Value > to.Value)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (!string.IsNullOrWhiteSpace(q) && q.Trim().Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });

            var scopedOrders = db.AccessoryPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId)
                .InLocationScope(db, companyId, allowedLocations);
            var query = from order in scopedOrders
                        join line in db.AccessoryPurchaseOrderLines.AsNoTracking()
                            on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                        join accessory in db.Accessories.AsNoTracking()
                            on new { line.CompanyId, Id = line.AccessoryId } equals new { accessory.CompanyId, Id = accessory.Id }
                        where order.CompanyId == companyId
                        select new { order, line, accessory };
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.order.Status == status.Trim());
            if (from is not null) query = query.Where(x => x.order.OrderDate >= from.Value);
            if (to is not null) query = query.Where(x => x.order.OrderDate <= to.Value);
            if (overdueOnly)
            {
                var cutoff = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
                query = query.Where(x => (x.order.Status == "Ordered" || x.order.Status == "PartiallyReceived") && x.order.ExpectedOn != null && x.order.ExpectedOn < cutoff);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.order.OrderNumber.Contains(term) || x.order.Supplier.Contains(term) ||
                    (x.accessory.ItemNumber != null && x.accessory.ItemNumber.Contains(term)) ||
                    (x.order.Notes != null && x.order.Notes.Contains(term)) || x.accessory.Name.Contains(term));
            }

            const int exportLimit = 50_000;
            var count = await query.CountAsync(ct);
            if (count > exportLimit)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = [$"Dışa aktarma en fazla {exportLimit:N0} sipariş satırını destekler. Sonuçları arama veya durum filtresiyle daraltın."] });

            var rows = await query.OrderByDescending(x => x.order.OrderDate).ThenBy(x => x.order.OrderNumber).ThenBy(x => x.accessory.Name)
                .Select(x => new
                {
                    x.order.OrderNumber, x.order.Supplier, x.order.Status, x.order.OrderDate, x.order.ExpectedOn, x.order.Currency,
                    Accessory = x.accessory.Name, x.accessory.ItemNumber,
                    x.line.Quantity, x.line.QuantityReceived, Remaining = x.line.Quantity - x.line.QuantityReceived,
                    x.line.UnitCost, TotalCost = x.line.Quantity * x.line.UnitCost,
                    Subtotal = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.order.Id)
                        .Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m,
                    x.order.TaxRatePercent, x.order.DiscountAmount, x.order.FreightAmount, x.line.Notes
                }).ToListAsync(ct);

            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                var trimmed = value.TrimStart();
                var startsWithFormula = trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@');
                var safe = startsWithFormula ? "'" + value : value;
                return "\"" + safe.Replace("\"", "\"\"") + "\"";
            }

            var csv = new StringBuilder();
            csv.AppendLine("Sipariş No;Tedarikçi;Durum;Sipariş Tarihi;Beklenen Tarih;Para Birimi;Aksesuar;Stok Kodu;Sipariş Adedi;Teslim Alınan;Kalan;Birim Bedeli;Satır Toplamı;Ara Toplam;İskonto;Vergi Oranı %;Vergi;Nakliye;Genel Toplam;Satır Notu");
            foreach (var row in rows)
            {
                var taxAmount = CalculateTax(row.Subtotal, row.DiscountAmount, row.TaxRatePercent);
                var grandTotal = CalculateTotal(row.Subtotal, row.DiscountAmount, taxAmount, row.FreightAmount);
                csv.AppendLine(string.Join(';', new[]
                {
                    Cell(row.OrderNumber), Cell(row.Supplier), Cell(row.Status), Cell(row.OrderDate.ToString("yyyy-MM-dd")),
                    Cell(row.ExpectedOn?.ToString("yyyy-MM-dd")), Cell(row.Currency), Cell(row.Accessory), Cell(row.ItemNumber),
                    row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.QuantityReceived.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.UnitCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    row.TotalCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    row.Subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    row.DiscountAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    row.TaxRatePercent.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    taxAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    row.FreightAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    grandTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(row.Notes)
                }));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-aksesuar-siparisleri-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-orders/suppliers", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var names = new List<string>();
            names.AddRange(await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).Select(x => x.Name).ToListAsync(ct));
            names.AddRange(await db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => x.Supplier).ToListAsync(ct));
            names.AddRange(await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Supplier != null).Select(x => x.Supplier!).ToListAsync(ct));
            names.AddRange(await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Supplier != null).Select(x => x.Supplier!).ToListAsync(ct));
            names.AddRange(await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId && x.Supplier != null).Select(x => x.Supplier!).ToListAsync(ct));
            names.AddRange(await db.SoftwareLicenses.AsNoTracking().Where(x => x.CompanyId == companyId && x.Supplier != null).Select(x => x.Supplier!).ToListAsync(ct));
            names.AddRange(await db.AssetMaintenances.AsNoTracking().Where(x => x.CompanyId == companyId && x.Supplier != null).Select(x => x.Supplier!).ToListAsync(ct));
            var items = names.Select(x => x.Trim()).Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(500).ToArray();
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-orders", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] DateOnly? asOf,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] bool overdueOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (from is not null && to is not null && from.Value > to.Value)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId)
                .InLocationScope(db, companyId, allowedLocations);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (from is not null) query = query.Where(x => x.OrderDate >= from.Value);
            if (to is not null) query = query.Where(x => x.OrderDate <= to.Value);
            if (overdueOnly)
            {
                var cutoff = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
                query = query.Where(x => (x.Status == "Ordered" || x.Status == "PartiallyReceived") && x.ExpectedOn != null && x.ExpectedOn < cutoff);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
                query = query.Where(x => x.OrderNumber.Contains(term) || x.Supplier.Contains(term) ||
                    (x.Notes != null && x.Notes.Contains(term)) || db.AccessoryPurchaseOrderLines.Any(line =>
                        line.CompanyId == companyId && line.PurchaseOrderId == x.Id &&
                        db.Accessories.Any(accessory => accessory.CompanyId == companyId && accessory.Id == line.AccessoryId &&
                            (accessory.Name.Contains(term) || (accessory.ItemNumber != null && accessory.ItemNumber.Contains(term))))));
            }
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.OrderDate, x.ExpectedOn, x.Currency, x.CreatedAt, x.UpdatedAt,
                    lineCount = db.AccessoryPurchaseOrderLines.Count(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id),
                    quantity = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (int?)line.Quantity) ?? 0,
                    quantityReceived = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (int?)line.QuantityReceived) ?? 0,
                    subtotal = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount
                }).ToListAsync(ct);
            var items = rows.Select(x =>
            {
                var taxAmount = CalculateTax(x.subtotal, x.DiscountAmount, x.TaxRatePercent);
                return new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.OrderDate, x.ExpectedOn, x.Currency, x.CreatedAt, x.UpdatedAt,
                    x.lineCount, x.quantity, x.quantityReceived, x.subtotal, x.TaxRatePercent, x.DiscountAmount, x.FreightAmount, taxAmount,
                    totalCost = CalculateTotal(x.subtotal, x.DiscountAmount, taxAmount, x.FreightAmount) };
            }).ToArray();
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-orders/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var order = await db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .InLocationScope(db, companyId, allowedLocations)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.ApprovalNote, x.ReviewedByUserId, x.ReviewedAt, x.OrderDate, x.ExpectedOn, x.Currency, x.Notes,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var hiddenActorIds = await EmployeeAccessIdentityScope.HiddenUserIdsAsync(db, companyId, employeeScope,
                [order.ReviewedByUserId], ct);
            var visibleOrder = new
            {
                order.Id, order.OrderNumber, order.Supplier, order.Status, order.ApprovalStatus, order.ApprovalNote,
                ReviewedByUserId = order.ReviewedByUserId is Guid reviewerId && hiddenActorIds.Contains(reviewerId)
                    ? (Guid?)null : order.ReviewedByUserId,
                order.ReviewedAt, order.OrderDate, order.ExpectedOn, order.Currency, order.Notes,
                order.TaxRatePercent, order.DiscountAmount, order.FreightAmount, order.CreatedAt, order.UpdatedAt
            };
            var lines = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).Select(x => new
                {
                    x.Id, x.AccessoryId, accessoryName = db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.AccessoryId).Select(a => a.Name).FirstOrDefault(),
                    itemNumber = db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.AccessoryId).Select(a => a.ItemNumber).FirstOrDefault(),
                    x.Quantity, x.QuantityReceived, x.QuantityReturned, remaining = x.Quantity - x.QuantityReceived, x.UnitCost, totalCost = x.Quantity * x.UnitCost, x.Notes
                }).ToListAsync(ct);
            var subtotal = lines.Sum(x => x.totalCost);
            var taxAmount = CalculateTax(subtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(lines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, subtotal);
            var pricedLines = lines.Select(line => new { line.Id, line.AccessoryId, line.accessoryName, line.itemNumber,
                line.Quantity, line.QuantityReceived, line.QuantityReturned, line.remaining, line.UnitCost, line.totalCost, landedUnitCost = landedUnitCosts[line.Id], line.Notes }).ToArray();
            var totalCost = CalculateTotal(subtotal, order.DiscountAmount, taxAmount, order.FreightAmount);
            var budget = allowedLocations is null
                ? await db.CompanyPurchaseBudgets.AsNoTracking().Where(x => x.CompanyId == companyId &&
                    x.BudgetYear == order.OrderDate.Year && x.Currency == order.Currency).Select(x => (decimal?)x.Amount).SingleOrDefaultAsync(ct)
                : null;
            decimal? committedBeforeThisOrder = budget is null ? null : await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db,
                companyId, order.OrderDate.Year, order.Currency, ct, exceptAccessoryOrderId: id);
            return Results.Ok(new { order = visibleOrder, lines = pricedLines, subtotal, taxableAmount = subtotal - order.DiscountAmount, taxAmount,
                totalCost, budget = budget is null ? null : new { amount = budget.Value, committedBeforeThisOrder,
                    projectedCommitted = committedBeforeThisOrder!.Value + totalCost, exceeds = committedBeforeThisOrder!.Value + totalCost > budget.Value },
                quantity = lines.Sum(x => x.Quantity), quantityReceived = lines.Sum(x => x.QuantityReceived) });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessory-orders", async (CreateOrderRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var lines = request.Items ?? [];
            var errors = new Dictionary<string, string[]>();
            var supplier = Clean(request.Supplier);
            var orderNumber = Clean(request.OrderNumber);
            var currency = request.Currency?.Trim().ToUpperInvariant() ?? "";
            var taxRatePercent = request.TaxRatePercent ?? 0m;
            var discountAmount = request.DiscountAmount ?? 0m;
            var freightAmount = request.FreightAmount ?? 0m;
            if (supplier is null || supplier.Length > 200) errors["supplier"] = ["Tedarikçi zorunludur ve en fazla 200 karakter olabilir."];
            if (orderNumber?.Length > 80) errors["orderNumber"] = ["Sipariş numarası en fazla 80 karakter olabilir."];
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) errors["currency"] = ["Üç harfli para birimi girin; ör. TRY, EUR veya USD."];
            if (taxRatePercent is < 0 or > 100) errors["taxRatePercent"] = ["Vergi oranı %0 ile %100 arasında olmalıdır."];
            if (discountAmount is < 0 or > 1000000000000000m) errors["discountAmount"] = ["İskonto 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (freightAmount is < 0 or > 1000000000000000m) errors["freightAmount"] = ["Nakliye 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (request.OrderDate == default) errors["orderDate"] = ["Sipariş tarihi zorunludur."];
            if (request.ExpectedOn is DateOnly expected && expected < request.OrderDate) errors["expectedOn"] = ["Beklenen teslim tarihi sipariş tarihinden önce olamaz."];
            if (request.Notes?.Length > 2000) errors["notes"] = ["Not en fazla 2.000 karakter olabilir."];
            if (lines.Length is < 1 or > 100 || lines.Any(x => x.AccessoryId == Guid.Empty || x.Quantity is < 1 or > 100000 || x.UnitCost is < 0 or > 1000000000m || x.Notes?.Length > 500) || lines.Select(x => x.AccessoryId).Distinct().Count() != lines.Length)
                errors["items"] = ["1–100 farklı aksesuar seçin; adet 1–100.000, birim fiyat 0–1.000.000.000 aralığında olmalı."];
            if (lines.All(x => x.Quantity is >= 1 and <= 100000 && x.UnitCost is >= 0 and <= 1000000000m))
            {
                var validatedSubtotal = lines.Sum(x => x.Quantity * x.UnitCost);
                if (validatedSubtotal > 1000000000000000m) errors["items"] = ["Sipariş ara toplamı 1.000.000.000.000.000 tutarını aşamaz."];
                if (discountAmount > validatedSubtotal) errors["discountAmount"] = ["İskonto sipariş ara toplamından fazla olamaz."];
            }
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var accessoryIds = lines.Select(x => x.AccessoryId).ToArray();
            var existingIds = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
            if (existingIds.Count != lines.Length) return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Aksesuarlardan biri bu şirkette bulunamadı."] });
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoriesAsync(db, companyId, accessoryIds, allowedLocations, ct))
                return Results.NotFound(new { message = "Sipariş kalemlerinden biri erişilebilir konumlarda bulunamadı." });
            var normalizedSupplier = supplier!.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
            var supplierEntry = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.NormalizedName == normalizedSupplier, ct);
            if (supplierEntry is null)
            {
                supplierEntry = new SupplierCatalogEntry { CompanyId = companyId, Name = supplier, NormalizedName = normalizedSupplier };
                db.SupplierCatalogEntries.Add(supplierEntry);
            }
            else if (!supplierEntry.IsActive) return Results.Conflict(new { message = "Bu tedarikçi arşivlenmiş. Yeni siparişten önce tedarikçi kataloğundan geri alın." });
            else supplier = supplierEntry.Name;
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var subtotal = lines.Sum(x => x.Quantity * x.UnitCost);
            var taxAmount = CalculateTax(subtotal, discountAmount, taxRatePercent);
            var orderTotal = CalculateTotal(subtotal, discountAmount, taxAmount, freightAmount);
            var budget = await db.CompanyPurchaseBudgets.AsNoTracking().Where(x => x.CompanyId == companyId &&
                x.BudgetYear == request.OrderDate.Year && x.Currency == currency).Select(x => (decimal?)x.Amount).SingleOrDefaultAsync(ct);
            var committed = budget is null ? 0m : await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db, companyId,
                request.OrderDate.Year, currency, ct);
            var approvalStatus = budget is not null && committed + orderTotal > budget.Value ? "Pending" : "NotRequired";
            orderNumber ??= $"ST-{request.OrderDate:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var order = new AccessoryPurchaseOrder
            {
                CompanyId = companyId, OrderNumber = orderNumber, Supplier = supplier!, SupplierId = supplierEntry.Id, Status = "Ordered",
                ApprovalStatus = approvalStatus,
                OrderDate = request.OrderDate, ExpectedOn = request.ExpectedOn, Currency = currency,
                TaxRatePercent = taxRatePercent, DiscountAmount = discountAmount, FreightAmount = freightAmount,
                Notes = Clean(request.Notes), CreatedByUserId = actorId
            };
            db.AccessoryPurchaseOrders.Add(order);
            foreach (var line in lines)
                db.AccessoryPurchaseOrderLines.Add(new AccessoryPurchaseOrderLine
                {
                    CompanyId = companyId, PurchaseOrderId = order.Id, AccessoryId = line.AccessoryId,
                    Quantity = line.Quantity, UnitCost = line.UnitCost, Notes = Clean(line.Notes)
                });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = order.Id, Action = "accessory-order.created", Details = $"{order.OrderNumber} · {lines.Length} kalem · bütçe onayı {approvalStatus}" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu sipariş numarası şirkette zaten kullanılıyor. Başka bir numara seçin." }); }
            await tx.CommitAsync(ct);
            return Results.Created($"/api/accessory-orders/{order.Id}", new { order.Id, order.OrderNumber, order.ApprovalStatus,
                approvalRequired = order.ApprovalStatus == "Pending",
                budgetAmount = allowedLocations is null ? (decimal?)budget : null,
                committedBefore = allowedLocations is null ? (decimal?)committed : null,
                orderTotal });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-orders/{id:guid}/receive", async (Guid id, ReceiveOrderRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.LineId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1–100 farklı sipariş satırı seçin; her teslim alınan miktar 1–100.000 arasında olmalı."] });
            if (request.ReceivedOn == default || request.Notes?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim alma tarihi zorunludur; not en fazla 1.000 karakter olabilir."] });

            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (order.ApprovalStatus is "Pending" or "Rejected") return Results.Conflict(new { message = order.ApprovalStatus == "Pending"
                ? "Bütçe aşımındaki sipariş, yetkili yönetici onayından önce teslim alınamaz."
                : "Bütçe istisnası reddedilen sipariş teslim alınamaz." });
            if (order.Status is "Cancelled" or "Received") return Results.Conflict(new { message = "Bu sipariş artık teslim alınamaz." });
            if (request.ReceivedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim alma tarihi sipariş tarihinden önce olamaz."] });
            var lineIds = items.Select(x => x.LineId).ToArray();
            var lines = await db.AccessoryPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırlarından biri bulunamadı; stok değişmedi." });
            var allOrderLines = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
            var orderSubtotal = allOrderLines.Sum(x => x.Quantity * x.UnitCost);
            var taxAmount = CalculateTax(orderSubtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(allOrderLines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, orderSubtotal);
            var accessoryIds = lines.Select(x => x.AccessoryId).Distinct().Order().ToArray();
            var accessories = await db.Accessories.FromSqlInterpolated(
                $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({accessoryIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
            if (accessories.Count != accessoryIds.Length) return Results.Conflict(new { message = "Sipariş kalemindeki aksesuar bulunamadı; stok değişmedi." });
            foreach (var line in lines)
            {
                var quantity = items.Single(x => x.LineId == line.Id).Quantity;
                if (quantity > line.Quantity - line.QuantityReceived)
                    return Results.Conflict(new { message = "Teslim alınan miktar siparişte kalan miktarı aşamaz; hiçbir stok değişmedi." });
                var accessory = accessories.Single(x => x.Id == line.AccessoryId);
                if ((long)accessory.TotalQuantity + quantity > 100000)
                    return Results.Conflict(new { message = $"{accessory.Name} için toplam stok 100.000 adedi aşar; hiçbir stok değişmedi." });
            }

            var note = Clean(request.Notes);
            foreach (var line in lines)
            {
                var quantity = items.Single(x => x.LineId == line.Id).Quantity;
                var accessory = accessories.Single(x => x.Id == line.AccessoryId);
                var landedUnitCost = landedUnitCosts[line.Id];
                line.QuantityReceived += quantity;
                accessory.TotalQuantity += quantity;
                accessory.Supplier = order.Supplier;
                accessory.SupplierId = order.SupplierId;
                accessory.UnitCost = landedUnitCost;
                accessory.Currency = order.Currency;
                accessory.PurchaseDate = request.ReceivedOn;
                accessory.UpdatedAt = DateTimeOffset.UtcNow;
                db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = accessory.Id, ActorUserId = actorId, MovementType = "Received", Quantity = quantity, Notes = $"Sipariş {order.OrderNumber} · teslim alma {request.ReceivedOn:yyyy-MM-dd}" + (note is null ? "" : $" · {note}") });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", TargetId = accessory.Id, Action = "accessory-order.received", Details = $"Sipariş {order.OrderNumber} · {quantity} adet · birim maliyet {landedUnitCost:0.00} {order.Currency}" });
            }
            var allLines = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id).ToListAsync(ct);
            var nextReceived = allLines.Sum(x => x.QuantityReceived) + items.Sum(x => x.Quantity);
            var orderedQuantity = allLines.Sum(x => x.Quantity);
            order.Status = nextReceived == orderedQuantity ? "Received" : "PartiallyReceived";
            order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = id, Action = "accessory-order.receipt", Details = $"{items.Sum(x => x.Quantity)} adet teslim alındı; toplam {nextReceived}/{orderedQuantity}" });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new { order.Status, receivedQuantity = items.Sum(x => x.Quantity), orderQuantityReceived = nextReceived, orderQuantity = orderedQuantity });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-orders/{id:guid}/approval", async (Guid id, ApprovalDecisionRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var decision = request.Decision?.Trim(); var note = Clean(request.Note);
            if (decision is not ("Approved" or "Rejected"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["decision"] = ["Karar Approved veya Rejected olmalıdır."] });
            if (note is null || note.Length is < 10 or > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Karar gerekçesi 10–2.000 karakter olmalıdır."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (order.ApprovalStatus != "Pending") return Results.Conflict(new { message = "Yalnızca bekleyen bütçe onayları karara bağlanabilir." });
            if (order.CreatedByUserId == actorId) return Results.Conflict(new { message = "Siparişi oluşturan kullanıcı kendi bütçe istisnasını onaylayamaz veya reddedemez." });
            order.ApprovalStatus = decision; order.ReviewedByUserId = actorId; order.ReviewedAt = DateTimeOffset.UtcNow;
            order.ApprovalNote = note; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = id,
                Action = decision == "Approved" ? "accessory-order.approved" : "accessory-order.rejected", Details = $"{order.OrderNumber} · {note}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { order.Id, order.OrderNumber, order.ApprovalStatus, order.ReviewedByUserId, order.ReviewedAt, order.ApprovalNote });
        }).RequireAuthorization("settings.manage");

        api.MapPost("/accessory-orders/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (order.Status is "Received" or "Cancelled") return Results.Conflict(new { message = "Tamamlanmış veya iptal edilmiş sipariş değiştirilemez." });
            order.Status = "Cancelled"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = id, Action = "accessory-order.cancelled", Details = order.OrderNumber });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool UserId(ClaimsPrincipal user, out Guid userId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
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
            ? 0m : Math.Round(amount * lineSubtotal / orderSubtotal, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record CreateOrderRequest(string? OrderNumber, string? Supplier, DateOnly OrderDate, DateOnly? ExpectedOn,
        string? Currency, decimal? TaxRatePercent, decimal? DiscountAmount, decimal? FreightAmount, string? Notes, CreateOrderLine[]? Items);
    private sealed record CreateOrderLine(Guid AccessoryId, int Quantity, decimal UnitCost, string? Notes);
    private sealed record ReceiveOrderRequest(ReceiptLine[]? Items, DateOnly ReceivedOn, string? Notes);
    private sealed record ReceiptLine(Guid LineId, int Quantity);
    private sealed record ApprovalDecisionRequest(string? Decision, string? Note);
}
