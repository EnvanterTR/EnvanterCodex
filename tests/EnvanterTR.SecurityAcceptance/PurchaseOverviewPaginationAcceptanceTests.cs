using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Unified_purchase_pages_and_export_preserve_one_order_across_all_four_types()
    {
        var prefix = $"PAGE-{Guid.NewGuid():N}";
        var expectedIds = new HashSet<Guid>();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var license = new SoftwareLicense { CompanyId = _companyId, Name = prefix, Seats = 16 };
            db.SoftwareLicenses.Add(license);
            for (var index = 0; index < 16; index++)
            {
                var number = $"{prefix}-{new[] { "z", "A", "İ", "é" }[index % 4]}-{index:D2}";
                var date = new DateOnly(2026, 10, 1).AddDays(index % 3);
                var asset = new AssetPurchaseOrder { CompanyId = _companyId, OrderNumber = number, Supplier = prefix, OrderDate = date };
                var accessory = new AccessoryPurchaseOrder { CompanyId = _companyId, OrderNumber = number, Supplier = prefix, OrderDate = date };
                var stock = new StockPurchaseOrder { CompanyId = _companyId, OrderNumber = number, Supplier = prefix, OrderDate = date };
                var purchase = new SoftwareLicensePurchaseOrder { CompanyId = _companyId, LicenseId = license.Id,
                    OrderNumber = number, OrderDate = index % 2 == 0 ? date : null,
                    CreatedAt = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), SeatsPurchased = 1 };
                db.AssetPurchaseOrders.Add(asset);
                db.AccessoryPurchaseOrders.Add(accessory);
                db.StockPurchaseOrders.Add(stock);
                db.SoftwareLicensePurchaseOrders.Add(purchase);
                expectedIds.UnionWith(new[] { asset.Id, accessory.Id, stock.Id, purchase.Id });
            }
            await db.SaveChangesAsync();
        }

        async Task<JsonElement[]> ReadPage(int page, int size)
        {
            using var response = await SendAsAdminAsync($"/api/purchasing/orders?q={prefix}&page={page}&pageSize={size}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {body}");
            using var payload = JsonDocument.Parse(body);
            Assert.Equal(64, payload.RootElement.GetProperty("total").GetInt32());
            return payload.RootElement.GetProperty("items").EnumerateArray().Select(item => item.Clone()).ToArray();
        }

        var all = await ReadPage(1, 100);
        Assert.Equal(expectedIds.Order(), all.Select(item => item.GetProperty("id").GetGuid()).Order());
        var paged = (await ReadPage(1, 25)).Concat(await ReadPage(2, 25)).Concat(await ReadPage(3, 25)).ToArray();
        Assert.Equal(all.Select(item => item.GetProperty("id").GetGuid()), paged.Select(item => item.GetProperty("id").GetGuid()));
        Assert.Empty(await ReadPage(4, 25));

        using var csv = await SendAsAdminAsync($"/api/purchasing/orders/export.csv?q={prefix}");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        var rows = (await csv.Content.ReadAsStringAsync()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
        Assert.Equal(all.Select(item => item.GetProperty("orderNumber").GetString()), rows.Select(row => row.Split(';')[1].Trim('"')));
    }
}
