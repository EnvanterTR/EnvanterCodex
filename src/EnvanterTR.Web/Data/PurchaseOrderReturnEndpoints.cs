using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class PurchaseOrderReturnEndpoints
{
    public static RouteGroupBuilder MapPurchaseOrderReturnEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/asset-orders/{id:guid}/returns", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId)) return Results.Forbid();
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, userId, ct);
            if (!await PurchaseOrderLocationAccess.CanAccessAssetOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            var returns = await db.PurchaseOrderReturns.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.OrderType == "Asset" && x.PurchaseOrderId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.ReturnNumber, x.ReturnedOn, x.Reason, x.Notes, x.CreatedAt }).ToListAsync(ct);
            var returnIds = returns.Select(x => x.Id).ToArray();
            var lines = await db.PurchaseOrderReturnLines.AsNoTracking()
                .Where(x => x.CompanyId == companyId && returnIds.Contains(x.PurchaseOrderReturnId))
                .Select(x => new { x.PurchaseOrderReturnId, x.AssetId, x.Quantity }).ToListAsync(ct);
            var assetIds = lines.Where(x => x.AssetId.HasValue).Select(x => x.AssetId!.Value).Distinct().ToArray();
            var assetNameQuery = db.Assets.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.CompanyId == companyId && assetIds.Contains(x.Id));
            if (allowedLocations is not null)
                assetNameQuery = assetNameQuery.Where(x => x.DeletedAt == null && x.LocationCatalogId.HasValue &&
                    allowedLocations.Contains(x.LocationCatalogId.Value));
            var assetNames = await assetNameQuery
                .Select(x => new { x.Id, x.AssetTag, x.Name }).ToDictionaryAsync(x => x.Id, ct);
            return Results.Ok(returns.Select(item => new
            {
                item.Id, item.ReturnNumber, item.ReturnedOn, item.Reason, item.Notes, item.CreatedAt,
                lines = lines.Where(line => line.PurchaseOrderReturnId == item.Id).Select(line => new
                {
                    assetId = line.AssetId.HasValue && assetNames.ContainsKey(line.AssetId.Value) ? line.AssetId : null,
                    line.Quantity,
                    assetTag = line.AssetId.HasValue && assetNames.TryGetValue(line.AssetId.Value, out var assetForTag) ? assetForTag.AssetTag : null,
                    assetName = line.AssetId.HasValue && assetNames.TryGetValue(line.AssetId.Value, out var assetForName) ? assetForName.Name : null
                })
            }));
        }).RequireAuthorization("asset.read");

        api.MapPost("/asset-orders/{id:guid}/returns", async (Guid id, CreateAssetReturnRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Results.Forbid();
            var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
            var assetIds = request.AssetIds ?? [];
            if (request.ReturnedOn == default || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 200 ||
                request.Notes?.Length > 1000 || assetIds.Length is < 1 or > 100 || assetIds.Any(x => x == Guid.Empty) ||
                assetIds.Distinct().Count() != assetIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["return"] = ["İade tarihi, en fazla 200 karakterlik gerekçe ve 1–100 farklı varlık seçilmelidir; not en fazla 1.000 karakter olabilir."] });

            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var order = await db.AssetPurchaseOrders.FromSqlInterpolated(
                $"SELECT * FROM \"AssetPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (order is null) return Results.NotFound();
            if (!await PurchaseOrderLocationAccess.CanAccessAssetOrderAsync(db, companyId, id, allowedLocations, ct)) return Results.NotFound();
            if (order.Status == "Cancelled") return Results.Conflict(new { message = "İptal edilmiş siparişten varlık iade edilemez." });
            if (request.ReturnedOn < order.OrderDate)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["returnedOn"] = ["İade tarihi sipariş tarihinden önce olamaz."] });

            var assets = await db.Assets.FromSqlInterpolated(
                $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"PurchaseOrderId\" = {id} AND \"Id\" = ANY ({assetIds}) AND \"DeletedAt\" IS NULL FOR UPDATE")
                .IgnoreQueryFilters()
                .ToListAsync(ct);
            if (assets.Count != assetIds.Length)
                return Results.Conflict(new { message = "Bir veya daha fazla varlık bu şirkete ait siparişin etkin varlığı değil; iade kaydı oluşturulmadı." });
            if (assets.Any(asset => asset.PurchaseOrderLineId is null || asset.AssignedEmployeeId is not null ||
                    asset.Status == AssetStatuses.Deployed || asset.Status == AssetStatuses.ReturnedToSupplier))
                return Results.Conflict(new { message = "Yalnızca sipariş satırına bağlı ve zimmette olmayan varlıklar iade edilebilir." });
            if (await db.Assets.AnyAsync(x => x.CompanyId == companyId && x.ParentAssetId.HasValue && assetIds.Contains(x.ParentAssetId.Value), ct) ||
                await db.AssetMaintenances.AnyAsync(x => x.CompanyId == companyId && assetIds.Contains(x.AssetId) && (x.Status == "InProgress" || x.Status == "Scheduled"), ct) ||
                await db.AccessoryAssignments.AnyAsync(x => x.CompanyId == companyId && x.AssetId != null && assetIds.Contains(x.AssetId.Value) && x.Quantity > x.ReturnedQuantity, ct) ||
                await db.ComponentAllocations.AnyAsync(x => x.CompanyId == companyId && assetIds.Contains(x.AssetId) && x.Quantity > x.RemovedQuantity, ct) ||
                await db.SoftwareLicenseAssignments.AnyAsync(x => x.CompanyId == companyId && x.AssetId != null && assetIds.Contains(x.AssetId.Value) && x.RevokedAt == null, ct) ||
                await db.AssetReservations.AnyAsync(x => x.CompanyId == companyId && assetIds.Contains(x.AssetId) && (x.Status == "Pending" || x.Status == "Approved"), ct) ||
                await db.InventoryRequests.AnyAsync(x => x.CompanyId == companyId && x.ItemType == "Asset" && assetIds.Contains(x.ItemId) && x.Status == "Pending", ct))
                return Results.Conflict(new { message = "Açık zimmet, alt varlık, bakım, stok, lisans, rezervasyon veya talep ilişkisi bulunan varlık iade edilemez." });

            var lineIds = assets.Select(x => x.PurchaseOrderLineId!.Value).Distinct().ToArray();
            var orderLines = await db.AssetPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == id && lineIds.Contains(x.Id)).ToListAsync(ct);
            if (orderLines.Count != lineIds.Length)
                return Results.Conflict(new { message = "Varlıklardan birinin sipariş satırı bulunamadı; iade kaydı oluşturulmadı." });

            var now = DateTimeOffset.UtcNow;
            var returnRecord = new PurchaseOrderReturn
            {
                CompanyId = companyId, OrderType = "Asset", PurchaseOrderId = id,
                ReturnNumber = $"SR-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..23].ToUpperInvariant(),
                ReturnedOn = request.ReturnedOn, Reason = request.Reason.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(), CreatedByUserId = actorId
            };
            db.PurchaseOrderReturns.Add(returnRecord);
            foreach (var asset in assets)
            {
                var line = orderLines.Single(x => x.Id == asset.PurchaseOrderLineId);
                if (line.QuantityReturned >= line.QuantityReceived)
                    return Results.Conflict(new { message = "Sipariş satırında iade edilebilir miktar kalmadı." });
                line.QuantityReturned++;
                asset.Status = AssetStatuses.ReturnedToSupplier;
                asset.DeletedAt = now;
                asset.DeletedByUserId = actorId;
                asset.UpdatedAt = now;
                db.PurchaseOrderReturnLines.Add(new PurchaseOrderReturnLine
                    { CompanyId = companyId, PurchaseOrderReturnId = returnRecord.Id, AssetId = asset.Id, Quantity = 1 });
                db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
                    EventType = "ReturnedToSupplier", Details = $"Tedarikçiye iade · {returnRecord.ReturnNumber} · {returnRecord.Reason}" });
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "AssetPurchaseOrder", TargetId = id,
                Action = "asset-order.returned", Details = $"{returnRecord.ReturnNumber} · {assets.Count} varlık · {returnRecord.Reason}" });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "İade kaydı çakışma nedeniyle kaydedilemedi; siparişi yenileyip tekrar deneyin." }); }
            await tx.CommitAsync(ct);
            return Results.Created($"/api/asset-orders/{id}/returns/{returnRecord.Id}", new { returnRecord.Id, returnRecord.ReturnNumber, returnedAssets = assets.Count });
        }).RequireAuthorization("asset.write");

        api.MapGet("/accessory-orders/{id:guid}/returns", (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
            HistoryAsync("Accessory", id, user, db, ct)).RequireAuthorization("asset.read");
        api.MapGet("/stock-orders/{id:guid}/returns", (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
            HistoryAsync("Stock", id, user, db, ct)).RequireAuthorization("asset.read");
        api.MapPost("/accessory-orders/{id:guid}/returns", (Guid id, CreateStockReturnRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
            CreateAccessoryReturnAsync(id, request, user, db, ct)).RequireAuthorization("asset.write");
        api.MapPost("/stock-orders/{id:guid}/returns", (Guid id, CreateStockReturnRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
            CreateStockReturnAsync(id, request, user, db, ct)).RequireAuthorization("asset.write");

        return api;
    }

    private static async Task<IResult> HistoryAsync(string orderType, Guid orderId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!Scope(user, out var companyId)) return Results.Forbid();
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
        var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
        var exists = orderType switch
        {
            "Accessory" => await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, orderId, allowedLocations, ct),
            "Stock" => await PurchaseOrderLocationAccess.CanAccessStockOrderAsync(db, companyId, orderId, allowedLocations, ct),
            _ => false
        };
        if (!exists) return Results.NotFound();
        var returns = await db.PurchaseOrderReturns.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.OrderType == orderType && x.PurchaseOrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.ReturnNumber, x.ReturnedOn, x.Reason, x.Notes, x.CreatedAt }).ToListAsync(ct);
        var ids = returns.Select(x => x.Id).ToArray();
        var lines = await db.PurchaseOrderReturnLines.AsNoTracking()
            .Where(x => x.CompanyId == companyId && ids.Contains(x.PurchaseOrderReturnId))
            .Select(x => new { x.PurchaseOrderReturnId, x.PurchaseOrderLineId, x.Quantity }).ToListAsync(ct);
        return Results.Ok(returns.Select(item => new
        {
            item.Id, item.ReturnNumber, item.ReturnedOn, item.Reason, item.Notes, item.CreatedAt,
            lines = lines.Where(line => line.PurchaseOrderReturnId == item.Id)
                .Select(line => new { line.PurchaseOrderLineId, line.Quantity })
        }));
    }

    private static async Task<IResult> CreateAccessoryReturnAsync(Guid orderId, CreateStockReturnRequest request,
        ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!ReturnScope(user, request, out var companyId, out var actorId, out var reason, out var validation)) return validation!;
        var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var order = await db.AccessoryPurchaseOrders.FromSqlInterpolated(
            $"SELECT * FROM \"AccessoryPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {orderId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (order is null) return Results.NotFound();
        if (!await PurchaseOrderLocationAccess.CanAccessAccessoryOrderAsync(db, companyId, orderId, allowedLocations, ct)) return Results.NotFound();
        if (order.Status == "Cancelled") return Results.Conflict(new { message = "İptal edilmiş siparişten iade yapılamaz." });
        if (request.ReturnedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["returnedOn"] = ["İade tarihi sipariş tarihinden önce olamaz."] });
        var items = request.Items!;
        var lineIds = items.Select(x => x.LineId).ToArray();
        var lines = await db.AccessoryPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == orderId && lineIds.Contains(x.Id)).ToListAsync(ct);
        if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırı bulunamadı; stok değişmedi." });
        var accessoryIds = lines.Select(x => x.AccessoryId).Distinct().Order().ToArray();
        var accessories = await db.Accessories.FromSqlInterpolated(
            $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({accessoryIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
        if (accessories.Count != accessoryIds.Length) return Results.Conflict(new { message = "Sipariş aksesuarı bulunamadı; stok değişmedi." });
        foreach (var line in lines)
        {
            var quantity = items.Single(x => x.LineId == line.Id).Quantity;
            if (quantity > line.QuantityReceived - line.QuantityReturned)
                return Results.Conflict(new { message = "İade miktarı teslim alınmış ve daha önce iade edilmemiş miktarı aşamaz." });
        }
        foreach (var group in lines.GroupBy(x => x.AccessoryId))
        {
            var quantity = group.Sum(line => items.Single(x => x.LineId == line.Id).Quantity);
            var item = accessories.Single(x => x.Id == group.Key);
            var allocated = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == item.Id)
                .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0;
            if (item.TotalQuantity - allocated < quantity)
                return Results.Conflict(new { message = $"{item.Name} stoğunda iade için yeterli boş miktar yok; zimmetli adetler iade edilemez." });
        }
        var record = NewReturn(companyId, "Accessory", orderId, actorId, request, reason);
        db.PurchaseOrderReturns.Add(record);
        var note = ReturnNote(record, order.OrderNumber);
        foreach (var line in lines)
        {
            var quantity = items.Single(x => x.LineId == line.Id).Quantity;
            var item = accessories.Single(x => x.Id == line.AccessoryId);
            line.QuantityReturned += quantity;
            item.TotalQuantity -= quantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.PurchaseOrderReturnLines.Add(new PurchaseOrderReturnLine { CompanyId = companyId, PurchaseOrderReturnId = record.Id,
                PurchaseOrderLineId = line.Id, Quantity = quantity });
            db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = item.Id, ActorUserId = actorId,
                MovementType = "SupplierReturned", Quantity = quantity, Notes = note });
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Accessory", TargetId = item.Id,
                Action = "accessory-order.returned", Details = $"{record.ReturnNumber} · {quantity} adet · {reason}" });
        }
        AddOrderReturnAudit(db, companyId, actorId, "AccessoryPurchaseOrder", orderId, order.OrderNumber, record, items.Sum(x => x.Quantity));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Results.Created($"/api/accessory-orders/{orderId}/returns/{record.Id}", new { record.Id, record.ReturnNumber, returnedQuantity = items.Sum(x => x.Quantity) });
    }

    private static async Task<IResult> CreateStockReturnAsync(Guid orderId, CreateStockReturnRequest request,
        ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        if (!ReturnScope(user, request, out var companyId, out var actorId, out var reason, out var validation)) return validation!;
        var allowedLocations = await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var order = await db.StockPurchaseOrders.FromSqlInterpolated(
            $"SELECT * FROM \"StockPurchaseOrders\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {orderId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (order is null) return Results.NotFound();
        if (!await PurchaseOrderLocationAccess.CanAccessStockOrderAsync(db, companyId, orderId, allowedLocations, ct)) return Results.NotFound();
        if (order.Status == "Cancelled") return Results.Conflict(new { message = "İptal edilmiş siparişten iade yapılamaz." });
        if (request.ReturnedOn < order.OrderDate) return Results.ValidationProblem(new Dictionary<string, string[]> { ["returnedOn"] = ["İade tarihi sipariş tarihinden önce olamaz."] });
        var items = request.Items!;
        var lineIds = items.Select(x => x.LineId).ToArray();
        var lines = await db.StockPurchaseOrderLines.Where(x => x.CompanyId == companyId && x.PurchaseOrderId == orderId && lineIds.Contains(x.Id)).ToListAsync(ct);
        if (lines.Count != items.Length) return Results.NotFound(new { message = "Sipariş satırı bulunamadı; stok değişmedi." });
        var consumableIds = lines.Where(x => x.ConsumableId.HasValue).Select(x => x.ConsumableId!.Value).Distinct().Order().ToArray();
        var componentIds = lines.Where(x => x.ComponentId.HasValue).Select(x => x.ComponentId!.Value).Distinct().Order().ToArray();
        var consumables = consumableIds.Length == 0 ? [] : await db.Consumables.FromSqlInterpolated(
            $"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({consumableIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
        var components = componentIds.Length == 0 ? [] : await db.InventoryComponents.FromSqlInterpolated(
            $"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = ANY ({componentIds}) ORDER BY \"Id\" FOR UPDATE").ToListAsync(ct);
        if (consumables.Count != consumableIds.Length || components.Count != componentIds.Length)
            return Results.Conflict(new { message = "İade edilecek stok kaydı bulunamadı; hiçbir stok değişmedi." });
        foreach (var line in lines)
        {
            var quantity = items.Single(x => x.LineId == line.Id).Quantity;
            if (quantity > line.QuantityReceived - line.QuantityReturned)
                return Results.Conflict(new { message = "İade miktarı teslim alınmış ve daha önce iade edilmemiş miktarı aşamaz." });
        }
        foreach (var group in lines.GroupBy(x => (x.ConsumableId, x.ComponentId)))
        {
            var quantity = group.Sum(line => items.Single(x => x.LineId == line.Id).Quantity);
            var first = group.First();
            var total = first.ConsumableId is Guid consumableId ? consumables.Single(x => x.Id == consumableId).TotalQuantity : components.Single(x => x.Id == first.ComponentId).TotalQuantity;
            var allocated = first.ComponentId is Guid componentId
                ? await db.ComponentAllocations.Where(x => x.CompanyId == companyId && x.ComponentId == componentId)
                    .SumAsync(x => (int?)(x.Quantity - x.RemovedQuantity), ct) ?? 0
                : 0;
            if (total - allocated < quantity)
                return Results.Conflict(new { message = "Seçilen stokta iade edilebilir serbest miktar yetersiz; tahsisli bileşenler iade edilemez." });
        }
        var record = NewReturn(companyId, "Stock", orderId, actorId, request, reason);
        db.PurchaseOrderReturns.Add(record);
        var note = ReturnNote(record, order.OrderNumber);
        foreach (var line in lines)
        {
            var quantity = items.Single(x => x.LineId == line.Id).Quantity;
            line.QuantityReturned += quantity;
            db.PurchaseOrderReturnLines.Add(new PurchaseOrderReturnLine { CompanyId = companyId, PurchaseOrderReturnId = record.Id,
                PurchaseOrderLineId = line.Id, Quantity = quantity });
            if (line.ConsumableId is Guid consumableId)
            {
                var item = consumables.Single(x => x.Id == consumableId); item.TotalQuantity -= quantity; item.UpdatedAt = DateTimeOffset.UtcNow;
                db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id, ActorUserId = actorId,
                    MovementType = "SupplierReturned", Quantity = quantity, Notes = note });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Consumable", TargetId = item.Id,
                    Action = "stock-order.returned", Details = $"{record.ReturnNumber} · {quantity} adet · {reason}" });
            }
            else
            {
                var item = components.Single(x => x.Id == line.ComponentId); item.TotalQuantity -= quantity; item.UpdatedAt = DateTimeOffset.UtcNow;
                db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = item.Id, ActorUserId = actorId,
                    MovementType = "SupplierReturned", Quantity = quantity, Notes = note });
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "InventoryComponent", TargetId = item.Id,
                    Action = "stock-order.returned", Details = $"{record.ReturnNumber} · {quantity} adet · {reason}" });
            }
        }
        AddOrderReturnAudit(db, companyId, actorId, "StockPurchaseOrder", orderId, order.OrderNumber, record, items.Sum(x => x.Quantity));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Results.Created($"/api/stock-orders/{orderId}/returns/{record.Id}", new { record.Id, record.ReturnNumber, returnedQuantity = items.Sum(x => x.Quantity) });
    }

    private static bool ReturnScope(ClaimsPrincipal user, CreateStockReturnRequest request, out Guid companyId, out Guid actorId,
        out string reason, out IResult? error)
    {
        companyId = Guid.Empty; actorId = Guid.Empty; reason = request.Reason?.Trim() ?? ""; error = null;
        if (!Scope(user, out companyId) || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out actorId))
        { error = Results.Forbid(); return false; }
        var items = request.Items ?? [];
        if (request.ReturnedOn == default || reason.Length is < 1 or > 200 || request.Notes?.Length > 1000 || items.Length is < 1 or > 100 ||
            items.Any(x => x.LineId == Guid.Empty || x.Quantity is < 1 or > 100000) || items.Select(x => x.LineId).Distinct().Count() != items.Length)
        { error = Results.ValidationProblem(new Dictionary<string, string[]> { ["return"] = ["Tarih, gerekçe (1–200 karakter) ve 1–100 farklı sipariş satırı için 1–100.000 arası miktar girin; not en fazla 1.000 karakter olabilir."] }); return false; }
        return true;
    }

    private static PurchaseOrderReturn NewReturn(Guid companyId, string orderType, Guid orderId, Guid actorId,
        CreateStockReturnRequest request, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        return new PurchaseOrderReturn { CompanyId = companyId, OrderType = orderType, PurchaseOrderId = orderId,
            ReturnNumber = $"SR-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..23].ToUpperInvariant(), ReturnedOn = request.ReturnedOn,
            Reason = reason, Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(), CreatedByUserId = actorId };
    }

    private static string ReturnNote(PurchaseOrderReturn record, string orderNumber) =>
        $"Tedarikçi iadesi · {orderNumber} · {record.ReturnNumber} · {record.Reason}" + (record.Notes is null ? "" : $" · {record.Notes}");

    private static void AddOrderReturnAudit(AppDbContext db, Guid companyId, Guid actorId, string type, Guid orderId,
        string orderNumber, PurchaseOrderReturn record, int quantity) => db.AuditEntries.Add(new AuditEntry
    {
        CompanyId = companyId, ActorUserId = actorId, TargetType = type, TargetId = orderId, Action = "purchase-order.returned",
        Details = $"{orderNumber} · {record.ReturnNumber} · {quantity} adet · {record.Reason}"
    });

    private static bool Scope(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    public sealed record CreateAssetReturnRequest(DateOnly ReturnedOn, string? Reason, string? Notes, Guid[]? AssetIds);
    public sealed record CreateStockReturnRequest(DateOnly ReturnedOn, string? Reason, string? Notes, ReturnQuantity[]? Items);
    public sealed record ReturnQuantity(Guid LineId, int Quantity);
}
