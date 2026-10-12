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
        if (key.Length is < 2 or > 40 || mode is not ("equals" or "empty" or "contains"))
            return (query, "Özel alan filtresi geçersiz.");

        var fieldType = await db.AssetCustomFieldDefinitions.AsNoTracking()
            .Where(field => field.CompanyId == companyId && field.Key == key && field.IsActive)
            .Select(field => field.FieldType)
            .SingleOrDefaultAsync(cancellationToken);
        if (fieldType is null)
            return (query, "Filtrelenen özel alan bu şirkette etkin değil.");
        if (fieldType == "sensitive")
            return (query, "Hassas özel alanlar gizlilik için filtrelenemez.");

        var keyPrefix = JsonSerializer.Serialize(key) + ":";
        if (mode == "empty")
        {
            if (!string.IsNullOrWhiteSpace(value)) return (query, "Boş alan filtresinde değer gönderilemez.");
            query = query.Where(asset => asset.CustomFieldsJson == null || !asset.CustomFieldsJson.Contains(keyPrefix));
            return (query, null);
        }

        if (value is null || value.Length > 2000 || value.Length == 0)
            return (query, "Özel alan filtresi için 1–2.000 karakterlik bir değer girilmelidir.");
        if (mode == "contains")
        {
            if (fieldType != "text") return (query, "Metin içinde arama yalnızca text türündeki özel alanlarda kullanılabilir.");
            var serializedValue = JsonSerializer.Serialize(value);
            var escapedValue = EscapeLikePattern(serializedValue[1..^1]);
            var pattern = $"%{EscapeLikePattern(keyPrefix)}\"%{escapedValue}%\"%";
            query = query.Where(asset => asset.CustomFieldsJson != null &&
                EF.Functions.Like(asset.CustomFieldsJson, pattern, "\\"));
            return (query, null);
        }
        var serializedPair = keyPrefix + JsonSerializer.Serialize(value);
        query = query.Where(asset => asset.CustomFieldsJson != null && asset.CustomFieldsJson.Contains(serializedPair));
        return (query, null);
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
