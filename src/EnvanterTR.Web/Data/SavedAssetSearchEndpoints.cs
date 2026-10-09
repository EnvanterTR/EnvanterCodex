using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record SavedAssetSearchFilters(
    string? Q, string? Status, string? Warranty, Guid? CategoryId, Guid? BrandId,
    Guid? ModelId, Guid? EmployeeId, string? EmployeeName, string? Location,
    string? SortBy, string? SortDirection, string? CustomFieldKey = null,
    string? CustomFieldValue = null, string? CustomFieldMode = null);

public sealed record SaveAssetSearchRequest(string? Name, SavedAssetSearchFilters? Filters);

public static class SavedAssetSearchEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] Statuses = ["Available", "Deployed", "Pending", "Maintenance", "Unusable", "Archived"];
    private static readonly string[] Warranties = ["expired", "expiring", "active", "unspecified"];
    private static readonly string[] SortFields = ["tag", "name", "category", "brand", "employee", "location", "status", "warranty", "updated"];

    public static void MapSavedAssetSearchEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/saved-searches", async (ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var rows = await db.SavedAssetSearches.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            var items = rows.Select(x => new
            {
                x.Id, x.Name,
                Filters = JsonSerializer.Deserialize<SavedAssetSearchFilters>(x.FiltersJson, JsonOptions),
                x.UpdatedAt
            });
            return Results.Ok(new { items });
        }).RequireAuthorization("asset.read");

        api.MapPost("/assets/saved-searches", async ([FromBody] SaveAssetSearchRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var name = request.Name?.Trim();
            var filters = request.Filters;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 60)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Arama adı 1–60 karakter olmalıdır."] });
            if (!Valid(filters))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["filters"] = ["Arama filtrelerinden biri geçersiz veya çok uzun."] });
            var validatedFilters = filters!;
            if (!string.IsNullOrWhiteSpace(validatedFilters.CustomFieldKey))
            {
                var fieldType = await db.AssetCustomFieldDefinitions.AsNoTracking()
                    .Where(field => field.CompanyId == companyId && field.Key == validatedFilters.CustomFieldKey && field.IsActive)
                    .Select(field => field.FieldType)
                    .SingleOrDefaultAsync(cancellationToken);
                if (fieldType is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["customFieldKey"] = ["Filtrelenen özel alan bu şirkette etkin değil."] });
                if (validatedFilters.CustomFieldMode == "contains" && fieldType != "text")
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["customFieldMode"] = ["Metin içinde arama yalnızca text türündeki özel alanlarda kullanılabilir."] });
            }

            var normalizedName = name.ToUpperInvariant();
            var row = await db.SavedAssetSearches.SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.UserId == userId && x.NormalizedName == normalizedName, cancellationToken);
            if (row is null)
            {
                if (await db.SavedAssetSearches.CountAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken) >= 25)
                    return Results.Conflict(new { message = "En fazla 25 kayıtlı arama saklanabilir. Önce bir kaydı silin." });
                row = new SavedAssetSearch { CompanyId = companyId, UserId = userId, Name = name, NormalizedName = normalizedName, FiltersJson = "{}" };
                db.SavedAssetSearches.Add(row);
            }
            row.Name = name;
            row.FiltersJson = JsonSerializer.Serialize(validatedFilters, JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Aynı adlı arama eşzamanlı olarak kaydedildi. Listeyi yenileyip tekrar deneyin." });
            }
            return Results.Ok(new { row.Id, row.Name, Filters = validatedFilters, row.UpdatedAt });
        }).RequireAuthorization("asset.read");

        api.MapDelete("/assets/saved-searches/{id:guid}", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.SavedAssetSearches.SingleOrDefaultAsync(x =>
                x.Id == id && x.CompanyId == companyId && x.UserId == userId, cancellationToken);
            if (row is null) return Results.NotFound();
            db.SavedAssetSearches.Remove(row);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("asset.read");
    }

    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var validCompany = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var validUser = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return validCompany && validUser;
    }

    private static bool Valid(SavedAssetSearchFilters? filters) => filters is not null &&
        (filters.Q?.Length ?? 0) <= 120 && (filters.EmployeeName?.Length ?? 0) <= 200 &&
        (filters.Location?.Length ?? 0) <= 160 &&
        (filters.CustomFieldKey?.Length ?? 0) <= 40 && (filters.CustomFieldValue?.Length ?? 0) <= 2000 &&
        (string.IsNullOrWhiteSpace(filters.CustomFieldKey)
            ? filters.CustomFieldValue is null && filters.CustomFieldMode is null
            : filters.CustomFieldMode == "empty" && string.IsNullOrWhiteSpace(filters.CustomFieldValue) ||
              (filters.CustomFieldMode is "equals" or "contains") && !string.IsNullOrEmpty(filters.CustomFieldValue)) &&
        (string.IsNullOrEmpty(filters.Status) || Statuses.Contains(filters.Status, StringComparer.Ordinal)) &&
        (string.IsNullOrEmpty(filters.Warranty) || Warranties.Contains(filters.Warranty, StringComparer.Ordinal)) &&
        (string.IsNullOrEmpty(filters.SortBy) || SortFields.Contains(filters.SortBy, StringComparer.Ordinal)) &&
        (string.IsNullOrEmpty(filters.SortDirection) || filters.SortDirection is "asc" or "desc");
}
