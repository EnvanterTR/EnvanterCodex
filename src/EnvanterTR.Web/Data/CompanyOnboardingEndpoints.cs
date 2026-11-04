using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public static class CompanyOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapCompanyOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/onboarding/status", async (ClaimsPrincipal user, AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("company_id"), out var companyId))
                return Results.Forbid();

            var company = await db.Companies.AsNoTracking()
                .Where(item => item.Id == companyId && item.IsActive && item.DeletedAt == null)
                .Select(item => new { item.Name, item.Slug, item.ContactEmail, item.BrandLogoPath,
                    item.BrandPrimaryColor, item.CreatedAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (company is null) return Results.NotFound();

            var imported = await db.AuditEntries.AsNoTracking().AnyAsync(entry => entry.CompanyId == companyId &&
                (entry.Action == "CsvImported" || entry.Action == "XlsxImported"), cancellationToken);
            var memberCount = await db.CompanyMemberships.AsNoTracking()
                .CountAsync(member => member.CompanyId == companyId, cancellationToken);
            var assetCount = await db.Assets.AsNoTracking().CountAsync(asset => asset.CompanyId == companyId,
                cancellationToken);

            var steps = new[]
            {
                new { key = "company", title = "Firma bilgileri", complete = !string.IsNullOrWhiteSpace(company.Name) &&
                    !string.IsNullOrWhiteSpace(company.Slug) && !string.IsNullOrWhiteSpace(company.ContactEmail),
                    page = "Firma profili" },
                new { key = "branding", title = "Logo ve renk", complete = !string.IsNullOrWhiteSpace(company.BrandLogoPath) ||
                    !string.Equals(company.BrandPrimaryColor, "#1769E0", StringComparison.OrdinalIgnoreCase),
                    page = "Logo ve tema" },
                new { key = "import", title = "İlk toplu veri aktarımı", complete = imported, page = "Varlıklar" },
                new { key = "members", title = "Ekip üyelerini davet edin", complete = memberCount > 1,
                    page = "Uygulama kullanıcıları" },
                new { key = "firstAsset", title = "İlk varlığı kaydedin", complete = assetCount > 0, page = "Varlıklar" }
            };
            var completedSteps = steps.Count(step => step.complete);
            var visible = completedSteps < steps.Length && company.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-30);
            return Results.Ok(new { visible, completedSteps, totalSteps = steps.Length, steps });
        }).RequireAuthorization("settings.manage");

        return endpoints;
    }
}
