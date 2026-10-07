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
            var employees = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
            if (!string.IsNullOrWhiteSpace(term)) employees = employees.Where(x => x.DisplayName.Contains(term) || x.EmployeeNumber.Contains(term));
            var items = await employees.OrderBy(x => x.DisplayName).Take(200)
                .Select(x => new { x.Id, x.DisplayName, x.EmployeeNumber }).ToListAsync(ct);
            return Results.Ok(new { items });
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
            if (!string.IsNullOrWhiteSpace(term))
            {
                accessories = accessories.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)));
                consumables = consumables.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)));
            }
            var accessoryItems = await accessories.OrderBy(x => x.Name).Take(200).Select(x => new RequestCatalogItem(x.Id, "Accessory", x.Name, x.Category, x.ModelNumber,
                x.Location, x.TotalQuantity - (db.AccessoryAssignments.Where(a => a.CompanyId == companyId && a.AccessoryId == x.Id).Sum(a => (int?)(a.Quantity - a.ReturnedQuantity)) ?? 0))).ToListAsync(ct);
            var consumableItems = await consumables.OrderBy(x => x.Name).Take(200).Select(x => new RequestCatalogItem(x.Id, "Consumable", x.Name, x.Category, x.ModelNumber,
                x.Location, x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0))).ToListAsync(ct);
            var items = accessoryItems.Concat(consumableItems).OrderBy(x => x.Name).ThenBy(x => x.Type).Take(200).ToList();
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapGet("/requests", async ([FromQuery] string? q, [FromQuery] string? status, ClaimsPrincipal user, AppDbContext db,
            CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId);
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
                    (x.ItemType == "Consumable" && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == x.ItemId && c.Name.Contains(term))));
            }
            var total = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new
                {
                    x.Id, x.ItemType, x.ItemId, x.Quantity, x.Status, x.RequestNote, x.ReviewNote, x.CreatedAt, x.ReviewedAt,
                    x.RequestedByUserId, x.ReviewedByUserId,
                    employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                    item = x.ItemType == "Accessory"
                        ? db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name).FirstOrDefault()
                        : db.Consumables.Where(c => c.CompanyId == companyId && c.Id == x.ItemId).Select(c => c.Name).FirstOrDefault(),
                    requestedBy = db.Users.Where(u => u.Id == x.RequestedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    reviewedBy = db.Users.Where(u => u.Id == x.ReviewedByUserId).Select(u => u.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { items = rows, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/requests/export.csv", async ([FromQuery] string? q, [FromQuery] string? status,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var query = db.InventoryRequests.AsNoTracking().Where(x => x.CompanyId == companyId);
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
                    (x.ItemType == "Consumable" && db.Consumables.Any(c => c.CompanyId == companyId && c.Id == x.ItemId && c.Name.Contains(term))));
            }
            if (await query.CountAsync(ct) > 10000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = ["Dışa aktarım en fazla 10.000 talep içerir; aramayı veya durumu daraltın."] });
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new
            {
                x.ItemType, x.Quantity, x.Status, x.RequestNote, x.ReviewNote, x.CreatedAt, x.ReviewedAt,
                employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                employeeNumber = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.EmployeeNumber).FirstOrDefault(),
                item = x.ItemType == "Accessory"
                    ? db.Accessories.Where(a => a.CompanyId == companyId && a.Id == x.ItemId).Select(a => a.Name).FirstOrDefault()
                    : db.Consumables.Where(c => c.CompanyId == companyId && c.Id == x.ItemId).Select(c => c.Name).FirstOrDefault(),
                requestedBy = db.Users.Where(u => u.Id == x.RequestedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                reviewedBy = db.Users.Where(u => u.Id == x.ReviewedByUserId).Select(u => u.DisplayName).FirstOrDefault()
            }).ToListAsync(ct);
            var csv = new System.Text.StringBuilder("\uFEFF\"Çalışan\",\"Çalışan numarası\",\"Kalem\",\"Tür\",\"Adet\",\"Durum\",\"Talep eden\",\"Talep tarihi (UTC)\",\"Talep notu\",\"Kararı veren\",\"Karar tarihi (UTC)\",\"Karar notu\"\r\n");
            foreach (var row in rows)
            {
                var cells = new[]
                {
                    CsvCell(row.employee), CsvCell(row.employeeNumber), CsvCell(row.item),
                    CsvCell(row.ItemType == "Accessory" ? "Aksesuar" : "Sarf"), CsvCell(row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
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
            if (request.Quantity is < 1 or > 100000 || request.Note?.Length > 1000 || request.ItemType is not ("Accessory" or "Consumable"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Talep türü, adet veya not bilgisi geçersiz. Adet 1–100.000; not en fazla 1.000 karakter olmalıdır."] });
            if (!await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == request.EmployeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Bu şirketteki etkin bir çalışan seçilmelidir."] });
            var available = await AvailableAsync(db, companyId, request.ItemType, request.ItemId, ct);
            if (available is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["itemId"] = ["İstenen kalem bu şirkette bulunamadı veya talebe açık değil."] });
            if (request.Quantity > available) return Results.Conflict(new { message = $"Şu anda yalnızca {available} adet kullanılabilir. Onay sırasında stok yeniden kontrol edilir." });
            var row = new InventoryRequest { CompanyId = companyId, EmployeeId = request.EmployeeId, RequestedByUserId = userId,
                ItemType = request.ItemType, ItemId = request.ItemId, Quantity = request.Quantity, RequestNote = Clean(request.Note) };
            db.InventoryRequests.Add(row);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = userId, TargetType = "InventoryRequest", TargetId = row.Id,
                Action = "request.created", Details = $"{request.ItemType}; {request.Quantity} adet; çalışan {request.EmployeeId}." });
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
                    : await ApproveConsumableAsync(db, row, companyId, userId, request.Note, ct);
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

        return api;
    }

    private static async Task<int?> AvailableAsync(AppDbContext db, Guid companyId, string type, Guid itemId, CancellationToken ct)
    {
        if (type == "Accessory")
        {
            var item = await db.Accessories.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == itemId && x.IsRequestable)
                .Select(x => (int?)x.TotalQuantity).SingleOrDefaultAsync(ct);
            if (item is null) return null;
            var assigned = await db.AccessoryAssignments.Where(x => x.CompanyId == companyId && x.AccessoryId == itemId)
                .SumAsync(x => (int?)(x.Quantity - x.ReturnedQuantity), ct) ?? 0;
            return Math.Max(0, item.Value - assigned);
        }
        var consumable = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == itemId && x.IsRequestable)
            .Select(x => (int?)x.TotalQuantity).SingleOrDefaultAsync(ct);
        if (consumable is null) return null;
        var issued = await db.ConsumableMovements.Where(x => x.CompanyId == companyId && x.ConsumableId == itemId && x.MovementType == "Checkout")
            .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
        return Math.Max(0, consumable.Value - issued);
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

    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
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
