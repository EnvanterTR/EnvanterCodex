using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AccessoryCategoryEndpoints
{
    public static void MapAccessoryCategoryEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/accessory-categories", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            var items = await db.AccessoryCategories.AsNoTracking().Where(x => x.CompanyId == companyId)
                .OrderBy(x => x.IsArchived).ThenBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.IsArchived,
                    accessoryCount = db.Accessories.Count(a => a.CompanyId == companyId && a.CategoryId == x.Id) })
                .ToListAsync(ct);
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapPost("/accessory-categories", async ([FromBody] AccessoryCategoryRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            var name = NormalizeName(request.Name);
            if (name is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Kategori adı zorunludur ve 120 karakteri aşamaz."] });
            if (await db.AccessoryCategories.AnyAsync(x => x.CompanyId == companyId && x.Name.ToUpper() == name.ToUpper(), ct))
                return Results.Conflict(new { message = "Bu aksesuar kategorisi zaten var." });
            var category = new AccessoryCategory { CompanyId = companyId, Name = name };
            db.AccessoryCategories.Add(category);
            db.AuditEntries.Add(Audit(user, companyId, category.Id, "AccessoryCategoryCreated", name));
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu aksesuar kategorisi zaten var." }); }
            return Results.Created($"/api/accessory-categories/{category.Id}", new { category.Id, category.Name, category.IsArchived });
        }).RequireAuthorization("asset.write");

        api.MapPatch("/accessory-categories/{id:guid}", async (Guid id, [FromBody] AccessoryCategoryRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            var category = await db.AccessoryCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (category is null) return Results.NotFound();
            var name = NormalizeName(request.Name);
            if (name is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Kategori adı zorunludur ve 120 karakteri aşamaz."] });
            if (await db.AccessoryCategories.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Name.ToUpper() == name.ToUpper(), ct))
                return Results.Conflict(new { message = "Bu aksesuar kategorisi zaten var." });
            var oldName = category.Name;
            category.Name = name;
            await db.Accessories.Where(x => x.CompanyId == companyId && x.CategoryId == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Category, name).SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow), ct);
            db.AuditEntries.Add(Audit(user, companyId, id, "AccessoryCategoryRenamed", $"{oldName} → {name}"));
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu aksesuar kategorisi zaten var." }); }
            return Results.Ok(new { category.Id, category.Name, category.IsArchived });
        }).RequireAuthorization("asset.write");

        api.MapPost("/accessory-categories/{id:guid}/archive", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompany(user, out var companyId)) return Results.Forbid();
            var category = await db.AccessoryCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (category is null) return Results.NotFound();
            category.IsArchived = !category.IsArchived;
            db.AuditEntries.Add(Audit(user, companyId, id, category.IsArchived ? "AccessoryCategoryArchived" : "AccessoryCategoryRestored", category.Name));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { category.Id, category.Name, category.IsArchived });
        }).RequireAuthorization("asset.write");
    }

    public static async Task<AccessoryCategory?> ResolveAsync(AppDbContext db, Guid companyId, Guid? categoryId,
        string? legacyName, CancellationToken ct)
    {
        if (categoryId is Guid id)
            return await db.AccessoryCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id && !x.IsArchived, ct);
        if (string.IsNullOrWhiteSpace(legacyName)) return null;
        var normalized = legacyName.Trim();
        if (normalized.Length > 120) return null;
        return await db.AccessoryCategories.SingleOrDefaultAsync(x => x.CompanyId == companyId && !x.IsArchived && x.Name.ToUpper() == normalized.ToUpper(), ct);
    }

    public static string? NormalizeName(string? value)
    {
        var name = value?.Trim();
        return string.IsNullOrWhiteSpace(name) || name.Length > 120 ? null : name;
    }

    private static AuditEntry Audit(ClaimsPrincipal user, Guid companyId, Guid targetId, string action, string details) => new()
    {
        CompanyId = companyId,
        ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
        TargetType = "AccessoryCategory", TargetId = targetId, Action = action, Details = details
    };

    private static bool TryCompany(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    public sealed record AccessoryCategoryRequest(string? Name);
}
