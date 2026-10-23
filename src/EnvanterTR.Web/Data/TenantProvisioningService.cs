using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace EnvanterTR.Web.Data;

public sealed record CloudTenantProvisioningRequest(string CompanyName, string Slug, string ContactEmail,
    string OwnerName, string OwnerEmail, Guid PlanId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool IsTrial = false, string? PaymentReference = null, Guid? CompanyId = null);

public sealed record TenantProvisioningResult(Company Company, EnvanterUser Owner,
    TenantSubscription Subscription, bool OwnerWasConfirmed);

/// <summary>
/// Stages every new company and owner membership in the caller's transaction.
/// The caller commits its payment/audit records with these records, then sends the appropriate notice.
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
            Status = request.IsTrial ? "Trial" : "Active", IsDemo = false, StartsAt = request.StartsAt,
            EndsAt = request.EndsAt, PaymentReference = request.PaymentReference };
        db.TenantSubscriptions.Add(subscription);
        return new(company, owner, subscription, wasConfirmed);
    }

    private void StageCompany(Company company, EnvanterUser owner)
    {
        db.Companies.Add(company);
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = company.Id, UserId = owner.Id, RoleKey = "owner" });
    }

    private void RequireTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Tenant provisioning requires an active database transaction.");
    }
}
