using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record AcceptSoftwareLicenseEulaRequest(Guid EulaAttachmentId, bool Confirmed);

public static class SoftwareLicenseEmployeeEndpoints
{
    public static IEndpointRouteBuilder MapSoftwareLicenseEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/session/software-licenses", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive, ct)) return Results.Forbid();
            var membership = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => new { x.EmployeeId }).SingleOrDefaultAsync(ct);
            if (membership is null) return Results.Forbid();
            if (membership.EmployeeId is not Guid employeeId)
                return Results.Ok(new { linked = false, employeeName = (string?)null, items = Array.Empty<object>() });

            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId)
                .Select(x => new { x.DisplayName, x.IsActive, x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(ct);
            if (employee is null || !employee.IsActive || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, userId,
                    employee.LocationCatalogId, employee.CompanyUnitId, ct)) return Results.Forbid();

            var rows = await (from assignment in db.SoftwareLicenseAssignments.AsNoTracking()
                              join license in db.SoftwareLicenses.AsNoTracking() on new { assignment.CompanyId, assignment.LicenseId }
                                  equals new { license.CompanyId, LicenseId = license.Id }
                              where assignment.CompanyId == companyId && assignment.EmployeeId == employeeId &&
                                    assignment.RevokedAt == null && license.RequiresEulaAcceptance
                              orderby license.Name, assignment.AssignedAt
                              select new
                              {
                                  assignment.Id,
                                  assignment.LicenseId,
                                  assignment.AssignedAt,
                                  license.Name,
                                  license.Publisher,
                                  license.ProductVersion,
                                  license.ExpirationDate,
                                  CurrentEula = db.SoftwareLicenseAttachments.AsNoTracking()
                                      .Where(file => file.CompanyId == companyId && file.LicenseId == license.Id && file.IsCurrentEula)
                                      .Select(file => new { file.Id, file.OriginalFileName, file.EulaVersion, file.Sha256 })
                                      .SingleOrDefault()
                              }).ToListAsync(ct);

            var attachmentIds = rows.Where(x => x.CurrentEula is not null).Select(x => x.CurrentEula!.Id).Distinct().ToArray();
            var acceptedIds = attachmentIds.Length == 0
                ? new HashSet<Guid>()
                : (await db.SoftwareLicenseEmployeeEulaAcceptances.AsNoTracking()
                    .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.AcceptedByUserId == userId &&
                                rows.Select(row => row.Id).Contains(x.AssignmentId) && attachmentIds.Contains(x.EulaAttachmentId))
                    .Select(x => new { x.AssignmentId, x.EulaAttachmentId }).ToListAsync(ct))
                  .Select(x => x.AssignmentId).ToHashSet();

            var items = rows.Select(row => new
            {
                assignmentId = row.Id,
                row.LicenseId,
                row.Name,
                row.Publisher,
                row.ProductVersion,
                row.ExpirationDate,
                row.AssignedAt,
                eulaAvailable = row.CurrentEula is not null,
                eulaAttachmentId = row.CurrentEula?.Id,
                eulaFileName = row.CurrentEula?.OriginalFileName,
                eulaVersion = row.CurrentEula?.EulaVersion,
                eulaSha256 = row.CurrentEula?.Sha256,
                eulaUrl = row.CurrentEula is null ? null : $"/api/session/software-licenses/{row.Id}/eula",
                accepted = row.CurrentEula is not null && acceptedIds.Contains(row.Id)
            }).ToArray();
            return Results.Ok(new { linked = true, employeeName = employee.DisplayName, items });
        }).RequireAuthorization();

        endpoints.MapGet("/api/session/software-licenses/{assignmentId:guid}/eula", async (Guid assignmentId,
            HttpContext context, ClaimsPrincipal user, AppDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();
            if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive, ct)) return Results.Forbid();
            var employeeId = await db.CompanyMemberships.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.UserId == userId)
                .Select(x => x.EmployeeId).SingleOrDefaultAsync(ct);
            if (employeeId is not Guid linkedEmployeeId) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == linkedEmployeeId)
                .Select(x => new { x.IsActive, x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(ct);
            if (employee is null || !employee.IsActive || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, userId,
                    employee.LocationCatalogId, employee.CompanyUnitId, ct)) return Results.Forbid();
            var file = await (from assignment in db.SoftwareLicenseAssignments.AsNoTracking()
                              join license in db.SoftwareLicenses.AsNoTracking() on new { assignment.CompanyId, assignment.LicenseId }
                                  equals new { license.CompanyId, LicenseId = license.Id }
                              join attachment in db.SoftwareLicenseAttachments.AsNoTracking() on new { license.CompanyId, LicenseId = license.Id }
                                  equals new { attachment.CompanyId, attachment.LicenseId }
                              where assignment.CompanyId == companyId && assignment.Id == assignmentId && assignment.EmployeeId == linkedEmployeeId &&
                                    assignment.RevokedAt == null && license.RequiresEulaAcceptance && attachment.IsCurrentEula
                              select new { license.Id, attachment.StorageFileName, attachment.ContentType, attachment.OriginalFileName })
                .SingleOrDefaultAsync(ct);
            if (file is null || !SoftwareLicenseEndpoints.IsSafeLicenseAttachmentName(file.StorageFileName)) return Results.NotFound();
            var path = Path.Combine(SoftwareLicenseEndpoints.LicenseAttachmentDirectory(configuration, companyId, file.Id), file.StorageFileName);
            if (!CompanyMediaPathGuard.IsSafePath(configuration, companyId, path) || !System.IO.File.Exists(path))
                return Results.NotFound();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(stream, file.ContentType, enableRangeProcessing: false);
        }).RequireAuthorization();

        endpoints.MapPost("/api/session/software-licenses/{assignmentId:guid}/eula/accept", async (Guid assignmentId,
            [FromBody] AcceptSoftwareLicenseEulaRequest request, HttpContext context, IAntiforgery antiforgery,
            ClaimsPrincipal user, AppDbContext db, CancellationToken ct) =>
        {
            if (!await HasValidCsrfAsync(context, antiforgery)) return Results.BadRequest(new { message = "İstek doğrulaması başarısız." });
            if (!request.Confirmed || request.EulaAttachmentId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["confirmed"] = ["Güncel EULA belgesini açıp okuduğunuzu ve kabul ettiğinizi onaylayın."] });
            if (!TryScope(user, out var companyId, out var userId)) return Results.Forbid();

            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var membership = await db.CompanyMemberships.FromSqlInterpolated(
                    $"SELECT * FROM \"CompanyMemberships\" WHERE \"CompanyId\" = {companyId} AND \"UserId\" = {userId} FOR SHARE")
                .SingleOrDefaultAsync(ct);
            if (membership?.EmployeeId is not Guid employeeId) return Results.Forbid();
            if (!await db.Companies.AsNoTracking().AnyAsync(x => x.Id == companyId && x.IsActive, ct)) return Results.Forbid();
            var employee = await db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == employeeId)
                .Select(x => new { x.IsActive, x.LocationCatalogId, x.CompanyUnitId }).SingleOrDefaultAsync(ct);
            if (employee is null || !employee.IsActive || !await CompanyLocationAccess.CanAccessEmployeeAsync(db, companyId, userId,
                    employee.LocationCatalogId, employee.CompanyUnitId, ct)) return Results.Forbid();

            var assignment = await db.SoftwareLicenseAssignments.FromSqlInterpolated(
                    $"SELECT * FROM \"SoftwareLicenseAssignments\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assignmentId} AND \"EmployeeId\" = {employeeId} AND \"RevokedAt\" IS NULL FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (assignment is null) return Results.NotFound();
            var license = await db.SoftwareLicenses.FromSqlInterpolated(
                    $"SELECT * FROM \"SoftwareLicenses\" WHERE \"CompanyId\" = {companyId} AND \"Id\" = {assignment.LicenseId} FOR SHARE")
                .SingleOrDefaultAsync(ct);
            if (license is null || !license.RequiresEulaAcceptance || license.IsArchived)
                return Results.Conflict(new { message = "Bu atama için güncel EULA kabulü gerekmiyor." });
            var currentEula = await db.SoftwareLicenseAttachments.AsNoTracking().SingleOrDefaultAsync(x =>
                x.CompanyId == companyId && x.LicenseId == license.Id && x.IsCurrentEula, ct);
            if (currentEula is null) return Results.Conflict(new { message = "Bu lisans için güncel EULA belgesi bulunmuyor. Şirket yöneticinizden belgeyi istemelisiniz." });
            if (currentEula.Id != request.EulaAttachmentId || string.IsNullOrWhiteSpace(currentEula.EulaVersion) ||
                string.IsNullOrWhiteSpace(currentEula.Sha256))
                return Results.Conflict(new { message = "EULA sürümü güncellendi. Belgeyi yenileyip tekrar kontrol edin." });

            var alreadyAccepted = await db.SoftwareLicenseEmployeeEulaAcceptances.AnyAsync(x =>
                x.CompanyId == companyId && x.AssignmentId == assignment.Id && x.EulaAttachmentId == currentEula.Id && x.AcceptedByUserId == userId, ct);
            if (alreadyAccepted)
            {
                await tx.CommitAsync(ct);
                return Results.Ok(new { accepted = true, assignmentId, eulaAttachmentId = currentEula.Id, eulaVersion = currentEula.EulaVersion });
            }
            var email = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.Email).SingleOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(email)) return Results.Forbid();
            var now = DateTimeOffset.UtcNow;
            db.SoftwareLicenseEmployeeEulaAcceptances.Add(new SoftwareLicenseEmployeeEulaAcceptance
            {
                CompanyId = companyId,
                LicenseId = license.Id,
                AssignmentId = assignment.Id,
                EmployeeId = employeeId,
                AcceptedByUserId = userId,
                AcceptedByEmailSnapshot = email,
                EulaAttachmentId = currentEula.Id,
                EulaVersion = currentEula.EulaVersion,
                EulaSha256 = currentEula.Sha256,
                AcceptedAt = now
            });
            var detail = $"Çalışan kendi hesabından lisans EULA'sını kabul etti (sürüm {currentEula.EulaVersion}, SHA-256 {currentEula.Sha256}).";
            db.SoftwareLicenseEvents.Add(new SoftwareLicenseEvent
            {
                CompanyId = companyId, LicenseId = license.Id, EmployeeId = employeeId, ActorUserId = userId,
                EventType = "EulaAccepted", Details = detail, OccurredAt = now
            });
            db.AuditEntries.Add(new AuditEntry
            {
                CompanyId = companyId, ActorUserId = userId, TargetType = "SoftwareLicenseAssignment", TargetId = assignment.Id,
                Action = "EmployeeEulaAccepted", Details = detail
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new { accepted = true, assignmentId, eulaAttachmentId = currentEula.Id, eulaVersion = currentEula.EulaVersion, acceptedAt = now });
        }).RequireAuthorization();

        return endpoints;
    }

    private static bool TryScope(ClaimsPrincipal user, out Guid companyId, out Guid userId)
    {
        companyId = Guid.Empty;
        userId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("company_id"), out companyId) &&
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private static async Task<bool> HasValidCsrfAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
