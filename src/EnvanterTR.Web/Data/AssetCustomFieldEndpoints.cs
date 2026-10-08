using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class AssetCustomFieldEndpoints
{
    private static readonly HashSet<string> SupportedTypes = ["text", "number", "date", "checkbox", "select"];

    public static IEndpointRouteBuilder MapAssetCustomFieldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/assets/custom-fields").RequireAuthorization();
        api.MapGet("", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var fields = await db.AssetCustomFieldDefinitions.AsNoTracking()
                .Where(x => x.CompanyId == companyId).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new { x.Id, x.Key, x.Name, x.FieldType, x.OptionsJson, x.IsRequired, x.SortOrder, x.IsActive })
                .ToListAsync(ct);
            return Results.Ok(fields.Select(x => new { x.Id, x.Key, x.Name, x.FieldType,
                options = ParseOptions(x.OptionsJson), x.IsRequired, x.SortOrder, x.IsActive }));
        }).RequireAuthorization("asset.read");

        api.MapPost("", async ([FromBody] AssetCustomFieldDefinitionRequest request, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (!TryNormalizeDefinition(request, out var key, out var name, out var type, out var options, out var error))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["field"] = [error!] });
            if (await db.AssetCustomFieldDefinitions.AnyAsync(x => x.CompanyId == companyId && x.Key == key, ct))
                return Results.Conflict(new { message = "Bu şirket içinde özel alan anahtarı zaten kullanılıyor." });
            var field = new AssetCustomFieldDefinition { CompanyId = companyId, Key = key!, Name = name!,
                FieldType = type!, OptionsJson = options, IsRequired = request.IsRequired,
                SortOrder = Math.Clamp(request.SortOrder, -10000, 10000) };
            db.AssetCustomFieldDefinitions.Add(field);
            AddAudit(db, user, companyId, field.Id, "AssetCustomFieldCreated", $"Varlık özel alanı oluşturuldu: {field.Name}.");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/assets/custom-fields/{field.Id}", ToResponse(field));
        }).RequireAuthorization("asset.write");

        api.MapPut("/{id:guid}", async (Guid id, [FromBody] AssetCustomFieldDefinitionRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (!TryNormalizeDefinition(request, out var key, out var name, out var type, out var options, out var error))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["field"] = [error!] });
            var field = await db.AssetCustomFieldDefinitions.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (field is null) return Results.NotFound();
            if (!string.Equals(field.Key, key, StringComparison.Ordinal) &&
                await db.AssetCustomFieldDefinitions.AnyAsync(x => x.CompanyId == companyId && x.Id != id && x.Key == key, ct))
                return Results.Conflict(new { message = "Bu şirket içinde özel alan anahtarı zaten kullanılıyor." });
            var storedValues = await db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId && x.CustomFieldsJson != null)
                .Select(x => x.CustomFieldsJson!).ToListAsync(ct);
            var fieldInUse = storedValues.Any(json =>
            {
                try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json)?.ContainsKey(field.Key) == true; }
                catch (JsonException) { return false; }
            });
            if (fieldInUse && (field.Key != key || field.FieldType != type || field.OptionsJson != options))
                return Results.Conflict(new { message = "Bu alan varlık kayıtlarında kullanılıyor. Anahtar, tür ve seçenekler mevcut değerlerle uyumsuz olabileceği için değiştirilemez; yeni alan oluşturup değerleri aktarın." });
            var before = field.Name;
            field.Key = key!; field.Name = name!; field.FieldType = type!; field.OptionsJson = options;
            field.IsRequired = request.IsRequired; field.SortOrder = Math.Clamp(request.SortOrder, -10000, 10000);
            field.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, user, companyId, field.Id, "AssetCustomFieldUpdated", $"Varlık özel alanı güncellendi: {before} → {field.Name}.");
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(field));
        }).RequireAuthorization("asset.write");

        api.MapPatch("/{id:guid}/active", async (Guid id, [FromBody] SetAssetCustomFieldActiveRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var field = await db.AssetCustomFieldDefinitions.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
            if (field is null) return Results.NotFound();
            if (field.IsActive == request.IsActive) return Results.Ok(new { field.Id, field.IsActive });
            field.IsActive = request.IsActive; field.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, user, companyId, field.Id, request.IsActive ? "AssetCustomFieldActivated" : "AssetCustomFieldDeactivated",
                $"Varlık özel alanı {(request.IsActive ? "etkinleştirildi" : "pasifleştirildi")}: {field.Name}.");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { field.Id, field.IsActive });
        }).RequireAuthorization("asset.write");
        return endpoints;
    }

    public static async Task<(string? Json, string? Error)> NormalizeValuesAsync(AppDbContext db, Guid companyId,
        Dictionary<string, string>? submitted, string? existingJson, bool preserveWhenMissing, CancellationToken ct)
    {
        var fields = await db.AssetCustomFieldDefinitions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive).ToListAsync(ct);
        return NormalizeValues(fields, submitted, existingJson, preserveWhenMissing);
    }

    public static (string? Json, string? Error) NormalizeValues(IReadOnlyCollection<AssetCustomFieldDefinition> fields,
        Dictionary<string, string>? submitted, string? existingJson = null, bool preserveWhenMissing = false)
    {
        if (preserveWhenMissing && submitted is null) return (existingJson, null);
        var input = submitted ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var definitions = fields.ToDictionary(x => x.Key, StringComparer.Ordinal);
        foreach (var key in input.Keys)
            if (!definitions.ContainsKey(key)) return (null, $"'{key}' bu şirkette etkin bir özel alan değil.");
        Dictionary<string, string> values;
        try
        {
            values = preserveWhenMissing && !string.IsNullOrWhiteSpace(existingJson)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson) ?? new(StringComparer.Ordinal)
                : new(StringComparer.Ordinal);
        }
        catch (JsonException) { values = new(StringComparer.Ordinal); }
        foreach (var field in fields)
        {
            input.TryGetValue(field.Key, out var raw);
            var value = raw?.Trim() ?? string.Empty;
            if (value.Length == 0)
            {
                if (field.IsRequired) return (null, $"'{field.Name}' alanı zorunludur.");
                values.Remove(field.Key);
                continue;
            }
            if (value.Length > 2000) return (null, $"'{field.Name}' alanı 2.000 karakteri aşamaz.");
            var valid = field.FieldType switch
            {
                "text" => true,
                "number" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "date" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                "checkbox" => bool.TryParse(value, out _),
                "select" => ParseOptions(field.OptionsJson).Contains(value, StringComparer.Ordinal),
                _ => false
            };
            if (!valid) return (null, $"'{field.Name}' alanı {field.FieldType} türüyle eşleşmiyor.");
            values[field.Key] = value;
        }
        var serialized = JsonSerializer.Serialize(values);
        return serialized.Length > 16000
            ? (null, "Varlık özel alanlarının toplam boyutu 16.000 karakteri aşamaz.")
            : (serialized, null);
    }

    public static string[] ExportValues(string? json, IReadOnlyCollection<AssetCustomFieldDefinition> fields)
    {
        Dictionary<string, string> values;
        try { values = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "{}") ?? new(StringComparer.Ordinal); }
        catch (JsonException) { values = new(StringComparer.Ordinal); }
        return fields.OrderBy(x => x.SortOrder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(field => values.GetValueOrDefault(field.Key, "")).ToArray();
    }

    private static bool TryNormalizeDefinition(AssetCustomFieldDefinitionRequest request, out string? key,
        out string? name, out string? type, out string? optionsJson, out string? error)
    {
        key = request.Key?.Trim().ToLowerInvariant(); name = request.Name?.Trim();
        type = request.FieldType?.Trim().ToLowerInvariant(); optionsJson = null; error = null;
        if (key is null || !FieldKeyRegex().IsMatch(key)) error = "Anahtar 2–40 karakter olmalı; küçük harf, rakam ve alt çizgi kullanın ve harfle başlayın.";
        else if (string.IsNullOrWhiteSpace(name) || name.Length > 100) error = "Alan adı zorunludur ve 100 karakteri aşamaz.";
        else if (type is null || !SupportedTypes.Contains(type)) error = "Alan türü text, number, date, checkbox veya select olmalıdır.";
        else
        {
            var options = (request.Options ?? []).Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!).Distinct(StringComparer.Ordinal).ToArray();
            if (type == "select" && (options.Length is < 1 or > 100 || options.Any(x => x.Length > 100))) error = "Seçim alanı 1–100 adet, en fazla 100 karakterlik farklı seçenek içermelidir.";
            else if (type != "select" && options.Length > 0) error = "Seçenekler yalnızca select türünde kullanılabilir.";
            else if (type == "select") optionsJson = JsonSerializer.Serialize(options);
        }
        return error is null;
    }

    private static object ToResponse(AssetCustomFieldDefinition x) => new { x.Id, x.Key, x.Name, x.FieldType,
        options = ParseOptions(x.OptionsJson), x.IsRequired, x.SortOrder, x.IsActive };
    private static string[] ParseOptions(string? json)
    {
        try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }
    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static void AddAudit(AppDbContext db, ClaimsPrincipal user, Guid companyId, Guid fieldId, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId,
            ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : null,
            TargetType = "AssetCustomField", TargetId = fieldId, Action = action, Details = details });

    [GeneratedRegex("^[a-z][a-z0-9_]{1,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyRegex();
}

public sealed record SetAssetCustomFieldActiveRequest(bool IsActive);
