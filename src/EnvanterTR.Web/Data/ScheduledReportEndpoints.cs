using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record ScheduledReportRequest(string? Name, string? ReportKind, string? Frequency,
    string? TimeZoneId, int LocalHour, int LocalMinute, int WeekDay, int MonthDay,
    string? Language, bool IsEnabled, Guid? Version);

internal static class ScheduledReportEndpoints
{
    public static void MapScheduledReportEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/reports/schedules/{id:guid}/preview", async (Guid id, ClaimsPrincipal user,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!Identity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.ScheduledReports.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null) return Results.NotFound();
            if (row.ReportKind == "maintenance" && !await TenantPlanFeatures.IsEnabledAsync(companyId, TenantPlanFeatures.Maintenance, db, ct))
                return TenantPlanFeatures.FeatureUnavailable(TenantPlanFeatures.Maintenance);
            var document = await ScheduledReportData.BuildAsync(db, row, ct);
            return Results.Ok(new { row.Id, row.Name, row.Language, generatedAt = DateTimeOffset.UtcNow,
                document.Headers, document.Rows });
        }).RequireAuthorization("asset.read");

        api.MapGet("/reports/schedules", async (ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Identity(user, out var companyId, out var userId)) return Results.Forbid();
            var account = await db.Users.AsNoTracking().Where(x => x.Id == userId)
                .Select(x => new { x.Email, x.EmailConfirmed }).SingleAsync(ct);
            var rows = await db.ScheduledReports.AsNoTracking().Where(x => x.CompanyId == companyId && x.UserId == userId)
                .OrderBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(new { items = rows.Select(Response), emailAddress = account.Email,
                emailConfirmed = account.EmailConfirmed, smtpConfigured = ScheduledReportEmailService.IsConfigured(configuration),
                maintenanceEnabled = await TenantPlanFeatures.IsEnabledAsync(companyId, TenantPlanFeatures.Maintenance, db, ct) });
        }).RequireAuthorization("asset.read");

        api.MapPost("/reports/schedules", async ([FromBody] ScheduledReportRequest request, ClaimsPrincipal user,
            AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Identity(user, out var companyId, out var userId)) return Results.Forbid();
            var error = await ValidateAsync(request, companyId, userId, db, configuration, ct);
            if (error is not null) return error;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            // Serialize creation for one membership so concurrent requests cannot exceed its limit.
            var lockKey = $"report-schedules:{companyId:N}:{userId:N}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
            if (await db.ScheduledReports.CountAsync(x => x.CompanyId == companyId && x.UserId == userId, ct) >= 10)
                return Results.Conflict(new { message = "En fazla 10 rapor takvimi oluşturabilirsiniz." });
            var row = new ScheduledReport { CompanyId = companyId, UserId = userId, Name = request.Name!.Trim(), ReportKind = request.ReportKind! };
            Apply(row, request);
            db.ScheduledReports.Add(row);
            Audit(db, row, "Created");
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(Response(row));
        }).RequireAuthorization("asset.read");

        api.MapPut("/reports/schedules/{id:guid}", async (Guid id, [FromBody] ScheduledReportRequest request,
            ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!Identity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.ScheduledReports.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null) return Results.NotFound();
            if (request.Version is null || row.Version != request.Version)
                return Results.Conflict(new { message = "Rapor takvimi değişti; listeyi yenileyip tekrar deneyin." });
            var error = await ValidateAsync(request, companyId, userId, db, configuration, ct);
            if (error is not null) return error;
            Apply(row, request); Audit(db, row, "Updated");
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Rapor takvimi değişti; listeyi yenileyip tekrar deneyin." }); }
            return Results.Ok(Response(row));
        }).RequireAuthorization("asset.read");

        api.MapDelete("/reports/schedules/{id:guid}", async (Guid id, [FromQuery] Guid version,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!Identity(user, out var companyId, out var userId)) return Results.Forbid();
            var row = await db.ScheduledReports.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.UserId == userId, ct);
            if (row is null) return Results.NotFound();
            if (row.Version != version) return Results.Conflict(new { message = "Rapor takvimi değişti; listeyi yenileyip tekrar deneyin." });
            db.ScheduledReports.Remove(row); Audit(db, row, "Deleted");
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Rapor takvimi değişti; listeyi yenileyip tekrar deneyin." }); }
            return Results.NoContent();
        }).RequireAuthorization("asset.read");
    }

    private static async Task<IResult?> ValidateAsync(ScheduledReportRequest r, Guid companyId, Guid userId,
        AppDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 80 || r.Name.Any(char.IsControl)) errors["name"] = ["Rapor adı 1–80 karakter olmalı ve kontrol karakteri içermemelidir."];
        if (r.ReportKind is not ("inventory" or "maintenance" or "licenses" or "stock")) errors["reportKind"] = ["Geçerli bir rapor türü seçin."];
        if (r.Frequency is not ("daily" or "weekly" or "monthly")) errors["frequency"] = ["Günlük, haftalık veya aylık gönderim seçin."];
        if (!ReportScheduleClock.TryGetTimeZone(r.TimeZoneId, out _)) errors["timeZoneId"] = ["Geçerli bir saat dilimi seçin."];
        if (r.LocalHour is < 0 or > 23 || r.LocalMinute is < 0 or > 59 || r.WeekDay is < 0 or > 6 || r.MonthDay is < 1 or > 28)
            errors["time"] = ["Saat, haftanın günü veya ayın günü geçersiz."];
        if (r.Language is not ("tr" or "en" or "ru" or "uk" or "mk")) errors["language"] = ["Geçerli bir rapor dili seçin."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (r.ReportKind == "maintenance" && !await TenantPlanFeatures.IsEnabledAsync(companyId, TenantPlanFeatures.Maintenance, db, ct))
            return TenantPlanFeatures.FeatureUnavailable(TenantPlanFeatures.Maintenance);
        if (r.IsEnabled)
        {
            if (!await db.Users.AnyAsync(x => x.Id == userId && x.EmailConfirmed && x.Email != null, ct))
                return Results.Conflict(new { message = "E-posta raporlarını açmak için hesabınızın e-posta adresi doğrulanmalıdır." });
            if (!ScheduledReportEmailService.IsConfigured(configuration))
                return Results.Conflict(new { message = "E-posta sunucusu yapılandırılmadan otomatik rapor gönderimi açılamaz. Takvimi kapalı olarak kaydedebilirsiniz." });
        }
        return null;
    }

    private static void Apply(ScheduledReport row, ScheduledReportRequest request)
    {
        row.Name = request.Name!.Trim(); row.ReportKind = request.ReportKind!; row.Frequency = request.Frequency!;
        row.TimeZoneId = request.TimeZoneId!; row.LocalHour = request.LocalHour; row.LocalMinute = request.LocalMinute;
        row.WeekDay = request.WeekDay; row.MonthDay = request.MonthDay; row.Language = request.Language!; row.IsEnabled = request.IsEnabled;
        row.NextDueAt = ReportScheduleClock.NextDue(row, DateTimeOffset.UtcNow);
        row.ClaimToken = null; row.ClaimedAt = null; row.RetryAfter = null; row.AttemptCount = 0;
        row.LastStatus = "Idle"; row.LastErrorCode = null; row.UpdatedAt = DateTimeOffset.UtcNow; row.Version = Guid.NewGuid();
    }

    private static object Response(ScheduledReport r) => new
    {
        r.Id, r.Name, r.ReportKind, r.Frequency, r.TimeZoneId, r.LocalHour, r.LocalMinute, r.WeekDay, r.MonthDay,
        r.Language, r.IsEnabled, r.NextDueAt, r.LastSentAt, r.LastAttemptAt, r.RetryAfter, r.LastStatus,
        r.LastErrorCode, r.AttemptCount, r.CreatedAt, r.UpdatedAt, r.Version
    };
    private static bool Identity(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        var company = Guid.TryParse(user.FindFirstValue("company_id"), out companyId);
        var account = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return company && account;
    }
    private static void Audit(AppDbContext db, ScheduledReport row, string action) => db.AuditEntries.Add(new AuditEntry
    {
        CompanyId = row.CompanyId, ActorUserId = row.UserId, TargetType = "ScheduledReport", TargetId = row.Id,
        Action = action, Details = $"{row.ReportKind}; {row.Frequency}; {row.TimeZoneId}; {row.LocalHour:D2}:{row.LocalMinute:D2}; enabled={row.IsEnabled}"
    });
}
