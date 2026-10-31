using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Shared_tenant_provisioning_requires_transaction_and_rolls_back_all_new_records()
    {
        var plan = await SeedTrialPlan();
        var companyId = Guid.NewGuid();
        var email = $"rollback-{companyId:N}@example.invalid";
        var now = DateTimeOffset.UtcNow;
        var request = new CloudTenantProvisioningRequest("Rollback company", $"rollback-{companyId:N}", email,
            "Rollback owner", email, plan.Id, now, now.AddDays(15), IsTrial: true, CompanyId: companyId);
        Guid ownerId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => provisioning.ProvisionCloudAsync(request, CancellationToken.None));
            Assert.False(await db.Users.AnyAsync(user => user.Email == email));

            await using var transaction = await db.Database.BeginTransactionAsync();
            var provisioned = await provisioning.ProvisionCloudAsync(request, CancellationToken.None);
            Assert.NotNull(provisioned);
            ownerId = provisioned.Owner.Id;
            await db.SaveChangesAsync();
            Assert.True(await db.Companies.AnyAsync(company => company.Id == companyId && company.SecurityRequireMfa));
            Assert.True(await db.CompanyMemberships.AnyAsync(member => member.CompanyId == companyId && member.UserId == ownerId && member.RoleKey == "owner"));
            Assert.True(await db.TenantSubscriptions.AnyAsync(subscription => subscription.CompanyId == companyId && subscription.Status == "Trial"));
            await transaction.RollbackAsync();
        }
        await using var verification = _factory!.Services.CreateAsyncScope();
        var saved = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await saved.Users.AnyAsync(user => user.Id == ownerId));
        Assert.False(await saved.Companies.AnyAsync(company => company.Id == companyId));
        Assert.False(await saved.CompanyMemberships.AnyAsync(member => member.CompanyId == companyId));
        Assert.False(await saved.TenantSubscriptions.AnyAsync(subscription => subscription.CompanyId == companyId));
    }

    [Fact]
    public async Task Shared_initial_company_provisioning_preserves_owner_and_creates_no_subscription()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
        var owner = await db.Users.SingleAsync(user => user.Id == _userId);
        var passwordHash = owner.PasswordHash;
        var securityStamp = owner.SecurityStamp;
        Assert.Throws<InvalidOperationException>(() => provisioning.ProvisionInitialCompany("Initial company", owner));
        await using var transaction = await db.Database.BeginTransactionAsync();
        var company = provisioning.ProvisionInitialCompany("Initial company", owner);
        await db.SaveChangesAsync();
        Assert.True(await db.Companies.AnyAsync(item => item.Id == company.Id));
        var membership = await db.CompanyMemberships.SingleAsync(item => item.CompanyId == company.Id);
        Assert.Equal(owner.Id, membership.UserId);
        Assert.Equal("owner", membership.RoleKey);
        Assert.False(await db.TenantSubscriptions.AnyAsync(subscription => subscription.CompanyId == company.Id));
        await db.Entry(owner).ReloadAsync();
        Assert.Equal(passwordHash, owner.PasswordHash);
        Assert.Equal(securityStamp, owner.SecurityStamp);
        await transaction.RollbackAsync();
    }
}
