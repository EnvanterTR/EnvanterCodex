using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class SupplierCatalogEndpoints
{
    public static RouteGroupBuilder MapSupplierCatalogEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/suppliers", async ([FromQuery] string? q, [FromQuery] bool includeArchived,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId)) return Results.Forbid();
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama en fazla 120 karakter olabilir."] });
            var query = db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!includeArchived) query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => x.Name.Contains(term) || (x.ContactName != null && x.ContactName.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)) || (x.Phone != null && x.Phone.Contains(term)));
            }
            var items = await query.OrderBy(x => x.Name).Take(500).Select(x => new
            {
                x.Id, x.Name, x.ContactName, x.Email, x.Phone, x.Website, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt,
                logoUrl = x.LogoFileName == null ? null : $"/api/suppliers/{x.Id}/logo",
                orderCount = db.AccessoryPurchaseOrders.Count(o => o.CompanyId == companyId && o.SupplierId == x.Id) +
                    db.StockPurchaseOrders.Count(o => o.CompanyId == companyId && o.SupplierId == x.Id) +
                    db.AssetPurchaseOrders.Count(o => o.CompanyId == companyId && o.SupplierId == x.Id),
                accessoryCount = db.Accessories.Count(a => a.CompanyId == companyId && a.SupplierId == x.Id),
                consumableCount = db.Consumables.Count(c => c.CompanyId == companyId && c.SupplierId == x.Id),
                componentCount = db.InventoryComponents.Count(c => c.CompanyId == companyId && c.SupplierId == x.Id),
                licenseCount = db.SoftwareLicenses.Count(l => l.CompanyId == companyId && l.SupplierId == x.Id),
                maintenanceCount = db.AssetMaintenances.Count(m => m.CompanyId == companyId && m.SupplierId == x.Id)
            }).ToListAsync(ct);
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapGet("/suppliers/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId)) return Results.Forbid();
            var entry = await db.SupplierCatalogEntries.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id)
                .Select(x => new { x.Id, x.Name, x.ContactName, x.Email, x.Phone, x.Website, x.Notes, x.IsActive, x.CreatedAt, x.UpdatedAt,
                    logoUrl = x.LogoFileName == null ? null : $"/api/suppliers/{x.Id}/logo" }).SingleOrDefaultAsync(ct);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        }).RequireAuthorization("asset.read");

        api.MapPost("/suppliers", async (SupplierRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var errors = Validate(request, out var name, out var normalized, out var contact, out var email, out var phone, out var website, out var notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId && x.NormalizedName == normalized, ct))
                return Results.Conflict(new { message = "Bu tedarikçi bu şirket kataloğunda zaten kayıtlı." });
            var entry = new SupplierCatalogEntry { CompanyId = companyId, Name = name!, NormalizedName = normalized!, ContactName = contact, Email = email, Phone = phone, Website = website, Notes = notes };
            db.SupplierCatalogEntries.Add(entry);
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = entry.Id, Action = "supplier.created", Details = entry.Name });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/suppliers/{entry.Id}", new { entry.Id, entry.Name, entry.ContactName, entry.Email, entry.Phone, entry.Website, entry.Notes, entry.IsActive });
        }).RequireAuthorization("asset.write");

        api.MapPut("/suppliers/{id:guid}", async (Guid id, SupplierRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var entry = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (entry is null) return Results.NotFound();
            var errors = Validate(request, out var name, out var normalized, out var contact, out var email, out var phone, out var website, out var notes);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            if (await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.NormalizedName == normalized, ct))
                return Results.Conflict(new { message = "Bu tedarikçi adı bu şirket kataloğunda zaten kullanılıyor." });
            var before = entry.Name;
            var oldNormalized = entry.NormalizedName;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var updatedAt = DateTimeOffset.UtcNow;
            entry.Name = name!; entry.NormalizedName = normalized!; entry.ContactName = contact; entry.Email = email;
            entry.Phone = phone; entry.Website = website; entry.Notes = notes; entry.UpdatedAt = updatedAt;
            var linkedCount = 0;
            if (oldNormalized != normalized)
            {
                linkedCount += await db.Accessories.Where(x => x.CompanyId == companyId && (x.SupplierId == id || (x.SupplierId == null && x.Supplier != null && x.Supplier.Trim().ToUpper() == oldNormalized))).ExecuteUpdateAsync(x => x.SetProperty(y => y.Supplier, name).SetProperty(y => y.SupplierId, (Guid?)id).SetProperty(y => y.UpdatedAt, updatedAt), ct);
                linkedCount += await db.Consumables.Where(x => x.CompanyId == companyId && (x.SupplierId == id || (x.SupplierId == null && x.Supplier != null && x.Supplier.Trim().ToUpper() == oldNormalized))).ExecuteUpdateAsync(x => x.SetProperty(y => y.Supplier, name).SetProperty(y => y.SupplierId, (Guid?)id).SetProperty(y => y.UpdatedAt, updatedAt), ct);
                linkedCount += await db.InventoryComponents.Where(x => x.CompanyId == companyId && (x.SupplierId == id || (x.SupplierId == null && x.Supplier != null && x.Supplier.Trim().ToUpper() == oldNormalized))).ExecuteUpdateAsync(x => x.SetProperty(y => y.Supplier, name).SetProperty(y => y.SupplierId, (Guid?)id).SetProperty(y => y.UpdatedAt, updatedAt), ct);
                linkedCount += await db.SoftwareLicenses.Where(x => x.CompanyId == companyId && (x.SupplierId == id || (x.SupplierId == null && x.Supplier != null && x.Supplier.Trim().ToUpper() == oldNormalized))).ExecuteUpdateAsync(x => x.SetProperty(y => y.Supplier, name).SetProperty(y => y.SupplierId, (Guid?)id).SetProperty(y => y.UpdatedAt, updatedAt), ct);
                linkedCount += await db.AssetMaintenances.Where(x => x.CompanyId == companyId && (x.SupplierId == id || (x.SupplierId == null && x.Supplier != null && x.Supplier.Trim().ToUpper() == oldNormalized))).ExecuteUpdateAsync(x => x.SetProperty(y => y.Supplier, name).SetProperty(y => y.SupplierId, (Guid?)id).SetProperty(y => y.UpdatedAt, updatedAt), ct);
            }
            db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = id, Action = "supplier.updated", Details = $"{before} → {entry.Name}; bağlı kayıt güncellendi: {linkedCount}" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { entry.Id, entry.Name, entry.ContactName, entry.Email, entry.Phone, entry.Website, entry.Notes, entry.IsActive, entry.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/suppliers/{id:guid}/archive", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var entry = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (entry is null) return Results.NotFound();
            if (entry.IsActive)
            {
                entry.IsActive = false; entry.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = id, Action = "supplier.archived", Details = entry.Name });
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");

        api.MapPost("/suppliers/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Scope(user, out var companyId) || !Actor(user, out var actorId)) return Results.Forbid();
            var entry = await db.SupplierCatalogEntries.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (entry is null) return Results.NotFound();
            if (!entry.IsActive && await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.IsActive && x.NormalizedName == entry.NormalizedName, ct))
                return Results.Conflict(new { message = "Aynı adlı etkin bir tedarikçi bulunduğu için bu kayıt geri alınamıyor. Önce etkin kaydı arşivleyin veya yeniden adlandırın." });
            if (!entry.IsActive)
            {
                entry.IsActive = true; entry.UpdatedAt = DateTimeOffset.UtcNow;
                db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Supplier", TargetId = id, Action = "supplier.restored", Details = entry.Name });
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        }).RequireAuthorization("asset.write");
        return api;
    }

    private static Dictionary<string, string[]> Validate(SupplierRequest request, out string? name, out string? normalized,
        out string? contact, out string? email, out string? phone, out string? website, out string? notes)
    {
        name = Clean(request.Name); normalized = name?.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        contact = Clean(request.ContactName); email = Clean(request.Email); phone = Clean(request.Phone);
        website = Clean(request.Website); notes = Clean(request.Notes);
        var errors = new Dictionary<string, string[]>();
        if (name is null || name.Length > 200) errors["name"] = ["Tedarikçi adı zorunludur ve en fazla 200 karakter olabilir."];
        if (contact?.Length > 160) errors["contactName"] = ["Yetkili adı en fazla 160 karakter olabilir."];
        if (email?.Length > 254 || (email is not null && (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))) errors["email"] = ["Geçerli bir e-posta adresi girin (en fazla 254 karakter)."];
        if (phone?.Length > 50) errors["phone"] = ["Telefon en fazla 50 karakter olabilir."];
        if (website?.Length > 500 || (website is not null && (!Uri.TryCreate(website, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))) errors["website"] = ["Web adresi http:// veya https:// ile başlamalı ve en fazla 500 karakter olmalıdır."];
        if (notes?.Length > 2000) errors["notes"] = ["Notlar en fazla 2.000 karakter olabilir."];
        return errors;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static async Task<Guid?> ResolveSupplierIdAsync(AppDbContext db, Guid companyId, string? supplier, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(supplier)) return null;
        var normalized = supplier.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        return await db.SupplierCatalogEntries.Where(x => x.CompanyId == companyId && x.NormalizedName == normalized)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    }
    private static bool Scope(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static bool Actor(ClaimsPrincipal user, out Guid userId) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    private sealed record SupplierRequest(string? Name, string? ContactName, string? Email, string? Phone, string? Website, string? Notes);
}
