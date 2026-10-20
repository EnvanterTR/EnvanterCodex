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
    public async Task Asset_employee_relations_use_direct_unit_and_inherited_location_scope_in_lists_reports_and_exports()
    {
        var unitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var employees = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var assets = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var suffix = Guid.NewGuid().ToString("N");
        var names = new[] { $"DirectEmployee{suffix}", $"InheritedEmployee{suffix}", $"OutsideEmployee{suffix}" };
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = unitId, CompanyId = _companyId, Name = $"Asset scope unit {suffix}", NormalizedName = $"ASSET SCOPE UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside asset scope unit {suffix}", NormalizedName = $"OUTSIDE ASSET SCOPE UNIT {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = locationId, CompanyId = _companyId, CompanyUnitId = unitId,
                Name = $"Asset scope location {suffix}", NormalizedName = $"ASSET SCOPE LOCATION {suffix}" });
            db.AssetCategories.Add(new AssetCategory { Id = categoryId, CompanyId = _companyId, Name = $"Scope category {suffix}" });
            for (var index = 0; index < employees.Length; index++)
            {
                db.Employees.Add(new Employee { Id = employees[index], CompanyId = _companyId,
                    CompanyUnitId = index == 0 ? unitId : index == 2 ? outsideUnitId : null,
                    LocationCatalogId = index == 0 ? null : locationId,
                    DisplayName = names[index], EmployeeNumber = $"EMP-{index}-{suffix}" });
                db.Assets.Add(new Asset { Id = assets[index], CompanyId = _companyId, CategoryId = categoryId,
                    AssetTag = $"SCOPE-ASSET-{index}-{suffix}", Name = $"Scope asset {index} {suffix}",
                    LocationCatalogId = locationId, AssignedEmployeeId = employees[index], Status = AssetStatuses.Deployed,
                    ExpectedReturnOn = tomorrow, WarrantyExpiresAt = tomorrow, NextAuditOn = tomorrow });
                db.AssetEvents.Add(new AssetEvent { CompanyId = _companyId, AssetId = assets[index], EmployeeId = employees[index],
                    ActorUserId = _userId, EventType = "Assigned" });
                db.AssetMaintenances.Add(new AssetMaintenance { CompanyId = _companyId, AssetId = assets[index],
                    ResponsibleEmployeeId = employees[index], ActorUserId = _viewerUserId, MaintenanceType = $"Scope maintenance {suffix}",
                    Status = "Scheduled", PlannedOn = tomorrow, StartedOn = tomorrow, Currency = "TRY" });
                db.AssetReservations.Add(new AssetReservation { CompanyId = _companyId, AssetId = assets[index],
                    EmployeeId = employees[index], RequestedByUserId = _userId, Status = "Approved",
                    StartsOn = tomorrow, EndsOn = tomorrow.AddDays(1) });
            }
            db.AssetAttachments.Add(new AssetAttachment { CompanyId = _companyId, AssetId = assets[0], UploadedByUserId = _viewerUserId,
                OriginalFileName = "scope-proof.pdf", StorageFileName = "scope-proof.pdf", ContentType = "application/pdf", Length = 8 });
            var hiddenActor = await db.Users.SingleAsync(user => user.Id == _viewerUserId);
            hiddenActor.DisplayName = names[2];
            var hiddenActorMembership = await db.CompanyMemberships.SingleAsync(member => member.CompanyId == _companyId && member.UserId == _viewerUserId);
            hiddenActorMembership.EmployeeId = employees[2];
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = unitId });
            await db.SaveChangesAsync();
        }

        foreach (var route in new[]
        {
            "/api/employees", "/api/assets", "/api/assets/checkouts", "/api/assets/export-all.csv",
            "/api/reports/warranties", "/api/reports/warranties/export.csv", "/api/assets/audit-schedule",
            "/api/maintenances", "/api/maintenances/export.csv", "/api/calendar?kind=return", "/api/calendar?kind=reservation"
        })
        {
            using var response = await SendAsUserAsync(HttpMethod.Get, route, _sameCompanyOtherUserId, _companyId);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{route}: {response.StatusCode}: {body}");
            Assert.Contains(names[0], body, StringComparison.Ordinal);
            Assert.Contains(names[1], body, StringComparison.Ordinal);
            Assert.DoesNotContain(names[2], body, StringComparison.Ordinal);
            Assert.DoesNotContain(employees[2].ToString(), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"EMP-2-{suffix}", body, StringComparison.Ordinal);
        }
        foreach (var route in new[] { "/api/assets", "/api/assets/checkouts", "/api/reports/warranties", "/api/assets/audit-schedule", "/api/maintenances" })
        {
            using var allowed = await SendAsUserAsync(HttpMethod.Get, $"{route}?q={Uri.EscapeDataString(names[0])}", _sameCompanyOtherUserId, _companyId);
            using var allowedPayload = JsonDocument.Parse(await allowed.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            Assert.Equal(1, allowedPayload.RootElement.GetProperty("total").GetInt32());
            using var denied = await SendAsUserAsync(HttpMethod.Get, $"{route}?q={Uri.EscapeDataString(names[2])}", _sameCompanyOtherUserId, _companyId);
            using var deniedPayload = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
            Assert.Equal(0, deniedPayload.RootElement.GetProperty("total").GetInt32());
        }
        for (var index = 0; index < assets.Length; index++)
        {
            using var response = await SendAsUserAsync(HttpMethod.Get, $"/api/assets/{assets[index]}", _sameCompanyOtherUserId, _companyId);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(names[2], body, StringComparison.Ordinal);
            if (index < 2) Assert.Contains(names[index], body, StringComparison.Ordinal);
            else
            {
                Assert.DoesNotContain(names[index], body, StringComparison.Ordinal);
                Assert.DoesNotContain(employees[index].ToString(), body, StringComparison.OrdinalIgnoreCase);
            }
            using var maintenance = await SendAsUserAsync(HttpMethod.Get, $"/api/assets/{assets[index]}/maintenances", _sameCompanyOtherUserId, _companyId);
            Assert.Equal(HttpStatusCode.OK, maintenance.StatusCode);
            Assert.DoesNotContain(names[2], await maintenance.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        using (var filtered = await SendAsUserAsync(HttpMethod.Get, $"/api/assets?employeeId={employees[2]}", _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }
    }

    [Fact]
    public async Task Asset_scope_mutations_reject_hidden_checkout_targets_and_catalog_employee_changes_atomically()
    {
        var unitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var outsideLocationId = Guid.NewGuid();
        var emptyLocationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var outsideEmployeeId = Guid.NewGuid();
        var outsideAssetId = Guid.NewGuid();
        var legacyAssetId = Guid.NewGuid();
        var accessoryId = Guid.NewGuid();
        var outsideAccessoryId = Guid.NewGuid();
        var hiddenAssignmentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");
        var locationName = $"Mutation location {suffix}";
        var outsideLocationName = $"Outside mutation location {suffix}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = unitId, CompanyId = _companyId, Name = $"Mutation unit {suffix}", NormalizedName = $"MUTATION UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside mutation unit {suffix}", NormalizedName = $"OUTSIDE MUTATION UNIT {suffix}" });
            db.CompanyLocations.AddRange(
                new CompanyLocation { Id = locationId, CompanyId = _companyId, CompanyUnitId = unitId, Name = locationName, NormalizedName = locationName.ToUpperInvariant() },
                new CompanyLocation { Id = outsideLocationId, CompanyId = _companyId, CompanyUnitId = outsideUnitId, Name = outsideLocationName, NormalizedName = outsideLocationName.ToUpperInvariant(), IsDefault = true },
                new CompanyLocation { Id = emptyLocationId, CompanyId = _companyId, CompanyUnitId = unitId, Name = $"Empty location {suffix}", NormalizedName = $"EMPTY LOCATION {suffix}" });
            db.Employees.AddRange(
                new Employee { Id = employeeId, CompanyId = _companyId, CompanyUnitId = unitId, EmployeeNumber = $"MUT-IN-{suffix}", DisplayName = "Allowed mutation employee" },
                new Employee { Id = outsideEmployeeId, CompanyId = _companyId, CompanyUnitId = outsideUnitId, LocationCatalogId = locationId,
                    Location = locationName, EmployeeNumber = $"MUT-OUT-{suffix}", DisplayName = "Hidden mutation employee" });
            db.AssetCategories.Add(new AssetCategory { Id = categoryId, CompanyId = _companyId, Name = $"Mutation category {suffix}" });
            db.Assets.Add(new Asset { Id = outsideAssetId, CompanyId = _companyId, CategoryId = categoryId, LocationCatalogId = outsideLocationId,
                Name = "Outside mutation asset", AssetTag = $"MUT-ASSET-{suffix}" });
            db.Assets.Add(new Asset { Id = legacyAssetId, CompanyId = _companyId, CategoryId = categoryId, Location = $"Legacy location {suffix}",
                Name = "Legacy mutation asset", AssetTag = $"LEGACY-MUT-ASSET-{suffix}" });
            db.Accessories.AddRange(
                new Accessory { Id = accessoryId, CompanyId = _companyId, Name = "Allowed mutation accessory", LocationCatalogId = locationId, TotalQuantity = 20 },
                new Accessory { Id = outsideAccessoryId, CompanyId = _companyId, Name = "Outside mutation accessory", LocationCatalogId = outsideLocationId, TotalQuantity = 20 });
            db.AccessoryAssignments.Add(new AccessoryAssignment { Id = hiddenAssignmentId, CompanyId = _companyId, AccessoryId = accessoryId,
                TargetType = "Employee", EmployeeId = outsideEmployeeId, Quantity = 1 });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = unitId });
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        Task<HttpResponseMessage> Write(HttpMethod method, string route, object body) => SendAsUserAsync(method, route,
            _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(body), token, cookie);

        foreach (var target in new[] { "Employee", "Asset", "Location" })
        {
            var body = new { targetType = target, employeeId = (Guid?)outsideEmployeeId, assetId = (Guid?)outsideAssetId,
                location = outsideLocationName, quantity = 1, items = new[] { new { accessoryId, quantity = 1 } } };
            using var single = await Write(HttpMethod.Post, $"/api/accessories/{accessoryId}/checkout", body);
            Assert.Equal(HttpStatusCode.BadRequest, single.StatusCode);
            using var bulk = await Write(HttpMethod.Post, "/api/accessories/bulk-checkout", body);
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
        }
        using (var hiddenStock = await Write(HttpMethod.Post, "/api/accessories/bulk-checkout", new { targetType = "Employee", employeeId,
                   items = new[] { new { accessoryId = outsideAccessoryId, quantity = 1 } } }))
            Assert.Equal(HttpStatusCode.NotFound, hiddenStock.StatusCode);
        using (var allowedCheckout = await Write(HttpMethod.Post, $"/api/accessories/{accessoryId}/checkout", new { targetType = "Employee", employeeId, quantity = 1 }))
            Assert.Equal(HttpStatusCode.Created, allowedCheckout.StatusCode);
        using (var hiddenReturn = await Write(HttpMethod.Post, $"/api/accessories/{accessoryId}/checkin", new { assignmentId = hiddenAssignmentId, quantity = 1 }))
            Assert.Equal(HttpStatusCode.NotFound, hiddenReturn.StatusCode);
        using (var hiddenBulkReturn = await Write(HttpMethod.Post, $"/api/accessories/{accessoryId}/bulk-checkin",
                   new { items = new[] { new { assignmentId = hiddenAssignmentId, quantity = 1 } } }))
            Assert.Equal(HttpStatusCode.NotFound, hiddenBulkReturn.StatusCode);
        using (var hiddenLocationManager = await Write(HttpMethod.Post, $"/api/locations/catalog/{outsideLocationId}/manager", new { employeeId }))
            Assert.Equal(HttpStatusCode.NotFound, hiddenLocationManager.StatusCode);
        using (var hiddenManager = await Write(HttpMethod.Post, $"/api/locations/catalog/{locationId}/manager", new { employeeId = outsideEmployeeId }))
            Assert.Equal(HttpStatusCode.BadRequest, hiddenManager.StatusCode);
        using (var allowedManager = await Write(HttpMethod.Post, $"/api/locations/catalog/{locationId}/manager", new { employeeId }))
            Assert.Equal(HttpStatusCode.OK, allowedManager.StatusCode);
        using (var hiddenEmployeeRename = await Write(HttpMethod.Patch, $"/api/locations/catalog/{locationId}", new { name = $"Blocked rename {suffix}" }))
            Assert.Equal(HttpStatusCode.Forbidden, hiddenEmployeeRename.StatusCode);
        using (var allowedRename = await Write(HttpMethod.Patch, $"/api/locations/catalog/{emptyLocationId}", new { name = $"Allowed rename {suffix}" }))
            Assert.Equal(HttpStatusCode.OK, allowedRename.StatusCode);
        foreach (var operation in new[] { "archive", "restore" })
        {
            using var deniedLocationOperation = await Write(HttpMethod.Post, $"/api/locations/catalog/{outsideLocationId}/{operation}", new { });
            Assert.Equal(HttpStatusCode.NotFound, deniedLocationOperation.StatusCode);
            using var allowedLocationOperation = await Write(HttpMethod.Post, $"/api/locations/catalog/{emptyLocationId}/{operation}", new { });
            Assert.Equal(HttpStatusCode.OK, allowedLocationOperation.StatusCode);
        }
        using (var deniedDefault = await Write(HttpMethod.Post, $"/api/locations/catalog/{outsideLocationId}/default", new { isDefault = false }))
            Assert.Equal(HttpStatusCode.NotFound, deniedDefault.StatusCode);
        using (var deniedDefaultReplacement = await Write(HttpMethod.Post, $"/api/locations/catalog/{emptyLocationId}/default", new { isDefault = true }))
            Assert.Equal(HttpStatusCode.Forbidden, deniedDefaultReplacement.StatusCode);
        using (var deniedLegacyRename = await Write(HttpMethod.Patch, "/api/locations/rename",
                   new { currentName = $"Legacy location {suffix}", newName = $"Renamed legacy {suffix}" }))
            Assert.Equal(HttpStatusCode.Forbidden, deniedLegacyRename.StatusCode);
        using (var deniedParent = await Write(HttpMethod.Post, "/api/locations/catalog", new { name = "Hidden parent child", parentLocationId = outsideLocationId }))
            Assert.Equal(HttpStatusCode.NotFound, deniedParent.StatusCode);
        using (var deniedUnit = await Write(HttpMethod.Post, "/api/locations/catalog", new { name = "Hidden unit child", parentLocationId = emptyLocationId, companyUnitId = outsideUnitId }))
            Assert.Equal(HttpStatusCode.Forbidden, deniedUnit.StatusCode);
        using (var allowedChild = await Write(HttpMethod.Post, "/api/locations/catalog", new { name = "Allowed child", parentLocationId = emptyLocationId }))
            Assert.Equal(HttpStatusCode.Created, allowedChild.StatusCode);
        using (var deniedPath = await Write(HttpMethod.Post, "/api/locations/catalog/from-path", new { path = outsideLocationName + " / Hidden child" }))
            Assert.Equal(HttpStatusCode.NotFound, deniedPath.StatusCode);
        using (var deniedNewRoot = await Write(HttpMethod.Post, "/api/locations/catalog/from-path", new { path = $"Unscoped root {suffix} / Child" }))
            Assert.Equal(HttpStatusCode.Forbidden, deniedNewRoot.StatusCode);
        using (var allowedPath = await Write(HttpMethod.Post, "/api/locations/catalog/from-path", new { path = $"Allowed rename {suffix} / Allowed path child" }))
            Assert.Equal(HttpStatusCode.OK, allowedPath.StatusCode);
        await using (var restrictedScope = _factory!.Services.CreateAsyncScope())
        {
            var db = restrictedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(member => member.CompanyId == _companyId && member.UserId == _sameCompanyOtherUserId);
            membership.LocationAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipLocationAccesses.Add(new CompanyMembershipLocationAccess { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, LocationId = emptyLocationId });
            var selectedChild = await db.CompanyLocations.SingleAsync(location => location.Id == emptyLocationId);
            selectedChild.ParentLocationId = locationId;
            await db.SaveChangesAsync();
        }
        using (var allowedDescendantPath = await Write(HttpMethod.Post, "/api/locations/catalog/from-path",
                   new { path = $"{locationName} / Allowed rename {suffix} / Selected descendant" }))
            Assert.Equal(HttpStatusCode.OK, allowedDescendantPath.StatusCode);
        using (var deniedAncestorPath = await Write(HttpMethod.Post, "/api/locations/catalog/from-path", new { path = locationName }))
            Assert.Equal(HttpStatusCode.NotFound, deniedAncestorPath.StatusCode);
        using (var deniedRestrictedRoot = await Write(HttpMethod.Post, "/api/locations/catalog", new { name = "Restricted root", companyUnitId = unitId }))
            Assert.Equal(HttpStatusCode.Forbidden, deniedRestrictedRoot.StatusCode);
        await using var verificationScope = _factory!.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(locationName, await verificationDb.CompanyLocations.Where(x => x.Id == locationId).Select(x => x.Name).SingleAsync());
        Assert.Equal(locationName, await verificationDb.Employees.Where(x => x.Id == outsideEmployeeId).Select(x => x.Location).SingleAsync());
        Assert.Equal(0, await verificationDb.AccessoryAssignments.Where(x => x.Id == hiddenAssignmentId).Select(x => x.ReturnedQuantity).SingleAsync());
        Assert.Equal(2, await verificationDb.AccessoryAssignments.CountAsync(x => x.CompanyId == _companyId && x.AccessoryId == accessoryId));
        Assert.Equal(0, await verificationDb.AccessoryAssignments.CountAsync(x => x.CompanyId == _companyId && x.AccessoryId == outsideAccessoryId));
        Assert.True(await verificationDb.CompanyLocations.Where(x => x.Id == outsideLocationId).Select(x => x.IsDefault && x.IsActive).SingleAsync());
        Assert.Equal($"Legacy location {suffix}", await verificationDb.Assets.Where(x => x.Id == legacyAssetId).Select(x => x.Location).SingleAsync());
    }
}
