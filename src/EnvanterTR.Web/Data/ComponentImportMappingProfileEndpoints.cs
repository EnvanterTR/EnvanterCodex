using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class ComponentImportMappingProfileEndpoints
{
    private const string Family = "components";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapComponentImportMappingProfileEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/components/import-mappings", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var rows = await db.SavedAssetImportMappings.AsNoTracking().Where(x => x.CompanyId == companyId && x.UserId == userId && x.Family == Family)
                .OrderBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(new { items = rows.Select(row => new { row.Id, row.Name, Mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(row.MappingJson, JsonOptions) ?? [], row.UpdatedAt }) });
        }).RequireAuthorization("asset.write");

        api.MapPost("/components/import-mappings", async ([FromBody] SaveAssetImportMappingRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 60) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Profil adı 1–60 karakter olmalıdır."] });
            if (!ComponentCsvImportMapping.ValidateExplicitMappings(request.Mappings, out var error)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = [error] });
            var normalizedName = name.ToUpperInvariant();
            var row = await db.SavedAssetImportMappings.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId && x.Family == Family && x.NormalizedName == normalizedName, ct);
            if (row is null)
            {
                if (await db.SavedAssetImportMappings.CountAsync(x => x.CompanyId == companyId && x.UserId == userId && x.Family == Family, ct) >= 25)
                    return Results.Conflict(new { message = "En fazla 25 bileşen eşleme profili saklanabilir. Önce bir profili silin." });
                row = new SavedAssetImportMapping { CompanyId = companyId, UserId = userId, Family = Family, Name = name, NormalizedName = normalizedName, MappingJson = "{}" };
                db.SavedAssetImportMappings.Add(row);
            }
            row.Name = name; row.MappingJson = JsonSerializer.Serialize(request.Mappings, JsonOptions); row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Aynı adlı profil eşzamanlı olarak kaydedildi. Listeyi yenileyip tekrar deneyin." }); }
            return Results.Ok(new { row.Id, row.Name, Mappings = request.Mappings, row.UpdatedAt });
        }).RequireAuthorization("asset.write").WithMetadata(new RequestSizeLimitAttribute(32_768));

        api.MapDelete("/components/import-mappings/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.SavedAssetImportMappings.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.UserId == userId && x.Family == Family, ct);
            if (row is null) return Results.NotFound();
            db.SavedAssetImportMappings.Remove(row); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("asset.write");
    }

    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var validCompany = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var validUser = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return validCompany && validUser;
    }
}
