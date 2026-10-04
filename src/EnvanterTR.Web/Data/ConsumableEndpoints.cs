using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class ConsumableEndpoints
{
    public static RouteGroupBuilder MapConsumableEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/consumables", async ([FromQuery] string? q, [FromQuery] string? stock,
            [FromQuery] string? sortBy, [FromQuery] string? sortDirection, [FromQuery] int page,
            [FromQuery] int pageSize, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 200);
            var query = db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
                query = query.Where(x => x.Name.Contains(term) || (x.Category != null && x.Category.Contains(term)) ||
                    (x.ModelNumber != null && x.ModelNumber.Contains(term)) || (x.ItemNumber != null && x.ItemNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.Contains(term)) || (x.Location != null && x.Location.Contains(term)));
            }
            // Stock filtering and values use correlated aggregates so each tenant's rows stay isolated.
            if (stock == "low") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) <= x.MinimumQuantity);
            if (stock == "available") query = query.Where(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0) > 0);
            var sort = (sortBy ?? "name").ToLowerInvariant(); var desc = (sortDirection ?? "asc").ToLowerInvariant() == "desc";
            IOrderedQueryable<Consumable>? ordered = (sort, desc) switch
            {
                ("name", false) => query.OrderBy(x => x.Name), ("name", true) => query.OrderByDescending(x => x.Name),
                ("remaining", false) => query.OrderBy(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0)),
                ("remaining", true) => query.OrderByDescending(x => x.TotalQuantity - (db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0)),
                ("updated", false) => query.OrderBy(x => x.UpdatedAt), ("updated", true) => query.OrderByDescending(x => x.UpdatedAt), _ => null
            };
            if (ordered is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["sortBy"] = ["Sıralama alanı desteklenmiyor."] });
            var total = await query.CountAsync(ct);
            var items = await ordered.ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                x.MinimumQuantity, x.TotalQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.Notes,
                x.IsRequestable, x.UpdatedAt,
                issued = db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0
            }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        }).RequireAuthorization("asset.read");

        api.MapGet("/consumables/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var item = await db.Consumables.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.Name, x.Category, x.ModelNumber, x.ItemNumber, x.Supplier, x.Location,
                    x.MinimumQuantity, x.TotalQuantity, x.UnitCost, x.Currency, x.PurchaseDate, x.Notes, x.IsRequestable, x.UpdatedAt,
                    issued = db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == x.Id && m.MovementType == "Checkout").Sum(m => (int?)m.Quantity) ?? 0 })
                .SingleOrDefaultAsync(ct);
            if (item is null) return Results.NotFound();
            var movements = await db.ConsumableMovements.AsNoTracking().Where(m => m.CompanyId == companyId && m.ConsumableId == id)
                .OrderByDescending(m => m.OccurredAt).Take(100).Select(m => new { m.Id, m.MovementType, m.Quantity, m.Notes, m.OccurredAt,
                    employee = db.Employees.Where(e => e.CompanyId == companyId && e.Id == m.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    actor = db.Users.Where(u => u.Id == m.ActorUserId).Select(u => u.DisplayName).FirstOrDefault() }).ToListAsync(ct);
            return Results.Ok(new { item, movements });
        }).RequireAuthorization("asset.read");

        api.MapPost("/consumables", async (CreateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var errors = Validate(request.Name, request.MinimumQuantity, request.Quantity, request.UnitCost, request.Currency,
                request.Category, request.ModelNumber, request.ItemNumber, request.Supplier, request.Location, request.Notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var item = new Consumable { CompanyId = companyId, Name = request.Name.Trim(), Category = Clean(request.Category),
                ModelNumber = Clean(request.ModelNumber), ItemNumber = Clean(request.ItemNumber), Supplier = Clean(request.Supplier),
                Location = Clean(request.Location), MinimumQuantity = request.MinimumQuantity, TotalQuantity = request.Quantity,
                UnitCost = request.UnitCost, Currency = request.Currency.Trim().ToUpperInvariant(), PurchaseDate = request.PurchaseDate,
                Notes = Clean(request.Notes), IsRequestable = request.IsRequestable };
            db.Consumables.Add(item);
            if (request.Quantity > 0) db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = item.Id,
                ActorUserId = UserId(user), MovementType = "Received", Quantity = request.Quantity, Notes = "İlk stok kaydı" });
            AddAudit(db, companyId, user, item.Id, "consumable.created", item.Name);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/consumables/{item.Id}", new { item.Id });
        }).RequireAuthorization("asset.write");

        api.MapPut("/consumables/{id:guid}", async (Guid id, UpdateRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            var item = await db.Consumables.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            var errors = Validate(request.Name, request.MinimumQuantity, item.TotalQuantity, request.UnitCost, request.Currency,
                request.Category, request.ModelNumber, request.ItemNumber, request.Supplier, request.Location, request.Notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            item.Name = request.Name.Trim(); item.Category = Clean(request.Category); item.ModelNumber = Clean(request.ModelNumber);
            item.ItemNumber = Clean(request.ItemNumber); item.Supplier = Clean(request.Supplier); item.Location = Clean(request.Location);
            item.MinimumQuantity = request.MinimumQuantity; item.UnitCost = request.UnitCost; item.Currency = request.Currency.Trim().ToUpperInvariant();
            item.PurchaseDate = request.PurchaseDate; item.Notes = Clean(request.Notes); item.IsRequestable = request.IsRequestable; item.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, companyId, user, item.Id, "consumable.updated", item.Name);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/consumables/{id:guid}/stock", async (Guid id, StockRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is 0 or < -100000 or > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Stok değişimi sıfır dışında, -100.000 ile 100.000 arasında olmalı."] });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var item = await db.Consumables.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            var consumed = await db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == id && m.MovementType == "Checkout").SumAsync(m => (int?)m.Quantity, ct) ?? 0;
            var newTotal = item.TotalQuantity + request.Quantity;
            if (newTotal < consumed || newTotal > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = [$"Yeni toplam stok tüketilmiş miktardan ({consumed}) az veya 100.000'den fazla olamaz."] });
            item.TotalQuantity = newTotal; item.UpdatedAt = DateTimeOffset.UtcNow;
            db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = id, ActorUserId = UserId(user),
                MovementType = request.Quantity > 0 ? "Received" : "Adjustment", Quantity = Math.Abs(request.Quantity), Notes = Clean(request.Notes) });
            AddAudit(db, companyId, user, id, "consumable.stock-adjusted", $"{request.Quantity:+#;-#;0} adet");
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.Ok(new { totalQuantity = item.TotalQuantity, remaining = item.TotalQuantity - consumed });
        }).RequireAuthorization("asset.write");

        api.MapPost("/consumables/{id:guid}/checkout", async (Guid id, CheckoutRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId)) return Results.Forbid();
            if (request.Quantity is < 1 or > 100000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Tüketim adedi 1 ile 100.000 arasında olmalı."] });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var item = await db.Consumables.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (item is null) return Results.NotFound();
            if (!await db.Employees.AnyAsync(e => e.CompanyId == companyId && e.Id == request.EmployeeId && e.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Aktif çalışan seçilmelidir."] });
            var consumed = await db.ConsumableMovements.Where(m => m.CompanyId == companyId && m.ConsumableId == id && m.MovementType == "Checkout").SumAsync(m => (int?)m.Quantity, ct) ?? 0;
            var remaining = item.TotalQuantity - consumed;
            if (request.Quantity > remaining) return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = [$"Yeterli stok yok. Kullanılabilir miktar: {remaining}."] });
            db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = companyId, ConsumableId = id, EmployeeId = request.EmployeeId,
                ActorUserId = UserId(user), MovementType = "Checkout", Quantity = request.Quantity, Notes = Clean(request.Notes) });
            AddAudit(db, companyId, user, id, "consumable.checked-out", $"{request.Quantity} adet; çalışan {request.EmployeeId}");
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.Ok(new { remaining = remaining - request.Quantity });
        }).RequireAuthorization("asset.write");

        return api;
    }

    private static bool Tenant(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue("company_id"), out id);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void AddAudit(AppDbContext db, Guid companyId, ClaimsPrincipal user, Guid id, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = UserId(user), TargetType = "Consumable", TargetId = id, Action = action, Details = details });

    private static Dictionary<string, string[]> Validate(string name, int minimum, int total, decimal? cost, string currency,
        string? category, string? model, string? item, string? supplier, string? location, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string key, string message) => errors[key] = [message];
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) Add("name", "Ad zorunludur ve 200 karakteri aşamaz.");
        if (minimum is < 0 or > 100000) Add("minimumQuantity", "Minimum miktar 0 ile 100.000 arasında olmalı.");
        if (total is < 0 or > 100000) Add("quantity", "Toplam stok 0 ile 100.000 arasında olmalı.");
        if (cost < 0) Add("unitCost", "Birim maliyeti negatif olamaz.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || currency.Any(c => !char.IsAsciiLetter(c))) Add("currency", "Para birimi üç harfli kod olmalı.");
        if (category?.Length > 120) Add("category", "Kategori 120 karakteri aşamaz."); if (model?.Length > 120) Add("modelNumber", "Model 120 karakteri aşamaz.");
        if (item?.Length > 120) Add("itemNumber", "Ürün numarası 120 karakteri aşamaz."); if (supplier?.Length > 200) Add("supplier", "Tedarikçi 200 karakteri aşamaz.");
        if (location?.Length > 160) Add("location", "Konum 160 karakteri aşamaz."); if (notes?.Length > 2000) Add("notes", "Notlar 2.000 karakteri aşamaz.");
        return errors;
    }
    public sealed record CreateRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber, string? Supplier,
        string? Location, int MinimumQuantity, int Quantity, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable);
    public sealed record UpdateRequest(string Name, string? Category, string? ModelNumber, string? ItemNumber, string? Supplier,
        string? Location, int MinimumQuantity, decimal? UnitCost, string Currency, DateOnly? PurchaseDate, string? Notes, bool IsRequestable);
    public sealed record StockRequest(int Quantity, string? Notes);
    public sealed record CheckoutRequest(Guid EmployeeId, int Quantity, string? Notes);
}
