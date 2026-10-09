using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record SaveAssetImportMappingRequest(string? Name, Dictionary<string, string>? Mappings);

public static class SavedAssetImportMappingEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapSavedAssetImportMappingEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/import-mappings", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var rows = await db.SavedAssetImportMappings.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            var items = rows.Select(row => new
            {
                row.Id, row.Name,
                Mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(row.MappingJson, JsonOptions) ?? [],
                row.UpdatedAt
            });
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapPost("/assets/import-mappings", async ([FromBody] SaveAssetImportMappingRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 60)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Profil adı 1–60 karakter olmalıdır."] });
            var customFields = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(field => field.CompanyId == companyId && field.IsActive).ToListAsync(cancellationToken);
            if (!AssetCsvImportMapping.ValidateExplicitMappings(request.Mappings, customFields, out var error))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["mappings"] = [error] });

            var normalizedName = name.ToUpperInvariant();
            var row = await db.SavedAssetImportMappings.SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.UserId == userId && x.NormalizedName == normalizedName, cancellationToken);
            if (row is null)
            {
                if (await db.SavedAssetImportMappings.CountAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken) >= 25)
                    return Results.Conflict(new { message = "En fazla 25 eşleme profili saklanabilir. Önce bir profili silin." });
                row = new SavedAssetImportMapping
                {
                    CompanyId = companyId, UserId = userId, Name = name, NormalizedName = normalizedName, MappingJson = "{}"
                };
                db.SavedAssetImportMappings.Add(row);
            }
            row.Name = name;
            row.MappingJson = JsonSerializer.Serialize(request.Mappings, JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Aynı adlı profil eşzamanlı olarak kaydedildi. Listeyi yenileyip tekrar deneyin." });
            }
            return Results.Ok(new { row.Id, row.Name, Mappings = request.Mappings, row.UpdatedAt });
        }).RequireAuthorization("asset.write");

        api.MapDelete("/assets/import-mappings/{id:guid}", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.SavedAssetImportMappings.SingleOrDefaultAsync(x =>
                x.Id == id && x.CompanyId == companyId && x.UserId == userId, cancellationToken);
            if (row is null) return Results.NotFound();
            db.SavedAssetImportMappings.Remove(row);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("asset.write");
    }

    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var companyValid = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var userValid = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return companyValid && userValid;
    }
}
