using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Employee_linked_notification_states_require_direct_company_unit_access()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var licenseId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Notification allowed {suffix}", NormalizedName = $"NOTIFICATION ALLOWED {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Notification outside {suffix}", NormalizedName = $"NOTIFICATION OUTSIDE {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = locationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = $"Notification location {suffix}", NormalizedName = $"NOTIFICATION LOCATION {suffix}" });
            db.Employees.Add(new Employee { Id = employeeId, CompanyId = _companyId,
                EmployeeNumber = $"NOTIFICATION-EMP-{suffix}", DisplayName = $"Notification outside employee {suffix}",
                LocationCatalogId = locationId, CompanyUnitId = outsideUnitId });
            var existing = await db.Assets.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            db.Assets.Add(new Asset { Id = assetId, CompanyId = _companyId, CategoryId = existing.CategoryId,
                AssetTag = $"NOTIFICATION-ASSET-{suffix}", Name = "Due return outside employee scope",
                Status = AssetStatuses.Deployed, LocationCatalogId = locationId, AssignedEmployeeId = employeeId,
                ExpectedReturnOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) });
            db.SoftwareLicenses.Add(new SoftwareLicense { Id = licenseId, CompanyId = _companyId,
                Name = $"Notification license {suffix}", Seats = 1 });
            db.SoftwareLicenseAssignments.Add(new SoftwareLicenseAssignment { CompanyId = _companyId,
                LicenseId = licenseId, EmployeeId = employeeId, ActorUserId = _sameCompanyOtherUserId });
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.AssetReservations.Add(new AssetReservation { Id = reservationId, CompanyId = _companyId,
                AssetId = assetId, EmployeeId = employeeId, RequestedByUserId = _sameCompanyOtherUserId,
                StartsOn = today.AddDays(1), EndsOn = today.AddDays(3), Status = "Approved" });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.RemoveRange(await db.CompanyMembershipCompanyUnitAccesses
                .Where(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId).ToListAsync());
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        foreach (var (kind, recordId) in new[] { ("return", assetId), ("license", licenseId), ("reservation", reservationId) })
        {
            using var hidden = await SendAsUserAsync(HttpMethod.Put, "/api/notifications/states", _sameCompanyOtherUserId, _companyId,
                System.Net.Http.Json.JsonContent.Create(new { kind, recordId, action = "dismiss" }), requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        }
        await using (var rejected = _factory!.Services.CreateAsyncScope())
        {
            var db = rejected.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(await db.OperationalNotificationStates.Where(x => x.CompanyId == _companyId &&
                x.UserId == _sameCompanyOtherUserId && (x.RecordId == assetId || x.RecordId == licenseId || x.RecordId == reservationId)).ToListAsync());
        }

        await using (var grantUnit = _factory!.Services.CreateAsyncScope())
        {
            var db = grantUnit.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = await db.Employees.SingleAsync(x => x.CompanyId == _companyId && x.Id == employeeId);
            employee.CompanyUnitId = allowedUnitId;
            await db.SaveChangesAsync();
        }
        foreach (var (kind, recordId) in new[] { ("return", assetId), ("license", licenseId), ("reservation", reservationId) })
        {
            using var allowed = await SendAsUserAsync(HttpMethod.Put, "/api/notifications/states", _sameCompanyOtherUserId, _companyId,
                System.Net.Http.Json.JsonContent.Create(new { kind, recordId, action = "dismiss" }), requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        await using (var accepted = _factory!.Services.CreateAsyncScope())
        {
            var db = accepted.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(3, await db.OperationalNotificationStates.CountAsync(x => x.CompanyId == _companyId &&
                x.UserId == _sameCompanyOtherUserId && x.DismissedAt != null &&
                (x.RecordId == assetId || x.RecordId == licenseId || x.RecordId == reservationId)));
            var employee = await db.Employees.SingleAsync(x => x.CompanyId == _companyId && x.Id == employeeId);
            employee.CompanyUnitId = outsideUnitId;
            await db.SaveChangesAsync();
        }

        var stateKeys = new[] { $"return:{assetId:D}", $"license:{licenseId:D}", $"reservation:{reservationId:D}" };
        var statePath = "/api/notifications/states?" + string.Join("&", stateKeys.Select(key => "key=" + Uri.EscapeDataString(key)));
        using (var hiddenStates = await SendAsUserAsync(HttpMethod.Get, statePath, _sameCompanyOtherUserId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, hiddenStates.StatusCode);
            using var payload = JsonDocument.Parse(await hiddenStates.Content.ReadAsStringAsync());
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
        }
    }
}
