using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static partial class PlatformEndpoints
{
    private static void MapCompanyDeletionEndpoints(RouteGroupBuilder admin)
    {
        admin.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            if ((HttpMethods.IsPost(http.Request.Method) || HttpMethods.IsPut(http.Request.Method) ||
                 HttpMethods.IsPatch(http.Request.Method)) &&
                !http.Request.Path.Value!.TrimEnd('/').EndsWith("/restore", StringComparison.Ordinal) &&
                Guid.TryParse(http.GetRouteValue("companyId")?.ToString(), out var companyId))
            {
                var db = http.RequestServices.GetRequiredService<AppDbContext>();
                if (await db.Companies.AsNoTracking().AnyAsync(company => company.Id == companyId &&
                        company.DeletedAt != null, http.RequestAborted))
                    return Results.Conflict(new { message = "Silinen şirketi önce çöp kutusundan geri alın." });
            }
            return await next(context);
        });
        // The platform group and request middleware enforce root MFA freshness and CSRF.
        // Never delete the shared Identity account, memberships, media or financial history.
        admin.MapDelete("/companies/{companyId:guid}", async (Guid companyId,
            [FromBody] DeleteCompanyRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length is < 8 or > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Silme için 8-1000 karakterlik gerekçe gereklidir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(company.Slug) ||
                !string.Equals(request.ConfirmSlug?.Trim(), company.Slug, StringComparison.Ordinal))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["confirmSlug"] = ["Onay için şirketin alt alan adını birebir yazın."] });
            if (company.DeletedAt is not null)
                return Results.Ok(new { company.Id, company.DeletedAt, company.IsActive, unchanged = true });

            var now = DateTimeOffset.UtcNow;
            company.DeletedAt = now;
            company.IsActive = false;
            var sessions = await db.UserSessions.Where(item => item.CompanyId == companyId && item.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.RevokedAt, now), cancellationToken);
            var apiTokens = await db.CompanyApiTokens.Where(item => item.CompanyId == companyId && item.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.RevokedAt, now), cancellationToken);
            AddAudit(db, user, "CompanySoftDeleted", "Company", companyId, companyId,
                JsonSerializer.Serialize(new { reason = request.Note.Trim(), company.Slug, sessions, apiTokens,
                    recoverable = true, billingChanged = false }));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { company.Id, company.DeletedAt, company.IsActive, unchanged = false,
                revokedSessions = sessions, revokedApiTokens = apiTokens, billingChanged = false });
        });

        admin.MapPost("/companies/{companyId:guid}/restore", async (Guid companyId,
            [FromBody] RestoreCompanyRequest request, ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length is < 8 or > 1000)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["Geri alma için 8-1000 karakterlik gerekçe gereklidir."] });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound();
            if (company.DeletedAt is null)
                return Results.Ok(new { company.Id, company.IsActive, unchanged = true });
            var deletedAt = company.DeletedAt;
            company.DeletedAt = null;
            // Restoration never revives old sessions/tokens or resumes access/billing silently.
            company.IsActive = false;
            AddAudit(db, user, "CompanyRestored", "Company", companyId, companyId,
                JsonSerializer.Serialize(new { reason = request.Note.Trim(), deletedAt, billingChanged = false }));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(new { company.Id, company.IsActive, unchanged = false, billingChanged = false });
        });
    }
}
