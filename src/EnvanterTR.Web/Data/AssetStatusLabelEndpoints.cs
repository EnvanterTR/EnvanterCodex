using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetStatusLabelEndpoints
{
    private static readonly Regex CustomStatusCode = new("^Custom_[a-f0-9]{8}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedStatuses = AssetStatusConfiguration.BuiltIns
        .Select(x => x.Status).ToHashSet(StringComparer.Ordinal);

    public static RouteGroupBuilder MapAssetStatusLabelEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/assets/status-labels", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var json = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.AssetStatusLabelsJson).SingleOrDefaultAsync(ct);
            if (json is null && !await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, ct))
                return Results.NotFound();
            return Results.Ok(ToResponse(AssetStatusConfiguration.Parse(json)));
        }).RequireAuthorization("asset.read");

        api.MapPut("/assets/status-labels", async ([FromBody] AssetStatusLabelsRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (await IsLocationScopeRestrictedAsync(user, db, companyId, ct)) return Results.Forbid();
            if (request.Labels is null || request.Labels.Length != SupportedStatuses.Count)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["labels"] = ["Altı sabit varlık durumunun her biri için bir görünen ad girin."] });

            var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in request.Labels)
            {
                var status = item.Status?.Trim() ?? string.Empty;
                var label = item.Label?.Trim() ?? string.Empty;
                if (!SupportedStatuses.Contains(status))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Desteklenmeyen sabit varlık durumu."] });
                if (label.Length is < 1 or > 40)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["label"] = ["Durum adı 1–40 karakter olmalıdır."] });
                if (!normalized.TryAdd(status, label))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Her durum yalnızca bir kez gönderilebilir."] });
            }
            if (!SupportedStatuses.SetEquals(normalized.Keys))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["labels"] = ["Altı sabit varlık durumunun tamamı gönderilmelidir."] });

            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, ct);
            if (company is null) return Results.NotFound();
            var current = AssetStatusConfiguration.Parse(company.AssetStatusLabelsJson);
            if (current.Labels.Count == normalized.Count && normalized.All(pair =>
                    current.Labels.GetValueOrDefault(pair.Key, AssetStatusConfiguration.BuiltIns.Single(x => x.Status == pair.Key).Label) == pair.Value))
                return Results.Ok(ToResponse(current));

            company.AssetStatusLabelsJson = AssetStatusConfiguration.Serialize(current with { Labels = normalized });
            AddAudit(db, companyId, user, "AssetStatusLabelsUpdated", "Varlık durumlarının şirket içindeki görünen adları güncellendi.");
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(AssetStatusConfiguration.Parse(company.AssetStatusLabelsJson)));
        }).RequireAuthorization("settings.manage");

        api.MapGet("/assets/status-config", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            var json = await db.Companies.AsNoTracking().Where(x => x.Id == companyId)
                .Select(x => x.AssetStatusLabelsJson).SingleOrDefaultAsync(ct);
            if (json is null && !await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId, ct))
                return Results.NotFound();
            return Results.Ok(ToConfigResponse(AssetStatusConfiguration.Parse(json)));
        }).RequireAuthorization("asset.read");

        api.MapPut("/assets/status-config", async ([FromBody] AssetStatusConfigurationRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryCompanyId(user, out var companyId)) return Results.Forbid();
            if (await IsLocationScopeRestrictedAsync(user, db, companyId, ct)) return Results.Forbid();
            if (request.CustomStatuses is null || request.Transitions is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["configuration"] = ["Özel durumları ve geçiş listesini birlikte gönderin."] });
            if (request.CustomStatuses.Length > AssetStatusConfiguration.MaximumCustomStatuses)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["customStatuses"] = [$"En fazla {AssetStatusConfiguration.MaximumCustomStatuses} özel durum tanımlanabilir."] });
            if (request.Transitions.Length > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["transitions"] = ["En fazla 100 geçiş kuralı tanımlanabilir."] });

            var customStatuses = new List<CompanyAssetStatusDefinition>();
            var codes = new HashSet<string>(StringComparer.Ordinal);
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in request.CustomStatuses)
            {
                var code = item.Status?.Trim() ?? string.Empty;
                var label = item.Label?.Trim() ?? string.Empty;
                var color = item.Color?.Trim() ?? string.Empty;
                if (!CustomStatusCode.IsMatch(code) || SupportedStatuses.Contains(code))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Özel durum kodu Custom_ ve ardından 8 küçük onaltılık karakterden oluşmalıdır."] });
                if (!codes.Add(code))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Özel durum kodları benzersiz olmalıdır."] });
                if (label.Length is < 2 or > 40 || !labels.Add(label))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["label"] = ["Özel durum adları 2–40 karakter olmalı ve birbirinden farklı olmalıdır."] });
                if (!HexColor.IsMatch(color))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["color"] = ["Renk #RRGGBB biçiminde olmalıdır."] });
                customStatuses.Add(new CompanyAssetStatusDefinition(code, label, color.ToUpperInvariant()));
            }

            var validManualStatuses = AssetStatusConfiguration.ManualBuiltIns.Concat(codes).ToHashSet(StringComparer.Ordinal);
            var transitions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var seenEdges = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in request.Transitions)
            {
                var from = edge.From?.Trim() ?? string.Empty;
                var to = edge.To?.Trim() ?? string.Empty;
                if (!validManualStatuses.Contains(from) || !validManualStatuses.Contains(to) || from == to)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["transitions"] = ["Geçişler tanımlı durumlar arasında olmalı; atama, iade ve arşiv geri alma gibi sistem işlemleri bu listede düzenlenemez."] });
                if (!seenEdges.Add($"{from}\n{to}"))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["transitions"] = ["Aynı geçiş kuralı birden fazla gönderilemez."] });
                if (!transitions.TryGetValue(from, out var allowed)) transitions[from] = allowed = [];
                allowed.Add(to);
            }

            var company = await db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, ct);
            if (company is null) return Results.NotFound();
            var removedStatuses = AssetStatusConfiguration.Parse(company.AssetStatusLabelsJson).CustomStatuses
                .Select(x => x.Status).Except(codes, StringComparer.Ordinal).ToArray();
            if (removedStatuses.Length > 0 && await db.Assets.AsNoTracking().AnyAsync(x =>
                    x.CompanyId == companyId && removedStatuses.Contains(x.Status), ct))
                return Results.Conflict(new { message = "Kullanımdaki özel durum kaldırılamaz. Önce varlıkları başka duruma alın." });

            var config = AssetStatusConfiguration.Normalize(new CompanyAssetStatusConfigurationDocument(
                1, AssetStatusConfiguration.Parse(company.AssetStatusLabelsJson).Labels, customStatuses,
                transitions.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.Ordinal)));
            foreach (var definition in customStatuses)
                if (!AssetStatusConfiguration.CanReachAvailable(config, definition.Status))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["transitions"] = [$"“{definition.Label}” durumundan Kullanılabilir durumuna giden en az bir geçiş yolu tanımlayın."] });

            var json = AssetStatusConfiguration.Serialize(config);
            if (json.Length > AssetStatusConfiguration.MaximumStoredLength)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["configuration"] = ["Durum ve geçiş yapılandırması şirket ayarları için çok büyük; bazı geçiş kurallarını kaldırın."] });
            if (company.AssetStatusLabelsJson == json)
                return Results.Ok(ToConfigResponse(config));

            company.AssetStatusLabelsJson = json;
            AddAudit(db, companyId, user, "AssetStatusConfigurationUpdated", "Özel varlık durumları ve geçiş kuralları güncellendi.");
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToConfigResponse(config));
        }).RequireAuthorization("settings.manage");

        return api;
    }

    public static object ToResponse(CompanyAssetStatusConfigurationDocument configuration) =>
        AssetStatusConfiguration.BuiltIns.Select(x => new
        {
            status = x.Status,
            defaultLabel = x.Label,
            label = configuration.Labels.GetValueOrDefault(x.Status, x.Label),
            color = AssetStatusConfiguration.DefaultColor(x.Status),
            custom = false
        }).Cast<object>().Concat(configuration.CustomStatuses.Select(x => (object)new
        {
            status = x.Status,
            defaultLabel = x.Label,
            label = x.Label,
            color = x.Color,
            custom = true
        })).ToArray();

    private static object ToConfigResponse(CompanyAssetStatusConfigurationDocument configuration)
    {
        var manualStatuses = AssetStatusConfiguration.ManualBuiltIns.Concat(configuration.CustomStatuses.Select(x => x.Status)).ToHashSet(StringComparer.Ordinal);
        return new
        {
            builtInStatuses = AssetStatusConfiguration.BuiltIns.Select(x => new
            {
                status = x.Status,
                label = configuration.Labels.GetValueOrDefault(x.Status, x.Label),
                defaultLabel = x.Label,
                color = AssetStatusConfiguration.DefaultColor(x.Status),
                systemManaged = x.Status is AssetStatuses.Deployed
            }),
            customStatuses = configuration.CustomStatuses,
            transitions = configuration.Transitions.Where(x => manualStatuses.Contains(x.Key))
                .SelectMany(x => x.Value.Where(manualStatuses.Contains).Select(to => new { from = x.Key, to })).ToArray()
        };
    }

    private static void AddAudit(AppDbContext db, Guid companyId, ClaimsPrincipal user, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry
        {
            CompanyId = companyId,
            ActorUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : null,
            TargetType = "CompanyAssetStatusConfiguration",
            TargetId = companyId,
            Action = action,
            Details = details
        });

    private static bool TryCompanyId(ClaimsPrincipal user, out Guid companyId) =>
        Guid.TryParse(user.FindFirstValue("company_id"), out companyId);

    private static async Task<bool> IsLocationScopeRestrictedAsync(ClaimsPrincipal user, AppDbContext db,
        Guid companyId, CancellationToken ct)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return true;
        return await CompanyLocationAccess.GetEffectiveLocationIdsAsync(db, companyId, actorId, ct) is not null;
    }
}

public sealed record AssetStatusLabelsRequest(AssetStatusLabelInput[]? Labels);
public sealed record AssetStatusLabelInput(string? Status, string? Label);
public sealed record AssetStatusConfigurationRequest(AssetCustomStatusInput[]? CustomStatuses, AssetStatusTransitionInput[]? Transitions);
public sealed record AssetCustomStatusInput(string? Status, string? Label, string? Color);
public sealed record AssetStatusTransitionInput(string? From, string? To);
