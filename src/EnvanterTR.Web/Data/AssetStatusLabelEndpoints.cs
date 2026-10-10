using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetStatusLabelEndpoints
{
    private static readonly (string Status, string DefaultLabel)[] Defaults =
    [
        (AssetStatuses.Available, "Kullanılabilir"),
        (AssetStatuses.Deployed, "Atanmış"),
        (AssetStatuses.Pending, "Beklemede"),
        (AssetStatuses.Maintenance, "Bakımda"),
        (AssetStatuses.Unusable, "Kullanılamaz"),
        (AssetStatuses.Archived, "Arşiv")
    ];
    private static readonly HashSet<string> SupportedStatuses = Defaults.Select(x => x.Status).ToHashSet(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapAssetStatusLabelEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/status-labels", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var json = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.AssetStatusLabelsJson).SingleOrDefaultAsync(ct);
            if (json is null && !await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, ct))
                return Results.NotFound();
            return Results.Ok(ToResponse(json));
        }).RequireAuthorization("asset.read");

        api.MapPut("/assets/status-labels", async ([FromBody] AssetStatusLabelsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (request.Labels is null || request.Labels.Length != Defaults.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["labels"] = ["Altı sabit varlık durumunun her biri için bir görünen ad girin."] });

            var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in request.Labels)
            {
                var status = item.Status?.Trim() ?? string.Empty;
                var label = item.Label?.Trim() ?? string.Empty;
                if (!SupportedStatuses.Contains(status))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Desteklenmeyen varlık durumu."] });
                if (label.Length is < 1 or > 40)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["label"] = ["Durum adı 1–40 karakter olmalıdır."] });
                if (!normalized.TryAdd(status, label))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Her durum yalnızca bir kez gönderilebilir."] });
            }
            if (!SupportedStatuses.SetEquals(normalized.Keys))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["labels"] = ["Altı sabit varlık durumunun tamamı gönderilmelidir."] });

            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, ct);
            if (company is null) return Results.NotFound();
            var before = ParseLabels(company.AssetStatusLabelsJson);
            if (Defaults.All(x => before.GetValueOrDefault(x.Status, x.DefaultLabel) == normalized[x.Status]))
                return Results.Ok(ToResponse(company.AssetStatusLabelsJson));

            company.AssetStatusLabelsJson = JsonSerializer.Serialize(normalized, JsonOptions);
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId,
                ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
                TargetType = "CompanyAssetStatusLabels",
                TargetId = companyId,
                Action = "AssetStatusLabelsUpdated",
                Details = "Varlık durumlarının şirket içindeki görünen adları güncellendi."
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(company.AssetStatusLabelsJson));
        }).RequireAuthorization("settings.manage");

        return api;
    }

    private static object ToResponse(string? json)
    {
        var labels = ParseLabels(json);
        return Defaults.Select(x => new
        {
            status = x.Status,
            defaultLabel = x.DefaultLabel,
            label = labels.GetValueOrDefault(x.Status, x.DefaultLabel)
        });
    }

    private static Dictionary<string, string> ParseLabels(string? json)
    {
        try
        {
            var stored = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            return stored is null ? new(StringComparer.Ordinal) : new(stored, StringComparer.Ordinal);
        }
        catch (JsonException) { return new(StringComparer.Ordinal); }
    }

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
}

public sealed record AssetStatusLabelsRequest(AssetStatusLabelInput[]? Labels);
public sealed record AssetStatusLabelInput(string? Status, string? Label);
