using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetCustomFieldFilter
{
    public static async Task<(IQueryable<Asset> Query, string? Error)> ApplyAsync(
        IQueryable<Asset> query, Guid companyId, string? key, string? value, string? mode,
        AppDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return value is null && mode is null
                ? (query, null)
                : (query, "Özel alan anahtarı seçilmelidir.");

        key = key.Trim();
        if (key.Length is < 2 or > 40 || mode is not ("equals" or "empty"))
            return (query, "Özel alan filtresi geçersiz.");

        var fieldExists = await db.AssetCustomFieldDefinitions.AsNoTracking()
            .AnyAsync(field => field.CompanyId == companyId && field.Key == key && field.IsActive, cancellationToken);
        if (!fieldExists)
            return (query, "Filtrelenen özel alan bu şirkette etkin değil.");

        var keyPrefix = JsonSerializer.Serialize(key) + ":";
        if (mode == "empty")
        {
            if (!string.IsNullOrWhiteSpace(value)) return (query, "Boş alan filtresinde değer gönderilemez.");
            query = query.Where(asset => asset.CustomFieldsJson == null || !asset.CustomFieldsJson.Contains(keyPrefix));
            return (query, null);
        }

        if (value is null || value.Length > 2000 || value.Length == 0)
            return (query, "Eşitlik filtresi için 1–2.000 karakterlik bir değer girilmelidir.");
        var serializedPair = keyPrefix + JsonSerializer.Serialize(value);
        query = query.Where(asset => asset.CustomFieldsJson != null && asset.CustomFieldsJson.Contains(serializedPair));
        return (query, null);
    }
}
