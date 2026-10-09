using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record UpdateOperationalNotificationPreferencesRequest(string[]? EnabledKinds, string[]? EmailEnabledKinds = null);
public sealed record UpdateOperationalNotificationStateRequest(string? Kind, Guid RecordId, string? Action);

public static class OperationalNotificationEndpoints
{
    private static readonly string[] AllowedKinds = ["maintenance", "audit", "license", "warranty", "return"];
    private static readonly string[] AllowedEmailKinds = [.. AllowedKinds, "assignment"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapOperationalNotificationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/notifications/preferences", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.OperationalNotificationPreferences.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => new { x.EnabledKindsJson, x.EmailEnabledKindsJson }).SingleOrDefaultAsync(ct);
            return Results.Ok(new { enabledKinds = ParseKinds(row?.EnabledKindsJson), emailEnabledKinds = ParseEmailKinds(row?.EmailEnabledKindsJson) });
        }).RequireAuthorization("asset.read");

        api.MapPut("/notifications/preferences", async ([FromBody] UpdateOperationalNotificationPreferencesRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var requestedKinds = request.EnabledKinds?.Select(kind => kind?.Trim().ToLowerInvariant()).ToArray();
            if (requestedKinds is null || requestedKinds.Length > AllowedKinds.Length ||
                requestedKinds.Any(kind => kind is null || !AllowedKinds.Contains(kind, StringComparer.Ordinal)) ||
                requestedKinds.Distinct(StringComparer.Ordinal).Count() != requestedKinds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["enabledKinds"] = ["Bildirim türü seçimi geçersiz."] });

            var requestedEmailKinds = request.EmailEnabledKinds?.Select(kind => kind?.Trim().ToLowerInvariant()).ToArray() ?? [];
            if (requestedEmailKinds.Length > AllowedEmailKinds.Length ||
                requestedEmailKinds.Any(kind => kind is null || !AllowedEmailKinds.Contains(kind, StringComparer.Ordinal)) ||
                requestedEmailKinds.Distinct(StringComparer.Ordinal).Count() != requestedEmailKinds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["emailEnabledKinds"] = ["E-posta bildirim türü seçimi geçersiz."] });

            var kinds = AllowedKinds.Where(requestedKinds.Contains).ToArray();
            var emailKinds = AllowedEmailKinds.Where(requestedEmailKinds.Contains).ToArray();
            var row = await db.OperationalNotificationPreferences.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null)
            {
                row = new OperationalNotificationPreference { CompanyId = companyId, UserId = userId,
                    EnabledKindsJson = "[]", EmailEnabledKindsJson = "[]" };
                db.OperationalNotificationPreferences.Add(row);
            }
            row.EnabledKindsJson = JsonSerializer.Serialize(kinds, JsonOptions);
            row.EmailEnabledKindsJson = JsonSerializer.Serialize(emailKinds, JsonOptions);
            row.UpdatedAt = DateTimeOffset.UtcNow;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bildirim tercihleri eşzamanlı olarak değiştirildi. Yenileyip tekrar deneyin." }); }
            return Results.Ok(new { enabledKinds = kinds, emailEnabledKinds = emailKinds, row.UpdatedAt });
        }).RequireAuthorization("asset.read");

        api.MapGet("/notifications/states", async (string[]? key, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            if (key is null || key.Length is < 1 or > 500)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["key"] = ["1–500 bildirim anahtarı gönderilmelidir."] });
            var parsed = new List<(string Kind, Guid RecordId)>();
            foreach (var candidate in key)
            {
                var separator = candidate?.IndexOf(':') ?? -1;
                if (separator <= 0 || separator == candidate!.Length - 1)
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["key"] = ["Bildirim anahtarı kind:recordId biçiminde olmalıdır."] });
                var kind = candidate[..separator].Trim().ToLowerInvariant();
                if (!AllowedKinds.Contains(kind, StringComparer.Ordinal) || !Guid.TryParse(candidate[(separator + 1)..], out var recordId))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["key"] = ["Bildirim anahtarı geçersiz."] });
                parsed.Add((kind, recordId));
            }
            var kinds = parsed.Select(x => x.Kind).Distinct(StringComparer.Ordinal).ToArray();
            var ids = parsed.Select(x => x.RecordId).Distinct().ToArray();
            var rows = await db.OperationalNotificationStates.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId && kinds.Contains(x.Kind) && ids.Contains(x.RecordId))
                .Select(x => new { x.Kind, x.RecordId, x.ReadAt, x.DismissedAt }).ToListAsync(ct);
            return Results.Ok(new { items = rows });
        }).RequireAuthorization("asset.read");

        api.MapPut("/notifications/states", async ([FromBody] UpdateOperationalNotificationStateRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryIdentity(user, out var companyId, out var userId)) return Results.Forbid();
            var kind = request.Kind?.Trim().ToLowerInvariant();
            var action = request.Action?.Trim().ToLowerInvariant();
            if (request.RecordId == Guid.Empty || kind is null || !AllowedKinds.Contains(kind, StringComparer.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["notification"] = ["Bildirim türü veya kayıt kimliği geçersiz."] });
            if (action is not ("read" or "unread" or "dismiss" or "restore"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["action"] = ["İşlem read, unread, dismiss veya restore olmalıdır."] });
            var belongsToCompany = kind switch
            {
                "maintenance" => await db.AssetMaintenances.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == request.RecordId, ct),
                "license" => await db.SoftwareLicenses.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == request.RecordId, ct),
                "warranty" => await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == request.RecordId && x.WarrantyExpiresAt != null, ct),
                "audit" => await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == request.RecordId && x.NextAuditOn != null, ct),
                "return" => await db.Assets.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == request.RecordId && x.ExpectedReturnOn != null, ct),
                _ => false
            };
            if (!belongsToCompany) return Results.NotFound();

            var row = await db.OperationalNotificationStates.SingleOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId && x.Kind == kind && x.RecordId == request.RecordId, ct);
            if (row is null)
            {
                row = new OperationalNotificationState { CompanyId = companyId, UserId = userId, Kind = kind, RecordId = request.RecordId };
                db.OperationalNotificationStates.Add(row);
            }
            var now = DateTimeOffset.UtcNow;
            switch (action)
            {
                case "read": row.ReadAt ??= now; break;
                case "unread": row.ReadAt = null; break;
                case "dismiss": row.ReadAt ??= now; row.DismissedAt ??= now; break;
                case "restore": row.DismissedAt = null; break;
            }
            row.UpdatedAt = now;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { return Results.Conflict(new { message = "Bildirim durumu eşzamanlı olarak değiştirildi. Yenileyip tekrar deneyin." }); }
            return Results.Ok(new { row.Kind, row.RecordId, row.ReadAt, row.DismissedAt, row.UpdatedAt });
        }).RequireAuthorization("asset.read");
    }

    private static string[] ParseKinds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return AllowedKinds;
        try
        {
            var kinds = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
            if (kinds is null || kinds.Length > AllowedKinds.Length || kinds.Any(x => !AllowedKinds.Contains(x, StringComparer.Ordinal)) ||
                kinds.Distinct(StringComparer.Ordinal).Count() != kinds.Length) return AllowedKinds;
            return AllowedKinds.Where(kinds.Contains).ToArray();
        }
        catch (JsonException) { return AllowedKinds; }
    }

    private static string[] ParseEmailKinds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var kinds = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
            if (kinds is null || kinds.Length > AllowedEmailKinds.Length || kinds.Any(x => !AllowedEmailKinds.Contains(x, StringComparer.Ordinal)) ||
                kinds.Distinct(StringComparer.Ordinal).Count() != kinds.Length) return [];
            return AllowedEmailKinds.Where(kinds.Contains).ToArray();
        }
        catch (JsonException) { return []; }
    }

    private static bool TryIdentity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var validCompany = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var validUser = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return validCompany && validUser;
    }
}
