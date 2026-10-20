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
    public async Task Dashboard_employee_count_applies_direct_company_unit_scope_and_includes_locationless_allowed_employees()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var allowedLocatedEmployeeId = Guid.NewGuid();
        var allowedLocationlessEmployeeId = Guid.NewGuid();
        var outsideUnitEmployeeId = Guid.NewGuid();

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Dashboard allowed {suffix}", NormalizedName = $"DASHBOARD ALLOWED {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Dashboard outside {suffix}", NormalizedName = $"DASHBOARD OUTSIDE {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = $"Dashboard location {suffix}", NormalizedName = $"DASHBOARD LOCATION {suffix}" });
            db.Employees.AddRange(
                new Employee { Id = allowedLocatedEmployeeId, CompanyId = _companyId, CompanyUnitId = allowedUnitId,
                    LocationCatalogId = allowedLocationId, EmployeeNumber = $"DASH-ALLOWED-LOC-{suffix}", DisplayName = $"Dashboard allowed located {suffix}" },
                new Employee { Id = allowedLocationlessEmployeeId, CompanyId = _companyId, CompanyUnitId = allowedUnitId,
                    EmployeeNumber = $"DASH-ALLOWED-NOLOC-{suffix}", DisplayName = $"Dashboard allowed locationless {suffix}" },
                new Employee { Id = outsideUnitEmployeeId, CompanyId = _companyId, CompanyUnitId = outsideUnitId,
                    LocationCatalogId = allowedLocationId, EmployeeNumber = $"DASH-OUTSIDE-UNIT-{suffix}", DisplayName = $"Dashboard hidden conflicting unit {suffix}" });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        int expectedActiveEmployees;
        await using (var verifyScope = _factory!.Services.CreateAsyncScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessScope = await CompanyLocationAccess.GetEmployeeAccessScopeAsync(db, _companyId,
                _sameCompanyOtherUserId, CancellationToken.None);
            expectedActiveEmployees = await accessScope.Apply(db.Employees.AsNoTracking().Where(x => x.CompanyId == _companyId))
                .CountAsync(x => x.IsActive);
            Assert.True(expectedActiveEmployees >= 2);
        }

        using var response = await SendAsUserAsync(HttpMethod.Get, "/api/dashboard/summary", _sameCompanyOtherUserId, _companyId, role: "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedActiveEmployees, payload.RootElement.GetProperty("activeEmployees").GetInt32());
    }
}
