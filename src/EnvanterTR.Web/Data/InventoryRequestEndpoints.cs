using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class InventoryRequestEndpoints
{
    public static RouteGroupBuilder MapInventoryRequestEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/requests/employees", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var term = q?.Trim();
            if (term?.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            var userId = UserId(user);
            var selfEmployee = userId is Guid accountId
                ? await (from membership in db.CompanyMemberships.AsNoTracking()
                         join employee in db.Employees.AsNoTracking() on new { membership.CompanyId, Id = membership.EmployeeId }
                             equals new { employee.CompanyId, Id = (Guid?)employee.Id }
                         where membership.CompanyId == companyId && membership.UserId == accountId
                         select new { employee.Id, employee.DisplayName, employee.EmployeeNumber, employee.IsActive }).SingleOrDefaultAsync(ct)
                : null;
            var employees = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
            if (!CanReviewRequests(user))
            {
                employees = selfEmployee is { IsActive: true }
                    ? employees.Where(x => x.Id == selfEmployee.Id)
                    : employees.Where(_ => false);
            }
            if (!string.IsNullOrWhiteSpace(term)) employees = employees.Where(x => x.DisplayName.Contains(term) || x.EmployeeNumber.Contains(term));
            var items = await employees.OrderBy(x => x.DisplayName).Take(200)
                .Select(x => new { x.Id, x.DisplayName, x.EmployeeNumber }).ToListAsync(ct);
            return Results.Ok(new { items, selfEmployee });
        }).RequireAuthorization("asset.read");

        api.MapGet("/requests/catalog", async ([FromQuery] string? q, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var term = q?.Trim();
            if (term?.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            var accessories = db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsRequestable && x.TotalQuantity >
                (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0));
            var consumables = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsRequestable && x.TotalQuantity >
                (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0));
            var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsRequestable &&
                x.Status == AssetStatuses.Available && x.AssignedEmployeeId == null);
            if (!string.IsNullOrWhiteSpace(term))
            {
                accessories = accessories.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)));
                consumables = consumables.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)));
                assets = assets.Where(x => x.Name.Contains(term) || x.AssetTag.Contains(term) ||
                    (x.SerialNumber != null && x.SerialNumber.Contains(term)));
            }
            var accessoryItems = await accessories.OrderBy(x => x.Name).Take(200).Select(x => new RequestCatalogItem(x.Id, "Accessory", x.Name, x.Category, x.ModelNumber,
                x.Location, x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0))).ToListAsync(ct);
            var consumableItems = await consumables.OrderBy(x => x.Name).Take(200).Select(x => new RequestCatalogItem(x.Id, "Consumable", x.Name, x.Category, x.ModelNumber,
                x.Location, x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0))).ToListAsync(ct);
            var assetItems = await assets.OrderBy(x => x.Name).Take(200).Select(x => new RequestCatalogItem(x.Id, "Asset", x.Name,
                db.AssetCategories.Where(c => c.CompanyId == companyId && c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault(), x.AssetTag,
                x.Location, 1)).ToListAsync(ct);
            var kitsQuery = db.AccessoryKits.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive &&
                db.AccessoryKitLines.Any(line => line.CompanyId == companyId && line.AccessoryKitId == x.Id));
            if (!string.IsNullOrWhiteSpace(term)) kitsQuery = kitsQuery.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
            var kits = await kitsQuery.OrderBy(x => x.Name).Take(200).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var kitAvailability = await AvailableKitsAsync(db, companyId, kits.Select(x => x.Id).ToArray(), ct);
            var kitItems = kits.Where(x => kitAvailability.ContainsKey(x.Id))
                .Select(x => new RequestCatalogItem(x.Id, "AccessoryKit", x.Name, "Hazır kit", null, null, kitAvailability[x.Id]));
            var items = accessoryItems.Concat(consumableItems).Concat(assetItems).Concat(kitItems).Where(x => x.Available > 0)
                .OrderBy(x => x.Name).ThenBy(x => x.Type).Take(200).ToList();
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapGet("/requests", async ([FromQuery] string? q, [FromQuery] string? status, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!CanReviewRequests(user)) query = query.Where(x => x.RequestedByUserId == userId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim();
                if (normalized is not ("Pending" or "Approved" or "Rejected" or "Cancelled"))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Talep durumu geçerli değil."] });
                query = query.Where(x => x.Status == normalized);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.EmployeeId &&
                    (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    (x.RequestNote != null && x.RequestNote.Contains(term)) ||
                    (x.ItemType == "Accessory" && db.Accessories.Any(a => a.CompanyId == companyId && a.Id == x.ItemId && a.Name.Contains(term))) ||
                    (x.ItemType == "Consumable" && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == x.ItemId && c.Name.Contains(term))) ||
                    (x.ItemType == "AccessoryKit" && db.AccessoryKits.Any(k => k.CompanyId == companyId && k.Id == x.ItemId && k.Name.Contains(term))) ||
                    (x.ItemType == "Asset" && db.Assets.Any(a => a.CompanyId == companyId && a.Id == x.ItemId && (a.Name.Contains(term) || a.AssetTag.Contains(term)))));
            }
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.ItemType, x.ItemId, x.Quantity, x.Status, x.RequestNote, x.ReviewNote, x.CreatedAt, x.ReviewedAt,
                    x.KitReturnedAt, x.KitReturnNote,
                    x.RequestedByUserId, x.ReviewedByUserId,
                    employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    item = x.ItemType == "Accessory"
                        ? db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name).FirstOrDefault()
                        : x.ItemType == "Consumable"
                            ? db.Consumables.Where(c => c.CompanyId == companyId && c.Id == x.ItemId).Select(c => c.Name).FirstOrDefault()
                            : x.ItemType == "AccessoryKit"
                                ? db.AccessoryKits.Where(k => k.CompanyId == companyId && k.Id == x.ItemId).Select(k => k.Name).FirstOrDefault()
                                : db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name + " · " + a.AssetTag).FirstOrDefault(),
                    requestedBy = db.Users.Where(u => u.Id == x.RequestedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    reviewedBy = db.Users.Where(u => u.Id == x.ReviewedByUserId).Select(u => u.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { items = rows, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/requests/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            var query = db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!CanReviewRequests(user)) query = query.Where(x => x.RequestedByUserId == userId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim();
                if (normalized is not ("Pending" or "Approved" or "Rejected" or "Cancelled"))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Talep durumu geçerli değil."] });
                query = query.Where(x => x.Status == normalized);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.EmployeeId &&
                    (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    (x.RequestNote != null && x.RequestNote.Contains(term)) ||
                    (x.ItemType == "Accessory" && db.Accessories.Any(a => a.CompanyId == companyId && a.Id == x.ItemId && a.Name.Contains(term))) ||
                    (x.ItemType == "Consumable" && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == x.ItemId && c.Name.Contains(term))) ||
                    (x.ItemType == "AccessoryKit" && db.AccessoryKits.Any(k => k.CompanyId == companyId && k.Id == x.ItemId && k.Name.Contains(term))) ||
                    (x.ItemType == "Asset" && db.Assets.Any(a => a.CompanyId == companyId && a.Id == x.ItemId && (a.Name.Contains(term) || a.AssetTag.Contains(term)))));
            }
            if (await query.CountAsync(ct) > 10000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım en fazla 10.000 talep içerir; aramayı veya durumu daraltın."] });
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new
            {
                x.ItemType, x.Quantity, x.Status, x.RequestNote, x.ReviewNote, x.CreatedAt, x.ReviewedAt, x.KitReturnedAt, x.KitReturnNote,
                employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                item = x.ItemType == "Accessory"
                    ? db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name).FirstOrDefault()
                    : x.ItemType == "Consumable"
                        ? db.Consumables.Where(c => c.CompanyId == companyId && c.Id == x.ItemId).Select(c => c.Name).FirstOrDefault()
                        : x.ItemType == "AccessoryKit"
                            ? db.AccessoryKits.Where(k => k.CompanyId == companyId && k.Id == x.ItemId).Select(k => k.Name).FirstOrDefault()
                            : db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name + " · " + a.AssetTag).FirstOrDefault(),
                requestedBy = db.Users.Where(u => u.Id == x.RequestedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                reviewedBy = db.Users.Where(u => u.Id == x.ReviewedByUserId).Select(u => u.DisplayName).FirstOrDefault()
            }).ToListAsync(ct);
            var csv = new System.Text.StringBuilder("\uFEFF\"Çalışan\",\"Çalışan numarası\",\"Kalem\",\"Tür\",\"Adet\",\"Durum\",\"Talep eden\",\"Talep tarihi (UTC)\",\"Talep notu\",\"Kararı veren\",\"Karar tarihi (UTC)\",\"Karar notu\"\r\n");
            foreach (var row in rows)
            {
                var cells = new[]
                {
                    CsvCell(row.employee), CsvCell(row.employeeNumber), CsvCell(row.item),
                    CsvCell(row.ItemType switch { "Accessory" => "Aksesuar", "Consumable" => "Sarf", "Asset" => "Varlık", "AccessoryKit" => "Hazır kit", _ => row.ItemType }), CsvCell(row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.Status switch { "Pending" => "Bekliyor", "Approved" => "Onaylandı", "Rejected" => "Reddedildi", "Cancelled" => "İptal edildi", _ => row.Status }),
                    CsvCell(row.requestedBy), CsvCell(row.CreatedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
                    CsvCell(row.RequestNote), CsvCell(row.reviewedBy),
                    CsvCell(row.ReviewedAt?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)), CsvCell(row.ReviewNote)
                };
                csv.AppendJoin(',', cells).Append("\r\n");
            }
            return Results.File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"envantertr-stok-talepleri-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization("asset.read");

        api.MapPost("/requests", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            if (request.Quantity is < 1 or > 100000 || request.Note?.Length > 1000 || request.ItemType is not ("Accessory" or "Consumable" or "Asset" or "AccessoryKit") ||
                (request.ItemType == "Asset" && request.Quantity != 1))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Talep türü, adet veya not bilgisi geçersiz. Adet 1–100.000; not en fazla 1.000 karakter olmalıdır."] });
            var linkedEmployeeId = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId).Select(x => x.EmployeeId).SingleOrDefaultAsync(ct);
            if (linkedEmployeeId is null && !CanReviewRequests(user))
                return Results.Conflict(new { message = "Talep göndermeden önce şirket yöneticisi hesabınızı çalışan profiline bağlamalı." });
            if (linkedEmployeeId is Guid mappedEmployeeId && request.EmployeeId != mappedEmployeeId)
                return Results.Forbid();
            var employeeId = linkedEmployeeId ?? request.EmployeeId;
            if (!await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == employeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Bu şirketteki etkin bir çalışan seçilmelidir."] });
            var available = await AvailableAsync(db, companyId, request.ItemType, request.ItemId, ct);
            if (available is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["itemId"] = ["İstenen kalem bu şirkette bulunamadı veya talebe açık değil."] });
            if (request.Quantity > available) return Results.Conflict(new { message = $"Şu anda yalnızca {available} adet kullanılabilir. Onay sırasında stok yeniden kontrol edilir." });
            var row = new InventoryRequest { CompanyId = companyId, EmployeeId = employeeId, RequestedByUserId = userId,
                ItemType = request.ItemType, ItemId = request.ItemId, Quantity = request.Quantity, RequestNote = Clean(request.Note) };
            db.InventoryRequests.Add(row);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "InventoryRequest", TargetId = row.Id,
                Action = "request.created", Details = $"{request.ItemType}; {request.Quantity} adet; çalışan {employeeId}." });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/requests/{row.Id}", new { row.Id, row.Status });
        }).RequireAuthorization("asset.read");

        api.MapPost("/requests/{id:guid}/decision", async (Guid id, DecisionRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            if (request.Note?.Length > 1000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Karar notu en fazla 1.000 karakter olmalıdır."] });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var row = await db.InventoryRequests.FromSqlInterpolated(
                $"SELECT * FROM \"InventoryRequests\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (row is null) return Results.NotFound();
            if (row.Status != "Pending") return Results.Conflict(new { message = "Bu talep daha önce sonuçlandırılmış." });
            if (request.Approve)
            {
                var available = row.ItemType == "Accessory"
                    ? await ApproveAccessoryAsync(db, row, companyId, userId, request.Note, ct)
                    : row.ItemType == "Consumable"
                        ? await ApproveConsumableAsync(db, row, companyId, userId, request.Note, ct)
                        : row.ItemType == "AccessoryKit"
                            ? await ApproveAccessoryKitAsync(db, row, companyId, userId, request.Note, ct)
                            : await ApproveAssetAsync(db, row, companyId, userId, request.Note, ct);
                if (available < 0) return Results.Conflict(new { message = "Kalem artık talebe açık değil veya stok kaydı bulunamadı." });
                if (row.Quantity > available) return Results.Conflict(new { message = $"Yeterli stok kalmadı. Kullanılabilir miktar: {available}. Talep açık bırakıldı." });
            }
            row.Status = request.Approve ? "Approved" : "Rejected"; row.ReviewedByUserId = userId;
            row.ReviewedAt = DateTimeOffset.UtcNow; row.ReviewNote = Clean(request.Note);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "InventoryRequest", TargetId = id,
                Action = request.Approve ? "request.approved" : "request.rejected", Details = $"{row.Quantity} adet; çalışan {row.EmployeeId}." });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(new { row.Id, row.Status, row.ReviewedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/requests/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var row = await db.InventoryRequests.FromSqlInterpolated(
                $"SELECT * FROM \"InventoryRequests\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (row is null) return Results.NotFound();
            if (row.RequestedByUserId != userId) return Results.Forbid();
            if (row.Status != "Pending") return Results.Conflict(new { message = "Yalnızca bekleyen talepler iptal edilebilir." });
            row.Status = "Cancelled"; row.ReviewedAt = DateTimeOffset.UtcNow;
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "InventoryRequest", TargetId = id,
                Action = "request.cancelled", Details = $"{row.Quantity} adet talep iptal edildi." });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.read");

        api.MapPost("/requests/{id:guid}/return-kit", async (Guid id, DecisionRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            if (request.Note?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["İade notu en fazla 1.000 karakter olabilir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var inventoryRequest = await db.InventoryRequests.FromSqlInterpolated(
                $"SELECT * FROM \"InventoryRequests\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (inventoryRequest is null) return Results.NotFound();
            if (inventoryRequest.ItemType != "AccessoryKit" || inventoryRequest.Status != "Approved")
                return Results.Conflict(new { message = "Yalnızca onaylanmış hazır kit talepleri iade alınabilir." });
            if (inventoryRequest.KitReturnedAt is not null)
                return Results.Conflict(new { message = "Bu hazır kit talebinin iadesi daha önce kaydedilmiş." });

            var fulfillment = await db.InventoryRequestKitFulfillmentLines
                .Where(x => x.CompanyId == companyId && x.InventoryRequestId == id).OrderBy(x => x.ItemType).ThenBy(x => x.ItemId).ToListAsync(ct);
            if (fulfillment.Count == 0)
                return Results.Conflict(new { message = "Bu eski kit talebinde güvenilir teslim bağlantısı yok; stok geçmişini incelemeden otomatik iade yapılamaz." });
            var returnableLines = fulfillment.Where(x => x.ItemType is "Accessory" or "Asset" or "Component").ToList();
            if (returnableLines.Count == 0)
                return Results.Conflict(new { message = "Bu kit yalnızca tüketilen sarf malzemelerinden oluşuyor; geri alınabilir kalem yok." });

            var now = DateTimeOffset.UtcNow;
            foreach (var line in returnableLines)
            {
                if (line.ReturnedAt is not null) continue;
                switch (line.ItemType)
                {
                    case "Accessory":
                    {
                        if (line.AccessoryAssignmentId is not Guid assignmentId) return Results.Conflict(new { message = "Aksesuar teslim bağlantısı eksik." });
                        var assignment = await db.AccessoryAssignments.FromSqlInterpolated(
                            $"SELECT * FROM \"AccessoryAssignments\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assignmentId} FOR UPDATE").SingleOrDefaultAsync(ct);
                        if (assignment is null || assignment.AccessoryId != line.ItemId || assignment.EmployeeId != inventoryRequest.EmployeeId || assignment.Quantity != line.Quantity)
                            return Results.Conflict(new { message = "Kit aksesuarının güncel zimmet kaydı teslimle eşleşmiyor; otomatik iade durduruldu." });
                        var remaining = assignment.Quantity - assignment.ReturnedQuantity;
                        if (remaining > 0)
                        {
                            assignment.ReturnedQuantity += remaining; assignment.LastReturnedAt = now;
                            db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = line.ItemId,
                                AssignmentId = assignment.Id, EmployeeId = inventoryRequest.EmployeeId, ActorUserId = userId,
                                MovementType = "Checkin", Quantity = remaining, Notes = Clean(request.Note) ?? "Hazır kit iadesi" });
                        }
                        break;
                    }
                    case "Component":
                    {
                        if (line.ComponentAllocationId is not Guid allocationId) return Results.Conflict(new { message = "Bileşen tahsis bağlantısı eksik." });
                        var allocation = await db.ComponentAllocations.FromSqlInterpolated(
                            $"SELECT * FROM \"ComponentAllocations\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {allocationId} FOR UPDATE").SingleOrDefaultAsync(ct);
                        if (allocation is null || allocation.ComponentId != line.ItemId || allocation.AssetId != line.AssetId || allocation.Quantity != line.Quantity)
                            return Results.Conflict(new { message = "Kit bileşeninin tahsis kaydı teslimle eşleşmiyor; otomatik iade durduruldu." });
                        var remaining = allocation.Quantity - allocation.RemovedQuantity;
                        if (remaining > 0)
                        {
                            allocation.RemovedQuantity += remaining; allocation.LastRemovedAt = now;
                            db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = line.ItemId,
                                AllocationId = allocation.Id, AssetId = allocation.AssetId, ActorUserId = userId,
                                MovementType = "Checkin", Quantity = remaining, Notes = Clean(request.Note) ?? "Hazır kit iadesi" });
                        }
                        break;
                    }
                    case "Asset":
                    {
                        if (line.AssetId is not Guid assetId || assetId != line.ItemId) return Results.Conflict(new { message = "Kit varlığı teslim bağlantısı eksik." });
                        var asset = await db.Assets.FromSqlInterpolated(
                            $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assetId} AND \"DeletedAt\" IS NULL FOR UPDATE")
                            .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
                        if (asset is null || asset.AssignedEmployeeId != inventoryRequest.EmployeeId || asset.Status != AssetStatuses.Deployed)
                            return Results.Conflict(new { message = "Kit varlığının güncel zimmeti teslim kaydıyla eşleşmiyor; otomatik iade durduruldu." });
                        asset.AssignedEmployeeId = null; asset.Status = AssetStatuses.Available; asset.UpdatedAt = now;
                        db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = assetId, ActorUserId = userId,
                            EmployeeId = inventoryRequest.EmployeeId, EventType = "Returned",
                            Details = Clean(request.Note) ?? "Hazır kit iadesi", OccurredAt = now });
                        break;
                    }
                }
                line.ReturnedAt = now;
            }

            inventoryRequest.KitReturnedAt = now;
            inventoryRequest.KitReturnNote = Clean(request.Note);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "InventoryRequest", TargetId = id,
                Action = "request.kit-returned", Details = "Hazır kitin geri alınabilir kalemleri iade edildi; sarf malzemeleri tüketilmiş olarak kaldı." });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(new { inventoryRequest.Id, inventoryRequest.KitReturnedAt });
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static async Task<int?> AvailableAsync(AppDbContext db, Guid companyId, string type, Guid itemId, CancellationToken ct)
    {
        if (type == "AccessoryKit")
        {
            if (!await db.AccessoryKits.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == itemId && x.IsActive, ct))
                return null;
            var availableKits = await AvailableKitsAsync(db, companyId, [itemId], ct);
            return availableKits.GetValueOrDefault(itemId);
        }
        if (type == "Accessory")
        {
            var item = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == itemId && x.IsRequestable)
                .Select(x => (int?)x.TotalQuantity).SingleOrDefaultAsync(ct);
            if (item is null) return null;
            var assigned = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == itemId)
                .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0;
            return Math.Max(0, item.Value - assigned);
        }
        if (type == "Asset")
            return await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == itemId && x.IsRequestable &&
                x.Status == AssetStatuses.Available && x.AssignedEmployeeId == null).Select(_ => (int?)1).SingleOrDefaultAsync(ct);
        var consumable = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == itemId && x.IsRequestable)
            .Select(x => (int?)x.TotalQuantity).SingleOrDefaultAsync(ct);
        if (consumable is null) return null;
        var issued = await db.ConsumableMovements.Where(x => x.CompanyId == companyId && x.ConsumableId == itemId && x.MovementType == "Checkout")
            .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
        return Math.Max(0, consumable.Value - issued);
    }

    private static async Task<Dictionary<Guid, int>> AvailableKitsAsync(AppDbContext db, Guid companyId, Guid[] kitIds, CancellationToken ct)
    {
        if (kitIds.Length == 0) return [];
        var lines = await db.AccessoryKitLines.AsNoTracking()
            .Where(x => x.CompanyId == companyId && kitIds.Contains(x.AccessoryKitId)).ToListAsync(ct);
        var results = new Dictionary<Guid, int>();
        foreach (var group in lines.GroupBy(x => x.AccessoryKitId))
        {
            if (group.Count() == 0 || group.Count() > 50) continue;
            var available = new List<int>(group.Count());
            foreach (var line in group)
            {
                var itemId = line.AccessoryId ?? line.ConsumableId ?? line.AssetId ?? line.ComponentId;
                if (itemId is not Guid id) { available.Clear(); break; }
                var remaining = line.ItemType switch
                {
                    "Accessory" => await AvailableAsync(db, companyId, "Accessory", id, ct),
                    "Consumable" => await AvailableAsync(db, companyId, "Consumable", id, ct),
                    "Asset" => await AvailableAsync(db, companyId, "Asset", id, ct),
                    "Component" => await AvailableComponentAsync(db, companyId, id, ct),
                    _ => null
                };
                if (remaining is null) { available.Clear(); break; }
                available.Add(remaining.Value / line.Quantity);
            }
            if (available.Count == group.Count()) results[group.Key] = available.Min();
        }
        return results;
    }

    private static async Task<int?> AvailableComponentAsync(AppDbContext db, Guid companyId, Guid id, CancellationToken ct)
    {
        var total = await db.InventoryComponents.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
            .Select(x => (int?)x.TotalQuantity).SingleOrDefaultAsync(ct);
        if (total is null) return null;
        var allocated = await db.ComponentAllocations.AsNoTracking().Where(x => x.CompanyId == companyId && x.ComponentId == id)
            .SumAsync(x => (int?)(x.Quantity - x.RemovedQuantity), ct) ?? 0;
        return Math.Max(0, total.Value - allocated);
    }

    private static async Task<int> ApproveAccessoryKitAsync(AppDbContext db, InventoryRequest request, Guid companyId, Guid actorId, string? note, CancellationToken ct)
    {
        var kit = await db.AccessoryKits.FromSqlInterpolated(
            $"SELECT * FROM \"AccessoryKits\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.ItemId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (kit is null || !kit.IsActive) return -1;
        var employee = await db.Employees.FromSqlInterpolated(
            $"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.EmployeeId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (employee is null || !employee.IsActive) return -1;
        var lines = await db.AccessoryKitLines.AsNoTracking().Where(x => x.CompanyId == companyId && x.AccessoryKitId == kit.Id)
            .OrderBy(x => x.ItemType).ThenBy(x => x.AccessoryId ?? x.ConsumableId ?? x.AssetId ?? x.ComponentId).ToListAsync(ct);
        if (lines.Count is < 1 or > 50) return -1;
        if (lines.Any(x => x.ItemType == "Component") && lines.Count(x => x.ItemType == "Asset") != 1) return -1;

        var locked = new Dictionary<(string Type, Guid Id), object>();
        foreach (var line in lines)
        {
            var itemId = line.AccessoryId ?? line.ConsumableId ?? line.AssetId ?? line.ComponentId;
            if (itemId is not Guid id) return -1;
            object? item = line.ItemType switch
            {
                "Accessory" => await db.Accessories.FromSqlInterpolated($"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct),
                "Consumable" => await db.Consumables.FromSqlInterpolated($"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct),
                "Asset" => await db.Assets.FromSqlInterpolated($"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters().SingleOrDefaultAsync(ct),
                "Component" => await db.InventoryComponents.FromSqlInterpolated($"SELECT * FROM \"InventoryComponents\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct),
                _ => null
            };
            if (item is null) return -1;
            locked[(line.ItemType, id)] = item;
        }
        var availablePerLine = new List<int>(lines.Count);
        foreach (var line in lines)
        {
            var id = (line.AccessoryId ?? line.ConsumableId ?? line.AssetId ?? line.ComponentId)!.Value;
            int? remaining = line.ItemType switch
            {
                "Accessory" when locked[(line.ItemType, id)] is Accessory item && item.IsRequestable =>
                    Math.Max(0, item.TotalQuantity - (await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == id).SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0)),
                "Consumable" when locked[(line.ItemType, id)] is Consumable item && item.IsRequestable =>
                    Math.Max(0, item.TotalQuantity - (await db.ConsumableMovements.Where(x => x.CompanyId == companyId && x.ConsumableId == id && x.MovementType == "Checkout").SumAsync(x => (int?)x.Quantity, ct) ?? 0)),
                "Asset" when locked[(line.ItemType, id)] is Asset item && item.IsRequestable && item.Status == AssetStatuses.Available && item.AssignedEmployeeId is null => 1,
                "Component" when locked[(line.ItemType, id)] is InventoryComponent item => await AvailableComponentAsync(db, companyId, id, ct),
                _ => null
            };
            if (remaining is null) return -1;
            availablePerLine.Add(remaining.Value / line.Quantity);
        }
        var available = availablePerLine.Min();
        if (request.Quantity > available) return available;

        var movementNote = Clean(note) ?? "Hazır kit talebi onayı";
        var targetAsset = lines.Where(x => x.AssetId.HasValue).Select(x => x.AssetId).SingleOrDefault();
        var fulfillmentLines = new List<InventoryRequestKitFulfillmentLine>(lines.Count);
        foreach (var line in lines)
        {
            var quantity = checked(line.Quantity * request.Quantity);
            var markerNote = $"[KIT:{kit.Id}] {kit.Name} — {movementNote}";
            var id = (line.AccessoryId ?? line.ConsumableId ?? line.AssetId ?? line.ComponentId)!.Value;
            switch (line.ItemType)
            {
                case "Accessory":
                    var assignment = new AccessoryAssignment { CompanyId = companyId, AccessoryId = id,
                        TargetType = "Employee", EmployeeId = request.EmployeeId, Quantity = quantity, Notes = markerNote };
                    db.AccessoryAssignments.Add(assignment);
                    fulfillmentLines.Add(new InventoryRequestKitFulfillmentLine { CompanyId = companyId, InventoryRequestId = request.Id,
                        ItemType = line.ItemType, ItemId = id, Quantity = quantity, AccessoryAssignmentId = assignment.Id });
                    db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = id,
                        AssignmentId = assignment.Id, EmployeeId = request.EmployeeId, ActorUserId = actorId,
                        MovementType = "Checkout", Quantity = quantity, Notes = markerNote });
                    break;
                case "Consumable":
                    fulfillmentLines.Add(new InventoryRequestKitFulfillmentLine { CompanyId = companyId, InventoryRequestId = request.Id,
                        ItemType = line.ItemType, ItemId = id, Quantity = quantity });
                    db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = id,
                        EmployeeId = request.EmployeeId, ActorUserId = actorId, MovementType = "Checkout", Quantity = quantity, Notes = markerNote });
                    break;
                case "Asset":
                    var asset = (Asset)locked[(line.ItemType, id)];
                    asset.AssignedEmployeeId = request.EmployeeId; asset.Status = AssetStatuses.Deployed; asset.UpdatedAt = DateTimeOffset.UtcNow;
                    fulfillmentLines.Add(new InventoryRequestKitFulfillmentLine { CompanyId = companyId, InventoryRequestId = request.Id,
                        ItemType = line.ItemType, ItemId = id, Quantity = quantity, AssetId = id });
                    db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = id, ActorUserId = actorId,
                        EmployeeId = request.EmployeeId, EventType = "Assigned", Details = markerNote });
                    break;
                case "Component":
                    if (targetAsset is not Guid assetId) return -1;
                    var allocation = new ComponentAllocation { CompanyId = companyId, ComponentId = id, AssetId = assetId,
                        ActorUserId = actorId, Quantity = quantity, Notes = markerNote };
                    db.ComponentAllocations.Add(allocation);
                    fulfillmentLines.Add(new InventoryRequestKitFulfillmentLine { CompanyId = companyId, InventoryRequestId = request.Id,
                        ItemType = line.ItemType, ItemId = id, Quantity = quantity, ComponentAllocationId = allocation.Id, AssetId = assetId });
                    db.ComponentMovements.Add(new ComponentMovement { CompanyId = companyId, ComponentId = id,
                        AllocationId = allocation.Id, AssetId = assetId, ActorUserId = actorId,
                        MovementType = "Checkout", Quantity = quantity, Notes = markerNote });
                    break;
            }
        }
        db.InventoryRequestKitFulfillmentLines.AddRange(fulfillmentLines);
        kit.UpdatedAt = DateTimeOffset.UtcNow;
        return available;
    }

    private static async Task<int> ApproveAccessoryAsync(AppDbContext db, InventoryRequest request, Guid companyId, Guid actorId, string? note, CancellationToken ct)
    {
        var item = await db.Accessories.FromSqlInterpolated(
            $"SELECT * FROM \"Accessories\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.ItemId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (item is null || !item.IsRequestable) return -1;
        var assigned = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == item.Id)
            .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0;
        var available = Math.Max(0, item.TotalQuantity - assigned);
        if (request.Quantity > available) return available;
        var assignment = new AccessoryAssignment { CompanyId = companyId, AccessoryId = item.Id, TargetType = "Employee",
            EmployeeId = request.EmployeeId, Quantity = request.Quantity, Notes = Clean(note) ?? "Stok talebi onayı" };
        db.AccessoryAssignments.Add(assignment);
        db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = companyId, AccessoryId = item.Id, AssignmentId = assignment.Id,
            EmployeeId = request.EmployeeId, ActorUserId = actorId, MovementType = "Checkout", Quantity = request.Quantity,
            Notes = Clean(note) ?? "Stok talebi onayı" });
        item.UpdatedAt = DateTimeOffset.UtcNow;
        return available;
    }

    private static async Task<int> ApproveConsumableAsync(AppDbContext db, InventoryRequest request, Guid companyId, Guid actorId, string? note, CancellationToken ct)
    {
        var item = await db.Consumables.FromSqlInterpolated(
            $"SELECT * FROM \"Consumables\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.ItemId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (item is null || !item.IsRequestable) return -1;
        var issued = await db.ConsumableMovements.Where(x => x.CompanyId == companyId && x.ConsumableId == item.Id && x.MovementType == "Checkout")
            .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
        var available = Math.Max(0, item.TotalQuantity - issued);
        if (request.Quantity > available) return available;
        db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id,
            EmployeeId = request.EmployeeId, ActorUserId = actorId, MovementType = "Checkout", Quantity = request.Quantity,
            Notes = Clean(note) ?? "Stok talebi onayı" });
        item.UpdatedAt = DateTimeOffset.UtcNow;
        return available;
    }

    private static async Task<int> ApproveAssetAsync(AppDbContext db, InventoryRequest request, Guid companyId, Guid actorId, string? note, CancellationToken ct)
    {
        if (request.Quantity != 1) return -1;
        var employee = await db.Employees.FromSqlInterpolated(
            $"SELECT * FROM \"Employees\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.EmployeeId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (employee is null || !employee.IsActive) return -1;
        var asset = await db.Assets.FromSqlInterpolated(
            $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.ItemId} AND \"DeletedAt\" IS NULL FOR UPDATE").IgnoreQueryFilters().SingleOrDefaultAsync(ct);
        if (asset is null || !asset.IsRequestable || asset.Status != AssetStatuses.Available || asset.AssignedEmployeeId is not null) return -1;
        asset.AssignedEmployeeId = request.EmployeeId;
        asset.Status = AssetStatuses.Deployed;
        asset.UpdatedAt = DateTimeOffset.UtcNow;
        db.AssetEvents.Add(new AssetEvent { CompanyId = companyId, AssetId = asset.Id, ActorUserId = actorId,
            EmployeeId = request.EmployeeId, EventType = "Assigned", Details = Clean(note) ?? "Varlık talebi onayı" });
        return 1;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static bool CanReviewRequests(ClaimsPrincipal user) =>
        user.FindFirstValue("company_role") is "owner" or "admin" or "asset_manager" or "asset_operator";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CsvCell(string? value)
    {
        var safe = value?.Trim() ?? "";
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
    private sealed record RequestCatalogItem(Guid Id, string Type, string Name, string? Category, string? ModelNumber, string? Location, int Available);
    public sealed record CreateRequest(Guid EmployeeId, string ItemType, Guid ItemId, int Quantity, string? Note);
    public sealed record DecisionRequest(bool Approve, string? Note);
}
