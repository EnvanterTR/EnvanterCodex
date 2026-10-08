using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record CustomAssetReportDefinitionRequest(
    string? Name, string[]? Columns, string? Q, string? Status, string? Warranty,
    string? SortBy, string? SortDirection);

public static class CustomAssetReportEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] BuiltInColumns = ["tag", "name", "status", "category", "brand", "model", "serial", "location", "employeeNumber", "employeeName", "purchaseDate", "warrantyExpiresAt", "purchaseCost", "currency", "parentAssetTag"];
    private static readonly string[] Statuses = ["Available", "Deployed", "Pending", "Maintenance", "Unusable", "Archived"];
    private static readonly string[] Warranties = ["expired", "expiring", "active", "unspecified"];
    private static readonly string[] SortFields = ["tag", "name", "category", "brand", "employee", "location", "status", "warranty", "updated"];

    public static void MapCustomAssetReportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/custom-reports", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var rows = await db.CustomAssetReports.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .OrderBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(new { items = rows.Select(ToResponse) });
        }).RequireAuthorization("asset.read");

        api.MapPost("/assets/custom-reports", async ([FromBody] CustomAssetReportDefinitionRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var validation = await ValidateAsync(request, companyId, db, ct);
            if (validation.Error is not null) return validation.Error;
            var normalizedName = validation.Name!.ToUpperInvariant();
            var row = await db.CustomAssetReports.SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.UserId == userId && x.NormalizedName == normalizedName, ct);
            if (row is null)
            {
                if (await db.CustomAssetReports.CountAsync(x => x.CompanyId == companyId && x.UserId == userId, ct) >= 30)
                    return Results.Conflict(new { message = "En fazla 30 özel rapor taslağı saklanabilir. Önce bir taslağı silin." });
                row = new CustomAssetReport { CompanyId = companyId, UserId = userId,
                    Name = validation.Name!, NormalizedName = normalizedName, DefinitionJson = "{}" };
                db.CustomAssetReports.Add(row);
            }
            row.Name = validation.Name!;
            row.DefinitionJson = JsonSerializer.Serialize(validation.Definition, JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu adla başka bir rapor aynı anda kaydedildi. Listeyi yenileyin." }); }
            return Results.Ok(ToResponse(row));
        }).RequireAuthorization("asset.read");

        api.MapPut("/assets/custom-reports/{id:guid}", async (Guid id, [FromBody] CustomAssetReportDefinitionRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.CustomAssetReports.SingleOrDefaultAsync(x =>
                x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null) return Results.NotFound();
            var validation = await ValidateAsync(request, companyId, db, ct);
            if (validation.Error is not null) return validation.Error;
            var normalizedName = validation.Name!.ToUpperInvariant();
            if (await db.CustomAssetReports.AnyAsync(x => x.CompanyId == companyId && x.UserId == userId &&
                x.Id != id && x.NormalizedName == normalizedName, ct))
                return Results.Conflict(new { message = "Bu ad başka bir rapor taslağında kullanılıyor." });
            row.Name = validation.Name!;
            row.NormalizedName = normalizedName;
            row.DefinitionJson = JsonSerializer.Serialize(validation.Definition, JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bu ad başka bir rapor taslağında kullanılıyor." }); }
            return Results.Ok(ToResponse(row));
        }).RequireAuthorization("asset.read");

        api.MapDelete("/assets/custom-reports/{id:guid}", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.CustomAssetReports.SingleOrDefaultAsync(x =>
                x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null) return Results.NotFound();
            db.CustomAssetReports.Remove(row);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("asset.read");
    }

    private static async Task<(string? Name, object? Definition, IResult? Error)> ValidateAsync(
        CustomAssetReportDefinitionRequest request, Guid companyId, AppDbContext db, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
            return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Rapor adı 1–80 karakter olmalıdır."] }));
        var columns = request.Columns;
        if (columns is null || columns.Length is < 1 or > 64 || columns.Any(string.IsNullOrWhiteSpace) ||
            columns.Distinct(StringComparer.Ordinal).Count() != columns.Length)
            return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["columns"] = ["1–64 farklı rapor sütunu seçin."] }));
        if ((request.Q?.Length ?? 0) > 120 ||
            (!string.IsNullOrEmpty(request.Status) && !Statuses.Contains(request.Status, StringComparer.Ordinal)) ||
            (!string.IsNullOrEmpty(request.Warranty) && !Warranties.Contains(request.Warranty, StringComparer.Ordinal)) ||
            (!string.IsNullOrEmpty(request.SortBy) && !SortFields.Contains(request.SortBy, StringComparer.Ordinal)) ||
            (!string.IsNullOrEmpty(request.SortDirection) && request.SortDirection is not ("asc" or "desc")))
            return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["filters"] = ["Rapor arama, durum, garanti veya sıralama ölçütlerinden biri geçersiz."] }));

        var customColumns = columns.Where(x => x.StartsWith("custom:", StringComparison.Ordinal)).ToArray();
        var customKeys = customColumns.Select(x => x[7..]).ToArray();
        if (customColumns.Length != customKeys.Length || customKeys.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 40))
            return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["columns"] = ["Özel alan sütun anahtarı geçersiz."] }));
        var availableCustomKeys = await db.AssetCustomFieldDefinitions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive).Select(x => x.Key).ToListAsync(ct);
        if (columns.Any(column => !BuiltInColumns.Contains(column, StringComparer.Ordinal) &&
            (!column.StartsWith("custom:", StringComparison.Ordinal) || !availableCustomKeys.Contains(column[7..], StringComparer.Ordinal))))
            return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["columns"] = ["Rapor sütunları etkin varlık alanlarından seçilmelidir."] }));

        var definition = new { columns, q = request.Q?.Trim() ?? "", status = request.Status ?? "",
            warranty = request.Warranty ?? "", sortBy = request.SortBy ?? "tag", sortDirection = request.SortDirection ?? "asc" };
        var json = JsonSerializer.Serialize(definition, JsonOptions);
        if (json.Length > 8192) return (null, null, Results.ValidationProblem(new Dictionary<string, string[]> { ["definition"] = ["Rapor tanımı çok büyük."] }));
        return (name, definition, null);
    }

    private static object ToResponse(CustomAssetReport row)
    {
        using var document = JsonDocument.Parse(row.DefinitionJson);
        return new { row.Id, row.Name, definition = document.RootElement.Clone(), row.CreatedAt, row.UpdatedAt };
    }

    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var companyValid = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var userValid = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return companyValid && userValid;
    }
}
