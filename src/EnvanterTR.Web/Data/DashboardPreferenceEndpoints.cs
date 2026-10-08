using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record UpdateDashboardPreferencesRequest(string[]? VisibleWidgets, string[]? WidgetOrder = null, DashboardChartDefinition[]? CustomCharts = null);
public sealed record DashboardChartDefinition(string Dimension, string Type);
internal sealed record DashboardPreferenceDocument(string[] VisibleWidgets, string[] WidgetOrder, DashboardChartDefinition[]? CustomCharts = null);
internal sealed record DashboardChartItem(string Label, int Count);
internal sealed record DashboardChartGroup(Guid? Id, int Count);

public static class DashboardPreferenceEndpoints
{
    private static readonly string[] AllowedWidgets = ["stats", "assets", "activity", "upcoming", "status"];
    private static readonly string[] DefaultWidgetOrder = ["stats", "assets", "activity", "upcoming", "status"];
    private static readonly string[] AllowedChartDimensions = ["status", "category", "brand", "model", "location"];
    private static readonly string[] AllowedChartTypes = ["bar", "donut"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapDashboardPreferenceEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard/preferences", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var json = await db.DashboardPreferences.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.VisibleWidgetsJson).SingleOrDefaultAsync(ct);
            if (json is null) return Results.Ok(new { visibleWidgets = AllowedWidgets, widgetOrder = DefaultWidgetOrder, customCharts = Array.Empty<DashboardChartDefinition>() });
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var legacyWidgets = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
                    return Results.Ok(new { visibleWidgets = Valid(legacyWidgets) ? Normalize(legacyWidgets!) : AllowedWidgets, widgetOrder = DefaultWidgetOrder, customCharts = Array.Empty<DashboardChartDefinition>() });
                }
                var preferences = JsonSerializer.Deserialize<DashboardPreferenceDocument>(json, JsonOptions);
                return Results.Ok(new
                {
                    visibleWidgets = Valid(preferences?.VisibleWidgets) ? Normalize(preferences!.VisibleWidgets) : AllowedWidgets,
                    widgetOrder = ValidOrder(preferences?.WidgetOrder) ? NormalizeOrder(preferences!.WidgetOrder) : DefaultWidgetOrder,
                    customCharts = ValidCustomCharts(preferences?.CustomCharts) ? NormalizeCustomCharts(preferences!.CustomCharts!) : Array.Empty<DashboardChartDefinition>()
                });
            }
            catch (JsonException) { return Results.Ok(new { visibleWidgets = AllowedWidgets }); }
        }).RequireAuthorization("asset.read");

        api.MapPut("/dashboard/preferences", async ([FromBody] UpdateDashboardPreferencesRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            if (!Valid(request.VisibleWidgets))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["visibleWidgets"] = ["Pano bölümü seçimi geçersiz."] });
            var widgets = Normalize(request.VisibleWidgets!);
            var order = request.WidgetOrder ?? DefaultWidgetOrder;
            if (!ValidOrder(order))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["widgetOrder"] = ["Pano sırası her bölümü bir kez içermelidir."] });
            order = NormalizeOrder(order);
            if (!ValidCustomCharts(request.CustomCharts))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["customCharts"] = ["En fazla 6 farklı pano grafiği tanımlayabilirsiniz; ölçüt ve grafik türü desteklenen değerlerden seçilmelidir."] });
            var customCharts = NormalizeCustomCharts(request.CustomCharts ?? []);
            var row = await db.DashboardPreferences.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null)
            {
                row = new DashboardPreference { CompanyId = companyId, UserId = userId, VisibleWidgetsJson = "[]" };
                db.DashboardPreferences.Add(row);
            }
            row.VisibleWidgetsJson = JsonSerializer.Serialize(new DashboardPreferenceDocument(widgets, order, customCharts), JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Pano ayarları eşzamanlı olarak değiştirildi. Yenileyip tekrar deneyin." }); }
            return Results.Ok(new { visibleWidgets = widgets, widgetOrder = order, customCharts, row.UpdatedAt });
        }).RequireAuthorization("asset.read");

        api.MapGet("/dashboard/charts/{dimension}", async (string dimension, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out _)) return Results.Forbid();
            dimension = dimension.Trim().ToLowerInvariant();
            if (!AllowedChartDimensions.Contains(dimension, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["dimension"] = ["Grafik ölçütü geçersiz."] });
            var assets = db.Assets.AsNoTracking().Where(a => a.CompanyId == companyId && a.DeletedAt == null);
            var total = await assets.CountAsync(ct);
            List<DashboardChartItem> items;
            if (dimension == "status")
            {
                var groups = await assets.GroupBy(a => a.Status).Select(g => new { Key = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count).Take(10).ToListAsync(ct);
                var labels = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [AssetStatuses.Available] = "Kullanılabilir", [AssetStatuses.Deployed] = "Atanmış",
                    [AssetStatuses.Pending] = "Beklemede", [AssetStatuses.Maintenance] = "Bakımda",
                    [AssetStatuses.Unusable] = "Kullanılamaz", [AssetStatuses.Archived] = "Arşiv"
                };
                items = groups.Select(g => new DashboardChartItem(labels.GetValueOrDefault(g.Key, g.Key), g.Count)).ToList();
            }
            else if (dimension == "location")
            {
                var groups = await assets.GroupBy(a => a.Location).Select(g => new { Key = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count).Take(10).ToListAsync(ct);
                items = groups.Select(g => new DashboardChartItem(string.IsNullOrWhiteSpace(g.Key) ? "Konum belirtilmemiş" : g.Key!, g.Count)).ToList();
            }
            else
            {
                List<DashboardChartGroup> groups;
                Dictionary<Guid, string> labels;
                if (dimension == "category")
                {
                    groups = await assets.GroupBy(a => a.CategoryId).Select(g => new DashboardChartGroup(g.Key, g.Count()))
                        .OrderByDescending(g => g.Count).Take(10).ToListAsync(ct);
                    var ids = groups.Where(g => g.Id.HasValue).Select(g => g.Id!.Value).ToArray();
                    labels = await db.AssetCategories.AsNoTracking().Where(c => c.CompanyId == companyId && ids.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
                }
                else if (dimension == "brand")
                {
                    groups = await assets.GroupBy(a => a.BrandId).Select(g => new DashboardChartGroup(g.Key, g.Count()))
                        .OrderByDescending(g => g.Count).Take(10).ToListAsync(ct);
                    var ids = groups.Where(g => g.Id.HasValue).Select(g => g.Id!.Value).ToArray();
                    labels = await db.AssetBrands.AsNoTracking().Where(b => b.CompanyId == companyId && ids.Contains(b.Id))
                        .ToDictionaryAsync(b => b.Id, b => b.Name, ct);
                }
                else
                {
                    groups = await assets.GroupBy(a => a.ModelId).Select(g => new DashboardChartGroup(g.Key, g.Count()))
                        .OrderByDescending(g => g.Count).Take(10).ToListAsync(ct);
                    var ids = groups.Where(g => g.Id.HasValue).Select(g => g.Id!.Value).ToArray();
                    labels = await db.AssetModels.AsNoTracking().Where(m => m.CompanyId == companyId && ids.Contains(m.Id))
                        .ToDictionaryAsync(m => m.Id, m => m.Name, ct);
                }
                items = groups.Select(g => new DashboardChartItem(g.Id is Guid id ? labels.GetValueOrDefault(id, "Katalog kaydı yok") : "Belirtilmemiş", g.Count)).ToList();
            }
            var shown = items.Sum(i => i.Count);
            if (total > shown) items.Add(new DashboardChartItem("Diğer", total - shown));
            return Results.Ok(new { dimension, items });
        }).RequireAuthorization("asset.read");
    }

    private static string[] Normalize(IEnumerable<string> widgets) => AllowedWidgets
        .Where(x => widgets.Contains(x, StringComparer.Ordinal)).ToArray();
    private static string[] NormalizeOrder(IEnumerable<string> widgets) => widgets.ToArray();
    private static bool ValidOrder(string[]? widgets) => widgets is { Length: 5 } &&
        widgets.Distinct(StringComparer.Ordinal).Count() == AllowedWidgets.Length &&
        widgets.All(x => AllowedWidgets.Contains(x, StringComparer.Ordinal));
    private static bool ValidCustomCharts(DashboardChartDefinition[]? charts)
    {
        if (charts is null) return true;
        if (charts.Length > 6 || charts.Any(chart => chart is null ||
            !AllowedChartDimensions.Contains(chart.Dimension?.Trim().ToLowerInvariant(), StringComparer.Ordinal) ||
            !AllowedChartTypes.Contains(chart.Type?.Trim().ToLowerInvariant(), StringComparer.Ordinal))) return false;
        return charts.Select(chart => $"{chart.Dimension.Trim().ToLowerInvariant()}:{chart.Type.Trim().ToLowerInvariant()}")
            .Distinct(StringComparer.Ordinal).Count() == charts.Length;
    }
    private static DashboardChartDefinition[] NormalizeCustomCharts(IEnumerable<DashboardChartDefinition> charts) => charts
        .Select(chart => new DashboardChartDefinition(chart.Dimension.Trim().ToLowerInvariant(), chart.Type.Trim().ToLowerInvariant())).ToArray();
    private static bool Valid(string[]? widgets) => widgets is { Length: > 0 and <= 5 } &&
        widgets.Distinct(StringComparer.Ordinal).Count() == widgets.Length &&
        widgets.All(x => AllowedWidgets.Contains(x, StringComparer.Ordinal));
    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var validCompany = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var validUser = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return validCompany && validUser;
    }
}
