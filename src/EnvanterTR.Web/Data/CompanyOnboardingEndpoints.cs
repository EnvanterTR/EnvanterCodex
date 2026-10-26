using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

// Mapped inside OperationalEndpoints: active membership, CSRF and normal subscription guards apply.
public static class CompanyOnboardingEndpoints
{
    private const string CompletedAction = "CompanyOnboardingCompleted";

    public static void MapCompanyOnboardingEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/company/onboarding", async (ClaimsPrincipal user, AppDbContext db, HttpContext context,
            CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId)) return Results.Forbid();
            var state = await ReadAsync(db, companyId, ct);
            return state is null ? Results.NotFound() : Results.Ok(state);
        }).RequireAuthorization("settings.manage");

        api.MapPost("/company/onboarding/complete", async (ClaimsPrincipal user, AppDbContext db,
            HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId) ||
                !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Forbid();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            // Serializes completion/replay without adding a new table or changing customer records.
            var company = await db.Companies.FromSqlInterpolated(
                $"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (company is null) return Results.NotFound();
            var state = await ReadAsync(db, companyId, ct);
            if (state is null) return Results.NotFound();
            if (state.CompletedAt is not null) return Results.Ok(state);
            if (!state.Ready) return Results.Conflict(new { message = "Kurulumu tamamlamak için beş adımın kayıtlarını oluşturun.", state });
            var entry = new AuditEntry { CompanyId = companyId, ActorUserId = actorId, TargetType = "Company",
                TargetId = companyId, Action = CompletedAction, Details = "Beş adımlı şirket kurulumu gerçek kayıtlarla tamamlandı." };
            db.AuditEntries.Add(entry);
            await db.SaveChangesAsync(ct);
            // PostgreSQL stores microseconds; return its persisted timestamp so replay is identical.
            var completedAt = await db.AuditEntries.Where(a => a.Id == entry.Id)
                .Select(a => (DateTimeOffset?)a.OccurredAt).SingleAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(state with { CompletedAt = completedAt });
        }).RequireAuthorization("settings.manage");
    }

    private static async Task<OnboardingState?> ReadAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        // One SQL snapshot: another tenant, archived asset or inactive employee cannot complete a step.
        var row = await db.Companies.AsNoTracking().Where(c => c.Id == companyId && c.IsActive && c.DeletedAt == null)
            .Select(c => new
            {
                Profile = c.Name != "" && c.Slug != null && c.Slug != "" && c.ContactEmail != null && c.ContactEmail != "",
                Location = db.CompanyLocations.Any(l => l.CompanyId == companyId && l.IsActive),
                Employee = db.Employees.Any(e => e.CompanyId == companyId && e.IsActive),
                Asset = db.Assets.Any(a => a.CompanyId == companyId && a.DeletedAt == null && a.Status != AssetStatuses.Archived),
                Assignment = db.Assets.Any(a => a.CompanyId == companyId && a.DeletedAt == null &&
                    a.Status == AssetStatuses.Deployed && db.Employees.Any(e => e.CompanyId == companyId &&
                        e.Id == a.AssignedEmployeeId && e.IsActive)),
                CompletedAt = db.AuditEntries.Where(a => a.CompanyId == companyId && a.TargetId == companyId &&
                    a.Action == CompletedAction).Select(a => (DateTimeOffset?)a.OccurredAt).Min()
            }).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        return new OnboardingState([
            new("profile", row.Profile), new("location", row.Location), new("employee", row.Employee),
            new("asset", row.Asset), new("assignment", row.Assignment)
        ], row.CompletedAt);
    }

    public sealed record OnboardingStep(string Key, bool Ready);
    public sealed record OnboardingState(IReadOnlyList<OnboardingStep> Steps, DateTimeOffset? CompletedAt)
    {
        public int ReadyCount => Steps.Count(s => s.Ready);
        public bool Ready => ReadyCount == 5;
    }
}
