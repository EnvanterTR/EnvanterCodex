using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record UpdateOperationalNotificationPreferencesRequest(string[]? EnabledKinds, string[]? EmailEnabledKinds = null);
public sealed record UpdateOperationalNotificationStateRequest(string? Kind, Guid RecordId, string? Action);

public static class OperationalNotificationEndpoints
{
    private static readonly string[] AllowedKinds = ["maintenance", "audit", "license", "warranty", "return", "reservation"];
    private static readonly string[] AllowedEmailKinds = [.. AllowedKinds, "assignment", "asset-life", "subscription"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed record NotificationStateRow(string Kind, Guid RecordId, DateTimeOffset? ReadAt, DateTimeOffset? DismissedAt);

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
                .Select(x => new NotificationStateRow(x.Kind, x.RecordId, x.ReadAt, x.DismissedAt)).ToListAsync(ct);
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, userId, ct);
            var accessibleRecords = await GetAccessibleNotificationRecordsAsync(rows, companyId, userId, user, employeeScope, db, ct);
            var visibleRows = rows.Where(row => accessibleRecords.Contains((row.Kind, row.RecordId))).ToArray();
            return Results.Ok(new { items = visibleRows });
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
            var employeeScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, companyId, userId, ct);
            if (!await CanAccessNotificationRecordAsync(kind, request.RecordId, companyId, userId, user, employeeScope, db, ct))
                return Results.NotFound();

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

    private static async Task<bool> CanAccessNotificationRecordAsync(string kind, Guid recordId, Guid companyId,
        Guid userId, ClaimsPrincipal user, EmployeeAccessScope employeeScope, AppDbContext db, CancellationToken ct)
    {
        var allowedLocations = employeeScope.AllowedLocations;
        var accessibleEmployeeIds = employeeScope.Apply(db.Employees.AsNoTracking()
                .Where(employee => employee.CompanyId == companyId))
            .Select(employee => employee.Id);

        switch (kind)
        {
            case "maintenance":
                var maintenance = await (from item in db.AssetMaintenances.AsNoTracking()
                    join asset in db.Assets.AsNoTracking() on new { item.CompanyId, AssetId = item.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                    where item.CompanyId == companyId && item.Id == recordId
                    select new { asset.LocationCatalogId }).SingleOrDefaultAsync(ct);
                return maintenance is not null && (allowedLocations is null ||
                    maintenance.LocationCatalogId is Guid maintenanceLocation && allowedLocations.Contains(maintenanceLocation));
            case "license":
                var licenseQuery = db.SoftwareLicenses.AsNoTracking()
                    .Where(item => item.CompanyId == companyId && item.Id == recordId);
                if (employeeScope.IsRestricted)
                    licenseQuery = licenseQuery.Where(item => db.SoftwareLicenseAssignments.Any(assignment =>
                        assignment.CompanyId == companyId && assignment.LicenseId == item.Id &&
                        (assignment.EmployeeId != null || assignment.AssetId != null) &&
                        (assignment.EmployeeId == null || accessibleEmployeeIds.Contains(assignment.EmployeeId.Value)) &&
                        (assignment.AssetId == null || db.Assets.Any(asset => asset.CompanyId == companyId &&
                            asset.Id == assignment.AssetId && (allowedLocations == null || asset.LocationCatalogId.HasValue &&
                                allowedLocations.Contains(asset.LocationCatalogId.Value))))));
                return await licenseQuery.AnyAsync(ct);
            case "warranty":
            case "audit":
            case "return":
                var assetQuery = db.Assets.AsNoTracking()
                    .Where(asset => asset.CompanyId == companyId && asset.Id == recordId);
                if (kind == "warranty") assetQuery = assetQuery.Where(asset => asset.WarrantyExpiresAt != null);
                else if (kind == "audit") assetQuery = assetQuery.Where(asset => asset.NextAuditOn != null);
                else assetQuery = assetQuery.Where(asset => asset.Status == AssetStatuses.Deployed &&
                    asset.AssignedEmployeeId != null && asset.ExpectedReturnOn != null);
                if (allowedLocations is not null)
                    assetQuery = assetQuery.Where(asset => asset.LocationCatalogId.HasValue &&
                        allowedLocations.Contains(asset.LocationCatalogId.Value));
                if (kind == "return" && employeeScope.IsRestricted)
                    assetQuery = assetQuery.Where(asset => asset.AssignedEmployeeId.HasValue &&
                        accessibleEmployeeIds.Contains(asset.AssignedEmployeeId.Value));
                return await assetQuery.AnyAsync(ct);
            case "reservation":
                var reservationQuery = from reservation in db.AssetReservations.AsNoTracking()
                    join asset in db.Assets.AsNoTracking() on new { reservation.CompanyId, AssetId = reservation.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                    where reservation.CompanyId == companyId && reservation.Id == recordId && reservation.Status == "Approved" &&
                        (CanReview(user) || reservation.RequestedByUserId == userId ||
                            db.CompanyMemberships.Any(membership => membership.CompanyId == companyId && membership.UserId == userId && membership.EmployeeId == reservation.EmployeeId)) &&
                        (allowedLocations == null || asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)) &&
                        accessibleEmployeeIds.Contains(reservation.EmployeeId)
                    select reservation.Id;
                return await reservationQuery.AnyAsync(ct);
            default:
                return false;
        }
    }

    private static async Task<HashSet<(string Kind, Guid RecordId)>> GetAccessibleNotificationRecordsAsync(
        IReadOnlyCollection<NotificationStateRow> rows, Guid companyId, Guid userId, ClaimsPrincipal user,
        EmployeeAccessScope employeeScope, AppDbContext db, CancellationToken ct)
    {
        var result = new HashSet<(string Kind, Guid RecordId)>();
        var idsByKind = rows.GroupBy(row => row.Kind)
            .ToDictionary(group => group.Key, group => group.Select(row => row.RecordId).Distinct().ToArray());
        var allowedLocations = employeeScope.AllowedLocations;
        var accessibleEmployeeIds = employeeScope.Apply(db.Employees.AsNoTracking()
                .Where(employee => employee.CompanyId == companyId))
            .Select(employee => employee.Id);

        if (idsByKind.TryGetValue("maintenance", out var maintenanceIds))
        {
            var accessibleIds = await (from item in db.AssetMaintenances.AsNoTracking()
                join asset in db.Assets.AsNoTracking() on new { item.CompanyId, AssetId = item.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                where item.CompanyId == companyId && maintenanceIds.Contains(item.Id) &&
                    (allowedLocations == null || asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value))
                select item.Id).ToListAsync(ct);
            foreach (var id in accessibleIds) result.Add(("maintenance", id));
        }

        if (idsByKind.TryGetValue("license", out var licenseIds))
        {
            var accessibleLicenses = db.SoftwareLicenses.AsNoTracking()
                .Where(item => item.CompanyId == companyId && licenseIds.Contains(item.Id));
            if (employeeScope.IsRestricted)
                accessibleLicenses = accessibleLicenses.Where(item => db.SoftwareLicenseAssignments.Any(assignment =>
                    assignment.CompanyId == companyId && assignment.LicenseId == item.Id &&
                    (assignment.EmployeeId != null || assignment.AssetId != null) &&
                    (assignment.EmployeeId == null || accessibleEmployeeIds.Contains(assignment.EmployeeId.Value)) &&
                    (assignment.AssetId == null || db.Assets.Any(asset => asset.CompanyId == companyId &&
                        asset.Id == assignment.AssetId && (allowedLocations == null || asset.LocationCatalogId.HasValue &&
                            allowedLocations.Contains(asset.LocationCatalogId.Value))))));
            foreach (var id in await accessibleLicenses.Select(item => item.Id).ToListAsync(ct)) result.Add(("license", id));
        }

        foreach (var kind in new[] { "warranty", "audit", "return" })
        {
            if (!idsByKind.TryGetValue(kind, out var assetIds)) continue;
            var assets = db.Assets.AsNoTracking().Where(asset => asset.CompanyId == companyId && assetIds.Contains(asset.Id));
            if (kind == "warranty") assets = assets.Where(asset => asset.WarrantyExpiresAt != null);
            else if (kind == "audit") assets = assets.Where(asset => asset.NextAuditOn != null);
            else assets = assets.Where(asset => asset.Status == AssetStatuses.Deployed && asset.AssignedEmployeeId != null && asset.ExpectedReturnOn != null);
            if (allowedLocations is not null)
                assets = assets.Where(asset => asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value));
            if (kind == "return" && employeeScope.IsRestricted)
                assets = assets.Where(asset => asset.AssignedEmployeeId.HasValue && accessibleEmployeeIds.Contains(asset.AssignedEmployeeId.Value));
            foreach (var id in await assets.Select(asset => asset.Id).ToListAsync(ct)) result.Add((kind, id));
        }

        if (idsByKind.TryGetValue("reservation", out var reservationIds))
        {
            var accessibleReservations = from reservation in db.AssetReservations.AsNoTracking()
                join asset in db.Assets.AsNoTracking() on new { reservation.CompanyId, AssetId = reservation.AssetId } equals new { asset.CompanyId, AssetId = asset.Id }
                where reservation.CompanyId == companyId && reservationIds.Contains(reservation.Id) && reservation.Status == "Approved" &&
                    (CanReview(user) || reservation.RequestedByUserId == userId ||
                        db.CompanyMemberships.Any(membership => membership.CompanyId == companyId && membership.UserId == userId && membership.EmployeeId == reservation.EmployeeId)) &&
                    (allowedLocations == null || asset.LocationCatalogId.HasValue && allowedLocations.Contains(asset.LocationCatalogId.Value)) &&
                    accessibleEmployeeIds.Contains(reservation.EmployeeId)
                select reservation.Id;
            foreach (var id in await accessibleReservations.ToListAsync(ct)) result.Add(("reservation", id));
        }
        return result;
    }

    private static bool CanReview(ClaimsPrincipal user) => user.FindFirstValue("company_role") is "owner" or "admin" or "asset_manager" or "asset_operator";
}
