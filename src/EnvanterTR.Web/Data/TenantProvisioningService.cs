using Microsoft.AspNetCore.Identity;

namespace EnvanterTR.Web.Data;

/// <summary>
/// Creates the tenant, its initial owner membership, and subscription as one shared
/// provisioning operation. Callers own the transaction and any endpoint-specific audit.
/// </summary>
public sealed class TenantProvisioningService(
    AppDbContext db,
    UserManager<EnvanterUser> userManager)
{
    public async Task<TenantProvisioningResult> ProvisionCloudCompanyAsync(
        string companyName,
        string slug,
        string? contactEmail,
        string ownerName,
        string ownerEmail,
        SubscriptionPlan plan,
        int termDays,
        string paymentReference,
        DateTimeOffset startsAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.IsActive || !string.Equals(plan.DeploymentKind, "cloud", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("An active Cloud plan is required.", nameof(plan));
        if (termDays is < 1 or > 1095)
            throw new ArgumentOutOfRangeException(nameof(termDays));

        var owner = await userManager.FindByEmailAsync(ownerEmail);
        var ownerEmailConfirmed = owner?.EmailConfirmed == true;
        if (owner is null)
        {
            var temporaryPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) + "aA1!";
            owner = new EnvanterUser
            {
                UserName = ownerEmail,
                Email = ownerEmail,
                EmailConfirmed = false,
                DisplayName = ownerName
            };
            var created = await userManager.CreateAsync(owner, temporaryPassword);
            if (!created.Succeeded)
                throw new TenantOwnerCreationException();
        }

        var company = new Company
        {
            Name = companyName,
            Slug = slug,
            ContactEmail = contactEmail ?? ownerEmail,
            SecurityRequireMfa = true
        };
        var subscription = new TenantSubscription
        {
            CompanyId = company.Id,
            PlanId = plan.Id,
            Status = "Active",
            IsDemo = false,
            StartsAt = startsAt,
            EndsAt = startsAt.AddDays(termDays),
            PaymentReference = paymentReference
        };

        AddCompanyOwner(company, owner);
        db.TenantSubscriptions.Add(subscription);

        return new TenantProvisioningResult(company, owner, subscription, ownerEmailConfirmed);
    }

    public TenantProvisioningResult ProvisionInitialCompany(
        Company company,
        EnvanterUser owner)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(owner);
        AddCompanyOwner(company, owner);
        return new TenantProvisioningResult(company, owner, null, owner.EmailConfirmed);
    }

    private void AddCompanyOwner(Company company, EnvanterUser owner)
    {
        db.Companies.Add(company);
        db.CompanyMemberships.Add(new CompanyMembership
        {
            CompanyId = company.Id,
            UserId = owner.Id,
            RoleKey = "owner"
        });
    }
}

public sealed record TenantProvisioningResult(
    Company Company,
    EnvanterUser Owner,
    TenantSubscription? Subscription,
    bool OwnerEmailConfirmed);

public sealed class TenantOwnerCreationException : Exception { }
