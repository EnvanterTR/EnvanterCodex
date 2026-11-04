using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
            var otherMembership = await db.CompanyMemberships.SingleOrDefaultAsync(row => row.CompanyId == _otherCompanyId && row.UserId == _userId);
            if (otherMembership is null)
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            else
                otherMembership.RoleKey = "admin";
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

    [Fact]
    public async Task Software_license_keys_are_encrypted_audited_and_company_role_csrf_scoped()
    {
        const string firstKey = "acceptance-license-key-3c9b";
        const string replacementKey = "replacement-license-key-7a10";
        var keyPath = $"/api/software-licenses/{_ownLicenseId}/key";
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var noCsrf = await SendAsUserAsync(HttpMethod.Put, keyPath, _userId, _companyId, JsonContent.Create(new { key = firstKey })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var viewerWrite = await SendAsRoleAsync(HttpMethod.Put, keyPath, "viewer", JsonContent.Create(new { key = firstKey }), token, cookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerWrite.StatusCode);
        using (var viewerReveal = await SendAsRoleAsync(HttpMethod.Post, keyPath + "/reveal", "viewer", JsonContent.Create(new { }), token, cookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerReveal.StatusCode);
        await SetCompanyRoleAsync("admin");

        async Task<HttpResponseMessage> Write(HttpMethod method, string path, object? body = null) =>
            await SendAsRoleAsync(method, path, "admin", body is null ? null : JsonContent.Create(body), token, cookie);

        using (var noCsrfReveal = await SendAsUserAsync(HttpMethod.Post, keyPath + "/reveal", _userId, _companyId, JsonContent.Create(new { })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrfReveal.StatusCode);
        using (var set = await Write(HttpMethod.Put, keyPath, new { key = firstKey }))
            Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        string firstCiphertext;
        var keyProtection = _factory!.Services.GetRequiredService<SoftwareLicenseKeyProtection>();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            firstCiphertext = await db.SoftwareLicenses.Where(row => row.CompanyId == _companyId && row.Id == _ownLicenseId)
                .Select(row => row.ProtectedLicenseKey!).SingleAsync();
            Assert.StartsWith("enc:v1:", firstCiphertext, StringComparison.Ordinal);
            Assert.DoesNotContain(firstKey, firstCiphertext, StringComparison.Ordinal);
        }
        Assert.Equal(firstKey, keyProtection.Unprotect(_companyId, _ownLicenseId, firstCiphertext));
        Assert.Throws<CryptographicException>(() => keyProtection.Unprotect(_otherCompanyId, _ownLicenseId, firstCiphertext));
        Assert.Throws<CryptographicException>(() => keyProtection.Unprotect(_companyId, Guid.NewGuid(), firstCiphertext));
        var unicodeKey = string.Concat(Enumerable.Repeat("🔐", 1024));
        var protectedUnicodeKey = keyProtection.Protect(_companyId, _ownLicenseId, unicodeKey);
        Assert.True(protectedUnicodeKey.Length <= 12000);
        Assert.Equal(unicodeKey, keyProtection.Unprotect(_companyId, _ownLicenseId, protectedUnicodeKey));

        using (var detail = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var body = await detail.Content.ReadAsStringAsync();
            Assert.DoesNotContain(firstKey, body, StringComparison.Ordinal);
            using var payload = JsonDocument.Parse(body);
            Assert.True(payload.RootElement.GetProperty("license").GetProperty("hasLicenseKey").GetBoolean());
        }
        using (var list = await SendAsAdminAsync("/api/software-licenses?status=all"))
            Assert.DoesNotContain(firstKey, await list.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using (var export = await SendAsAdminAsync("/api/software-licenses/export.csv?status=all"))
            Assert.DoesNotContain(firstKey, await export.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using (var membershipScope = _factory!.Services.CreateAsyncScope())
        {
            var db = membershipScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherMembership = await db.CompanyMemberships.SingleOrDefaultAsync(row => row.CompanyId == _otherCompanyId && row.UserId == _userId);
            if (otherMembership is null)
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            else
                otherMembership.RoleKey = "admin";
            await db.SaveChangesAsync();
        }
        using (var foreignReveal = await SendAsUserAsync(HttpMethod.Post, keyPath + "/reveal", _userId, _otherCompanyId,
            JsonContent.Create(new { }), token, cookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignReveal.StatusCode);
        using (var foreignSet = await SendAsUserAsync(HttpMethod.Put, keyPath, _userId, _otherCompanyId,
            JsonContent.Create(new { key = "foreign-attempt" }), token, cookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignSet.StatusCode);

        using (var reveal = await Write(HttpMethod.Post, keyPath + "/reveal", new { }))
        {
            Assert.Equal(HttpStatusCode.OK, reveal.StatusCode);
            Assert.Equal("no-store, no-cache, max-age=0", reveal.Headers.GetValues("Cache-Control").Single());
            using var payload = JsonDocument.Parse(await reveal.Content.ReadAsStringAsync());
            Assert.Equal(firstKey, payload.RootElement.GetProperty("key").GetString());
        }
        using (var replacement = await Write(HttpMethod.Put, keyPath, new { key = replacementKey }))
            Assert.Equal(HttpStatusCode.NoContent, replacement.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var replacementCiphertext = await db.SoftwareLicenses.Where(row => row.Id == _ownLicenseId).Select(row => row.ProtectedLicenseKey!).SingleAsync();
            Assert.NotEqual(firstCiphertext, replacementCiphertext);
            Assert.DoesNotContain(replacementKey, replacementCiphertext, StringComparison.Ordinal);
        }
        using (var clearWithoutCsrf = await SendAsUserAsync(HttpMethod.Delete, keyPath, _userId, _companyId))
            Assert.Equal(HttpStatusCode.BadRequest, clearWithoutCsrf.StatusCode);
        using (var clear = await Write(HttpMethod.Delete, keyPath))
            Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
        using (var noLongerExists = await Write(HttpMethod.Post, keyPath + "/reveal", new { }))
            Assert.Equal(HttpStatusCode.NotFound, noLongerExists.StatusCode);

        await using (var auditScope = _factory!.Services.CreateAsyncScope())
        {
            var db = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.SoftwareLicenses.Where(row => row.Id == _ownLicenseId).AnyAsync(row => row.ProtectedLicenseKey != null));
            Assert.Equal(2, await db.AuditEntries.CountAsync(row => row.TargetId == _ownLicenseId && row.Action == "LicenseKeyUpdated"));
            Assert.Equal(1, await db.AuditEntries.CountAsync(row => row.TargetId == _ownLicenseId && row.Action == "LicenseKeyViewed"));
            Assert.Equal(1, await db.AuditEntries.CountAsync(row => row.TargetId == _ownLicenseId && row.Action == "LicenseKeyCleared"));
            Assert.DoesNotContain(await db.AuditEntries.Where(row => row.TargetId == _ownLicenseId).Select(row => row.Details ?? "").ToListAsync(),
                detail => detail.Contains(firstKey, StringComparison.Ordinal) || detail.Contains(replacementKey, StringComparison.Ordinal));
        }
    }
}
