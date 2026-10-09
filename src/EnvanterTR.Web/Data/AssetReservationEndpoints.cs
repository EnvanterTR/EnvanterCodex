using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class AssetReservationEndpoints
{
    public static RouteGroupBuilder MapAssetReservationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/reservations/options", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            var reviewer = CanReview(user);
            var employees = db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
            var assets = db.Assets.AsNoTracking().Where(x => x.CompanyId == companyId &&
                x.Status == AssetStatuses.Available && x.AssignedEmployeeId == null);
            if (!reviewer)
            {
                var employeeId = await db.CompanyMemberships.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.UserId == userId).Select(x => x.EmployeeId).SingleOrDefaultAsync(ct);
                employees = employeeId is Guid linked ? employees.Where(x => x.Id == linked) : employees.Where(_ => false);
                assets = assets.Where(x => x.IsRequestable);
            }
            var employeeItems = await employees.OrderBy(x => x.DisplayName).Take(200)
                .Select(x => new { x.Id, x.DisplayName, x.EmployeeNumber }).ToListAsync(ct);
            var assetItems = await assets.OrderBy(x => x.AssetTag).Take(200)
                .Select(x => new { x.Id, x.AssetTag, x.Name, x.Location }).ToListAsync(ct);
            return Results.Ok(new { employees = employeeItems, assets = assetItems });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reservations", async ([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            if (!ValidStatus(status)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Rezervasyon durumu geçerli değil."] });
            if (from > to) return Results.ValidationProblem(new Dictionary<string, string[]> { ["dateRange"] = ["Başlangıç tarihi bitiş tarihinden sonra olamaz."] });
            if (q?.Trim().Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Arama metni 120 karakteri aşamaz."] });
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
            var query = db.AssetReservations.AsNoTracking().Where(x => x.CompanyId == companyId);
            if (!CanReview(user)) query = query.Where(x => x.RequestedByUserId == userId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            if (from is DateOnly starts) query = query.Where(x => x.EndsOn >= starts);
            if (to is DateOnly ends) query = query.Where(x => x.StartsOn <= ends);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x => db.Assets.Any(a => a.CompanyId == companyId && a.Id == x.AssetId &&
                    (a.AssetTag.Contains(term) || a.Name.Contains(term))) ||
                    db.Employees.Any(e => e.CompanyId == companyId && e.Id == x.EmployeeId &&
                        (e.DisplayName.Contains(term) || e.EmployeeNumber.Contains(term))) ||
                    (x.RequestNote != null && x.RequestNote.Contains(term)) ||
                    (x.ReviewNote != null && x.ReviewNote.Contains(term)));
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.StartsOn).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
                {
                    x.Id, x.AssetId, x.EmployeeId, x.StartsOn, x.EndsOn, x.Status, x.RequestNote, x.ReviewNote,
                    x.RequestedByUserId, x.ReviewedByUserId, x.CreatedAt, x.ReviewedAt,
                    assetTag = db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.AssetTag).FirstOrDefault(),
                    assetName = db.Assets.Where(a => a.CompanyId == companyId && a.Id == x.AssetId).Select(a => a.Name).FirstOrDefault(),
                    employeeName = db.Employees.Where(e => e.CompanyId == companyId && e.Id == x.EmployeeId).Select(e => e.DisplayName).FirstOrDefault(),
                    requestedBy = db.Users.Where(u => u.Id == x.RequestedByUserId).Select(u => u.DisplayName).FirstOrDefault(),
                    reviewedBy = db.Users.Where(u => u.Id == x.ReviewedByUserId).Select(u => u.DisplayName).FirstOrDefault()
                }).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total, canReview = CanReview(user), currentUserId = userId });
        }).RequireAuthorization("asset.read");

        api.MapPost("/reservations", async ([FromBody] CreateAssetReservationRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.AssetId == Guid.Empty || request.EmployeeId == Guid.Empty || request.StartsOn < today ||
                request.EndsOn < request.StartsOn || request.EndsOn.DayNumber - request.StartsOn.DayNumber > 365 ||
                request.StartsOn > today.AddYears(2) || request.RequestNote?.Length > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["reservation"] = ["Varlık ve çalışan seçin. Başlangıç bugünden sonraki iki yıl içinde, bitiş başlangıçtan sonra ve aralık en fazla 366 gün olmalı; not 1.000 karakteri aşamaz."] });

            var reviewer = CanReview(user);
            var linkedEmployeeId = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId).Select(x => x.EmployeeId).SingleOrDefaultAsync(ct);
            if (!reviewer && (linkedEmployeeId is null || linkedEmployeeId != request.EmployeeId)) return Results.Forbid();
            if (!await db.Employees.AnyAsync(x => x.CompanyId == companyId && x.Id == request.EmployeeId && x.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employeeId"] = ["Bu şirketteki etkin bir çalışan seçilmelidir."] });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var asset = await db.Assets.FromSqlInterpolated(
                $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {request.AssetId} AND \"DeletedAt\" IS NULL FOR UPDATE")
                .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
            if (asset is null) return Results.NotFound();
            if (asset.Status != AssetStatuses.Available || asset.AssignedEmployeeId is not null || (!reviewer && !asset.IsRequestable))
                return Results.Conflict(new { message = "Bu varlık şu anda bu rezervasyon için uygun değil." });
            if (await HasOverlapAsync(db, companyId, asset.Id, request.StartsOn, request.EndsOn, null, ct))
                return Results.Conflict(new { message = "Bu tarihlerde aynı varlık için bekleyen veya onaylanmış başka bir rezervasyon var." });

            var reservation = new AssetReservation
            {
                CompanyId = companyId, AssetId = asset.Id, EmployeeId = request.EmployeeId,
                RequestedByUserId = userId, StartsOn = request.StartsOn, EndsOn = request.EndsOn,
                RequestNote = Clean(request.RequestNote)
            };
            db.AssetReservations.Add(reservation);
            AddAudit(db, companyId, userId, reservation.Id, "ReservationRequested", $"{asset.AssetTag}: {reservation.StartsOn:yyyy-MM-dd} – {reservation.EndsOn:yyyy-MM-dd}");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Created($"/api/reservations/{reservation.Id}", new { reservation.Id, reservation.Status });
        }).RequireAuthorization("asset.read");

        api.MapPost("/reservations/{id:guid}/decision", async (Guid id, [FromBody] DecideAssetReservationRequest request,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId || !CanReview(user)) return Results.Forbid();
            if (request.Note?.Length > 1000) return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Karar notu 1.000 karakteri aşamaz."] });
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var reservation = await db.AssetReservations.FromSqlInterpolated(
                $"SELECT * FROM \"AssetReservations\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (reservation is null) return Results.NotFound();
            if (reservation.Status != "Pending") return Results.Conflict(new { message = "Yalnızca bekleyen rezervasyonlar karara bağlanabilir." });
            var asset = await db.Assets.FromSqlInterpolated(
                $"SELECT * FROM \"Assets\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {reservation.AssetId} AND \"DeletedAt\" IS NULL FOR UPDATE")
                .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
            if (asset is null) return Results.Conflict(new { message = "Rezervasyona bağlı varlık artık etkin değil." });
            if (request.Approve)
            {
                if (asset.Status != AssetStatuses.Available || asset.AssignedEmployeeId is not null)
                    return Results.Conflict(new { message = "Varlık artık kullanılabilir durumda değil; rezervasyon onaylanmadı." });
                if (await HasOverlapAsync(db, companyId, asset.Id, reservation.StartsOn, reservation.EndsOn, reservation.Id, ct))
                    return Results.Conflict(new { message = "Aynı tarihlerde bekleyen veya onaylanmış başka bir rezervasyon var." });
                reservation.Status = "Approved";
            }
            else reservation.Status = "Rejected";
            reservation.ReviewNote = Clean(request.Note);
            reservation.ReviewedByUserId = userId;
            reservation.ReviewedAt = reservation.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, companyId, userId, reservation.Id, request.Approve ? "ReservationApproved" : "ReservationRejected",
                $"{asset.AssetTag}: {reservation.StartsOn:yyyy-MM-dd} – {reservation.EndsOn:yyyy-MM-dd}" + (reservation.ReviewNote is null ? "" : $" · {reservation.ReviewNote}"));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { reservation.Id, reservation.Status, reservation.ReviewedAt });
        }).RequireAuthorization("asset.write");

        api.MapPost("/reservations/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Tenant(user, out var companyId) || UserId(user) is not Guid userId) return Results.Forbid();
            var reviewer = CanReview(user);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var reservation = await db.AssetReservations.FromSqlInterpolated(
                $"SELECT * FROM \"AssetReservations\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (reservation is null) return Results.NotFound();
            if (!reviewer && reservation.RequestedByUserId != userId) return Results.Forbid();
            if (reservation.Status is not ("Pending" or "Approved")) return Results.Conflict(new { message = "Bu rezervasyon iptal edilemez." });
            reservation.Status = "Cancelled";
            reservation.UpdatedAt = DateTimeOffset.UtcNow;
            AddAudit(db, companyId, userId, reservation.Id, "ReservationCancelled", $"Varlık {reservation.AssetId}; {reservation.StartsOn:yyyy-MM-dd} – {reservation.EndsOn:yyyy-MM-dd}");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { reservation.Id, reservation.Status });
        }).RequireAuthorization("asset.read");

        return api;
    }

    private static Task<bool> HasOverlapAsync(AppDbContext db, Guid companyId, Guid assetId,
        DateOnly startsOn, DateOnly endsOn, Guid? exceptId, CancellationToken ct) =>
        db.AssetReservations.AnyAsync(x => x.CompanyId == companyId && x.AssetId == assetId &&
            x.Id != exceptId && (x.Status == "Pending" || x.Status == "Approved") &&
            x.StartsOn <= endsOn && x.EndsOn >= startsOn, ct);

    private static void AddAudit(AppDbContext db, Guid companyId, Guid actorId, Guid reservationId, string action, string details) =>
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = actorId,
            TargetType = "AssetReservation", TargetId = reservationId, Action = action, Details = details });

    private static bool ValidStatus(string? status) => string.IsNullOrWhiteSpace(status) ||
        status.Trim() is "Pending" or "Approved" or "Rejected" or "Cancelled" or "Completed";
    private static bool Tenant(ClaimsPrincipal user, out Guid companyId) => Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
    private static Guid? UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static bool CanReview(ClaimsPrincipal user) => user.FindFirstValue("company_role") is "owner" or "admin" or "asset_manager" or "asset_operator";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record CreateAssetReservationRequest(Guid AssetId, Guid EmployeeId, DateOnly StartsOn, DateOnly EndsOn, string? RequestNote);
    public sealed record DecideAssetReservationRequest(bool Approve, string? Note);
}
