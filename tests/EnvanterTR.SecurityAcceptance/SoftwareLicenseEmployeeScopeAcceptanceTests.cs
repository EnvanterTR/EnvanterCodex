using System.Net;
using System.Text;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Software_license_scope_allows_location_inherited_unit_but_rejects_conflicting_employee_unit()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var inheritedEmployeeId = Guid.NewGuid();
        var conflictingEmployeeId = Guid.NewGuid();
        var assignableEmployeeId = Guid.NewGuid();
        var licenseId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var licenseName = $"Inherited unit license {suffix}";
        var inheritedName = $"Inherited location employee {suffix}";

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Allowed inherited unit {suffix}", NormalizedName = $"ALLOWED INHERITED UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside inherited unit {suffix}", NormalizedName = $"OUTSIDE INHERITED UNIT {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId, CompanyUnitId = allowedUnitId,
                Name = $"Allowed inherited location {suffix}", NormalizedName = $"ALLOWED INHERITED LOCATION {suffix}" });
            db.Employees.AddRange(
                new Employee { Id = inheritedEmployeeId, CompanyId = _companyId, LocationCatalogId = allowedLocationId,
                    EmployeeNumber = $"INHERITED-{suffix}", DisplayName = inheritedName },
                new Employee { Id = conflictingEmployeeId, CompanyId = _companyId, LocationCatalogId = allowedLocationId, CompanyUnitId = outsideUnitId,
                    EmployeeNumber = $"CONFLICT-{suffix}", DisplayName = $"Conflicting unit employee {suffix}" },
                new Employee { Id = assignableEmployeeId, CompanyId = _companyId, LocationCatalogId = allowedLocationId,
                    EmployeeNumber = $"ASSIGNABLE-{suffix}", DisplayName = $"Assignable inherited employee {suffix}" });
            db.SoftwareLicenses.Add(new SoftwareLicense { Id = licenseId, CompanyId = _companyId, Name = licenseName, Seats = 4 });
            db.SoftwareLicenseAssignments.AddRange(
                new SoftwareLicenseAssignment { CompanyId = _companyId, LicenseId = licenseId, EmployeeId = inheritedEmployeeId, ActorUserId = _userId },
                new SoftwareLicenseAssignment { CompanyId = _companyId, LicenseId = licenseId, EmployeeId = conflictingEmployeeId, ActorUserId = _userId });
            var membership = await db.CompanyMemberships.SingleAsync(item => item.CompanyId == _companyId && item.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using (var assignments = await SendAsUserAsync(HttpMethod.Get,
                   $"/api/software-license-assignments?active=true&q={Uri.EscapeDataString(suffix)}", _sameCompanyOtherUserId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, assignments.StatusCode);
            using var payload = JsonDocument.Parse(await assignments.Content.ReadAsStringAsync());
            var item = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(inheritedEmployeeId, item.GetProperty("employeeId").GetGuid());
            Assert.Contains(inheritedName, payload.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.DoesNotContain("CONFLICT-" + suffix, payload.RootElement.GetRawText(), StringComparison.Ordinal);
        }

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var blockedImport = await SendAsUserAsync(HttpMethod.Post, "/api/software-licenses/import.csv",
                   _sameCompanyOtherUserId, _companyId, new StringContent("", Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, blockedImport.StatusCode);
        using (var allowed = await SendAsUserAsync(HttpMethod.Post, $"/api/software-licenses/{licenseId}/assign",
                   _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(new { employeeId = assignableEmployeeId,
                       assetId = (Guid?)null, eulaAccepted = false, notes = "" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        using (var denied = await SendAsUserAsync(HttpMethod.Post, $"/api/software-licenses/{licenseId}/assign",
                   _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(new { employeeId = conflictingEmployeeId,
                       assetId = (Guid?)null, eulaAccepted = false, notes = "" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
    }
}
