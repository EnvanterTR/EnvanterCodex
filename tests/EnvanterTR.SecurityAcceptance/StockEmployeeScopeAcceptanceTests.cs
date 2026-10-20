using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
    public async Task Stock_and_model_employee_scope_matches_customer_api_and_explicit_location_restrictions()
    {
        var unitId = Guid.NewGuid();
        var outsideUnitId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        var consumableId = Guid.NewGuid();
        var accessoryId = Guid.NewGuid();
        var kitId = Guid.NewGuid();
        var hiddenActorId = Guid.NewGuid();
        var employeeIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var suffix = Guid.NewGuid().ToString("N");
        var employeeNames = new[] { $"DIRECT-EMPLOYEE-{suffix}", $"INHERITED-EMPLOYEE-{suffix}", $"HIDDEN-EMPLOYEE-{suffix}" };
        var apiSecret = $"etr_live_scope_stock_acceptance_{suffix}";

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.AddRange(
                new CompanyUnit { Id = unitId, CompanyId = _companyId, Name = $"Stock unit {suffix}", NormalizedName = $"STOCK UNIT {suffix}" },
                new CompanyUnit { Id = outsideUnitId, CompanyId = _companyId, Name = $"Outside stock unit {suffix}", NormalizedName = $"OUTSIDE STOCK UNIT {suffix}" });
            db.CompanyLocations.Add(new CompanyLocation { Id = locationId, CompanyId = _companyId, CompanyUnitId = unitId,
                Name = $"Stock location {suffix}", NormalizedName = $"STOCK LOCATION {suffix}" });
            db.AssetCategories.Add(new AssetCategory { Id = categoryId, CompanyId = _companyId, Name = $"Stock category {suffix}" });
            db.AssetModels.Add(new AssetModel { Id = modelId, CompanyId = _companyId, Name = $"Stock model {suffix}", CategoryId = categoryId });
            db.InventoryComponents.Add(new InventoryComponent { Id = componentId, CompanyId = _companyId,
                Name = $"Scoped component {suffix}", LocationCatalogId = locationId, TotalQuantity = 10 });
            db.ComponentMovements.Add(new ComponentMovement { CompanyId = _companyId, ComponentId = componentId,
                ActorUserId = hiddenActorId, MovementType = "Received", Quantity = 1 });
            db.Consumables.Add(new Consumable { Id = consumableId, CompanyId = _companyId, Name = $"Scoped consumable {suffix}", LocationCatalogId = locationId, TotalQuantity = 50 });
            db.Accessories.Add(new Accessory { Id = accessoryId, CompanyId = _companyId, Name = $"Scoped accessory {suffix}", LocationCatalogId = locationId, TotalQuantity = 50 });
            db.AccessoryKits.Add(new AccessoryKit { Id = kitId, CompanyId = _companyId, Name = $"Scoped kit {suffix}" });
            db.AccessoryKitLines.Add(new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId,
                ItemType = "Accessory", AccessoryId = accessoryId, Quantity = 1 });
            for (var index = 0; index < employeeIds.Length; index++)
            {
                db.Employees.Add(new Employee { Id = employeeIds[index], CompanyId = _companyId,
                    CompanyUnitId = index == 0 ? unitId : index == 2 ? outsideUnitId : null,
                    LocationCatalogId = index == 0 ? null : locationId,
                    DisplayName = employeeNames[index], EmployeeNumber = $"STOCK-EMP-{index}-{suffix}" });
                db.Assets.Add(new Asset { CompanyId = _companyId, CategoryId = categoryId, ModelId = modelId,
                    AssetTag = $"STOCK-ASSET-{index}-{suffix}", Name = $"Scoped model asset {index}",
                    LocationCatalogId = locationId, AssignedEmployeeId = employeeIds[index], Status = AssetStatuses.Deployed });
                db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = _companyId, ConsumableId = consumableId,
                    EmployeeId = employeeIds[index], MovementType = "Checkout", Quantity = 1,
                    ActorUserId = index == 0 ? hiddenActorId : _userId });
                var assignment = new AccessoryAssignment { CompanyId = _companyId, AccessoryId = accessoryId,
                    TargetType = "Employee", EmployeeId = employeeIds[index], Quantity = 1,
                    Notes = $"[KIT:{kitId}] PRIVATE-KIT-NOTE-{suffix}" };
                db.AccessoryAssignments.Add(assignment);
                db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = _companyId, AccessoryId = accessoryId,
                    AssignmentId = assignment.Id, EmployeeId = employeeIds[index], MovementType = "Checkout", Quantity = 1,
                    ActorUserId = index == 0 ? hiddenActorId : _userId });
            }
            var hiddenActorEmail = $"hidden-actor-{suffix}@example.invalid";
            db.Users.Add(new EnvanterUser
            {
                Id = hiddenActorId, UserName = $"hidden-actor-{suffix}", NormalizedUserName = $"HIDDEN-ACTOR-{suffix}".ToUpperInvariant(),
                Email = hiddenActorEmail, NormalizedEmail = hiddenActorEmail.ToUpperInvariant(), EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DisplayName = $"HIDDEN-ACTOR-{suffix}"
            });
            db.CompanyMemberships.Add(new CompanyMembership
                { CompanyId = _companyId, UserId = hiddenActorId, EmployeeId = employeeIds[2], RoleKey = "viewer" });
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "admin";
            membership.LocationAccessMode = CompanyLocationAccess.All;
            membership.CompanyUnitAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipCompanyUnitAccesses.Add(new CompanyMembershipCompanyUnitAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, CompanyUnitId = unitId });
            db.CompanyApiTokens.Add(new CompanyApiToken { CompanyId = _companyId, UserId = _sameCompanyOtherUserId,
                Name = "Scoped stock acceptance", TokenPrefix = apiSecret[..16],
                TokenHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiSecret))),
                ScopesJson = JsonSerializer.Serialize(new[] { "people:read" }) });
            await db.SaveChangesAsync();
        }

        using (var response = await SendAsUserAsync(HttpMethod.Get, $"/api/accessory-kits/{kitId}/history", _sameCompanyOtherUserId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(2, items.Length);
            Assert.All(items, item =>
            {
                Assert.Equal(JsonValueKind.Null, item.GetProperty("actor").ValueKind);
                Assert.Equal(JsonValueKind.Null, item.GetProperty("notes").ValueKind);
            });
        }
        using (var response = await SendAsAdminAsync($"/api/accessory-kits/{kitId}/history"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains($"HIDDEN-ACTOR-{suffix}", body, StringComparison.Ordinal);
            Assert.Contains($"PRIVATE-KIT-NOTE-{suffix}", body, StringComparison.Ordinal);
        }

        foreach (var route in new[] { $"/api/consumables/{consumableId}", $"/api/models/{modelId}/details",
                     $"/api/inventory/movements?q={suffix}", $"/api/inventory/movements?q={suffix}&export=true" })
        {
            using var response = await SendAsUserAsync(HttpMethod.Get, route, _sameCompanyOtherUserId, _companyId);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{route}: {response.StatusCode}: {body}");
            Assert.Contains(employeeNames[0], body, StringComparison.Ordinal);
            Assert.Contains(employeeNames[1], body, StringComparison.Ordinal);
            Assert.DoesNotContain(employeeNames[2], body, StringComparison.Ordinal);
            Assert.DoesNotContain($"HIDDEN-ACTOR-{suffix}", body, StringComparison.Ordinal);
        }

        async Task<string> ReadCustomerEmployeesAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/employees?q={suffix}&pageSize=100");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiSecret);
            using var response = await _client!.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Customer employees: {response.StatusCode}: {body}");
            return body;
        }
        var unitOnlyEmployees = await ReadCustomerEmployeesAsync();
        Assert.Contains(employeeNames[0], unitOnlyEmployees, StringComparison.Ordinal);
        Assert.Contains(employeeNames[1], unitOnlyEmployees, StringComparison.Ordinal);
        Assert.DoesNotContain(employeeNames[2], unitOnlyEmployees, StringComparison.Ordinal);

        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        foreach (var index in new[] { 0, 2 })
        {
            using var response = await SendAsUserAsync(HttpMethod.Post, $"/api/consumables/{consumableId}/checkout",
                _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(new { employeeId = employeeIds[index], quantity = 1 }), token, cookie);
            Assert.Equal(index == 0 ? HttpStatusCode.OK : HttpStatusCode.BadRequest, response.StatusCode);
        }

        await using (var fixture = _factory!.Services.CreateAsyncScope())
        {
            var db = fixture.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.LocationAccessMode = CompanyLocationAccess.Restricted;
            db.CompanyMembershipLocationAccesses.Add(new CompanyMembershipLocationAccess
                { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, LocationId = locationId });
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.ConsumableMovements.CountAsync(x => x.CompanyId == _companyId && x.ConsumableId == consumableId && x.EmployeeId == employeeIds[2]));
        }
        var locationRestrictedEmployees = await ReadCustomerEmployeesAsync();
        Assert.DoesNotContain(employeeNames[0], locationRestrictedEmployees, StringComparison.Ordinal);
        Assert.Contains(employeeNames[1], locationRestrictedEmployees, StringComparison.Ordinal);
        Assert.DoesNotContain(employeeNames[2], locationRestrictedEmployees, StringComparison.Ordinal);
    }
}
