using System.Net;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Operational_email_respects_direct_units_and_explicit_locations_for_employee_relationships(bool restrictLocations)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefixes = new[] { $"DIRECT-{suffix}", $"INHERITED-{suffix}", $"HIDDEN-{suffix}" };
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CompanyUnits.AddRange(
            new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Allowed {suffix}", NormalizedName = $"ALLOWED {suffix}" },
            new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside {suffix}", NormalizedName = $"OUTSIDE {suffix}" });
        db.CompanyLocations.Add(new CompanyLocation { Id = locationId, CompanyId = _companyId,
            CompanyUnitId = allowedUnitId, Name = $"Email {suffix}", NormalizedName = $"EMAIL {suffix}" });
        var membership = await db.CompanyMemberships.SingleAsync(item => item.CompanyId == _companyId && item.UserId == _userId);
        membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
        membership.LocationAccessMode = restrictLocations ? CompanyLocationAccess.Restricted : CompanyLocationAccess.All;
        db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
            { CompanyId = _companyId, UserId = _userId, CompanyUnitId = allowedUnitId });
        if (restrictLocations)
            db.CompanyMembershipLocationAccesses.Add(new CompanyMembershipLocationAccess
                { CompanyId = _companyId, UserId = _userId, LocationId = locationId });
        var categoryId = await GetOwnCategoryIdAsync();
        for (var index = 0; index < prefixes.Length; index++)
        {
            var prefix = prefixes[index];
            var employee = new Employee { CompanyId = _companyId, DisplayName = prefix, EmployeeNumber = prefix,
                LocationCatalogId = index == 0 ? null : locationId,
                CompanyUnitId = index == 0 ? allowedUnitId : index == 2 ? outsideUnitId : null };
            db.Employees.Add(employee);
            foreach (var days in new[] { 30, 15, 7, 3, 1 })
            {
                var license = new SoftwareLicense { CompanyId = _companyId, Name = $"{prefix}-LICENSE-{days}",
                    Seats = 1, ExpirationDate = today.AddDays(days) };
                db.SoftwareLicenses.Add(license);
                db.SoftwareLicenseAssignments.Add(new SoftwareLicenseAssignment { CompanyId = _companyId,
                    LicenseId = license.Id, EmployeeId = employee.Id, ActorUserId = _userId });
            }
            foreach (var kind in new[] { "RETURN", "RESERVATION", "ASSIGNMENT" })
            {
                var asset = new Asset { CompanyId = _companyId, CategoryId = categoryId, AssetTag = $"{prefix}-{kind}",
                    Name = $"{kind} fixture", LocationCatalogId = locationId,
                    Status = kind == "RETURN" ? AssetStatuses.Deployed : AssetStatuses.Available,
                    AssignedEmployeeId = kind == "RETURN" ? employee.Id : null,
                    ExpectedReturnOn = kind == "RETURN" ? today.AddDays(index == 0 ? -2 : 2) : null };
                db.Assets.Add(asset);
                if (kind == "RESERVATION")
                    db.AssetReservations.Add(new AssetReservation { CompanyId = _companyId, AssetId = asset.Id,
                        EmployeeId = employee.Id, RequestedByUserId = _userId, Status = "Approved",
                        StartsOn = today.AddDays(1), EndsOn = today.AddDays(2) });
                if (kind == "ASSIGNMENT")
                    db.AssetEvents.Add(new AssetEvent { CompanyId = _companyId, AssetId = asset.Id,
                        EmployeeId = employee.Id, ActorUserId = _userId, EventType = "Assigned", OccurredAt = DateTimeOffset.UtcNow });
            }
        }
        db.OperationalNotificationPreferences.Add(new OperationalNotificationPreference { CompanyId = _companyId, UserId = _userId,
            EnabledKindsJson = "[]", EmailEnabledKindsJson = "[\"license\",\"return\",\"reservation\",\"assignment\"]" });
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<OperationalNotificationEmailService>();
        await service.RunOnceAsync(CancellationToken.None);
        await service.RunOnceAsync(CancellationToken.None);
        var recipient = $"acceptance-{_userId:N}@example.invalid";
        var message = Assert.Single(_smtpCapture.Messages, item => item.Contains($"To: {recipient}", StringComparison.OrdinalIgnoreCase));
        var body = WebUtility.HtmlDecode(DecodeMimeBody(message));
        foreach (var marker in new[] { "LICENSE-30", "LICENSE-15", "LICENSE-7", "LICENSE-3", "LICENSE-1", "RETURN", "RESERVATION", "ASSIGNMENT" })
        {
            Assert.Contains($"{prefixes[1]}-{marker}", body, StringComparison.Ordinal);
            Assert.DoesNotContain($"{prefixes[2]}-{marker}", body, StringComparison.Ordinal);
            if (restrictLocations) Assert.DoesNotContain($"{prefixes[0]}-{marker}", body, StringComparison.Ordinal);
            else Assert.Contains($"{prefixes[0]}-{marker}", body, StringComparison.Ordinal);
        }
        var digest = await db.OperationalNotificationEmailDigests.SingleAsync(item => item.CompanyId == _companyId && item.UserId == _userId);
        Assert.Equal("Sent", digest.Status);
        Assert.Equal(1, digest.AttemptCount);
    }
}
