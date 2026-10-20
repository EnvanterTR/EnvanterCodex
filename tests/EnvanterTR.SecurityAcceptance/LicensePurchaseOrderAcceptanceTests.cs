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
    public async Task License_purchase_orders_preserve_supplier_links_and_enforce_tenant_role_csrf_and_archive_state()
    {
        var license = new SoftwareLicense { CompanyId = _companyId, Name = "Purchase acceptance", Seats = 5, Currency = "TRY" };
        var otherLicense = new SoftwareLicense { CompanyId = _companyId, Name = "Other purchase parent", Seats = 2, Currency = "TRY" };
        var supplier = new SupplierCatalogEntry { CompanyId = _companyId, Name = "Purchase supplier", NormalizedName = "PURCHASE SUPPLIER" };
        var foreignSupplier = new SupplierCatalogEntry { CompanyId = _otherCompanyId, Name = "Foreign purchase supplier", NormalizedName = "FOREIGN PURCHASE SUPPLIER" };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SoftwareLicenses.AddRange(license, otherLicense);
            db.SupplierCatalogEntries.AddRange(supplier, foreignSupplier);
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var path = $"/api/software-licenses/{license.Id}/purchase-orders";
        object Body(Guid supplierId, int seats = 3) => new { supplierId, orderNumber = " ACCEPT-PO ", orderDate = "2026-10-20",
            seatsPurchased = seats, unitCost = (decimal?)null, currency = "usd", notes = "Proof" };
        Task<HttpResponseMessage> Write(HttpMethod method, string url, object? body = null) =>
            SendAsRoleAsync(method, url, "admin", body is null ? null : JsonContent.Create(body), token, cookie);
        using (var denied = await SendAsUserAsync(HttpMethod.Post, path, _userId, _companyId, JsonContent.Create(Body(supplier.Id))))
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var denied = await SendAsRoleAsync(HttpMethod.Post, path, "viewer", JsonContent.Create(Body(supplier.Id)), token, cookie))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var foreign = await Write(HttpMethod.Post, path, Body(foreignSupplier.Id)))
            Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        using (var invalid = await Write(HttpMethod.Post, path, Body(supplier.Id, 0)))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var created = await Write(HttpMethod.Post, path, Body(supplier.Id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdPayload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var orderId = createdPayload.RootElement.GetProperty("id").GetGuid();
        var itemPath = $"{path}/{orderId}";
        using (var wrongParent = await Write(HttpMethod.Put, $"/api/software-licenses/{otherLicense.Id}/purchase-orders/{orderId}", Body(supplier.Id)))
            Assert.Equal(HttpStatusCode.NotFound, wrongParent.StatusCode);
        using (var crossTenant = await SendAsUserAsync(HttpMethod.Put, itemPath, _userId, _otherCompanyId, JsonContent.Create(Body(foreignSupplier.Id)), token, cookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var update = await Write(HttpMethod.Put, itemPath, Body(supplier.Id, 4)))
            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        using (var archive = await Write(HttpMethod.Post, itemPath + "/archive"))
            Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        using (var archivedUpdate = await Write(HttpMethod.Put, itemPath, Body(supplier.Id)))
            Assert.Equal(HttpStatusCode.Conflict, archivedUpdate.StatusCode);
        using (var repeatedArchive = await Write(HttpMethod.Post, itemPath + "/archive"))
            Assert.Equal(HttpStatusCode.NoContent, repeatedArchive.StatusCode);
        using (var restore = await Write(HttpMethod.Post, itemPath + "/restore"))
            Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);
        using var detail = await SendAsAdminAsync($"/api/software-licenses/{license.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var order = Assert.Single(payload.RootElement.GetProperty("purchaseOrders").EnumerateArray());
        Assert.Equal(supplier.Id, order.GetProperty("supplierId").GetGuid());
        Assert.Equal(supplier.Name, order.GetProperty("supplier").GetString());
        Assert.Equal(4, order.GetProperty("seatsPurchased").GetInt32());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("unitCost").ValueKind);
        Assert.False(order.GetProperty("isArchived").GetBoolean());
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.SoftwareLicensePurchaseOrders.SingleAsync(row => row.Id == orderId);
            Assert.Equal("ACCEPT-PO", stored.OrderNumber);
            Assert.Equal("USD", stored.Currency);
            Assert.Equal(5, await db.SoftwareLicenses.Where(row => row.Id == license.Id).Select(row => row.Seats).SingleAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync(row => row.TargetId == license.Id && row.Action == "PurchaseOrderArchived"));
            Assert.True(await db.AuditEntries.AnyAsync(row => row.TargetId == license.Id && row.Action == "PurchaseOrderRestored"));
            await db.SoftwareLicenses.Where(row => row.Id == license.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.IsArchived, true));
        }
        using (var blocked = await Write(HttpMethod.Post, path, Body(supplier.Id)))
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        using (var blocked = await Write(HttpMethod.Post, itemPath + "/archive"))
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
    }
}
