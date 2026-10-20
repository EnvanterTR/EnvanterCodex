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
    public async Task Audit_and_activity_employee_records_follow_direct_company_unit_scope()
    {
        var allowedUnitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var allowedLocationId = Guid.NewGuid();
        var allowedEmployeeId = Guid.NewGuid();
        var outsideEmployeeId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var allowedAuditId = Guid.NewGuid();
        var outsideAuditId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var actionPrefix = $"ScopeAudit-{suffix}";

        await using (var setup = _factory!.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = allowedUnitId, CompanyId = _companyId, Name = $"Audit allowed {suffix}", NormalizedName = $"AUDIT ALLOWED {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Audit outside {suffix}", NormalizedName = $"AUDIT OUTSIDE {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = allowedLocationId, CompanyId = _companyId,
                CompanyUnitId = allowedUnitId, Name = $"Audit location {suffix}", NormalizedName = $"AUDIT LOCATION {suffix}" });
            db.AssetCategories.Add(new AssetCategory { Id = categoryId, CompanyId = _companyId, Name = $"Audit category {suffix}" });
            db.Employees.AddRange(
                new Employee { Id = allowedEmployeeId, CompanyId = _companyId, CompanyUnitId = allowedUnitId,
                    EmployeeNumber = $"AUDIT-ALLOWED-{suffix}", DisplayName = $"Audit allowed locationless {suffix}" },
                new Employee { Id = outsideEmployeeId, CompanyId = _companyId, CompanyUnitId = outsideUnitId,
                    LocationCatalogId = allowedLocationId, EmployeeNumber = $"AUDIT-OUTSIDE-{suffix}", DisplayName = $"Audit unauthorized unit {suffix}" });
            db.Assets.Add(new Asset { Id = assetId, CompanyId = _companyId, CategoryId = categoryId,
                AssetTag = $"AUDIT-ASSET-{suffix}", Name = $"Audit asset {suffix}", LocationCatalogId = allowedLocationId });
            db.AssetEvents.Add(new AssetEvent { Id = activityId, CompanyId = _companyId, AssetId = assetId,
                ActorUserId = _userId, EventType = "Assigned", EmployeeId = outsideEmployeeId, Details = $"Private activity {suffix}" });
            db.AuditEntries.AddRange(
                new AuditEntry { Id = allowedAuditId, CompanyId = _companyId, ActorUserId = _userId, TargetType = "Employee",
                    TargetId = allowedEmployeeId, Action = $"{actionPrefix}-allowed", Details = "private employee audit" },
                new AuditEntry { Id = outsideAuditId, CompanyId = _companyId, ActorUserId = _userId, TargetType = "Employee",
                    TargetId = outsideEmployeeId, Action = $"{actionPrefix}-outside", Details = "outside employee audit" });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = allowedUnitId });
            await db.SaveChangesAsync();
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get, $"/api/audit?q={actionPrefix}", _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var entries = payload.RootElement.GetProperty("entries").EnumerateArray().ToArray();
            Assert.Contains(entries, entry => entry.GetProperty("id").GetGuid() == allowedAuditId);
            Assert.DoesNotContain(entries, entry => entry.GetProperty("id").GetGuid() == outsideAuditId);
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get, "/api/audit/actions", _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var actions = payload.RootElement.GetProperty("actions").EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.Contains($"{actionPrefix}-allowed", actions);
            Assert.DoesNotContain($"{actionPrefix}-outside", actions);
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get, $"/api/audit/export.csv?q={actionPrefix}", _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains(allowedEmployeeId.ToString(), csv, StringComparison.Ordinal);
            Assert.DoesNotContain(outsideEmployeeId.ToString(), csv, StringComparison.Ordinal);
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get, $"/api/activity?assetId={assetId}", _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var entry = Assert.Single(payload.RootElement.GetProperty("entries").EnumerateArray(), row => row.GetProperty("id").GetGuid() == activityId);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("details").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("employeeId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("employeeName").ValueKind);
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get,
                   $"/api/activity?assetId={assetId}&q={Uri.EscapeDataString($"AUDIT-OUTSIDE-{suffix}")}",
                   _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get,
                   $"/api/activity?assetId={assetId}&employeeId={outsideEmployeeId}",
                   _sameCompanyOtherUserId, _companyId, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }
    }
}
