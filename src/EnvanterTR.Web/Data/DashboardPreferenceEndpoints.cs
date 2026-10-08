using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record UpdateDashboardPreferencesRequest(string[]? VisibleWidgets, string[]? WidgetOrder = null);
internal sealed record DashboardPreferenceDocument(string[] VisibleWidgets, string[] WidgetOrder);

public static class DashboardPreferenceEndpoints
{
    private static readonly string[] AllowedWidgets = ["stats", "assets", "activity", "upcoming", "status"];
    private static readonly string[] DefaultWidgetOrder = ["stats", "assets", "activity", "upcoming", "status"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapDashboardPreferenceEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard/preferences", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var json = await db.DashboardPreferences.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.VisibleWidgetsJson).SingleOrDefaultAsync(ct);
            if (json is null) return Results.Ok(new { visibleWidgets = AllowedWidgets, widgetOrder = DefaultWidgetOrder });
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var legacyWidgets = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
                    return Results.Ok(new { visibleWidgets = Valid(legacyWidgets) ? Normalize(legacyWidgets!) : AllowedWidgets, widgetOrder = DefaultWidgetOrder });
                }
                var preferences = JsonSerializer.Deserialize<DashboardPreferenceDocument>(json, JsonOptions);
                return Results.Ok(new
                {
                    visibleWidgets = Valid(preferences?.VisibleWidgets) ? Normalize(preferences!.VisibleWidgets) : AllowedWidgets,
                    widgetOrder = ValidOrder(preferences?.WidgetOrder) ? NormalizeOrder(preferences!.WidgetOrder) : DefaultWidgetOrder
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
            var row = await db.DashboardPreferences.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null)
            {
                row = new DashboardPreference { CompanyId = companyId, UserId = userId, VisibleWidgetsJson = "[]" };
                db.DashboardPreferences.Add(row);
            }
            row.VisibleWidgetsJson = JsonSerializer.Serialize(new DashboardPreferenceDocument(widgets, order), JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Pano ayarları eşzamanlı olarak değiştirildi. Yenileyip tekrar deneyin." }); }
            return Results.Ok(new { visibleWidgets = widgets, widgetOrder = order, row.UpdatedAt });
        }).RequireAuthorization("asset.read");
    }

    private static string[] Normalize(IEnumerable<string> widgets) => AllowedWidgets
        .Where(x => widgets.Contains(x, StringComparer.Ordinal)).ToArray();
    private static string[] NormalizeOrder(IEnumerable<string> widgets) => widgets.ToArray();
    private static bool ValidOrder(string[]? widgets) => widgets is { Length: 5 } &&
        widgets.Distinct(StringComparer.Ordinal).Count() == AllowedWidgets.Length &&
        widgets.All(x => AllowedWidgets.Contains(x, StringComparer.Ordinal));
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
