using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryPurchaseOrderEndpoints
{
    public static RouteGroupBuilder MapAccessoryPurchaseOrderEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/accessory-orders", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] int page, [FromQuery] int pageSize, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama 120 karakteri aşamaz."] });
                query = query.Where(x => x.OrderNumber.Contains(term) || x.Supplier.Contains(term) ||
                    (x.Notes != null && x.Notes.Contains(term)) || db.AccessoryPurchaseOrderLines.Any(line =>
                        line.CompanyId == companyId && line.PurchaseOrderId == x.Id &&
                        db.Accessories.Any(accessory => accessory.CompanyId == companyId && accessory.Id == line.AccessoryId && accessory.Name.Contains(term))));
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency, x.CreatedAt, x.UpdatedAt,
                    lineCount = db.AccessoryPurchaseOrderLines.Count(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id),
                    quantity = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (int?)line.Quantity) ?? 0,
                    quantityReceived = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (int?)line.QuantityReceived) ?? 0,
                    totalCost = db.AccessoryPurchaseOrderLines.Where(line => line.CompanyId == companyId && line.PurchaseOrderId == x.Id).Sum(line => (decimal?)(line.Quantity * line.UnitCost)) ?? 0m
                }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/accessory-orders/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var order = await db.AccessoryPurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.OrderNumber, x.Supplier, x.Status, x.OrderDate, x.ExpectedOn, x.Currency, x.Notes, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            var lines = await db.AccessoryPurchaseOrderLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id)
                .OrderBy(x => x.CreatedAt).Select(x => new
                {
                    x.Id, x.AccessoryId, accessoryName = db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.AccessoryId).Select(a => a.Name).FirstOrDefault(),
                    itemNumber = db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.AccessoryId).Select(a => a.ItemNumber).FirstOrDefault(),
                    x.Quantity, x.QuantityReceived, remaining = x.Quantity - x.QuantityReceived, x.UnitCost, totalCost = x.Quantity * x.UnitCost, x.Notes
                }).ToListAsync(ct);
            return Results.Ok(new { order, lines, totalCost = lines.Sum(x => x.totalCost), quantity = lines.Sum(x => x.Quantity), quantityReceived = lines.Sum(x => x.QuantityReceived) });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessory-orders", async (CreateOrderRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var lines = request.Items ?? [];
            var errors = new Dictionary<string, string[]>();
            var supplier = Clean(request.Supplier);
            var orderNumber = Clean(request.OrderNumber);
            var currency = request.Currency?.Trim().ToUpperInvariant() ?? "";
            if (supplier is null || supplier.Length > 200) errors["supplier"] = ["Tedarikçi zorunludur ve en fazla 200 karakter olabilir."];
            if (orderNumber?.Length > 80) errors["orderNumber"] = ["Sipariş numarası en fazla 80 karakter olabilir."];
            if (currency.Length != 3 || !currency.All(char.IsAsciiLetter)) errors["currency"] = ["Üç harfli para birimi girin; ör. TRY, EUR veya USD."];
            if (request.OrderDate == default) errors["orderDate"] = ["Sipariş tarihi zorunludur."];
            if (request.ExpectedOn is DateOnly expected && expected < request.OrderDate) errors["expectedOn"] = ["Beklenen teslim tarihi sipariş tarihinden önce olamaz."];
            if (request.Notes?.Length > 2000) errors["notes"] = ["Not en fazla 2.000 karakter olabilir."];
            if (lines.Length is < 1 or > 100 || lines.Any(x => x.AccessoryId == Guid.Empty || x.Quantity is < 1 or > 100000 || x.UnitCost is < 0 or > 1000000000m || x.Notes?.Length > 500) || lines.Select(x => x.AccessoryId).Distinct().Count() != lines.Length)
                errors["items"] = ["1–100 farklı aksesuar seçin; adet 1–100.000, birim fiyat 0–1.000.000.000 aralığında olmalı."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var accessoryIds = lines.Select(x => x.AccessoryId).ToArray();
            var existingIds = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && accessoryIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
            if (existingIds.Count != lines.Length) return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Aksesuarlardan biri bu şirkette bulunamadı."] });
            orderNumber ??= $"ST-{request.OrderDate:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var order = new AccessoryPurchaseOrder
            {
                CompanyId = companyId, OrderNumber = orderNumber, Supplier = supplier!, Status = "Ordered",
                OrderDate = request.OrderDate, ExpectedOn = request.ExpectedOn, Currency = currency,
                Notes = Clean(request.Notes), CreatedByUserId = actorId
            };
            db.AccessoryPurchaseOrders.Add(order);
            foreach (var line in lines)
                db.AccessoryPurchaseOrderLines.Add(new AccessoryPurchaseOrderLine
                {
                    CompanyId = companyId, PurchaseOrderId = order.Id, AccessoryId = line.AccessoryId,
                    Quantity = line.Quantity, UnitCost = line.UnitCost, Notes = Clean(line.Notes)
                });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = order.Id, Action = "accessory-order.created", Details = $"{order.OrderNumber} · {lines.Length} kalem" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu sipariş numarası şirkette zaten kullanılıyor. Başka bir numara seçin." }); }
            return Results.Created($"/api/accessory-orders/{order.Id}", new { order.Id, order.OrderNumber });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-orders/{id:guid}/receive", async (Guid id, ReceiveOrderRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            var items = request.Items ?? [];
            if (items.Length is < 1 or > 100 || items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.LineId).Distinct().Count() != items.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["1–100 farklı sipariş satırı seçin; her teslim alınan miktar 1–100.000 arasında olmalı."] });
            if (request.ReceivedOn == default || request.Notes?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim alma tarihi zorunludur; not en fazla 1.000 karakter olabilir."] });

            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (order.Status is "Cancelled" or "Received") return Results.Conflict(new { message = "Bu sipariş artık teslim alınamaz." });
            if (request.ReceivedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["receivedOn"] = ["Teslim alma tarihi sipariş tarihinden önce olamaz."] });
            var lineIds = items.Select(x => x.LineId).ToArray();
            var lines = await db.AccessoryPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırlarından biri bulunamadı; stok değişmedi." });
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
                line.QuantityReceived += quantity;
                accessory.TotalQuantity += quantity;
                accessory.Supplier = order.Supplier;
                accessory.UnitCost = line.UnitCost;
                accessory.Currency = order.Currency;
                accessory.PurchaseDate = request.ReceivedOn;
                accessory.UpdatedAt = DateTimeOffset.UtcNow;
                db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = accessory.Id, ActorUserId = actorId, MovementType = "Received", Quantity = quantity, Notes = $"Sipariş {order.OrderNumber} · teslim alma {request.ReceivedOn:yyyy-MM-dd}" + (note is null ? "" : $" · {note}") });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", TargetId = accessory.Id, Action = "accessory-order.received", Details = $"Sipariş {order.OrderNumber} · {quantity} adet" });
            }
            var allLines = await db.AccessoryPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id).ToListAsync(ct);
            var nextReceived = allLines.Sum(x => x.QuantityReceived) + items.Sum(x => x.Quantity);
            var orderedQuantity = allLines.Sum(x => x.Quantity);
            order.Status = nextReceived == orderedQuantity ? "Received" : "PartiallyReceived";
            order.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AccessoryPurchaseOrder", TargetId = id, Action = "accessory-order.receipt", Details = $"{items.Sum(x => x.Quantity)} adet teslim alındı; toplam {nextReceived}/{orderedQuantity}" });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new { order.Status, receivedQuantity = items.Sum(x => x.Quantity), orderQuantityReceived = nextReceived, orderQuantity = orderedQuantity });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-orders/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || !UserId(user, out var actorId)) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
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

    private sealed record CreateOrderRequest(string? OrderNumber, string? Supplier, DateOnly OrderDate, DateOnly? ExpectedOn, string? Currency, string? Notes, CreateOrderLine[]? Items);
    private sealed record CreateOrderLine(Guid AccessoryId, int Quantity, decimal UnitCost, string? Notes);
    private sealed record ReceiveOrderRequest(ReceiptLine[]? Items, DateOnly ReceivedOn, string? Notes);
    private sealed record ReceiptLine(Guid LineId, int Quantity);
}
