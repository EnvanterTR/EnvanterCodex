using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetPurchaseOrderEndpoints
{
    public static RouteGroupBuilder MapAssetPurchaseOrderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/asset-orders/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var scopedOrders = db.AssetPurchaseOrders.AsNoTracking().Where(order => order.CompanyId == companyId)
                .InLocationScope(db, companyId, allowedLocations);
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(status) && status is not ("Ordered" or "PartiallyReceived" or "Received" or "Cancelled"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Sipariş durumu geçersiz."] });
            var query = from order in scopedOrders
                        join line in db.AssetPurchaseOrderLines.AsNoTracking() on new { order.CompanyId, Id = order.Id } equals new { line.CompanyId, Id = line.PurchaseOrderId }
                        where order.CompanyId == companyId
                        select new { order, line };
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.order.Status == status);
            if (from is not null) query = query.Where(x => x.order.OrderDate >= from);
            if (to is not null) query = query.Where(x => x.order.OrderDate <= to);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.order.OrderNumber.Contains(term) || x.order.Supplier.Contains(term) ||
                    x.line.Name.Contains(term) || (x.order.Notes != null && x.order.Notes.Contains(term)));
            }
            const int limit = 50_000;
            if (await query.CountAsync(ct) > limit)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = [$"Dışa aktarma en fazla {limit:N0} sipariş satırını destekler. Sonuçları arama veya durum filtresiyle daraltın."] });
            var rows = await query.OrderByDescending(x => x.order.OrderDate).ThenBy(x => x.order.OrderNumber)
                .Select(x => new { x.order.OrderNumber, x.order.Supplier, x.order.Status, x.order.OrderDate, x.order.ExpectedOn,
                    x.order.Currency, x.line.Name, x.line.Quantity, x.line.QuantityReceived, Remaining = x.line.Quantity - x.line.QuantityReceived,
                    x.line.UnitCost, Total = x.line.UnitCost * x.line.Quantity,
                    Subtotal = db.AssetPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.order.Id)
                        .Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m,
                    x.order.TaxRatePercent, x.order.DiscountAmount, x.order.FreightAmount, x.line.WarrantyExpiresAt, x.line.Notes,
                    Location = db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == x.line.LocationCatalogId)
                        .Select(location => location.Name).FirstOrDefault() })
                .ToListAsync(ct);
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                var trimmed = value.TrimStart();
                var formula = trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@';
                return "\"" + (formula ? "'" + value : value).Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("Sipariş No;Tedarikçi;Durum;Sipariş Tarihi;Beklenen Tarih;Para Birimi;Varlık;Konum;Sipariş Adedi;Teslim Alınan;Kalan;Birim Bedeli;Satır Toplamı;Ara Toplam;İskonto;Vergi Oranı %;Vergi;Nakliye;Genel Toplam;Garanti Bitişi;Satır Notu\r\n");
            foreach (var row in rows)
            {
                var taxAmount = CalculateTax(row.Subtotal, row.DiscountAmount, row.TaxRatePercent);
                var grandTotal = CalculateTotal(row.Subtotal, row.DiscountAmount, taxAmount, row.FreightAmount);
                csv.AppendLine(string.Join(';', new[] { Cell(row.OrderNumber), Cell(row.Supplier), Cell(row.Status),
                Cell(row.OrderDate.ToString("yyyy-MM-dd")), Cell(row.ExpectedOn?.ToString("yyyy-MM-dd")), Cell(row.Currency), Cell(row.Name), Cell(row.Location),
                row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), row.QuantityReceived.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture), row.UnitCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), row.Subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.DiscountAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), row.TaxRatePercent.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                taxAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), row.FreightAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                grandTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(row.WarrantyExpiresAt?.ToString("yyyy-MM-dd")), Cell(row.Notes) }));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-varlik-siparisleri-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/asset-orders", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(status) && status is not ("Ordered" or "PartiallyReceived" or "Received" or "Cancelled"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Sipariş durumu geçersiz."] });
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId)
                .InLocationScope(db, companyId, allowedLocations);
            if (status is not null) query = query.Where(x => x.Status == status);
            if (from is not null) query = query.Where(x => x.OrderDate >= from);
            if (to is not null) query = query.Where(x => x.OrderDate <= to);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.OrderNumber.Contains(term) || x.Supplier.Contains(term) ||
                    (x.Notes != null && x.Notes.Contains(term)) || db.AssetPurchaseOrderLines.Any(l =>
                        l.CompanyId == companyId && l.PurchaseOrderId == x.Id && l.Name.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.OrderDate, x.ExpectedOn, x.Currency,
                    lineCount = db.AssetPurchaseOrderLines.Count(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id),
                    quantity = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.Quantity) ?? 0,
                    quantityReceived = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.QuantityReceived) ?? 0,
                    subtotal = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (decimal?)(l.Quantity * l.UnitCost)) ?? 0m,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount
                }).ToListAsync(ct);
            var items = rows.Select(x =>
            {
                var taxAmount = CalculateTax(x.subtotal, x.DiscountAmount, x.TaxRatePercent);
                return new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.OrderDate, x.ExpectedOn, x.Currency,
                    x.lineCount, x.quantity, x.quantityReceived, subtotal = x.subtotal, x.TaxRatePercent, x.DiscountAmount, x.FreightAmount,
                    taxAmount, totalCost = CalculateTotal(x.subtotal, x.DiscountAmount, taxAmount, x.FreightAmount) };
            }).ToList();
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization("asset.read");

        api.MapGet("/asset-orders/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var order = await db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .InLocationScope(db, companyId, allowedLocations)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.ApprovalStatus, x.ApprovalNote, x.ReviewedByUserId, x.ReviewedAt,
                    x.CreatedByUserId, x.OrderDate, x.ExpectedOn, x.Currency, x.Notes,
                    x.TaxRatePercent, x.DiscountAmount, x.FreightAmount, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            var lines = await (from line in db.AssetPurchaseOrderLines.AsNoTracking()
                join category in db.AssetCategories.AsNoTracking() on new { line.CompanyId, Id = line.CategoryId } equals new { category.CompanyId, Id = category.Id }
                where line.CompanyId == companyId && line.PurchaseOrderId == id
                orderby line.CreatedAt
                select new { line.Id, line.Name, line.CategoryId, categoryName = category.Name, line.BrandId, line.ModelId,
                    line.Quantity, line.QuantityReceived, line.QuantityReturned, remaining = line.Quantity - line.QuantityReceived,
                    line.UnitCost, line.WarrantyExpiresAt, line.Notes, line.LocationCatalogId,
                    locationName = db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == line.LocationCatalogId)
                        .Select(location => location.Name).FirstOrDefault() }).ToListAsync(ct);
            var receivedAssets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.AssetTag).Select(x => new { x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber, x.PurchaseDate,
                    x.PurchaseOrderLineId, x.AssignedEmployeeId }).ToListAsync(ct);
            var attachments = await db.AssetPurchaseOrderAttachments.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    x.UploadedByUserId,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, actorId, ct);
            var hiddenActorIds = await EmployeeAccessIdentityScope.HiddenUserIdsAsync(db, companyId, employeeScope,
                new Guid?[] { order.CreatedByUserId, order.ReviewedByUserId }.Concat(attachments.Select(x => x.UploadedByUserId)), ct);
            var visibleOrder = new
            {
                order.Id, order.OrderNumber, order.Supplier, order.Status, order.ApprovalStatus, order.ApprovalNote,
                ReviewedByUserId = order.ReviewedByUserId is Guid reviewerId && hiddenActorIds.Contains(reviewerId)
                    ? (Guid?)null : order.ReviewedByUserId,
                order.ReviewedAt,
                CreatedByUserId = order.CreatedByUserId is Guid creatorId && hiddenActorIds.Contains(creatorId)
                    ? (Guid?)null : order.CreatedByUserId,
                order.OrderDate, order.ExpectedOn, order.Currency, order.Notes, order.TaxRatePercent,
                order.DiscountAmount, order.FreightAmount, order.CreatedAt, order.UpdatedAt
            };
            var visibleAttachments = attachments.Select(item => new
            {
                item.Id, item.OriginalFileName, item.ContentType, item.Length, item.CreatedAt,
                uploadedBy = item.UploadedByUserId is Guid uploaderId && hiddenActorIds.Contains(uploaderId) ? null : item.uploadedBy
            }).ToList();
            var subtotal = lines.Sum(x => x.Quantity * x.UnitCost);
            var taxAmount = CalculateTax(subtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(lines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, subtotal);
            var pricedLines = lines.Select(line => new
            {
                line.Id, line.Name, line.CategoryId, line.categoryName, line.BrandId, line.ModelId,
                line.Quantity, line.QuantityReceived, line.QuantityReturned, line.remaining, line.UnitCost, landedUnitCost = landedUnitCosts[line.Id].AverageUnitCost,
                line.WarrantyExpiresAt, line.Notes, line.LocationCatalogId, line.locationName
            }).ToArray();
            var budget = allowedLocations is null
                ? await db.CompanyPurchaseBudgets.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.BudgetYear == order.OrderDate.Year && x.Currency == order.Currency)
                    .Select(x => (decimal?)x.Amount).SingleOrDefaultAsync(ct)
                : null;
            decimal? committedBeforeThisOrder = null;
            if (budget is not null)
                committedBeforeThisOrder = await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db, companyId, order.OrderDate.Year,
                    order.Currency, ct, exceptAssetOrderId: id);
            return Results.Ok(new { order = visibleOrder, lines = pricedLines, subtotal, taxableAmount = subtotal - order.DiscountAmount, taxAmount,
                totalCost = CalculateTotal(subtotal, order.DiscountAmount, taxAmount, order.FreightAmount),
                budget = budget is null ? null : new { amount = budget.Value, committedBeforeThisOrder,
                    projectedCommitted = committedBeforeThisOrder!.Value + CalculateTotal(subtotal, order.DiscountAmount, taxAmount, order.FreightAmount),
                    exceeds = committedBeforeThisOrder!.Value + CalculateTotal(subtotal, order.DiscountAmount, taxAmount, order.FreightAmount) > budget.Value },
                quantity = lines.Sum(x => x.Quantity), quantityReceived = lines.Sum(x => x.QuantityReceived), assets = receivedAssets, attachments = visibleAttachments,
                maxAttachmentCount = 30, maxAttachmentSizeBytes = 10 * 1024 * 1024 });
        }).RequireAuthorization("asset.read");

        api.MapPost("/asset-orders", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var supplier = Clean(request.Supplier); var number = Clean(request.OrderNumber); var currency = request.Currency?.Trim().ToUpperInvariant() ?? "";
            var lines = request.Items ?? [];
            var taxRatePercent = request.TaxRatePercent ?? 0m;
            var discountAmount = request.DiscountAmount ?? 0m;
            var freightAmount = request.FreightAmount ?? 0m;
            var errors = new Dictionary<string, string[]>();
            if (supplier is null || supplier.Length > 200) errors["supplier"] = ["Tedarikçi zorunludur ve 200 karakteri aşamaz."];
            if (number?.Length > 80) errors["orderNumber"] = ["Sipariş numarası 80 karakteri aşamaz."];
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) errors["currency"] = ["Üç harfli para birimi girin (TRY, EUR, USD)."];
            if (request.OrderDate == default || request.ExpectedOn is DateOnly expected && expected < request.OrderDate) errors["dates"] = ["Sipariş tarihi zorunludur; beklenen tarih daha önce olamaz."];
            if (taxRatePercent is < 0 or > 100) errors["taxRatePercent"] = ["Vergi oranı %0 ile %100 arasında olmalıdır."];
            if (discountAmount is < 0 or > 1000000000000000m) errors["discountAmount"] = ["İskonto 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (freightAmount is < 0 or > 1000000000000000m) errors["freightAmount"] = ["Nakliye 0 ile 1.000.000.000.000.000 arasında olmalıdır."];
            if (request.Notes?.Length > 2000) errors["notes"] = ["Not en fazla 2.000 karakter olabilir."];
            if (lines.Length is < 1 or > 100 || lines.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Trim().Length > 200 || x.CategoryId == Guid.Empty ||
                x.BrandId == Guid.Empty || x.ModelId == Guid.Empty || x.Quantity is < 1 or > 1000 || x.UnitCost is < 0 or > 1000000000m ||
                x.WarrantyExpiresAt is DateOnly warranty && request.OrderDate != default && warranty < request.OrderDate || x.Notes?.Length > 500) ||
                lines.Sum(x => (long)x.Quantity) > 5000)
                errors["items"] = ["1–100 satır girin. Her satırda ad, kategori, 1–1.000 adet ve geçerli maliyet olmalı; toplam en fazla 5.000 varlık olabilir."];
            if (allowedLocations is not null && lines.Any(x => x.LocationCatalogId is not Guid locationId || !allowedLocations.Contains(locationId)))
                errors["items"] = ["Her sipariş satırı için erişiminiz olan etkin bir hedef konum seçin."];
            if (lines.All(x => x.Quantity is >= 1 and <= 1000 && x.UnitCost is >= 0 and <= 1000000000m))
            {
                var subtotal = lines.Sum(x => x.Quantity * x.UnitCost);
                if (subtotal > 1000000000000000m) errors["items"] = ["Sipariş ara toplamı 1.000.000.000.000.000 tutarını aşamaz."];
                if (discountAmount > subtotal) errors["discountAmount"] = ["İskonto sipariş ara toplamından fazla olamaz."];
            }
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var locationIds = lines.Where(x => x.LocationCatalogId.HasValue).Select(x => x.LocationCatalogId!.Value).Distinct().ToArray();
            if (locationIds.Length > 0 && await db.CompanyLocations.CountAsync(x => x.CompanyId == companyId && x.IsActive && locationIds.Contains(x.Id), ct) != locationIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Hedef konumlardan biri etkin değil veya bu şirkete ait değil."] });

            var categoryIds = lines.Select(x => x.CategoryId).Distinct().ToArray();
            if (await db.AssetCategories.CountAsync(x => x.CompanyId == companyId && categoryIds.Contains(x.Id), ct) != categoryIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Kategorilerden biri bu şirkete ait değil."] });
            var brandIds = lines.Where(x => x.BrandId.HasValue).Select(x => x.BrandId!.Value).Distinct().ToArray();
            if (brandIds.Length > 0 && await db.AssetBrands.CountAsync(x => x.CompanyId == companyId && brandIds.Contains(x.Id), ct) != brandIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Markalardan biri bu şirkete ait değil."] });
            var modelIds = lines.Where(x => x.ModelId.HasValue).Select(x => x.ModelId!.Value).Distinct().ToArray();
            if (modelIds.Length > 0 && await db.AssetModels.CountAsync(x => x.CompanyId == companyId && modelIds.Contains(x.Id), ct) != modelIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Modellerden biri bu şirkete ait değil."] });
            foreach (var line in lines.Where(x => x.ModelId.HasValue))
                if (await db.AssetModels.AnyAsync(x => x.CompanyId == companyId && x.Id == line.ModelId && x.BrandId != line.BrandId, ct))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Model ve marka eşleşmiyor."] });

            var supplierId = await SupplierCatalogEndpoints.ResolveSupplierIdAsync(db, companyId, supplier, ct);
            if (supplierId is null || !await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId && x.Id == supplierId && x.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplier"] = ["Etkin tedarikçi kataloğundan bir tedarikçi seçin."] });
            number ??= $"AT-{request.OrderDate:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var orderSubtotal = lines.Sum(x => x.Quantity * x.UnitCost);
            var orderTax = CalculateTax(orderSubtotal, discountAmount, taxRatePercent);
            var orderTotal = CalculateTotal(orderSubtotal, discountAmount, orderTax, freightAmount);
            var budget = await db.CompanyPurchaseBudgets.SingleOrDefaultAsync(x => x.CompanyId == companyId &&
                x.BudgetYear == request.OrderDate.Year && x.Currency == currency, ct);
            var committed = budget is null ? 0m : await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db, companyId, request.OrderDate.Year, currency, ct);
            var approvalStatus = budget is not null && committed + orderTotal > budget.Amount ? "Pending" : "NotRequired";
            var order = new AssetPurchaseOrder { CompanyId = companyId, OrderNumber = number, Supplier = supplier!, SupplierId = supplierId,
                Status = "Ordered", ApprovalStatus = approvalStatus, OrderDate = request.OrderDate, ExpectedOn = request.ExpectedOn, Currency = currency,
                TaxRatePercent = taxRatePercent, DiscountAmount = discountAmount, FreightAmount = freightAmount,
                Notes = Clean(request.Notes), CreatedByUserId = actorId };
            db.AssetPurchaseOrders.Add(order);
            foreach (var line in lines) db.AssetPurchaseOrderLines.Add(new AssetPurchaseOrderLine { CompanyId = companyId, PurchaseOrderId = order.Id,
                Name = line.Name!.Trim(), CategoryId = line.CategoryId, BrandId = line.BrandId, ModelId = line.ModelId,
                LocationCatalogId = line.LocationCatalogId, Quantity = line.Quantity, UnitCost = line.UnitCost, WarrantyExpiresAt = line.WarrantyExpiresAt, Notes = Clean(line.Notes) });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = order.Id,
                Action = "asset-order.created", Details = $"{order.OrderNumber} · {lines.Length} kalem · {lines.Sum(x => x.Quantity)} adet" +
                    (approvalStatus == "Pending" ? $" · bütçe {committed + orderTotal:0.00}/{budget!.Amount:0.00} {currency}; onay bekliyor" : "") });
            try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu sipariş numarası şirkette zaten kullanılıyor." }); }
            return Results.Created($"/api/asset-orders/{order.Id}", new { order.Id, order.OrderNumber, order.ApprovalStatus,
                approvalRequired = order.ApprovalStatus == "Pending",
                budgetAmount = allowedLocations is null ? (decimal?)budget?.Amount : null,
                committedBefore = allowedLocations is null ? (decimal?)committed : null,
                orderTotal });
        }).RequireAuthorization("asset.write");

        api.MapPost("/asset-orders/{id:guid}/receive", async (Guid id, ReceiptRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAssetOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 1000) ||
                items.Select(x => x.LineId).Distinct().Count() != items.Length || request.ReceivedOn == default || request.Notes?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Teslim tarihi ve en fazla 100 farklı satır için 1–1.000 arası adet girin."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Cancelled" or "Received") return Results.Conflict(new { message = "Bu sipariş artık teslim alınamaz." });
            if (order.ApprovalStatus is "Pending" or "Rejected") return Results.Conflict(new { message = order.ApprovalStatus == "Pending"
                ? "Bütçe aşımı için yönetici onayı bekleniyor; onaylanmadan teslim alınamaz."
                : "Sipariş bütçe onayı reddedildi; teslim alınamaz." });
            if (request.ReceivedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim tarihi sipariş tarihinden önce olamaz."] });
            var lineIds = items.Select(x => x.LineId).ToArray();
            var lines = await db.AssetPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırlarından biri bulunamadı; varlık eklenmedi." });
            foreach (var line in lines)
            {
                var qty = items.Single(x => x.LineId == line.Id).Quantity;
                if (qty > line.Quantity - line.QuantityReceived) return Results.Conflict(new { message = "Teslim alınan miktar kalan sipariş miktarını aşamaz; hiçbir varlık eklenmedi." });
            }
            var allOrderLines = await db.AssetPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
            var orderSubtotal = allOrderLines.Sum(x => x.Quantity * x.UnitCost);
            var taxAmount = CalculateTax(orderSubtotal, order.DiscountAmount, order.TaxRatePercent);
            var landedUnitCosts = CalculateLandedUnitCosts(allOrderLines.Select(x => (x.Id, x.Quantity, x.UnitCost)).ToArray(),
                order.DiscountAmount, taxAmount, order.FreightAmount, orderSubtotal);
            var note = Clean(request.Notes); var received = 0;
            foreach (var line in lines)
            {
                var qty = items.Single(x => x.LineId == line.Id).Quantity;
                var allocation = landedUnitCosts[line.Id];
                for (var index = 0; index < qty; index++)
                {
                    var landedUnitCost = allocation.UnitCostAt(line.QuantityReceived + index);
                    var asset = new Asset { CompanyId = companyId, PurchaseOrderId = id, PurchaseOrderLineId = line.Id, AssetTag = $"AT-{request.ReceivedOn:yyMMdd}-{Guid.NewGuid():N}"[..23].ToUpperInvariant(),
                        Name = line.Name, CategoryId = line.CategoryId, BrandId = line.BrandId, ModelId = line.ModelId,
                        LocationCatalogId = line.LocationCatalogId,
                        Location = line.LocationCatalogId is Guid locationId
                            ? await db.CompanyLocations.Where(location => location.CompanyId == companyId && location.Id == locationId).Select(location => location.Name).SingleAsync(ct)
                            : null,
                        PurchaseDate = request.ReceivedOn, WarrantyExpiresAt = line.WarrantyExpiresAt, PurchaseCost = landedUnitCost, Currency = order.Currency };
                    db.Assets.Add(asset);
                    var details = $"Satın alma siparişi {order.OrderNumber} · {order.Supplier}" + (note is null ? "" : $" · {note}");
                    db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                        EventType = "Purchased", Details = details });
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", TargetId = asset.Id,
                        Action = "asset-order.received", Details = $"{order.OrderNumber} · {asset.AssetTag} · edinim maliyeti {landedUnitCost:0.00} {order.Currency}" });
                    received++;
                }
                line.QuantityReceived += qty;
            }
            var all = await db.AssetPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id).ToListAsync(ct);
            var totalReceived = all.Sum(x => x.QuantityReceived) + received; var totalOrdered = all.Sum(x => x.Quantity);
            order.Status = totalReceived == totalOrdered ? "Received" : "PartiallyReceived"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = id,
                Action = "asset-order.receipt", Details = $"{received} varlık teslim alındı; toplam {totalReceived}/{totalOrdered}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { order.Status, receivedQuantity = received, orderQuantityReceived = totalReceived, orderQuantity = totalOrdered });
        }).RequireAuthorization("asset.write");

        api.MapPost("/asset-orders/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAssetOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Received" or "Cancelled") return Results.Conflict(new { message = "Tamamlanmış veya iptal edilmiş sipariş değiştirilemez." });
            order.Status = "Cancelled"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = id,
                Action = "asset-order.cancelled", Details = order.OrderNumber });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(new { order.Status });
        }).RequireAuthorization("asset.write");

        api.MapGet("/asset-purchase-budgets/{year:int}", async (int year, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (!Actor(user, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null) return Results.Forbid();
            if (year is < 2000 or > 2100) return Results.ValidationProblem(new Dictionary<string, string[]> { ["year"] = ["Bütçe yılı 2000–2100 arasında olmalıdır."] });
            var budgets = await db.CompanyPurchaseBudgets.AsNoTracking().Where(x => x.CompanyId == companyId && x.BudgetYear == year)
                .OrderBy(x => x.Currency).Select(x => new { x.Id, x.BudgetYear, x.Currency, x.Amount, x.UpdatedAt }).ToListAsync(ct);
            var items = new List<object>(budgets.Count);
            foreach (var item in budgets)
            {
                var committed = await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db, companyId, year, item.Currency, ct);
                items.Add(new { item.Id, item.BudgetYear, item.Currency, item.Amount, committedAmount = committed,
                    remainingAmount = item.Amount - committed, overBudget = committed > item.Amount, item.UpdatedAt });
            }
            return Results.Ok(new { year, items });
        }).RequireAuthorization("asset.read");

        api.MapPut("/asset-purchase-budgets/{year:int}/{currency}", async (int year, string currency, BudgetRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null) return Results.Forbid();
            currency = currency.Trim().ToUpperInvariant();
            if (year is < 2000 or > 2100 || request.Amount is < 0 or > 1000000000000000m ||
                currency.Length != 3 || !currency.All(char.IsAsciiLetter))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["budget"] = ["2000–2100 yılı, üç harfli para birimi ve 0–1.000.000.000.000.000 arası bütçe girin."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var budget = await db.CompanyPurchaseBudgets.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.BudgetYear == year && x.Currency == currency, ct);
            var isNew = budget is null;
            if (budget is null) budget = new CompanyPurchaseBudget { CompanyId = companyId, BudgetYear = year, Currency = currency, CreatedByUserId = actorId };
            budget.Amount = request.Amount; budget.UpdatedByUserId = actorId; budget.UpdatedAt = DateTimeOffset.UtcNow;
            if (isNew) db.CompanyPurchaseBudgets.Add(budget);
            var committed = await PurchaseBudgetQueries.CommittedPurchaseAmountAsync(db, companyId, year, currency, ct);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "CompanyPurchaseBudget", TargetId = budget.Id,
                Action = isNew ? "purchase-budget.created" : "purchase-budget.updated",
                Details = $"{year} · {currency} · {request.Amount:0.00} · mevcut taahhüt {committed:0.00}" });
            try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu yıl ve para birimi için bütçe aynı anda değiştirildi; sayfayı yenileyip tekrar deneyin." }); }
            return Results.Ok(new { budget.Id, budget.BudgetYear, budget.Currency, budget.Amount, committedAmount = committed,
                remainingAmount = budget.Amount - committed, overBudget = committed > budget.Amount, budget.UpdatedAt });
        }).RequireAuthorization("settings.manage");

        api.MapPost("/asset-orders/{id:guid}/approval", async (Guid id, ApprovalDecisionRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            if (await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null) return Results.Forbid();
            var decision = request.Decision?.Trim();
            var note = Clean(request.Note);
            if (decision is not ("Approved" or "Rejected"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["decision"] = ["Karar Approved veya Rejected olmalıdır."] });
            if (note is null || note.Length is < 10 or > 2000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Karar gerekçesi 10–2.000 karakter olmalıdır."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.ApprovalStatus != "Pending") return Results.Conflict(new { message = "Yalnızca bekleyen bütçe onayları karara bağlanabilir." });
            if (order.CreatedByUserId == actorId) return Results.Conflict(new { message = "Siparişi oluşturan kullanıcı kendi bütçe istisnasını onaylayamaz veya reddedemez." });
            order.ApprovalStatus = decision; order.ReviewedByUserId = actorId; order.ReviewedAt = DateTimeOffset.UtcNow;
            order.ApprovalNote = note; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = id,
                Action = decision == "Approved" ? "asset-order.approved" : "asset-order.rejected", Details = $"{order.OrderNumber} · {note}" });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Results.Ok(new { order.Id, order.OrderNumber, order.ApprovalStatus, order.ReviewedByUserId, order.ReviewedAt, order.ApprovalNote });
        }).RequireAuthorization("settings.manage");
        return api;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool Actor(ClaimsPrincipal user, out Guid actorId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static decimal CalculateTax(decimal subtotal, decimal discount, decimal taxRatePercent) =>
        Math.Round((subtotal - discount) * taxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
    private static decimal CalculateTotal(decimal subtotal, decimal discount, decimal tax, decimal freight) => subtotal - discount + tax + freight;
    private static Dictionary<Guid, LandedUnitCostAllocation> CalculateLandedUnitCosts(IReadOnlyList<(Guid Id, int Quantity, decimal UnitCost)> lines,
        decimal discount, decimal tax, decimal freight, decimal subtotal)
    {
        var costs = new Dictionary<Guid, LandedUnitCostAllocation>(lines.Count);
        var allocatedDiscount = 0m; var allocatedTax = 0m; var allocatedFreight = 0m;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index]; var isLast = index == lines.Count - 1;
            var lineSubtotal = line.Quantity * line.UnitCost;
            var discountShare = isLast ? discount - allocatedDiscount : Allocate(discount, lineSubtotal, subtotal);
            var taxShare = isLast ? tax - allocatedTax : Allocate(tax, lineSubtotal, subtotal);
            var freightShare = isLast ? freight - allocatedFreight : Allocate(freight, lineSubtotal, subtotal);
            allocatedDiscount += discountShare; allocatedTax += taxShare; allocatedFreight += freightShare;
            var lineTotal = lineSubtotal - discountShare + taxShare + freightShare;
            var lineCents = decimal.Round(lineTotal * 100m, 0, MidpointRounding.AwayFromZero);
            var baseUnitCents = decimal.Floor(lineCents / line.Quantity);
            var extraCentUnits = checked((int)(lineCents - baseUnitCents * line.Quantity));
            costs[line.Id] = new LandedUnitCostAllocation(
                Math.Round(lineTotal / line.Quantity, 2, MidpointRounding.AwayFromZero), baseUnitCents / 100m, extraCentUnits);
        }
        return costs;

        static decimal Allocate(decimal amount, decimal lineSubtotal, decimal orderSubtotal) => orderSubtotal == 0m
            ? 0m
            : Math.Round(amount * lineSubtotal / orderSubtotal, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record LandedUnitCostAllocation(decimal AverageUnitCost, decimal BaseUnitCost, int ExtraCentUnits)
    {
        public decimal UnitCostAt(int zeroBasedUnitIndex) => BaseUnitCost + (zeroBasedUnitIndex < ExtraCentUnits ? 0.01m : 0m);
    }

    public sealed record CreateRequest(string? OrderNumber, string? Supplier, DateOnly OrderDate, DateOnly? ExpectedOn,
        decimal? TaxRatePercent, decimal? DiscountAmount, decimal? FreightAmount,
        string? Currency, string? Notes, CreateLine[]? Items);
    public sealed record CreateLine(string? Name, Guid CategoryId, Guid? BrandId, Guid? ModelId, int Quantity,
        decimal UnitCost, DateOnly? WarrantyExpiresAt, string? Notes, Guid? LocationCatalogId = null);
    public sealed record ReceiptRequest(DateOnly ReceivedOn, string? Notes, ReceiptLine[]? Items);
    public sealed record ReceiptLine(Guid LineId, int Quantity);
    public sealed record BudgetRequest(decimal Amount);
    public sealed record ApprovalDecisionRequest(string? Decision, string? Note);
}
