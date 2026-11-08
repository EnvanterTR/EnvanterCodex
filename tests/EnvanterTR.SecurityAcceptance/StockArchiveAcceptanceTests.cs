using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Stock_catalogs_archive_and_restore_without_cross_tenant_access_or_history_loss()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var outsideIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Accessories.Add(new Accessory { Id = ids[0], CompanyId = _companyId, Name = $"Archive accessory {suffix}" });
            db.Consumables.Add(new Consumable { Id = ids[1], CompanyId = _companyId, Name = $"Archive consumable {suffix}" });
            db.InventoryComponents.Add(new InventoryComponent { Id = ids[2], CompanyId = _companyId, Name = $"Archive component {suffix}" });
            db.Accessories.Add(new Accessory { Id = outsideIds[0], CompanyId = _otherCompanyId, Name = $"Outside archive accessory {suffix}" });
            db.Consumables.Add(new Consumable { Id = outsideIds[1], CompanyId = _otherCompanyId, Name = $"Outside archive consumable {suffix}" });
            db.InventoryComponents.Add(new InventoryComponent { Id = outsideIds[2], CompanyId = _otherCompanyId, Name = $"Outside archive component {suffix}" });
            db.AccessoryMovements.Add(new AccessoryMovement { CompanyId = _companyId, AccessoryId = ids[0], MovementType = "Received", Quantity = 1 });
            db.ConsumableMovements.Add(new ConsumableMovement { CompanyId = _companyId, ConsumableId = ids[1], MovementType = "Received", Quantity = 1 });
            db.ComponentMovements.Add(new ComponentMovement { CompanyId = _companyId, ComponentId = ids[2], MovementType = "Received", Quantity = 1 });
            await db.SaveChangesAsync();
        }

        var (token, cookie) = await GetStockArchiveCsrfAsync();
        var routes = new[] { $"/api/accessories/{ids[0]}", $"/api/consumables/{ids[1]}", $"/api/components/{ids[2]}" };
        var outsideRoutes = new[] { $"/api/accessories/{outsideIds[0]}", $"/api/consumables/{outsideIds[1]}", $"/api/components/{outsideIds[2]}" };
        var listRoutes = new[] { $"/api/accessories?q=Archive%20accessory%20{suffix}", $"/api/consumables?q=Archive%20consumable%20{suffix}", $"/api/components?q=Archive%20component%20{suffix}" };
        for (var index = 0; index < routes.Length; index++)
        {
            using var forbidden = await SendAsUserAsync(HttpMethod.Delete, outsideRoutes[index], _userId,
                _companyId, requestToken: token, antiforgeryCookie: cookie);
            Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);

            using var archived = await SendAsUserAsync(HttpMethod.Delete, routes[index], _userId, _companyId,
                requestToken: token, antiforgeryCookie: cookie);
            Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
            using var hidden = await SendAsAdminAsync(listRoutes[index]);
            Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
            using (var list = await JsonDocument.ParseAsync(await hidden.Content.ReadAsStreamAsync()))
                Assert.Equal(0, list.RootElement.GetProperty("total").GetInt32());

            using var archivedListResponse = await SendAsAdminAsync(listRoutes[index] + "&includeArchived=true");
            Assert.Equal(HttpStatusCode.OK, archivedListResponse.StatusCode);
            using (var archivedList = await JsonDocument.ParseAsync(await archivedListResponse.Content.ReadAsStreamAsync()))
                Assert.Equal(1, archivedList.RootElement.GetProperty("total").GetInt32());

            await using (var scope = _factory!.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.True(index switch
                {
                    0 => await db.Accessories.Where(x => x.CompanyId == _companyId && x.Id == ids[0]).Select(x => x.IsArchived).SingleAsync(),
                    1 => await db.Consumables.Where(x => x.CompanyId == _companyId && x.Id == ids[1]).Select(x => x.IsArchived).SingleAsync(),
                    _ => await db.InventoryComponents.Where(x => x.CompanyId == _companyId && x.Id == ids[2]).Select(x => x.IsArchived).SingleAsync()
                });
            }

            using var restored = await SendAsUserAsync(HttpMethod.Post, routes[index] + "/restore", _userId, _companyId,
                requestToken: token, antiforgeryCookie: cookie);
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
            using var visible = await SendAsAdminAsync(routes[index]);
            Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        }

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verifyDb.Accessories.Where(x => x.CompanyId == _companyId && x.Id == ids[0]).Select(x => x.IsArchived).SingleAsync());
        Assert.False(await verifyDb.Consumables.Where(x => x.CompanyId == _companyId && x.Id == ids[1]).Select(x => x.IsArchived).SingleAsync());
        Assert.False(await verifyDb.InventoryComponents.Where(x => x.CompanyId == _companyId && x.Id == ids[2]).Select(x => x.IsArchived).SingleAsync());
        Assert.Equal(1, await verifyDb.AccessoryMovements.CountAsync(x => x.CompanyId == _companyId && x.AccessoryId == ids[0]));
        Assert.Equal(1, await verifyDb.ConsumableMovements.CountAsync(x => x.CompanyId == _companyId && x.ConsumableId == ids[1]));
        Assert.Equal(1, await verifyDb.ComponentMovements.CountAsync(x => x.CompanyId == _companyId && x.ComponentId == ids[2]));
    }

    private async Task<(string Token, string Cookie)> GetStockArchiveCsrfAsync()
    {
        using var response = await SendAsAdminAsync("/api/session/csrf");
        response.EnsureSuccessStatusCode();
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        return (token, cookie);
    }
}
