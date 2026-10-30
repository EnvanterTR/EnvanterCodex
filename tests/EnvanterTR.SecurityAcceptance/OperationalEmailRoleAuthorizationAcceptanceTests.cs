using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData("built-in")]
    [InlineData("custom-permission")]
    [InlineData("custom-inactive")]
    public async Task Operational_email_rechecks_current_inventory_read_permission(string change)
    {
        var categoryId = await GetOwnCategoryIdAsync();
        var roleKey = "email-" + Guid.NewGuid().ToString("N")[..12];
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            membership.RoleKey = change == "built-in" ? "hr_operator" : roleKey;
            if (change != "built-in")
                db.CompanyCustomRoles.Add(new CompanyCustomRole
                {
                    CompanyId = _companyId, RoleKey = roleKey, Name = roleKey, NormalizedName = roleKey.ToUpperInvariant(),
                    PermissionsJson = change == "custom-inactive" ? "[\"assets.read\"]" : "[\"people.read\"]",
                    IsActive = change != "custom-inactive", CreatedByUserId = _userId
                });
            db.Assets.Add(new Asset
            {
                CompanyId = _companyId, CategoryId = categoryId, AssetTag = roleKey, Name = "Private warranty reminder",
                Status = AssetStatuses.Available, WarrantyExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3)
            });
            db.OperationalNotificationPreferences.Add(new OperationalNotificationPreference
            {
                CompanyId = _companyId, UserId = _userId, EnabledKindsJson = "[]", EmailEnabledKindsJson = "[\"warranty\"]"
            });
            await db.SaveChangesAsync();
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OperationalNotificationEmailService>().RunOnceAsync(CancellationToken.None);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(_smtpCapture.Messages);
            Assert.False(await db.OperationalNotificationEmailDigests.AnyAsync(x => x.CompanyId == _companyId && x.UserId == _userId));
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            membership.RoleKey = "viewer";
            await db.SaveChangesAsync();
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<OperationalNotificationEmailService>();
            await service.RunOnceAsync(CancellationToken.None);
            await service.RunOnceAsync(CancellationToken.None);
            Assert.Single(_smtpCapture.Messages);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var digest = await db.OperationalNotificationEmailDigests.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            Assert.Equal("Sent", digest.Status);
            Assert.Equal(1, digest.AttemptCount);
        }
    }
}
