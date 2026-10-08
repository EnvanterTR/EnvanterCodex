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
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(status) && status is not ("Ordered" or "PartiallyReceived" or "Received" or "Cancelled"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Sipariş durumu geçersiz."] });
            var query = from order in db.AssetPurchaseOrders.AsNoTracking()
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
                    x.line.UnitCost, Total = x.line.UnitCost * x.line.Quantity, x.line.WarrantyExpiresAt, x.line.Notes })
                .ToListAsync(ct);
            static string Cell(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "";
                var trimmed = value.TrimStart();
                var formula = trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@';
                return "\"" + (formula ? "'" + value : value).Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder("Sipariş No;Tedarikçi;Durum;Sipariş Tarihi;Beklenen Tarih;Para Birimi;Varlık;Sipariş Adedi;Teslim Alınan;Kalan;Birim Bedeli;Satır Toplamı;Garanti Bitişi;Satır Notu\r\n");
            foreach (var row in rows) csv.AppendLine(string.Join(';', new[] { Cell(row.OrderNumber), Cell(row.Supplier), Cell(row.Status),
                Cell(row.OrderDate.ToString("yyyy-MM-dd")), Cell(row.ExpectedOn?.ToString("yyyy-MM-dd")), Cell(row.Currency), Cell(row.Name),
                row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), row.QuantityReceived.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture), row.UnitCost.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Cell(row.WarrantyExpiresAt?.ToString("yyyy-MM-dd")), Cell(row.Notes) }));
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", $"envantertr-varlik-siparisleri-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapGet("/asset-orders", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page, [FromQuery] int pageSize,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (from is not null && to is not null && from > to)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
            if (!string.IsNullOrWhiteSpace(status) && status is not ("Ordered" or "PartiallyReceived" or "Received" or "Cancelled"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Sipariş durumu geçersiz."] });
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
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
            var items = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency,
                    lineCount = db.AssetPurchaseOrderLines.Count(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id),
                    quantity = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.Quantity) ?? 0,
                    quantityReceived = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (int?)l.QuantityReceived) ?? 0,
                    totalCost = db.AssetPurchaseOrderLines.Where(l => l.CompanyId == companyId && l.PurchaseOrderId == x.Id).Sum(l => (decimal?)(l.Quantity * l.UnitCost)) ?? 0m
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page, pageSize });
        }).RequireAuthorization("asset.read");

        api.MapGet("/asset-orders/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var order = await db.AssetPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency, x.Notes, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            var lines = await (from line in db.AssetPurchaseOrderLines.AsNoTracking()
                join category in db.AssetCategories.AsNoTracking() on new { line.CompanyId, Id = line.CategoryId } equals new { category.CompanyId, Id = category.Id }
                where line.CompanyId == companyId && line.PurchaseOrderId == id
                orderby line.CreatedAt
                select new { line.Id, line.Name, line.CategoryId, categoryName = category.Name, line.BrandId, line.ModelId,
                    line.Quantity, line.QuantityReceived, remaining = line.Quantity - line.QuantityReceived,
                    line.UnitCost, line.WarrantyExpiresAt, line.Notes }).ToListAsync(ct);
            var receivedAssets = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.AssetTag).Select(x => new { x.Id, x.AssetTag, x.Name, x.Status, x.SerialNumber, x.PurchaseDate }).ToListAsync(ct);
            var attachments = await db.AssetPurchaseOrderAttachments.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.Length, x.CreatedAt,
                    uploadedBy = db.Users.Where(actor => actor.Id == x.UploadedByUserId).Select(actor => actor.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            return Results.Ok(new { order, lines, totalCost = lines.Sum(x => x.Quantity * x.UnitCost),
                quantity = lines.Sum(x => x.Quantity), quantityReceived = lines.Sum(x => x.QuantityReceived), assets = receivedAssets, attachments,
                maxAttachmentCount = 30, maxAttachmentSizeBytes = 10 * 1024 * 1024 });
        }).RequireAuthorization("asset.read");

        api.MapPost("/asset-orders", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var supplier = Clean(request.Supplier); var number = Clean(request.OrderNumber); var currency = request.Currency?.Trim().ToUpperInvariant() ?? "";
            var lines = request.Items ?? [];
            var errors = new Dictionary<string, string[]>();
            if (supplier is null || supplier.Length > 200) errors["supplier"] = ["Tedarikçi zorunludur ve 200 karakteri aşamaz."];
            if (number?.Length > 80) errors["orderNumber"] = ["Sipariş numarası 80 karakteri aşamaz."];
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) errors["currency"] = ["Üç harfli para birimi girin (TRY, EUR, USD)."];
            if (request.OrderDate == default || request.ExpectedOn is DateOnly expected && expected < request.OrderDate) errors["dates"] = ["Sipariş tarihi zorunludur; beklenen tarih daha önce olamaz."];
            if (request.Notes?.Length > 2000) errors["notes"] = ["Not en fazla 2.000 karakter olabilir."];
            if (lines.Length is < 1 or > 100 || lines.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Trim().Length > 200 || x.CategoryId == Guid.Empty ||
                x.BrandId == Guid.Empty || x.ModelId == Guid.Empty || x.Quantity is < 1 or > 1000 || x.UnitCost is < 0 or > 1000000000m ||
                x.WarrantyExpiresAt is DateOnly warranty && request.OrderDate != default && warranty < request.OrderDate || x.Notes?.Length > 500) ||
                lines.Sum(x => (long)x.Quantity) > 5000)
                errors["items"] = ["1–100 satır girin. Her satırda ad, kategori, 1–1.000 adet ve geçerli maliyet olmalı; toplam en fazla 5.000 varlık olabilir."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

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
            var order = new AssetPurchaseOrder { CompanyId = companyId, OrderNumber = number, Supplier = supplier!, SupplierId = supplierId,
                Status = "Ordered", OrderDate = request.OrderDate, ExpectedOn = request.ExpectedOn, Currency = currency,
                Notes = Clean(request.Notes), CreatedByUserId = actorId };
            db.AssetPurchaseOrders.Add(order);
            foreach (var line in lines) db.AssetPurchaseOrderLines.Add(new AssetPurchaseOrderLine { CompanyId = companyId, PurchaseOrderId = order.Id,
                Name = line.Name!.Trim(), CategoryId = line.CategoryId, BrandId = line.BrandId, ModelId = line.ModelId,
                Quantity = line.Quantity, UnitCost = line.UnitCost, WarrantyExpiresAt = line.WarrantyExpiresAt, Notes = Clean(line.Notes) });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = order.Id,
                Action = "asset-order.created", Details = $"{order.OrderNumber} · {lines.Length} kalem · {lines.Sum(x => x.Quantity)} adet" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu sipariş numarası şirkette zaten kullanılıyor." }); }
            return Results.Created($"/api/asset-orders/{order.Id}", new { order.Id, order.OrderNumber });
        }).RequireAuthorization("asset.write");

        api.MapPost("/asset-orders/{id:guid}/receive", async (Guid id, ReceiptRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 1000) ||
                items.Select(x => x.LineId).Distinct().Count() != items.Length || request.ReceivedOn == default || request.Notes?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Teslim tarihi ve en fazla 100 farklı satır için 1–1.000 arası adet girin."] });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Cancelled" or "Received") return Results.Conflict(new { message = "Bu sipariş artık teslim alınamaz." });
            if (request.ReceivedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim tarihi sipariş tarihinden önce olamaz."] });
            var lineIds = items.Select(x => x.LineId).ToArray();
            var lines = await db.AssetPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırlarından biri bulunamadı; varlık eklenmedi." });
            foreach (var line in lines)
            {
                var qty = items.Single(x => x.LineId == line.Id).Quantity;
                if (qty > line.Quantity - line.QuantityReceived) return Results.Conflict(new { message = "Teslim alınan miktar kalan sipariş miktarını aşamaz; hiçbir varlık eklenmedi." });
            }
            var note = Clean(request.Notes); var received = 0;
            foreach (var line in lines)
            {
                var qty = items.Single(x => x.LineId == line.Id).Quantity;
                for (var index = 0; index < qty; index++)
                {
                    var asset = new Asset { CompanyId = companyId, PurchaseOrderId = id, AssetTag = $"AT-{request.ReceivedOn:yyMMdd}-{Guid.NewGuid():N}"[..23].ToUpperInvariant(),
                        Name = line.Name, CategoryId = line.CategoryId, BrandId = line.BrandId, ModelId = line.ModelId,
                        PurchaseDate = request.ReceivedOn, WarrantyExpiresAt = line.WarrantyExpiresAt, PurchaseCost = line.UnitCost, Currency = order.Currency };
                    db.Assets.Add(asset);
                    var details = $"Satın alma siparişi {order.OrderNumber} · {order.Supplier}" + (note is null ? "" : $" · {note}");
                    db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                        EventType = "Purchased", Details = details });
                    db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Asset", TargetId = asset.Id,
                        Action = "asset-order.received", Details = $"{order.OrderNumber} · {asset.AssetTag}" });
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
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated($"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Received" or "Cancelled") return Results.Conflict(new { message = "Tamamlanmış veya iptal edilmiş sipariş değiştirilemez." });
            order.Status = "Cancelled"; order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = id,
                Action = "asset-order.cancelled", Details = order.OrderNumber });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Results.Ok(new { order.Status });
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool Actor(ClaimsPrincipal user, out Guid actorId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record CreateRequest(string? OrderNumber, string? Supplier, DateOnly OrderDate, DateOnly? ExpectedOn,
        string? Currency, string? Notes, CreateLine[]? Items);
    public sealed record CreateLine(string? Name, Guid CategoryId, Guid? BrandId, Guid? ModelId, int Quantity,
        decimal UnitCost, DateOnly? WarrantyExpiresAt, string? Notes);
    public sealed record ReceiptRequest(DateOnly ReceivedOn, string? Notes, ReceiptLine[]? Items);
    public sealed record ReceiptLine(Guid LineId, int Quantity);
}
