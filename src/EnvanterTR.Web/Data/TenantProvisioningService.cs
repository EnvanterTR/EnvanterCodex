using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnvanterTR.Web.Data;

public sealed record CloudTenantProvisioningRequest(string CompanyName, string Slug, string ContactEmail,
    string OwnerName, string OwnerEmail, Guid PlanId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool IsTrial = false, string? PaymentReference = null, Guid? CompanyId = null, bool IsDemo = false);

public sealed record TenantProvisioningResult(Company Company, EnvanterUser Owner,
    TenantSubscription Subscription, bool OwnerWasConfirmed);

/// <summary>
/// Stages every new company and owner membership in the caller's transaction.
/// The caller owns the payment/audit transaction and notification ordering.
/// Public paid provisioning requires SMTP acceptance before committing a newly staged portal.
/// </summary>
public sealed class TenantProvisioningService(AppDbContext db, UserManager<EnvanterUser> users)
{
    public Company ProvisionInitialCompany(string companyName, EnvanterUser owner)
    {
        RequireTransaction();
        var company = new Company { Name = companyName };
        StageCompany(company, owner);
        return company;
    }

    public async Task<TenantProvisioningResult?> ProvisionCloudAsync(CloudTenantProvisioningRequest request,
        CancellationToken cancellationToken)
    {
        RequireTransaction();
        if (!TenantHostResolver.IsValidSlug(request.Slug) || request.PlanId == Guid.Empty ||
            request.CompanyId == Guid.Empty || request.EndsAt <= request.StartsAt)
            throw new ArgumentException("Tenant provisioning requires a valid portal, plan and subscription period.", nameof(request));
        cancellationToken.ThrowIfCancellationRequested();
        if (request.IsDemo && !string.IsNullOrWhiteSpace(request.PaymentReference))
            throw new ArgumentException("A demo cannot contain a paid payment reference.", nameof(request));
        var owner = await users.FindByEmailAsync(request.OwnerEmail);
        var wasConfirmed = owner?.EmailConfirmed == true;
        if (owner is null)
        {
            owner = new EnvanterUser { UserName = request.OwnerEmail, Email = request.OwnerEmail,
                EmailConfirmed = false, DisplayName = request.OwnerName };
            var created = await users.CreateAsync(owner, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!");
            if (!created.Succeeded) return null;
        }

        var company = new Company { Id = request.CompanyId ?? Guid.NewGuid(), Name = request.CompanyName,
            Slug = request.Slug, ContactEmail = request.ContactEmail, SecurityRequireMfa = true };
        StageCompany(company, owner);
        var subscription = new TenantSubscription { CompanyId = company.Id, PlanId = request.PlanId,
            Status = request.IsTrial || request.IsDemo ? "Trial" : "Active", IsDemo = request.IsDemo, StartsAt = request.StartsAt,
            EndsAt = request.EndsAt, PaymentReference = request.PaymentReference };
        db.TenantSubscriptions.Add(subscription);
        return new(company, owner, subscription, wasConfirmed);
    }

    private void StageCompany(Company company, EnvanterUser owner)
    {
        db.Companies.Add(company);
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = company.Id, UserId = owner.Id, RoleKey = "owner" });
    }

    public async Task<bool> CanPurchaseAnnualPlanAsync(Guid companyId, SubscriptionPlan plan, CancellationToken ct)
    {
        var current = await (from subscription in db.TenantSubscriptions.AsNoTracking()
            join currentPlan in db.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals currentPlan.Id
            where subscription.CompanyId == companyId
            orderby subscription.Status == "Active" || subscription.Status == "Trial" ? 0 : 1, subscription.StartsAt descending
            select new { subscription.Status, subscription.PlanId, subscription.EndsAt, currentPlan.DeploymentKind }).FirstOrDefaultAsync(ct);
        if (plan.DeploymentKind != "cloud" || current is null || current.DeploymentKind != "cloud" || current.Status == "Suspended") return false;
        // Preserve an unexpired paid period: renew its plan rather than silently replacing it.
        if (current.Status is "Active" or "Cancelled" && current.EndsAt > DateTimeOffset.UtcNow && current.PlanId != plan.Id) return false;
        if (plan.MaxLoginUsers is int seats && await db.CompanyMemberships.CountAsync(member => member.CompanyId == companyId, ct) > seats) return false;
        return plan.MaxAssets is not int assets || await db.Assets.CountAsync(asset => asset.CompanyId == companyId && asset.DeletedAt == null, ct) <= assets;
    }

    public async Task<Company?> ActivateExistingCompanyAsync(CompanyCheckoutOrder order, SubscriptionPlan plan, CancellationToken ct)
    {
        RequireTransaction();
        if (order.TargetCompanyId is not Guid companyId || order.RequestedByUserId is not Guid ownerId || order.TermDays != 365)
            throw new ArgumentException("Existing-company checkout context is incomplete.", nameof(order));
        var company = await db.Companies.FromSqlInterpolated($"SELECT * FROM \"Companies\" WHERE \"Id\" = {companyId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (company is null || !company.IsActive || !await db.CompanyMemberships.AnyAsync(member =>
                member.CompanyId == companyId && member.UserId == ownerId && member.RoleKey == "owner" &&
                db.Users.Any(account => account.Id == ownerId && account.EmailConfirmed), ct) ||
            !await CanPurchaseAnnualPlanAsync(companyId, plan, ct)) return null;
        var now = DateTimeOffset.UtcNow;
        var previous = await db.TenantSubscriptions.Where(subscription => subscription.CompanyId == companyId &&
            (subscription.Status == "Active" || subscription.Status == "Trial" || subscription.Status == "Cancelled"))
            .OrderByDescending(subscription => subscription.EndsAt).FirstOrDefaultAsync(ct);
        var paidUntil = previous is { Status: "Active" or "Cancelled" } && previous.PlanId == plan.Id && previous.EndsAt > now
            ? previous.EndsAt : now;
        await db.TenantSubscriptions.Where(subscription => subscription.CompanyId == companyId &&
            (subscription.Status == "Trial" || subscription.Status == "Active"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(subscription => subscription.Status,
                subscription => subscription.EndsAt <= now ? "Expired" : "Cancelled"), ct);
        var activated = new TenantSubscription { CompanyId = companyId, PlanId = plan.Id, Status = "Active", IsDemo = false,
            StartsAt = now, EndsAt = paidUntil.AddDays(365), PaymentReference = order.PaymentReference };
        db.TenantSubscriptions.Add(activated);
        db.AuditEntries.Add(new AuditEntry { CompanyId = companyId, ActorUserId = ownerId, Action = "PaidSubscriptionActivated",
            TargetType = "TenantSubscription", TargetId = activated.Id, Details = System.Text.Json.JsonSerializer.Serialize(new { checkoutId = order.Id, planId = plan.Id }) });
        return company;
    }

    private void RequireTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Tenant provisioning requires an active database transaction.");
    }
}
