using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.IO.Compression;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

[CollectionDefinition("Tenant session acceptance", DisableParallelization = true)]
public sealed class TenantSessionAcceptanceCollection { }

[Collection("Tenant session acceptance")]
public sealed class TenantSessionAcceptanceTests : IAsyncLifetime
{
    private const string TestScheme = "SecurityAcceptance";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sameCompanyOtherUserId = Guid.NewGuid();
    private readonly Guid _viewerUserId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _otherCompanyId = Guid.NewGuid();
    private readonly Guid _ownAssetId = Guid.NewGuid();
    private readonly Guid _ownSecondAssetId = Guid.NewGuid();
    private readonly Guid _otherCompanyAssetId = Guid.NewGuid();
    private readonly Guid _ownLicenseId = Guid.NewGuid();
    private readonly Guid _otherCompanyLicenseId = Guid.NewGuid();
    private readonly Guid _ownAccessoryId = Guid.NewGuid();
    private readonly Guid _otherCompanyAccessoryId = Guid.NewGuid();
    private readonly Guid _ownConsumableId = Guid.NewGuid();
    private readonly Guid _otherCompanyConsumableId = Guid.NewGuid();
    private readonly Guid _ownComponentId = Guid.NewGuid();
    private readonly Guid _otherCompanyComponentId = Guid.NewGuid();
    private readonly Guid _ownEmployeeId = Guid.NewGuid();
    private readonly Guid _ownSecondEmployeeId = Guid.NewGuid();
    private readonly Guid _otherCompanyEmployeeId = Guid.NewGuid();

    [Fact]
    public async Task Credential_query_strings_are_removed_before_a_page_is_served()
    {
        using var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/?email=admin%40example.test&password=secret-value&twoFactorCode=123456&companyId=private-id&view=assets");

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/?view=assets", response.Headers.Location?.OriginalString);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.DoesNotContain("secret-value", response.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("admin%40example.test", response.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_checkout_requires_csrf_and_fails_closed_without_provider_configuration()
    {
        var request = new InitializeCompanyCheckoutRequest(Guid.NewGuid(), "Synthetic Checkout Ltd", "synthetic-checkout",
            "owner@example.invalid", "Test", "Owner", "owner@example.invalid", "+905550000001", "10000000146",
            "Test Street 1", "Istanbul", "Turkey", "34000", Guid.NewGuid(), "monthly");
        using var missingCsrf = await _client!.PostAsJsonAsync("/api/checkout/initialize", request);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using var csrfResponse = await _client!.GetAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/initialize")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-CSRF-TOKEN", requestToken);
        message.Headers.Add("Cookie", antiforgeryCookie);
        using var unconfigured = await _client.SendAsync(message);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unconfigured.StatusCode);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.CompanyCheckoutOrders.AnyAsync(x => x.Id == request.CheckoutId));
    }

    [Fact]
    public async Task Public_checkout_uses_server_price_verifies_provider_and_provisions_once()
    {
        var checkoutId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Code = $"co-{checkoutId:N}", Name = "Synthetic Cloud", DeploymentKind = "cloud",
            MonthlyPrice = 125.50m, AnnualPrice = 1200m, Currency = "TRY", IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync();
        }

        var provider = new SyntheticIyzipayHandler();
        using var checkoutFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                ["ForwardedHeaders:Enabled"] = "true",
                ["Storage:CompanyMediaPath"] = _mediaPath,
                ["Email:Smtp:Host"] = "127.0.0.1",
                ["Email:Smtp:Port"] = _smtpCapture.Port.ToString(CultureInfo.InvariantCulture),
                ["Email:Smtp:EnableSsl"] = "false",
                ["Email:FromAddress"] = "no-reply@example.invalid",
                ["Email:PublicBaseUrl"] = "https://portal.example.invalid",
                ["Iyzipay:ApiKey"] = "synthetic-api-key",
                ["Iyzipay:SecretKey"] = "synthetic-secret-key",
                ["Iyzipay:BaseUrl"] = "https://sandbox-api.iyzipay.com",
                ["Iyzipay:CallbackUrl"] = "https://envantertr.com/api/checkout/callback"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IyzipayCheckoutClient>();
                services.AddSingleton(_ => new IyzipayCheckoutClient(
                    new HttpClient(provider) { BaseAddress = new Uri("https://sandbox-api.iyzipay.com") },
                    Options.Create(new IyzipayOptions
                    {
                        ApiKey = "synthetic-api-key", SecretKey = "synthetic-secret-key",
                        BaseUrl = "https://sandbox-api.iyzipay.com",
                        CallbackUrl = "https://envantertr.com/api/checkout/callback"
                    })));
            });
        });
        using var client = checkoutFactory.CreateClient();
        using var csrfResponse = await client.GetAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var request = new InitializeCompanyCheckoutRequest(checkoutId, "Synthetic Checkout Ltd", $"checkout-{checkoutId:N}"[..20],
            "owner@example.invalid", "Test", "Owner", $"owner-{checkoutId:N}@example.invalid", "+905550000002", "10000000146",
            "Synthetic Street 2", "Istanbul", "Turkey", "34000", plan.Id, "monthly");
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/initialize") { Content = JsonContent.Create(request) };
        message.Headers.Add("X-CSRF-TOKEN", requestToken);
        message.Headers.Add("Cookie", antiforgeryCookie);
        using var initialized = await client.SendAsync(message);
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        using var initializedPayload = JsonDocument.Parse(await initialized.Content.ReadAsStringAsync());
        Assert.StartsWith("https://sandbox-api.iyzipay.com/checkoutform/", initializedPayload.RootElement.GetProperty("checkoutUrl").GetString());
        Assert.Equal(125.50m, provider.InitializedAmount);

        await using (var scope = checkoutFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.CompanyCheckoutOrders.SingleAsync(x => x.Id == checkoutId);
            Assert.Equal(125.50m, order.Amount);
            Assert.Equal("PaymentStarted", order.Status);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("synthetic-provider-token"))), order.CheckoutTokenHash);
            Assert.DoesNotContain("https://sandbox-api", order.CheckoutPageUrlProtected, StringComparison.Ordinal);
        }

        using var callbackClient = checkoutFactory.CreateClient();
        using var callback = await callbackClient.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = "synthetic-provider-token" }));
        Assert.Equal(HttpStatusCode.OK, callback.StatusCode);
        Assert.Contains("Ödeme alındı", await callback.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(1, provider.RetrieveCount);

        using var replay = await callbackClient.PostAsync("/api/checkout/callback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = "synthetic-provider-token" }));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, provider.RetrieveCount);
        await using (var scope = checkoutFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.CompanyCheckoutOrders.SingleAsync(x => x.Id == checkoutId);
            Assert.Equal("Provisioned", order.Status);
            Assert.NotNull(order.CompanyId);
            Assert.Equal(1, await db.Companies.CountAsync(x => x.Slug == request.Slug));
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(x => x.CompanyId == order.CompanyId &&
                x.Status == "Active" && !x.IsDemo && x.PlanId == plan.Id && x.PaymentReference == "synthetic-payment-id"));
            Assert.Equal(1, await db.CompanyMemberships.CountAsync(x => x.CompanyId == order.CompanyId && x.RoleKey == "owner"));
        }
    }

    [Fact]
    public async Task Bulk_asset_edits_are_atomic_company_scoped_version_checked_and_audited()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var secondAssetId = _ownSecondAssetId;
        var targetCategoryId = Guid.NewGuid();
        Guid ownCategoryId;
        Guid brandId;
        Guid modelId;
        const string bulkCustomFieldKey = "bulk_reference";
        const string requiredCustomFieldKey = "legacy_required";
        const string inactiveCustomFieldKey = "inactive_value";
        List<BulkAssetVersion> versions;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ownAsset = await db.Assets.SingleAsync(asset => asset.CompanyId == _companyId && asset.Id == _ownAssetId);
            ownCategoryId = ownAsset.CategoryId;
            var brand = new AssetBrand { CompanyId = _companyId, Name = $"Bulk brand {_userId:N}" };
            var model = new AssetModel { CompanyId = _companyId, BrandId = brand.Id, CategoryId = ownCategoryId, Name = $"Bulk model {_userId:N}" };
            brandId = brand.Id;
            modelId = model.Id;
            db.AssetBrands.Add(brand);
            db.AssetModels.Add(model);
            db.AssetCategories.Add(new AssetCategory { Id = targetCategoryId, CompanyId = _companyId, Name = $"Bulk target {_userId:N}" });
            ownAsset.BrandId = brandId;
            ownAsset.ModelId = modelId;
            ownAsset.SerialNumber = "OWN-SERIAL-OLD";
            ownAsset.Notes = "Keep the first asset note";
            ownAsset.CustomFieldsJson = JsonSerializer.Serialize(new Dictionary<string, string>
                { [bulkCustomFieldKey] = "old-first", ["preserve_me"] = "first-value" });
            ownAsset.PurchaseDate = new DateOnly(2025, 1, 2);
            ownAsset.WarrantyExpiresAt = new DateOnly(2027, 1, 2);
            ownAsset.PurchaseCost = 10m;
            ownAsset.Currency = "TRY";
            db.Assets.Add(new Asset
            {
                Id = secondAssetId, CompanyId = _companyId, AssetTag = $"BULK-{_userId:N}",
                Name = "Second bulk asset", CategoryId = ownCategoryId, BrandId = brandId, ModelId = modelId,
                SerialNumber = "SECOND-SERIAL-OLD",
                Notes = "Keep the second asset note", PurchaseDate = new DateOnly(2024, 3, 4),
                WarrantyExpiresAt = new DateOnly(2026, 3, 4), PurchaseCost = 20m, Currency = "TRY",
                CustomFieldsJson = JsonSerializer.Serialize(new Dictionary<string, string>
                    { [bulkCustomFieldKey] = "old-second", ["preserve_me"] = "second-value" })
            });
            db.AssetCustomFieldDefinitions.AddRange(
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = bulkCustomFieldKey,
                    Name = "Bulk reference", FieldType = "text", IsRequired = false },
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = requiredCustomFieldKey,
                    Name = "Legacy required field", FieldType = "text", IsRequired = true },
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = inactiveCustomFieldKey,
                    Name = "Inactive field", FieldType = "text", IsActive = false });
            await db.SaveChangesAsync();
            versions = await db.Assets.AsNoTracking().Where(asset => asset.CompanyId == _companyId &&
                    (asset.Id == _ownAssetId || asset.Id == secondAssetId))
                .Select(asset => new BulkAssetVersion(asset.Id, asset.UpdatedAt)).ToListAsync();
            Assert.Equal(2, versions.Count);
        }

        var successPayload = new
        {
            assets = versions,
            fields = new[] { "name", "purchaseCost", "warrantyExpiresAt", "isRequestable", "serialNumber", "notes", "customFields" },
            name = "Bulk refreshed assets", purchaseCost = 1234.50m, currency = "EUR",
            warrantyExpiresAt = (DateOnly?)null, isRequestable = false, serialNumber = "BULK-SERIAL-2026",
            notes = "Reviewed during October inventory", customFields = new Dictionary<string, string?> { [bulkCustomFieldKey] = "reviewed" },
            note = "October inventory review"
        };
        using (var response = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(successPayload), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, payload.RootElement.GetProperty("updated").GetInt32());
            Assert.Equal(0, payload.RootElement.GetProperty("unchanged").GetInt32());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = await db.Assets.AsNoTracking().Where(asset => asset.CompanyId == _companyId &&
                (asset.Id == _ownAssetId || asset.Id == secondAssetId)).ToListAsync();
            Assert.Equal(2, saved.Count);
            Assert.All(saved, asset =>
            {
                Assert.Equal("Bulk refreshed assets", asset.Name);
                Assert.Equal(1234.50m, asset.PurchaseCost);
                Assert.Equal("EUR", asset.Currency);
                Assert.Null(asset.WarrantyExpiresAt);
                Assert.False(asset.IsRequestable);
                Assert.Equal(ownCategoryId, asset.CategoryId);
                Assert.Equal(brandId, asset.BrandId);
                Assert.Equal(modelId, asset.ModelId);
                Assert.Equal(AssetStatuses.Available, asset.Status);
            });
            Assert.All(saved, asset =>
            {
                Assert.Equal("BULK-SERIAL-2026", asset.SerialNumber);
                Assert.Equal("Reviewed during October inventory", asset.Notes);
                using var customValues = JsonDocument.Parse(asset.CustomFieldsJson!);
                Assert.Equal("reviewed", customValues.RootElement.GetProperty(bulkCustomFieldKey).GetString());
                Assert.Equal(asset.Id == _ownAssetId ? "first-value" : "second-value",
                    customValues.RootElement.GetProperty("preserve_me").GetString());
                Assert.False(customValues.RootElement.TryGetProperty(requiredCustomFieldKey, out _));
            });
            var events = await db.AssetEvents.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
                (entry.AssetId == _ownAssetId || entry.AssetId == secondAssetId) && entry.Details!.Contains("October inventory review"))
                .ToListAsync();
            Assert.Equal(2, events.Count);
            Assert.All(events, entry => Assert.Contains("Toplu varlık düzenleme:", entry.Details));
            Assert.Equal(2, await db.AuditEntries.CountAsync(entry => entry.CompanyId == _companyId &&
                (entry.TargetId == _ownAssetId || entry.TargetId == secondAssetId) && entry.Action == "BulkUpdated"));
            versions = await db.Assets.AsNoTracking().Where(asset => asset.CompanyId == _companyId &&
                    (asset.Id == _ownAssetId || asset.Id == secondAssetId))
                .Select(asset => new BulkAssetVersion(asset.Id, asset.UpdatedAt)).ToListAsync();
        }

        using (var clearOptionalCustomValue = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "customFields" },
                       customFields = new Dictionary<string, string?> { [bulkCustomFieldKey] = "" }
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, clearOptionalCustomValue.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var asset in await db.Assets.AsNoTracking().Where(item => item.CompanyId == _companyId &&
                         (item.Id == _ownAssetId || item.Id == secondAssetId)).ToListAsync())
            {
                using var customValues = JsonDocument.Parse(asset.CustomFieldsJson!);
                Assert.False(customValues.RootElement.TryGetProperty(bulkCustomFieldKey, out _));
                Assert.Equal(asset.Id == _ownAssetId ? "first-value" : "second-value",
                    customValues.RootElement.GetProperty("preserve_me").GetString());
            }
            versions = await db.Assets.AsNoTracking().Where(item => item.CompanyId == _companyId &&
                    (item.Id == _ownAssetId || item.Id == secondAssetId))
                .Select(item => new BulkAssetVersion(item.Id, item.UpdatedAt)).ToListAsync();
        }

        using (var clearRequiredCustomValue = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "customFields" },
                       customFields = new Dictionary<string, string?> { [requiredCustomFieldKey] = "" }
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, clearRequiredCustomValue.StatusCode);
        using (var editInactiveCustomValue = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "customFields" },
                       customFields = new Dictionary<string, string?> { [inactiveCustomFieldKey] = "blocked" }
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, editInactiveCustomValue.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.Assets.CountAsync(asset => asset.CompanyId == _companyId &&
                (asset.Id == _ownAssetId || asset.Id == secondAssetId) && asset.SerialNumber == "BULK-SERIAL-2026" &&
                asset.Notes == "Reviewed during October inventory"));
            versions = await db.Assets.AsNoTracking().Where(item => item.CompanyId == _companyId &&
                    (item.Id == _ownAssetId || item.Id == secondAssetId))
                .Select(item => new BulkAssetVersion(item.Id, item.UpdatedAt)).ToListAsync();
        }

        using (var incompatibleCategory = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "categoryId" }, categoryId = targetCategoryId
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, incompatibleCategory.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var legacyModel = new AssetModel { CompanyId = _companyId, BrandId = brandId,
                CategoryId = targetCategoryId, Name = $"Legacy mismatch {_userId:N}" };
            db.AssetModels.Add(legacyModel);
            var asset = await db.Assets.SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownAssetId);
            asset.ModelId = legacyModel.Id;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            versions = await db.Assets.AsNoTracking().Where(item => item.CompanyId == _companyId &&
                    (item.Id == _ownAssetId || item.Id == secondAssetId))
                .Select(item => new BulkAssetVersion(item.Id, item.UpdatedAt)).ToListAsync();
        }
        using (var unrelatedFieldEdit = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "name" }, name = "Name only edit"
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, unrelatedFieldEdit.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            versions = await db.Assets.AsNoTracking().Where(item => item.CompanyId == _companyId &&
                    (item.Id == _ownAssetId || item.Id == secondAssetId))
                .Select(item => new BulkAssetVersion(item.Id, item.UpdatedAt)).ToListAsync();
        }

        var staleVersions = versions.Select((version, index) => index == 0
            ? version with { UpdatedAt = version.UpdatedAt.AddSeconds(-1) }
            : version).ToArray();
        using (var stale = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = staleVersions, fields = new[] { "name" }, name = "Must not partially save"
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        BulkAssetVersion foreignVersion;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreignVersion = await db.Assets.AsNoTracking().Where(asset => asset.Id == _otherCompanyAssetId)
                .Select(asset => new BulkAssetVersion(asset.Id, asset.UpdatedAt)).SingleAsync();
        }
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = new[] { versions[0], foreignVersion }, fields = new[] { "name" }, name = "Cross tenant write"
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var forbidden = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/bulk-update", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       assets = versions, fields = new[] { "name" }, name = "Viewer cannot bulk edit"
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.Assets.CountAsync(asset => asset.CompanyId == _companyId &&
                (asset.Id == _ownAssetId || asset.Id == secondAssetId) && asset.Name == "Name only edit"));
        }
    }

    [Fact]
    public async Task Asset_contracts_are_company_scoped_audited_and_support_safe_attachments()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var contractPath = "/api/contracts";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var licenseDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var license = await licenseDb.SoftwareLicenses.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownLicenseId);
            license.LicenseReference = "DO-NOT-EXPORT-LICENSE-REFERENCE";
            await licenseDb.SaveChangesAsync();
        }

        using (var crossTenantLink = await SendAsRoleAsync(HttpMethod.Post, contractPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       name = "Invalid tenant contract", currency = "TRY",
                       assetIds = new[] { _ownAssetId, _otherCompanyAssetId }
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, crossTenantLink.StatusCode);

        using var create = await SendAsRoleAsync(HttpMethod.Post, contractPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                name = "Annual hardware support", contractNumber = "SUP-2026-01", contractType = "Support",
                startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                endDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), cost = 1200m, currency = "USD",
                notes = "=HYPERLINK(\"https://invalid.example\")", assetIds = new[] { _ownAssetId },
                licenseIds = new[] { _ownLicenseId }
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var contractId = created.RootElement.GetProperty("id").GetGuid();

        await SetCompanyRoleAsync("viewer");
        using (var forbidden = await SendAsRoleAsync(HttpMethod.Post, contractPath, "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { name = "Viewer cannot create", currency = "TRY" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var detail = await SendAsAdminAsync($"/api/contracts/{contractId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal("expiring", payload.RootElement.GetProperty("contract").GetProperty("status").GetString());
            Assert.Equal(_ownAssetId, payload.RootElement.GetProperty("assets")[0].GetProperty("id").GetGuid());
            Assert.Equal(_ownLicenseId, payload.RootElement.GetProperty("licenses")[0].GetProperty("id").GetGuid());
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var tenantDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            tenantDb.CompanyMemberships.Add(new CompanyMembership
                { CompanyId = _otherCompanyId, UserId = _sameCompanyOtherUserId, RoleKey = "admin" });
            await tenantDb.SaveChangesAsync();
        }
        using (var otherTenant = await SendAsUserAsync(HttpMethod.Get, $"/api/contracts/{contractId}",
                   _sameCompanyOtherUserId, _otherCompanyId, role: "admin"))
            Assert.Equal(HttpStatusCode.NotFound, otherTenant.StatusCode);

        using (var invalidUpload = new MultipartFormDataContent())
        {
            invalidUpload.Add(new ByteArrayContent("not a pdf"u8.ToArray()), "file", "contract.pdf");
            using var response = await SendAsRoleAsync(HttpMethod.Post, $"/api/contracts/{contractId}/attachments", "admin",
                invalidUpload, requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Guid attachmentId;
        using (var validUpload = new MultipartFormDataContent())
        {
            validUpload.Add(new ByteArrayContent("%PDF-1.7\ncontract"u8.ToArray()), "file", "support.pdf");
            using var response = await SendAsRoleAsync(HttpMethod.Post, $"/api/contracts/{contractId}/attachments", "admin",
                validUpload, requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            attachmentId = payload.RootElement.GetProperty("id").GetGuid();
            using var download = await SendAsAdminAsync($"/api/contracts/{contractId}/attachments/{attachmentId}/file");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("%PDF-1.7\ncontract"u8.ToArray(), await download.Content.ReadAsByteArrayAsync());

            using var companyExport = await SendAsAdminAsync("/api/company/export.json");
            Assert.Equal(HttpStatusCode.OK, companyExport.StatusCode);
            using var exportJson = JsonDocument.Parse(await companyExport.Content.ReadAsStringAsync());
            Assert.Contains(exportJson.RootElement.GetProperty("assetContracts").EnumerateArray(),
                item => item.GetProperty("id").GetGuid() == contractId);
            Assert.Contains(exportJson.RootElement.GetProperty("assetContractAssets").EnumerateArray(),
                item => item.GetProperty("contractId").GetGuid() == contractId && item.GetProperty("assetId").GetGuid() == _ownAssetId);
            Assert.Contains(exportJson.RootElement.GetProperty("assetContractLicenses").EnumerateArray(),
                item => item.GetProperty("contractId").GetGuid() == contractId && item.GetProperty("licenseId").GetGuid() == _ownLicenseId);
            var exportedLicense = Assert.Single(exportJson.RootElement.GetProperty("softwareLicenses").EnumerateArray(),
                item => item.GetProperty("id").GetGuid() == _ownLicenseId);
            Assert.Equal(JsonValueKind.Null, exportedLicense.GetProperty("licenseReference").ValueKind);

            using var archiveResponse = await SendAsAdminAsync("/api/company/export.zip");
            Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
            using var archive = new ZipArchive(new MemoryStream(await archiveResponse.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
            Assert.Contains(archive.Entries, entry => entry.FullName ==
                $"media/contracts/{contractId:N}/attachments/{attachmentId:N}.pdf");
        }
        using (var files = await SendAsAdminAsync($"/api/contracts/{contractId}/attachments"))
        {
            using var payload = JsonDocument.Parse(await files.Content.ReadAsStringAsync());
            Assert.Equal("support.pdf", payload.RootElement.GetProperty("items")[0].GetProperty("originalFileName").GetString());
        }

        await SetCompanyRoleAsync("viewer");
        using (var viewerDelete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/contracts/{contractId}/attachments/{attachmentId}", "viewer",
                   requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerDelete.StatusCode);
        using (var otherTenantCsrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _otherCompanyId))
        using (var otherTenantCsrfPayload = JsonDocument.Parse(await otherTenantCsrf.Content.ReadAsStringAsync()))
        {
            var otherTenantToken = otherTenantCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
            using var crossTenantDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/contracts/{contractId}/attachments/{attachmentId}",
                _sameCompanyOtherUserId, _otherCompanyId, requestToken: otherTenantToken, antiforgeryCookie: antiforgeryCookie, role: "admin");
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDelete.StatusCode);
        }
        await SetCompanyRoleAsync("admin");
        using (var delete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/contracts/{contractId}/attachments/{attachmentId}", "admin",
                   requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using (var remainingFiles = await SendAsAdminAsync($"/api/contracts/{contractId}/attachments"))
        using (var payload = JsonDocument.Parse(await remainingFiles.Content.ReadAsStringAsync()))
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
        using (var deletedDownload = await SendAsAdminAsync($"/api/contracts/{contractId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, deletedDownload.StatusCode);

        using (var export = await SendAsAdminAsync("/api/contracts/export.csv"))
        {
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("'=" , csv, StringComparison.Ordinal);
        }
        await SetCompanyRoleAsync("admin");
        using (var archive = await SendAsRoleAsync(HttpMethod.Post, $"/api/contracts/{contractId}/archive", "admin",
                   content: null, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        using (var archivedCsv = await SendAsAdminAsync("/api/contracts/export.csv"))
        {
            Assert.Equal(HttpStatusCode.OK, archivedCsv.StatusCode);
            var csv = await archivedCsv.Content.ReadAsStringAsync();
            Assert.Contains("Annual hardware support", csv, StringComparison.Ordinal);
            Assert.Contains("Arşiv", csv, StringComparison.Ordinal);
        }
        using (var restore = await SendAsRoleAsync(HttpMethod.Post, $"/api/contracts/{contractId}/restore", "admin",
                   content: null, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var db = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await db.AuditEntries.Where(x => x.CompanyId == _companyId && x.TargetId == contractId).ToListAsync(),
            item => item.Action == "contract.created");
        Assert.Equal(1, await db.AssetContractAssets.CountAsync(x => x.CompanyId == _companyId && x.ContractId == contractId));
        Assert.Equal(1, await db.AssetContractLicenses.CountAsync(x => x.CompanyId == _companyId && x.ContractId == contractId));
    }

    [Fact]
    public async Task Company_tasks_are_company_scoped_audited_and_enforce_assigned_employee_transitions()
    {
        await using (var fixtureScope = _factory!.Services.CreateAsyncScope())
        {
            var fixtureDb = fixtureScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await fixtureDb.CompanyMemberships.SingleAsync(item =>
                item.CompanyId == _companyId && item.UserId == _userId);
            membership.EmployeeId = _ownEmployeeId;
            await fixtureDb.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var title = $"OWN-TASK-{Guid.NewGuid():N}";
        var assignedPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            title,
            description = "Tenant-scoped acceptance task",
            priority = "High",
            assignedEmployeeId = _ownEmployeeId,
            dueAt = (DateTimeOffset?)null
        });
        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, "/api/tasks", "admin", assignedPayload))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/tasks", "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                title,
                description = "Tenant-scoped acceptance task",
                priority = "High",
                assignedEmployeeId = _ownEmployeeId,
                dueAt = (DateTimeOffset?)null
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var taskId = created.RootElement.GetProperty("id").GetGuid();

        using (var filtered = await SendAsAdminAsync($"/api/tasks?q={Uri.EscapeDataString(title)}&status=Open&priority=High&page=1&pageSize=1"))
        {
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            using var payload = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(taskId, Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        }
        using (var invalidStatus = await SendAsAdminAsync("/api/tasks?status=Unknown"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        using (var invalidPriority = await SendAsAdminAsync("/api/tasks?priority=Unknown"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidPriority.StatusCode);
        var updatedTitle = $"{title}-UPDATED";
        var dueAt = DateTimeOffset.UtcNow.AddDays(5);
        using (var update = await SendAsRoleAsync(HttpMethod.Put, $"/api/tasks/{taskId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { title = updatedTitle, description = "Updated task details", priority = "Urgent", assignedEmployeeId = _ownEmployeeId, dueAt }),
                   requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var payload = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
            Assert.Equal(updatedTitle, payload.RootElement.GetProperty("title").GetString());
            Assert.Equal("Urgent", payload.RootElement.GetProperty("priority").GetString());
        }

        using (var unassignedResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/tasks", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       title = $"OWN-UNASSIGNED-{Guid.NewGuid():N}",
                       description = (string?)null,
                       priority = "Normal",
                       assignedEmployeeId = (Guid?)null,
                       dueAt = (DateTimeOffset?)null
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Created, unassignedResponse.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var memberList = await SendAsRoleAsync(HttpMethod.Get, "/api/tasks", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, memberList.StatusCode);
            using var payload = JsonDocument.Parse(await memberList.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("canManage").GetBoolean());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("summary").GetProperty("open").GetInt32());
            Assert.Equal(taskId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.Equal(updatedTitle, payload.RootElement.GetProperty("items")[0].GetProperty("title").GetString());
        }
        using (var forbiddenCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/tasks", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { title = "Not allowed", priority = "Low" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);
        using (var forbiddenUpdate = await SendAsRoleAsync(HttpMethod.Put, $"/api/tasks/{taskId}", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { title = "Not allowed", priority = "Low" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);
        using (var invalidTransition = await SendAsRoleAsync(HttpMethod.Patch, $"/api/tasks/{taskId}/status", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { status = "Completed" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, invalidTransition.StatusCode);
        using (var start = await SendAsRoleAsync(HttpMethod.Patch, $"/api/tasks/{taskId}/status", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { status = "InProgress" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using (var complete = await SendAsRoleAsync(HttpMethod.Patch, $"/api/tasks/{taskId}/status", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { status = "Completed" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        using (var reopen = await SendAsRoleAsync(HttpMethod.Patch, $"/api/tasks/{taskId}/status", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { status = "InProgress" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, reopen.StatusCode);

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Completed", await auditDb.CompanyTasks.Where(task => task.CompanyId == _companyId && task.Id == taskId)
            .Select(task => task.Status).SingleAsync());
        Assert.Equal(4, await auditDb.AuditEntries.CountAsync(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "CompanyTask" && entry.TargetId == taskId));
    }

    [Fact]
    public async Task Support_tickets_enforce_tenant_ownership_csrf_platform_replies_and_audit()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var subject = $"SUPPORT-{Guid.NewGuid():N}";

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, "/api/support/tickets", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { subject, category = "Access", message = "Sign-in assistance" })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using var create = await SendAsRoleAsync(HttpMethod.Post, "/api/support/tickets", "admin",
            System.Net.Http.Json.JsonContent.Create(new { subject, category = "Access", message = "Sign-in assistance" }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var ticketId = created.RootElement.GetProperty("id").GetGuid();

        await FlushSupportTicketEmailsAsync(ticketId, expectedSentCount: 2);
        await using (var notificationScope = _factory!.Services.CreateAsyncScope())
        {
            var notificationDb = notificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var initialNotifications = await notificationDb.SupportTicketEmailOutbox.AsNoTracking()
                .Where(item => item.CompanyId == _companyId && item.TicketId == ticketId).ToListAsync();
            Assert.Equal(2, initialNotifications.Count);
            Assert.All(initialNotifications, item => Assert.Equal("Sent", item.Status));
            Assert.Contains(initialNotifications, item => item.RecipientEmail == "support@example.invalid" && item.EventType == "TicketCreated");
            Assert.Contains(initialNotifications, item => item.RecipientEmail == $"acceptance-{_userId:N}@example.invalid" && item.EventType == "TicketCreated");
        }

        using (var managerList = await SendAsRoleAsync(HttpMethod.Get, "/api/support/tickets", "admin"))
        using (var payload = JsonDocument.Parse(await managerList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, managerList.StatusCode);
            Assert.Contains(payload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == ticketId);
            Assert.True(payload.RootElement.GetProperty("canViewAll").GetBoolean());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var secondMember = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            secondMember.RoleKey = "viewer";
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        using (var otherMemberList = await SendAsUserAsync(HttpMethod.Get, "/api/support/tickets", _sameCompanyOtherUserId, _companyId, role: "viewer"))
        using (var payload = JsonDocument.Parse(await otherMemberList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, otherMemberList.StatusCode);
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.False(payload.RootElement.GetProperty("canViewAll").GetBoolean());
        }
        using (var otherMemberDetail = await SendAsUserAsync(HttpMethod.Get, $"/api/support/tickets/{ticketId}", _sameCompanyOtherUserId, _companyId, role: "viewer"))
            Assert.Equal(HttpStatusCode.NotFound, otherMemberDetail.StatusCode);
        using (var wrongTenant = await SendAsUserAsync(HttpMethod.Get, $"/api/support/tickets/{ticketId}", _sameCompanyOtherUserId, _otherCompanyId))
            Assert.True(wrongTenant.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
                $"Cross-tenant access must fail closed; received {(int)wrongTenant.StatusCode}.");

        using var platformCsrfResponse = await SendAsPlatformAdminRequestAsync(HttpMethod.Get, "/api/session/csrf");
        using var platformCsrfPayload = JsonDocument.Parse(await platformCsrfResponse.Content.ReadAsStringAsync());
        var platformRequestToken = platformCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var platformAntiforgeryCookie = antiforgeryCookie;
        using (var platformReply = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, $"/api/platform/support/tickets/{ticketId}/messages",
                   System.Net.Http.Json.JsonContent.Create(new { body = "We are checking your account access." }), platformRequestToken, platformAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.Created, platformReply.StatusCode);
        await FlushSupportTicketEmailsAsync(ticketId, expectedSentCount: 3);
        using (var statusAfterReply = await SendAsRoleAsync(HttpMethod.Get, $"/api/support/tickets/{ticketId}", "admin"))
        using (var payload = JsonDocument.Parse(await statusAfterReply.Content.ReadAsStringAsync()))
        {
            Assert.Equal("AwaitingCustomer", payload.RootElement.GetProperty("ticket").GetProperty("status").GetString());
            Assert.Contains(payload.RootElement.GetProperty("messages").EnumerateArray(), message => message.GetProperty("isSupportReply").GetBoolean());
        }

        using (var customerReply = await SendAsRoleAsync(HttpMethod.Post, $"/api/support/tickets/{ticketId}/messages", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { body = "Thank you; this is resolved." }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Created, customerReply.StatusCode);
        await FlushSupportTicketEmailsAsync(ticketId, expectedSentCount: 4);
        using (var close = await SendAsRoleAsync(HttpMethod.Post, $"/api/support/tickets/{ticketId}/close", "admin",
                   requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        await FlushSupportTicketEmailsAsync(ticketId, expectedSentCount: 5);
        using (var replyToClosed = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, $"/api/platform/support/tickets/{ticketId}/messages",
                   System.Net.Http.Json.JsonContent.Create(new { body = "A closed ticket cannot receive a reply." }), platformRequestToken, platformAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, replyToClosed.StatusCode);

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(3, await auditDb.AuditEntries.CountAsync(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "SupportTicket" && entry.TargetId == ticketId));
        Assert.Equal(1, await auditDb.PlatformAuditEntries.CountAsync(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "SupportTicket" && entry.TargetId == ticketId));
    }

    private async Task FlushSupportTicketEmailsAsync(Guid ticketId, int expectedSentCount)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            await using (var scope = _factory!.Services.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<SupportTicketEmailService>();
                await service.RunOnceAsync(CancellationToken.None);
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var sentCount = await db.SupportTicketEmailOutbox.AsNoTracking()
                    .CountAsync(item => item.TicketId == ticketId && item.Status == "Sent");
                if (sentCount >= expectedSentCount) return;
            }
            await Task.Delay(250);
        }

        await using var finalScope = _factory!.Services.CreateAsyncScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = await finalDb.SupportTicketEmailOutbox.AsNoTracking()
            .Where(item => item.TicketId == ticketId).Select(item => new { item.EventType, item.Status, item.LastErrorType }).ToListAsync();
        Assert.True(notifications.Count(item => item.Status == "Sent") >= expectedSentCount,
            $"Support email queue did not deliver {expectedSentCount} messages: {string.Join(", ", notifications.Select(item => $"{item.EventType}:{item.Status}:{item.LastErrorType}"))}");
    }

    [Fact]
    public async Task Bulk_employee_status_changes_are_company_scoped_atomic_and_audited()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(new Employee { Id = _ownSecondEmployeeId, CompanyId = _companyId,
                EmployeeNumber = $"OWN-SECOND-{_userId:N}", DisplayName = "Second own employee" });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var employeeIds = new[] { _ownEmployeeId, _ownSecondEmployeeId };

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds, isActive = false })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using (var duplicateIds = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { _ownEmployeeId, _ownEmployeeId }, isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, duplicateIds.StatusCode);
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { _ownEmployeeId, _otherCompanyEmployeeId }, isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var viewerWrite = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-active", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds, isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerWrite.StatusCode);
        await SetCompanyRoleAsync("admin");

        using (var summary = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-offboarding-summary", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds, isActive = false }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
            using var payload = JsonDocument.Parse(await summary.Content.ReadAsStringAsync());
            Assert.Equal(2, payload.RootElement.GetProperty("items").GetArrayLength());
        }
        using (var update = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/bulk-active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeIds, isActive = false }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var payload = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
            Assert.Equal(2, payload.RootElement.GetProperty("changed").GetInt32());
        }

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.All(await auditDb.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id)).ToListAsync(), employee => Assert.False(employee.IsActive));
        Assert.Equal(2, await auditDb.AuditEntries.CountAsync(entry => entry.CompanyId == _companyId &&
            entry.TargetId.HasValue && employeeIds.Contains(entry.TargetId.Value) && entry.Action == "Deactivated"));
    }
    private string? _previousConnectionString;
    private string? _previousForwardedHeadersSetting;
    private readonly SmtpCaptureServer _smtpCapture = new();
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), "envantertr-security-media-" + Guid.NewGuid().ToString("N"));
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [Fact]
    public async Task Every_api_endpoint_declares_an_explicit_authentication_boundary()
    {
        var endpoints = _factory!.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        Assert.True(endpoints.Length >= 300, $"Expected the complete API endpoint inventory, found {endpoints.Length}.");
        var unprotected = endpoints.Where(endpoint =>
                endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null &&
                !endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any())
            .Select(DescribeEndpoint)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.True(unprotected.Length == 0,
            "API endpoints need explicit authorization or AllowAnonymous metadata:\n" + string.Join("\n", unprotected));

        var actualPublicRoutes = endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(DescribeEndpoint)
            .ToHashSet(StringComparer.Ordinal);
        var expectedPublicRoutes = new HashSet<string>(StringComparer.Ordinal)
        {
            "GET /api/status",
            "GET /api/setup/status",
            "GET /api/session/csrf",
            "GET /api/platform/bootstrap/status",
            "POST /api/setup/initial-owner",
            "POST /api/platform/bootstrap",
            "POST /api/session/login",
            "POST /api/session/password/forgot",
            "POST /api/session/password/reset",
            "POST /api/checkout/initialize",
            "POST /api/checkout/callback",
            "GET /api/plans",
            "GET /api/site-content",
            "POST /api/demo-requests",
            "GET /api/v1",
            "GET /api/v1/accessories",
            "GET /api/v1/assets",
            "GET /api/v1/employees"
        };
        Assert.True(expectedPublicRoutes.SetEquals(actualPublicRoutes),
            "Review anonymous API route changes. Unexpected: " +
            string.Join(", ", actualPublicRoutes.Except(expectedPublicRoutes).OrderBy(x => x, StringComparer.Ordinal)) +
            "; missing: " +
            string.Join(", ", expectedPublicRoutes.Except(actualPublicRoutes).OrderBy(x => x, StringComparer.Ordinal)));

        var policies = endpoints.SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>())
            .Select(data => data.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Select(policy => policy!)
            .ToHashSet(StringComparer.Ordinal);
        var expectedPolicies = new HashSet<string>(StringComparer.Ordinal)
        {
            "asset.read", "asset.write", "settings.manage", "people.read", "people.manage",
            "people.files.read", "people.files.manage", "company.member", "platform.admin"
        };
        Assert.True(expectedPolicies.SetEquals(policies),
            "Review API authorization policy coverage. Unexpected: " +
            string.Join(", ", policies.Except(expectedPolicies).OrderBy(x => x, StringComparer.Ordinal)) +
            "; unused: " +
            string.Join(", ", expectedPolicies.Except(policies).OrderBy(x => x, StringComparer.Ordinal)));
    }

    [Fact]
    public async Task Company_units_are_hierarchical_tenant_scoped_and_archived_without_hard_deletes()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var suffix = _userId.ToString("N");
        var rootName = $"Org root {suffix}";
        var childName = $"Org child {suffix}";
        var otherUnitId = Guid.NewGuid();
        var auditorId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyUnits.Add(new CompanyUnit { Id = otherUnitId, CompanyId = _otherCompanyId,
                Name = $"Private company unit {suffix}", NormalizedName = $"PRIVATE COMPANY UNIT {suffix}" });
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId,
                UserId = _sameCompanyOtherUserId, RoleKey = "admin" });
            db.Users.Add(new EnvanterUser { Id = auditorId, UserName = $"company-units-auditor-{auditorId:N}",
                NormalizedUserName = $"COMPANY-UNITS-AUDITOR-{auditorId:N}".ToUpperInvariant(),
                Email = $"company-units-auditor-{auditorId:N}@example.invalid",
                NormalizedEmail = $"COMPANY-UNITS-AUDITOR-{auditorId:N}@EXAMPLE.INVALID".ToUpperInvariant(),
                EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"), DisplayName = "Company unit auditor" });
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId,
                UserId = auditorId, RoleKey = "auditor" });
            await db.SaveChangesAsync();
        }

        using (var readOnlyRole = await SendAsUserAsync(HttpMethod.Get, "/api/company-units", auditorId, _companyId, role: "auditor"))
            Assert.Equal(HttpStatusCode.OK, readOnlyRole.StatusCode);
        using (var readOnlyWrite = await SendAsUserAsync(HttpMethod.Post, "/api/company-units", auditorId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { name = $"Denied {suffix}" }), requestToken, antiforgeryCookie, role: "auditor"))
            Assert.Equal(HttpStatusCode.Forbidden, readOnlyWrite.StatusCode);

        Guid rootId;
        using (var createRoot = await SendAsRoleAsync(HttpMethod.Post, "/api/company-units", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = rootName, code = $"ROOT-{suffix}" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, createRoot.StatusCode);
            using var payload = JsonDocument.Parse(await createRoot.Content.ReadAsStringAsync());
            rootId = payload.RootElement.GetProperty("id").GetGuid();
        }
        Guid childId;
        using (var createChild = await SendAsRoleAsync(HttpMethod.Post, "/api/company-units", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = childName, parentCompanyUnitId = rootId }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, createChild.StatusCode);
            using var payload = JsonDocument.Parse(await createChild.Content.ReadAsStringAsync());
            childId = payload.RootElement.GetProperty("id").GetGuid();
        }
        using (var duplicate = await SendAsRoleAsync(HttpMethod.Post, "/api/company-units", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = rootName }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using (var foreignParent = await SendAsRoleAsync(HttpMethod.Post, "/api/company-units", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = $"Foreign parent {suffix}", parentCompanyUnitId = otherUnitId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, foreignParent.StatusCode);
        using (var cycle = await SendAsRoleAsync(HttpMethod.Put, $"/api/company-units/{rootId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = rootName, code = $"ROOT-{suffix}", parentCompanyUnitId = childId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, cycle.StatusCode);
        using (var archiveParentWithChild = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{rootId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, archiveParentWithChild.StatusCode);

        using (var crossTenant = await SendAsUserAsync(HttpMethod.Get, "/api/company-units", _sameCompanyOtherUserId, _otherCompanyId))
        {
            Assert.Equal(HttpStatusCode.OK, crossTenant.StatusCode);
            using var payload = JsonDocument.Parse(await crossTenant.Content.ReadAsStringAsync());
            var names = payload.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("name").GetString()).ToArray();
            Assert.Contains($"Private company unit {suffix}", names);
            Assert.DoesNotContain(rootName, names);
        }
        using (var archiveChild = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{childId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, archiveChild.StatusCode);
        using (var archiveRoot = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{rootId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = false }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, archiveRoot.StatusCode);
        using (var defaultList = await SendAsRoleAsync(HttpMethod.Get, "/api/company-units", "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, defaultList.StatusCode);
            using var payload = JsonDocument.Parse(await defaultList.Content.ReadAsStringAsync());
            Assert.DoesNotContain(payload.RootElement.GetProperty("items").EnumerateArray(), item =>
                item.GetProperty("id").GetGuid() == rootId || item.GetProperty("id").GetGuid() == childId);
        }
        using (var includingArchived = await SendAsRoleAsync(HttpMethod.Get, "/api/company-units?includeInactive=true", "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, includingArchived.StatusCode);
            using var payload = JsonDocument.Parse(await includingArchived.Content.ReadAsStringAsync());
            Assert.Contains(payload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == rootId);
        }
        using (var restoreChildTooEarly = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{childId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = true }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, restoreChildTooEarly.StatusCode);
        using (var restoreRoot = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{rootId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = true }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restoreRoot.StatusCode);
        using (var restoreChild = await SendAsRoleAsync(HttpMethod.Patch, $"/api/company-units/{childId}/active", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { isActive = true }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restoreChild.StatusCode);
    }

    [Fact]
    public async Task Anonymous_http_requests_are_rejected_by_every_protected_api_route()
    {
        var protectedRoutes = _factory!.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true &&
                               endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .SelectMany(endpoint =>
            {
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"];
                var path = BuildSampleRoutePath(endpoint);
                var contentType = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IAcceptsMetadata>()?.ContentTypes.FirstOrDefault();
                return methods.Select(method => (Method: method, Path: path, ContentType: contentType));
            })
            .Distinct()
            .OrderBy(route => route.Path, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ToArray();

        Assert.True(protectedRoutes.Length >= 280,
            $"Expected to exercise the protected API surface, found {protectedRoutes.Length} method/path routes.");
        foreach (var route in protectedRoutes)
        {
            using var request = new HttpRequestMessage(new HttpMethod(route.Method), route.Path);
            if (route.Method is not ("GET" or "HEAD" or "OPTIONS"))
            {
                if (string.Equals(route.ContentType, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
                    request.Content = new MultipartFormDataContent();
                else
                    request.Content = new StringContent("{}", Encoding.UTF8,
                        string.IsNullOrWhiteSpace(route.ContentType) ? "application/json" : route.ContentType);
            }
            using var response = await _client!.SendAsync(request);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"Anonymous {route.Method} {route.Path} returned {(int)response.StatusCode} instead of 401.");
        }
    }

    [Fact]
    public async Task Every_api_route_policy_matches_the_company_and_platform_role_matrix()
    {
        var rolesByPolicy = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["asset.read"] = new(["owner", "admin", "asset_manager", "asset_operator", "auditor", "viewer"]),
            ["asset.write"] = new(["owner", "admin", "asset_manager", "asset_operator"]),
            ["settings.manage"] = new(["owner", "admin"]),
            ["people.read"] = new(["owner", "admin", "asset_manager", "asset_operator", "auditor", "hr_operator"]),
            ["people.manage"] = new(["owner", "admin", "asset_manager", "hr_operator"]),
            ["people.files.read"] = new(["owner", "admin", "hr_operator"]),
            ["people.files.manage"] = new(["owner", "admin", "hr_operator"]),
            ["company.member"] = new(["owner", "admin", "asset_manager", "asset_operator", "auditor", "viewer", "hr_operator"]),
            ["platform.admin"] = new(["platform_admin"])
        };
        var roles = rolesByPolicy.Values.SelectMany(roles => roles).Distinct(StringComparer.Ordinal).ToArray();
        var endpoints = _factory!.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true &&
                               endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .ToArray();
        Assert.True(endpoints.Length >= 300, $"Expected at least 300 protected API endpoints, found {endpoints.Length}.");

        var authorization = _factory.Services.GetRequiredService<IAuthorizationService>();
        var policyProvider = _factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        foreach (var endpoint in endpoints)
        {
            var authorizationData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var policies = authorizationData
                .Select(data => data.Policy)
                .Where(policy => !string.IsNullOrWhiteSpace(policy))
                .Select(policy => policy!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var combinedPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizationData);
            Assert.NotNull(combinedPolicy);
            foreach (var role in roles)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                    new Claim("company_role", role),
                    new Claim("company_id", Guid.NewGuid().ToString("D")),
                };
                if (role == "platform_admin") claims.Add(new Claim("platform_admin", "true"));
                var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "RoutePolicyAcceptance"));
                var expected = policies.All(policy => rolesByPolicy.TryGetValue(policy, out var allowedRoles) && allowedRoles.Contains(role));
                var result = await authorization.AuthorizeAsync(user, resource: null, combinedPolicy!);
                Assert.True(expected == result.Succeeded,
                    $"Policy matrix mismatch for role '{role}' at {DescribeEndpoint(endpoint)}: expected {expected}, received {result.Succeeded}.");
            }
        }
    }

    private static string BuildSampleRoutePath(RouteEndpoint endpoint)
    {
        var rawPath = endpoint.RoutePattern.RawText!;
        return Regex.Replace(rawPath, @"\{[^{}]+\}", match =>
        {
            var parameterText = match.Value[1..^1].TrimStart('*');
            var parameterName = parameterText.Split(':', '?')[0];
            var parameter = endpoint.RoutePattern.Parameters.First(candidate => candidate.Name == parameterName);
            var policies = string.Join(":", parameter.ParameterPolicies.Select(policy => policy.Content));
            var sample = policies.Contains("guid", StringComparison.OrdinalIgnoreCase) ? Guid.NewGuid().ToString("D") :
                policies.Contains("int", StringComparison.OrdinalIgnoreCase) || policies.Contains("long", StringComparison.OrdinalIgnoreCase) ? "1" :
                policies.Contains("alpha", StringComparison.OrdinalIgnoreCase) ? "x" : "acceptance";
            return Uri.EscapeDataString(sample);
        }, RegexOptions.CultureInvariant);
    }

    [Fact]
    public async Task Platform_overview_distinguishes_customer_trials_from_demo_subscriptions()
    {
        var plan = new SubscriptionPlan
        {
            Code = $"trial-{_companyId:N}", Name = "Subscription overview acceptance", DeploymentKind = "cloud",
            IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.TenantSubscriptions.AddRange(
                new TenantSubscription
                {
                    CompanyId = _companyId, PlanId = plan.Id, Status = "Trial", IsDemo = false,
                    StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), EndsAt = DateTimeOffset.UtcNow.AddDays(14)
                },
                new TenantSubscription
                {
                    CompanyId = _otherCompanyId, PlanId = plan.Id, Status = "Trial", IsDemo = true,
                    StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), EndsAt = DateTimeOffset.UtcNow.AddDays(14)
                });
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using var response = await SendAsPlatformAdminAsync("/api/platform/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var subscriptions = payload.RootElement.GetProperty("subscriptions");
        Assert.True(subscriptions.GetProperty("customerTrials").GetInt32() >= 1);
        Assert.True(subscriptions.GetProperty("demos").GetInt32() >= 1);
    }

    [Fact]
    public async Task Platform_checkout_orders_are_searchable_paginated_and_hide_provider_secrets()
    {
        var plan = new SubscriptionPlan
        {
            Code = $"orders-{_companyId:N}", Name = "Checkout orders acceptance", DeploymentKind = "cloud",
            IsActive = true, IsPublic = true
        };
        var marker = $"ORDER-ACCEPTANCE-{Guid.NewGuid():N}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.CompanyCheckoutOrders.AddRange(
                new CompanyCheckoutOrder
                {
                    Id = Guid.NewGuid(), CompanyName = "Order Search Company", Slug = $"order-search-{_companyId:N}",
                    ContactEmail = "contact@example.invalid", OwnerName = "Order Search Owner", OwnerEmail = "owner@example.invalid",
                    PlanId = plan.Id, TermDays = 365, Amount = 12000, Currency = "TRY", Status = "Provisioned",
                    PaymentReference = marker, CheckoutTokenHash = "provider-token-hash-must-not-leak",
                    CheckoutPageUrlProtected = "protected-provider-url-must-not-leak", CompanyId = _companyId,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                    PaidAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                },
                new CompanyCheckoutOrder
                {
                    Id = Guid.NewGuid(), CompanyName = "Unrelated Order Company", Slug = $"unrelated-{_companyId:N}",
                    ContactEmail = "other@example.invalid", OwnerName = "Other Owner", OwnerEmail = "other-owner@example.invalid",
                    PlanId = plan.Id, TermDays = 30, Amount = 1500, Currency = "TRY", Status = "PaymentFailed",
                    CheckoutTokenHash = "second-secret", CheckoutPageUrlProtected = "second-protected-url",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                });
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using (var denied = await SendAsAdminAsync("/api/platform/checkout-orders"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var response = await SendAsPlatformAdminAsync($"/api/platform/checkout-orders?q={Uri.EscapeDataString(marker)}&status=Provisioned&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);
        var root = payload.RootElement;
        Assert.Equal(1, root.GetProperty("total").GetInt32());
        Assert.Equal(1, root.GetProperty("items").GetArrayLength());
        Assert.Equal(marker, root.GetProperty("items")[0].GetProperty("paymentReference").GetString());
        Assert.Contains("Checkout orders acceptance", body, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-token-hash-must-not-leak", body, StringComparison.Ordinal);
        Assert.DoesNotContain("protected-provider-url-must-not-leak", body, StringComparison.Ordinal);
        Assert.DoesNotContain("second-secret", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Platform_member_directory_searches_cross_company_accounts_and_hides_identity_secrets()
    {
        var userId = Guid.NewGuid();
        var marker = $"MEMBER-DIRECTORY-{userId:N}";
        var earlierMembership = DateTimeOffset.UtcNow.AddDays(-90);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var plan = new SubscriptionPlan
            {
                Code = $"member-{userId:N}", Name = "Member Directory Acceptance Cloud",
                DeploymentKind = "cloud", IsActive = true, IsPublic = true
            };
            db.SubscriptionPlans.Add(plan);
            var memberSubscription = new TenantSubscription
            {
                CompanyId = _companyId, PlanId = plan.Id, Status = "Active", IsDemo = false,
                StartsAt = earlierMembership, EndsAt = DateTimeOffset.UtcNow.AddDays(90),
                PaymentReference = "member-directory-payment-reference"
            };
            db.TenantSubscriptions.Add(memberSubscription);
            db.CompanyCheckoutOrders.Add(new CompanyCheckoutOrder
            {
                Id = Guid.NewGuid(), CompanyName = "Directory Acceptance Company", Slug = $"member-{userId:N}",
                ContactEmail = $"{marker}@example.invalid", OwnerName = "Directory Acceptance Member",
                OwnerEmail = $"{marker}@example.invalid", PlanId = plan.Id, TermDays = 365, Amount = 4200,
                Currency = "TRY", Status = "Provisioned", PaymentReference = "member-directory-order-payment",
                CheckoutTokenHash = "checkout-token-hash-must-not-leak",
                CheckoutPageUrlProtected = "protected-checkout-url-must-not-leak",
                CompanyId = _companyId, CreatedAt = earlierMembership.AddDays(-4),
                PaidAt = earlierMembership.AddDays(-3), ExpiresAt = earlierMembership.AddDays(30)
            });
            db.PlatformDemoRequests.Add(new PlatformDemoRequest
            {
                CompanyName = "Directory Acceptance Company", ContactName = "Directory Acceptance Member",
                Email = $"{marker}@example.invalid", Source = "acceptance", Status = "DemoGranted",
                ConsentAt = earlierMembership.AddDays(-10), ConsentNoticeVersion = "acceptance-v1",
                CreatedAt = earlierMembership.AddDays(-10), UpdatedAt = earlierMembership.AddDays(-8),
                CompanyId = _companyId, DemoGrantedAt = earlierMembership.AddDays(-9)
            });
            db.Users.Add(new EnvanterUser
            {
                Id = userId, UserName = $"directory-{userId:N}", NormalizedUserName = $"DIRECTORY-{userId:N}",
                Email = $"{marker}@example.invalid", NormalizedEmail = $"{marker}@EXAMPLE.INVALID",
                EmailConfirmed = true, PhoneNumber = "+905551234567", PhoneNumberConfirmed = true,
                TwoFactorEnabled = true, LockoutEnabled = true, AccessFailedCount = 2,
                PasswordHash = "identity-password-hash-must-not-leak",
                SecurityStamp = "identity-security-stamp-must-not-leak",
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DisplayName = "Directory Acceptance Member"
            });
            db.CompanyMemberships.AddRange(
                new CompanyMembership { UserId = userId, CompanyId = _companyId, RoleKey = "owner", CreatedAt = earlierMembership },
                new CompanyMembership { UserId = userId, CompanyId = _otherCompanyId, RoleKey = "viewer", CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) });
            db.UserSessions.Add(new UserSession
            {
                UserId = userId, CompanyId = _otherCompanyId, PlatformAdmin = false,
                CreatedAt = DateTimeOffset.UtcNow, LastSeenAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
            });
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using (var denied = await SendAsAdminAsync($"/api/platform/members?q={Uri.EscapeDataString(marker)}"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var response = await SendAsPlatformAdminAsync($"/api/platform/members?q={Uri.EscapeDataString(marker)}&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);
        var root = payload.RootElement;
        Assert.Equal(1, root.GetProperty("total").GetInt32());
        var result = root.GetProperty("items")[0];
        Assert.Equal(2, result.GetProperty("membershipCount").GetInt32());
        Assert.True(result.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Equal(2, result.GetProperty("memberships").GetArrayLength());
        Assert.InRange((result.GetProperty("firstMembershipAt").GetDateTimeOffset() - earlierMembership).Duration(),
            TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
        Assert.DoesNotContain("identity-password-hash-must-not-leak", body, StringComparison.Ordinal);
        Assert.DoesNotContain("identity-security-stamp-must-not-leak", body, StringComparison.Ordinal);
        Assert.DoesNotContain("identity-authenticator-key-must-not-leak", body, StringComparison.Ordinal);

        using var profile = await SendAsPlatformAdminAsync($"/api/platform/members/{userId}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var profileBody = await profile.Content.ReadAsStringAsync();
        using var profileJson = JsonDocument.Parse(profileBody);
        Assert.Equal(2, profileJson.RootElement.GetProperty("membershipCount").GetInt32());
        Assert.Equal(2, profileJson.RootElement.GetProperty("memberships").GetArrayLength());
        Assert.True(profileJson.RootElement.GetProperty("account").GetProperty("phoneNumberConfirmed").GetBoolean());
        Assert.Contains("Member Directory Acceptance Cloud", profileBody, StringComparison.Ordinal);
        Assert.Contains("member-directory-payment-reference", profileBody, StringComparison.Ordinal);
        Assert.DoesNotContain("identity-password-hash-must-not-leak", profileBody, StringComparison.Ordinal);
        Assert.DoesNotContain("identity-security-stamp-must-not-leak", profileBody, StringComparison.Ordinal);
        Assert.DoesNotContain("identity-authenticator-key-must-not-leak", profileBody, StringComparison.Ordinal);

        using (var tenantDenied = await SendAsAdminAsync($"/api/platform/members/{userId}/timeline"))
            Assert.Equal(HttpStatusCode.Forbidden, tenantDenied.StatusCode);
        using var csrfResponse = await SendAsPlatformAdminRequestAsync(HttpMethod.Get, "/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var membershipPath = $"/api/platform/members/{userId}/memberships/{_otherCompanyId}";
        using (var tenantRoleChange = await SendAsUserAsync(HttpMethod.Patch, membershipPath + "/role", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { roleKey = "admin", note = "acceptance" })))
            Assert.Equal(HttpStatusCode.Forbidden, tenantRoleChange.StatusCode);
        using (var noCsrf = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, membershipPath + "/role",
                   System.Net.Http.Json.JsonContent.Create(new { roleKey = "admin", note = "acceptance" })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using (var protectedOwnerRole = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch,
                   $"/api/platform/members/{userId}/memberships/{_companyId}/role",
                   System.Net.Http.Json.JsonContent.Create(new { roleKey = "admin", note = "acceptance" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, protectedOwnerRole.StatusCode);
        using (var protectedOwnerRemoval = await SendAsPlatformAdminRequestAsync(HttpMethod.Delete,
                   $"/api/platform/members/{userId}/memberships/{_companyId}",
                   System.Net.Http.Json.JsonContent.Create(new { note = "acceptance" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, protectedOwnerRemoval.StatusCode);
        using (var changedRole = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, membershipPath + "/role",
                   System.Net.Http.Json.JsonContent.Create(new { roleKey = "admin", note = "Role reviewed by acceptance" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, changedRole.StatusCode);
            using var rolePayload = JsonDocument.Parse(await changedRole.Content.ReadAsStringAsync());
            Assert.True(rolePayload.RootElement.GetProperty("changed").GetBoolean());
            Assert.Equal("admin", rolePayload.RootElement.GetProperty("roleKey").GetString());
        }
        using (var sameRole = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, membershipPath + "/role",
                   System.Net.Http.Json.JsonContent.Create(new { roleKey = "admin", note = "Repeated safe action" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, sameRole.StatusCode);
            using var sameRolePayload = JsonDocument.Parse(await sameRole.Content.ReadAsStringAsync());
            Assert.False(sameRolePayload.RootElement.GetProperty("changed").GetBoolean());
        }
        using (var removedMembership = await SendAsPlatformAdminRequestAsync(HttpMethod.Delete, membershipPath,
                   System.Net.Http.Json.JsonContent.Create(new { note = "Membership no longer needed" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, removedMembership.StatusCode);
            using var removalPayload = JsonDocument.Parse(await removedMembership.Content.ReadAsStringAsync());
            Assert.Equal(1, removalPayload.RootElement.GetProperty("revokedSessions").GetInt32());
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.CompanyMemberships.AnyAsync(x => x.UserId == userId && x.CompanyId == _otherCompanyId));
            Assert.Equal("owner", await db.CompanyMemberships.Where(x => x.UserId == userId && x.CompanyId == _companyId)
                .Select(x => x.RoleKey).SingleAsync());
            Assert.NotNull(await db.UserSessions.Where(x => x.UserId == userId && x.CompanyId == _otherCompanyId)
                .Select(x => x.RevokedAt).SingleAsync());
            var roleAudit = await db.PlatformAuditEntries.SingleAsync(x => x.TargetType == "CompanyMembership" &&
                x.TargetId == userId && x.CompanyId == _otherCompanyId && x.Action == "MemberRoleChanged");
            Assert.Contains("Role reviewed by acceptance", roleAudit.Details, StringComparison.Ordinal);
            Assert.True(await db.PlatformAuditEntries.AnyAsync(x => x.TargetType == "CompanyMembership" &&
                x.TargetId == userId && x.CompanyId == _otherCompanyId && x.Action == "MemberRemoved"));
        }
        var timeline = new List<string>();
        var timelineBody = new List<string>();
        var timelinePages = 0;
        for (var page = 1; page <= 4; page++)
        {
            using var timelineResponse = await SendAsPlatformAdminAsync($"/api/platform/members/{userId}/timeline?page={page}&pageSize=4");
            Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);
            var pageBody = await timelineResponse.Content.ReadAsStringAsync();
            timelineBody.Add(pageBody);
            timelinePages++;
            using var timelinePayload = JsonDocument.Parse(pageBody);
            var timelineRoot = timelinePayload.RootElement;
            timeline.AddRange(timelineRoot.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("title").GetString()!));
            if (!timelineRoot.GetProperty("hasMore").GetBoolean()) break;
        }
        Assert.Contains("Demo talebi alındı", timeline);
        Assert.Contains("Satın alma kaydı oluşturuldu", timeline);
        Assert.Contains("Ödeme doğrulandı", timeline);
        Assert.Contains("Demo erişimi tanımlandı", timeline);
        Assert.Contains("Şirket üyeliği kaldırıldı", timeline);
        Assert.True(timelinePages > 1, "The member timeline should paginate when more than four events exist.");
        var serializedTimeline = string.Join('\n', timelineBody);
        Assert.DoesNotContain("checkout-token-hash-must-not-leak", serializedTimeline, StringComparison.Ordinal);
        Assert.DoesNotContain("protected-checkout-url-must-not-leak", serializedTimeline, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Platform_admin_can_invite_new_and_existing_members_with_company_scope_and_retryable_email()
    {
        var plan = new SubscriptionPlan
        {
            Code = $"member-invite-{_companyId:N}"[..40], Name = "Platform member invite acceptance",
            DeploymentKind = "cloud", IsActive = true, IsPublic = true
        };
        var email = $"platform-invite-{Guid.NewGuid():N}@example.invalid";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.TenantSubscriptions.AddRange(
                new TenantSubscription { CompanyId = _companyId, PlanId = plan.Id, Status = "Active", EndsAt = DateTimeOffset.UtcNow.AddDays(60) },
                new TenantSubscription { CompanyId = _otherCompanyId, PlanId = plan.Id, Status = "Active", EndsAt = DateTimeOffset.UtcNow.AddDays(60) });
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var invite = new { companyId = _companyId, fullName = "New Platform Invite", email, roleKey = "viewer" };

        using (var denied = await SendAsUserAsync(HttpMethod.Post, "/api/platform/members/invite", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(invite)))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using (var invalidRole = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/members/invite",
                   System.Net.Http.Json.JsonContent.Create(new { companyId = _companyId, fullName = "Invalid Role", email, roleKey = "owner" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);

        Guid invitedUserId;
        using (var created = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/members/invite",
                   System.Net.Http.Json.JsonContent.Create(invite), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
            Assert.False(payload.RootElement.GetProperty("resent").GetBoolean());
            invitedUserId = payload.RootElement.GetProperty("userId").GetGuid();
        }

        using (var resent = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/members/invite",
                   System.Net.Http.Json.JsonContent.Create(invite), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, resent.StatusCode);
            using var payload = JsonDocument.Parse(await resent.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
            Assert.True(payload.RootElement.GetProperty("resent").GetBoolean());
            Assert.Equal(invitedUserId, payload.RootElement.GetProperty("userId").GetGuid());
        }

        using (var existingAccount = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/members/invite",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       companyId = _otherCompanyId, fullName = "Security Acceptance User",
                       email = $"acceptance-{_userId:N}@example.invalid", roleKey = "asset_manager"
                   }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, existingAccount.StatusCode);
            using var payload = JsonDocument.Parse(await existingAccount.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
            Assert.Equal(_userId, payload.RootElement.GetProperty("userId").GetGuid());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var invited = await db.Users.SingleAsync(account => account.Id == invitedUserId);
            Assert.Equal(email, invited.Email);
            Assert.False(invited.EmailConfirmed);
            Assert.Equal(1, await db.CompanyMemberships.CountAsync(membership => membership.CompanyId == _companyId && membership.UserId == invitedUserId));
            Assert.Equal("viewer", await db.CompanyMemberships.Where(membership => membership.CompanyId == _companyId && membership.UserId == invitedUserId)
                .Select(membership => membership.RoleKey).SingleAsync());
            Assert.Equal("admin", await db.CompanyMemberships.Where(membership => membership.CompanyId == _companyId && membership.UserId == _userId)
                .Select(membership => membership.RoleKey).SingleAsync());
            Assert.Equal("asset_manager", await db.CompanyMemberships.Where(membership => membership.CompanyId == _otherCompanyId && membership.UserId == _userId)
                .Select(membership => membership.RoleKey).SingleAsync());
            Assert.Contains(await db.PlatformAuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _otherCompanyId).Select(entry => entry.Action).ToListAsync(),
                action => action == "MemberInvited");
        }

        Assert.Equal(3, _smtpCapture.Messages.Count);
        Assert.Contains(_smtpCapture.Messages, message => message.Contains($"To: {email}", StringComparison.OrdinalIgnoreCase));
        var existingAccountMessage = Assert.Single(_smtpCapture.Messages, message =>
            message.Contains($"To: acceptance-{_userId:N}@example.invalid", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Other security acceptance company", DecodeMimeBody(existingAccountMessage), StringComparison.Ordinal);
        Assert.DoesNotContain("token=", existingAccountMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_demo_requests_are_validated_deduplicated_and_visible_only_to_platform_admins()
    {
        var email = $"demo-request-{Guid.NewGuid():N}@example.invalid";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var missingConsent = new
        {
            companyName = "Demo Request Acceptance", contactName = "Synthetic Contact", email,
            phone = "+90 212 555 0000", jobTitle = "IT Manager", teamSize = "51-200",
            assetCount = "251-1000", message = "Synthetic demo request", consent = false, website = ""
        };
        using (var rejected = await client.PostAsJsonAsync("/api/demo-requests", missingConsent))
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var request = new
        {
            companyName = "Demo Request Acceptance", contactName = "Synthetic Contact", email = email.ToUpperInvariant(),
            phone = "+90 212 555 0000", jobTitle = "IT Manager", teamSize = "51-200",
            assetCount = "251-1000", message = "Synthetic demo request", consent = true, website = ""
        };
        using (var accepted = await client.PostAsJsonAsync("/api/demo-requests", request))
            Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        using (var duplicate = await client.PostAsJsonAsync("/api/demo-requests", request))
            Assert.Equal(HttpStatusCode.Accepted, duplicate.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lead = await db.PlatformDemoRequests.SingleAsync(x => x.Email == email);
            Assert.Equal("Demo Request Acceptance", lead.CompanyName);
            Assert.Equal("Synthetic Contact", lead.ContactName);
            Assert.Equal("New", lead.Status);
            Assert.Equal("PublicWebsite", lead.Source);
            Assert.Equal("demo-request-v1", lead.ConsentNoticeVersion);
            Assert.Equal("Synthetic demo request", lead.Message);
        }

        using (var anonymous = await client.GetAsync("/api/platform/demo-requests"))
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using (var tenant = await SendAsUserAsync(HttpMethod.Get, "/api/platform/demo-requests", _userId, _companyId))
            Assert.Equal(HttpStatusCode.Forbidden, tenant.StatusCode);
        using (var platform = await SendAsPlatformAdminAsync($"/api/platform/demo-requests?q={Uri.EscapeDataString(email)}"))
        {
            Assert.Equal(HttpStatusCode.OK, platform.StatusCode);
            using var payload = JsonDocument.Parse(await platform.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(email, payload.RootElement.GetProperty("items")[0].GetProperty("email").GetString());
        }
    }

    [Fact]
    public async Task Platform_demo_request_stages_require_real_demo_or_paid_company_subscription_and_are_audited()
    {
        var email = $"demo-stage-{Guid.NewGuid():N}@example.invalid";
        var leadId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Code = $"demo-stage-{_companyId:N}"[..40], Name = "Demo lifecycle acceptance", DeploymentKind = "cloud", IsActive = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            db.SubscriptionPlans.Add(plan);
            db.PlatformDemoRequests.Add(new PlatformDemoRequest
            {
                Id = leadId, CompanyName = "Lifecycle Company", ContactName = "Synthetic Contact", Email = email,
                Source = "PublicWebsite", Status = "New", ConsentNoticeVersion = "demo-request-v1"
            });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var demoStage = new { status = "DemoGranted", assignedAdminUserId = (Guid?)_userId, followUpAt = (DateTimeOffset?)null, companyId = (Guid?)_companyId, note = "Synthetic demo granted" };

        using (var falseDemo = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(demoStage), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, falseDemo.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                CompanyId = _companyId, PlanId = plan.Id, Status = "Trial", IsDemo = true,
                StartsAt = DateTimeOffset.UtcNow.AddDays(-15), EndsAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }
        using (var expiredDemo = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(demoStage), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, expiredDemo.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await db.TenantSubscriptions.SingleAsync(x => x.CompanyId == _companyId);
            subscription.StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            subscription.EndsAt = DateTimeOffset.UtcNow.AddDays(14);
            await db.SaveChangesAsync();
        }
        using (var granted = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(demoStage), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
            using var payload = JsonDocument.Parse(await granted.Content.ReadAsStringAsync());
            Assert.Equal("DemoGranted", payload.RootElement.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("demoGrantedAt").GetString()));
        }

        var convertedStage = new { status = "Converted", assignedAdminUserId = (Guid?)_userId, followUpAt = (DateTimeOffset?)null, companyId = (Guid?)_companyId, note = "Synthetic conversion" };
        using (var prematureConversion = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(convertedStage), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, prematureConversion.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await db.TenantSubscriptions.SingleAsync(x => x.CompanyId == _companyId);
            subscription.IsDemo = false;
            await db.SaveChangesAsync();
        }
        using (var unpaidTrial = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(convertedStage), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, unpaidTrial.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await db.TenantSubscriptions.SingleAsync(x => x.CompanyId == _companyId);
            subscription.Status = "Active";
            await db.SaveChangesAsync();
        }
        using (var converted = await SendAsPlatformAdminRequestAsync(HttpMethod.Patch, $"/api/platform/demo-requests/{leadId}",
                   JsonContent.Create(convertedStage), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, converted.StatusCode);
            using var payload = JsonDocument.Parse(await converted.Content.ReadAsStringAsync());
            Assert.Equal("Converted", payload.RootElement.GetProperty("status").GetString());
        }
        using (var detail = await SendAsPlatformAdminAsync($"/api/platform/demo-requests/{leadId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal("Lifecycle Company", payload.RootElement.GetProperty("lead").GetProperty("companyName").GetString());
            Assert.True(payload.RootElement.GetProperty("history").GetArrayLength() >= 2);
            Assert.Contains("Synthetic conversion", await detail.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var actions = await db.PlatformAuditEntries.AsNoTracking().Where(x => x.TargetType == "PlatformDemoRequest" && x.TargetId == leadId)
                .Select(x => x.Action).ToListAsync();
            Assert.Contains("DemoRequestStatusChanged", actions);
        }
    }

    [Fact]
    public async Task Platform_admin_can_adjust_subscription_plan_and_dates_with_validation_and_audit()
    {
        var originalPlan = new SubscriptionPlan
        {
            Code = $"sub-old-{_companyId:N}", Name = "Original Cloud", DeploymentKind = "cloud", IsActive = true
        };
        var upgradedPlan = new SubscriptionPlan
        {
            Code = $"sub-new-{_companyId:N}", Name = "Upgraded Cloud", DeploymentKind = "cloud", IsActive = true
        };
        var subscriptionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.AddRange(originalPlan, upgradedPlan);
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                Id = subscriptionId, CompanyId = _companyId, PlanId = originalPlan.Id, Status = "Active", IsDemo = false,
                StartsAt = now.AddDays(-10), EndsAt = now.AddDays(20), PaymentReference = "preserved-payment-reference"
            });
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var invalidRangeRequest = new
        {
            planId = upgradedPlan.Id, startsAt = now.AddDays(25), endsAt = now.AddDays(20), note = "invalid range must be rejected"
        };
        using (var invalidRange = await SendAsPlatformAdminRequestAsync(HttpMethod.Put,
                   $"/api/platform/companies/{_companyId}/subscriptions/{subscriptionId}",
                   System.Net.Http.Json.JsonContent.Create(invalidRangeRequest), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);

        var editedStartsAt = now.AddDays(-2);
        var editedEndsAt = now.AddDays(90);
        var editRequest = new { planId = upgradedPlan.Id, startsAt = editedStartsAt, endsAt = editedEndsAt, note = "Customer success courtesy extension" };
        using (var denied = await SendAsUserAsync(HttpMethod.Put,
                   $"/api/platform/companies/{_companyId}/subscriptions/{subscriptionId}", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(editRequest)))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var saved = await SendAsPlatformAdminRequestAsync(HttpMethod.Put,
                   $"/api/platform/companies/{_companyId}/subscriptions/{subscriptionId}",
                   System.Net.Http.Json.JsonContent.Create(editRequest), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
            using var payload = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
            Assert.Equal(upgradedPlan.Id, payload.RootElement.GetProperty("planId").GetGuid());
            Assert.Equal(editedStartsAt.ToUniversalTime(), payload.RootElement.GetProperty("startsAt").GetDateTimeOffset());
            Assert.Equal(editedEndsAt.ToUniversalTime(), payload.RootElement.GetProperty("endsAt").GetDateTimeOffset());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var subscription = await db.TenantSubscriptions.SingleAsync(x => x.Id == subscriptionId);
            Assert.Equal("Active", subscription.Status);
            Assert.False(subscription.IsDemo);
            Assert.Equal("preserved-payment-reference", subscription.PaymentReference);
            var audit = await db.PlatformAuditEntries.SingleAsync(x => x.TargetId == subscriptionId && x.Action == "SubscriptionManuallyAdjusted");
            Assert.Contains(originalPlan.Id.ToString(), audit.Details, StringComparison.Ordinal);
            Assert.Contains(upgradedPlan.Id.ToString(), audit.Details, StringComparison.Ordinal);
            Assert.Contains("Customer success courtesy extension", audit.Details, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation()
    {
        var plan = new SubscriptionPlan
        {
            Code = $"sale-{_companyId:N}", Name = "Paid Cloud provisioning acceptance", DeploymentKind = "cloud",
            IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsPlatformAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var email = $"paid-owner-{Guid.NewGuid():N}@example.invalid";
        var slug = $"paid-{Guid.NewGuid():N}";
        var requestBody = new
        {
            companyName = "Paid Cloud Acceptance Company", slug, contactEmail = email,
            ownerName = "Paid Cloud Owner", ownerEmail = email, planId = plan.Id,
            termDays = 365, paymentReference = $"acceptance-{Guid.NewGuid():N}", paymentVerified = true
        };

        using (var created = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/companies",
                   System.Net.Http.Json.JsonContent.Create(requestBody), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Assert.Equal(slug, payload.RootElement.GetProperty("slug").GetString());
            Assert.Equal(365, (payload.RootElement.GetProperty("endsAt").GetDateTimeOffset() -
                payload.RootElement.GetProperty("startsAt").GetDateTimeOffset()).Days);
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
        }

        Guid companyId;
        Guid ownerId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(x => x.Slug == slug);
            companyId = company.Id;
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == companyId);
            ownerId = membership.UserId;
            Assert.Equal("owner", membership.RoleKey);
            var owner = await db.Users.SingleAsync(x => x.Id == ownerId);
            Assert.Equal(email, owner.Email);
            Assert.False(owner.EmailConfirmed);
            var subscription = await db.TenantSubscriptions.SingleAsync(x => x.CompanyId == companyId);
            Assert.Equal("Active", subscription.Status);
            Assert.False(subscription.IsDemo);
            Assert.Equal(requestBody.paymentReference, subscription.PaymentReference);
        }

        Assert.Contains(_smtpCapture.Messages, message => message.Contains(email, StringComparison.OrdinalIgnoreCase));
        using (var resent = await SendAsPlatformAdminRequestAsync(HttpMethod.Post,
                   $"/api/platform/companies/{companyId}/owner-invitation/resend", new StringContent("{}", Encoding.UTF8, "application/json"),
                   requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, resent.StatusCode);
            using var payload = JsonDocument.Parse(await resent.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
        }
        using (var duplicateSlug = await SendAsPlatformAdminRequestAsync(HttpMethod.Post, "/api/platform/companies",
                   System.Net.Http.Json.JsonContent.Create(requestBody), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicateSlug.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.Companies.CountAsync(x => x.Slug == slug));
            Assert.Equal(1, await db.CompanyMemberships.CountAsync(x => x.CompanyId == companyId && x.UserId == ownerId));
            Assert.Equal(1, await db.TenantSubscriptions.CountAsync(x => x.CompanyId == companyId));
        }
    }

    [Fact]
    public async Task Company_member_and_platform_admin_boundaries_are_enforced_over_http()
    {
        using (var companyMember = await SendAsRoleAsync(HttpMethod.Get, "/api/company/branding", "viewer"))
            Assert.Equal(HttpStatusCode.OK, companyMember.StatusCode);

        await SetCompanyRoleAsync("unsupported_role");
        using (var nonMemberRole = await SendAsRoleAsync(HttpMethod.Get, "/api/company/branding", "unsupported_role"))
            Assert.Equal(HttpStatusCode.Forbidden, nonMemberRole.StatusCode);
        await SetCompanyRoleAsync("admin");

        using (var tenantAdmin = await SendAsAdminAsync("/api/platform/overview"))
            Assert.Equal(HttpStatusCode.Forbidden, tenantAdmin.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        using var platformAdmin = await SendAsPlatformAdminAsync("/api/platform/overview");
        Assert.Equal(HttpStatusCode.OK, platformAdmin.StatusCode);
        using var payload = JsonDocument.Parse(await platformAdmin.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.GetProperty("companies").GetProperty("total").GetInt32() >= 2);
    }

    private static string DescribeEndpoint(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
        return $"{(methods is { Count: > 0 } ? string.Join(",", methods) : "*")} {endpoint.RoutePattern.RawText}";
    }

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set ENVANTERTR_SECURITY_TEST_CONNECTION to the disposable acceptance-test PostgreSQL database.");

        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EnvanterTR");
        _previousForwardedHeadersSetting = Environment.GetEnvironmentVariable("ForwardedHeaders__Enabled");
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", connectionString);
        Environment.SetEnvironmentVariable("ForwardedHeaders__Enabled", "true");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = connectionString,
                    ["ForwardedHeaders:Enabled"] = "true",
                    ["Storage:CompanyMediaPath"] = _mediaPath,
                    ["Email:Smtp:Host"] = "127.0.0.1",
                    ["Email:Smtp:Port"] = _smtpCapture.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["Email:Smtp:EnableSsl"] = "false",
                    ["Email:FromAddress"] = "no-reply@example.invalid",
                    ["Email:PublicBaseUrl"] = "https://portal.example.invalid",
                    ["Support:Email"] = "support@example.invalid",
                    ["Iyzipay:ApiKey"] = "",
                    ["Iyzipay:SecretKey"] = "",
                    ["Iyzipay:CallbackUrl"] = ""
                }));
            builder.ConfigureTestServices(services => services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestScheme;
                    options.DefaultChallengeScheme = TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, AcceptanceHeaderAuthenticationHandler>(TestScheme, _ => { }));
        });
        _client = _factory!.CreateClient();

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        db.Companies.AddRange(
            new Company { Id = _companyId, Name = "Security acceptance company", Slug = $"acceptance-{_companyId:N}", IsActive = true },
            new Company { Id = _otherCompanyId, Name = "Other security acceptance company", Slug = $"other-{_otherCompanyId:N}", IsActive = true });
        var ownCategory = new AssetCategory { CompanyId = _companyId, Name = "Own category" };
        var otherCategory = new AssetCategory { CompanyId = _otherCompanyId, Name = "Other category" };
        db.AssetCategories.AddRange(ownCategory, otherCategory);
        db.Assets.AddRange(
            new Asset { Id = _ownAssetId, CompanyId = _companyId, AssetTag = "OWN-1", Name = "Own asset", CategoryId = ownCategory.Id, IsRequestable = true },
            new Asset { Id = _otherCompanyAssetId, CompanyId = _otherCompanyId, AssetTag = "OTHER-1", Name = "Other tenant asset", CategoryId = otherCategory.Id, IsRequestable = true });
        db.AssetEvents.AddRange(
            new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, ActorUserId = _userId, EventType = "Audited", Details = "Fiziksel denetim tamamlandı · OWN-AUDIT-REPORT" },
            new AssetEvent { CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId, EventType = "Audited", Details = "OTHER-AUDIT-REPORT" });
        db.SoftwareLicenses.AddRange(
            new SoftwareLicense { Id = _ownLicenseId, CompanyId = _companyId, Name = $"OWN-SECURITY-LICENSE-{_userId:N}", Seats = 4, Currency = "USD", ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) },
            new SoftwareLicense { Id = _otherCompanyLicenseId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-LICENSE-{_userId:N}", Seats = 8, Currency = "USD", ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) });
        var ownAccessoryCategory = new AccessoryCategory { CompanyId = _companyId, Name = "Klavye" };
        var otherAccessoryCategory = new AccessoryCategory { CompanyId = _otherCompanyId, Name = "Gizli kategori" };
        db.AccessoryCategories.AddRange(ownAccessoryCategory, otherAccessoryCategory);
        db.Accessories.AddRange(
            new Accessory { Id = _ownAccessoryId, CompanyId = _companyId, Name = $"OWN-SECURITY-ACCESSORY-{_userId:N}", CategoryId = ownAccessoryCategory.Id, Category = "Klavye", Location = "İstanbul Ofis", TotalQuantity = 5 },
            new Accessory { Id = _otherCompanyAccessoryId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-ACCESSORY-{_userId:N}", CategoryId = otherAccessoryCategory.Id, Category = "Gizli kategori", Location = "Gizli konum", TotalQuantity = 7 });
        db.Consumables.AddRange(
            new Consumable { Id = _ownConsumableId, CompanyId = _companyId, Name = $"OWN-SECURITY-CONSUMABLE-{_userId:N}", TotalQuantity = 5 },
            new Consumable { Id = _otherCompanyConsumableId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-CONSUMABLE-{_userId:N}", TotalQuantity = 7 });
        db.InventoryComponents.AddRange(
            new InventoryComponent { Id = _ownComponentId, CompanyId = _companyId, Name = $"OWN-SECURITY-COMPONENT-{_userId:N}", TotalQuantity = 5 },
            new InventoryComponent { Id = _otherCompanyComponentId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-COMPONENT-{_userId:N}", TotalQuantity = 7 });
        db.Employees.AddRange(
            new Employee { Id = _ownEmployeeId, CompanyId = _companyId, EmployeeNumber = $"OWN-{_userId:N}", DisplayName = $"OWN-SECURITY-EMPLOYEE-{_userId:N}", Department = "Security QA", StartedOn = new DateOnly(2020, 4, 15) },
            new Employee { Id = _otherCompanyEmployeeId, CompanyId = _otherCompanyId, EmployeeNumber = $"OTHER-{_userId:N}", DisplayName = $"OTHER-SECURITY-EMPLOYEE-{_userId:N}" });
        db.AccessoryMovements.AddRange(
            new AccessoryMovement { CompanyId = _companyId, AccessoryId = _ownAccessoryId, MovementType = "Received", Quantity = 2 },
            new AccessoryMovement { CompanyId = _otherCompanyId, AccessoryId = _otherCompanyAccessoryId, MovementType = "Received", Quantity = 2 });
        db.AccessoryAssignments.AddRange(
            new AccessoryAssignment { CompanyId = _companyId, AccessoryId = _ownAccessoryId, TargetType = "Asset", AssetId = _ownAssetId, Quantity = 3, ReturnedQuantity = 1, Notes = "Own asset accessory" },
            new AccessoryAssignment { CompanyId = _otherCompanyId, AccessoryId = _otherCompanyAccessoryId, TargetType = "Asset", AssetId = _otherCompanyAssetId, Quantity = 4, Notes = "Other tenant accessory" });
        db.ConsumableMovements.AddRange(
            new ConsumableMovement { CompanyId = _companyId, ConsumableId = _ownConsumableId, MovementType = "Received", Quantity = 2 },
            new ConsumableMovement { CompanyId = _otherCompanyId, ConsumableId = _otherCompanyConsumableId, MovementType = "Received", Quantity = 2 });
        db.ComponentMovements.AddRange(
            new ComponentMovement { CompanyId = _companyId, ComponentId = _ownComponentId, MovementType = "Received", Quantity = 2 },
            new ComponentMovement { CompanyId = _otherCompanyId, ComponentId = _otherCompanyComponentId, MovementType = "Received", Quantity = 2 });
        db.ComponentAllocations.AddRange(
            new ComponentAllocation { CompanyId = _companyId, ComponentId = _ownComponentId, AssetId = _ownAssetId, Quantity = 5, RemovedQuantity = 2, Notes = "Own asset component" },
            new ComponentAllocation { CompanyId = _otherCompanyId, ComponentId = _otherCompanyComponentId, AssetId = _otherCompanyAssetId, Quantity = 6, Notes = "Other tenant component" });
        db.Users.Add(new EnvanterUser
        {
            Id = _userId,
            UserName = $"acceptance-{_userId:N}",
            NormalizedUserName = $"ACCEPTANCE-{_userId:N}".ToUpperInvariant(),
            Email = $"acceptance-{_userId:N}@example.invalid",
            NormalizedEmail = $"ACCEPTANCE-{_userId:N}@EXAMPLE.INVALID".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DisplayName = "Security Acceptance User"
        });
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _userId, RoleKey = "admin" });
        db.Users.Add(new EnvanterUser
        {
            Id = _sameCompanyOtherUserId,
            UserName = $"acceptance-other-{_sameCompanyOtherUserId:N}",
            NormalizedUserName = $"ACCEPTANCE-OTHER-{_sameCompanyOtherUserId:N}".ToUpperInvariant(),
            Email = $"acceptance-other-{_sameCompanyOtherUserId:N}@example.invalid",
            NormalizedEmail = $"ACCEPTANCE-OTHER-{_sameCompanyOtherUserId:N}@EXAMPLE.INVALID".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DisplayName = "Second Security Acceptance User"
        });
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _sameCompanyOtherUserId, RoleKey = "admin" });
        db.Users.Add(new EnvanterUser
        {
            Id = _viewerUserId,
            UserName = $"acceptance-viewer-{_viewerUserId:N}",
            NormalizedUserName = $"ACCEPTANCE-VIEWER-{_viewerUserId:N}".ToUpperInvariant(),
            Email = $"acceptance-viewer-{_viewerUserId:N}@example.invalid",
            NormalizedEmail = $"ACCEPTANCE-VIEWER-{_viewerUserId:N}@EXAMPLE.INVALID".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DisplayName = "Security Acceptance Viewer"
        });
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _viewerUserId, RoleKey = "viewer" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Cloud_company_invites_are_unlimited_even_for_legacy_capped_plans_while_local_caps_remain_enforced()
    {
        var plan = new SubscriptionPlan
        {
            Code = $"c-{_companyId:N}", Name = "Cloud invite regression", DeploymentKind = "cloud",
            MaxLoginUsers = 1, IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                CompanyId = _companyId, PlanId = plan.Id, Status = "Active",
                StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), EndsAt = DateTimeOffset.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var publicPlans = await _client!.GetAsync("/api/plans"))
        {
            Assert.Equal(HttpStatusCode.OK, publicPlans.StatusCode);
            using var plansPayload = JsonDocument.Parse(await publicPlans.Content.ReadAsStringAsync());
            var cloudPlan = plansPayload.RootElement.EnumerateArray()
                .Single(x => x.GetProperty("code").GetString() == plan.Code);
            Assert.Equal("cloud", cloudPlan.GetProperty("deploymentKind").GetString());
            Assert.Equal(JsonValueKind.Null, cloudPlan.GetProperty("maxLoginUsers").ValueKind);
        }

        var cloudInviteEmail = $"cloud-invite-{Guid.NewGuid():N}@example.invalid";
        using (var cloudInvite = await SendAsUserAsync(HttpMethod.Post, "/api/members/invite", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { fullName = "Cloud invite regression", email = cloudInviteEmail, roleKey = "viewer" }),
                   requestToken, antiforgeryCookie, role: "admin"))
            Assert.Equal(HttpStatusCode.Created, cloudInvite.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await (from membership in db.CompanyMemberships
                join invited in db.Users on membership.UserId equals invited.Id
                where membership.CompanyId == _companyId && invited.Email == cloudInviteEmail
                select membership).AnyAsync());
            plan = await db.SubscriptionPlans.SingleAsync(x => x.Id == plan.Id);
            plan.DeploymentKind = "local-windows";
            await db.SaveChangesAsync();
        }

        var localInviteEmail = $"local-invite-{Guid.NewGuid():N}@example.invalid";
        using (var localInvite = await SendAsUserAsync(HttpMethod.Post, "/api/members/invite", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { fullName = "Local invite regression", email = localInviteEmail, roleKey = "viewer" }),
                   requestToken, antiforgeryCookie, role: "admin"))
        {
            Assert.Equal(HttpStatusCode.Conflict, localInvite.StatusCode);
            var body = await localInvite.Content.ReadAsStringAsync();
            Assert.Contains("1 giriş hesabıyla sınırlı", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Existing_account_can_join_an_additional_company_with_its_own_role_and_access_notice()
    {
        const string password = "Acceptance-multi-company-Password-47!";
        var plan = new SubscriptionPlan
        {
            Code = $"m-{_companyId:N}", Name = "Multi-company invite", DeploymentKind = "cloud",
            MaxLoginUsers = 1, IsActive = true, IsPublic = true
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var users = services.GetRequiredService<UserManager<EnvanterUser>>();
            var existingUser = await users.FindByIdAsync(_userId.ToString());
            Assert.NotNull(existingUser);
            var passwordResult = await users.AddPasswordAsync(existingUser, password);
            Assert.True(passwordResult.Succeeded, string.Join("; ", passwordResult.Errors.Select(error => error.Description)));

            var db = services.GetRequiredService<AppDbContext>();
            db.SubscriptionPlans.Add(plan);
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                CompanyId = _otherCompanyId, PlanId = plan.Id, Status = "Active",
                StartsAt = DateTimeOffset.UtcNow.AddMinutes(-1), EndsAt = DateTimeOffset.UtcNow.AddDays(30)
            });
            db.CompanyMemberships.Add(new CompanyMembership
            {
                CompanyId = _otherCompanyId, UserId = _sameCompanyOtherUserId, RoleKey = "owner"
            });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _otherCompanyId, role: "owner");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var invite = await SendAsUserAsync(HttpMethod.Post, "/api/members/invite", _sameCompanyOtherUserId, _otherCompanyId,
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       fullName = "Security Acceptance User",
                       email = $"acceptance-{_userId:N}@example.invalid",
                       roleKey = "viewer"
                   }), requestToken, antiforgeryCookie, role: "owner"))
        {
            Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
            using var payload = JsonDocument.Parse(await invite.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("invitationSent").GetBoolean());
            Assert.Equal(_userId, payload.RootElement.GetProperty("userId").GetGuid());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sourceMembership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            var targetMembership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _otherCompanyId && x.UserId == _userId);
            Assert.Equal("admin", sourceMembership.RoleKey);
            Assert.Equal("viewer", targetMembership.RoleKey);
        }

        var accessMessage = Assert.Single(_smtpCapture.Messages);
        Assert.Contains($"To: acceptance-{_userId:N}@example.invalid", accessMessage, StringComparison.OrdinalIgnoreCase);
        var accessBody = DecodeMimeBody(accessMessage);
        Assert.Contains("Other security acceptance company", accessBody, StringComparison.Ordinal);
        Assert.Contains("Görüntüleyici", accessBody, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", accessMessage, StringComparison.OrdinalIgnoreCase);

        using var loginClient = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var loginCsrfResponse = await loginClient.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, loginCsrfResponse.StatusCode);
        using var loginCsrfPayload = JsonDocument.Parse(await loginCsrfResponse.Content.ReadAsStringAsync());
        var loginRequestToken = loginCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var loginAntiforgeryCookie = loginCsrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var companyChooser = await SendAnonymousLoginAsync(loginClient, $"acceptance-{_userId:N}@example.invalid", password, null,
                   loginRequestToken, loginAntiforgeryCookie))
        using (var choices = JsonDocument.Parse(await companyChooser.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.Conflict, companyChooser.StatusCode);
            var companies = choices.RootElement.GetProperty("companies").EnumerateArray().ToArray();
            Assert.Equal(2, companies.Length);
            Assert.Contains(companies, company => company.GetProperty("id").GetGuid() == _companyId);
            Assert.Contains(companies, company => company.GetProperty("id").GetGuid() == _otherCompanyId);
        }

        using (var selectedCompanyLogin = await SendAnonymousLoginAsync(loginClient, $"acceptance-{_userId:N}@example.invalid", password, null,
                   loginRequestToken, loginAntiforgeryCookie, companyId: _otherCompanyId))
        using (var selectedSession = JsonDocument.Parse(await selectedCompanyLogin.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, selectedCompanyLogin.StatusCode);
            Assert.Equal(_otherCompanyId, selectedSession.RootElement.GetProperty("companyId").GetGuid());
            Assert.Equal("viewer", selectedSession.RootElement.GetProperty("role").GetString());
        }
    }

    [Fact]
    public async Task Asset_status_display_labels_are_company_scoped_audited_and_manager_only()
    {
        var defaults = new[]
        {
            new { status = AssetStatuses.Available, label = "Dağıtıma hazır" },
            new { status = AssetStatuses.Deployed, label = "Zimmetli" },
            new { status = AssetStatuses.Pending, label = "Onay bekliyor" },
            new { status = AssetStatuses.Maintenance, label = "Serviste" },
            new { status = AssetStatuses.Unusable, label = "Kullanılamaz" },
            new { status = AssetStatuses.Archived, label = "Arşiv" }
        };

        using (var initial = await SendAsUserAsync(HttpMethod.Get, "/api/assets/status-labels", _userId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
            using var payload = JsonDocument.Parse(await initial.Content.ReadAsStringAsync());
            Assert.Equal(6, payload.RootElement.GetArrayLength());
            Assert.Equal("Kullanılabilir", payload.RootElement[0].GetProperty("label").GetString());
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var missingCsrf = await SendAsUserAsync(HttpMethod.Put, "/api/assets/status-labels", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { labels = defaults })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var changed = defaults.Select(item => item.status == AssetStatuses.Available
            ? new { item.status, label = "Dağıtıma hazır" }
            : item).ToArray();
        using (var save = await SendAsUserAsync(HttpMethod.Put, "/api/assets/status-labels", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { labels = changed }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            using var payload = JsonDocument.Parse(await save.Content.ReadAsStringAsync());
            Assert.Equal("Dağıtıma hazır", payload.RootElement[0].GetProperty("label").GetString());
        }

        using (var otherMember = await SendAsUserAsync(HttpMethod.Get, "/api/assets/status-labels", _sameCompanyOtherUserId, _companyId))
        {
            Assert.Equal(HttpStatusCode.OK, otherMember.StatusCode);
            using var payload = JsonDocument.Parse(await otherMember.Content.ReadAsStringAsync());
            Assert.Equal("Dağıtıma hazır", payload.RootElement[0].GetProperty("label").GetString());
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _sameCompanyOtherUserId, RoleKey = "admin" });
            await db.SaveChangesAsync();
        }
        using (var otherCompany = await SendAsUserAsync(HttpMethod.Get, "/api/assets/status-labels", _sameCompanyOtherUserId, _otherCompanyId))
        {
            Assert.Equal(HttpStatusCode.OK, otherCompany.StatusCode);
            using var payload = JsonDocument.Parse(await otherCompany.Content.ReadAsStringAsync());
            Assert.Equal("Kullanılabilir", payload.RootElement[0].GetProperty("label").GetString());
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x =>
                x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
            membership.RoleKey = "viewer";
            await db.SaveChangesAsync();
        }
        using (var viewerWrite = await SendAsUserAsync(HttpMethod.Put, "/api/assets/status-labels",
                   _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(new { labels = defaults }),
                   requestToken, antiforgeryCookie, role: "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, viewerWrite.StatusCode);

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await auditDb.AuditEntries.AsNoTracking().Where(x => x.CompanyId == _companyId).ToListAsync(),
            entry => entry.Action == "AssetStatusLabelsUpdated" && entry.TargetId == _companyId);
    }

    [Fact]
    public async Task Asset_table_model_and_manufacturer_sort_use_their_own_catalog_fields()
    {
        var token = _userId.ToString("N");
        Guid alphaAssetId;
        Guid zetaAssetId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.AssetCategories.Where(x => x.CompanyId == _companyId).Select(x => x.Id).FirstAsync();
            var alphaManufacturer = new AssetBrand { CompanyId = _companyId, Name = $"ZebraSort-{token}" };
            var zetaManufacturer = new AssetBrand { CompanyId = _companyId, Name = $"AcerSort-{token}" };
            var alphaModel = new AssetModel { CompanyId = _companyId, Name = $"AlphaModel-{token}", BrandId = alphaManufacturer.Id };
            var zetaModel = new AssetModel { CompanyId = _companyId, Name = $"ZetaModel-{token}", BrandId = zetaManufacturer.Id };
            alphaAssetId = Guid.NewGuid();
            zetaAssetId = Guid.NewGuid();
            db.AssetBrands.AddRange(alphaManufacturer, zetaManufacturer);
            db.AssetModels.AddRange(alphaModel, zetaModel);
            db.Assets.AddRange(
                new Asset { Id = alphaAssetId, CompanyId = _companyId, AssetTag = $"MODEL-SORT-{token}-A", Name = "Alpha model asset", CategoryId = categoryId, BrandId = alphaManufacturer.Id, ModelId = alphaModel.Id },
                new Asset { Id = zetaAssetId, CompanyId = _companyId, AssetTag = $"MODEL-SORT-{token}-Z", Name = "Zeta model asset", CategoryId = categoryId, BrandId = zetaManufacturer.Id, ModelId = zetaModel.Id });
            await db.SaveChangesAsync();
        }

        var query = $"q=MODEL-SORT-{token}&pageSize=20&sortDirection=asc";
        using (var modelResponse = await SendAsAdminAsync($"/api/assets?{query}&sortBy=model"))
        using (var payload = JsonDocument.Parse(await modelResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, modelResponse.StatusCode);
            var ids = payload.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid()).ToArray();
            Assert.Equal(new[] { alphaAssetId, zetaAssetId }, ids);
        }

        using (var manufacturerResponse = await SendAsAdminAsync($"/api/assets?{query}&sortBy=brand"))
        using (var payload = JsonDocument.Parse(await manufacturerResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, manufacturerResponse.StatusCode);
            var ids = payload.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid()).ToArray();
            Assert.Equal(new[] { zetaAssetId, alphaAssetId }, ids);
        }
    }

    [Fact]
    public async Task Saved_asset_searches_are_private_to_user_and_company_and_require_csrf_for_changes()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetCustomFieldDefinitions.Add(new AssetCustomFieldDefinition
            {
                CompanyId = _companyId, Key = "asset_code", Name = "Asset code", FieldType = "text"
            });
            await db.SaveChangesAsync();
        }
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var noCsrfResponse = await SendAsUserAsync(HttpMethod.Post, "/api/assets/saved-searches", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Laptop", filters = new { q = "laptop" } })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrfResponse.StatusCode);

        var filters = new
        {
            q = "laptop", status = "Available", warranty = "active", categoryId = (Guid?)null,
            brandId = (Guid?)null, modelId = (Guid?)null, employeeId = (Guid?)null,
            customFieldKey = "asset_code", customFieldValue = "TR-", customFieldMode = "contains",
            employeeName = "", location = "Merkez", sortBy = "model", sortDirection = "desc"
        };
        using var createResponse = await SendAsUserAsync(HttpMethod.Post, "/api/assets/saved-searches", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Laptop", filters }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var searchId = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Available", created.RootElement.GetProperty("filters").GetProperty("status").GetString());
        Assert.Equal("asset_code", created.RootElement.GetProperty("filters").GetProperty("customFieldKey").GetString());
        Assert.Equal("TR-", created.RootElement.GetProperty("filters").GetProperty("customFieldValue").GetString());
        Assert.Equal("contains", created.RootElement.GetProperty("filters").GetProperty("customFieldMode").GetString());
        Assert.Equal("model", created.RootElement.GetProperty("filters").GetProperty("sortBy").GetString());

        using (var invalidTenantField = await SendAsUserAsync(HttpMethod.Post, "/api/assets/saved-searches", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Invalid custom field", filters = new { customFieldKey = "other_code", customFieldMode = "empty" } }),
            requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidTenantField.StatusCode);

        using (var updateResponse = await SendAsUserAsync(HttpMethod.Post, "/api/assets/saved-searches", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "LAPTOP", filters = new { q = "notebook", sortBy = "tag", sortDirection = "asc" } }),
            requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            using var updated = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
            Assert.Equal(searchId, updated.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("notebook", updated.RootElement.GetProperty("filters").GetProperty("q").GetString());
        }

        using (var ownList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/saved-searches", _userId, _companyId))
        using (var ownPayload = JsonDocument.Parse(await ownList.Content.ReadAsStringAsync()))
            Assert.Single(ownPayload.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/saved-searches", _sameCompanyOtherUserId, _companyId))
        using (var otherUserPayload = JsonDocument.Parse(await otherUserList.Content.ReadAsStringAsync()))
            Assert.Empty(otherUserPayload.RootElement.GetProperty("items").EnumerateArray());

        using (var crossCompanyList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/saved-searches", _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, crossCompanyList.StatusCode);

        using var otherUserCsrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var otherUserCsrfPayload = JsonDocument.Parse(await otherUserCsrfResponse.Content.ReadAsStringAsync());
        var otherUserRequestToken = otherUserCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using (var otherUserDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/saved-searches/{searchId}",
            _sameCompanyOtherUserId, _companyId, requestToken: otherUserRequestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.True(otherUserDelete.StatusCode == HttpStatusCode.NotFound,
                $"A different company member must not delete this user's saved search. Received {(int)otherUserDelete.StatusCode}: {await otherUserDelete.Content.ReadAsStringAsync()}");

        using (var deleteResponse = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/saved-searches/{searchId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Saved_asset_import_mappings_are_validated_and_private_to_their_owner()
    {
        using var csrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _userId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var map = new Dictionary<string, string> { ["Inventory Code"] = "assetTag", ["Item Description"] = "name", ["Ignore me"] = "ignore" };

        using (var noCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = map })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        using var create = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = map }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var profileId = created.RootElement.GetProperty("id").GetGuid();

        var accessoryMap = new Dictionary<string, string>
        {
            ["Item Description"] = "name", ["Quantity On Hand"] = "totalQuantity", ["Do Not Import"] = "ignore"
        };
        using var accessoryCreate = await SendAsUserAsync(HttpMethod.Post, "/api/accessories/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = accessoryMap }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, accessoryCreate.StatusCode);
        using var accessoryCreated = JsonDocument.Parse(await accessoryCreate.Content.ReadAsStringAsync());
        var accessoryProfileId = accessoryCreated.RootElement.GetProperty("id").GetGuid();

        var accessoryCsv = new StringContent("Item Description,Quantity On Hand,Do Not Import\nAcceptance Widget,3,private note\n",
            System.Text.Encoding.UTF8, "text/csv");
        var accessoryHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(accessoryMap)));
        using (var accessoryPreview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/accessories/import/preview.csv", "admin",
            accessoryCsv, requestToken, antiforgeryCookie, new Dictionary<string, string> { ["X-Accessory-Import-Mapping"] = accessoryHeader }))
        {
            Assert.True(accessoryPreview.StatusCode == HttpStatusCode.OK,
                $"Accessory mapped preview failed: {(int)accessoryPreview.StatusCode} {await accessoryPreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await accessoryPreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        var consumableMap = new Dictionary<string, string>
        {
            ["Material Description"] = "name", ["Quantity On Hand"] = "totalQuantity", ["Do Not Import"] = "ignore"
        };
        using (var consumableNoCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/consumables/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = consumableMap })))
            Assert.Equal(HttpStatusCode.BadRequest, consumableNoCsrf.StatusCode);
        using var consumableCreate = await SendAsUserAsync(HttpMethod.Post, "/api/consumables/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = consumableMap }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, consumableCreate.StatusCode);
        using var consumableCreated = JsonDocument.Parse(await consumableCreate.Content.ReadAsStringAsync());
        var consumableProfileId = consumableCreated.RootElement.GetProperty("id").GetGuid();
        var consumableCsv = new StringContent("Material Description,Quantity On Hand,Do Not Import\nPrinter Paper,12,private note\n",
            System.Text.Encoding.UTF8, "text/csv");
        var consumableHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(consumableMap)));
        using (var consumablePreview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/consumables/import/preview.csv", "admin",
            consumableCsv, requestToken, antiforgeryCookie, new Dictionary<string, string> { ["X-Consumable-Import-Mapping"] = consumableHeader }))
        {
            Assert.True(consumablePreview.StatusCode == HttpStatusCode.OK,
                $"Consumable mapped preview failed: {(int)consumablePreview.StatusCode} {await consumablePreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await consumablePreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        var componentMap = new Dictionary<string, string>
        {
            ["Part Description"] = "name", ["Available Units"] = "totalQuantity", ["Ignore Extra"] = "ignore"
        };
        using (var componentNoCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/components/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = componentMap })))
            Assert.Equal(HttpStatusCode.BadRequest, componentNoCsrf.StatusCode);
        using var componentCreate = await SendAsUserAsync(HttpMethod.Post, "/api/components/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = componentMap }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, componentCreate.StatusCode);
        using var componentCreated = JsonDocument.Parse(await componentCreate.Content.ReadAsStringAsync());
        var componentProfileId = componentCreated.RootElement.GetProperty("id").GetGuid();
        var componentCsv = new StringContent("Part Description,Available Units,Ignore Extra\nSSD 512,4,private note\n",
            System.Text.Encoding.UTF8, "text/csv");
        var componentHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(componentMap)));
        using (var componentPreview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/components/import/preview.csv", "admin",
            componentCsv, requestToken, antiforgeryCookie, new Dictionary<string, string> { ["X-Component-Import-Mapping"] = componentHeader }))
        {
            Assert.True(componentPreview.StatusCode == HttpStatusCode.OK,
                $"Component mapped preview failed: {(int)componentPreview.StatusCode} {await componentPreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await componentPreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        var licenseMap = new Dictionary<string, string>
        {
            ["Product Title"] = "name", ["License Category"] = "licenseType", ["Named Users"] = "seats", ["Ignore Extra"] = "ignore"
        };
        using (var licenseNoCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/software-licenses/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = licenseMap })))
            Assert.Equal(HttpStatusCode.BadRequest, licenseNoCsrf.StatusCode);
        using var licenseCreate = await SendAsUserAsync(HttpMethod.Post, "/api/software-licenses/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Vendor A", mappings = licenseMap }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, licenseCreate.StatusCode);
        using var licenseCreated = JsonDocument.Parse(await licenseCreate.Content.ReadAsStringAsync());
        var licenseProfileId = licenseCreated.RootElement.GetProperty("id").GetGuid();
        var licenseCsv = new StringContent("Product Title,License Category,Named Users,Ignore Extra\nOffice Suite,Subscription,20,secret\n",
            System.Text.Encoding.UTF8, "text/csv");
        var licenseHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(licenseMap)));
        using (var licensePreview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/software-licenses/import/preview.csv", "admin",
            licenseCsv, requestToken, antiforgeryCookie, new Dictionary<string, string> { ["X-Software-License-Import-Mapping"] = licenseHeader }))
        {
            Assert.True(licensePreview.StatusCode == HttpStatusCode.OK,
                $"Software license mapped preview failed: {(int)licensePreview.StatusCode} {await licensePreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await licensePreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        var employeeMap = new Dictionary<string, string>
        {
            ["Staff Code"] = "employeeNumber", ["Full Name"] = "displayName", ["Business Unit"] = "department", ["Do Not Import"] = "ignore"
        };
        using (var employeeNoCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/employees/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "HR export", mappings = employeeMap })))
            Assert.Equal(HttpStatusCode.BadRequest, employeeNoCsrf.StatusCode);
        using var employeeCreate = await SendAsUserAsync(HttpMethod.Post, "/api/employees/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "HR export", mappings = employeeMap }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, employeeCreate.StatusCode);
        using var employeeCreated = JsonDocument.Parse(await employeeCreate.Content.ReadAsStringAsync());
        var employeeProfileId = employeeCreated.RootElement.GetProperty("id").GetGuid();

        var employeeCsv = new StringContent("Staff Code,Full Name,Business Unit,Do Not Import\nMAP-100,Test Employee,Operations,private note\n",
            System.Text.Encoding.UTF8, "text/csv");
        var employeeHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(employeeMap)));
        using (var employeePreview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/employees/import/preview.csv", "admin",
            employeeCsv, requestToken, antiforgeryCookie, new Dictionary<string, string> { ["X-Employee-Import-Mapping"] = employeeHeader }))
        {
            Assert.True(employeePreview.StatusCode == HttpStatusCode.OK,
                $"Employee mapped preview failed: {(int)employeePreview.StatusCode} {await employeePreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await employeePreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        var localizedEmployeeCsv = new StringContent(
            "Табельный номер,Имя и фамилия,Электронная почта,Отдел,Должность,Телефон,Местоположение,Дата начала работы,Руководитель,Статус,Дата создания,Табельный номер руководителя\n" +
            "MAP-101,Test Russian Import,,,,,,, ,Активен,2026-10-09T12:00:00Z,\n",
            System.Text.Encoding.UTF8, "text/csv");
        using (var localizedEmployeePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.csv", "admin",
            localizedEmployeeCsv, requestToken, antiforgeryCookie))
        {
            Assert.True(localizedEmployeePreview.StatusCode == HttpStatusCode.OK,
                $"Localized employee CSV preview failed: {(int)localizedEmployeePreview.StatusCode} {await localizedEmployeePreview.Content.ReadAsStringAsync()}");
            using var previewPayload = JsonDocument.Parse(await localizedEmployeePreview.Content.ReadAsStringAsync());
            Assert.True(previewPayload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, previewPayload.RootElement.GetProperty("rowCount").GetInt32());
        }

        using (var invalid = await SendAsUserAsync(HttpMethod.Post, "/api/assets/import-mappings", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Bad", mappings = new Dictionary<string, string> { ["Secret"] = "custom:otherTenantOnly" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using (var ownList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/import-mappings", _userId, _companyId))
        using (var own = JsonDocument.Parse(await ownList.Content.ReadAsStringAsync()))
            Assert.Equal(profileId, Assert.Single(own.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var ownAccessoryList = await SendAsUserAsync(HttpMethod.Get, "/api/accessories/import-mappings", _userId, _companyId))
        using (var ownAccessory = JsonDocument.Parse(await ownAccessoryList.Content.ReadAsStringAsync()))
            Assert.Equal(accessoryProfileId, Assert.Single(ownAccessory.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var ownEmployeeList = await SendAsUserAsync(HttpMethod.Get, "/api/employees/import-mappings", _userId, _companyId))
        using (var ownEmployee = JsonDocument.Parse(await ownEmployeeList.Content.ReadAsStringAsync()))
            Assert.Equal(employeeProfileId, Assert.Single(ownEmployee.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var ownConsumableList = await SendAsUserAsync(HttpMethod.Get, "/api/consumables/import-mappings", _userId, _companyId))
        using (var ownConsumable = JsonDocument.Parse(await ownConsumableList.Content.ReadAsStringAsync()))
            Assert.Equal(consumableProfileId, Assert.Single(ownConsumable.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var ownComponentList = await SendAsUserAsync(HttpMethod.Get, "/api/components/import-mappings", _userId, _companyId))
        using (var ownComponent = JsonDocument.Parse(await ownComponentList.Content.ReadAsStringAsync()))
            Assert.Equal(componentProfileId, Assert.Single(ownComponent.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var ownLicenseList = await SendAsUserAsync(HttpMethod.Get, "/api/software-licenses/import-mappings", _userId, _companyId))
        using (var ownLicense = JsonDocument.Parse(await ownLicenseList.Content.ReadAsStringAsync()))
            Assert.Equal(licenseProfileId, Assert.Single(ownLicense.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var otherUserList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var other = JsonDocument.Parse(await otherUserList.Content.ReadAsStringAsync()))
            Assert.Empty(other.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserAccessoryList = await SendAsUserAsync(HttpMethod.Get, "/api/accessories/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var otherAccessory = JsonDocument.Parse(await otherUserAccessoryList.Content.ReadAsStringAsync()))
            Assert.Empty(otherAccessory.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserEmployeeList = await SendAsUserAsync(HttpMethod.Get, "/api/employees/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var otherEmployee = JsonDocument.Parse(await otherUserEmployeeList.Content.ReadAsStringAsync()))
            Assert.Empty(otherEmployee.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserConsumableList = await SendAsUserAsync(HttpMethod.Get, "/api/consumables/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var otherConsumable = JsonDocument.Parse(await otherUserConsumableList.Content.ReadAsStringAsync()))
            Assert.Empty(otherConsumable.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserComponentList = await SendAsUserAsync(HttpMethod.Get, "/api/components/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var otherComponent = JsonDocument.Parse(await otherUserComponentList.Content.ReadAsStringAsync()))
            Assert.Empty(otherComponent.RootElement.GetProperty("items").EnumerateArray());

        using (var otherUserLicenseList = await SendAsUserAsync(HttpMethod.Get, "/api/software-licenses/import-mappings", _sameCompanyOtherUserId, _companyId))
        using (var otherLicense = JsonDocument.Parse(await otherUserLicenseList.Content.ReadAsStringAsync()))
            Assert.Empty(otherLicense.RootElement.GetProperty("items").EnumerateArray());

        using (var crossCompanyList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/import-mappings", _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, crossCompanyList.StatusCode);

        using var otherUserCsrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var otherUserCsrfPayload = JsonDocument.Parse(await otherUserCsrfResponse.Content.ReadAsStringAsync());
        var otherUserToken = otherUserCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using (var otherUserDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/import-mappings/{profileId}", _sameCompanyOtherUserId, _companyId,
            requestToken: otherUserToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, otherUserDelete.StatusCode);

        using (var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/import-mappings/{profileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using (var deleteAccessory = await SendAsUserAsync(HttpMethod.Delete, $"/api/accessories/import-mappings/{accessoryProfileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteAccessory.StatusCode);
        using (var deleteEmployee = await SendAsUserAsync(HttpMethod.Delete, $"/api/employees/import-mappings/{employeeProfileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteEmployee.StatusCode);
        using (var deleteConsumable = await SendAsUserAsync(HttpMethod.Delete, $"/api/consumables/import-mappings/{consumableProfileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteConsumable.StatusCode);
        using (var deleteComponent = await SendAsUserAsync(HttpMethod.Delete, $"/api/components/import-mappings/{componentProfileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteComponent.StatusCode);
        using (var deleteLicense = await SendAsUserAsync(HttpMethod.Delete, $"/api/software-licenses/import-mappings/{licenseProfileId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, deleteLicense.StatusCode);
    }

    [Fact]
    public async Task Approved_stock_imports_create_tenant_scoped_suppliers_only_after_preview()
    {
        using var csrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _userId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var token = Guid.NewGuid().ToString("N");
        static string NormalizeSupplier(string name) => name.Trim().Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
        var duplicatedVendor = $"Other Tenant Vendor {token}";
        var otherTenantVendor = duplicatedVendor;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupplierCatalogEntries.Add(new SupplierCatalogEntry
            {
                CompanyId = _otherCompanyId, Name = otherTenantVendor,
                NormalizedName = NormalizeSupplier(otherTenantVendor)
            });
            await db.SaveChangesAsync();
        }

        var cases = new[]
        {
            ("/api/accessories/import/preview.csv", "/api/accessories/import.csv",
                "Aksesuar,Kategori,Model,Ürün no,Tedarikçi,Konum,Toplam,Minimum,Birim maliyeti,Para birimi,Alış tarihi,İstek yapılabilir,Notlar\n" +
                $"Accessory {token},, ,ACC-{token},{duplicatedVendor},,3,0,,TRY,,,\n" +
                $"Accessory extra {token},,,ACCX-{token},{duplicatedVendor.ToUpperInvariant()},,1,0,,TRY,,,\n", duplicatedVendor, "accessory"),
            ("/api/consumables/import/preview.csv", "/api/consumables/import.csv",
                "Malzeme,Kategori,Model,Ürün no,Tedarikçi,Konum,Toplam,Minimum,Birim maliyeti,Para birimi,Alış tarihi,İstek yapılabilir,Notlar\n" +
                $"Consumable {token},,,CON-{token},Consumable Vendor {token},,5,0,,TRY,,false,\n", $"Consumable Vendor {token}", "consumable"),
            ("/api/components/import/preview.csv", "/api/components/import.csv",
                "Bileşen,Kategori,Model,Ürün no,Seri no,Tedarikçi,Konum,Toplam,Minimum,Birim maliyeti,Para birimi,Alış tarihi,Notlar\n" +
                $"Component {token},,,CMP-{token},,Component Vendor {token},,2,0,,TRY,,\n", $"Component Vendor {token}", "component"),
            ("/api/software-licenses/import/preview.csv", "/api/software-licenses/import.csv",
                "Lisans,Yayıncı,Ürün sürümü,Tür,Tedarikçi,Koltuk,Birim maliyeti,Para birimi,Alış tarihi,Bitiş tarihi,Otomatik yenileme,Notlar\n" +
                $"License {token},Publisher,1,Subscription,License Vendor {token},3,,TRY,,,false,\n", $"License Vendor {token}", "license")
        };

        foreach (var (previewPath, importPath, csv, supplierName, family) in cases)
        {
            var normalizedSupplier = NormalizeSupplier(supplierName);
            using (var preview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, previewPath, "admin",
                new StringContent(csv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
                new Dictionary<string, string>()))
            {
                Assert.True(preview.StatusCode == HttpStatusCode.OK,
                    $"{family} supplier preview failed: {(int)preview.StatusCode} {await preview.Content.ReadAsStringAsync()}");
                using var payload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
                Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
                Assert.Contains(supplierName, payload.RootElement.GetProperty("suppliersToCreate")
                    .EnumerateArray().Select(value => value.GetString()));
            }

            await using (var scope = _factory!.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.False(await db.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == _companyId && x.NormalizedName == normalizedSupplier));
            }

            using (var imported = await SendAsRoleWithHeadersAsync(HttpMethod.Post, importPath, "admin",
                new StringContent(csv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
                new Dictionary<string, string>()))
            {
                Assert.True(imported.StatusCode == HttpStatusCode.OK,
                    $"{family} supplier import failed: {(int)imported.StatusCode} {await imported.Content.ReadAsStringAsync()}");
                using var payload = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
                Assert.Equal(1, payload.RootElement.GetProperty("suppliersCreated").GetInt32());
            }

            await using (var scope = _factory!.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var supplier = await db.SupplierCatalogEntries.SingleAsync(x => x.CompanyId == _companyId &&
                    x.NormalizedName == normalizedSupplier);
                Assert.True(await db.AuditEntries.AnyAsync(x => x.CompanyId == _companyId && x.TargetType == "Supplier" &&
                    x.TargetId == supplier.Id && x.Action == "supplier.created.import"));
                if (family == "accessory")
                {
                    var linked = await db.Accessories.Where(x => x.CompanyId == _companyId && x.Name.Contains(token)).ToListAsync();
                    Assert.Equal(2, linked.Count);
                    Assert.All(linked, item => Assert.Equal(supplier.Id, item.SupplierId));
                }
                else if (family == "consumable")
                    Assert.Equal(supplier.Id, await db.Consumables.Where(x => x.CompanyId == _companyId && x.Name == $"Consumable {token}").Select(x => x.SupplierId).SingleAsync());
                else if (family == "component")
                    Assert.Equal(supplier.Id, await db.InventoryComponents.Where(x => x.CompanyId == _companyId && x.Name == $"Component {token}").Select(x => x.SupplierId).SingleAsync());
                else
                    Assert.Equal(supplier.Id, await db.SoftwareLicenses.Where(x => x.CompanyId == _companyId && x.Name == $"License {token}").Select(x => x.SupplierId).SingleAsync());
            }
        }

        await using var tenantScope = _factory!.Services.CreateAsyncScope();
        var tenantDb = tenantScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedOtherTenantVendor = NormalizeSupplier(otherTenantVendor);
        var importedTenantSupplier = await tenantDb.SupplierCatalogEntries.SingleAsync(x => x.CompanyId == _companyId &&
            x.NormalizedName == normalizedOtherTenantVendor);
        var otherTenantSupplier = await tenantDb.SupplierCatalogEntries.SingleAsync(x => x.CompanyId == _otherCompanyId &&
            x.NormalizedName == normalizedOtherTenantVendor);
        Assert.NotEqual(importedTenantSupplier.Id, otherTenantSupplier.Id);
    }

    [Fact]
    public async Task Custom_asset_reports_sync_across_sessions_but_remain_private_to_their_owner()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var definition = new { name = "Laptop warranties", columns = new[] { "tag", "name", "warrantyExpiresAt" },
            q = "laptop", status = "Available", warranty = "expiring", sortBy = "warranty", sortDirection = "asc" };

        using (var noCsrf = await SendAsUserAsync(HttpMethod.Post, "/api/assets/custom-reports", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(definition)))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        using var create = await SendAsUserAsync(HttpMethod.Post, "/api/assets/custom-reports", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(definition), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var reportId = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("expiring", created.RootElement.GetProperty("definition").GetProperty("warranty").GetString());

        using (var invalidColumns = await SendAsUserAsync(HttpMethod.Post, "/api/assets/custom-reports", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Invalid", columns = new[] { "custom:secret-from-another-company" } }),
            requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidColumns.StatusCode);

        using (var ownList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/custom-reports", _userId, _companyId))
        using (var payload = JsonDocument.Parse(await ownList.Content.ReadAsStringAsync()))
            Assert.Equal(reportId, Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        using (var otherUserList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/custom-reports", _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await otherUserList.Content.ReadAsStringAsync()))
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());

        using (var crossCompanyList = await SendAsUserAsync(HttpMethod.Get, "/api/assets/custom-reports", _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, crossCompanyList.StatusCode);

        var otherCsrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using (otherCsrfResponse)
        using (var otherCsrf = JsonDocument.Parse(await otherCsrfResponse.Content.ReadAsStringAsync()))
        {
            var otherToken = otherCsrf.RootElement.GetProperty("requestToken").GetString()!;
            var otherCookie = otherCsrfResponse.Headers.TryGetValues("Set-Cookie", out var otherSetCookies)
                ? otherSetCookies.Select(value => value.Split(';', 2)[0])
                    .FirstOrDefault(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase))
                : null;
            using var forbiddenUpdate = await SendAsUserAsync(HttpMethod.Put, $"/api/assets/custom-reports/{reportId}",
                _sameCompanyOtherUserId, _companyId, System.Net.Http.Json.JsonContent.Create(definition), otherToken, otherCookie ?? antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, forbiddenUpdate.StatusCode);
        }

        using var update = await SendAsUserAsync(HttpMethod.Put, $"/api/assets/custom-reports/{reportId}", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Warranty review", columns = new[] { "tag", "warrantyExpiresAt" },
                q = "device", status = "", warranty = "expired", sortBy = "tag", sortDirection = "desc" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/custom-reports/{reportId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Dashboard_preferences_are_private_validated_and_csrf_protected()
    {
        using (var defaultsResponse = await SendAsAdminAsync("/api/dashboard/preferences"))
        using (var defaults = JsonDocument.Parse(await defaultsResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, defaultsResponse.StatusCode);
            Assert.Equal(5, defaults.RootElement.GetProperty("visibleWidgets").GetArrayLength());
            Assert.Equal(5, defaults.RootElement.GetProperty("widgetOrder").GetArrayLength());
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var noCsrf = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = new[] { "stats", "activity" } })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        using (var invalid = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = new[] { "stats", "admin" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using (var empty = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = Array.Empty<string>() }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        using (var invalidOrder = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = new[] { "stats", "activity" }, widgetOrder = new[] { "stats", "assets", "assets", "upcoming", "status" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidOrder.StatusCode);

        using (var update = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = new[] { "stats", "activity" }, widgetOrder = new[] { "status", "upcoming", "activity", "assets", "stats" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using (var sameUser = await SendAsUserAsync(HttpMethod.Get, "/api/dashboard/preferences", _userId, _companyId))
        using (var payload = JsonDocument.Parse(await sameUser.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, sameUser.StatusCode);
            Assert.Equal(new[] { "stats", "activity" }, payload.RootElement.GetProperty("visibleWidgets")
                .EnumerateArray().Select(x => x.GetString()).ToArray());
            Assert.Equal(new[] { "status", "upcoming", "activity", "assets", "stats" }, payload.RootElement.GetProperty("widgetOrder")
                .EnumerateArray().Select(x => x.GetString()).ToArray());
        }

        using (var otherUser = await SendAsUserAsync(HttpMethod.Get, "/api/dashboard/preferences", _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await otherUser.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
            Assert.Equal(5, payload.RootElement.GetProperty("visibleWidgets").GetArrayLength());
            Assert.Equal(5, payload.RootElement.GetProperty("widgetOrder").GetArrayLength());
        }

        using (var otherCompany = await SendAsUserAsync(HttpMethod.Get, "/api/dashboard/preferences", _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, otherCompany.StatusCode);
    }

    [Fact]
    public async Task Dashboard_custom_chart_dimensions_return_translatable_results()
    {
        foreach (var dimension in new[] { "status", "category", "brand", "model", "location" })
        {
            using var response = await SendAsAdminAsync($"/api/dashboard/charts/{dimension}");
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(dimension, payload.RootElement.GetProperty("dimension").GetString());
            Assert.Equal(JsonValueKind.Array, payload.RootElement.GetProperty("items").ValueKind);
            foreach (var item in payload.RootElement.GetProperty("items").EnumerateArray())
            {
                Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("label").GetString()));
                Assert.True(item.GetProperty("count").GetInt32() > 0);
            }
        }
    }

    [Fact]
    public async Task Appearance_preferences_are_private_validated_csrf_protected_and_preserved_with_dashboard_updates()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var defaults = await SendAsUserAsync(HttpMethod.Get, "/api/appearance/preferences", _userId, _companyId))
        using (var payload = JsonDocument.Parse(await defaults.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, defaults.StatusCode);
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("theme").ValueKind);
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("language").ValueKind);
        }

        using (var noCsrf = await SendAsUserAsync(HttpMethod.Put, "/api/appearance/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { theme = "dark", language = "en" })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        using (var invalid = await SendAsUserAsync(HttpMethod.Put, "/api/appearance/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { theme = "amoled", language = "en" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using (var update = await SendAsUserAsync(HttpMethod.Put, "/api/appearance/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { theme = "dark", language = "uk" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using (var dashboardUpdate = await SendAsUserAsync(HttpMethod.Put, "/api/dashboard/preferences", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { visibleWidgets = new[] { "stats", "activity" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, dashboardUpdate.StatusCode);

        using (var sameUser = await SendAsUserAsync(HttpMethod.Get, "/api/appearance/preferences", _userId, _companyId))
        using (var payload = JsonDocument.Parse(await sameUser.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, sameUser.StatusCode);
            Assert.Equal("dark", payload.RootElement.GetProperty("theme").GetString());
            Assert.Equal("uk", payload.RootElement.GetProperty("language").GetString());
        }

        using (var otherUser = await SendAsUserAsync(HttpMethod.Get, "/api/appearance/preferences", _sameCompanyOtherUserId, _companyId))
        using (var payload = JsonDocument.Parse(await otherUser.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("theme").ValueKind);
        }

        using (var otherCompany = await SendAsUserAsync(HttpMethod.Get, "/api/appearance/preferences", _userId, _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, otherCompany.StatusCode);
    }

    [Fact]
    public async Task Public_web_shell_uses_EnvanterTR_branding_without_reference_product_name()
    {
        var response = await _client!.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("EnvanterTR", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Snipe-IT", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SnipeIT", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_rate_limit_partitions_by_forwarded_client_ip()
    {
        using var csrfResponse = await _client!.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(requestToken));

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var response = await SendLoginAsync("203.0.113.10", requestToken!);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var limitedResponse = await SendLoginAsync("203.0.113.10", requestToken!))
            Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);

        // A different client address must receive its own bucket even though
        // both requests pass through the same TestServer/reverse-proxy hop.
        using var otherClientResponse = await SendLoginAsync("203.0.113.11", requestToken!);
        Assert.Equal(HttpStatusCode.Unauthorized, otherClientResponse.StatusCode);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _smtpCapture.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", _previousConnectionString);
        Environment.SetEnvironmentVariable("ForwardedHeaders__Enabled", _previousForwardedHeadersSetting);
        if (Directory.Exists(_mediaPath)) Directory.Delete(_mediaPath, recursive: true);
    }

    [Fact]
    public async Task Role_downgrade_revocation_and_company_suspension_take_effect_on_the_next_request()
    {
        Assert.Equal(HttpStatusCode.OK, (await SendAsAdminAsync("/api/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await SendAsAdminAsync("/api/assets", _otherCompanyId)).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            membership.RoleKey = "viewer";
            await db.SaveChangesAsync();
        }

        // The request still presents an admin claim, as an old cookie would. The server
        // refreshes it from the database before authorization evaluates settings.manage.
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsAdminAsync("/api/members")).StatusCode);
        using (var assetsResponse = await SendAsAdminAsync("/api/assets?page=1&pageSize=20"))
        {
            Assert.Equal(HttpStatusCode.OK, assetsResponse.StatusCode);
            using var payload = JsonDocument.Parse(await assetsResponse.Content.ReadAsStringAsync());
            var assetIds = payload.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid()).ToArray();
            Assert.Single(assetIds);
            Assert.DoesNotContain(_otherCompanyAssetId, assetIds);
        }
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsAdminAsync($"/api/assets/{_otherCompanyAssetId}")).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            db.CompanyMemberships.Remove(membership);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsAdminAsync("/api/assets")).StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _userId, RoleKey = "admin" });
            var company = await db.Companies.SingleAsync(x => x.Id == _companyId);
            company.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsAdminAsync("/api/assets")).StatusCode);
    }

    [Fact]
    public async Task Byod_filter_and_export_are_company_scoped_and_mark_personal_devices()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Assets.Where(x => x.Id == _ownAssetId || x.Id == _otherCompanyAssetId)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsByod, true));
        }

        using (var response = await SendAsAdminAsync("/api/assets?byod=true"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var assets = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(_ownAssetId, Assert.Single(assets).GetProperty("id").GetGuid());
            Assert.True(assets[0].GetProperty("isByod").GetBoolean());
        }

        using (var response = await SendAsAdminAsync("/api/assets?byod=false"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
        }

        using (var response = await SendAsAdminAsync("/api/assets/export-all.csv?byod=true"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("BYOD", csv, StringComparison.Ordinal);
            Assert.Contains("Evet", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-1", csv, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task License_expiry_report_list_detail_and_csv_are_scoped_to_the_active_company()
    {
        using (var reportResponse = await SendAsAdminAsync("/api/reports/software-licenses?state=all"))
        {
            Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
            using var payload = JsonDocument.Parse(await reportResponse.Content.ReadAsStringAsync());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Single(items);
            Assert.Equal(_ownLicenseId, items[0].GetProperty("id").GetGuid());
        }

        using (var ownLicenseResponse = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}"))
            Assert.Equal(HttpStatusCode.OK, ownLicenseResponse.StatusCode);
        using (var otherLicenseResponse = await SendAsAdminAsync($"/api/software-licenses/{_otherCompanyLicenseId}"))
            Assert.Equal(HttpStatusCode.NotFound, otherLicenseResponse.StatusCode);

        using var csvResponse = await SendAsAdminAsync("/api/reports/software-licenses/export.csv?state=all");
        Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
        Assert.True(csvResponse.Headers.CacheControl?.NoStore == true);
        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains($"OWN-SECURITY-LICENSE-{_userId:N}", csv, StringComparison.Ordinal);
        Assert.False(csv.Contains($"OTHER-SECURITY-LICENSE-{_userId:N}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Warranty_report_filters_paginate_export_and_remain_company_scoped()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var through = today.AddDays(30);
        var upcomingId = Guid.NewGuid();
        var laterId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var asset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            asset.AssetTag = "WARRANTY-EXPIRED";
            asset.Name = "=WARRANTY-CSV-FORMULA";
            asset.WarrantyExpiresAt = today.AddDays(-1);
            asset.Location = "Warranty Lab";
            asset.AssignedEmployeeId = _ownEmployeeId;
            asset.Status = AssetStatuses.Deployed;
            var otherAsset = await db.Assets.SingleAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyAssetId);
            otherAsset.WarrantyExpiresAt = today.AddDays(-2);
            var categoryId = asset.CategoryId;
            db.Assets.AddRange(
                new Asset { Id = upcomingId, CompanyId = _companyId, AssetTag = "WARRANTY-UPCOMING-UNIQUE", Name = "Upcoming warranty device", CategoryId = categoryId, Status = AssetStatuses.Deployed, WarrantyExpiresAt = today.AddDays(5), Location = "Warranty Lab", AssignedEmployeeId = _ownEmployeeId },
                new Asset { Id = laterId, CompanyId = _companyId, AssetTag = "WARRANTY-LATER", Name = "Later warranty device", CategoryId = categoryId, WarrantyExpiresAt = through.AddDays(1) },
                new Asset { Id = missingId, CompanyId = _companyId, AssetTag = "WARRANTY-MISSING", Name = "Missing warranty device", CategoryId = categoryId },
                new Asset { Id = archivedId, CompanyId = _companyId, AssetTag = "WARRANTY-ARCHIVED", Name = "Archived warranty device", CategoryId = categoryId, Status = AssetStatuses.Archived, WarrantyExpiresAt = today.AddDays(-3) });
            for (var index = 0; index < 55; index++)
                db.Assets.Add(new Asset { CompanyId = _companyId, AssetTag = $"WARRANTY-PAGE-{index:D3}", Name = $"Warranty page device {index:D3}", CategoryId = categoryId, WarrantyExpiresAt = today.AddDays(1 + index % 20) });
            await db.SaveChangesAsync();
        }

        using (var upcomingResponse = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&state=upcoming&page=1&pageSize=50"))
        {
            Assert.Equal(HttpStatusCode.OK, upcomingResponse.StatusCode);
            using var payload = JsonDocument.Parse(await upcomingResponse.Content.ReadAsStringAsync());
            Assert.Equal(56, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(50, payload.RootElement.GetProperty("items").GetArrayLength());
            Assert.Equal(2, payload.RootElement.GetProperty("pageCount").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("expired").GetInt32());
            Assert.Equal(56, payload.RootElement.GetProperty("upcoming").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("withoutDate").GetInt32());
            Assert.DoesNotContain(payload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == archivedId);
        }
        using (var secondPage = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&state=upcoming&page=2&pageSize=50"))
        using (var payload = JsonDocument.Parse(await secondPage.Content.ReadAsStringAsync()))
            Assert.Equal(6, payload.RootElement.GetProperty("items").GetArrayLength());
        using (var search = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&state=upcoming&q=WARRANTY-UPCOMING-UNIQUE"))
        using (var payload = JsonDocument.Parse(await search.Content.ReadAsStringAsync()))
        {
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            var item = payload.RootElement.GetProperty("items")[0];
            Assert.Equal(upcomingId, item.GetProperty("id").GetGuid());
            Assert.Equal("Warranty Lab", item.GetProperty("location").GetString());
            Assert.Equal($"OWN-SECURITY-EMPLOYEE-{_userId:N}", item.GetProperty("employeeName").GetString());
        }
        foreach (var (state, expectedId) in new[] { ("expired", _ownAssetId), ("later", laterId), ("missing", missingId) })
        {
            using var response = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&state={state}");
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(payload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == expectedId);
        }
        using (var csvResponse = await SendAsAdminAsync($"/api/reports/warranties/export.csv?through={through:yyyy-MM-dd}&state=expired"))
        {
            Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
            Assert.True(csvResponse.Headers.CacheControl?.NoStore == true);
            Assert.Equal("1", csvResponse.Headers.GetValues("X-Total-Count").Single());
            var csv = await csvResponse.Content.ReadAsStringAsync();
            Assert.Contains("'=WARRANTY-CSV-FORMULA", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-1", csv, StringComparison.Ordinal);
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _sameCompanyOtherUserId, RoleKey = "admin" });
            await db.SaveChangesAsync();
        }
        using (var otherCompany = await SendAsUserAsync(HttpMethod.Get, $"/api/reports/warranties?through={through:yyyy-MM-dd}&state=expired", _sameCompanyOtherUserId, _otherCompanyId, role: "admin"))
        using (var payload = JsonDocument.Parse(await otherCompany.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, otherCompany.StatusCode);
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_otherCompanyAssetId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var viewer = await SendAsRoleAsync(HttpMethod.Get, $"/api/reports/warranties?through={through:yyyy-MM-dd}&state=all", "viewer"))
            Assert.Equal(HttpStatusCode.OK, viewer.StatusCode);
        using (var invalidPast = await SendAsAdminAsync($"/api/reports/warranties?through={today.AddDays(-1):yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidPast.StatusCode);
        using (var invalidHorizon = await SendAsAdminAsync($"/api/reports/warranties?through={today.AddDays(367):yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidHorizon.StatusCode);
        using (var invalidState = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&state=unknown"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidState.StatusCode);
        using (var longSearch = await SendAsAdminAsync($"/api/reports/warranties?through={through:yyyy-MM-dd}&q={new string('x', 121)}"))
            Assert.Equal(HttpStatusCode.BadRequest, longSearch.StatusCode);
    }

    [Fact]
    public async Task Inventory_and_operations_lists_apply_documented_pagination_defaults_when_query_is_omitted()
    {
        var endpoints = new (string Path, HttpStatusCode ExpectedStatus)[]
        {
            ("/api/assets", HttpStatusCode.OK),
            ("/api/accessories", HttpStatusCode.OK),
            ("/api/consumables", HttpStatusCode.OK),
            ("/api/components", HttpStatusCode.OK),
            ("/api/software-licenses", HttpStatusCode.OK),
            ("/api/software-license-assignments", HttpStatusCode.OK),
            ("/api/employees", HttpStatusCode.OK),
            ("/api/activity", HttpStatusCode.OK),
            ("/api/audit", HttpStatusCode.OK),
            ("/api/inventory/movements", HttpStatusCode.OK),
            ("/api/accessory-orders", HttpStatusCode.OK),
            ("/api/asset-orders", HttpStatusCode.OK),
            ("/api/stock-orders", HttpStatusCode.OK),
            ("/api/requests", HttpStatusCode.OK),
            ("/api/reports/warranties", HttpStatusCode.OK),
            ("/api/reports/pending-deliveries", HttpStatusCode.OK),
            ("/api/reports/depreciation", HttpStatusCode.OK),
            ("/api/assets/export.csv", HttpStatusCode.OK)
        };

        foreach (var (path, expectedStatus) in endpoints)
        {
            using var response = await SendAsAdminAsync(path);
            var detail = response.StatusCode == expectedStatus ? "" : await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expectedStatus,
                $"{path} should respond with {(int)expectedStatus} when paging and optional filters are omitted, but returned {(int)response.StatusCode}: {detail}");
        }

        using var activityResponse = await SendAsAdminAsync("/api/activity");
        using var activity = JsonDocument.Parse(await activityResponse.Content.ReadAsStringAsync());
        Assert.True(activity.RootElement.GetProperty("total").GetInt32() > 0,
            "The active company's seeded activity should appear in the dashboard history.");
        Assert.NotEmpty(activity.RootElement.GetProperty("entries").EnumerateArray());
    }

    [Fact]
    public async Task Custom_asset_report_exports_only_selected_active_company_columns()
    {
        const string customFieldKey = "acceptanceCode";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetCustomFieldDefinitions.Add(new AssetCustomFieldDefinition
            {
                CompanyId = _companyId, Key = customFieldKey, Name = "Acceptance code", FieldType = "text"
            });
            var asset = await db.Assets.SingleAsync(x => x.Id == _ownAssetId);
            asset.CustomFieldsJson = "{\"acceptanceCode\":\"RND-42\"}";
            await db.SaveChangesAsync();
        }

        using (var response = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets/export-all.csv?columns=tag,name,custom:{customFieldKey}&status=Available", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("1", response.Headers.GetValues("X-Total-Count").Single());
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"Etiket\";\"Ad\";\"Özel alan: acceptanceCode\"", csv, StringComparison.Ordinal);
            Assert.Contains("\"OWN-1\";\"Own asset\";\"RND-42\"", csv, StringComparison.Ordinal);
            Assert.False(csv.Contains("Marka", StringComparison.Ordinal));
            Assert.False(csv.Contains("OTHER-1", StringComparison.Ordinal));
        }

        using (var duplicateColumn = await SendAsRoleAsync(HttpMethod.Get,
                   "/api/assets/export-all.csv?columns=tag,tag", "viewer"))
            Assert.Equal(HttpStatusCode.BadRequest, duplicateColumn.StatusCode);

        using (var otherTenantColumn = await SendAsRoleAsync(HttpMethod.Get,
                   "/api/assets/export-all.csv?columns=custom:otherTenantOnly", "viewer"))
            Assert.Equal(HttpStatusCode.BadRequest, otherTenantColumn.StatusCode);

    }

    [Fact]
    public async Task Asset_custom_field_filter_is_tenant_scoped_and_applies_to_csv()
    {
        const string fieldKey = "asset_code";
        var containsAssetId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.AssetCategories.Where(x => x.CompanyId == _companyId).Select(x => x.Id).SingleAsync();
            db.AssetCustomFieldDefinitions.AddRange(
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = fieldKey, Name = "Asset code", FieldType = "text" },
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = "asset_number", Name = "Asset number", FieldType = "number" },
                new AssetCustomFieldDefinition { CompanyId = _otherCompanyId, Key = "other_code", Name = "Other code", FieldType = "text" });
            var ownAsset = await db.Assets.SingleAsync(x => x.Id == _ownAssetId);
            ownAsset.CustomFieldsJson = "{\"asset_code\":\"TR-001\"}";
            db.Assets.Add(new Asset { Id = _ownSecondAssetId, CompanyId = _companyId, AssetTag = "OWN-2",
                Name = "Uncoded asset", CategoryId = categoryId, CustomFieldsJson = "{}" });
            db.Assets.Add(new Asset { Id = containsAssetId, CompanyId = _companyId, AssetTag = "OWN-3",
                Name = "Partially coded asset", CategoryId = categoryId, CustomFieldsJson = "{\"asset_code\":\"DEV-42 100%_Ready\"}" });
            await db.SaveChangesAsync();
        }

        using (var matching = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets?customFieldKey={fieldKey}&customFieldMode=equals&customFieldValue=TR-001", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, matching.StatusCode);
            using var payload = JsonDocument.Parse(await matching.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownAssetId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var empty = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets?customFieldKey={fieldKey}&customFieldMode=empty", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            using var payload = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownSecondAssetId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var contains = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets?customFieldKey={fieldKey}&customFieldMode=contains&customFieldValue={Uri.EscapeDataString("100%_")}", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, contains.StatusCode);
            using var payload = JsonDocument.Parse(await contains.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(containsAssetId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var nonTextContains = await SendAsRoleAsync(HttpMethod.Get,
                   "/api/assets?customFieldKey=asset_number&customFieldMode=contains&customFieldValue=42", "viewer"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, nonTextContains.StatusCode);
        }

        using (var otherTenantField = await SendAsRoleAsync(HttpMethod.Get,
                   "/api/assets?customFieldKey=other_code&customFieldMode=empty", "viewer"))
            Assert.Equal(HttpStatusCode.BadRequest, otherTenantField.StatusCode);

        using (var export = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets/export-all.csv?customFieldKey={fieldKey}&customFieldMode=equals&customFieldValue=TR-001", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            Assert.Equal("1", export.Headers.GetValues("X-Total-Count").Single());
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("OWN-1", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OWN-2", csv, StringComparison.Ordinal);
        }

        using (var containsExport = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets/export-all.csv?customFieldKey={fieldKey}&customFieldMode=contains&customFieldValue={Uri.EscapeDataString("100%_")}", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, containsExport.StatusCode);
            Assert.Equal("1", containsExport.Headers.GetValues("X-Total-Count").Single());
            var csv = await containsExport.Content.ReadAsStringAsync();
            Assert.Contains("OWN-3", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OWN-1", csv, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Sensitive_asset_fields_are_hidden_from_members_and_excluded_from_exports_and_filters()
    {
        const string fieldKey = "recovery_code";
        const string secretValue = "sensitive-acceptance-value-91a2";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var definition = new AssetCustomFieldDefinition
            {
                CompanyId = _companyId, Key = fieldKey, Name = "Recovery code", FieldType = "sensitive"
            };
            db.AssetCustomFieldDefinitions.Add(definition);
            var protection = scope.ServiceProvider.GetRequiredService<AssetCustomFieldProtection>();
            var protectedValues = protection.Protect(JsonSerializer.Serialize(new Dictionary<string, string>
                { [fieldKey] = secretValue }), null, _companyId, [definition]);
            Assert.Null(protectedValues.Error);
            var asset = await db.Assets.SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownAssetId);
            asset.CustomFieldsJson = protectedValues.Json;
            await db.SaveChangesAsync();
            Assert.DoesNotContain(secretValue, asset.CustomFieldsJson, StringComparison.Ordinal);
        }

        using (var adminDetail = await SendAsRoleAsync(HttpMethod.Get, $"/api/assets/{_ownAssetId}", "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, adminDetail.StatusCode);
            using var payload = JsonDocument.Parse(await adminDetail.Content.ReadAsStringAsync());
            Assert.Equal(secretValue, payload.RootElement.GetProperty("asset").GetProperty("customFields")
                .GetProperty(fieldKey).GetString());
        }
        using (var viewerDetail = await SendAsUserAsync(HttpMethod.Get, $"/api/assets/{_ownAssetId}", _viewerUserId, _companyId, role: "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, viewerDetail.StatusCode);
            using var payload = JsonDocument.Parse(await viewerDetail.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("asset").GetProperty("customFields").TryGetProperty(fieldKey, out _));
            Assert.DoesNotContain(secretValue, payload.RootElement.GetRawText(), StringComparison.Ordinal);
        }
        using (var export = await SendAsUserAsync(HttpMethod.Get,
                   $"/api/assets/export-all.csv?columns=tag,custom:{fieldKey}", _viewerUserId, _companyId, role: "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains($"\"Özel alan: {fieldKey}\"", csv, StringComparison.Ordinal);
            Assert.DoesNotContain(secretValue, csv, StringComparison.Ordinal);
        }
        using (var filter = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/assets?customFieldKey={fieldKey}&customFieldMode=equals&customFieldValue={Uri.EscapeDataString(secretValue)}", "admin"))
            Assert.Equal(HttpStatusCode.BadRequest, filter.StatusCode);
        using (var report = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/custom-reports", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = "Sensitive report", columns = new[] { "tag", $"custom:{fieldKey}" } })))
            Assert.Equal(HttpStatusCode.BadRequest, report.StatusCode);
    }

    [Fact]
    public async Task Label_stock_templates_are_company_scoped_validated_and_preserve_legacy_settings()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.SingleAsync(x => x.Id == _companyId);
            company.LabelPrintSettingsJson = JsonSerializer.Serialize(new
            {
                Barcode1d = "code39", Barcode2d = "none", WidthMm = 70m, HeightMm = 35m,
                OffsetXmm = 2m, OffsetYmm = -2m, PaddingMm = 3m, CodeSizeMm = 18m,
                QrTarget = "tag", ShowName = true, ShowSerial = false, ShowModel = true,
                ShowLocation = false, ShowStatus = false
            });
            await db.SaveChangesAsync();
        }
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var settings = new
        {
            barcode1d = "code128", barcode2d = "qrcode", widthMm = 60, heightMm = 40,
            offsetXmm = 1, offsetYmm = -1, paddingMm = 3, codeSizeMm = 20,
            qrTarget = "tag", showName = true, showSerial = true, showModel = false,
            showLocation = false, showStatus = false
        };
        var templateId = Guid.NewGuid();
        var payload = System.Net.Http.Json.JsonContent.Create(new
        {
            templates = new[] { new { id = templateId, name = "Raf etiketi", manufacturer = "Zebra", printerModel = "ZD421", productCode = "Z-60X40", settings } }
        });
        using (var save = await SendAsRoleAsync(HttpMethod.Put, "/api/company/label-templates", "admin", payload,
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        using (var read = await SendAsRoleAsync(HttpMethod.Get, "/api/company/label-templates", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var result = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            Assert.Equal(templateId, result.RootElement[0].GetProperty("id").GetGuid());
            Assert.Equal("Z-60X40", result.RootElement[0].GetProperty("productCode").GetString());
            Assert.Equal(60, result.RootElement[0].GetProperty("settings").GetProperty("widthMm").GetInt32());
        }

        using (var legacySettings = await SendAsRoleAsync(HttpMethod.Get, "/api/company/label-settings", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, legacySettings.StatusCode);
            using var result = JsonDocument.Parse(await legacySettings.Content.ReadAsStringAsync());
            Assert.Equal("code39", result.RootElement.GetProperty("settings").GetProperty("barcode1d").GetString());
            Assert.Equal(70, result.RootElement.GetProperty("settings").GetProperty("widthMm").GetInt32());
        }

        using var duplicatePayload = System.Net.Http.Json.JsonContent.Create(new
        {
            templates = new[]
            {
                new { id = templateId, name = "Aynı", manufacturer = "Zebra", printerModel = "ZD421", productCode = "Z-60X40", settings },
                new { id = templateId, name = "Tekrar", manufacturer = "Zebra", printerModel = "ZD421", productCode = "Z-60X40", settings }
            }
        });
        using var duplicate = await SendAsRoleAsync(HttpMethod.Put, "/api/company/label-templates", "admin", duplicatePayload,
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Request_catalog_includes_only_requestable_available_assets_from_the_active_company()
    {
        using var response = await SendAsAdminAsync("/api/requests/catalog");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var assetItems = items.Where(item => item.GetProperty("type").GetString() == "Asset").ToArray();

        var assetItem = Assert.Single(assetItems);
        Assert.Equal(_ownAssetId, assetItem.GetProperty("id").GetGuid());
        Assert.Equal("OWN-1", assetItem.GetProperty("modelNumber").GetString());
        Assert.Equal(1, assetItem.GetProperty("available").GetInt32());
        Assert.DoesNotContain(items, item => item.GetProperty("name").GetString() == "Other tenant asset");
    }

    [Fact]
    public async Task Accessory_kits_support_company_scoped_crud_safe_export_and_archive_restore()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var kitName = $"=OWN-KIT-{_userId:N}";
        var kitPath = "/api/accessory-kits";
        var item = new[] { new { itemType = "Accessory", itemId = _ownAccessoryId, quantity = 2 } };

        using (var foreignItem = await SendAsRoleAsync(HttpMethod.Post, kitPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = kitName, description = "formula export", items = new[] { new { itemType = "Accessory", itemId = _otherCompanyAccessoryId, quantity = 2 } } }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, foreignItem.StatusCode);

        Guid kitId;
        using (var create = await SendAsRoleAsync(HttpMethod.Post, kitPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = kitName, description = "formula export", items = item }),
                   requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            using var payload = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            kitId = payload.RootElement.GetProperty("id").GetGuid();
        }

        using (var duplicate = await SendAsRoleAsync(HttpMethod.Post, kitPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = kitName.ToLowerInvariant(), description = (string?)null, items = item }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using (var update = await SendAsRoleAsync(HttpMethod.Put, $"{kitPath}/{kitId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { name = kitName, description = "updated kit", items = new[] { new { itemType = "Accessory", itemId = _ownAccessoryId, quantity = 3 } } }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        await SetCompanyRoleAsync("viewer");
        try
        {
            using var viewerUpdate = await SendAsRoleAsync(HttpMethod.Put, $"{kitPath}/{kitId}", "viewer",
                System.Net.Http.Json.JsonContent.Create(new { name = kitName, description = (string?)null, items = item }),
                requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.Forbidden, viewerUpdate.StatusCode);
        }
        finally { await SetCompanyRoleAsync("admin"); }

        using (var list = await SendAsAdminAsync($"{kitPath}?q={Uri.EscapeDataString(_userId.ToString("N"))}"))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            var kit = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(kitId, kit.GetProperty("id").GetGuid());
            Assert.Equal(1, kit.GetProperty("availableKits").GetInt32()); // 3 available accessories / 3 per kit.
            Assert.Equal(3, kit.GetProperty("totalUnits").GetInt32());
        }

        using (var csv = await SendAsAdminAsync($"{kitPath}/export.csv?q={Uri.EscapeDataString(_userId.ToString("N"))}"))
        {
            Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
            var text = await csv.Content.ReadAsStringAsync();
            Assert.Contains("\"'=OWN-KIT-", text, StringComparison.Ordinal);
            Assert.Contains("updated kit", text, StringComparison.Ordinal);
        }

        using (var foreignList = await SendAsAdminAsync($"{kitPath}?q={Uri.EscapeDataString(_userId.ToString("N"))}", _otherCompanyId))
            Assert.Equal(HttpStatusCode.Unauthorized, foreignList.StatusCode);

        using (var archive = await SendAsRoleAsync(HttpMethod.Delete, $"{kitPath}/{kitId}", "admin", requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        using (var archivedList = await SendAsAdminAsync($"{kitPath}?q={Uri.EscapeDataString(_userId.ToString("N"))}"))
        {
            using var payload = JsonDocument.Parse(await archivedList.Content.ReadAsStringAsync());
            Assert.False(Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("isActive").GetBoolean());
        }
        using (var history = await SendAsAdminAsync($"{kitPath}/{kitId}/history"))
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using (var restore = await SendAsRoleAsync(HttpMethod.Post, $"{kitPath}/{kitId}/restore", "admin",
                   new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    [Fact]
    public async Task Accessory_kit_requests_are_company_scoped_and_approve_all_stock_lines_atomically()
    {
        var kitId = Guid.NewGuid();
        var otherKitId = Guid.NewGuid();
        var kitName = $"OWN-REQUEST-KIT-{_userId:N}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessory = await db.Accessories.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAccessoryId);
            accessory.IsRequestable = true;
            db.AccessoryKits.AddRange(
                new AccessoryKit { Id = kitId, CompanyId = _companyId, Name = kitName },
                new AccessoryKit { Id = otherKitId, CompanyId = _otherCompanyId, Name = "OTHER-REQUEST-KIT" });
            db.AccessoryKitLines.AddRange(
                new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId, ItemType = "Accessory", AccessoryId = _ownAccessoryId, Quantity = 1 },
                new AccessoryKitLine { CompanyId = _otherCompanyId, AccessoryKitId = otherKitId, ItemType = "Accessory", AccessoryId = _otherCompanyAccessoryId, Quantity = 1 });
            await db.SaveChangesAsync();
        }

        using var catalogResponse = await SendAsAdminAsync("/api/requests/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        using var catalog = JsonDocument.Parse(await catalogResponse.Content.ReadAsStringAsync());
        var kit = Assert.Single(catalog.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "AccessoryKit" && item.GetProperty("id").GetGuid() == kitId);
        Assert.Equal(3, kit.GetProperty("available").GetInt32());
        Assert.DoesNotContain(catalog.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == otherKitId);

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using (var foreignRequest = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "AccessoryKit", itemId = otherKitId, quantity = 1, note = "tenant boundary" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, foreignRequest.StatusCode);
        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "AccessoryKit", itemId = kitId, quantity = 2, note = "two complete kits" }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createPayload = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var requestId = createPayload.RootElement.GetProperty("id").GetGuid();
        using var decisionResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{requestId}/decision", "admin",
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = "kit approved" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assignment = Assert.Single(await db.AccessoryAssignments.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId && x.EmployeeId == _ownEmployeeId && x.Notes!.Contains($"[KIT:{kitId}]" )).ToListAsync());
            Assert.Equal(2, assignment.Quantity);
            Assert.Contains(await db.AccessoryMovements.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId && x.AssignmentId == assignment.Id).ToListAsync(),
                movement => movement.MovementType == "Checkout" && movement.Quantity == 2 && movement.ActorUserId == _userId);
            Assert.Equal("Approved", (await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == requestId)).Status);
        }

        using var searchResponse = await SendAsAdminAsync($"/api/requests?q={Uri.EscapeDataString(kitName)}");
        using var searchPayload = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        Assert.Contains(searchPayload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == requestId && item.GetProperty("item").GetString() == kitName);
        using var exportResponse = await SendAsAdminAsync($"/api/requests/export.csv?q={Uri.EscapeDataString(kitName)}");
        var csv = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains(kitName, csv);
        Assert.Contains("Hazır kit", csv);
    }

    [Fact]
    public async Task Mixed_kits_validate_tenant_items_and_issue_all_four_types_as_one_request()
    {
        var kitId = Guid.NewGuid();
        var kitName = $"OWN-MIXED-KIT-{_userId:N}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Accessories.Single(x => x.CompanyId == _companyId && x.Id == _ownAccessoryId).IsRequestable = true;
            db.Consumables.Single(x => x.CompanyId == _companyId && x.Id == _ownConsumableId).IsRequestable = true;
            db.AccessoryKits.Add(new AccessoryKit { Id = kitId, CompanyId = _companyId, Name = kitName });
            db.AccessoryKitLines.AddRange(
                new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId, ItemType = "Accessory", AccessoryId = _ownAccessoryId, Quantity = 1 },
                new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId, ItemType = "Consumable", ConsumableId = _ownConsumableId, Quantity = 2 },
                new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId, ItemType = "Asset", AssetId = _ownAssetId, Quantity = 1 },
                new AccessoryKitLine { CompanyId = _companyId, AccessoryKitId = kitId, ItemType = "Component", ComponentId = _ownComponentId, Quantity = 1 });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var foreignKit = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-kits", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       name = $"FOREIGN-MIXED-KIT-{_userId:N}", description = (string?)null,
                       items = new[] { new { itemType = "Component", itemId = _otherCompanyComponentId, quantity = 1 },
                           new { itemType = "Asset", itemId = _ownAssetId, quantity = 1 } }
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, foreignKit.StatusCode);

        using var catalogResponse = await SendAsAdminAsync("/api/requests/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        using var catalog = JsonDocument.Parse(await catalogResponse.Content.ReadAsStringAsync());
        var kit = Assert.Single(catalog.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "AccessoryKit" && item.GetProperty("id").GetGuid() == kitId);
        Assert.Equal(1, kit.GetProperty("available").GetInt32());

        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "AccessoryKit", itemId = kitId, quantity = 1, note = "mixed kit" }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createPayload = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var requestId = createPayload.RootElement.GetProperty("id").GetGuid();
        using var decisionResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{requestId}/decision", "admin",
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = "mixed kit approved" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessoryAssignment = Assert.Single(await db.AccessoryAssignments.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId && x.Notes!.Contains($"[KIT:{kitId}]" )).ToListAsync());
            Assert.Equal(1, accessoryAssignment.Quantity);
            Assert.Contains(await db.ConsumableMovements.Where(x => x.CompanyId == _companyId && x.ConsumableId == _ownConsumableId).ToListAsync(),
                x => x.MovementType == "Checkout" && x.Quantity == 2 && x.Notes!.Contains($"[KIT:{kitId}]"));
            var asset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            Assert.Equal(AssetStatuses.Deployed, asset.Status);
            Assert.Equal(_ownEmployeeId, asset.AssignedEmployeeId);
            Assert.Contains(await db.ComponentAllocations.Where(x => x.CompanyId == _companyId && x.ComponentId == _ownComponentId).ToListAsync(),
                x => x.AssetId == _ownAssetId && x.Quantity == 1 && x.Notes!.Contains($"[KIT:{kitId}]"));
            Assert.Equal("Approved", (await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == requestId)).Status);
        }

        using var historyResponse = await SendAsAdminAsync($"/api/accessory-kits/{kitId}/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var historyPayload = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(4, historyPayload.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(new[] { "Accessory", "Consumable", "Asset", "Component" }.OrderBy(x => x),
            historyPayload.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("itemType").GetString()!).OrderBy(x => x));

        using var returnResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{requestId}/return-kit", "admin",
            System.Net.Http.Json.JsonContent.Create(new { note = "kit checked back in" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);
        using (var duplicateReturn = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{requestId}/return-kit", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "duplicate" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicateReturn.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fulfillment = await db.InventoryRequestKitFulfillmentLines.Where(x => x.CompanyId == _companyId && x.InventoryRequestId == requestId).ToListAsync();
            Assert.Equal(4, fulfillment.Count);
            Assert.Equal(3, fulfillment.Count(x => x.ReturnedAt != null));
            Assert.Null(fulfillment.Single(x => x.ItemType == "Consumable").ReturnedAt);
            var request = await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == requestId);
            Assert.NotNull(request.KitReturnedAt);
            Assert.Equal("kit checked back in", request.KitReturnNote);
            var accessoryAssignment = await db.AccessoryAssignments.SingleAsync(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId && x.Notes!.Contains($"[KIT:{kitId}]"));
            Assert.Equal(accessoryAssignment.Quantity, accessoryAssignment.ReturnedQuantity);
            Assert.Equal(AssetStatuses.Available, (await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId)).Status);
            Assert.Contains(await db.ConsumableMovements.Where(x => x.CompanyId == _companyId && x.ConsumableId == _ownConsumableId).ToListAsync(),
                x => x.MovementType == "Checkout" && x.Quantity == 2 && x.Notes!.Contains($"[KIT:{kitId}]"));
        }
    }

    [Fact]
    public async Task Requesting_an_asset_rejects_invalid_scope_and_approval_assigns_it_atomically()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var wrongTenantResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "Asset", itemId = _otherCompanyAssetId, quantity = 1, note = "cross-tenant" }),
            requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, wrongTenantResponse.StatusCode);

        using (var multipleAssetResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 2, note = "invalid quantity" }),
            requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, multipleAssetResponse.StatusCode);

        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 1, note = "acceptance request" }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createPayload = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var requestId = createPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Pending", createPayload.RootElement.GetProperty("status").GetString());

        using var decisionResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{requestId}/decision", "admin",
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = "accepted" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
        Assert.Equal(AssetStatuses.Deployed, asset.Status);
        Assert.Equal(_ownEmployeeId, asset.AssignedEmployeeId);
        Assert.Contains(await db.AssetEvents.Where(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId).ToListAsync(),
            item => item.EventType == "Assigned" && item.EmployeeId == _ownEmployeeId);
        Assert.Equal("Approved", (await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == requestId)).Status);
    }

    [Fact]
    public async Task Inventory_requests_support_rejection_and_requester_only_cancellation()
    {
        using var adminCsrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var adminCsrfPayload = JsonDocument.Parse(await adminCsrfResponse.Content.ReadAsStringAsync());
        var adminRequestToken = adminCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = adminCsrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        async Task<Guid> CreatePendingRequestAsync(string note)
        {
            using var response = await SendAsRoleAsync(HttpMethod.Post, "/api/requests", "admin",
                System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 1, note }),
                adminRequestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pending", payload.RootElement.GetProperty("status").GetString());
            return payload.RootElement.GetProperty("id").GetGuid();
        }

        var rejectedId = await CreatePendingRequestAsync("filterable request for rejection");
        using (var rejected = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{rejectedId}/decision", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { approve = false, note = "Rejected after stock review" }), adminRequestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
            using var payload = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
            Assert.Equal("Rejected", payload.RootElement.GetProperty("status").GetString());
        }

        using (var filtered = await SendAsAdminAsync("/api/requests?status=Rejected&q=OWN-1"))
        using (var payload = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            var row = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(rejectedId, row.GetProperty("id").GetGuid());
            Assert.Equal("Rejected after stock review", row.GetProperty("reviewNote").GetString());
        }
        using (var export = await SendAsAdminAsync("/api/requests/export.csv?status=Rejected&q=OWN-1"))
        {
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("Rejected after stock review", csv);
            Assert.Contains("Durum", csv);
            Assert.Contains("filterable request for rejection", csv);
        }

        var cancellableId = await CreatePendingRequestAsync("requester cancellation flow");
        using var viewerCsrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _viewerUserId, _companyId, role: "viewer");
        using var viewerCsrfPayload = JsonDocument.Parse(await viewerCsrfResponse.Content.ReadAsStringAsync());
        var viewerRequestToken = viewerCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using (var otherRequesterCancel = await SendAsUserAsync(HttpMethod.Post, $"/api/requests/{cancellableId}/cancel",
                   _viewerUserId, _companyId, requestToken: viewerRequestToken, antiforgeryCookie: antiforgeryCookie, role: "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, otherRequesterCancel.StatusCode);

        using (var cancelled = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{cancellableId}/cancel", "admin", null,
                   adminRequestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        using (var repeatedCancel = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{cancellableId}/cancel", "admin", null,
                   adminRequestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, repeatedCancel.StatusCode);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rejectedRequest = await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == rejectedId);
        var cancelledRequest = await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == cancellableId);
        Assert.Equal("Rejected", rejectedRequest.Status);
        Assert.Equal("Rejected after stock review", rejectedRequest.ReviewNote);
        Assert.NotNull(rejectedRequest.ReviewedAt);
        Assert.Equal("Cancelled", cancelledRequest.Status);
        Assert.NotNull(cancelledRequest.ReviewedAt);
    }

    [Fact]
    public async Task Member_employee_link_is_tenant_scoped_unique_and_binds_self_service_requests()
    {
        await using (var fixtureScope = _factory!.Services.CreateAsyncScope())
        {
            var fixtureDb = fixtureScope.ServiceProvider.GetRequiredService<AppDbContext>();
            fixtureDb.Employees.Add(new Employee { Id = _ownSecondEmployeeId, CompanyId = _companyId,
                EmployeeNumber = $"OWN-SECOND-{_userId:N}", DisplayName = $"OWN-SECOND-SECURITY-EMPLOYEE-{_userId:N}" });
            await fixtureDb.SaveChangesAsync();
        }

        using var adminCsrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var adminCsrfPayload = JsonDocument.Parse(await adminCsrfResponse.Content.ReadAsStringAsync());
        var adminRequestToken = adminCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var adminAntiforgeryCookie = adminCsrfResponse.Headers.TryGetValues("Set-Cookie", out var adminCookies)
            ? adminCookies.Select(value => value.Split(';', 2)[0])
                .FirstOrDefault(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase))
            : null;

        using (var linkResponse = await SendAsRoleAsync(HttpMethod.Patch, $"/api/members/{_sameCompanyOtherUserId}/employee", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId }), adminRequestToken, adminAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);

        using (var wrongTenantLink = await SendAsRoleAsync(HttpMethod.Patch, $"/api/members/{_sameCompanyOtherUserId}/employee", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _otherCompanyEmployeeId }), adminRequestToken, adminAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, wrongTenantLink.StatusCode);

        using (var duplicateLink = await SendAsRoleAsync(HttpMethod.Patch, $"/api/members/{_userId}/employee", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId }), adminRequestToken, adminAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicateLink.StatusCode);

        using (var memberList = await SendAsAdminAsync("/api/members"))
        using (var payload = JsonDocument.Parse(await memberList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, memberList.StatusCode);
            var linked = Assert.Single(payload.RootElement.GetProperty("members").EnumerateArray(),
                item => item.GetProperty("userId").GetGuid() == _sameCompanyOtherUserId);
            Assert.Equal(_ownEmployeeId, linked.GetProperty("employeeId").GetGuid());
            Assert.Equal($"OWN-{_userId:N}", linked.GetProperty("employeeNumber").GetString());
        }

        using var csrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _sameCompanyOtherUserId, _companyId);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.TryGetValues("Set-Cookie", out var userCookies)
            ? userCookies.Select(value => value.Split(';', 2)[0])
                .FirstOrDefault(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase))
            : null;

        using (var spoofedRequest = await SendAsUserAsync(HttpMethod.Post, "/api/requests", _sameCompanyOtherUserId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownSecondEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 1 }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, spoofedRequest.StatusCode);

        using var requestResponse = await SendAsUserAsync(HttpMethod.Post, "/api/requests", _sameCompanyOtherUserId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 1 }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        using var requestPayload = JsonDocument.Parse(await requestResponse.Content.ReadAsStringAsync());
        var requestId = requestPayload.RootElement.GetProperty("id").GetGuid();
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdRequest = await db.InventoryRequests.SingleAsync(x => x.CompanyId == _companyId && x.Id == requestId);
        Assert.Equal(_ownEmployeeId, createdRequest.EmployeeId);
        Assert.Equal(_sameCompanyOtherUserId, createdRequest.RequestedByUserId);

        db.InventoryRequests.Add(new InventoryRequest { CompanyId = _companyId, EmployeeId = _ownSecondEmployeeId,
            RequestedByUserId = _userId, ItemType = "Asset", ItemId = _ownAssetId, Quantity = 1, RequestNote = "Manager-created request" });
        var linkedMembership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _sameCompanyOtherUserId);
        linkedMembership.RoleKey = "viewer";
        await db.SaveChangesAsync();

        using (var managerRequests = await SendAsAdminAsync("/api/requests"))
        using (var managerPayload = JsonDocument.Parse(await managerRequests.Content.ReadAsStringAsync()))
            Assert.Equal(2, managerPayload.RootElement.GetProperty("total").GetInt32());

        using (var ownRequests = await SendAsUserAsync(HttpMethod.Get, "/api/requests", _sameCompanyOtherUserId, _companyId, role: "viewer"))
        using (var ownPayload = JsonDocument.Parse(await ownRequests.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, ownRequests.StatusCode);
            var ownItems = ownPayload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(requestId, Assert.Single(ownItems).GetProperty("id").GetGuid());
        }

        using (var employeeChoices = await SendAsUserAsync(HttpMethod.Get, "/api/requests/employees", _sameCompanyOtherUserId, _companyId, role: "viewer"))
        using (var employeePayload = JsonDocument.Parse(await employeeChoices.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, employeeChoices.StatusCode);
            var choices = employeePayload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(_ownEmployeeId, Assert.Single(choices).GetProperty("id").GetGuid());
        }

        using var ownExport = await SendAsUserAsync(HttpMethod.Get, "/api/requests/export.csv", _sameCompanyOtherUserId, _companyId, role: "viewer");
        var ownCsv = await ownExport.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, ownExport.StatusCode);
        Assert.Contains($"OWN-{_userId:N}", ownCsv);
        Assert.DoesNotContain("OWN-SECOND-", ownCsv);

        linkedMembership.EmployeeId = null;
        await db.SaveChangesAsync();
        using (var unlinkedChoices = await SendAsUserAsync(HttpMethod.Get, "/api/requests/employees", _sameCompanyOtherUserId, _companyId, role: "viewer"))
        using (var unlinkedPayload = JsonDocument.Parse(await unlinkedChoices.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, unlinkedChoices.StatusCode);
            Assert.Empty(unlinkedPayload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, unlinkedPayload.RootElement.GetProperty("selfEmployee").ValueKind);
        }

        using (var unlinkedRequest = await SendAsUserAsync(HttpMethod.Post, "/api/requests", _sameCompanyOtherUserId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownSecondEmployeeId, itemType = "Asset", itemId = _ownAssetId, quantity = 1 }),
                   requestToken, antiforgeryCookie, role: "viewer"))
            Assert.Equal(HttpStatusCode.Conflict, unlinkedRequest.StatusCode);
    }

    [Fact]
    public async Task Stock_lists_details_exports_and_combined_movements_stay_within_the_active_company()
    {
        var inventory = new (string ListPath, string DetailPath, string ExportPath, Guid OwnId, Guid OtherId,
            string OwnName, string OtherName)[]
        {
            ($"/api/accessories", $"/api/accessories/{_otherCompanyAccessoryId}", "/api/accessories/export.csv",
                _ownAccessoryId, _otherCompanyAccessoryId, $"OWN-SECURITY-ACCESSORY-{_userId:N}", $"OTHER-SECURITY-ACCESSORY-{_userId:N}"),
            ($"/api/consumables", $"/api/consumables/{_otherCompanyConsumableId}", "/api/consumables/export.csv",
                _ownConsumableId, _otherCompanyConsumableId, $"OWN-SECURITY-CONSUMABLE-{_userId:N}", $"OTHER-SECURITY-CONSUMABLE-{_userId:N}"),
            ($"/api/components", $"/api/components/{_otherCompanyComponentId}", "/api/components/export.csv",
                _ownComponentId, _otherCompanyComponentId, $"OWN-SECURITY-COMPONENT-{_userId:N}", $"OTHER-SECURITY-COMPONENT-{_userId:N}")
        };

        foreach (var (listPath, detailPath, exportPath, ownId, otherId, ownName, otherName) in inventory)
        {
            using (var listResponse = await SendAsAdminAsync(listPath))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                using var payload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
                var ids = payload.RootElement.GetProperty("items").EnumerateArray()
                    .Select(item => item.GetProperty("id").GetGuid()).ToArray();
                Assert.Contains(ownId, ids);
                Assert.DoesNotContain(otherId, ids);
            }

            using (var detailResponse = await SendAsAdminAsync(detailPath))
                Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);

            using var exportResponse = await SendAsAdminAsync(exportPath);
            Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
            var csv = await exportResponse.Content.ReadAsStringAsync();
            Assert.Contains(ownName, csv, StringComparison.Ordinal);
            Assert.False(csv.Contains(otherName, StringComparison.Ordinal));
        }

        using (var componentReport = await SendAsAdminAsync("/api/reports/components"))
        using (var reportPayload = JsonDocument.Parse(await componentReport.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, componentReport.StatusCode);
            Assert.Equal(1, reportPayload.RootElement.GetProperty("componentCount").GetInt32());
            Assert.Equal(5, reportPayload.RootElement.GetProperty("totalQuantity").GetInt32());
            Assert.Equal(3, reportPayload.RootElement.GetProperty("allocatedQuantity").GetInt32());
            Assert.Equal(2, reportPayload.RootElement.GetProperty("availableQuantity").GetInt32());
        }
        using (var auditReport = await SendAsAdminAsync("/api/reports/audits?q=OWN-AUDIT-REPORT"))
        {
            var auditBody = await auditReport.Content.ReadAsStringAsync();
            using var auditPayload = JsonDocument.Parse(auditBody);
            Assert.Equal(HttpStatusCode.OK, auditReport.StatusCode);
            Assert.Equal(1, auditPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Contains("OWN-1", auditBody, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-AUDIT-REPORT", auditBody, StringComparison.Ordinal);
        }
        using (var auditExport = await SendAsAdminAsync("/api/reports/audits/export.csv?q=OWN-AUDIT-REPORT"))
        {
            var csv = await auditExport.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, auditExport.StatusCode);
            Assert.Contains("OWN-1", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-AUDIT-REPORT", csv, StringComparison.Ordinal);
        }

        using (var movementsResponse = await SendAsAdminAsync("/api/inventory/movements"))
        {
            Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);
            using var payload = JsonDocument.Parse(await movementsResponse.Content.ReadAsStringAsync());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(3, items.Length);
            Assert.Contains(items, item => item.GetProperty("item").GetString() == $"OWN-SECURITY-ACCESSORY-{_userId:N}");
            Assert.Contains(items, item => item.GetProperty("item").GetString() == $"OWN-SECURITY-CONSUMABLE-{_userId:N}");
            Assert.Contains(items, item => item.GetProperty("item").GetString() == $"OWN-SECURITY-COMPONENT-{_userId:N}");
            Assert.DoesNotContain(items, item => item.GetProperty("item").GetString()?.StartsWith("OTHER-SECURITY-", StringComparison.Ordinal) == true);
        }

        using (var filteredMovements = await SendAsAdminAsync($"/api/inventory/movements?kind=accessory&movementType=Received&q={Uri.EscapeDataString($"OWN-SECURITY-ACCESSORY-{_userId:N}")}&page=1&pageSize=1"))
        using (var filteredPayload = JsonDocument.Parse(await filteredMovements.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, filteredMovements.StatusCode);
            Assert.Equal(1, filteredPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(1, filteredPayload.RootElement.GetProperty("items").GetArrayLength());
            Assert.Equal("accessory", filteredPayload.RootElement.GetProperty("items")[0].GetProperty("kind").GetString());
            Assert.Equal("Received", filteredPayload.RootElement.GetProperty("items")[0].GetProperty("movementType").GetString());
        }
        using (var secondMovementPage = await SendAsAdminAsync("/api/inventory/movements?page=2&pageSize=1"))
        using (var secondPagePayload = JsonDocument.Parse(await secondMovementPage.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, secondMovementPage.StatusCode);
            Assert.Equal(2, secondPagePayload.RootElement.GetProperty("page").GetInt32());
            Assert.Equal(3, secondPagePayload.RootElement.GetProperty("total").GetInt32());
            Assert.Single(secondPagePayload.RootElement.GetProperty("items").EnumerateArray());
        }
        using (var invalidMovementFilter = await SendAsAdminAsync("/api/inventory/movements?kind=unknown"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidMovementFilter.StatusCode);
        using (var invalidMovementDates = await SendAsAdminAsync("/api/inventory/movements?from=2026-10-12&to=2026-10-11"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidMovementDates.StatusCode);

        using var movementsExportResponse = await SendAsAdminAsync("/api/inventory/movements?export=true");
        Assert.Equal(HttpStatusCode.OK, movementsExportResponse.StatusCode);
        var movementsCsv = await movementsExportResponse.Content.ReadAsStringAsync();
        Assert.Contains($"OWN-SECURITY-ACCESSORY-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.Contains($"OWN-SECURITY-CONSUMABLE-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.Contains($"OWN-SECURITY-COMPONENT-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.False(movementsCsv.Contains("OTHER-SECURITY-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Employee_profile_lists_only_company_maintenance_assigned_to_that_employee()
    {
        var startedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetMaintenances.AddRange(
                new AssetMaintenance
                {
                    CompanyId = _companyId, AssetId = _ownAssetId, ResponsibleEmployeeId = _ownEmployeeId,
                    MaintenanceType = "OWN-EMPLOYEE-MAINTENANCE", Status = "InProgress", StartedOn = startedOn
                },
                new AssetMaintenance
                {
                    CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId, ResponsibleEmployeeId = _otherCompanyEmployeeId,
                    MaintenanceType = "OTHER-EMPLOYEE-MAINTENANCE", Status = "InProgress", StartedOn = startedOn
                });
            await db.SaveChangesAsync();
        }

        using var adminResponse = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        using var adminPayload = JsonDocument.Parse(await adminResponse.Content.ReadAsStringAsync());
        var ownMaintenance = Assert.Single(adminPayload.RootElement.GetProperty("responsibleMaintenances").EnumerateArray());
        Assert.Equal("OWN-EMPLOYEE-MAINTENANCE", ownMaintenance.GetProperty("maintenanceType").GetString());
        Assert.Equal(1, adminPayload.RootElement.GetProperty("responsibleMaintenanceCount").GetInt32());
        Assert.Equal(_ownAssetId, ownMaintenance.GetProperty("assetId").GetGuid());

        using var viewerResponse = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}", "viewer");
        Assert.Equal(HttpStatusCode.OK, viewerResponse.StatusCode);
        using var viewerPayload = JsonDocument.Parse(await viewerResponse.Content.ReadAsStringAsync());
        Assert.Equal("OWN-EMPLOYEE-MAINTENANCE", Assert.Single(viewerPayload.RootElement.GetProperty("responsibleMaintenances").EnumerateArray()).GetProperty("maintenanceType").GetString());

        using var otherCompanyResponse = await SendAsUserAsync(HttpMethod.Get, $"/api/employees/{_otherCompanyEmployeeId}", _userId, _companyId, role: "admin");
        Assert.Equal(HttpStatusCode.NotFound, otherCompanyResponse.StatusCode);
    }

    [Fact]
    public async Task Employee_edit_and_manager_hierarchy_are_company_scoped_validated_and_audited()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var managerId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(new Employee
            {
                Id = managerId, CompanyId = _companyId, EmployeeNumber = $"MANAGER-{_userId:N}",
                DisplayName = "Acceptance manager", Department = "IT"
            });
            await db.SaveChangesAsync();
        }

        using (var update = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       employeeNumber = $"UPDATED-{_userId:N}", displayName = "Updated acceptance employee",
                       email = "updated-employee@example.invalid", department = "IT", jobTitle = "Support",
                       phone = "+90 555 000 00 00", location = "Istanbul Office", startedOn = "2021-06-07"
                   }), requestToken, antiforgeryCookie))
        {
            Assert.True(update.StatusCode == HttpStatusCode.OK,
                $"Employee update failed: {(int)update.StatusCode} {await update.Content.ReadAsStringAsync()}");
            using var payload = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
            Assert.Equal("Updated acceptance employee", payload.RootElement.GetProperty("displayName").GetString());
            Assert.Equal("IT", payload.RootElement.GetProperty("department").GetString());
        }

        using (var assignManager = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}/manager", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { managerEmployeeId = managerId }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, assignManager.StatusCode);
            using var payload = JsonDocument.Parse(await assignManager.Content.ReadAsStringAsync());
            Assert.Equal(managerId, payload.RootElement.GetProperty("managerEmployeeId").GetGuid());
            Assert.Equal("Acceptance manager", payload.RootElement.GetProperty("managerName").GetString());
        }

        using (var selfManager = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}/manager", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { managerEmployeeId = _ownEmployeeId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, selfManager.StatusCode);

        using (var cycle = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{managerId}/manager", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { managerEmployeeId = _ownEmployeeId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);

        using (var crossCompanyManager = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}/manager", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { managerEmployeeId = _otherCompanyEmployeeId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, crossCompanyManager.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var viewerEdit = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       employeeNumber = $"UPDATED-{_userId:N}", displayName = "Unauthorized edit", email = (string?)null,
                       department = (string?)null, jobTitle = (string?)null, phone = (string?)null, location = (string?)null,
                       startedOn = (DateOnly?)null
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerEdit.StatusCode);

        await SetCompanyRoleAsync("admin");
        using (var otherEmployeeEdit = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_otherCompanyEmployeeId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(new
                   {
                       employeeNumber = "OTHER-EDIT", displayName = "Cross company edit", email = (string?)null,
                       department = (string?)null, jobTitle = (string?)null, phone = (string?)null, location = (string?)null,
                       startedOn = (DateOnly?)null
                   }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, otherEmployeeEdit.StatusCode);

        using (var removeManager = await SendAsRoleAsync(HttpMethod.Patch, $"/api/employees/{_ownEmployeeId}/manager", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { managerEmployeeId = (Guid?)null }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, removeManager.StatusCode);
            using var payload = JsonDocument.Parse(await removeManager.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("managerEmployeeId").ValueKind);
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var savedEmployee = await db.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownEmployeeId);
            Assert.Equal($"UPDATED-{_userId:N}".ToUpperInvariant(), savedEmployee.EmployeeNumber);
            Assert.Null(savedEmployee.ManagerEmployeeId);
            Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetId == _ownEmployeeId && x.Action == "Updated"));
            Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetId == _ownEmployeeId && x.Action == "ManagerChanged"));
        }
    }

    [Fact]
    public async Task Employee_private_notes_are_restricted_company_scoped_trimmed_and_audited_without_copying_content()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var save = await SendAsRoleAsync(HttpMethod.Put, $"/api/employees/{_ownEmployeeId}/private-notes", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { notes = "  HR-PRIVATE-SENTINEL — işe dönüş planı  " }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            using var payload = JsonDocument.Parse(await save.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("updated").GetBoolean());
            Assert.Equal("HR-PRIVATE-SENTINEL — işe dönüş planı", payload.RootElement.GetProperty("notes").GetString());
        }

        using (var read = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}/private-notes", "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var payload = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            Assert.Equal("HR-PRIVATE-SENTINEL — işe dönüş planı", payload.RootElement.GetProperty("notes").GetString());
            Assert.Equal(4000, payload.RootElement.GetProperty("maxLength").GetInt32());
        }

        using (var unchanged = await SendAsRoleAsync(HttpMethod.Put, $"/api/employees/{_ownEmployeeId}/private-notes", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { notes = "HR-PRIVATE-SENTINEL — işe dönüş planı" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
            using var payload = JsonDocument.Parse(await unchanged.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("updated").GetBoolean());
        }

        using (var tooLong = await SendAsRoleAsync(HttpMethod.Put, $"/api/employees/{_ownEmployeeId}/private-notes", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { notes = new string('x', 4001) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var deniedRead = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}/private-notes", "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, deniedRead.StatusCode);
        await SetCompanyRoleAsync("admin");

        using (var crossCompany = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_otherCompanyEmployeeId}/private-notes", "admin"))
            Assert.Equal(HttpStatusCode.NotFound, crossCompany.StatusCode);

        using (var clear = await SendAsRoleAsync(HttpMethod.Put, $"/api/employees/{_ownEmployeeId}/private-notes", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { notes = "   " }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
            using var payload = JsonDocument.Parse(await clear.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("updated").GetBoolean());
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("notes").ValueKind);
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = await db.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownEmployeeId);
            Assert.Null(employee.PrivateNotes);
            var audit = await db.AuditEntries.AsNoTracking()
                .Where(x => x.CompanyId == _companyId && x.TargetId == _ownEmployeeId && x.Action == "PrivateNotesUpdated")
                .ToListAsync();
            Assert.Equal(2, audit.Count);
            Assert.All(audit, entry => Assert.DoesNotContain("HR-PRIVATE-SENTINEL", entry.Details, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Employee_document_upload_download_and_delete_validate_content_and_leave_an_audit_trail()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nEmployee acceptance document\n%%EOF");

        using (var invalid = await SendEmployeeDocumentUploadAsync(_ownEmployeeId, [1, 2, 3, 4], "fake.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (var empty = await SendEmployeeDocumentUploadAsync(_ownEmployeeId, [], "empty.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        using var upload = await SendEmployeeDocumentUploadAsync(_ownEmployeeId, pdfBytes,
            "../../İK Belgesi.pdf", requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var documentId = uploaded.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("İK Belgesi.pdf", uploaded.RootElement.GetProperty("originalFileName").GetString());
        Assert.Equal("application/pdf", uploaded.RootElement.GetProperty("contentType").GetString());

        using (var list = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents"))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            Assert.Equal(30, payload.RootElement.GetProperty("maxFiles").GetInt32());
            Assert.Equal(10_485_760, payload.RootElement.GetProperty("maxFileSizeBytes").GetInt32());
            var item = Assert.Single(payload.RootElement.GetProperty("documents").EnumerateArray());
            Assert.Equal(documentId, item.GetProperty("id").GetGuid());
            Assert.Equal("İK Belgesi.pdf", item.GetProperty("originalFileName").GetString());
        }

        using (var download = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents/{documentId}/file"))
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pdfBytes, await download.Content.ReadAsByteArrayAsync());
        }

        var storedPath = Path.Combine(_mediaPath, _companyId.ToString("N"), "employees", _ownEmployeeId.ToString("N"), "documents", $"{documentId:N}.pdf");
        Assert.True(File.Exists(storedPath));
        await SetCompanyRoleAsync("viewer");
        using (var deniedList = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}/documents", "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);
        using (var deniedDelete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/employees/{_ownEmployeeId}/documents/{documentId}", "viewer",
                   requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
        Assert.True(File.Exists(storedPath));
        await SetCompanyRoleAsync("admin");

        using (var delete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/employees/{_ownEmployeeId}/documents/{documentId}", "admin",
                   requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(File.Exists(storedPath));
        using (var missing = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents/{documentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using (var list = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents"))
        using (var payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
            Assert.Empty(payload.RootElement.GetProperty("documents").EnumerateArray());

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actions = await auditDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "Employee" && entry.TargetId == _ownEmployeeId).Select(entry => entry.Action).ToListAsync();
        Assert.Contains("DocumentAdded", actions);
        Assert.Contains("DocumentRemoved", actions);
    }

    [Fact]
    public async Task Employee_csv_import_preview_is_read_only_and_commit_is_atomic_round_trips_managers_and_audits()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        const string header = "Personel No,Ad Soyad,E-posta,Departman,Görev Unvanı,Telefon,Konum,İşe Başlama Tarihi,Yönetici,Durum,Kayıt Tarihi,Yönetici Personel No\n";
        var managerNumber = $"IMPORT-MANAGER-{_userId:N}".ToUpperInvariant();
        var employeeNumber = $"IMPORT-EMPLOYEE-{_userId:N}".ToUpperInvariant();
        var csv = header +
            $"{managerNumber},Imported Manager,manager-{_userId:N}@example.invalid,IT,Lead,,Istanbul,2022-03-14,,Etkin,,\n" +
            $"{employeeNumber},Imported Employee,employee-{_userId:N}@example.invalid,IT,Support,,Istanbul,2024-01-08,Imported Manager,Etkin,,{managerNumber}\n";
        var content = () => new StringContent(csv, System.Text.Encoding.UTF8, "text/csv");

        using (var noCsrf = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import.csv", "admin", content()))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        using (var preview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.csv", "admin",
                   content(), requestToken, antiforgeryCookie))
        {
            Assert.True(preview.StatusCode == HttpStatusCode.OK,
                $"Employee CSV preview failed: {(int)preview.StatusCode} {await preview.Content.ReadAsStringAsync()}");
            using var payload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(2, payload.RootElement.GetProperty("rowCount").GetInt32());
        }

        await using (var beforeCommit = _factory!.Services.CreateAsyncScope())
        {
            var db = beforeCommit.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Employees.AnyAsync(x => x.CompanyId == _companyId &&
                (x.EmployeeNumber == managerNumber || x.EmployeeNumber == employeeNumber)));
        }

        var cyclicCsv = header +
            $"CYCLE-A-{_userId:N},Cycle A,,,,,,,,Etkin,,CYCLE-B-{_userId:N}\n" +
            $"CYCLE-B-{_userId:N},Cycle B,,,,,,,,Etkin,,CYCLE-A-{_userId:N}\n";
        using (var invalidPreview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.csv", "admin",
                   new StringContent(cyclicCsv, System.Text.Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidPreview.StatusCode);
            using var payload = JsonDocument.Parse(await invalidPreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(),
                error => error.GetString()?.Contains("döngü", StringComparison.OrdinalIgnoreCase) == true);
        }

        using (var commit = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import.csv", "admin",
                   content(), requestToken, antiforgeryCookie))
        {
            Assert.True(commit.StatusCode == HttpStatusCode.OK,
                $"Employee CSV commit failed: {(int)commit.StatusCode} {await commit.Content.ReadAsStringAsync()}");
            using var payload = JsonDocument.Parse(await commit.Content.ReadAsStringAsync());
            Assert.Equal(2, payload.RootElement.GetProperty("imported").GetInt32());
        }

        await using (var afterCommit = _factory!.Services.CreateAsyncScope())
        {
            var db = afterCommit.ServiceProvider.GetRequiredService<AppDbContext>();
            var manager = await db.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == managerNumber);
            var employee = await db.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == employeeNumber);
            Assert.Equal("Imported Manager", manager.DisplayName);
            Assert.True(manager.IsActive);
            Assert.Equal(new DateOnly(2022, 3, 14), manager.StartedOn);
            Assert.Equal(manager.Id, employee.ManagerEmployeeId);
            Assert.True(employee.IsActive);
            Assert.Equal(new DateOnly(2024, 1, 8), employee.StartedOn);
            Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetType == "Employee" &&
                (x.TargetId == manager.Id || x.TargetId == employee.Id) && x.Action == "Created"));
            Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetType == "Employee" &&
                x.TargetId == null && x.Action == "CsvImported"));
        }

        using (var duplicatePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.csv", "admin",
                   content(), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicatePreview.StatusCode);
            using var payload = JsonDocument.Parse(await duplicatePreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
        }

        await SetCompanyRoleAsync("viewer");
        using (var deniedCommit = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import.csv", "viewer",
                   content(), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, deniedCommit.StatusCode);
    }

    [Fact]
    public async Task Employee_xlsx_import_preview_and_commit_preserve_values_and_audit()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var employeeNumber = $"XLSX-{_userId:N}".ToUpperInvariant();
        var startedOn = new DateTime(2025, 2, 3);
        var workbook = CreateEmployeeImportWorkbook(
            ["Personel No", "Ad Soyad", "E-posta", "Departman", "Görev Unvanı", "Telefon", "Konum", "İşe Başlama Tarihi"],
            [employeeNumber, "Excel Employee", $"xlsx-{_userId:N}@example.invalid", "IT", "Engineer", "555-0100", "Istanbul", startedOn.ToOADate()]);
        HttpContent Content() => new ByteArrayContent(workbook) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") } };

        using (var preview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.xlsx", "admin",
                   Content(), requestToken, antiforgeryCookie))
        {
            Assert.True(preview.StatusCode == HttpStatusCode.OK,
                $"Employee XLSX preview failed: {(int)preview.StatusCode} {await preview.Content.ReadAsStringAsync()}");
            using var payload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal(1, payload.RootElement.GetProperty("rowCount").GetInt32());
        }

        await using (var beforeCommit = _factory!.Services.CreateAsyncScope())
        {
            var db = beforeCommit.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Employees.AnyAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == employeeNumber));
        }

        using (var commit = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import.xlsx", "admin",
                   Content(), requestToken, antiforgeryCookie))
        {
            Assert.True(commit.StatusCode == HttpStatusCode.OK,
                $"Employee XLSX commit failed: {(int)commit.StatusCode} {await commit.Content.ReadAsStringAsync()}");
            using var payload = JsonDocument.Parse(await commit.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("imported").GetInt32());
        }

        await using var afterCommit = _factory!.Services.CreateAsyncScope();
        var dbAfter = afterCommit.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await dbAfter.Employees.AsNoTracking().SingleAsync(x => x.CompanyId == _companyId && x.EmployeeNumber == employeeNumber);
        Assert.Equal("Excel Employee", employee.DisplayName);
        Assert.Equal($"xlsx-{_userId:N}@example.invalid", employee.Email);
        Assert.Equal(new DateOnly(2025, 2, 3), employee.StartedOn);
        Assert.True(employee.IsActive);
        Assert.Equal(1, await dbAfter.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetType == "Employee" &&
            x.TargetId == null && x.Action == "XlsxImported"));
    }

    [Fact]
    public async Task Asset_checkout_queue_is_company_scoped_searchable_and_filters_expected_returns()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var own = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            own.Status = AssetStatuses.Deployed;
            own.AssignedEmployeeId = _ownEmployeeId;
            own.ExpectedReturnOn = today;
            var other = await db.Assets.IgnoreQueryFilters().SingleAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyAssetId);
            other.Status = AssetStatuses.Deployed;
            other.AssignedEmployeeId = _otherCompanyEmployeeId;
            other.ExpectedReturnOn = today;
            await db.SaveChangesAsync();
        }

        using (var response = await SendAsAdminAsync("/api/assets/checkouts?state=due30&page=1&pageSize=10"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var item = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(_ownAssetId, item.GetProperty("id").GetGuid());
            Assert.Equal(_ownEmployeeId, item.GetProperty("employeeId").GetGuid());
            Assert.Equal("OWN-1", item.GetProperty("assetTag").GetString());
        }

        using (var response = await SendAsAdminAsync("/api/assets/checkouts?q=OTHER-1"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }

        using var invalidState = await SendAsAdminAsync("/api/assets/checkouts?state=all-tenants");
        Assert.Equal(HttpStatusCode.BadRequest, invalidState.StatusCode);
    }

    [Fact]
    public async Task Asset_detail_shows_open_accessory_and_component_relations_only_for_its_company()
    {
        using var response = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessories = payload.RootElement.GetProperty("accessories").EnumerateArray().ToArray();
        Assert.Single(accessories);
        Assert.Equal($"OWN-SECURITY-ACCESSORY-{_userId:N}", accessories[0].GetProperty("accessoryName").GetString());
        Assert.Equal(2, accessories[0].GetProperty("remaining").GetInt32());
        var components = payload.RootElement.GetProperty("components").EnumerateArray().ToArray();
        Assert.Single(components);
        Assert.Equal($"OWN-SECURITY-COMPONENT-{_userId:N}", components[0].GetProperty("componentName").GetString());
        Assert.Equal(3, components[0].GetProperty("remaining").GetInt32());
        Assert.DoesNotContain(accessories, item => item.GetProperty("accessoryName").GetString()?.StartsWith("OTHER-SECURITY-", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(components, item => item.GetProperty("componentName").GetString()?.StartsWith("OTHER-SECURITY-", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Asset_notes_require_write_access_and_are_audited_in_the_tenant_history()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var path = $"/api/assets/{_ownAssetId}/notes";

        using (var missingCsrf = await SendAsUserAsync(HttpMethod.Post, path, _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { note = "CSRF must be required" })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        const string note = "Dock 3 portu test edildi; bağlantı sabit.";
        using (var create = await SendAsUserAsync(HttpMethod.Post, path, _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { note }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using (var empty = await SendAsUserAsync(HttpMethod.Post, path, _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { note = "  " }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        using (var crossTenant = await SendAsUserAsync(HttpMethod.Post, $"/api/assets/{_otherCompanyAssetId}/notes", _userId, _companyId,
                   System.Net.Http.Json.JsonContent.Create(new { note }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        await using (var roleScope = _factory!.Services.CreateAsyncScope())
        {
            var roleDb = roleScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var membership = await roleDb.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
            membership.RoleKey = "viewer";
            await roleDb.SaveChangesAsync();
        }
        using (var viewer = await SendAsRoleAsync(HttpMethod.Post, path, "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { note = "Viewer cannot write" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);

        using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var savedNote = Assert.Single(payload.RootElement.GetProperty("history").EnumerateArray(),
            item => item.GetProperty("eventType").GetString() == "NoteAdded");
        Assert.Equal(note, savedNote.GetProperty("details").GetString());
        Assert.Equal("Security Acceptance User", savedNote.GetProperty("actorName").GetString());

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AuditEntries.AnyAsync(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "Asset" && entry.TargetId == _ownAssetId && entry.Action == "NoteAdded"));
        Assert.False(await db.AssetEvents.IgnoreQueryFilters().AnyAsync(item => item.CompanyId == _otherCompanyId &&
            item.AssetId == _otherCompanyAssetId && item.EventType == "NoteAdded"));
    }

    [Fact]
    public async Task Asset_trash_is_tenant_scoped_blocks_live_relations_and_restores_with_audit_history()
    {
        var trashAssetId = Guid.NewGuid();
        var collisionAssetId = Guid.NewGuid();
        var trashAssetTag = $"TRASH-{_userId:N}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
                .Select(x => x.CategoryId).SingleAsync();
            db.Assets.Add(new Asset { Id = trashAssetId, CompanyId = _companyId, AssetTag = trashAssetTag, Name = "Trash acceptance asset", CategoryId = categoryId });
            var foreignAsset = await db.Assets.IgnoreQueryFilters().SingleAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyAssetId);
            foreignAsset.DeletedAt = DateTimeOffset.UtcNow;
            foreignAsset.DeletedByUserId = _userId;
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var relatedAsset = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/trash", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "open relations must remain safe" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, relatedAsset.StatusCode);

        using (var foreignRestore = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_otherCompanyAssetId}/restore-from-trash", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "cross-tenant attempt" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignRestore.StatusCode);

        using (var trashResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{trashAssetId}/trash", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "retired test device" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, trashResponse.StatusCode);
            using var responsePayload = JsonDocument.Parse(await trashResponse.Content.ReadAsStringAsync());
            Assert.Equal(trashAssetId, responsePayload.RootElement.GetProperty("id").GetGuid());
        }

        using (var hiddenDetail = await SendAsAdminAsync($"/api/assets/{trashAssetId}"))
            Assert.Equal(HttpStatusCode.NotFound, hiddenDetail.StatusCode);
        using (var trashResponse = await SendAsAdminAsync("/api/assets/trash?page=1&pageSize=100"))
        {
            Assert.Equal(HttpStatusCode.OK, trashResponse.StatusCode);
            using var responsePayload = JsonDocument.Parse(await trashResponse.Content.ReadAsStringAsync());
            var items = responsePayload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == trashAssetId);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == _otherCompanyAssetId);
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
                .Select(x => x.CategoryId).SingleAsync();
            db.Assets.Add(new Asset { Id = collisionAssetId, CompanyId = _companyId, AssetTag = trashAssetTag, Name = "Active tag collision", CategoryId = categoryId });
            await db.SaveChangesAsync();
        }
        using (var collisionRestore = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{trashAssetId}/restore-from-trash", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "tag collision" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, collisionRestore.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var collision = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == collisionAssetId);
            db.Assets.Remove(collision);
            await db.SaveChangesAsync();
        }
        using (var restoreResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{trashAssetId}/restore-from-trash", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "recovered after review" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        using (var restoredDetail = await SendAsAdminAsync($"/api/assets/{trashAssetId}"))
            Assert.Equal(HttpStatusCode.OK, restoredDetail.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var events = await db.AssetEvents.AsNoTracking().Where(x => x.CompanyId == _companyId && x.AssetId == trashAssetId)
                .Select(x => x.EventType).ToArrayAsync();
            Assert.Contains("Trashed", events);
            Assert.Contains("RestoredFromTrash", events);
            var auditActions = await db.AuditEntries.AsNoTracking().Where(x => x.CompanyId == _companyId && x.TargetId == trashAssetId)
                .Select(x => x.Action).ToArrayAsync();
            Assert.Contains("Trashed", auditActions);
            Assert.Contains("RestoredFromTrash", auditActions);
        }
    }

    [Fact]
    public async Task Calendar_feed_for_upcoming_work_contains_only_the_active_company_records()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var to = today.AddDays(30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var response = await SendAsRoleAsync(HttpMethod.Get, $"/api/calendar?from={from}&to={to}&limit=500", "viewer");
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {responseBody}");

        using var payload = JsonDocument.Parse(responseBody);
        Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
        var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var ownLicense = Assert.Single(items);
        Assert.Equal(_ownLicenseId, ownLicense.GetProperty("recordId").GetGuid());
        Assert.StartsWith("OWN-SECURITY-LICENSE-", ownLicense.GetProperty("title").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain(items, item => item.GetProperty("title").GetString()?.StartsWith("OTHER-SECURITY-", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Expected_asset_returns_are_listed_in_the_calendar_only_for_the_active_company()
    {
        var expectedReturnOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var asset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            asset.Status = AssetStatuses.Deployed;
            asset.AssignedEmployeeId = _ownEmployeeId;
            asset.ExpectedReturnOn = expectedReturnOn;
            await db.SaveChangesAsync();
        }

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var to = expectedReturnOn.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var response = await SendAsRoleAsync(HttpMethod.Get, $"/api/calendar?from={from}&to={to}&kind=return&limit=500", "viewer");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {body}");
        using var payload = JsonDocument.Parse(body);
        var item = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("return", item.GetProperty("kind").GetString());
        Assert.Equal(_ownAssetId, item.GetProperty("assetId").GetGuid());
        Assert.Equal(expectedReturnOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), item.GetProperty("date").GetString());
        Assert.DoesNotContain("OTHER-SECURITY", item.GetProperty("title").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asset_assignment_accepts_a_valid_expected_return_and_return_clears_it_with_history()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var expectedReturnOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);
        using (var invalidDate = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/assign", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, expectedReturnOn = expectedReturnOn.AddDays(-20).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidDate.StatusCode);

        using var assign = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/assign", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, expectedReturnOn = expectedReturnOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), note = "Acceptance due date" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        using var assignedPayload = JsonDocument.Parse(await assign.Content.ReadAsStringAsync());
        Assert.Equal(expectedReturnOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), assignedPayload.RootElement.GetProperty("expectedReturnOn").GetString());

        using var returned = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/return", "admin",
            System.Net.Http.Json.JsonContent.Create(new { note = "Returned in acceptance test" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        using var returnedPayload = JsonDocument.Parse(await returned.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, returnedPayload.RootElement.GetProperty("expectedReturnOn").ValueKind);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await db.AssetEvents.Where(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId).ToListAsync(),
            item => item.EventType == "Assigned" && item.Details!.Contains(expectedReturnOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Assert.Contains(await db.AssetEvents.Where(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId).ToListAsync(),
            item => item.EventType == "Returned" && item.Details!.Contains(expectedReturnOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Asset_transfers_are_tenant_scoped_and_appear_in_the_transfer_history_filter()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        const string targetLocation = "Acceptance transfer destination";
        await SetCompanyRoleAsync("viewer");
        using (var readOnlyTransfer = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/transfer", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { targetLocation = "Read-only forbidden destination", notes = "Permission boundary" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnlyTransfer.StatusCode);
        await SetCompanyRoleAsync("admin");

        using var transfer = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/transfer", "admin",
            System.Net.Http.Json.JsonContent.Create(new { targetLocation, notes = "Transfer history acceptance" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        using var history = await SendAsAdminAsync($"/api/activity?eventType=Transferred&q={Uri.EscapeDataString("OWN-1")}");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyPayload = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var entry = Assert.Single(historyPayload.RootElement.GetProperty("entries").EnumerateArray());
        Assert.Equal(_ownAssetId, entry.GetProperty("assetId").GetGuid());
        Assert.Contains(targetLocation, entry.GetProperty("details").GetString(), StringComparison.Ordinal);

        using var crossTenantHistory = await SendAsAdminAsync($"/api/activity?eventType=Transferred&q={Uri.EscapeDataString("OWN-1")}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantHistory.StatusCode);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(targetLocation, await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
            .Select(x => x.Location).SingleAsync());

        var assignedAsset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
        assignedAsset.AssignedEmployeeId = _ownEmployeeId;
        assignedAsset.Status = AssetStatuses.Deployed;
        await db.SaveChangesAsync();
        using var assignedTransfer = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/transfer", "admin",
            System.Net.Http.Json.JsonContent.Create(new { targetLocation = "Assigned asset must not move", notes = "Assignment boundary" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Conflict, assignedTransfer.StatusCode);

        Assert.Equal(targetLocation, await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
            .Select(x => x.Location).SingleAsync());
        Assert.Equal(1, await db.AssetEvents.CountAsync(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId && x.EventType == "Transferred"));
    }

    [Fact]
    public async Task Asset_status_action_is_authorized_tenant_scoped_validated_and_written_to_history()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_otherCompanyAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var invalidStatus = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = "Deployed" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        using (var oversizedNote = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable, note = new string('x', 1901) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, oversizedNote.StatusCode);

        using (var changed = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable, note = "Hardware inspection" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            using var payload = JsonDocument.Parse(await changed.Content.ReadAsStringAsync());
            Assert.Equal(AssetStatuses.Unusable, payload.RootElement.GetProperty("status").GetString());
        }
        using (var unchanged = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Unusable }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
            using var payload = JsonDocument.Parse(await unchanged.Content.ReadAsStringAsync());
            Assert.Equal(AssetStatuses.Unusable, payload.RootElement.GetProperty("status").GetString());
        }
        using (var restored = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/status", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { status = AssetStatuses.Available, note = "Inspection cleared" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);

        using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailPayload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(AssetStatuses.Available, detailPayload.RootElement.GetProperty("asset").GetProperty("status").GetString());
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item =>
            item.GetProperty("eventType").GetString() == "StatusChanged" && item.GetProperty("details").GetString()!.Contains("Hardware inspection", StringComparison.Ordinal));

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.AssetEvents.AsNoTracking().Where(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId).ToListAsync();
        Assert.Equal(2, events.Count(item => item.EventType == "StatusChanged"));
        Assert.Contains(events, item => item.EventType == "StatusChanged" && item.ActorUserId == _userId && item.Details!.Contains("Inspection cleared", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history()
    {
        Asset current;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            current = await db.Assets.AsNoTracking().SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownAssetId);
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var updatedName = $"{current.Name} edited";
        object Request(DateTimeOffset expectedUpdatedAt, Guid categoryId, string? name = null) => new
        {
            assetTag = current.AssetTag,
            name = name ?? updatedName,
            categoryId,
            brandId = current.BrandId,
            modelId = current.ModelId,
            serialNumber = current.SerialNumber,
            location = current.Location,
            purchaseDate = current.PurchaseDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            warrantyExpiresAt = current.WarrantyExpiresAt?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            purchaseCost = current.PurchaseCost,
            currency = current.Currency,
            expectedUpdatedAt,
            isRequestable = current.IsRequestable,
            isByod = current.IsByod,
            defaultLocationCatalogId = current.DefaultLocationCatalogId,
            notes = current.Notes
        };

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, current.CategoryId))))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "viewer",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, current.CategoryId)), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_otherCompanyAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, current.CategoryId)), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var invalidCategory = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, Guid.NewGuid())), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);
        using (var staleVersion = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt.AddTicks(-1), current.CategoryId)), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, staleVersion.StatusCode);

        using (var updated = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, current.CategoryId)), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            using var payload = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("changed").GetBoolean());
        }
        using (var replay = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}", "admin",
                   System.Net.Http.Json.JsonContent.Create(Request(current.UpdatedAt, current.CategoryId, "Stale overwrite")), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);

        using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailPayload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(updatedName, detailPayload.RootElement.GetProperty("asset").GetProperty("name").GetString());
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item =>
            item.GetProperty("eventType").GetString() == "Updated" && item.GetProperty("details").GetString()!.Contains("ad", StringComparison.Ordinal));

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await verifyDb.Assets.AsNoTracking().SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownAssetId);
        Assert.Equal(updatedName, saved.Name);
        Assert.True(saved.UpdatedAt > current.UpdatedAt);
    }

    [Fact]
    public async Task Asset_attachment_upload_download_and_delete_are_authorized_tenant_scoped_and_audited()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nasset attachment acceptance\n%%EOF");

        using (var invalidFile = await SendAssetAttachmentUploadAsync(_ownAssetId, [1, 2, 3], "invalid.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidFile.StatusCode);
        using (var crossTenantUpload = await SendAssetAttachmentUploadAsync(_otherCompanyAssetId, pdfBytes, "foreign.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantUpload.StatusCode);
        using (var missingCsrf = await SendAsUserAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/attachments", _userId, _companyId,
                   new MultipartFormDataContent(), requestToken: null, antiforgeryCookie: null))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var readOnlyUpload = await SendAssetAttachmentUploadAsync(_ownAssetId, pdfBytes, "denied.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnlyUpload.StatusCode);
        await SetCompanyRoleAsync("admin");

        using var upload = await SendAssetAttachmentUploadAsync(_ownAssetId, pdfBytes, "inspection-receipt.pdf", requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploadPayload = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var attachmentId = uploadPayload.RootElement.GetProperty("id").GetGuid();
        var attachmentPath = Path.Combine(_mediaPath, _companyId.ToString("N"), "assets", _ownAssetId.ToString("N"), $"{attachmentId:N}.pdf");
        Assert.True(File.Exists(attachmentPath));

        using (var download = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/attachments/{attachmentId}/file?download=true"))
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(pdfBytes, await download.Content.ReadAsByteArrayAsync());
        }
        using (var crossTenantDownload = await SendAsAdminAsync($"/api/assets/{_otherCompanyAssetId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDownload.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnlyDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/{_ownAssetId}/attachments/{attachmentId}",
                   _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnlyDelete.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossTenantDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/{_otherCompanyAssetId}/attachments/{attachmentId}",
                   _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDelete.StatusCode);
        using (var removed = await SendAsUserAsync(HttpMethod.Delete, $"/api/assets/{_ownAssetId}/attachments/{attachmentId}",
                   _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.False(File.Exists(attachmentPath));
        using (var missingAfterDelete = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, missingAfterDelete.StatusCode);
        using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        using var detailPayload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.DoesNotContain(detailPayload.RootElement.GetProperty("attachments").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == attachmentId);
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item => item.GetProperty("eventType").GetString() == "AttachmentAdded");
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item => item.GetProperty("eventType").GetString() == "AttachmentRemoved");

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AssetAttachments.AnyAsync(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId && item.Id == attachmentId));
        var auditActions = await db.AuditEntries.AsNoTracking().Where(item => item.CompanyId == _companyId &&
            item.TargetType == "Asset" && item.TargetId == _ownAssetId).Select(item => item.Action).ToListAsync();
        Assert.Contains("AttachmentAdded", auditActions);
        Assert.Contains("AttachmentRemoved", auditActions);
    }

    [Fact]
    public async Task Asset_maintenance_start_and_completion_preserve_authorization_state_and_history()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var request = new { maintenanceType = "Inspection", supplier = "Local workshop", description = "Acceptance maintenance",
            startedOn = today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            cost = 1200m, currency = "TRY", notes = "Intake inspection", responsibleEmployeeId = _ownEmployeeId };

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances", "admin",
                   System.Net.Http.Json.JsonContent.Create(request)))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances", "viewer",
                   System.Net.Http.Json.JsonContent.Create(request), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_otherCompanyAssetId}/maintenances", "admin",
                   System.Net.Http.Json.JsonContent.Create(request), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var invalidDate = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { maintenanceType = "Inspection", startedOn = today.AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), currency = "TRY" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidDate.StatusCode);

        using var started = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances", "admin",
            System.Net.Http.Json.JsonContent.Create(request), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        using var startedPayload = JsonDocument.Parse(await started.Content.ReadAsStringAsync());
        var maintenanceId = startedPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("InProgress", startedPayload.RootElement.GetProperty("status").GetString());

        using (var duplicate = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances", "admin",
                   System.Net.Http.Json.JsonContent.Create(request), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using (var tooEarly = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/complete", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { completedOn = today.AddDays(-2).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), notes = "Too early" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, tooEarly.StatusCode);
        using (var completed = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/complete", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { completedOn = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), notes = "Inspection complete" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
        using (var duplicateCompletion = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/complete", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { completedOn = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicateCompletion.StatusCode);

        using var assetDetail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        Assert.Equal(HttpStatusCode.OK, assetDetail.StatusCode);
        using var detailPayload = JsonDocument.Parse(await assetDetail.Content.ReadAsStringAsync());
        Assert.Equal(AssetStatuses.Available, detailPayload.RootElement.GetProperty("asset").GetProperty("status").GetString());
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item => item.GetProperty("eventType").GetString() == "MaintenanceStarted");
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item => item.GetProperty("eventType").GetString() == "MaintenanceCompleted");
        using var maintenanceList = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/maintenances");
        Assert.Equal(HttpStatusCode.OK, maintenanceList.StatusCode);
        using var listPayload = JsonDocument.Parse(await maintenanceList.Content.ReadAsStringAsync());
        var row = Assert.Single(listPayload.RootElement.EnumerateArray());
        Assert.Equal(maintenanceId, row.GetProperty("id").GetGuid());
        Assert.Equal("Completed", row.GetProperty("status").GetString());
        Assert.Equal(_ownEmployeeId, row.GetProperty("responsibleEmployeeId").GetGuid());

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.AssetEvents.AsNoTracking().Where(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId).ToListAsync();
        Assert.Contains(events, item => item.EventType == "MaintenanceStarted" && item.ActorUserId == _userId);
        Assert.Contains(events, item => item.EventType == "MaintenanceCompleted" && item.ActorUserId == _userId);
    }

    [Fact]
    public async Task Scheduled_maintenance_cancellation_enforces_csrf_company_and_role_scope_and_preserves_history()
    {
        var plannedOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var scheduleRequest = new { maintenanceType = "Inspection", plannedOn = plannedOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), currency = "TRY" };

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(scheduleRequest)))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/schedule", "viewer",
                   System.Net.Http.Json.JsonContent.Create(scheduleRequest), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");

        using var scheduled = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/maintenances/schedule", "admin",
            System.Net.Http.Json.JsonContent.Create(scheduleRequest), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, scheduled.StatusCode);
        using var scheduledPayload = JsonDocument.Parse(await scheduled.Content.ReadAsStringAsync());
        var maintenanceId = scheduledPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Scheduled", scheduledPayload.RootElement.GetProperty("status").GetString());

        var filter = $"q=Inspection&status=Scheduled&from={plannedOn:yyyy-MM-dd}&to={plannedOn:yyyy-MM-dd}&page=1&pageSize=1";
        using (var filteredList = await SendAsAdminAsync($"/api/maintenances?{filter}"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredList.StatusCode);
            using var payload = JsonDocument.Parse(await filteredList.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(maintenanceId, Assert.Single(payload.RootElement.GetProperty("rows").EnumerateArray()).GetProperty("id").GetGuid());
        }
        await using (var membershipScope = _factory!.Services.CreateAsyncScope())
        {
            var membershipDb = membershipScope.ServiceProvider.GetRequiredService<AppDbContext>();
            membershipDb.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            await membershipDb.SaveChangesAsync();
        }
        using (var foreignList = await SendAsAdminAsync("/api/maintenances", _otherCompanyId))
        {
            Assert.Equal(HttpStatusCode.OK, foreignList.StatusCode);
        using (var payload = JsonDocument.Parse(await foreignList.Content.ReadAsStringAsync()))
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }
        using (var invalidStatus = await SendAsAdminAsync("/api/maintenances?status=Unknown"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        using (var invalidRange = await SendAsAdminAsync($"/api/maintenances?from={plannedOn.AddDays(1):yyyy-MM-dd}&to={plannedOn:yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
        using (var filteredCsv = await SendAsAdminAsync($"/api/maintenances/export.csv?{filter}"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredCsv.StatusCode);
            var csv = await filteredCsv.Content.ReadAsStringAsync();
            Assert.Contains("Bakım Türü", csv, StringComparison.Ordinal);
            Assert.Contains("Inspection", csv, StringComparison.Ordinal);
        }

        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Post,
                   $"/api/assets/{_otherCompanyAssetId}/maintenances/{maintenanceId}/cancel", "admin", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var cancelAsViewer = await SendAsRoleAsync(HttpMethod.Post,
                   $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/cancel", "viewer", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, cancelAsViewer.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var cancelled = await SendAsRoleAsync(HttpMethod.Post,
                   $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/cancel", "admin", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        using (var duplicate = await SendAsRoleAsync(HttpMethod.Post,
                   $"/api/assets/{_ownAssetId}/maintenances/{maintenanceId}/cancel", "admin", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var list = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/maintenances");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listPayload = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var row = Assert.Single(listPayload.RootElement.EnumerateArray());
        Assert.Equal(maintenanceId, row.GetProperty("id").GetGuid());
        Assert.Equal("Cancelled", row.GetProperty("status").GetString());
        using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        using var detailPayload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(AssetStatuses.Available, detailPayload.RootElement.GetProperty("asset").GetProperty("status").GetString());
        Assert.Contains(detailPayload.RootElement.GetProperty("history").EnumerateArray(), item => item.GetProperty("eventType").GetString() == "MaintenanceScheduleCancelled");

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await db.AssetEvents.AsNoTracking().Where(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId).ToListAsync(),
            item => item.EventType == "MaintenanceScheduleCancelled" && item.ActorUserId == _userId);
    }

    [Fact]
    public async Task Activity_history_lists_only_events_from_the_active_company()
    {
        var anchor = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetEvents.AddRange(
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, ActorUserId = _userId,
                    EmployeeId = _ownEmployeeId, EventType = "Created", Details = "ACTIVITY-PAGE-FIRST", OccurredAt = anchor },
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, ActorUserId = _userId,
                    EmployeeId = _ownEmployeeId, EventType = "Assigned", Details = "ACTIVITY-PAGE-SECOND", OccurredAt = anchor.AddMinutes(1) },
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, ActorUserId = _userId,
                    EmployeeId = _ownEmployeeId, EventType = "Transferred", Details = "ACTIVITY-TRANSFER-FILTER", OccurredAt = anchor.AddMinutes(2) });
            await db.SaveChangesAsync();
        }

        using (var firstPage = await SendAsAdminAsync("/api/activity?page=1&pageSize=2"))
        {
            Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
            using var payload = JsonDocument.Parse(await firstPage.Content.ReadAsStringAsync());
            Assert.Equal(4, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("page").GetInt32());
            Assert.Equal(2, payload.RootElement.GetProperty("entries").GetArrayLength());
        }
        using (var secondPage = await SendAsAdminAsync("/api/activity?page=2&pageSize=2"))
        {
            Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
            using var payload = JsonDocument.Parse(await secondPage.Content.ReadAsStringAsync());
            Assert.Equal(4, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(2, payload.RootElement.GetProperty("page").GetInt32());
            Assert.Equal(2, payload.RootElement.GetProperty("entries").GetArrayLength());
            Assert.DoesNotContain("OTHER-AUDIT-REPORT", await secondPage.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        var exactTime = Uri.EscapeDataString(anchor.AddMinutes(2).ToString("O", CultureInfo.InvariantCulture));
        using (var filtered = await SendAsAdminAsync($"/api/activity?eventType=Transferred&q=ACTIVITY-TRANSFER-FILTER&assetId={_ownAssetId}&employeeId={_ownEmployeeId}&from={exactTime}&to={exactTime}"))
        {
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            using var payload = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            var entry = Assert.Single(payload.RootElement.GetProperty("entries").EnumerateArray());
            Assert.Equal(_ownAssetId, entry.GetProperty("assetId").GetGuid());
            Assert.Equal("Transferred", entry.GetProperty("eventType").GetString());
            Assert.Contains("ACTIVITY-TRANSFER-FILTER", entry.GetProperty("details").GetString(), StringComparison.Ordinal);
        }
        using (var invalidRange = await SendAsAdminAsync($"/api/activity?from={exactTime}&to={Uri.EscapeDataString(anchor.ToString("O", CultureInfo.InvariantCulture))}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
        using (var longSearch = await SendAsAdminAsync($"/api/activity?q={new string('x', 121)}"))
            Assert.Equal(HttpStatusCode.BadRequest, longSearch.StatusCode);

        using var crossTenantHistory = await SendAsAdminAsync("/api/activity?q=OTHER-AUDIT-REPORT", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantHistory.StatusCode);
    }

    [Fact]
    public async Task Customer_api_tokens_are_hashed_scoped_tenant_bound_and_revocable()
    {
        using (var apiInfoResponse = await _client!.GetAsync("/api/v1"))
        {
            Assert.Equal(HttpStatusCode.OK, apiInfoResponse.StatusCode);
            using var apiInfo = JsonDocument.Parse(await apiInfoResponse.Content.ReadAsStringAsync());
            var openApiPath = apiInfo.RootElement.GetProperty("openApi").GetString();
            Assert.Equal("/openapi/customer-api-v1.json", openApiPath);
            using var specificationResponse = await _client.GetAsync(openApiPath);
            Assert.Equal(HttpStatusCode.OK, specificationResponse.StatusCode);
            using var specification = JsonDocument.Parse(await specificationResponse.Content.ReadAsStringAsync());
            var root = specification.RootElement;
            Assert.Equal("3.1.2", root.GetProperty("openapi").GetString());
            var paths = root.GetProperty("paths");
            foreach (var resource in new[] { "assets", "accessories", "employees" })
            {
                var operation = paths.GetProperty($"/api/v1/{resource}").GetProperty("get");
                Assert.Equal($"list{char.ToUpperInvariant(resource[0])}{resource[1..]}", operation.GetProperty("operationId").GetString());
                Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("401", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("429", out _));
                Assert.Contains(operation.GetProperty("parameters").EnumerateArray(), parameter =>
                    parameter.GetProperty("$ref").GetString() == "#/components/parameters/PageSize");
            }
            var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("CustomerBearer");
            Assert.Equal("http", bearer.GetProperty("type").GetString());
            Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using var createResponse = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/tokens", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Read-only integration", scopes = new[] { "assets:read" }, expiresAt = (DateTimeOffset?)null }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var tokenId = created.RootElement.GetProperty("id").GetGuid();
        var secret = created.RootElement.GetProperty("secret").GetString()!;
        Assert.StartsWith("etr_live_", secret, StringComparison.Ordinal);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = await db.Employees.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownEmployeeId);
            employee.PrivateNotes = "PRIVATE-HR-SENTINEL";
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedHash = await db.CompanyApiTokens.Where(x => x.Id == tokenId).Select(x => x.TokenHash).SingleAsync();
            Assert.NotEqual(secret, storedHash);
            Assert.Equal(64, storedHash.Length);
        }

        using (var assetsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/assets?page=1&pageSize=20"))
        {
            assetsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
            assetsRequest.Headers.Add("X-Company-Id", _otherCompanyId.ToString());
            using var assetsResponse = await _client!.SendAsync(assetsRequest);
            Assert.Equal(HttpStatusCode.OK, assetsResponse.StatusCode);
            using var assets = JsonDocument.Parse(await assetsResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, assets.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownAssetId, assets.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.DoesNotContain(_otherCompanyAssetId, assets.RootElement.GetProperty("items").EnumerateArray()
                .Select(x => x.GetProperty("id").GetGuid()));
        }

        using (var wrongScopeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/accessories"))
        {
            wrongScopeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
            using var wrongScopeResponse = await _client!.SendAsync(wrongScopeRequest);
            Assert.Equal(HttpStatusCode.Forbidden, wrongScopeResponse.StatusCode);
        }

        using var expandedScopeResponse = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/tokens", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Inventory and people", scopes = new[] { "accessories:read", "people:read" }, expiresAt = (DateTimeOffset?)null }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, expandedScopeResponse.StatusCode);
        using var expandedScopePayload = JsonDocument.Parse(await expandedScopeResponse.Content.ReadAsStringAsync());
        var expandedSecret = expandedScopePayload.RootElement.GetProperty("secret").GetString()!;
        using (var accessoriesRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/accessories"))
        {
            accessoriesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expandedSecret);
            using var accessoriesResponse = await _client!.SendAsync(accessoriesRequest);
            Assert.Equal(HttpStatusCode.OK, accessoriesResponse.StatusCode);
            using var accessories = JsonDocument.Parse(await accessoriesResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, accessories.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownAccessoryId, accessories.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var employeesRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/employees"))
        {
            employeesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expandedSecret);
            using var employeesResponse = await _client!.SendAsync(employeesRequest);
            Assert.Equal(HttpStatusCode.OK, employeesResponse.StatusCode);
            var employeesJson = await employeesResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("PRIVATE-HR-SENTINEL", employeesJson, StringComparison.Ordinal);
            using var employees = JsonDocument.Parse(employeesJson);
            Assert.Equal(1, employees.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownEmployeeId, employees.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using var revokeResponse = await SendAsUserAsync(HttpMethod.Post, $"/api/integrations/tokens/{tokenId}/revoke", _userId, _companyId,
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        using var revokedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/assets");
        revokedRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
        using var revokedResponse = await _client!.SendAsync(revokedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Fact]
    public async Task Asset_reservations_reject_date_overlaps_require_review_permissions_and_stay_in_tenant()
    {
        await using (var setupScope = _factory!.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await setupDb.AssetCategories.Where(x => x.CompanyId == _companyId).Select(x => x.Id).SingleAsync();
            setupDb.Assets.Add(new Asset { Id = _ownSecondAssetId, CompanyId = _companyId, AssetTag = "OWN-RES-2",
                Name = "Reservation fulfillment asset", CategoryId = categoryId });
            await setupDb.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var startsOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var endsOn = startsOn.AddDays(3);
        var reservationBody = System.Net.Http.Json.JsonContent.Create(new
        {
            assetId = _ownAssetId, employeeId = _ownEmployeeId, startsOn, endsOn, requestNote = "Acceptance reservation"
        });

        using var created = await SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin", reservationBody, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdPayload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var reservationId = createdPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Pending", createdPayload.RootElement.GetProperty("status").GetString());

        using var overlapping = await SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, employeeId = _ownEmployeeId, startsOn = startsOn.AddDays(1), endsOn, requestNote = "Overlap" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using var unauthorizedDecision = await SendAsRoleAsync(HttpMethod.Post, $"/api/reservations/{reservationId}/decision", "viewer",
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = (string?)null }), requestToken, antiforgeryCookie);
        Assert.True(unauthorizedDecision.StatusCode == HttpStatusCode.Forbidden,
            $"Read-only reviewer unexpectedly received {(int)unauthorizedDecision.StatusCode}: {await unauthorizedDecision.Content.ReadAsStringAsync()}");

        await SetCompanyRoleAsync("admin");
        using var approved = await SendAsRoleAsync(HttpMethod.Post, $"/api/reservations/{reservationId}/decision", "admin",
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = "=1+1" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var approvedPayload = JsonDocument.Parse(await approved.Content.ReadAsStringAsync());
        Assert.Equal("Approved", approvedPayload.RootElement.GetProperty("status").GetString());

        var followUpStartsOn = startsOn.AddDays(10);
        var followUpEndsOn = followUpStartsOn.AddDays(2);
        using var rejectedRequest = await SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, employeeId = _ownEmployeeId,
                startsOn = followUpStartsOn, endsOn = followUpEndsOn, requestNote = "Reject flow request" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, rejectedRequest.StatusCode);
        using var rejectedRequestPayload = JsonDocument.Parse(await rejectedRequest.Content.ReadAsStringAsync());
        var rejectedReservationId = rejectedRequestPayload.RootElement.GetProperty("id").GetGuid();
        using var rejected = await SendAsRoleAsync(HttpMethod.Post, $"/api/reservations/{rejectedReservationId}/decision", "admin",
            System.Net.Http.Json.JsonContent.Create(new { approve = false, note = "Rejection decision acceptance" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        using (var rejectedList = await SendAsAdminAsync("/api/reservations?status=Rejected&q=OWN-1"))
        {
            Assert.Equal(HttpStatusCode.OK, rejectedList.StatusCode);
            using var rejectedListPayload = JsonDocument.Parse(await rejectedList.Content.ReadAsStringAsync());
            var rejectedRow = Assert.Single(rejectedListPayload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(rejectedReservationId, rejectedRow.GetProperty("id").GetGuid());
            Assert.Equal("Rejection decision acceptance", rejectedRow.GetProperty("reviewNote").GetString());
        }

        using var cancellableRequest = await SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, employeeId = _ownEmployeeId,
                startsOn = followUpStartsOn, endsOn = followUpEndsOn, requestNote = "Requester cancellation acceptance" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, cancellableRequest.StatusCode);
        using var cancellablePayload = JsonDocument.Parse(await cancellableRequest.Content.ReadAsStringAsync());
        var cancellableReservationId = cancellablePayload.RootElement.GetProperty("id").GetGuid();
        using var viewerCsrfResponse = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _viewerUserId, _companyId, role: "viewer");
        Assert.Equal(HttpStatusCode.OK, viewerCsrfResponse.StatusCode);
        using var viewerCsrfPayload = JsonDocument.Parse(await viewerCsrfResponse.Content.ReadAsStringAsync());
        var viewerRequestToken = viewerCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using (var otherRequesterCancel = await SendAsUserAsync(HttpMethod.Post, $"/api/reservations/{cancellableReservationId}/cancel",
                   _viewerUserId, _companyId, requestToken: viewerRequestToken, antiforgeryCookie: antiforgeryCookie, role: "viewer"))
            Assert.True(otherRequesterCancel.StatusCode == HttpStatusCode.Forbidden,
                $"A different requester unexpectedly received {(int)otherRequesterCancel.StatusCode}: {await otherRequesterCancel.Content.ReadAsStringAsync()}");
        using var requesterCsrfResponse = await SendAsRoleAsync(HttpMethod.Get, "/api/session/csrf", "viewer");
        Assert.Equal(HttpStatusCode.OK, requesterCsrfResponse.StatusCode);
        using var requesterCsrfPayload = JsonDocument.Parse(await requesterCsrfResponse.Content.ReadAsStringAsync());
        var requesterRequestToken = requesterCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using (var requesterCancel = await SendAsRoleAsync(HttpMethod.Post, $"/api/reservations/{cancellableReservationId}/cancel",
                   "viewer", requestToken: requesterRequestToken, antiforgeryCookie: antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, requesterCancel.StatusCode);
            using var cancelledPayload = JsonDocument.Parse(await requesterCancel.Content.ReadAsStringAsync());
            Assert.Equal("Cancelled", cancelledPayload.RootElement.GetProperty("status").GetString());
        }

        using (var calendar = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/calendar?kind=reservation&from={startsOn:yyyy-MM-dd}&to={endsOn:yyyy-MM-dd}", "admin"))
        {
            Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
            using var calendarPayload = JsonDocument.Parse(await calendar.Content.ReadAsStringAsync());
            var calendarItem = Assert.Single(calendarPayload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal("reservation", calendarItem.GetProperty("kind").GetString());
            Assert.Equal(reservationId, calendarItem.GetProperty("recordId").GetGuid());
        }
        using (var readReservation = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "reservation", recordId = reservationId, action = "read" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, readReservation.StatusCode);
        using (var reservationState = await SendAsRoleAsync(HttpMethod.Get,
                   $"/api/notifications/states?key={Uri.EscapeDataString($"reservation:{reservationId:D}")}", "admin"))
        {
            using var statePayload = JsonDocument.Parse(await reservationState.Content.ReadAsStringAsync());
            Assert.NotEqual(JsonValueKind.Null, Assert.Single(statePayload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("readAt").ValueKind);
        }

        using (var overlappingAssignment = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/assign", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, expectedReturnOn = endsOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, overlappingAssignment.StatusCode);
        using (var beforeReservation = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/assign", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, expectedReturnOn = startsOn.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, beforeReservation.StatusCode);
        using (var returned = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/return", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { note = "Returned before reserved window" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var immediateEndsOn = today.AddDays(3);
        using var immediateReservation = await SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownSecondAssetId, employeeId = _ownEmployeeId, startsOn = today, endsOn = immediateEndsOn }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, immediateReservation.StatusCode);
        using var immediatePayload = JsonDocument.Parse(await immediateReservation.Content.ReadAsStringAsync());
        var immediateReservationId = immediatePayload.RootElement.GetProperty("id").GetGuid();
        using (var approveImmediate = await SendAsRoleAsync(HttpMethod.Post, $"/api/reservations/{immediateReservationId}/decision", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { approve = true, note = (string?)null }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, approveImmediate.StatusCode);
        using (var fulfillReservation = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownSecondAssetId}/assign", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, expectedReturnOn = immediateEndsOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, fulfillReservation.StatusCode);

        using var history = await SendAsAdminAsync("/api/reservations?status=Approved&q=OWN-1");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyPayload = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var row = Assert.Single(historyPayload.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(reservationId, row.GetProperty("id").GetGuid());
        Assert.Equal(_ownEmployeeId, row.GetProperty("employeeId").GetGuid());

        using (var export = await SendAsAdminAsync($"/api/reservations/export.csv?status=Approved&q=OWN-1&from={startsOn:yyyy-MM-dd}&to={endsOn:yyyy-MM-dd}"))
        {
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            Assert.Contains("text/csv", export.Content.Headers.ContentType?.ToString());
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("OWN-1", csv);
            Assert.Contains("'=1+1", csv);
            Assert.Equal(2, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        }
        using var crossTenant = await SendAsAdminAsync("/api/reservations", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenant.StatusCode);
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(3, await db.AssetReservations.CountAsync(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId));
        Assert.Equal("Completed", await db.AssetReservations.Where(x => x.Id == immediateReservationId).Select(x => x.Status).SingleAsync());
        Assert.Equal(0, await db.AssetReservations.CountAsync(x => x.CompanyId == _otherCompanyId));
        var foreignReservationId = Guid.NewGuid();
        db.AssetReservations.Add(new AssetReservation { Id = foreignReservationId, CompanyId = _otherCompanyId,
            AssetId = _otherCompanyAssetId, EmployeeId = _otherCompanyEmployeeId, RequestedByUserId = _userId,
            StartsOn = today, EndsOn = today.AddDays(1), Status = "Approved" });
        await db.SaveChangesAsync();
        using (var allReservationsExport = await SendAsAdminAsync("/api/reservations/export.csv"))
        {
            Assert.Equal(HttpStatusCode.OK, allReservationsExport.StatusCode);
            var csv = await allReservationsExport.Content.ReadAsStringAsync();
            Assert.Contains("OWN-1", csv);
            Assert.DoesNotContain("OTHER-1", csv);
            Assert.DoesNotContain("FOREIGN-RES", csv);
        }
        using (var foreignNotification = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "reservation", recordId = foreignReservationId, action = "dismiss" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignNotification.StatusCode);
        Assert.Equal(1, await db.AssetReservations.CountAsync(x => x.CompanyId == _otherCompanyId));
    }

    [Fact]
    public async Task Concurrent_overlapping_reservations_for_one_asset_create_only_one_pending_request()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var startsOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(12);
        var endsOn = startsOn.AddDays(4);
        var firstRequest = SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, employeeId = _ownEmployeeId, startsOn, endsOn, requestNote = "Concurrent reservation A" }), requestToken, antiforgeryCookie);
        var secondRequest = SendAsRoleAsync(HttpMethod.Post, "/api/reservations", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, employeeId = _ownEmployeeId, startsOn = startsOn.AddDays(2), endsOn, requestNote = "Concurrent reservation B" }), requestToken, antiforgeryCookie);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;
        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        var responseDetails = $"first={(int)firstResponse.StatusCode} {await firstResponse.Content.ReadAsStringAsync()}; " +
            $"second={(int)secondResponse.StatusCode} {await secondResponse.Content.ReadAsStringAsync()}";
        Assert.True(statuses.Contains(HttpStatusCode.Created), responseDetails);
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Created));
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Conflict));

        using var createdPayload = JsonDocument.Parse(await (firstResponse.StatusCode == HttpStatusCode.Created
            ? firstResponse.Content.ReadAsStringAsync() : secondResponse.Content.ReadAsStringAsync()));
        var reservationId = createdPayload.RootElement.GetProperty("id").GetGuid();
        await using var verificationScope = _factory!.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reservations = await verificationDb.AssetReservations
            .Where(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId && x.StartsOn <= endsOn && x.EndsOn >= startsOn)
            .ToListAsync();
        Assert.Single(reservations);
        Assert.Equal(reservationId, reservations[0].Id);
        Assert.Equal("Pending", reservations[0].Status);
        Assert.Single(await verificationDb.AuditEntries.Where(x => x.CompanyId == _companyId && x.TargetType == "AssetReservation" &&
            x.TargetId == reservationId && x.Action == "ReservationRequested").ToListAsync());
        Assert.Empty(await verificationDb.AssetReservations.Where(x => x.CompanyId == _otherCompanyId).ToListAsync());
    }

    [Fact]
    public async Task Operational_notification_preferences_and_read_dismiss_states_are_private_to_each_user_and_company()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var defaultPreferences = await SendAsRoleAsync(HttpMethod.Get, "/api/notifications/preferences", "viewer"))
        {
            Assert.Equal(HttpStatusCode.OK, defaultPreferences.StatusCode);
            using var payload = JsonDocument.Parse(await defaultPreferences.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "maintenance", "audit", "license", "warranty", "return", "reservation" },
                payload.RootElement.GetProperty("enabledKinds").EnumerateArray().Select(x => x.GetString()).ToArray());
        }
        using (var savedPreferences = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/preferences", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { enabledKinds = new[] { "LICENSE", "warranty" }, emailEnabledKinds = new[] { "LICENSE", "assignment", "asset-life", "reservation" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, savedPreferences.StatusCode);
        using (var savedPreferences = await SendAsRoleAsync(HttpMethod.Get, "/api/notifications/preferences", "viewer"))
        {
            using var payload = JsonDocument.Parse(await savedPreferences.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "license", "warranty" },
                payload.RootElement.GetProperty("enabledKinds").EnumerateArray().Select(x => x.GetString()).ToArray());
            Assert.Equal(new[] { "license", "reservation", "assignment", "asset-life" },
                payload.RootElement.GetProperty("emailEnabledKinds").EnumerateArray().Select(x => x.GetString()).ToArray());
        }
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var emailService = scope.ServiceProvider.GetRequiredService<OperationalNotificationEmailService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.DashboardPreferences.Add(new DashboardPreference
            {
                CompanyId = _companyId,
                UserId = _userId,
                VisibleWidgetsJson = "{\"visibleWidgets\":[],\"widgetOrder\":[],\"language\":\"en\"}"
            });
            var model = new AssetModel { CompanyId = _companyId, Name = "OWN-SERVICE-LIFE", DepreciationMonths = 24 };
            db.AssetModels.Add(model);
            var assetForLifetime = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            assetForLifetime.ModelId = model.Id;
            var lifetimeEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
            assetForLifetime.PurchaseDate = lifetimeEndDate.AddMonths(-24);
            var longLifeModel = new AssetModel { CompanyId = _companyId, Name = "OWN-LONG-LIFE", DepreciationMonths = 3600 };
            db.AssetModels.Add(longLifeModel);
            db.Assets.AddRange(Enumerable.Range(1, 5000).Select(index => new Asset
            {
                CompanyId = _companyId,
                AssetTag = $"LIFE-HISTORY-{index:D5}",
                Name = "Historical lifetime fixture",
                CategoryId = assetForLifetime.CategoryId,
                ModelId = longLifeModel.Id,
                PurchaseDate = lifetimeEndDate.AddDays(20).AddMonths(-3600)
            }));
            db.AssetEvents.Add(new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, EmployeeId = _ownEmployeeId,
                ActorUserId = _userId, EventType = "Assigned", Details = "assignment-digest-fixture", OccurredAt = DateTimeOffset.UtcNow });
            var reservationStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
            var reservationStartLabel = reservationStart.ToDateTime(TimeOnly.MinValue).ToString("d", CultureInfo.GetCultureInfo("en-US"));
            var reservationEndLabel = reservationStart.AddDays(2).ToDateTime(TimeOnly.MinValue).ToString("d", CultureInfo.GetCultureInfo("en-US"));
            db.AssetReservations.Add(new AssetReservation { CompanyId = _companyId, AssetId = _ownAssetId,
                EmployeeId = _ownEmployeeId, RequestedByUserId = _userId, StartsOn = reservationStart,
                EndsOn = reservationStart.AddDays(2), Status = "Approved" });
            await db.SaveChangesAsync();
            await emailService.RunOnceAsync(CancellationToken.None);
            var deliveredDigest = Assert.Single(_smtpCapture.Messages);
            Assert.Contains($"To: acceptance-{_userId:N}@example.invalid", deliveredDigest, StringComparison.OrdinalIgnoreCase);
            var digestBody = DecodeMimeBody(deliveredDigest);
            Assert.Contains("New assignment", digestBody, StringComparison.Ordinal);
            Assert.Contains("OWN-1", digestBody, StringComparison.Ordinal);
            Assert.Contains("OWN-SECURITY-EMPLOYEE", digestBody, StringComparison.Ordinal);
            Assert.Contains("Asset reservation", digestBody, StringComparison.Ordinal);
            Assert.Contains("End of service life", digestBody, StringComparison.Ordinal);
            Assert.Contains("EnvanterTR operations summary", digestBody, StringComparison.Ordinal);
            Assert.Contains("Type", digestBody, StringComparison.Ordinal);
            Assert.Contains($"{reservationStartLabel} – {reservationEndLabel}", digestBody, StringComparison.Ordinal);
            Assert.Contains("OWN-SERVICE-LIFE", digestBody, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-SECURITY-LICENSE", deliveredDigest, StringComparison.Ordinal);
            await emailService.RunOnceAsync(CancellationToken.None);
            Assert.Single(_smtpCapture.Messages);
        }
        using (var otherUserPreferences = await SendAsUserAsync(HttpMethod.Get, "/api/notifications/preferences",
                   _sameCompanyOtherUserId, _companyId))
        {
            using var payload = JsonDocument.Parse(await otherUserPreferences.Content.ReadAsStringAsync());
            Assert.Equal(6, payload.RootElement.GetProperty("enabledKinds").GetArrayLength());
            Assert.Empty(payload.RootElement.GetProperty("emailEnabledKinds").EnumerateArray());
        }
        using (var invalidPreference = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/preferences", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { enabledKinds = new[] { "payroll" } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidPreference.StatusCode);
        using (var assignmentIsNotAInAppKind = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/preferences", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { enabledKinds = new[] { "assignment" }, emailEnabledKinds = Array.Empty<string>() }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, assignmentIsNotAInAppKind.StatusCode);

        var key = $"license:{_ownLicenseId:D}";
        using (var emptyState = await SendAsRoleAsync(HttpMethod.Get, $"/api/notifications/states?key={Uri.EscapeDataString(key)}", "viewer"))
        {
            using var payload = JsonDocument.Parse(await emptyState.Content.ReadAsStringAsync());
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
        }
        using (var dismiss = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "license", recordId = _ownLicenseId, action = "dismiss" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, dismiss.StatusCode);
        using (var savedState = await SendAsRoleAsync(HttpMethod.Get, $"/api/notifications/states?key={Uri.EscapeDataString(key)}", "viewer"))
        {
            using var payload = JsonDocument.Parse(await savedState.Content.ReadAsStringAsync());
            var state = Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray());
            Assert.False(state.GetProperty("readAt").ValueKind == JsonValueKind.Null);
            Assert.False(state.GetProperty("dismissedAt").ValueKind == JsonValueKind.Null);
        }
        using (var otherUserState = await SendAsUserAsync(HttpMethod.Get, $"/api/notifications/states?key={Uri.EscapeDataString(key)}",
                   _sameCompanyOtherUserId, _companyId))
        {
            using var payload = JsonDocument.Parse(await otherUserState.Content.ReadAsStringAsync());
            Assert.Empty(payload.RootElement.GetProperty("items").EnumerateArray());
        }
        using (var crossTenantState = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "license", recordId = _otherCompanyLicenseId, action = "dismiss" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantState.StatusCode);
        using (var restore = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "license", recordId = _ownLicenseId, action = "restore" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        using (var restoredState = await SendAsRoleAsync(HttpMethod.Get, $"/api/notifications/states?key={Uri.EscapeDataString(key)}", "viewer"))
        {
            using var payload = JsonDocument.Parse(await restoredState.Content.ReadAsStringAsync());
            Assert.Null(Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("dismissedAt").GetString());
        }
    }

    [Fact]
    public async Task Asset_hierarchy_is_tenant_scoped_and_rejects_cycles()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId).Select(x => x.CategoryId).SingleAsync();
            db.Assets.Add(new Asset { Id = _ownSecondAssetId, CompanyId = _companyId, AssetTag = "OWN-2", Name = "Own child asset", CategoryId = categoryId });
            await db.SaveChangesAsync();
        }
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using var childDetail = await SendAsAdminAsync($"/api/assets/{_ownSecondAssetId}");
        using var childPayload = JsonDocument.Parse(await childDetail.Content.ReadAsStringAsync());
        var childUpdatedAt = childPayload.RootElement.GetProperty("asset").GetProperty("updatedAt").GetDateTimeOffset();
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownSecondAssetId}/parent", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { parentAssetId = _otherCompanyAssetId, expectedUpdatedAt = childUpdatedAt }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, crossTenant.StatusCode);
        using (var link = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownSecondAssetId}/parent", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { parentAssetId = _ownAssetId, expectedUpdatedAt = childUpdatedAt }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, link.StatusCode);

        using var parentDetail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        using var parentPayload = JsonDocument.Parse(await parentDetail.Content.ReadAsStringAsync());
        var parentUpdatedAt = parentPayload.RootElement.GetProperty("asset").GetProperty("updatedAt").GetDateTimeOffset();
        using (var cycle = await SendAsRoleAsync(HttpMethod.Patch, $"/api/assets/{_ownAssetId}/parent", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { parentAssetId = _ownSecondAssetId, expectedUpdatedAt = parentUpdatedAt }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);

        using var parentWithChildren = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
        using var parentWithChildrenPayload = JsonDocument.Parse(await parentWithChildren.Content.ReadAsStringAsync());
        Assert.Equal("Child", Assert.Single(parentWithChildrenPayload.RootElement.GetProperty("relatedAssets").EnumerateArray()).GetProperty("relationship").GetString());
    }

    [Fact]
    public async Task Asset_csv_import_preserves_parent_links_within_the_same_upload_and_rejects_cycles()
    {
        string categoryName;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
                .Select(x => x.CategoryId).SingleAsync();
            categoryName = await db.AssetCategories.Where(x => x.CompanyId == _companyId && x.Id == categoryId)
                .Select(x => x.Name).SingleAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var suffix = _userId.ToString("N").ToUpperInvariant();
        var rootTag = $"CSV-ROOT-{suffix}";
        var childTag = $"CSV-CHILD-{suffix}";
        var header = "Etiket;Ad;Durum;Kategori;Marka;Model;Seri No;Konum;Personel No;Çalışan;Alış Tarihi;Garanti Bitiş Tarihi;Alış Bedeli;Para Birimi;Üst Varlık Etiketi";
        static string Row(string tag, string name, string category, string? parent) =>
            string.Join(';', new[] { tag, name, AssetStatuses.Available, category, "", "", "", "", "", "", "", "", "", "TRY", parent ?? "" });
        var validCsv = string.Join("\r\n", [header, Row(rootTag, "CSV root", categoryName, null), Row(childTag, "CSV child", categoryName, rootTag)]);
        var cycleCsv = string.Join("\r\n", [header, Row(rootTag, "CSV root", categoryName, childTag), Row(childTag, "CSV child", categoryName, rootTag)]);

        using (var cyclePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(cycleCsv, System.Text.Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, cyclePreview.StatusCode);
            using var payload = JsonDocument.Parse(await cyclePreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(), error => error.GetString()!.Contains("döngü", StringComparison.OrdinalIgnoreCase));
        }

        using (var preview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(validCsv, System.Text.Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            using var payload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
        }

        using (var imported = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import.csv", "admin",
                   new StringContent(validCsv, System.Text.Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, imported.StatusCode);

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var root = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == rootTag);
        var child = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == childTag);
        Assert.Null(root.ParentAssetId);
        Assert.Equal(root.Id, child.ParentAssetId);
    }

    [Fact]
    public async Task Asset_csv_import_maps_reordered_english_headers_and_rejects_unmapped_data()
    {
        string categoryName;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
                .Select(x => x.CategoryId).SingleAsync();
            categoryName = await db.AssetCategories.Where(x => x.CompanyId == _companyId && x.Id == categoryId)
                .Select(x => x.Name).SingleAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var tag = $"MAP-{_userId:N}".ToUpperInvariant();
        var headers = "Serial number,Location,Purchase Cost,Asset Tag,Model,Manufacturer,Category,Name,Status,Purchase Date";
        var row = $"SN-001,\"Lab, Floor 2\",125.50,{tag},,,\"{categoryName}\",Desk,Ready to Deploy,10/8/2026";
        var compatibleCsv = $"{headers}\r\n{row}";
        using (var preview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(compatibleCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            var previewJson = await preview.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(previewJson);
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean(), previewJson);
        }

        using (var imported = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import.csv", "admin",
                   new StringContent(compatibleCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, imported.StatusCode);

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == tag);
        Assert.Equal("SN-001", asset.SerialNumber);
        Assert.Equal("Lab, Floor 2", asset.Location);
        Assert.Equal(125.50m, asset.PurchaseCost);
        Assert.Equal("TRY", asset.Currency);
        Assert.Equal(AssetStatuses.Available, asset.Status);
        Assert.Equal(new DateOnly(2026, 10, 8), asset.PurchaseDate);

        var turkishTag = $"MAP-TR-{_userId:N}".ToUpperInvariant();
        var turkishCsv = "Serial number;Location;Purchase Cost;Asset Tag;Model;Manufacturer;Category;Name;Status;Purchase Date\r\n" +
            $"SN-002;Lab;125,50;{turkishTag};;;\"{categoryName}\";Desk;Ready to Deploy;08.10.2026";
        using (var turkishPreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(turkishCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, turkishPreview.StatusCode);
            var previewJson = await turkishPreview.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(previewJson);
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean(), previewJson);
        }
        using (var turkishImport = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import.csv", "admin",
                   new StringContent(turkishCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, turkishImport.StatusCode);
        var turkishAsset = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == turkishTag);
        Assert.Equal(125.50m, turkishAsset.PurchaseCost);

        var snipeTag = $"SNIPE-{_userId:N}".ToUpperInvariant();
        var companyName = await verifyDb.Companies.Where(x => x.Id == _companyId).Select(x => x.Name).SingleAsync();
        const string snipeNotes = "Rack 2, port 18; warranty checked";
        var snipeCsv = "Asset Tag,Name,Status Label,Category,Manufacturer,Model,Model Number,Serial Number,Location,Default Location,Purchase Date,Purchase Cost,Company,Asset Notes\r\n" +
            $"{snipeTag},Snipe imported device,Ready to Deploy,\"{categoryName}\",,,,SN-SNIPE-001,Lab,,10/8/2026,125.50,\"{companyName}\",\"{snipeNotes}\"";
        using (var snipePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(snipeCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, snipePreview.StatusCode);
            var previewJson = await snipePreview.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(previewJson);
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean(), previewJson);
        }
        using (var snipeImport = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import.csv", "admin",
                   new StringContent(snipeCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.OK, snipeImport.StatusCode);
        var snipeAsset = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == snipeTag);
        Assert.Equal("SN-SNIPE-001", snipeAsset.SerialNumber);
        Assert.Equal("Lab", snipeAsset.Location);
        Assert.Equal(snipeNotes, snipeAsset.Notes);
        Assert.Equal(AssetStatuses.Available, snipeAsset.Status);
        using (var export = await SendAsAdminAsync("/api/assets/export-all.csv?columns=tag,modelNumber,defaultLocation,sourceCompany,notes"))
        {
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("\"Etiket\";\"Model Numarası\";\"Varsayılan Konum\";\"Kaynak Şirket\";\"Notlar\"", csv, StringComparison.Ordinal);
            Assert.Contains(companyName, csv, StringComparison.Ordinal);
            Assert.Contains(snipeNotes, csv, StringComparison.Ordinal);
        }

        var crossCompanyCsv = snipeCsv.Replace($"\"{companyName}\"", "\"Başka şirket\"", StringComparison.Ordinal);
        using (var crossCompanyPreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(crossCompanyCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, crossCompanyPreview.StatusCode);
            using var payload = JsonDocument.Parse(await crossCompanyPreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(), error =>
                error.GetString()!.Contains("başka bir şirkete", StringComparison.OrdinalIgnoreCase));
        }

        var unresolvedAssignmentCsv = "Asset Tag,Name,Category,Checked Out To\r\n" +
            $"UNRESOLVED-{_userId:N},Assigned Snipe asset,\"{categoryName}\",Imported person";
        using (var unresolvedAssignmentPreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(unresolvedAssignmentCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, unresolvedAssignmentPreview.StatusCode);
            using var payload = JsonDocument.Parse(await unresolvedAssignmentPreview.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Contains(payload.RootElement.GetProperty("errors").EnumerateArray(), error =>
                error.GetString()!.Contains("zimmetli çalışan eşleştirilemedi", StringComparison.OrdinalIgnoreCase));
        }

        var unsafeCsv = $"{headers},Unsupported private field\r\n{row},must not be discarded";
        using var unsafePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
            new StringContent(unsafeCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsafePreview.StatusCode);
        Assert.Contains("Unsupported private field", await unsafePreview.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asset_csv_import_accepts_explicit_user_column_mapping_for_preview_and_apply()
    {
        string categoryName;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.Assets.Where(x => x.CompanyId == _companyId && x.Id == _ownAssetId)
                .Select(x => x.CategoryId).SingleAsync();
            categoryName = await db.AssetCategories.Where(x => x.CompanyId == _companyId && x.Id == categoryId)
                .Select(x => x.Name).SingleAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var tag = $"MAPPED-{_userId:N}".ToUpperInvariant();
        const string csv = "Inventory Code,Description,Class\r\n";
        var content = csv + $"{tag},Mapped workstation,{categoryName}";
        var mappings = new Dictionary<string, string>
        {
            ["Inventory Code"] = "assetTag",
            ["Description"] = "name",
            ["Class"] = "category"
        };
        var mappingHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mappings)));

        using (var missingMapping = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(content, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, missingMapping.StatusCode);
            using var payload = JsonDocument.Parse(await missingMapping.Content.ReadAsStringAsync());
            Assert.Equal(3, payload.RootElement.GetProperty("unmappedColumns").GetArrayLength());
            Assert.True(payload.RootElement.GetProperty("mappingTargets").GetArrayLength() >= 16);
        }

        using (var preview = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(content, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
                   new Dictionary<string, string> { ["X-Asset-Import-Mapping"] = mappingHeader }))
        {
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            using var payload = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("isValid").GetBoolean());
        }

        using (var imported = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/assets/import.csv", "admin",
                   new StringContent(content, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
                   new Dictionary<string, string> { ["X-Asset-Import-Mapping"] = mappingHeader }))
            Assert.Equal(HttpStatusCode.OK, imported.StatusCode);

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await verifyDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.AssetTag == tag);
        Assert.Equal("Mapped workstation", asset.Name);
        Assert.Equal(categoryName, await verifyDb.AssetCategories.Where(x => x.Id == asset.CategoryId).Select(x => x.Name).SingleAsync());

        var conflictingCsv = $"Etiket,Ad,Kategori,Inventory Label\r\nCONFLICT-{_userId:N},Existing name,{categoryName},also a label";
        var conflictingMapping = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new Dictionary<string, string> { ["Inventory Label"] = "assetTag" })));
        using (var conflict = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
                   new StringContent(conflictingCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
                   new Dictionary<string, string> { ["X-Asset-Import-Mapping"] = conflictingMapping }))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, conflict.StatusCode);
            using var payload = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
            Assert.Contains(payload.RootElement.GetProperty("unmappedColumns").EnumerateArray(),
                column => column.GetString() == "Inventory Label");
        }

        using var malformedMapping = await SendAsRoleWithHeadersAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
            new StringContent(content, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie,
            new Dictionary<string, string> { ["X-Asset-Import-Mapping"] = "not-base64!" });
        Assert.Equal(HttpStatusCode.BadRequest, malformedMapping.StatusCode);
    }

    [Fact]
    public async Task Software_license_can_be_assigned_to_same_company_asset_and_is_visible_in_both_details()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var assignmentPath = $"/api/software-licenses/{_ownLicenseId}/assign";
        using (var crossCompanyResponse = await SendAsRoleAsync(HttpMethod.Post, assignmentPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new { assetId = _otherCompanyAssetId, notes = "cross-tenant attempt" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, crossCompanyResponse.StatusCode);

        using var assignmentResponse = await SendAsRoleAsync(HttpMethod.Post, assignmentPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, notes = "installed on device" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);
        using var assignmentPayload = JsonDocument.Parse(await assignmentResponse.Content.ReadAsStringAsync());
        var assignmentId = assignmentPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(1, assignmentPayload.RootElement.GetProperty("assigned").GetInt32());

        using (var duplicateResponse = await SendAsRoleAsync(HttpMethod.Post, assignmentPath, "admin",
                   System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using (var licenseResponse = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}"))
        {
            Assert.Equal(HttpStatusCode.OK, licenseResponse.StatusCode);
            using var payload = JsonDocument.Parse(await licenseResponse.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("license").GetProperty("assigned").GetInt32());
            var target = Assert.Single(payload.RootElement.GetProperty("assignments").EnumerateArray());
            Assert.Equal(_ownAssetId, target.GetProperty("assetId").GetGuid());
            Assert.Equal("OWN-1", target.GetProperty("assetTag").GetString());
        }
        using (var assetResponse = await SendAsAdminAsync($"/api/assets/{_ownAssetId}"))
        {
            Assert.Equal(HttpStatusCode.OK, assetResponse.StatusCode);
            using var payload = JsonDocument.Parse(await assetResponse.Content.ReadAsStringAsync());
            var licenses = payload.RootElement.GetProperty("softwareLicenses").EnumerateArray().ToArray();
            Assert.Single(licenses);
            Assert.Equal(_ownLicenseId, licenses[0].GetProperty("licenseId").GetGuid());
        }
        using (var searchResponse = await SendAsAdminAsync("/api/software-license-assignments?q=OWN-1"))
        {
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
            using var payload = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
            Assert.Equal(assignmentId, Assert.Single(payload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        }

        using var revokeResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/software-licenses/{_ownLicenseId}/revoke", "admin",
            System.Net.Http.Json.JsonContent.Create(new { assignmentId }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        using var afterRevokeResponse = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}");
        using var afterRevokePayload = JsonDocument.Parse(await afterRevokeResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, afterRevokePayload.RootElement.GetProperty("license").GetProperty("assigned").GetInt32());
    }

    [Fact]
    public async Task Software_license_bulk_assignment_is_atomic_and_tenant_scoped()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var additionalEmployees = Enumerable.Range(0, 4).Select(_ => new Employee
        {
            CompanyId = _companyId,
            EmployeeNumber = $"BULK-{Guid.NewGuid():N}"[..16],
            DisplayName = $"Bulk License {Guid.NewGuid():N}"[..24]
        }).ToArray();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.AddRange(additionalEmployees);
            var license = await db.SoftwareLicenses.SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownLicenseId);
            license.RequiresEulaAcceptance = true;
            await db.SaveChangesAsync();
        }

        var assignPath = $"/api/software-licenses/{_ownLicenseId}/assign/bulk";
        using (var missingDocumentResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/software-licenses/{_ownLicenseId}/assign", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = additionalEmployees[3].Id, eulaAccepted = true }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, missingDocumentResponse.StatusCode);

        using var eulaUpload = await SendLicenseAttachmentUploadAsync(_ownLicenseId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nEULA version one\n%%EOF"), "eula-v1.pdf", requestToken, antiforgeryCookie,
            isEulaDocument: true, eulaVersion: "1.0");
        Assert.Equal(HttpStatusCode.Created, eulaUpload.StatusCode);
        using var eulaUploadPayload = JsonDocument.Parse(await eulaUpload.Content.ReadAsStringAsync());
        var eulaAttachmentId = eulaUploadPayload.RootElement.GetProperty("id").GetGuid();

        using var missingSingleEulaResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/software-licenses/{_ownLicenseId}/assign", "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = additionalEmployees[3].Id }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, missingSingleEulaResponse.StatusCode);
        using var missingBulkEulaResponse = await SendAsRoleAsync(HttpMethod.Post, assignPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { _ownEmployeeId, additionalEmployees[0].Id } }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, missingBulkEulaResponse.StatusCode);
        using var assignedResponse = await SendAsRoleAsync(HttpMethod.Post, assignPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { _ownEmployeeId, additionalEmployees[0].Id }, notes = "Quarterly rollout", eulaAccepted = true }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, assignedResponse.StatusCode);
        using var assignedPayload = JsonDocument.Parse(await assignedResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, assignedPayload.RootElement.GetProperty("assigned").GetInt32());
        Assert.Equal(2, assignedPayload.RootElement.GetProperty("available").GetInt32());
        Assert.Equal(2, assignedPayload.RootElement.GetProperty("assignments").GetArrayLength());

        using var crossTenantResponse = await SendAsRoleAsync(HttpMethod.Post, assignPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { _otherCompanyEmployeeId }, eulaAccepted = true }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantResponse.StatusCode);

        using var duplicateIdsResponse = await SendAsRoleAsync(HttpMethod.Post, assignPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeIds = new[] { additionalEmployees[1].Id, additionalEmployees[1].Id }, eulaAccepted = true }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateIdsResponse.StatusCode);

        using var overCapacityResponse = await SendAsRoleAsync(HttpMethod.Post, assignPath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeIds = additionalEmployees.Skip(1).Select(employee => employee.Id).ToArray(), eulaAccepted = true }),
            requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Conflict, overCapacityResponse.StatusCode);

        using var detailResponse = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, detail.RootElement.GetProperty("license").GetProperty("assigned").GetInt32());
        Assert.True(detail.RootElement.GetProperty("license").GetProperty("requiresEulaAcceptance").GetBoolean());
        var assignmentEmployees = detail.RootElement.GetProperty("assignments").EnumerateArray()
            .Where(assignment => assignment.GetProperty("revokedAt").ValueKind == JsonValueKind.Null)
            .ToArray();
        Assert.Equal(new HashSet<Guid> { _ownEmployeeId, additionalEmployees[0].Id },
            assignmentEmployees.Select(assignment => assignment.GetProperty("employeeId").GetGuid()).ToHashSet());
        Assert.All(assignmentEmployees, assignment =>
        {
            Assert.NotEqual(JsonValueKind.Null, assignment.GetProperty("eulaAcceptedAt").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(assignment.GetProperty("eulaAcceptedBy").GetString()));
            Assert.Equal("1.0", assignment.GetProperty("eulaVersion").GetString());
            Assert.Equal(eulaAttachmentId, assignment.GetProperty("eulaAttachmentId").GetGuid());
        });

        using var replacementEula = await SendLicenseAttachmentUploadAsync(_ownLicenseId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nEULA version two\n%%EOF"), "eula-v2.pdf", requestToken, antiforgeryCookie,
            isEulaDocument: true, eulaVersion: "2.0");
        Assert.Equal(HttpStatusCode.Created, replacementEula.StatusCode);
        using var oldVersionDelete = await SendAsRoleAsync(HttpMethod.Delete,
            $"/api/software-licenses/{_ownLicenseId}/attachments/{eulaAttachmentId}", "admin", null, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Conflict, oldVersionDelete.StatusCode);
    }

    [Fact]
    public async Task Dashboard_inventory_counts_are_scoped_to_the_selected_company()
    {
        using var response = await SendAsAdminAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var summary = payload.RootElement;
        Assert.Equal(1, summary.GetProperty("totalAssets").GetInt32());
        Assert.Equal(1, summary.GetProperty("softwareLicenses").GetInt32());
        Assert.Equal(1, summary.GetProperty("accessories").GetInt32());
        Assert.Equal(1, summary.GetProperty("consumables").GetInt32());
        Assert.Equal(1, summary.GetProperty("components").GetInt32());
        Assert.Equal(1, summary.GetProperty("activeEmployees").GetInt32());
    }

    [Fact]
    public async Task Employee_list_detail_and_export_are_scoped_to_the_active_company()
    {
        using (var listResponse = await SendAsAdminAsync("/api/employees"))
        {
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            using var payload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Single(items);
            Assert.Equal(_ownEmployeeId, items[0].GetProperty("id").GetGuid());
        }

        using (var ownEmployeeResponse = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}"))
        {
            Assert.Equal(HttpStatusCode.OK, ownEmployeeResponse.StatusCode);
            using var payload = JsonDocument.Parse(await ownEmployeeResponse.Content.ReadAsStringAsync());
            Assert.Equal($"OWN-SECURITY-EMPLOYEE-{_userId:N}",
                payload.RootElement.GetProperty("employee").GetProperty("displayName").GetString());
        }

        using (var otherEmployeeResponse = await SendAsAdminAsync($"/api/employees/{_otherCompanyEmployeeId}"))
            Assert.Equal(HttpStatusCode.NotFound, otherEmployeeResponse.StatusCode);

        using (var filteredResponse = await SendAsAdminAsync($"/api/employees?q={Uri.EscapeDataString($"OWN-SECURITY-EMPLOYEE-{_userId:N}")}&department=Security%20QA&active=true&startedFrom=2020-01-01&startedTo=2020-12-31&page=1&pageSize=1"))
        using (var filteredPayload = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
            Assert.Equal(1, filteredPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(1, filteredPayload.RootElement.GetProperty("items").GetArrayLength());
            Assert.Equal(_ownEmployeeId, filteredPayload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var emptyDepartmentResponse = await SendAsAdminAsync("/api/employees?department=Different%20Department&active=true"))
        using (var emptyDepartmentPayload = JsonDocument.Parse(await emptyDepartmentResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, emptyDepartmentResponse.StatusCode);
            Assert.Equal(0, emptyDepartmentPayload.RootElement.GetProperty("total").GetInt32());
        }
        using (var invalidDateResponse = await SendAsAdminAsync("/api/employees?startedFrom=2020-12-31&startedTo=2020-01-01"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidDateResponse.StatusCode);

        using var exportResponse = await SendAsAdminAsync("/api/employees/export.csv");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var csv = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"Personel No\",\"Ad Soyad\"", csv, StringComparison.Ordinal);
        Assert.Contains($"OWN-SECURITY-EMPLOYEE-{_userId:N}", csv, StringComparison.Ordinal);
        Assert.False(csv.Contains($"OTHER-SECURITY-EMPLOYEE-{_userId:N}", StringComparison.Ordinal));

        using (var filteredExport = await SendAsAdminAsync($"/api/employees/export.csv?q={Uri.EscapeDataString($"OWN-SECURITY-EMPLOYEE-{_userId:N}")}&department=Security%20QA&active=true&startedFrom=2020-01-01&startedTo=2020-12-31"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredExport.StatusCode);
            Assert.Equal("1", filteredExport.Headers.GetValues("X-Total-Count").Single());
            var filteredCsv = await filteredExport.Content.ReadAsStringAsync();
            Assert.Contains($"OWN-SECURITY-EMPLOYEE-{_userId:N}", filteredCsv, StringComparison.Ordinal);
            Assert.DoesNotContain("OTHER-SECURITY-EMPLOYEE-", filteredCsv, StringComparison.Ordinal);
        }

        using var localizedExportResponse = await SendAsAdminAsync("/api/employees/export.csv?language=ru");
        Assert.Equal(HttpStatusCode.OK, localizedExportResponse.StatusCode);
        var localizedCsv = await localizedExportResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"Табельный номер\",\"Имя и фамилия\"", localizedCsv, StringComparison.Ordinal);
        Assert.Contains("\"Активен\"", localizedCsv, StringComparison.Ordinal);
        Assert.False(localizedCsv.Contains($"OTHER-SECURITY-EMPLOYEE-{_userId:N}", StringComparison.Ordinal));

        using var invalidLanguageResponse = await SendAsAdminAsync("/api/employees/export.csv?language=de");
        Assert.Equal(HttpStatusCode.BadRequest, invalidLanguageResponse.StatusCode);
    }

    [Fact]
    public async Task Software_license_attachments_are_validated_audited_and_tenant_scoped()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nlicense agreement\n%%EOF");
        using (var invalidUpload = await SendLicenseAttachmentUploadAsync(_ownLicenseId, new byte[] { 1, 2, 3 }, "fake.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidUpload.StatusCode);

        using var upload = await SendLicenseAttachmentUploadAsync(_ownLicenseId, pdfBytes, "..\\license-agreement.pdf", requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var attachmentId = uploaded.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("license-agreement.pdf", uploaded.RootElement.GetProperty("originalFileName").GetString());

        using (var ownDownload = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}/attachments/{attachmentId}/file"))
        {
            Assert.Equal(HttpStatusCode.OK, ownDownload.StatusCode);
            Assert.Equal("application/pdf", ownDownload.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pdfBytes, await ownDownload.Content.ReadAsByteArrayAsync());
        }
        using (var crossTenantDownload = await SendAsAdminAsync($"/api/software-licenses/{_otherCompanyLicenseId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDownload.StatusCode);

        using (var detail = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal(attachmentId, payload.RootElement.GetProperty("attachments")[0].GetProperty("id").GetGuid());
        }

        await SetCompanyRoleAsync("viewer");
        using (var forbiddenDeleteWithMethod = await SendAsUserAsync(HttpMethod.Delete, $"/api/software-licenses/{_ownLicenseId}/attachments/{attachmentId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenDeleteWithMethod.StatusCode);
        await SetCompanyRoleAsync("admin");

        using (var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/software-licenses/{_ownLicenseId}/attachments/{attachmentId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using (var missing = await SendAsAdminAsync($"/api/software-licenses/{_ownLicenseId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attachmentActions = await auditDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "SoftwareLicense" && entry.TargetId == _ownLicenseId).Select(entry => entry.Action).ToListAsync();
        Assert.Contains("AttachmentAdded", attachmentActions);
        Assert.Contains("AttachmentRemoved", attachmentActions);
    }

    [Fact]
    public void Maintenance_attachment_upload_enforces_request_body_limit()
    {
        var endpoint = _factory!.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/api/maintenances/{id:guid}/attachments" &&
                route.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST", StringComparer.OrdinalIgnoreCase) == true);

        var requestLimit = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>();
        Assert.NotNull(requestLimit);
        Assert.Equal(10 * 1024 * 1024 + 65_536, requestLimit.MaxRequestBodySize);
    }

    [Fact]
    public async Task Kestrel_rejects_a_request_exceeding_the_maintenance_attachment_limit()
    {
        var endpoint = _factory!.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/api/maintenances/{id:guid}/attachments" &&
                route.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST", StringComparer.OrdinalIgnoreCase) == true);
        var requestLimit = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>();
        var maxRequestBodySize = requestLimit?.MaxRequestBodySize
            ?? throw new InvalidOperationException("Maintenance attachment endpoint has no request body limit.");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
        await using var kestrel = builder.Build();
        kestrel.MapPost("/body-limit-probe", async (HttpRequest request, CancellationToken ct) =>
        {
            await request.Body.CopyToAsync(Stream.Null, ct);
            return Results.Ok();
        }).WithMetadata(requestLimit!);

        await kestrel.StartAsync();
        try
        {
            var address = kestrel.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses.Single()
                ?? throw new InvalidOperationException("Kestrel did not publish its loopback address.");
            using var client = new TcpClient(AddressFamily.InterNetwork);
            await client.ConnectAsync(System.Net.IPAddress.Loopback, new Uri(address).Port);
            await using var stream = client.GetStream();
            var requestHeaders = Encoding.ASCII.GetBytes(
                $"POST /body-limit-probe HTTP/1.1\r\nHost: localhost\r\nContent-Length: {maxRequestBodySize + 1}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(requestHeaders);
            await stream.FlushAsync();
            using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var statusLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));

            Assert.StartsWith("HTTP/1.1 413 ", statusLine, StringComparison.Ordinal);
        }
        finally
        {
            await kestrel.StopAsync();
        }
    }

    [Fact]
    public async Task Maintenance_attachments_are_validated_audited_and_included_in_company_archive()
    {
        var maintenanceId = Guid.NewGuid();
        var otherMaintenanceId = Guid.NewGuid();
        var otherAttachmentId = Guid.NewGuid();
        var otherAttachmentBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nother company maintenance file\n%%EOF");
        var otherAttachmentDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), "maintenances",
            otherMaintenanceId.ToString("N"), "attachments");
        Directory.CreateDirectory(otherAttachmentDirectory);
        await File.WriteAllBytesAsync(Path.Combine(otherAttachmentDirectory, $"{otherAttachmentId:N}.pdf"), otherAttachmentBytes);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetMaintenances.AddRange(
                new AssetMaintenance { Id = maintenanceId, CompanyId = _companyId, AssetId = _ownAssetId,
                    MaintenanceType = "Repair", StartedOn = new DateOnly(2026, 10, 1), CompletedOn = new DateOnly(2026, 10, 2),
                    Status = "Completed", Currency = "TRY" },
                new AssetMaintenance { Id = otherMaintenanceId, CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId,
                    MaintenanceType = "Repair", StartedOn = new DateOnly(2026, 10, 1), CompletedOn = new DateOnly(2026, 10, 2),
                    Status = "Completed", Currency = "TRY" });
            db.AssetMaintenanceAttachments.Add(new AssetMaintenanceAttachment { Id = otherAttachmentId, CompanyId = _otherCompanyId,
                MaintenanceId = otherMaintenanceId, OriginalFileName = "other-company.pdf", StorageFileName = $"{otherAttachmentId:N}.pdf",
                ContentType = "application/pdf", Length = otherAttachmentBytes.Length });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nmaintenance receipt\n%%EOF");
        using (var invalid = await SendMaintenanceAttachmentUploadAsync(maintenanceId, new byte[] { 1, 2, 3 }, "fake.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var upload = await SendMaintenanceAttachmentUploadAsync(maintenanceId, pdfBytes, "..\\maintenance-receipt.pdf", requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var attachmentId = uploaded.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("maintenance-receipt.pdf", uploaded.RootElement.GetProperty("originalFileName").GetString());

        using (var list = await SendAsAdminAsync($"/api/maintenances/{maintenanceId}/attachments"))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            Assert.Equal(attachmentId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var download = await SendAsAdminAsync($"/api/maintenances/{maintenanceId}/attachments/{attachmentId}/file"))
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(pdfBytes, await download.Content.ReadAsByteArrayAsync());
        }
        using (var crossTenantList = await SendAsAdminAsync($"/api/maintenances/{otherMaintenanceId}/attachments"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantList.StatusCode);
        using (var crossTenantDownload = await SendAsAdminAsync($"/api/maintenances/{otherMaintenanceId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDownload.StatusCode);
        using (var crossTenantUpload = await SendMaintenanceAttachmentUploadAsync(otherMaintenanceId, pdfBytes, "intrusion.pdf", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantUpload.StatusCode);
        using (var foreignAttachmentOnOwnMaintenance = await SendAsAdminAsync($"/api/maintenances/{maintenanceId}/attachments/{otherAttachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, foreignAttachmentOnOwnMaintenance.StatusCode);
        using (var crossTenantDelete = await SendAsUserAsync(HttpMethod.Delete,
            $"/api/maintenances/{otherMaintenanceId}/attachments/{otherAttachmentId}", _userId, _companyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantDelete.StatusCode);
        Assert.True(File.Exists(Path.Combine(otherAttachmentDirectory, $"{otherAttachmentId:N}.pdf")));
        await using (var verifyScope = _factory.Services.CreateAsyncScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await verifyDb.AssetMaintenanceAttachments.CountAsync(x => x.CompanyId == _otherCompanyId &&
                x.MaintenanceId == otherMaintenanceId && x.Id == otherAttachmentId));
        }

        using (var archiveResponse = await SendAsAdminAsync("/api/company/export.zip"))
        {
            Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
            using var archive = new ZipArchive(new MemoryStream(await archiveResponse.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
            var entry = Assert.Single(archive.Entries, item => item.FullName == $"media/maintenances/{maintenanceId:N}/attachments/{attachmentId:N}.pdf");
            await using var stream = entry.Open();
            using var archivedBytes = new MemoryStream();
            await stream.CopyToAsync(archivedBytes);
            Assert.Equal(pdfBytes, archivedBytes.ToArray());
        }

        await SetCompanyRoleAsync("viewer");
        using (var denied = await SendAsUserAsync(HttpMethod.Delete, $"/api/maintenances/{maintenanceId}/attachments/{attachmentId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var delete = await SendAsUserAsync(HttpMethod.Delete, $"/api/maintenances/{maintenanceId}/attachments/{attachmentId}",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using (var removed = await SendAsAdminAsync($"/api/maintenances/{maintenanceId}/attachments/{attachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);

        await using var auditScope = _factory.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actions = await auditDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "AssetMaintenance" && entry.TargetId == maintenanceId).Select(entry => entry.Action).ToListAsync();
        Assert.Contains("AttachmentAdded", actions);
        Assert.Contains("AttachmentRemoved", actions);
    }

    [Fact]
    public async Task Accessory_photos_are_company_scoped_audited_and_included_in_company_archive()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };
        using (var invalid = await SendAccessoryPhotoUploadAsync(_ownAccessoryId, [1, 2, 3], "fake.png", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (var tooLarge = await SendAccessoryPhotoUploadAsync(_ownAccessoryId, new byte[2 * 1024 * 1024 + 1], "large.png", requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        using var upload = await SendAccessoryPhotoUploadAsync(_ownAccessoryId, pngBytes, "product.png", requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        using (var photo = await SendAsAdminAsync($"/api/accessories/{_ownAccessoryId}/photo"))
        {
            Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
            Assert.Equal("image/png", photo.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pngBytes, await photo.Content.ReadAsByteArrayAsync());
        }
        using (var crossTenant = await SendAsAdminAsync($"/api/accessories/{_otherCompanyAccessoryId}/photo"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        string storedPhoto;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            storedPhoto = await db.Accessories.AsNoTracking().Where(item => item.CompanyId == _companyId && item.Id == _ownAccessoryId)
                .Select(item => item.PhotoFileName!).SingleAsync();
        }
        using (var archiveResponse = await SendAsAdminAsync("/api/company/export.zip"))
        {
            Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
            using var archive = new ZipArchive(new MemoryStream(await archiveResponse.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
            var entry = Assert.Single(archive.Entries, item => item.FullName == $"media/accessories/{_ownAccessoryId:N}/photo/{storedPhoto}");
            await using var stream = entry.Open();
            using var bytes = new MemoryStream();
            await stream.CopyToAsync(bytes);
            Assert.Equal(pngBytes, bytes.ToArray());
        }
        await SetCompanyRoleAsync("viewer");
        using (var denied = await SendAsUserAsync(HttpMethod.Delete, $"/api/accessories/{_ownAccessoryId}/photo",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var remove = await SendAsUserAsync(HttpMethod.Delete, $"/api/accessories/{_ownAccessoryId}/photo",
            _userId, _companyId, requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
        using (var missing = await SendAsAdminAsync($"/api/accessories/{_ownAccessoryId}/photo"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var auditScope = _factory!.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actions = await auditDb.AuditEntries.AsNoTracking().Where(entry => entry.CompanyId == _companyId &&
            entry.TargetType == "Accessory" && entry.TargetId == _ownAccessoryId).Select(entry => entry.Action).ToListAsync();
        Assert.Contains("PhotoUpdated", actions);
        Assert.Contains("PhotoRemoved", actions);
    }

    [Fact]
    public async Task Employee_photo_and_document_storage_are_scoped_to_the_active_company()
    {
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var documentId = Guid.NewGuid();
        var otherCompanyDocumentId = Guid.NewGuid();
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF");
        var ownAssetAttachmentId = Guid.NewGuid();
        var otherCompanyAssetAttachmentId = Guid.NewGuid();
        var ownAssetEventId = Guid.NewGuid();
        var otherCompanyAssetEventId = Guid.NewGuid();
        var assetPdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nasset attachment\n%%EOF");
        var ownPhotoDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "employees", _ownEmployeeId.ToString("N"));
        Directory.CreateDirectory(ownPhotoDirectory);
        await File.WriteAllBytesAsync(Path.Combine(ownPhotoDirectory, "profile.png"), pngSignature);
        var ownDocumentDirectory = Path.Combine(ownPhotoDirectory, "documents");
        Directory.CreateDirectory(ownDocumentDirectory);
        await File.WriteAllBytesAsync(Path.Combine(ownDocumentDirectory, $"{documentId:N}.pdf"), pdfBytes);
        var otherCompanyDocumentDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), "employees", _otherCompanyEmployeeId.ToString("N"), "documents");
        Directory.CreateDirectory(otherCompanyDocumentDirectory);
        await File.WriteAllBytesAsync(Path.Combine(otherCompanyDocumentDirectory, $"{otherCompanyDocumentId:N}.pdf"), pdfBytes);
        var ownAssetDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "assets", _ownAssetId.ToString("N"));
        Directory.CreateDirectory(ownAssetDirectory);
        await File.WriteAllBytesAsync(Path.Combine(ownAssetDirectory, $"{ownAssetAttachmentId:N}.pdf"), assetPdfBytes);
        var otherCompanyAssetDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), "assets", _otherCompanyAssetId.ToString("N"));
        Directory.CreateDirectory(otherCompanyAssetDirectory);
        await File.WriteAllBytesAsync(Path.Combine(otherCompanyAssetDirectory, $"{otherCompanyAssetAttachmentId:N}.pdf"), assetPdfBytes);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.EmployeeDocuments.AddRange(
                new EmployeeDocument { Id = documentId, CompanyId = _companyId, EmployeeId = _ownEmployeeId,
                    OriginalFileName = "own.pdf", StorageFileName = $"{documentId:N}.pdf", ContentType = "application/pdf", Length = pdfBytes.Length },
                new EmployeeDocument { Id = otherCompanyDocumentId, CompanyId = _otherCompanyId, EmployeeId = _otherCompanyEmployeeId,
                    OriginalFileName = "other.pdf", StorageFileName = $"{otherCompanyDocumentId:N}.pdf", ContentType = "application/pdf", Length = pdfBytes.Length });
            db.AssetAttachments.AddRange(
                new AssetAttachment { Id = ownAssetAttachmentId, CompanyId = _companyId, AssetId = _ownAssetId,
                    AssetEventId = ownAssetEventId, OriginalFileName = "asset-own.pdf", StorageFileName = $"{ownAssetAttachmentId:N}.pdf", ContentType = "application/pdf", Length = assetPdfBytes.Length },
                new AssetAttachment { Id = otherCompanyAssetAttachmentId, CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId,
                    OriginalFileName = "asset-other.pdf", StorageFileName = $"{otherCompanyAssetAttachmentId:N}.pdf", ContentType = "application/pdf", Length = assetPdfBytes.Length });
            db.AssetEvents.AddRange(
                new AssetEvent { Id = ownAssetEventId, CompanyId = _companyId, AssetId = _ownAssetId,
                    EventType = "Assigned", Details = "Acceptance assignment" },
                new AssetEvent { Id = otherCompanyAssetEventId, CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId,
                    EventType = "Assigned", Details = "Other company assignment" });
            await db.SaveChangesAsync();
        }

        using (var ownPhotoResponse = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/photo"))
        {
            Assert.Equal(HttpStatusCode.OK, ownPhotoResponse.StatusCode);
            Assert.Equal("image/png", ownPhotoResponse.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pngSignature, await ownPhotoResponse.Content.ReadAsByteArrayAsync());
        }
        using (var crossCompanyPhotoResponse = await SendAsAdminAsync($"/api/employees/{_otherCompanyEmployeeId}/photo"))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyPhotoResponse.StatusCode);

        using (var ownDocumentResponse = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents/{documentId}/file"))
        {
            Assert.Equal(HttpStatusCode.OK, ownDocumentResponse.StatusCode);
            Assert.Equal("application/pdf", ownDocumentResponse.Content.Headers.ContentType?.MediaType);
            Assert.Equal(pdfBytes, await ownDocumentResponse.Content.ReadAsByteArrayAsync());
        }
        await SetCompanyRoleAsync("asset_operator");
        using (var unauthorizedDocumentResponse = await SendAsAdminAsync($"/api/employees/{_ownEmployeeId}/documents/{documentId}/file"))
            Assert.Equal(HttpStatusCode.Forbidden, unauthorizedDocumentResponse.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossCompanyDocumentResponse = await SendAsAdminAsync($"/api/employees/{_otherCompanyEmployeeId}/documents/{otherCompanyDocumentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyDocumentResponse.StatusCode);

        using (var ownAssetFileResponse = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/attachments/{ownAssetAttachmentId}/file"))
        {
            Assert.Equal(HttpStatusCode.OK, ownAssetFileResponse.StatusCode);
            Assert.Equal("application/pdf", ownAssetFileResponse.Content.Headers.ContentType?.MediaType);
            Assert.Equal(assetPdfBytes, await ownAssetFileResponse.Content.ReadAsByteArrayAsync());
        }
        using (var crossCompanyAssetFileResponse = await SendAsAdminAsync($"/api/assets/{_otherCompanyAssetId}/attachments/{otherCompanyAssetAttachmentId}/file"))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyAssetFileResponse.StatusCode);
        await using (var valuationScope = _factory!.Services.CreateAsyncScope())
        {
            var valuationDb = valuationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var valuationAsset = await valuationDb.Assets.SingleAsync(x => x.Id == _ownAssetId);
            var valuationModel = new AssetModel
            {
                CompanyId = _companyId,
                CategoryId = valuationAsset.CategoryId,
                Name = $"VALUATION-{_userId:N}",
                DepreciationMonths = 36,
                DepreciationMinimum = 0m,
                DepreciationMinimumType = "amount",
                DepreciationMethod = "linear"
            };
            valuationDb.AssetModels.Add(valuationModel);
            valuationAsset.ModelId = valuationModel.Id;
            valuationAsset.PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);
            valuationAsset.PurchaseCost = 1200m;
            valuationAsset.Currency = "USD";
            await valuationDb.SaveChangesAsync();
        }
        using (var ownAssetDetail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}"))
        {
            Assert.Equal(HttpStatusCode.OK, ownAssetDetail.StatusCode);
            using var payload = JsonDocument.Parse(await ownAssetDetail.Content.ReadAsStringAsync());
            var valuation = payload.RootElement.GetProperty("valuation");
            Assert.Equal(12, valuation.GetProperty("ageMonths").GetInt32());
            Assert.Equal(36, valuation.GetProperty("depreciationMonths").GetInt32());
            Assert.Equal(24, valuation.GetProperty("remainingMonths").GetInt32());
            Assert.Equal(800m, valuation.GetProperty("currentValue").GetDecimal());
            var movement = payload.RootElement.GetProperty("history").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == ownAssetEventId);
            Assert.Equal(1, movement.GetProperty("attachmentCount").GetInt32());
            Assert.Equal(ownAssetEventId, payload.RootElement.GetProperty("attachments").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == ownAssetAttachmentId).GetProperty("assetEventId").GetGuid());
        }

        using var assetCsrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, assetCsrfResponse.StatusCode);
        using var assetCsrfPayload = JsonDocument.Parse(await assetCsrfResponse.Content.ReadAsStringAsync());
        var assetRequestToken = assetCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var assetAntiforgeryCookie = assetCsrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using (var upload = await SendAssetAttachmentUploadAsync(_ownAssetId, assetPdfBytes,
            "signed-assignment.pdf", assetRequestToken, assetAntiforgeryCookie, ownAssetEventId))
        {
            Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
            using var uploadPayload = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
            var uploadedId = uploadPayload.RootElement.GetProperty("id").GetGuid();
            using var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}");
            using var detailPayload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            var movement = detailPayload.RootElement.GetProperty("history").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == ownAssetEventId);
            Assert.Equal(2, movement.GetProperty("attachmentCount").GetInt32());
            Assert.Equal(ownAssetEventId, detailPayload.RootElement.GetProperty("attachments").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == uploadedId).GetProperty("assetEventId").GetGuid());
        }
        using (var foreignMovement = await SendAssetAttachmentUploadAsync(_ownAssetId, assetPdfBytes,
            "wrong-movement.pdf", assetRequestToken, assetAntiforgeryCookie, otherCompanyAssetEventId))
            Assert.Equal(HttpStatusCode.BadRequest, foreignMovement.StatusCode);
        using (var crossCompanyEmployeeUpload = await SendEmployeeDocumentUploadAsync(_otherCompanyEmployeeId, pdfBytes,
            "intrusion.pdf", assetRequestToken, assetAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyEmployeeUpload.StatusCode);
        using (var crossCompanyEmployeeDelete = await SendAsUserAsync(HttpMethod.Delete,
            $"/api/employees/{_otherCompanyEmployeeId}/documents/{otherCompanyDocumentId}", _userId, _companyId,
            requestToken: assetRequestToken, antiforgeryCookie: assetAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyEmployeeDelete.StatusCode);
        Assert.True(File.Exists(Path.Combine(otherCompanyDocumentDirectory, $"{otherCompanyDocumentId:N}.pdf")));
        await using (var verifyEmployeeScope = _factory.Services.CreateAsyncScope())
        {
            var verifyDb = verifyEmployeeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await verifyDb.EmployeeDocuments.CountAsync(x => x.CompanyId == _otherCompanyId &&
                x.EmployeeId == _otherCompanyEmployeeId && x.Id == otherCompanyDocumentId));
        }
        using (var crossCompanyAssetUpload = await SendAssetAttachmentUploadAsync(_otherCompanyAssetId, pdfBytes,
            "intrusion.pdf", assetRequestToken, assetAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyAssetUpload.StatusCode);
        using (var crossCompanyAssetDelete = await SendAsUserAsync(HttpMethod.Delete,
            $"/api/assets/{_otherCompanyAssetId}/attachments/{otherCompanyAssetAttachmentId}", _userId, _companyId,
            requestToken: assetRequestToken, antiforgeryCookie: assetAntiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossCompanyAssetDelete.StatusCode);
        Assert.True(File.Exists(Path.Combine(otherCompanyAssetDirectory, $"{otherCompanyAssetAttachmentId:N}.pdf")));
        await using (var verifyScope = _factory.Services.CreateAsyncScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await verifyDb.AssetAttachments.CountAsync(x => x.CompanyId == _otherCompanyId &&
                x.AssetId == _otherCompanyAssetId && x.Id == otherCompanyAssetAttachmentId));
        }
    }

    [Fact]
    public async Task Account_profile_photo_enforces_the_two_mib_limit_and_preserves_the_existing_photo()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var maximumPhoto = new byte[2 * 1024 * 1024];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(maximumPhoto, 0);
        using (var uploadResponse = await SendProfilePhotoUploadAsync(maximumPhoto, requestToken!, antiforgeryCookie))
            Assert.True(uploadResponse.StatusCode == HttpStatusCode.OK,
                $"Profile photo upload returned {(int)uploadResponse.StatusCode}: {await uploadResponse.Content.ReadAsStringAsync()}");

        var storedPhotoPath = Path.Combine(_mediaPath, "accounts", _userId.ToString("N"), "profile.png");
        Assert.True(File.Exists(storedPhotoPath));
        Assert.Equal(maximumPhoto, await File.ReadAllBytesAsync(storedPhotoPath));

        var oversizedPhoto = new byte[maximumPhoto.Length + 1];
        maximumPhoto.CopyTo(oversizedPhoto, 0);
        using (var oversizedResponse = await SendProfilePhotoUploadAsync(oversizedPhoto, requestToken!, antiforgeryCookie))
            Assert.True(oversizedResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge);
        Assert.Equal(maximumPhoto, await File.ReadAllBytesAsync(storedPhotoPath));

        using (var invalidImageResponse = await SendProfilePhotoUploadAsync(new byte[] { 1, 2, 3, 4 }, requestToken!, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidImageResponse.StatusCode);
        Assert.Equal(maximumPhoto, await File.ReadAllBytesAsync(storedPhotoPath));

        using var photoResponse = await SendAsAdminAsync("/api/session/photo");
        Assert.Equal(HttpStatusCode.OK, photoResponse.StatusCode);
        Assert.Equal("image/png", photoResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(maximumPhoto, await photoResponse.Content.ReadAsByteArrayAsync());

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/session/photo");
        deleteRequest.Headers.Add("X-Acceptance-User", _userId.ToString());
        deleteRequest.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        deleteRequest.Headers.Add("X-Acceptance-Role", "admin");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", requestToken);
        deleteRequest.Headers.Add("Cookie", antiforgeryCookie);
        using var deleteResponse = await _client!.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.False(File.Exists(storedPhotoPath));
    }

    [Fact]
    public async Task Accessory_filter_options_and_filters_are_company_scoped()
    {
        using var optionsResponse = await SendAsAdminAsync("/api/accessories/filter-options");
        Assert.Equal(HttpStatusCode.OK, optionsResponse.StatusCode);
        using var options = JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var categories = options.RootElement.GetProperty("categories").EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();
        var locations = options.RootElement.GetProperty("locations").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("Klavye", categories);
        Assert.DoesNotContain("Gizli kategori", categories);
        Assert.Contains("İstanbul Ofis", locations);
        Assert.DoesNotContain("Gizli konum", locations);

        using var matching = await SendAsAdminAsync("/api/accessories?category=Klavye&location=%C4%B0stanbul%20Ofis&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, matching.StatusCode);
        using var matchingJson = JsonDocument.Parse(await matching.Content.ReadAsStringAsync());
        Assert.Equal(1, matchingJson.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(_ownAccessoryId, matchingJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());

        using var otherTenant = await SendAsAdminAsync("/api/accessories?category=Gizli%20kategori&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, otherTenant.StatusCode);
        using var otherJson = JsonDocument.Parse(await otherTenant.Content.ReadAsStringAsync());
        Assert.Equal(0, otherJson.RootElement.GetProperty("total").GetInt32());

        Guid lowStockAccessoryId;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var categoryId = await db.AccessoryCategories.Where(item => item.CompanyId == _companyId && item.Name == "Klavye")
                .Select(item => item.Id).SingleAsync();
            var lowStock = new Accessory
            {
                CompanyId = _companyId, CategoryId = categoryId, Category = "Klavye",
                Name = $"=2+2-ACCESSORY-{_userId:N}", Location = "İstanbul Ofis", TotalQuantity = 0, MinimumQuantity = 1
            };
            lowStockAccessoryId = lowStock.Id;
            db.Accessories.Add(lowStock);
            await db.SaveChangesAsync();
        }

        using (var available = await SendAsAdminAsync($"/api/accessories?categoryId={options.RootElement.GetProperty("categories").EnumerateArray().Single(item => item.GetProperty("name").GetString() == "Klavye").GetProperty("id").GetGuid()}&stock=available&page=1&pageSize=1"))
        {
            Assert.Equal(HttpStatusCode.OK, available.StatusCode);
            using var payload = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownAccessoryId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var lowStock = await SendAsAdminAsync($"/api/accessories?category=Klavye&stock=low&q=%3D2%2B2-ACCESSORY-{_userId:N}"))
        {
            Assert.Equal(HttpStatusCode.OK, lowStock.StatusCode);
            using var payload = JsonDocument.Parse(await lowStock.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(lowStockAccessoryId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        var keyboardCategoryId = options.RootElement.GetProperty("categories").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Klavye").GetProperty("id").GetGuid();
        using (var filteredExport = await SendAsAdminAsync($"/api/accessories/export.csv?categoryId={keyboardCategoryId}&stock=low"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredExport.StatusCode);
            Assert.Equal("no-store", filteredExport.Headers.CacheControl?.ToString());
            Assert.Equal("1", filteredExport.Headers.GetValues("X-Total-Count").Single());
            var csv = await filteredExport.Content.ReadAsStringAsync();
            Assert.Contains("'=2+2-ACCESSORY-", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OWN-SECURITY-ACCESSORY-", csv, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Consumable_search_stock_filters_pagination_and_csv_export_match_company_scope()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        Guid lowStockId;
        var formulaName = $"=2+2-CONSUMABLE-{_userId:N}";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lowStock = new Consumable { CompanyId = _companyId, Name = formulaName, TotalQuantity = 0, MinimumQuantity = 2 };
            lowStockId = lowStock.Id;
            db.Consumables.Add(lowStock);
            await db.SaveChangesAsync();
        }

        using (var available = await SendAsAdminAsync($"/api/consumables?q={Uri.EscapeDataString($"OWN-SECURITY-CONSUMABLE-{_userId:N}")}&stock=available&sortBy=remaining&sortDirection=desc&page=1&pageSize=1"))
        {
            Assert.Equal(HttpStatusCode.OK, available.StatusCode);
            using var payload = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownConsumableId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var lowStock = await SendAsAdminAsync($"/api/consumables?q={Uri.EscapeDataString(formulaName)}&stock=low&page=1&pageSize=1"))
        {
            Assert.Equal(HttpStatusCode.OK, lowStock.StatusCode);
            using var payload = JsonDocument.Parse(await lowStock.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(lowStockId, payload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }
        using (var foreignTenantSearch = await SendAsAdminAsync($"/api/consumables?q={Uri.EscapeDataString($"OTHER-SECURITY-CONSUMABLE-{_userId:N}")}"))
        {
            Assert.Equal(HttpStatusCode.OK, foreignTenantSearch.StatusCode);
            using var payload = JsonDocument.Parse(await foreignTenantSearch.Content.ReadAsStringAsync());
            Assert.Equal(0, payload.RootElement.GetProperty("total").GetInt32());
        }
        using (var filteredCsv = await SendAsAdminAsync($"/api/consumables/export.csv?q={Uri.EscapeDataString(formulaName)}&stock=low"))
        {
            Assert.Equal(HttpStatusCode.OK, filteredCsv.StatusCode);
            var csv = await filteredCsv.Content.ReadAsStringAsync();
            Assert.Contains($"'=2+2-CONSUMABLE-{_userId:N}", csv, StringComparison.Ordinal);
            Assert.DoesNotContain("OWN-SECURITY-CONSUMABLE-", csv, StringComparison.Ordinal);
        }

        using (var addStock = await SendAsRoleAsync(HttpMethod.Post, $"/api/consumables/{_ownConsumableId}/stock", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { quantity = 3, notes = "acceptance receipt" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, addStock.StatusCode);
            using var result = JsonDocument.Parse(await addStock.Content.ReadAsStringAsync());
            Assert.Equal(8, result.RootElement.GetProperty("totalQuantity").GetInt32());
            Assert.Equal(8, result.RootElement.GetProperty("remaining").GetInt32());
        }
        using (var checkout = await SendAsRoleAsync(HttpMethod.Post, $"/api/consumables/{_ownConsumableId}/checkout", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, quantity = 3, notes = "acceptance issue" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
            using var result = JsonDocument.Parse(await checkout.Content.ReadAsStringAsync());
            Assert.Equal(5, result.RootElement.GetProperty("remaining").GetInt32());
        }
        using (var overAdjustment = await SendAsRoleAsync(HttpMethod.Post, $"/api/consumables/{_ownConsumableId}/stock", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { quantity = -6, notes = "would undercut issued stock" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, overAdjustment.StatusCode);
        using (var overCheckout = await SendAsRoleAsync(HttpMethod.Post, $"/api/consumables/{_ownConsumableId}/checkout", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, quantity = 6 }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, overCheckout.StatusCode);
        using (var detail = await SendAsAdminAsync($"/api/consumables/{_ownConsumableId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            using var result = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal(8, result.RootElement.GetProperty("item").GetProperty("totalQuantity").GetInt32());
            Assert.Equal(3, result.RootElement.GetProperty("item").GetProperty("issued").GetInt32());
            Assert.Contains(result.RootElement.GetProperty("movements").EnumerateArray(), movement => movement.GetProperty("notes").GetString() == "acceptance issue");
        }
        using (var overlongSearch = await SendAsAdminAsync($"/api/consumables?q={new string('x', 121)}"))
            Assert.Equal(HttpStatusCode.BadRequest, overlongSearch.StatusCode);
    }

    [Fact]
    public async Task Department_hierarchy_can_be_created_moved_and_filtered_by_floor()
    {
        var floorOne = new CompanyLocation { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Acceptance floor 1", NormalizedName = "ACCEPTANCE FLOOR 1" };
        var floorTwo = new CompanyLocation { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Acceptance floor 2", NormalizedName = "ACCEPTANCE FLOOR 2" };
        var parentOne = new CompanyDepartment { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Acceptance parent one", NormalizedName = "ACCEPTANCE PARENT ONE" };
        var parentTwo = new CompanyDepartment { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Acceptance parent two", NormalizedName = "ACCEPTANCE PARENT TWO", LocationCatalogId = floorTwo.Id };
        var child = new CompanyDepartment { Id = Guid.NewGuid(), CompanyId = _companyId, Name = "Acceptance child", NormalizedName = "ACCEPTANCE CHILD", ParentDepartmentId = parentOne.Id, LocationCatalogId = floorOne.Id };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyLocations.AddRange(floorOne, floorTwo);
            db.CompanyDepartments.AddRange(parentOne, parentTwo, child);
            await db.SaveChangesAsync();
        }

        using (var floorResponse = await SendAsAdminAsync($"/api/departments?locationId={floorOne.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, floorResponse.StatusCode);
            using var floorJson = JsonDocument.Parse(await floorResponse.Content.ReadAsStringAsync());
            var item = Assert.Single(floorJson.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(child.Id, item.GetProperty("departmentId").GetGuid());
            Assert.Equal("Acceptance parent one / Acceptance child", item.GetProperty("departmentPath").GetString());
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using var createResponse = await SendAsUserAsync(HttpMethod.Post, "/api/departments", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Acceptance new child", parentDepartmentId = parentOne.Id, locationCatalogId = floorOne.Id }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdPayload = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var createdId = createdPayload.RootElement.GetProperty("id").GetGuid();

        using var updateResponse = await SendAsUserAsync(HttpMethod.Put, $"/api/departments/{createdId}", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Acceptance moved child", parentDepartmentId = parentTwo.Id, locationCatalogId = floorTwo.Id }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using (var floorResponse = await SendAsAdminAsync($"/api/departments?locationId={floorTwo.Id}"))
        {
            using var floorJson = JsonDocument.Parse(await floorResponse.Content.ReadAsStringAsync());
            Assert.Contains(floorJson.RootElement.GetProperty("items").EnumerateArray(), item =>
                item.GetProperty("departmentId").GetGuid() == createdId &&
                item.GetProperty("departmentPath").GetString() == "Acceptance parent two / Acceptance moved child");
        }

        using var cycleResponse = await SendAsUserAsync(HttpMethod.Put, $"/api/departments/{parentTwo.Id}", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = parentTwo.Name, parentDepartmentId = createdId, locationCatalogId = floorTwo.Id }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, cycleResponse.StatusCode);
    }

    [Fact]
    public async Task Company_roles_follow_the_asset_stock_employee_and_settings_policy_matrix()
    {
        var roleMatrix = new[]
        {
            (Role: "owner", AssetRead: true, AssetWrite: true, EmployeeRead: true, EmployeeWrite: true, HrFiles: true, SettingsManage: true),
            (Role: "admin", AssetRead: true, AssetWrite: true, EmployeeRead: true, EmployeeWrite: true, HrFiles: true, SettingsManage: true),
            (Role: "asset_manager", AssetRead: true, AssetWrite: true, EmployeeRead: true, EmployeeWrite: true, HrFiles: false, SettingsManage: false),
            (Role: "asset_operator", AssetRead: true, AssetWrite: true, EmployeeRead: true, EmployeeWrite: false, HrFiles: false, SettingsManage: false),
            (Role: "auditor", AssetRead: true, AssetWrite: false, EmployeeRead: true, EmployeeWrite: false, HrFiles: false, SettingsManage: false),
            (Role: "viewer", AssetRead: true, AssetWrite: false, EmployeeRead: false, EmployeeWrite: false, HrFiles: false, SettingsManage: false),
            (Role: "hr_operator", AssetRead: false, AssetWrite: false, EmployeeRead: true, EmployeeWrite: true, HrFiles: true, SettingsManage: false)
        };

        foreach (var role in roleMatrix)
        {
            await SetCompanyRoleAsync(role.Role);

            using (var companyBranding = await SendAsRoleAsync(HttpMethod.Get, "/api/company/branding", role.Role))
                Assert.Equal(HttpStatusCode.OK, companyBranding.StatusCode);

            using (var platformOverview = await SendAsRoleAsync(HttpMethod.Get, "/api/platform/overview", role.Role))
                Assert.Equal(HttpStatusCode.Forbidden, platformOverview.StatusCode);

            using (var assetList = await SendAsRoleAsync(HttpMethod.Get, "/api/assets?page=1&pageSize=20", role.Role))
                Assert.Equal(role.AssetRead ? HttpStatusCode.OK : HttpStatusCode.Forbidden, assetList.StatusCode);

            using (var assetPreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", role.Role,
                       new StringContent("AssetTag,Name\n", System.Text.Encoding.UTF8, "text/csv")))
                Assert.Equal(role.AssetWrite, assetPreview.StatusCode != HttpStatusCode.Forbidden);

            using (var requestDecision = await SendAsRoleAsync(HttpMethod.Post, $"/api/requests/{Guid.NewGuid()}/decision", role.Role,
                       new StringContent("{}", System.Text.Encoding.UTF8, "application/json")))
                Assert.Equal(role.AssetWrite, requestDecision.StatusCode != HttpStatusCode.Forbidden);

            var stockReadPaths = new[]
            {
                "/api/accessories?page=1&pageSize=20",
                "/api/accessories/filter-options",
                "/api/consumables?page=1&pageSize=20",
                "/api/components?page=1&pageSize=20",
                "/api/software-licenses?page=1&pageSize=20"
            };
            foreach (var path in stockReadPaths)
            {
                using var stockList = await SendAsRoleAsync(HttpMethod.Get, path, role.Role);
                Assert.Equal(role.AssetRead ? HttpStatusCode.OK : HttpStatusCode.Forbidden, stockList.StatusCode);
            }

            var stockImportPreviews = new[]
            {
                "/api/accessories/import/preview.csv",
                "/api/consumables/import/preview.csv",
                "/api/components/import/preview.csv",
                "/api/software-licenses/import/preview.csv"
            };
            foreach (var path in stockImportPreviews)
            {
                using var stockPreview = await SendAsRoleAsync(HttpMethod.Post, path, role.Role,
                    new StringContent("invalid preview payload", System.Text.Encoding.UTF8, "text/csv"));
                Assert.Equal(role.AssetWrite, stockPreview.StatusCode != HttpStatusCode.Forbidden);
            }

            var stockMutationRequests = new[]
            {
                (Path: $"/api/accessories/{_ownAccessoryId}/checkout", Method: HttpMethod.Post),
                (Path: $"/api/consumables/{_ownConsumableId}/checkout", Method: HttpMethod.Post),
                (Path: $"/api/components/{_ownComponentId}/allocate", Method: HttpMethod.Post),
                (Path: "/api/software-licenses", Method: HttpMethod.Post)
            };
            foreach (var request in stockMutationRequests)
            {
                using var mutationResponse = await SendAsRoleAsync(request.Method, request.Path, role.Role,
                    new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
                Assert.Equal(role.AssetWrite, mutationResponse.StatusCode != HttpStatusCode.Forbidden);
            }

            using (var employeeList = await SendAsRoleAsync(HttpMethod.Get, "/api/employees?page=1&pageSize=20", role.Role))
                Assert.Equal(role.EmployeeRead ? HttpStatusCode.OK : HttpStatusCode.Forbidden, employeeList.StatusCode);

            using (var employeePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/employees/import/preview.csv", role.Role,
                       new StringContent("EmployeeNumber,DisplayName\n", System.Text.Encoding.UTF8, "text/csv")))
                Assert.Equal(role.EmployeeWrite, employeePreview.StatusCode != HttpStatusCode.Forbidden);

            using (var privateNotes = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}/private-notes", role.Role))
                Assert.Equal(role.HrFiles ? HttpStatusCode.OK : HttpStatusCode.Forbidden, privateNotes.StatusCode);

            using (var updatePrivateNotes = await SendAsRoleAsync(HttpMethod.Put, $"/api/employees/{_ownEmployeeId}/private-notes", role.Role,
                       new StringContent("{}", System.Text.Encoding.UTF8, "application/json")))
                Assert.Equal(role.HrFiles, updatePrivateNotes.StatusCode != HttpStatusCode.Forbidden);

            using (var employeeDocuments = await SendAsRoleAsync(HttpMethod.Get, $"/api/employees/{_ownEmployeeId}/documents", role.Role))
                Assert.Equal(role.HrFiles ? HttpStatusCode.OK : HttpStatusCode.Forbidden, employeeDocuments.StatusCode);

            using (var uploadEmployeeDocument = await SendAsRoleAsync(HttpMethod.Post, $"/api/employees/{_ownEmployeeId}/documents", role.Role,
                       new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded")))
                Assert.Equal(role.HrFiles, uploadEmployeeDocument.StatusCode != HttpStatusCode.Forbidden);

            using var companyProfile = await SendAsRoleAsync(HttpMethod.Get, "/api/company/profile", role.Role);
            Assert.Equal(role.SettingsManage ? HttpStatusCode.OK : HttpStatusCode.Forbidden, companyProfile.StatusCode);
        }
    }

    [Fact]
    public async Task Company_json_export_returns_a_valid_company_scoped_document()
    {
        var otherTenantUserId = Guid.NewGuid();
        var importTargetCompanyId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Companies.Add(new Company { Id = importTargetCompanyId, Name = "Empty import target", IsActive = true });
            db.CompanyMemberships.Add(new CompanyMembership
            {
                CompanyId = importTargetCompanyId, UserId = _userId, RoleKey = "admin"
            });
            db.Users.Add(new EnvanterUser
            {
                Id = otherTenantUserId,
                UserName = $"acceptance-other-tenant-{otherTenantUserId:N}",
                NormalizedUserName = $"ACCEPTANCE-OTHER-TENANT-{otherTenantUserId:N}".ToUpperInvariant(),
                Email = $"acceptance-other-tenant-{otherTenantUserId:N}@example.invalid",
                NormalizedEmail = $"ACCEPTANCE-OTHER-TENANT-{otherTenantUserId:N}@EXAMPLE.INVALID".ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DisplayName = "Other tenant profile"
            });
            db.CompanyMemberships.Add(new CompanyMembership
            {
                CompanyId = _otherCompanyId, UserId = otherTenantUserId, RoleKey = "admin"
            });
            await db.SaveChangesAsync();
        }

        var ownerPhoto = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x11 };
        var memberPhoto = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        var otherTenantPhoto = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x42 };
        var assetPhoto = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x22 };
        var assetPhotoId = Guid.NewGuid();
        var licenseFile = Encoding.ASCII.GetBytes("%PDF-1.7\nlicense agreement\n%%EOF");
        var licenseFileId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var supplierLogoName = Guid.NewGuid().ToString("N") + ".png";
        var supplierLogo = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x33 };
        var accountMedia = Path.Combine(_mediaPath, "accounts");
        var ownerDirectory = Path.Combine(accountMedia, _userId.ToString("N"));
        var memberDirectory = Path.Combine(accountMedia, _sameCompanyOtherUserId.ToString("N"));
        var otherTenantDirectory = Path.Combine(accountMedia, otherTenantUserId.ToString("N"));
        Directory.CreateDirectory(ownerDirectory);
        Directory.CreateDirectory(memberDirectory);
        Directory.CreateDirectory(otherTenantDirectory);
        await File.WriteAllBytesAsync(Path.Combine(ownerDirectory, "profile.png"), ownerPhoto);
        await File.WriteAllBytesAsync(Path.Combine(memberDirectory, "profile.webp"), memberPhoto);
        await File.WriteAllBytesAsync(Path.Combine(otherTenantDirectory, "profile.jpg"), otherTenantPhoto);
        var assetDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "assets", _ownAssetId.ToString("N"));
        var licenseDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "licenses", _ownLicenseId.ToString("N"), "attachments");
        Directory.CreateDirectory(assetDirectory);
        Directory.CreateDirectory(licenseDirectory);
        await File.WriteAllBytesAsync(Path.Combine(assetDirectory, $"{assetPhotoId:N}.png"), assetPhoto);
        await File.WriteAllBytesAsync(Path.Combine(licenseDirectory, $"{licenseFileId:N}.pdf"), licenseFile);
        var supplierLogoDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "catalog", "suppliers", supplierId.ToString("N"));
        Directory.CreateDirectory(supplierLogoDirectory);
        await File.WriteAllBytesAsync(Path.Combine(supplierLogoDirectory, supplierLogoName), supplierLogo);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupplierCatalogEntries.Add(new SupplierCatalogEntry
            {
                Id = supplierId, CompanyId = _companyId, Name = "Archive supplier", NormalizedName = "ARCHIVE SUPPLIER",
                Email = "supplier@example.invalid", LogoFileName = supplierLogoName
            });
            db.AssetAttachments.Add(new AssetAttachment
            {
                Id = assetPhotoId, CompanyId = _companyId, AssetId = _ownAssetId,
                OriginalFileName = "asset-photo.png", StorageFileName = $"{assetPhotoId:N}.png",
                ContentType = "image/png", Length = assetPhoto.Length
            });
            db.SoftwareLicenseAttachments.Add(new SoftwareLicenseAttachment
            {
                Id = licenseFileId, CompanyId = _companyId, LicenseId = _ownLicenseId,
                OriginalFileName = "license-agreement.pdf", StorageFileName = $"{licenseFileId:N}.pdf",
                ContentType = "application/pdf", Length = licenseFile.Length
            });
            db.AssetMaintenances.Add(new AssetMaintenance
            {
                Id = Guid.NewGuid(), CompanyId = _companyId, AssetId = _ownAssetId,
                MaintenanceType = "Inspection", StartedOn = new DateOnly(2026, 10, 1),
                Status = "InProgress", Currency = "TRY", Supplier = "Archive supplier", SupplierId = supplierId, CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.InventoryRequests.Add(new InventoryRequest
            {
                Id = Guid.NewGuid(), CompanyId = _companyId, EmployeeId = _ownEmployeeId,
                ItemType = "Asset", ItemId = _ownAssetId, Quantity = 1,
                Status = "Pending", CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await SetCompanyRoleAsync("viewer");
        using (var viewerJsonExport = await SendAsRoleAsync(HttpMethod.Get, "/api/company/export.json", "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, viewerJsonExport.StatusCode);
        using (var viewerZipExport = await SendAsRoleAsync(HttpMethod.Get, "/api/company/export.zip", "viewer"))
            Assert.Equal(HttpStatusCode.Forbidden, viewerZipExport.StatusCode);
        await SetCompanyRoleAsync("admin");

        using var response = await SendAsAdminAsync("/api/company/export.json");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Company JSON export returned {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        Assert.Equal("envantertr-company-data", document.RootElement.GetProperty("format").GetString());
        Assert.Equal(_companyId, document.RootElement.GetProperty("company").GetProperty("id").GetGuid());
        Assert.DoesNotContain(_otherCompanyAssetId.ToString(), body, StringComparison.OrdinalIgnoreCase);

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var previewResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/company/import/preview", "admin",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        using var preview = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        Assert.True(preview.RootElement.GetProperty("isValid").GetBoolean());
        var previewCounts = preview.RootElement.GetProperty("counts");
        Assert.Equal(1, previewCounts.GetProperty("inventoryRequests").GetInt32());
        Assert.Equal(1, previewCounts.GetProperty("assetMaintenances").GetInt32());
        Assert.Equal(1, previewCounts.GetProperty("supplierCatalogEntries").GetInt32());

        var invalidMaintenance = body.Replace("\"maintenanceType\":\"Inspection\"", "\"maintenanceType\":\"\"", StringComparison.Ordinal);
        Assert.NotEqual(body, invalidMaintenance);
        using var invalidPreviewResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/company/import/preview", "admin",
            new StringContent(invalidMaintenance, System.Text.Encoding.UTF8, "application/json"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, invalidPreviewResponse.StatusCode);
        using var invalidPreview = JsonDocument.Parse(await invalidPreviewResponse.Content.ReadAsStringAsync());
        Assert.False(invalidPreview.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Contains(invalidPreview.RootElement.GetProperty("errors").EnumerateArray(),
            error => error.GetString()!.Contains("Bakım türü", StringComparison.Ordinal));

        using var archiveResponse = await SendAsAdminAsync("/api/company/export.zip");
        var archiveBytes = await archiveResponse.Content.ReadAsByteArrayAsync();
        Assert.True(archiveResponse.StatusCode == HttpStatusCode.OK,
            $"Company ZIP export returned {(int)archiveResponse.StatusCode}: {System.Text.Encoding.UTF8.GetString(archiveBytes)}");
        using var archive = new ZipArchive(new MemoryStream(archiveBytes), ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName == "company-data.json");
        Assert.Contains(archive.Entries, entry => entry.FullName == "README.txt");
        var supplierLogoEntry = Assert.Single(archive.Entries,
            entry => entry.FullName == $"media/catalog/suppliers/{supplierId:N}/{supplierLogoName}");
        var ownerPhotoEntry = Assert.Single(archive.Entries,
            entry => entry.FullName == $"media/account-profiles/{_userId:N}/profile.png");
        var memberPhotoEntry = Assert.Single(archive.Entries,
            entry => entry.FullName == $"media/account-profiles/{_sameCompanyOtherUserId:N}/profile.webp");
        var assetPhotoEntry = Assert.Single(archive.Entries,
            entry => entry.FullName == $"media/assets/{_ownAssetId:N}/{assetPhotoId:N}.png");
        var licenseFileEntry = Assert.Single(archive.Entries,
            entry => entry.FullName == $"media/licenses/{_ownLicenseId:N}/attachments/{licenseFileId:N}.pdf");
        Assert.DoesNotContain(archive.Entries, entry =>
            entry.FullName.StartsWith($"media/account-profiles/{otherTenantUserId:N}/", StringComparison.Ordinal));
        await using var ownerPhotoStream = ownerPhotoEntry.Open();
        using var ownerPhotoBytes = new MemoryStream();
        await ownerPhotoStream.CopyToAsync(ownerPhotoBytes);
        Assert.Equal(ownerPhoto, ownerPhotoBytes.ToArray());
        await using var memberPhotoStream = memberPhotoEntry.Open();
        using var memberPhotoBytes = new MemoryStream();
        await memberPhotoStream.CopyToAsync(memberPhotoBytes);
        Assert.Equal(memberPhoto, memberPhotoBytes.ToArray());
        await using var assetPhotoStream = assetPhotoEntry.Open();
        using var assetPhotoBytes = new MemoryStream();
        await assetPhotoStream.CopyToAsync(assetPhotoBytes);
        Assert.Equal(assetPhoto, assetPhotoBytes.ToArray());
        await using var licenseFileStream = licenseFileEntry.Open();
        using var licenseFileBytes = new MemoryStream();
        await licenseFileStream.CopyToAsync(licenseFileBytes);
        Assert.Equal(licenseFile, licenseFileBytes.ToArray());
        await using var supplierLogoStream = supplierLogoEntry.Open();
        using var supplierLogoBytes = new MemoryStream();
        await supplierLogoStream.CopyToAsync(supplierLogoBytes);
        Assert.Equal(supplierLogo, supplierLogoBytes.ToArray());

        using var targetCsrfResponse = await SendAsAdminAsync("/api/session/csrf", importTargetCompanyId);
        Assert.Equal(HttpStatusCode.OK, targetCsrfResponse.StatusCode);
        using var targetCsrfPayload = JsonDocument.Parse(await targetCsrfResponse.Content.ReadAsStringAsync());
        var targetRequestToken = targetCsrfPayload.RootElement.GetProperty("requestToken").GetString();
        using var importResponse = await SendAsUserAsync(HttpMethod.Post, "/api/company/import/apply", _userId, importTargetCompanyId,
            new StringContent(body, Encoding.UTF8, "application/json"), targetRequestToken, antiforgeryCookie);
        var importBody = await importResponse.Content.ReadAsStringAsync();
        Assert.True(importResponse.StatusCode == HttpStatusCode.OK,
            $"Company JSON import returned {(int)importResponse.StatusCode}: {importBody}");
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var importedSupplier = await db.SupplierCatalogEntries.SingleAsync(x => x.CompanyId == importTargetCompanyId && x.Name == "Archive supplier");
            Assert.Equal("ARCHIVE SUPPLIER", importedSupplier.NormalizedName);
            Assert.Null(importedSupplier.LogoFileName);
            var importedMaintenance = await db.AssetMaintenances.SingleAsync(x => x.CompanyId == importTargetCompanyId && x.MaintenanceType == "Inspection");
            Assert.Equal(importedSupplier.Id, importedMaintenance.SupplierId);
        }
    }

    [Fact]
    public async Task Stock_purchase_orders_are_tenant_scoped_and_partial_receipts_update_both_stock_families_atomically()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var crossTenantPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            supplier = "Other tenant supplier", orderDate = DateOnly.FromDateTime(DateTime.UtcNow), currency = "TRY",
            items = new[] { new { consumableId = (Guid?)null, componentId = (Guid?)_otherCompanyComponentId, quantity = 1, unitCost = 10m } }
        });
        using var crossTenantCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/stock-orders", "admin", crossTenantPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantCreate.StatusCode);

        var invalidFinancialPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            supplier = "Invalid finance supplier", orderDate = DateOnly.FromDateTime(DateTime.UtcNow), currency = "TRY",
            taxRatePercent = 100.01m, discountAmount = 0m, freightAmount = 0m,
            items = new[] { new { consumableId = (Guid?)_ownConsumableId, componentId = (Guid?)null, quantity = 1, unitCost = 1m } }
        });
        using var invalidFinancialOrder = await SendAsRoleAsync(HttpMethod.Post, "/api/stock-orders", "admin", invalidFinancialPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, invalidFinancialOrder.StatusCode);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var orderNumber = $"ST-{Guid.NewGuid():N}"[..15];
        var createPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            orderNumber, supplier = "=Acceptance supplier", orderDate = today.AddDays(-2), expectedOn = (DateOnly?)today.AddDays(-1),
            currency = "TRY", taxRatePercent = 10m, discountAmount = 20m, freightAmount = 5m, notes = "Mixed stock receipt acceptance",
            items = new object[]
            {
                new { consumableId = (Guid?)_ownConsumableId, componentId = (Guid?)null, quantity = 4, unitCost = 12.5m, notes = "paper" },
                new { consumableId = (Guid?)null, componentId = (Guid?)_ownComponentId, quantity = 3, unitCost = 25m, notes = "memory" }
            }
        });
        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/stock-orders", "admin", createPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var orderId = created.RootElement.GetProperty("id").GetGuid();

        using var fakeInvoice = new MultipartFormDataContent();
        var invalidPdf = new ByteArrayContent("not a pdf"u8.ToArray());
        invalidPdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        fakeInvoice.Add(invalidPdf, "file", "invoice.pdf");
        using var rejectedAttachment = await SendAsRoleAsync(HttpMethod.Post, $"/api/stock-orders/{orderId}/attachments", "admin",
            fakeInvoice, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedAttachment.StatusCode);
        using var validInvoice = new MultipartFormDataContent();
        var invoiceBytes = "%PDF-1.7\nstock invoice"u8.ToArray();
        var invoice = new ByteArrayContent(invoiceBytes);
        invoice.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        validInvoice.Add(invoice, "file", "stock-invoice.pdf");
        using var uploadedAttachment = await SendAsRoleAsync(HttpMethod.Post, $"/api/stock-orders/{orderId}/attachments", "admin",
            validInvoice, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, uploadedAttachment.StatusCode);
        using var attachmentPayload = JsonDocument.Parse(await uploadedAttachment.Content.ReadAsStringAsync());
        var attachmentId = attachmentPayload.RootElement.GetProperty("id").GetGuid();
        using var attachmentList = await SendAsAdminAsync($"/api/stock-orders/{orderId}/attachments");
        Assert.Equal(HttpStatusCode.OK, attachmentList.StatusCode);
        using var attachmentListPayload = JsonDocument.Parse(await attachmentList.Content.ReadAsStringAsync());
        Assert.Equal(attachmentId, Assert.Single(attachmentListPayload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        using var attachmentDownload = await SendAsAdminAsync($"/api/stock-orders/{orderId}/attachments/{attachmentId}/file");
        Assert.Equal(HttpStatusCode.OK, attachmentDownload.StatusCode);
        Assert.Equal(invoiceBytes, await attachmentDownload.Content.ReadAsByteArrayAsync());
        using var crossTenantAttachmentList = await SendAsAdminAsync($"/api/stock-orders/{orderId}/attachments", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantAttachmentList.StatusCode);
        using var companyArchiveResponse = await SendAsAdminAsync("/api/company/export.zip");
        Assert.Equal(HttpStatusCode.OK, companyArchiveResponse.StatusCode);
        using (var archive = new ZipArchive(await companyArchiveResponse.Content.ReadAsStreamAsync(), ZipArchiveMode.Read, leaveOpen: true))
        {
            var archivedInvoice = Assert.Single(archive.Entries,
                entry => entry.FullName == $"media/stock-purchase-orders/{orderId:N}/attachments/{attachmentId:N}.pdf");
            await using var stream = archivedInvoice.Open(); using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes);
            Assert.Equal(invoiceBytes, bytes.ToArray());
        }

        using var exportResponse = await SendAsAdminAsync($"/api/stock-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}&status=Ordered");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Contains("'=Acceptance supplier", await exportResponse.Content.ReadAsStringAsync());
        using var overdueExport = await SendAsAdminAsync($"/api/stock-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}&overdueOnly=true");
        Assert.Equal(HttpStatusCode.OK, overdueExport.StatusCode);
        Assert.Contains(orderNumber, await overdueExport.Content.ReadAsStringAsync());
        using var overdueList = await SendAsAdminAsync($"/api/stock-orders?q={Uri.EscapeDataString(orderNumber)}&overdueOnly=true&page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.OK, overdueList.StatusCode);
        using var overduePayload = JsonDocument.Parse(await overdueList.Content.ReadAsStringAsync());
        Assert.Equal(1, overduePayload.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(120.50m, overduePayload.RootElement.GetProperty("items")[0].GetProperty("totalCost").GetDecimal());
        using var consumableList = await SendAsAdminAsync($"/api/stock-orders?q={Uri.EscapeDataString(orderNumber)}&itemType=Consumable&page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.OK, consumableList.StatusCode);
        using var consumablePayload = JsonDocument.Parse(await consumableList.Content.ReadAsStringAsync());
        Assert.Equal(1, consumablePayload.RootElement.GetProperty("total").GetInt32());
        using var componentExport = await SendAsAdminAsync($"/api/stock-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}&itemType=Component");
        Assert.Equal(HttpStatusCode.OK, componentExport.StatusCode);
        var componentCsv = await componentExport.Content.ReadAsStringAsync();
        Assert.Contains("Bileşen", componentCsv);
        Assert.DoesNotContain("Sarf;", componentCsv);
        Assert.Contains("Ara Toplam;İskonto;Vergi Oranı %;Vergi;Nakliye;Genel Toplam", componentCsv);
        Assert.Contains("125.00;20.00;10.00;10.50;5.00;120.50", componentCsv);
        using var invalidItemType = await SendAsAdminAsync($"/api/stock-orders?q={Uri.EscapeDataString(orderNumber)}&itemType=Invalid");
        Assert.Equal(HttpStatusCode.BadRequest, invalidItemType.StatusCode);
        using var crossTenantExport = await SendAsAdminAsync($"/api/stock-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantExport.StatusCode);
        using var crossTenantDetail = await SendAsAdminAsync($"/api/stock-orders/{orderId}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantDetail.StatusCode);

        using var detailResponse = await SendAsAdminAsync($"/api/stock-orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(125m, detail.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(105m, detail.RootElement.GetProperty("taxableAmount").GetDecimal());
        Assert.Equal(10.50m, detail.RootElement.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(120.50m, detail.RootElement.GetProperty("totalCost").GetDecimal());
        Assert.Equal(20m, detail.RootElement.GetProperty("order").GetProperty("discountAmount").GetDecimal());
        Assert.Equal(12.05m, detail.RootElement.GetProperty("lines").EnumerateArray().Single(line => line.GetProperty("itemType").GetString() == "Consumable").GetProperty("landedUnitCost").GetDecimal());
        Assert.Equal(24.10m, detail.RootElement.GetProperty("lines").EnumerateArray().Single(line => line.GetProperty("itemType").GetString() == "Component").GetProperty("landedUnitCost").GetDecimal());
        var lineIds = detail.RootElement.GetProperty("lines").EnumerateArray().ToDictionary(
            line => line.GetProperty("itemType").GetString()!, line => line.GetProperty("id").GetGuid());
        Assert.Equal(50m, detail.RootElement.GetProperty("lines").EnumerateArray().Single(line => line.GetProperty("itemType").GetString() == "Consumable").GetProperty("totalCost").GetDecimal());
        using var firstReceiptPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            receivedOn = today, notes = "Partial delivery",
            items = new[] { new { lineId = lineIds["Consumable"], quantity = 2 }, new { lineId = lineIds["Component"], quantity = 1 } }
        });
        using var firstReceipt = await SendAsRoleAsync(HttpMethod.Post, $"/api/stock-orders/{orderId}/receive", "admin", firstReceiptPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, firstReceipt.StatusCode);
        using var firstReceiptResult = JsonDocument.Parse(await firstReceipt.Content.ReadAsStringAsync());
        Assert.Equal("PartiallyReceived", firstReceiptResult.RootElement.GetProperty("status").GetString());

        using var secondReceiptPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            receivedOn = today, notes = "Final delivery",
            items = new[] { new { lineId = lineIds["Consumable"], quantity = 2 }, new { lineId = lineIds["Component"], quantity = 2 } }
        });
        using var secondReceipt = await SendAsRoleAsync(HttpMethod.Post, $"/api/stock-orders/{orderId}/receive", "admin", secondReceiptPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, secondReceipt.StatusCode);
        using var secondReceiptResult = JsonDocument.Parse(await secondReceipt.Content.ReadAsStringAsync());
        Assert.Equal("Received", secondReceiptResult.RootElement.GetProperty("status").GetString());
        Assert.Equal(7, secondReceiptResult.RootElement.GetProperty("orderQuantityReceived").GetInt32());

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(9, await db.Consumables.Where(x => x.CompanyId == _companyId && x.Id == _ownConsumableId).Select(x => x.TotalQuantity).SingleAsync());
        Assert.Equal(8, await db.InventoryComponents.Where(x => x.CompanyId == _companyId && x.Id == _ownComponentId).Select(x => x.TotalQuantity).SingleAsync());
        Assert.Equal(12.05m, await db.Consumables.Where(x => x.CompanyId == _companyId && x.Id == _ownConsumableId).Select(x => x.UnitCost).SingleAsync());
        Assert.Equal(24.10m, await db.InventoryComponents.Where(x => x.CompanyId == _companyId && x.Id == _ownComponentId).Select(x => x.UnitCost).SingleAsync());
        Assert.Equal(2, await db.ConsumableMovements.CountAsync(x => x.CompanyId == _companyId && x.ConsumableId == _ownConsumableId && x.MovementType == "Received" && x.Notes != null && x.Notes.Contains("ST-")));
        Assert.Equal(2, await db.ComponentMovements.CountAsync(x => x.CompanyId == _companyId && x.ComponentId == _ownComponentId && x.MovementType == "Received" && x.Notes != null && x.Notes.Contains("ST-")));
        Assert.Equal(1, await db.StockPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == orderId));
        using var deleteAttachment = await SendAsRoleAsync(HttpMethod.Delete, $"/api/stock-orders/{orderId}/attachments/{attachmentId}",
            "admin", null, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteAttachment.StatusCode);
        Assert.Equal(0, await db.StockPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == orderId));

        var accessoryOrderNumber = $"AC-{Guid.NewGuid():N}"[..15];
        using var accessoryOrderPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            orderNumber = accessoryOrderNumber, supplier = "=Accessory finance supplier", orderDate = today.AddDays(-2), expectedOn = today.AddDays(-1), currency = "TRY",
            taxRatePercent = 10m, discountAmount = 5m, freightAmount = 2m,
            items = new[] { new { accessoryId = _ownAccessoryId, quantity = 4, unitCost = 20m } }
        });
        using var accessoryOrderCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-orders", "admin", accessoryOrderPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryOrderCreate.StatusCode);
        using var accessoryOrderCreated = JsonDocument.Parse(await accessoryOrderCreate.Content.ReadAsStringAsync());
        var accessoryOrderId = accessoryOrderCreated.RootElement.GetProperty("id").GetGuid();
        using (var accessoryOrderList = await SendAsAdminAsync($"/api/accessory-orders?q={Uri.EscapeDataString(accessoryOrderNumber)}&status=Ordered&from={today.AddDays(-2):yyyy-MM-dd}&to={today.AddDays(-2):yyyy-MM-dd}&overdueOnly=true&asOf={today:yyyy-MM-dd}&page=1&pageSize=1"))
        {
            Assert.Equal(HttpStatusCode.OK, accessoryOrderList.StatusCode);
            using var listPayload = JsonDocument.Parse(await accessoryOrderList.Content.ReadAsStringAsync());
            var item = Assert.Single(listPayload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(1, listPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(accessoryOrderId, item.GetProperty("id").GetGuid());
            Assert.Equal(80m, item.GetProperty("subtotal").GetDecimal());
        }
        using (var invalidRange = await SendAsAdminAsync($"/api/accessory-orders?from={today.AddDays(1):yyyy-MM-dd}&to={today:yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
        using var invalidAccessoryAttachmentContent = new MultipartFormDataContent();
        var spoofedAccessoryPdf = new ByteArrayContent("not a pdf"u8.ToArray());
        spoofedAccessoryPdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        invalidAccessoryAttachmentContent.Add(spoofedAccessoryPdf, "file", "invoice.pdf");
        using var rejectedAccessoryAttachment = await SendAsRoleAsync(HttpMethod.Post,
            $"/api/accessory-orders/{accessoryOrderId}/attachments", "admin", invalidAccessoryAttachmentContent, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedAccessoryAttachment.StatusCode);

        using var accessoryAttachmentContent = new MultipartFormDataContent();
        var accessoryInvoice = new ByteArrayContent("%PDF-1.7\naccessory invoice"u8.ToArray());
        accessoryInvoice.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        accessoryAttachmentContent.Add(accessoryInvoice, "file", "accessory-invoice.pdf");
        using var accessoryAttachmentResponse = await SendAsRoleAsync(HttpMethod.Post,
            $"/api/accessory-orders/{accessoryOrderId}/attachments", "admin", accessoryAttachmentContent, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryAttachmentResponse.StatusCode);
        using var accessoryAttachmentPayload = JsonDocument.Parse(await accessoryAttachmentResponse.Content.ReadAsStringAsync());
        var accessoryAttachmentId = accessoryAttachmentPayload.RootElement.GetProperty("id").GetGuid();
        using var accessoryAttachmentList = await SendAsAdminAsync($"/api/accessory-orders/{accessoryOrderId}/attachments");
        Assert.Equal(HttpStatusCode.OK, accessoryAttachmentList.StatusCode);
        using var accessoryAttachmentListPayload = JsonDocument.Parse(await accessoryAttachmentList.Content.ReadAsStringAsync());
        Assert.Equal(accessoryAttachmentId, Assert.Single(accessoryAttachmentListPayload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        using var accessoryAttachmentDownload = await SendAsAdminAsync($"/api/accessory-orders/{accessoryOrderId}/attachments/{accessoryAttachmentId}/file");
        Assert.Equal(HttpStatusCode.OK, accessoryAttachmentDownload.StatusCode);
        Assert.Equal("%PDF-1.7\naccessory invoice", await accessoryAttachmentDownload.Content.ReadAsStringAsync());
        using var crossTenantAccessoryAttachmentList = await SendAsAdminAsync($"/api/accessory-orders/{accessoryOrderId}/attachments", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantAccessoryAttachmentList.StatusCode);
        using var accessoryCompanyArchiveResponse = await SendAsAdminAsync("/api/company/export.zip");
        Assert.Equal(HttpStatusCode.OK, accessoryCompanyArchiveResponse.StatusCode);
        using (var accessoryCompanyArchive = new ZipArchive(await accessoryCompanyArchiveResponse.Content.ReadAsStreamAsync(), ZipArchiveMode.Read, leaveOpen: true))
        {
            var archivedAccessoryInvoice = Assert.Single(accessoryCompanyArchive.Entries,
                entry => entry.FullName == $"media/accessory-purchase-orders/{accessoryOrderId:N}/attachments/{accessoryAttachmentId:N}.pdf");
            await using var archivedAccessoryInvoiceStream = archivedAccessoryInvoice.Open();
            using var archivedAccessoryInvoiceBytes = new MemoryStream();
            await archivedAccessoryInvoiceStream.CopyToAsync(archivedAccessoryInvoiceBytes);
            Assert.Equal("%PDF-1.7\naccessory invoice"u8.ToArray(), archivedAccessoryInvoiceBytes.ToArray());
        }
        using var accessoryOrderDetailResponse = await SendAsAdminAsync($"/api/accessory-orders/{accessoryOrderId}");
        Assert.Equal(HttpStatusCode.OK, accessoryOrderDetailResponse.StatusCode);
        using var accessoryOrderDetail = JsonDocument.Parse(await accessoryOrderDetailResponse.Content.ReadAsStringAsync());
        Assert.Equal(80m, accessoryOrderDetail.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(75m, accessoryOrderDetail.RootElement.GetProperty("taxableAmount").GetDecimal());
        Assert.Equal(7.5m, accessoryOrderDetail.RootElement.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(84.5m, accessoryOrderDetail.RootElement.GetProperty("totalCost").GetDecimal());
        Assert.Equal(21.13m, accessoryOrderDetail.RootElement.GetProperty("lines")[0].GetProperty("landedUnitCost").GetDecimal());
        using var accessoryOrderExport = await SendAsAdminAsync($"/api/accessory-orders/export.csv?q={Uri.EscapeDataString(accessoryOrderNumber)}");
        Assert.Equal(HttpStatusCode.OK, accessoryOrderExport.StatusCode);
        var accessoryCsv = await accessoryOrderExport.Content.ReadAsStringAsync();
        Assert.Contains("'=Accessory finance supplier", accessoryCsv, StringComparison.Ordinal);
        Assert.Contains("Ara Toplam;İskonto;Vergi Oranı %;Vergi;Nakliye;Genel Toplam", accessoryCsv);
        Assert.Contains("80.00;5.00;10.00;7.50;2.00;84.50", accessoryCsv);
        var accessoryLineId = accessoryOrderDetail.RootElement.GetProperty("lines")[0].GetProperty("id").GetGuid();
        using var accessoryReceiptPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            receivedOn = today, items = new[] { new { lineId = accessoryLineId, quantity = 4 } }
        });
        using var accessoryReceipt = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-orders/{accessoryOrderId}/receive", "admin",
            accessoryReceiptPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, accessoryReceipt.StatusCode);
        Assert.Equal(21.13m, await db.Accessories.Where(x => x.CompanyId == _companyId && x.Id == _ownAccessoryId).Select(x => x.UnitCost).SingleAsync());
        Assert.Equal(1, await db.AccessoryPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == accessoryOrderId));
        using var deleteAccessoryAttachment = await SendAsRoleAsync(HttpMethod.Delete,
            $"/api/accessory-orders/{accessoryOrderId}/attachments/{accessoryAttachmentId}", "admin", null, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteAccessoryAttachment.StatusCode);
        Assert.Equal(0, await db.AccessoryPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == accessoryOrderId));

        var cancelledOrderNumber = $"AC-CANCEL-{Guid.NewGuid():N}"[..20];
        using var cancelledOrderPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            orderNumber = cancelledOrderNumber, supplier = "Accessory cancellation supplier", orderDate = today, currency = "TRY",
            items = new[] { new { accessoryId = _ownAccessoryId, quantity = 1, unitCost = 1m } }
        });
        using var cancelledOrderCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-orders", "admin", cancelledOrderPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, cancelledOrderCreate.StatusCode);
        using var cancelledOrderResponse = JsonDocument.Parse(await cancelledOrderCreate.Content.ReadAsStringAsync());
        var cancelledOrderId = cancelledOrderResponse.RootElement.GetProperty("id").GetGuid();
        using (var cancel = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-orders/{cancelledOrderId}/cancel", "admin", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        using (var cancelledList = await SendAsAdminAsync($"/api/accessory-orders?q={Uri.EscapeDataString(cancelledOrderNumber)}&status=Cancelled"))
        {
            Assert.Equal(HttpStatusCode.OK, cancelledList.StatusCode);
            using var listPayload = JsonDocument.Parse(await cancelledList.Content.ReadAsStringAsync());
            Assert.Equal(cancelledOrderId, Assert.Single(listPayload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        }
        using (var repeatedCancel = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-orders/{cancelledOrderId}/cancel", "admin", null, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, repeatedCancel.StatusCode);
        using (var receiveCancelled = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-orders/{cancelledOrderId}/receive", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { receivedOn = today, items = new[] { new { lineId = Guid.NewGuid(), quantity = 1 } } }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, receiveCancelled.StatusCode);
    }

    [Fact]
    public async Task Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var orderNumber = $"AT-{Guid.NewGuid():N}"[..15];
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupplierCatalogEntries.Add(new SupplierCatalogEntry { CompanyId = _companyId, Name = "=Acceptance supplier", NormalizedName = "=ACCEPTANCE SUPPLIER" });
            await db.SaveChangesAsync();
        }

        using var activeSuppliersResponse = await SendAsAdminAsync("/api/suppliers");
        Assert.Equal(HttpStatusCode.OK, activeSuppliersResponse.StatusCode);
        using (var activeSuppliers = JsonDocument.Parse(await activeSuppliersResponse.Content.ReadAsStringAsync()))
            Assert.Contains(activeSuppliers.RootElement.GetProperty("items").EnumerateArray(),
                supplier => supplier.GetProperty("name").GetString() == "=Acceptance supplier");

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var crossTenantPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            supplier = "=Acceptance supplier", orderDate = today, currency = "TRY",
            items = new[] { new { name = "Foreign asset", categoryId = Guid.NewGuid(), brandId = (Guid?)null, modelId = (Guid?)null, quantity = 1, unitCost = 10m } }
        });
        using var crossTenantCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/asset-orders", "admin", crossTenantPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, crossTenantCreate.StatusCode);

        var createPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            orderNumber, supplier = "=Acceptance supplier", orderDate = today.AddDays(-1), expectedOn = (DateOnly?)today,
            currency = "TRY", taxRatePercent = 20m, discountAmount = 1500m, freightAmount = 600m, notes = "Purchase order acceptance",
            items = new[] { new { name = "Test laptop", categoryId = (await GetOwnCategoryIdAsync()), brandId = (Guid?)null, modelId = (Guid?)null, quantity = 3, unitCost = 12500m, warrantyExpiresAt = (DateOnly?)today.AddYears(2) } }
        });
        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/asset-orders", "admin", createPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var orderId = created.RootElement.GetProperty("id").GetGuid();

        using var badAttachmentContent = new MultipartFormDataContent();
        var spoofedPdf = new ByteArrayContent("not a pdf"u8.ToArray());
        spoofedPdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        badAttachmentContent.Add(spoofedPdf, "file", "invoice.pdf");
        using var rejectedAttachment = await SendAsRoleAsync(HttpMethod.Post, $"/api/asset-orders/{orderId}/attachments", "admin",
            badAttachmentContent, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedAttachment.StatusCode);

        using var attachmentContent = new MultipartFormDataContent();
        var invoicePdf = new ByteArrayContent("%PDF-1.7\nacceptance invoice"u8.ToArray());
        invoicePdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        attachmentContent.Add(invoicePdf, "file", "invoice.pdf");
        using var attachmentResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/asset-orders/{orderId}/attachments", "admin",
            attachmentContent, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, attachmentResponse.StatusCode);
        using var attachmentPayload = JsonDocument.Parse(await attachmentResponse.Content.ReadAsStringAsync());
        var attachmentId = attachmentPayload.RootElement.GetProperty("id").GetGuid();
        using var attachmentList = await SendAsAdminAsync($"/api/asset-orders/{orderId}/attachments");
        Assert.Equal(HttpStatusCode.OK, attachmentList.StatusCode);
        using var attachmentListPayload = JsonDocument.Parse(await attachmentList.Content.ReadAsStringAsync());
        Assert.Equal(attachmentId, Assert.Single(attachmentListPayload.RootElement.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        using var attachmentDownload = await SendAsAdminAsync($"/api/asset-orders/{orderId}/attachments/{attachmentId}/file");
        Assert.Equal(HttpStatusCode.OK, attachmentDownload.StatusCode);
        Assert.Equal("%PDF-1.7\nacceptance invoice", await attachmentDownload.Content.ReadAsStringAsync());
        using var crossTenantAttachmentList = await SendAsAdminAsync($"/api/asset-orders/{orderId}/attachments", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantAttachmentList.StatusCode);
        using var companyArchiveResponse = await SendAsAdminAsync("/api/company/export.zip");
        Assert.Equal(HttpStatusCode.OK, companyArchiveResponse.StatusCode);
        using (var companyArchive = new ZipArchive(await companyArchiveResponse.Content.ReadAsStreamAsync(), ZipArchiveMode.Read, leaveOpen: true))
        {
            var archivedInvoice = Assert.Single(companyArchive.Entries,
                entry => entry.FullName == $"media/asset-purchase-orders/{orderId:N}/attachments/{attachmentId:N}.pdf");
            await using var archivedInvoiceStream = archivedInvoice.Open();
            using var archivedInvoiceBytes = new MemoryStream();
            await archivedInvoiceStream.CopyToAsync(archivedInvoiceBytes);
            Assert.Equal("%PDF-1.7\nacceptance invoice"u8.ToArray(), archivedInvoiceBytes.ToArray());
        }

        using var csvResponse = await SendAsAdminAsync($"/api/asset-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}&status=Ordered&from={today.AddDays(-2):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains(orderNumber, csv);
        Assert.Contains("'=Acceptance supplier", csv);
        Assert.Contains("37500.00;1500.00;20.00;7200.00;600.00;43800.00", csv);
        using var crossTenantExport = await SendAsAdminAsync($"/api/asset-orders/export.csv?q={Uri.EscapeDataString(orderNumber)}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantExport.StatusCode);
        using var crossTenantDetail = await SendAsAdminAsync($"/api/asset-orders/{orderId}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantDetail.StatusCode);

        using var detailResponse = await SendAsAdminAsync($"/api/asset-orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(37500m, detail.RootElement.GetProperty("subtotal").GetDecimal());
        Assert.Equal(36000m, detail.RootElement.GetProperty("taxableAmount").GetDecimal());
        Assert.Equal(7200m, detail.RootElement.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(43800m, detail.RootElement.GetProperty("totalCost").GetDecimal());
        Assert.Equal(14600m, Assert.Single(detail.RootElement.GetProperty("lines").EnumerateArray()).GetProperty("landedUnitCost").GetDecimal());
        var lineId = Assert.Single(detail.RootElement.GetProperty("lines").EnumerateArray()).GetProperty("id").GetGuid();
        var receiptPayload = System.Net.Http.Json.JsonContent.Create(new
        {
            receivedOn = today, notes = "Partial device shipment", items = new[] { new { lineId, quantity = 2 } }
        });
        using var receiptResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/asset-orders/{orderId}/receive", "admin", receiptPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, receiptResponse.StatusCode);
        using var receiptResult = JsonDocument.Parse(await receiptResponse.Content.ReadAsStringAsync());
        Assert.Equal("PartiallyReceived", receiptResult.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, receiptResult.RootElement.GetProperty("receivedQuantity").GetInt32());

        using var receivedOrderResponse = await SendAsAdminAsync($"/api/asset-orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, receivedOrderResponse.StatusCode);
        using var receivedOrder = JsonDocument.Parse(await receivedOrderResponse.Content.ReadAsStringAsync());
        var receivedAssets = receivedOrder.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        Assert.Equal(2, receivedAssets.Length);
        var receivedAssetId = receivedAssets[0].GetProperty("id").GetGuid();
        using var assetProfileResponse = await SendAsAdminAsync($"/api/assets/{receivedAssetId}");
        Assert.Equal(HttpStatusCode.OK, assetProfileResponse.StatusCode);
        using var assetProfile = JsonDocument.Parse(await assetProfileResponse.Content.ReadAsStringAsync());
        Assert.Equal(orderId, assetProfile.RootElement.GetProperty("purchaseOrder").GetProperty("id").GetGuid());
        Assert.Equal(14600m, assetProfile.RootElement.GetProperty("asset").GetProperty("purchaseCost").GetDecimal());
        using var crossTenantAssetProfile = await SendAsAdminAsync($"/api/assets/{receivedAssetId}", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantAssetProfile.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await verifyDb.Assets.CountAsync(x => x.CompanyId == _companyId && x.Name == "Test laptop" && x.PurchaseOrderId == orderId));
        Assert.Equal(2, await verifyDb.AssetEvents.CountAsync(x => x.CompanyId == _companyId && x.EventType == "Purchased" && x.Details != null && x.Details.Contains(orderNumber)));
        Assert.Equal(1, await verifyDb.AssetPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == orderId));
        using var deleteAttachment = await SendAsRoleAsync(HttpMethod.Delete,
            $"/api/asset-orders/{orderId}/attachments/{attachmentId}", "admin", null, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteAttachment.StatusCode);
        Assert.Equal(0, await verifyDb.AssetPurchaseOrderAttachments.CountAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == orderId));
    }

    private async Task<Guid> GetOwnCategoryIdAsync()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AssetCategories.Where(x => x.CompanyId == _companyId).Select(x => x.Id).SingleAsync();
    }

    [Fact]
    public async Task Asset_purchase_report_is_company_scoped_searchable_and_exports_formula_safe_csv()
    {
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var asset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            asset.Name = "=PURCHASE-REPORT formula";
            asset.PurchaseDate = purchaseDate;
            asset.PurchaseCost = 1234.56m;
            asset.Currency = "TRY";
            await db.SaveChangesAsync();
        }

        using var reportResponse = await SendAsAdminAsync($"/api/reports/assets/purchases?q=PURCHASE-REPORT&from={purchaseDate.AddDays(-1):yyyy-MM-dd}&to={purchaseDate.AddDays(1):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        using var report = JsonDocument.Parse(await reportResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, report.RootElement.GetProperty("total").GetInt32());
        var item = report.RootElement.GetProperty("items")[0];
        Assert.Equal(_ownAssetId, item.GetProperty("id").GetGuid());
        Assert.Equal(1234.56m, item.GetProperty("purchaseCost").GetDecimal());

        using var exportResponse = await SendAsAdminAsync($"/api/reports/assets/purchases/export.csv?q=PURCHASE-REPORT&from={purchaseDate:yyyy-MM-dd}&to={purchaseDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var csv = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("'=PURCHASE-REPORT formula", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("OTHER-1", csv, StringComparison.Ordinal);

        using var crossTenant = await SendAsAdminAsync("/api/reports/assets/purchases?q=PURCHASE-REPORT", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenant.StatusCode);
        using var invalidRange = await SendAsAdminAsync($"/api/reports/assets/purchases?from={purchaseDate:yyyy-MM-dd}&to={purchaseDate.AddDays(-1):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
    }

    [Fact]
    public async Task Pending_delivery_report_unions_order_types_and_keeps_rows_company_scoped()
    {
        var order = new AccessoryPurchaseOrder
        {
            CompanyId = _companyId,
            OrderNumber = $"PENDING-{_userId:N}",
            Supplier = "Own supplier",
            Status = "PartiallyReceived",
            OrderDate = new DateOnly(2026, 10, 1),
            ExpectedOn = new DateOnly(2026, 10, 20),
            Currency = "TRY",
            CreatedByUserId = _userId
        };
        var otherOrder = new AccessoryPurchaseOrder
        {
            CompanyId = _otherCompanyId,
            OrderNumber = $"OTHER-PENDING-{_userId:N}",
            Supplier = "Other tenant supplier",
            Status = "PartiallyReceived",
            OrderDate = new DateOnly(2026, 10, 1),
            ExpectedOn = new DateOnly(2026, 10, 20),
            Currency = "TRY",
            CreatedByUserId = _userId
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AccessoryPurchaseOrders.AddRange(order, otherOrder);
            db.AccessoryPurchaseOrderLines.AddRange(new AccessoryPurchaseOrderLine
            {
                CompanyId = _companyId, PurchaseOrderId = order.Id, AccessoryId = _ownAccessoryId,
                Quantity = 8, QuantityReceived = 3, UnitCost = 125m
            }, new AccessoryPurchaseOrderLine
            {
                CompanyId = _otherCompanyId, PurchaseOrderId = otherOrder.Id, AccessoryId = _otherCompanyAccessoryId,
                Quantity = 9, QuantityReceived = 2, UnitCost = 200m
            });
            await db.SaveChangesAsync();
        }

        using var response = await SendAsAdminAsync("/api/reports/pending-deliveries?state=all&asOf=2026-10-10&page=1&pageSize=50");
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {responseBody}");
        using var payload = JsonDocument.Parse(responseBody);
        Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
        var item = payload.RootElement.GetProperty("items")[0];
        Assert.Equal("Aksesuar", item.GetProperty("orderType").GetString());
        Assert.Equal(order.OrderNumber, item.GetProperty("orderNumber").GetString());
        Assert.Equal(5, item.GetProperty("remaining").GetInt32());
        Assert.Equal("OWN-SECURITY-ACCESSORY-" + _userId.ToString("N"), item.GetProperty("itemName").GetString());
        Assert.DoesNotContain(otherOrder.OrderNumber, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Other tenant supplier", responseBody, StringComparison.Ordinal);

        using var export = await SendAsAdminAsync($"/api/reports/pending-deliveries/export.csv?q={order.OrderNumber}&asOf=2026-10-10");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var csv = await export.Content.ReadAsStringAsync();
        Assert.Contains(order.OrderNumber, csv, StringComparison.Ordinal);
        Assert.Contains("Own supplier", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(otherOrder.OrderNumber, csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Other tenant supplier", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asset_audit_schedule_is_tenant_scoped_and_filters_overdue_upcoming_and_search()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var through = today.AddDays(30);
        var categoryId = await GetOwnCategoryIdAsync();
        var dueAsset = new Asset
        {
            CompanyId = _companyId, AssetTag = $"OWN-AUDIT-DUE-{_userId:N}", Name = "Audit due next week",
            CategoryId = categoryId, Location = "North office", NextAuditOn = today.AddDays(7)
        };
        var laterAsset = new Asset
        {
            CompanyId = _companyId, AssetTag = $"OWN-AUDIT-LATER-{_userId:N}", Name = "Audit due later",
            CategoryId = categoryId, NextAuditOn = through.AddDays(1)
        };
        var unscheduledAsset = new Asset
        {
            CompanyId = _companyId, AssetTag = $"OWN-AUDIT-NONE-{_userId:N}", Name = "Audit plan missing",
            CategoryId = categoryId
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var overdueAsset = await db.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
            overdueAsset.NextAuditOn = today.AddDays(-1);
            var otherTenantAsset = await db.Assets.SingleAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyAssetId);
            otherTenantAsset.NextAuditOn = today.AddDays(1);
            db.Assets.AddRange(dueAsset, laterAsset, unscheduledAsset);
            await db.SaveChangesAsync();
        }

        using (var response = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=all"))
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {body}");
            using var payload = JsonDocument.Parse(body);
            Assert.Equal(2, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("overdue").GetInt32());
            Assert.Equal(1, payload.RootElement.GetProperty("dueSoon").GetInt32());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == _ownAssetId);
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == dueAsset.Id);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == laterAsset.Id);
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == _otherCompanyAssetId);
            Assert.DoesNotContain("OTHER-1", body, StringComparison.Ordinal);
        }

        using (var overdue = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=overdue"))
        using (var overduePayload = JsonDocument.Parse(await overdue.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, overdue.StatusCode);
            Assert.Equal(1, overduePayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(_ownAssetId, overduePayload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var upcoming = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=upcoming"))
        using (var upcomingPayload = JsonDocument.Parse(await upcoming.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, upcoming.StatusCode);
            Assert.Equal(1, upcomingPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(dueAsset.Id, upcomingPayload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var search = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=all&q={Uri.EscapeDataString("due next week")}"))
        using (var searchPayload = JsonDocument.Parse(await search.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            Assert.Equal(1, searchPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(dueAsset.Id, searchPayload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var unscheduled = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=unscheduled"))
        using (var unscheduledPayload = JsonDocument.Parse(await unscheduled.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, unscheduled.StatusCode);
            Assert.Equal(1, unscheduledPayload.RootElement.GetProperty("unscheduled").GetInt32());
            Assert.Equal(1, unscheduledPayload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(unscheduledAsset.Id, unscheduledPayload.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.DoesNotContain(unscheduledPayload.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == _otherCompanyAssetId);
        }

        using (var invalidState = await SendAsAdminAsync($"/api/assets/audit-schedule?through={through:yyyy-MM-dd}&state=other"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidState.StatusCode);
        using (var invalidRange = await SendAsAdminAsync($"/api/assets/audit-schedule?through={today.AddDays(367):yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
    }

    [Fact]
    public async Task Asset_physical_audit_submission_is_authorized_validated_tenant_scoped_and_written_to_history()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextAuditOn = today.AddDays(21);
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var validRequest = new { auditDate = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            notes = "Physical condition checked", nextAuditOn = nextAuditOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) };

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(validRequest)))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "viewer",
                   System.Net.Http.Json.JsonContent.Create(validRequest), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");
        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_otherCompanyAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(validRequest), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var futureAudit = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { auditDate = today.AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), notes = "Future" }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, futureAudit.StatusCode);
        using (var pastSchedule = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { auditDate = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                       nextAuditOn = today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, pastSchedule.StatusCode);
        using (var oversizedNotes = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { auditDate = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), notes = new string('x', 1901) }),
                   requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, oversizedNotes.StatusCode);

        using (var recorded = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{_ownAssetId}/audit", "admin",
                   System.Net.Http.Json.JsonContent.Create(validRequest), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, recorded.StatusCode);

        using (var detail = await SendAsAdminAsync($"/api/assets/{_ownAssetId}"))
        {
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            using var payload = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            Assert.Equal(nextAuditOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                payload.RootElement.GetProperty("asset").GetProperty("nextAuditOn").GetString());
            Assert.Contains(payload.RootElement.GetProperty("history").EnumerateArray(), item =>
                item.GetProperty("eventType").GetString() == "Audited" &&
                item.GetProperty("details").GetString()!.Contains("Physical condition checked", StringComparison.Ordinal));
        }

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await db.AssetEvents.AsNoTracking().Where(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId).ToListAsync(),
            item => item.EventType == "Audited" && item.ActorUserId == _userId && item.Details!.Contains("Physical condition checked", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Asset_audit_schedule_can_be_set_or_cleared_only_by_authorized_company_members()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scheduledOn = today.AddDays(45);
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var missingCsrf = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = scheduledOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) })))
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        await SetCompanyRoleAsync("viewer");
        using (var readOnly = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "viewer",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = scheduledOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, readOnly.StatusCode);
        await SetCompanyRoleAsync("admin");

        using (var crossTenant = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_otherCompanyAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = scheduledOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        using (var pastDate = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, pastDate.StatusCode);
        using (var tooFar = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = today.AddYears(10).AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, tooFar.StatusCode);

        using (var scheduled = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = scheduledOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, scheduled.StatusCode);
            using var payload = JsonDocument.Parse(await scheduled.Content.ReadAsStringAsync());
            Assert.Equal(scheduledOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), payload.RootElement.GetProperty("nextAuditOn").GetString());
        }

        using (var cleared = await SendAsRoleAsync(HttpMethod.Put, $"/api/assets/{_ownAssetId}/audit-schedule", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { nextAuditOn = (string?)null }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            using var payload = JsonDocument.Parse(await cleared.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("nextAuditOn").ValueKind);
        }

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await db.Assets.AsNoTracking().SingleAsync(item => item.CompanyId == _companyId && item.Id == _ownAssetId);
        Assert.Null(asset.NextAuditOn);
        var events = await db.AssetEvents.AsNoTracking().Where(item => item.CompanyId == _companyId && item.AssetId == _ownAssetId).ToListAsync();
        Assert.Contains(events, item => item.EventType == "AuditScheduled" && item.ActorUserId == _userId);
        Assert.Contains(events, item => item.EventType == "AuditScheduleCleared" && item.ActorUserId == _userId);
    }

    [Fact]
    public async Task Depreciation_report_matches_half_year_boundaries_and_does_not_apply_linear_floor()
    {
        var categoryId = await GetOwnCategoryIdAsync();
        var floorModel = new AssetModel
        {
            CompanyId = _companyId,
            Name = $"Half-year floor check {_userId:N}",
            DepreciationMonths = 24,
            DepreciationMinimum = 20,
            DepreciationMinimumType = "percent",
            DepreciationMethod = "half_1"
        };
        var juneBoundaryModel = new AssetModel
        {
            CompanyId = _companyId,
            Name = $"June boundary check {_userId:N}",
            DepreciationMonths = 24,
            DepreciationMethod = "half_2"
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AssetModels.AddRange(floorModel, juneBoundaryModel);
            var otherCategoryId = await db.AssetCategories.Where(x => x.CompanyId == _otherCompanyId)
                .Select(x => x.Id).SingleAsync();
            var otherModel = new AssetModel
            {
                CompanyId = _otherCompanyId, Name = $"Other tenant depreciation model {_userId:N}",
                DepreciationMonths = 24, DepreciationMethod = "linear"
            };
            db.AssetModels.Add(otherModel);
            db.Assets.AddRange(
                new Asset
                {
                    CompanyId = _companyId, AssetTag = $"DEP-FLOOR-{_userId:N}", Name = "Half-year floor asset",
                    CategoryId = categoryId, ModelId = floorModel.Id, PurchaseDate = new DateOnly(2024, 10, 1),
                    PurchaseCost = 1000m, Currency = "TRY"
                },
                new Asset
                {
                    CompanyId = _companyId, AssetTag = $"DEP-JUNE-{_userId:N}", Name = "June first day asset",
                    CategoryId = categoryId, ModelId = juneBoundaryModel.Id, PurchaseDate = new DateOnly(2026, 6, 1),
                    PurchaseCost = 1000m, Currency = "TRY"
                },
                new Asset
                {
                    CompanyId = _otherCompanyId, AssetTag = $"OTHER-DEP-{_userId:N}", Name = "Other tenant depreciation asset",
                    CategoryId = otherCategoryId, ModelId = otherModel.Id, PurchaseDate = new DateOnly(2024, 1, 1),
                    PurchaseCost = 5000m, Currency = "TRY"
                });
            await db.SaveChangesAsync();
        }

        using var response = await SendAsAdminAsync("/api/reports/depreciation?asOf=2026-06-02&page=1&pageSize=10");
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 but got {(int)response.StatusCode}: {responseBody}");
        using var payload = JsonDocument.Parse(responseBody);
        Assert.Equal(2, payload.RootElement.GetProperty("total").GetInt32());
        var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var halfYearAsset = items.Single(item => item.GetProperty("name").GetString() == "Half-year floor asset");
        Assert.Equal(0m, halfYearAsset.GetProperty("bookValue").GetDecimal());
        Assert.Equal(1000m, halfYearAsset.GetProperty("accumulatedDepreciation").GetDecimal());
        Assert.Equal(33.33m, halfYearAsset.GetProperty("monthlyDepreciation").GetDecimal());
        var juneBoundaryAsset = items.Single(item => item.GetProperty("name").GetString() == "June first day asset");
        Assert.Equal(1000m, juneBoundaryAsset.GetProperty("bookValue").GetDecimal());
        Assert.DoesNotContain(items, item => item.GetProperty("name").GetString() == "Other tenant depreciation asset");

        using var export = await SendAsAdminAsync("/api/reports/depreciation/export.csv?asOf=2026-06-02");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var csv = await export.Content.ReadAsStringAsync();
        Assert.Contains("Half-year floor asset", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Other tenant depreciation asset", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Brand_model_category_and_images_are_company_scoped_and_reusable()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using var categoryResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/lookups/categories", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = $"CAT-{_userId:N}" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryPayload = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryPayload.RootElement.GetProperty("id").GetGuid();

        using var brandResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/lookups/brands", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = $"BRAND-{_userId:N}", website = "https://brand.example.invalid", supportEmail = "support@example.invalid" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, brandResponse.StatusCode);
        using var brandPayload = JsonDocument.Parse(await brandResponse.Content.ReadAsStringAsync());
        var brandId = brandPayload.RootElement.GetProperty("id").GetGuid();

        using var modelResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/lookups/models", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = $"MODEL-{_userId:N}", brandId, categoryId }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, modelResponse.StatusCode);
        using var modelPayload = JsonDocument.Parse(await modelResponse.Content.ReadAsStringAsync());
        var modelId = modelPayload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(categoryId, modelPayload.RootElement.GetProperty("categoryId").GetGuid());

        using var categoryList = await SendAsAdminAsync("/api/lookups/categories");
        Assert.Equal(HttpStatusCode.OK, categoryList.StatusCode);
        using (var categoryListPayload = JsonDocument.Parse(await categoryList.Content.ReadAsStringAsync()))
        {
            var category = Assert.Single(categoryListPayload.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == categoryId);
            Assert.Equal(0, category.GetProperty("assetCount").GetInt32());
            Assert.Equal(1, category.GetProperty("modelCount").GetInt32());
        }
        using var categoryDeleteWhileInUse = await SendAsRoleAsync(HttpMethod.Delete, $"/api/lookups/categories/{categoryId}", "admin", null, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Conflict, categoryDeleteWhileInUse.StatusCode);
        Assert.Contains("modellerde kullanılıyor", await categoryDeleteWhileInUse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jNioAAAAASUVORK5CYII=");
        var ownSupplierId = Guid.NewGuid();
        var foreignSupplierId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupplierCatalogEntries.AddRange(
                new SupplierCatalogEntry { Id = ownSupplierId, CompanyId = _companyId, Name = $"OWN-SUPPLIER-{_userId:N}", NormalizedName = $"OWN-SUPPLIER-{_userId:N}".ToUpperInvariant() },
                new SupplierCatalogEntry { Id = foreignSupplierId, CompanyId = _otherCompanyId, Name = $"OTHER-SUPPLIER-{_userId:N}", NormalizedName = $"OTHER-SUPPLIER-{_userId:N}".ToUpperInvariant() });
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            await db.SaveChangesAsync();
        }

        using var supplierLogoForm = new MultipartFormDataContent();
        supplierLogoForm.Add(new ByteArrayContent(png), "file", "supplier.png");
        using var supplierLogoUpload = await SendAsRoleAsync(HttpMethod.Post, $"/api/suppliers/{ownSupplierId}/logo", "admin",
            supplierLogoForm, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, supplierLogoUpload.StatusCode);
        using (var supplierLogo = await SendAsAdminAsync($"/api/suppliers/{ownSupplierId}/logo"))
        {
            Assert.Equal(HttpStatusCode.OK, supplierLogo.StatusCode);
            Assert.Equal("image/png", supplierLogo.Content.Headers.ContentType?.MediaType);
            Assert.Equal(png, await supplierLogo.Content.ReadAsByteArrayAsync());
        }
        using (var crossTenantSupplierLogo = await SendAsAdminAsync($"/api/suppliers/{ownSupplierId}/logo", _otherCompanyId))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantSupplierLogo.StatusCode);
        using (var crossTenantSupplierRecord = await SendAsAdminAsync($"/api/suppliers/{foreignSupplierId}/logo"))
            Assert.Equal(HttpStatusCode.NotFound, crossTenantSupplierRecord.StatusCode);
        using var logoForm = new MultipartFormDataContent();
        var logoContent = new ByteArrayContent(png); logoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        logoForm.Add(logoContent, "file", "logo.png");
        using var logoResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/brands/{brandId}/logo", "admin", logoForm, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);

        using var modelPhotoForm = new MultipartFormDataContent();
        var modelPhotoContent = new ByteArrayContent(png); modelPhotoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        modelPhotoForm.Add(modelPhotoContent, "file", "model.png");
        using var modelPhotoResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/models/{modelId}/photo", "admin", modelPhotoForm, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, modelPhotoResponse.StatusCode);

        var modelPdf = Encoding.ASCII.GetBytes("%PDF-1.4\nmodel specification\n%%EOF");
        using var modelAttachmentForm = new MultipartFormDataContent();
        modelAttachmentForm.Add(new ByteArrayContent(modelPdf), "file", "specification.pdf");
        using var modelAttachmentUpload = await SendAsRoleAsync(HttpMethod.Post, $"/api/models/{modelId}/attachments", "admin",
            modelAttachmentForm, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, modelAttachmentUpload.StatusCode);
        using var modelAttachmentPayload = JsonDocument.Parse(await modelAttachmentUpload.Content.ReadAsStringAsync());
        var modelAttachmentId = modelAttachmentPayload.RootElement.GetProperty("id").GetGuid();
        using (var modelAttachmentList = await SendAsAdminAsync($"/api/models/{modelId}/attachments"))
        {
            Assert.Equal(HttpStatusCode.OK, modelAttachmentList.StatusCode);
            using var listPayload = JsonDocument.Parse(await modelAttachmentList.Content.ReadAsStringAsync());
            var modelAttachment = Assert.Single(listPayload.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal("specification.pdf", modelAttachment.GetProperty("originalFileName").GetString());
            Assert.Equal("application/pdf", modelAttachment.GetProperty("contentType").GetString());
        }
        using (var modelAttachmentFile = await SendAsAdminAsync($"/api/models/{modelId}/attachments/{modelAttachmentId}/file?download=true"))
        {
            Assert.Equal(HttpStatusCode.OK, modelAttachmentFile.StatusCode);
            Assert.Equal("application/pdf", modelAttachmentFile.Content.Headers.ContentType?.MediaType);
            Assert.Equal(modelPdf, await modelAttachmentFile.Content.ReadAsByteArrayAsync());
        }
        using (var foreignModelAttachmentList = await SendAsAdminAsync($"/api/models/{modelId}/attachments", _otherCompanyId))
            Assert.Equal(HttpStatusCode.NotFound, foreignModelAttachmentList.StatusCode);
        using (var foreignModelAttachmentFile = await SendAsAdminAsync($"/api/models/{modelId}/attachments/{modelAttachmentId}/file", _otherCompanyId))
            Assert.Equal(HttpStatusCode.NotFound, foreignModelAttachmentFile.StatusCode);
        using (var foreignModelAttachmentDelete = await SendAsUserAsync(HttpMethod.Delete,
            $"/api/models/{modelId}/attachments/{modelAttachmentId}", _userId, _otherCompanyId,
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie, role: "admin"))
            Assert.Equal(HttpStatusCode.NotFound, foreignModelAttachmentDelete.StatusCode);
        using var invalidModelAttachmentForm = new MultipartFormDataContent();
        invalidModelAttachmentForm.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("not a PDF")), "file", "fake.pdf");
        using (var invalidModelAttachment = await SendAsRoleAsync(HttpMethod.Post, $"/api/models/{modelId}/attachments", "admin",
            invalidModelAttachmentForm, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, invalidModelAttachment.StatusCode);
        using (var removeModelAttachment = await SendAsRoleAsync(HttpMethod.Delete,
            $"/api/models/{modelId}/attachments/{modelAttachmentId}", "admin", new StringContent(string.Empty), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NoContent, removeModelAttachment.StatusCode);
        await using (var verifyModelAttachmentAuditScope = _factory!.Services.CreateAsyncScope())
        {
            var db = verifyModelAttachmentAuditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.CompanyId == _companyId && x.TargetType == "AssetModel" &&
                x.TargetId == modelId && (x.Action == "AttachmentAdded" || x.Action == "AttachmentRemoved")));
        }

        using var modelPhoto = await SendAsAdminAsync($"/api/models/{modelId}/photo");
        Assert.Equal(HttpStatusCode.OK, modelPhoto.StatusCode);
        using var brandLogo = await SendAsAdminAsync($"/api/brands/{brandId}/logo");
        Assert.Equal(HttpStatusCode.OK, brandLogo.StatusCode);

        using var accessoryModelResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/lookups/accessory-models", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = $"ACCESSORY-MODEL-{_userId:N}", brandId }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryModelResponse.StatusCode);
        using var accessoryModelPayload = JsonDocument.Parse(await accessoryModelResponse.Content.ReadAsStringAsync());
        var accessoryModelId = accessoryModelPayload.RootElement.GetProperty("id").GetGuid();
        using var accessoryModelPhotoForm = new MultipartFormDataContent();
        var accessoryModelPhotoContent = new ByteArrayContent(png); accessoryModelPhotoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        accessoryModelPhotoForm.Add(accessoryModelPhotoContent, "file", "accessory-model.png");
        using var accessoryModelPhotoResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-models/{accessoryModelId}/photo", "admin", accessoryModelPhotoForm, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, accessoryModelPhotoResponse.StatusCode);

        var catalogImagePaths = new[]
        {
            $"/api/brands/{brandId}/logo",
            $"/api/models/{modelId}/photo",
            $"/api/accessory-models/{accessoryModelId}/photo",
            $"/api/suppliers/{ownSupplierId}/logo"
        };
        foreach (var imagePath in catalogImagePaths)
        {
            using (var ownImage = await SendAsAdminAsync(imagePath))
            {
                Assert.Equal(HttpStatusCode.OK, ownImage.StatusCode);
                Assert.Equal("image/png", ownImage.Content.Headers.ContentType?.MediaType);
                Assert.Equal(png, await ownImage.Content.ReadAsByteArrayAsync());
            }

            using (var crossTenantRead = await SendAsAdminAsync(imagePath, _otherCompanyId))
                Assert.Equal(HttpStatusCode.NotFound, crossTenantRead.StatusCode);

            using (var crossTenantDelete = await SendAsUserAsync(HttpMethod.Delete, imagePath, _userId, _otherCompanyId,
                requestToken: requestToken, antiforgeryCookie: antiforgeryCookie, role: "admin"))
                Assert.Equal(HttpStatusCode.NotFound, crossTenantDelete.StatusCode);

            using var crossTenantUploadForm = new MultipartFormDataContent();
            crossTenantUploadForm.Add(new ByteArrayContent(png), "file", "cross-tenant.png");
            using var crossTenantUpload = await SendAsUserAsync(HttpMethod.Post, imagePath, _userId, _otherCompanyId,
                crossTenantUploadForm, requestToken, antiforgeryCookie, role: "admin");
            Assert.Equal(HttpStatusCode.NotFound, crossTenantUpload.StatusCode);
        }

        using var accessoryCategoryResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-categories", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = $"KEYBOARD-{_userId:N}" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryCategoryResponse.StatusCode);
        using var accessoryCategoryPayload = JsonDocument.Parse(await accessoryCategoryResponse.Content.ReadAsStringAsync());
        var accessoryCategoryId = accessoryCategoryPayload.RootElement.GetProperty("id").GetGuid();

        using var accessoryResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/accessories", "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                name = $"ACCESSORY-{_userId:N}", categoryId = accessoryCategoryId, brandId, modelId = accessoryModelId,
                itemNumber = $"ITEM-{_userId:N}", supplier = "Test Supplier", location = "Test Location",
                minimumQuantity = 0, quantity = 2, unitCost = 100m, currency = "TRY",
                purchaseDate = (DateOnly?)null, notes = "Brand/model catalog acceptance", isRequestable = false
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryResponse.StatusCode);
        using var accessoryPayload = JsonDocument.Parse(await accessoryResponse.Content.ReadAsStringAsync());
        var accessoryId = accessoryPayload.RootElement.GetProperty("id").GetGuid();
        using var accessoryDetail = await SendAsAdminAsync($"/api/accessories/{accessoryId}");
        Assert.Equal(HttpStatusCode.OK, accessoryDetail.StatusCode);
        using var accessoryDetailPayload = JsonDocument.Parse(await accessoryDetail.Content.ReadAsStringAsync());
        var accessoryItem = accessoryDetailPayload.RootElement.GetProperty("item");
        Assert.Equal(brandId, accessoryItem.GetProperty("brandId").GetGuid());
        Assert.Equal(accessoryModelId, accessoryItem.GetProperty("modelId").GetGuid());
        Assert.Equal("ACCESSORY-MODEL-" + _userId.ToString("N"), accessoryItem.GetProperty("modelName").GetString());
        Assert.NotNull(accessoryItem.GetProperty("modelPhotoUrl").GetString());
        using var secondAccessoryResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/accessories", "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                name = $"ACCESSORY-SECOND-{_userId:N}", categoryId = accessoryCategoryId, brandId, modelId = accessoryModelId,
                itemNumber = $"ITEM-SECOND-{_userId:N}", supplier = "Test Supplier", location = "Test Location",
                minimumQuantity = 0, quantity = 1, unitCost = 100m, currency = "TRY",
                purchaseDate = (DateOnly?)null, notes = "Same reusable accessory model", isRequestable = false
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, secondAccessoryResponse.StatusCode);
    }

    [Fact]
    public async Task Accessory_categories_are_tenant_scoped_and_keep_linked_accessories_when_archived()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var categoryName = $"SECURITY-CATEGORY-{_userId:N}";

        using var categoryResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-categories", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = categoryName }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryPayload = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryPayload.RootElement.GetProperty("id").GetGuid();

        var foreignCategoryId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AccessoryCategories.Add(new AccessoryCategory { Id = foreignCategoryId, CompanyId = _otherCompanyId, Name = $"FOREIGN-{_userId:N}" });
            await db.SaveChangesAsync();
        }
        using var crossTenantRename = await SendAsUserAsync(HttpMethod.Patch, $"/api/accessory-categories/{foreignCategoryId}", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { name = "Cross tenant" }), requestToken, antiforgeryCookie, role: "admin");
        Assert.Equal(HttpStatusCode.NotFound, crossTenantRename.StatusCode);

        using var createAccessory = await SendAsRoleAsync(HttpMethod.Post, "/api/accessories", "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                name = $"CATEGORY-LINKED-{_userId:N}", categoryId, minimumQuantity = 0, quantity = 1,
                currency = "TRY", isRequestable = false
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createAccessory.StatusCode);
        using var itemPayload = JsonDocument.Parse(await createAccessory.Content.ReadAsStringAsync());
        var accessoryId = itemPayload.RootElement.GetProperty("id").GetGuid();

        using var categoryFilter = await SendAsAdminAsync($"/api/accessories?categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, categoryFilter.StatusCode);
        using var filteredPayload = JsonDocument.Parse(await categoryFilter.Content.ReadAsStringAsync());
        var filteredItem = Assert.Single(filteredPayload.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(categoryName, filteredItem.GetProperty("category").GetString());

        var renamedCategory = categoryName + "-RENAMED";
        using var renameResponse = await SendAsRoleAsync(HttpMethod.Patch, $"/api/accessory-categories/{categoryId}", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = renamedCategory }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);

        using var archiveResponse = await SendAsRoleAsync(HttpMethod.Post, $"/api/accessory-categories/{categoryId}/archive", "admin",
            new StringContent("{}", Encoding.UTF8, "application/json"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        using var archivedFilterOptions = await SendAsAdminAsync("/api/accessories/filter-options");
        using var archivedFilterPayload = JsonDocument.Parse(await archivedFilterOptions.Content.ReadAsStringAsync());
        Assert.DoesNotContain(archivedFilterPayload.RootElement.GetProperty("categories").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == categoryId);
        Assert.Contains(archivedFilterPayload.RootElement.GetProperty("archivedCategories").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == categoryId);

        using var retainedAccessory = await SendAsAdminAsync($"/api/accessories/{accessoryId}");
        Assert.Equal(HttpStatusCode.OK, retainedAccessory.StatusCode);
        using var retainedPayload = JsonDocument.Parse(await retainedAccessory.Content.ReadAsStringAsync());
        Assert.Equal(renamedCategory, retainedPayload.RootElement.GetProperty("item").GetProperty("category").GetString());
    }

    [Fact]
    public async Task Concurrent_asset_checkouts_assign_one_asset_to_only_one_employee()
    {
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Employees.Add(new Employee
            {
                Id = _ownSecondEmployeeId,
                CompanyId = _companyId,
                EmployeeNumber = $"OWN-SECOND-{_userId:N}",
                DisplayName = $"OWN-SECOND-SECURITY-EMPLOYEE-{_userId:N}"
            });
            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var path = $"/api/assets/{_ownAssetId}/assign";
        var firstRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownEmployeeId, note = "parallel asset checkout A" }), requestToken, antiforgeryCookie);
        var secondRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { employeeId = _ownSecondEmployeeId, note = "parallel asset checkout B" }), requestToken, antiforgeryCookie);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;

        var responseDetails = $"first={(int)firstResponse.StatusCode} {await firstResponse.Content.ReadAsStringAsync()}; " +
            $"second={(int)secondResponse.StatusCode} {await secondResponse.Content.ReadAsStringAsync()}";
        Assert.True(new[] { firstResponse.StatusCode, secondResponse.StatusCode }.Contains(HttpStatusCode.OK), responseDetails);
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.OK));
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.Conflict));

        await using var verificationScope = _factory!.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await verificationDb.Assets.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAssetId);
        Assert.Contains(asset.AssignedEmployeeId, new Guid?[] { _ownEmployeeId, _ownSecondEmployeeId });
        Assert.Equal(AssetStatuses.Deployed, asset.Status);
        Assert.Single(await verificationDb.AssetEvents.Where(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId && x.EventType == "Assigned").ToListAsync());
    }

    [Fact]
    public async Task Concurrent_component_checkouts_cannot_allocate_the_same_remaining_stock_twice()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var path = $"/api/components/{_ownComponentId}/allocate";
        var firstRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, quantity = 2, notes = "parallel checkout A" }), requestToken, antiforgeryCookie);
        var secondRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { assetId = _ownAssetId, quantity = 2, notes = "parallel checkout B" }), requestToken, antiforgeryCookie);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;

        var responseDetails = $"first={(int)firstResponse.StatusCode} {await firstResponse.Content.ReadAsStringAsync()}; " +
            $"second={(int)secondResponse.StatusCode} {await secondResponse.Content.ReadAsStringAsync()}";
        Assert.True(new[] { firstResponse.StatusCode, secondResponse.StatusCode }.Contains(HttpStatusCode.Created), responseDetails);
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.Created));
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.Conflict));

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allocated = await db.ComponentAllocations.Where(x => x.CompanyId == _companyId && x.ComponentId == _ownComponentId)
            .SumAsync(x => x.Quantity - x.RemovedQuantity);
        Assert.Equal(5, allocated);
    }

    [Fact]
    public async Task Concurrent_accessory_checkouts_cannot_allocate_the_same_remaining_stock_twice()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var path = $"/api/accessories/{_ownAccessoryId}/checkout";
        var firstRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { targetType = "location", location = "Concurrent checkout A", quantity = 2 }), requestToken, antiforgeryCookie);
        var secondRequest = SendAsRoleAsync(HttpMethod.Post, path, "admin",
            System.Net.Http.Json.JsonContent.Create(new { targetType = "location", location = "Concurrent checkout B", quantity = 2 }), requestToken, antiforgeryCookie);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;

        var responseDetails = $"first={(int)firstResponse.StatusCode} {await firstResponse.Content.ReadAsStringAsync()}; " +
            $"second={(int)secondResponse.StatusCode} {await secondResponse.Content.ReadAsStringAsync()}";
        Assert.True(new[] { firstResponse.StatusCode, secondResponse.StatusCode }.Contains(HttpStatusCode.Created), responseDetails);
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.Created));
        Assert.Equal(1, new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Count(status => status == HttpStatusCode.Conflict));

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outstanding = await db.AccessoryAssignments.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId)
            .SumAsync(x => x.Quantity - x.ReturnedQuantity);
        Assert.Equal(4, outstanding);
        Assert.Single(await db.AccessoryMovements.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId && x.MovementType == "Checkout").ToListAsync());
    }

    [Fact]
    public async Task Concurrent_accessory_checkout_and_stock_reduction_keep_stock_above_assignments()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var checkout = SendAsRoleAsync(HttpMethod.Post, $"/api/accessories/{_ownAccessoryId}/checkout", "admin",
            System.Net.Http.Json.JsonContent.Create(new { targetType = "location", location = "Concurrent checkout", quantity = 2 }), requestToken, antiforgeryCookie);
        var stockReduction = SendAsRoleAsync(HttpMethod.Post, $"/api/accessories/{_ownAccessoryId}/stock", "admin",
            System.Net.Http.Json.JsonContent.Create(new { quantity = -2, notes = "Concurrent stock reduction" }), requestToken, antiforgeryCookie);

        using var checkoutResponse = await checkout;
        using var stockResponse = await stockReduction;
        var responseDetails = $"checkout={(int)checkoutResponse.StatusCode} {await checkoutResponse.Content.ReadAsStringAsync()}; " +
            $"stock={(int)stockResponse.StatusCode} {await stockResponse.Content.ReadAsStringAsync()}";
        var validOutcome =
            (checkoutResponse.StatusCode == HttpStatusCode.Created && stockResponse.StatusCode == HttpStatusCode.Conflict) ||
            (checkoutResponse.StatusCode == HttpStatusCode.Conflict && stockResponse.StatusCode == HttpStatusCode.OK);
        Assert.True(validOutcome, responseDetails);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = await db.Accessories.SingleAsync(x => x.CompanyId == _companyId && x.Id == _ownAccessoryId);
        var outstanding = await db.AccessoryAssignments.Where(x => x.CompanyId == _companyId && x.AccessoryId == _ownAccessoryId)
            .SumAsync(x => x.Quantity - x.ReturnedQuantity);
        Assert.Equal(1, item.TotalQuantity - outstanding);
        Assert.Contains(item.TotalQuantity, new[] { 3, 5 });
    }

    [Fact]
    public async Task Concurrent_purchase_order_receipts_cannot_receive_the_same_remaining_stock_twice()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var orderNumber = $"RACE-{Guid.NewGuid():N}"[..15];
        using var createResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/stock-orders", "admin",
            System.Net.Http.Json.JsonContent.Create(new
            {
                orderNumber, supplier = "Concurrent receipt supplier", orderDate = today, currency = "TRY",
                items = new[] { new { consumableId = (Guid?)_ownConsumableId, componentId = (Guid?)null, quantity = 3, unitCost = 10m } }
            }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var orderId = created.RootElement.GetProperty("id").GetGuid();

        using var detailResponse = await SendAsAdminAsync($"/api/stock-orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var lineId = Assert.Single(detail.RootElement.GetProperty("lines").EnumerateArray()).GetProperty("id").GetGuid();

        var receivePath = $"/api/stock-orders/{orderId}/receive";
        var firstRequest = SendAsRoleAsync(HttpMethod.Post, receivePath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { receivedOn = today, items = new[] { new { lineId, quantity = 2 } } }), requestToken, antiforgeryCookie);
        var secondRequest = SendAsRoleAsync(HttpMethod.Post, receivePath, "admin",
            System.Net.Http.Json.JsonContent.Create(new { receivedOn = today, items = new[] { new { lineId, quantity = 2 } } }), requestToken, antiforgeryCookie);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;
        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        var responseDetails = $"first={(int)firstResponse.StatusCode} {await firstResponse.Content.ReadAsStringAsync()}; " +
            $"second={(int)secondResponse.StatusCode} {await secondResponse.Content.ReadAsStringAsync()}";
        Assert.True(statuses.Contains(HttpStatusCode.OK), responseDetails);
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Conflict));

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var line = await db.StockPurchaseOrderLines.SingleAsync(x => x.CompanyId == _companyId && x.PurchaseOrderId == orderId);
        Assert.Equal(2, line.QuantityReceived);
        Assert.Equal("PartiallyReceived", await db.StockPurchaseOrders.Where(x => x.CompanyId == _companyId && x.Id == orderId)
            .Select(x => x.Status).SingleAsync());
        Assert.Equal(1, await db.ConsumableMovements.CountAsync(x => x.CompanyId == _companyId && x.ConsumableId == _ownConsumableId &&
            x.MovementType == "Received" && x.Notes != null && x.Notes.Contains(orderNumber)));
        Assert.Equal(2, await db.ConsumableMovements.Where(x => x.CompanyId == _companyId && x.ConsumableId == _ownConsumableId &&
            x.MovementType == "Received" && x.Notes != null && x.Notes.Contains(orderNumber)).SumAsync(x => x.Quantity));
    }

    [Fact]
    public async Task Password_recovery_does_not_disclose_account_existence_and_reset_tokens_are_single_use()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        string accountEmail;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = await userManager.FindByIdAsync(_userId.ToString());
            Assert.NotNull(user);
            accountEmail = Assert.IsType<string>(user.Email);
            Assert.Equal(accountEmail.ToUpperInvariant(), user.NormalizedEmail);
            Assert.NotNull(await userManager.FindByEmailAsync(accountEmail));
        }

        using var existingAccountResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/forgot", "admin",
            System.Net.Http.Json.JsonContent.Create(new { email = accountEmail }), requestToken, antiforgeryCookie);
        using var missingAccountResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/forgot", "admin",
            System.Net.Http.Json.JsonContent.Create(new { email = "missing@example.invalid" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Accepted, existingAccountResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, missingAccountResponse.StatusCode);
        using var existingMessage = JsonDocument.Parse(await existingAccountResponse.Content.ReadAsStringAsync());
        using var missingMessage = JsonDocument.Parse(await missingAccountResponse.Content.ReadAsStringAsync());
        Assert.Equal(existingMessage.RootElement.GetProperty("message").GetString(),
            missingMessage.RootElement.GetProperty("message").GetString());
        var deliveredMessage = Assert.Single(_smtpCapture.Messages);
        Assert.Contains($"To: {accountEmail}", deliveredMessage, StringComparison.OrdinalIgnoreCase);
        var bodySeparator = deliveredMessage.IndexOf("\n\n", StringComparison.Ordinal);
        Assert.True(bodySeparator >= 0, "The captured SMTP message must contain a MIME body.");
        var headers = deliveredMessage[..bodySeparator];
        var encodedBody = deliveredMessage[(bodySeparator + 2)..].Replace("\r", "", StringComparison.Ordinal).Trim();
        var htmlBody = headers.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase)
            ? Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(encodedBody.Where(character => !char.IsWhiteSpace(character)))))
            : encodedBody;
        Assert.Contains("reset-password.html", htmlBody, StringComparison.Ordinal);
        Assert.Contains("#email", htmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("?email=", htmlBody, StringComparison.OrdinalIgnoreCase);

        string resetToken;
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = await userManager.FindByIdAsync(_userId.ToString());
            Assert.NotNull(user);
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        }

        const string newPassword = "New.Acceptance-Password-42!";
        var resetContent = () => System.Net.Http.Json.JsonContent.Create(new
        {
            email = accountEmail,
            token = resetToken,
            newPassword
        });
        using var resetResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/reset", "admin",
            resetContent(), requestToken, antiforgeryCookie);
        Assert.True(resetResponse.StatusCode == HttpStatusCode.OK,
            $"Password reset returned {(int)resetResponse.StatusCode}: {await resetResponse.Content.ReadAsStringAsync()}");

        using var replayResponse = await SendAsRoleAsync(HttpMethod.Post, "/api/session/password/reset", "admin",
            resetContent(), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);

        await using var verificationScope = _factory!.Services.CreateAsyncScope();
        var verificationManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
        var updatedUser = await verificationManager.FindByIdAsync(_userId.ToString());
        Assert.NotNull(updatedUser);
        Assert.True(await verificationManager.CheckPasswordAsync(updatedUser, newPassword));
    }

    [Fact]
    public async Task Login_refreshes_antiforgery_token_for_authenticated_session_requests()
    {
        const string password = "Acceptance-login-csrf-Password-43!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = await users.FindByIdAsync(_userId.ToString());
            Assert.NotNull(user);
            var passwordResult = await users.AddPasswordAsync(user, password);
            Assert.True(passwordResult.Succeeded, string.Join("; ", passwordResult.Errors.Select(error => error.Description)));
        }

        using var cookieFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EnvanterTR"] = Environment.GetEnvironmentVariable("ENVANTERTR_SECURITY_TEST_CONNECTION"),
                    ["Storage:CompanyMediaPath"] = _mediaPath
                }));
            builder.ConfigureTestServices(services => services.PostConfigure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.Zero));
        });
        using var loginClient = cookieFactory.CreateClient();
        using var anonymousCsrfResponse = await loginClient.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, anonymousCsrfResponse.StatusCode);
        using var anonymousCsrfPayload = JsonDocument.Parse(await anonymousCsrfResponse.Content.ReadAsStringAsync());
        var anonymousRequestToken = anonymousCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { email, password, companyId = _companyId, rememberMe = true })
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", anonymousRequestToken);
        using var loginResponse = await loginClient.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var sessionResponse = await loginClient.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        using (var refreshedSession = JsonDocument.Parse(await sessionResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(_companyId.ToString(), refreshedSession.RootElement.GetProperty("companyId").GetString());
            Assert.Equal("Security acceptance company", refreshedSession.RootElement.GetProperty("companyName").GetString());
            Assert.Equal("admin", refreshedSession.RootElement.GetProperty("role").GetString());
        }

        using var refreshedCompanyAssets = await loginClient.GetAsync("/api/assets?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, refreshedCompanyAssets.StatusCode);
        using (var refreshedAssets = JsonDocument.Parse(await refreshedCompanyAssets.Content.ReadAsStringAsync()))
        {
            var refreshedAssetTags = refreshedAssets.RootElement.GetProperty("items").EnumerateArray()
                .Select(asset => asset.GetProperty("assetTag").GetString()).ToArray();
            Assert.Contains("OWN-1", refreshedAssetTags);
            Assert.DoesNotContain("OTHER-1", refreshedAssetTags);
        }

        using var firstDeviceListResponse = await loginClient.GetAsync("/api/session/devices");
        Assert.Equal(HttpStatusCode.OK, firstDeviceListResponse.StatusCode);
        using (var firstDeviceList = JsonDocument.Parse(await firstDeviceListResponse.Content.ReadAsStringAsync()))
        {
            Assert.Single(firstDeviceList.RootElement.GetProperty("sessions").EnumerateArray());
            Assert.True(firstDeviceList.RootElement.GetProperty("sessions")[0].GetProperty("current").GetBoolean());
        }

        using var secondClient = cookieFactory.CreateClient();
        using var secondCsrfResponse = await secondClient.GetAsync("/api/session/csrf");
        using var secondCsrfPayload = JsonDocument.Parse(await secondCsrfResponse.Content.ReadAsStringAsync());
        var secondRequestToken = secondCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using var secondLoginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { email, password, companyId = _companyId, rememberMe = true })
        };
        secondLoginRequest.Headers.Add("X-CSRF-TOKEN", secondRequestToken);
        using var secondLoginResponse = await secondClient.SendAsync(secondLoginRequest);
        Assert.Equal(HttpStatusCode.OK, secondLoginResponse.StatusCode);

        using var authenticatedCsrfResponse = await loginClient.GetAsync("/api/session/csrf");
        using var authenticatedCsrfPayload = JsonDocument.Parse(await authenticatedCsrfResponse.Content.ReadAsStringAsync());
        var authenticatedRequestToken = authenticatedCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        using var devicesResponse = await loginClient.GetAsync("/api/session/devices");
        Assert.Equal(HttpStatusCode.OK, devicesResponse.StatusCode);
        Guid secondSessionId;
        using (var devicesPayload = JsonDocument.Parse(await devicesResponse.Content.ReadAsStringAsync()))
        {
            var devices = devicesPayload.RootElement.GetProperty("sessions").EnumerateArray().ToArray();
            Assert.Equal(2, devices.Length);
            Assert.Single(devices, device => device.GetProperty("current").GetBoolean());
            secondSessionId = devices.Single(device => !device.GetProperty("current").GetBoolean()).GetProperty("id").GetGuid();
        }

        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/session/devices/{secondSessionId:D}/revoke")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { })
        };
        revokeRequest.Headers.Add("X-CSRF-TOKEN", authenticatedRequestToken);
        using var revokeResponse = await loginClient.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        using var revokedSessionResponse = await secondClient.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSessionResponse.StatusCode);
        using var revokedAssetListResponse = await secondClient.GetAsync("/api/assets?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedAssetListResponse.StatusCode);

        using var remainingDevicesResponse = await loginClient.GetAsync("/api/session/devices");
        using (var remainingDevices = JsonDocument.Parse(await remainingDevicesResponse.Content.ReadAsStringAsync()))
            Assert.Single(remainingDevices.RootElement.GetProperty("sessions").EnumerateArray());

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/session/logout");
        logoutRequest.Headers.Add("X-CSRF-TOKEN", authenticatedRequestToken);
        using var logoutResponse = await loginClient.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use()
    {
        const string password = "Acceptance-2fa-Password-43!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
            var user = await users.FindByIdAsync(_userId.ToString());
            Assert.NotNull(user);
            var passwordResult = await users.AddPasswordAsync(user, password);
            Assert.True(passwordResult.Succeeded, string.Join("; ", passwordResult.Errors.Select(error => error.Description)));
        }

        using var setupCsrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var setupCsrfPayload = JsonDocument.Parse(await setupCsrfResponse.Content.ReadAsStringAsync());
        var setupToken = setupCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var setupCookie = setupCsrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var setupResponse = await SendAsUserAsync(HttpMethod.Post, "/api/session/2fa/setup", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { currentPassword = password }), setupToken, setupCookie);
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        using var setupPayload = JsonDocument.Parse(await setupResponse.Content.ReadAsStringAsync());
        var secret = setupPayload.RootElement.GetProperty("secret").GetString()!;
        Assert.StartsWith("otpauth://totp/", setupPayload.RootElement.GetProperty("authenticatorUri").GetString());

        using var enableResponse = await SendAsUserAsync(HttpMethod.Post, "/api/session/2fa/enable", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { code = GenerateAuthenticatorCode(secret) }), setupToken, setupCookie);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        using var enablePayload = JsonDocument.Parse(await enableResponse.Content.ReadAsStringAsync());
        var recoveryCode = enablePayload.RootElement.GetProperty("recoveryCodes").EnumerateArray().First().GetString()!;
        Assert.Equal(10, enablePayload.RootElement.GetProperty("recoveryCodes").GetArrayLength());

        using var loginClient = _factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var loginCsrfResponse = await loginClient.GetAsync("/api/session/csrf");
        using var loginCsrfPayload = JsonDocument.Parse(await loginCsrfResponse.Content.ReadAsStringAsync());
        var loginToken = loginCsrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var loginCookie = loginCsrfResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var challenge = await SendAnonymousLoginAsync(loginClient, email, password, null, loginToken, loginCookie))
        using (var payload = JsonDocument.Parse(await challenge.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.Conflict, challenge.StatusCode);
            Assert.True(payload.RootElement.GetProperty("twoFactorRequired").GetBoolean());
        }

        using (var recoveryLogin = await SendAnonymousLoginAsync(loginClient, email, password, recoveryCode, loginToken, loginCookie))
            Assert.Equal(HttpStatusCode.OK, recoveryLogin.StatusCode);

        using (var replayedRecovery = await SendAnonymousLoginAsync(loginClient, email, password, recoveryCode, loginToken, loginCookie))
        using (var payload = JsonDocument.Parse(await replayedRecovery.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.BadRequest, replayedRecovery.StatusCode);
            Assert.True(payload.RootElement.GetProperty("twoFactorRequired").GetBoolean());
        }

        using (var authenticatorLogin = await SendAnonymousLoginAsync(loginClient, email, password, GenerateAuthenticatorCode(secret), loginToken, loginCookie))
            Assert.Equal(HttpStatusCode.OK, authenticatorLogin.StatusCode);

        using var rotateResponse = await SendAsUserAsync(HttpMethod.Post, "/api/session/2fa/recovery-codes", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { currentPassword = password, code = GenerateAuthenticatorCode(secret) }), setupToken, setupCookie);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        using var rotatePayload = JsonDocument.Parse(await rotateResponse.Content.ReadAsStringAsync());
        var rotatedRecoveryCode = rotatePayload.RootElement.GetProperty("recoveryCodes").EnumerateArray().First().GetString()!;

        using (var disabledRecoveryLogin = await SendAnonymousLoginAsync(loginClient, email, password, rotatedRecoveryCode, loginToken, loginCookie))
            Assert.Equal(HttpStatusCode.OK, disabledRecoveryLogin.StatusCode);

        using var disableResponse = await SendAsUserAsync(HttpMethod.Post, "/api/session/2fa/disable", _userId, _companyId,
            System.Net.Http.Json.JsonContent.Create(new { currentPassword = password, code = GenerateAuthenticatorCode(secret) }), setupToken, setupCookie);
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        using (var passwordOnlyLogin = await SendAnonymousLoginAsync(loginClient, email, password, null, loginToken, loginCookie))
            Assert.Equal(HttpStatusCode.OK, passwordOnlyLogin.StatusCode);
        using (var logoutResponse = await SendAsUserAsync(HttpMethod.Post, "/api/session/logout", _userId, _companyId,
            requestToken: setupToken, antiforgeryCookie: setupCookie))
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        await using var auditScope = _factory.Services.CreateAsyncScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accountEvents = await auditDb.AuditEntries.AsNoTracking()
            .Where(entry => entry.CompanyId == _companyId && entry.TargetId == _userId)
            .Select(entry => new { entry.Action, entry.Details }).ToListAsync();
        Assert.True(accountEvents.Count(entry => entry.Action == "LoginSucceeded") >= 4);
        Assert.Contains(accountEvents, entry => entry.Action == "Logout");
        Assert.Contains(accountEvents, entry => entry.Action == "TwoFactorEnabled");
        Assert.Contains(accountEvents, entry => entry.Action == "TwoFactorRecoveryCodesRotated");
        Assert.Contains(accountEvents, entry => entry.Action == "TwoFactorDisabled");
        Assert.DoesNotContain(accountEvents, entry => entry.Details!.Contains(secret, StringComparison.Ordinal) ||
            entry.Details.Contains(recoveryCode, StringComparison.Ordinal) ||
            entry.Details.Contains(rotatedRecoveryCode, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_company_choices_are_limited_to_the_accounts_active_memberships()
    {
        const string password = "Acceptance-tenant-choice-Password-43!";
        var email = $"acceptance-{_userId:N}@example.invalid";
        var additionalCompanyId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var users = services.GetRequiredService<UserManager<EnvanterUser>>();
            var user = await users.FindByIdAsync(_userId.ToString());
            Assert.NotNull(user);
            var passwordResult = await users.AddPasswordAsync(user, password);
            Assert.True(passwordResult.Succeeded, string.Join("; ", passwordResult.Errors.Select(error => error.Description)));

            var db = services.GetRequiredService<AppDbContext>();
            db.Companies.Add(new Company { Id = additionalCompanyId, Name = "Authorized second company", Slug = $"authorized-{additionalCompanyId:N}", IsActive = true });
            db.CompanyMemberships.Add(new CompanyMembership { CompanyId = additionalCompanyId, UserId = _userId, RoleKey = "owner" });
            await db.SaveChangesAsync();
        }

        using var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var csrfResponse = await loginClient.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        using (var chooserResponse = await SendAnonymousLoginAsync(loginClient, email, password, null,
            requestToken, antiforgeryCookie, companyId: null))
        using (var chooser = JsonDocument.Parse(await chooserResponse.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.Conflict, chooserResponse.StatusCode);
            var companies = chooser.RootElement.GetProperty("companies").EnumerateArray().ToArray();
            Assert.Equal(2, companies.Length);
            Assert.Contains(companies, company => company.GetProperty("id").GetGuid() == _companyId);
            Assert.Contains(companies, company => company.GetProperty("id").GetGuid() == additionalCompanyId);
            Assert.DoesNotContain(companies, company => company.GetProperty("id").GetGuid() == _otherCompanyId);
            Assert.DoesNotContain(companies, company => company.GetProperty("name").GetString() == "Other security acceptance company");
        }

        using (var authorizedChoice = await SendAnonymousLoginAsync(loginClient, email, password, null,
            requestToken, antiforgeryCookie, companyId: additionalCompanyId))
        using (var session = JsonDocument.Parse(await authorizedChoice.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, authorizedChoice.StatusCode);
            Assert.Equal(additionalCompanyId, session.RootElement.GetProperty("companyId").GetGuid());
        }

        using var unrelatedChoice = await SendAnonymousLoginAsync(loginClient, email, password, null,
            requestToken, antiforgeryCookie, companyId: _otherCompanyId);
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedChoice.StatusCode);

        using (var tenantLogin = await SendAnonymousLoginAsync(loginClient, email, password, null,
            requestToken, antiforgeryCookie, companyId: null, host: $"acceptance-{_companyId:N}.envantertr.com"))
        using (var session = JsonDocument.Parse(await tenantLogin.Content.ReadAsStringAsync()))
        {
            Assert.Equal(HttpStatusCode.OK, tenantLogin.StatusCode);
            Assert.Equal(_companyId, session.RootElement.GetProperty("companyId").GetGuid());
        }

        using var wrongTenantChoice = await SendAnonymousLoginAsync(loginClient, email, password, null,
            requestToken, antiforgeryCookie, companyId: additionalCompanyId, host: $"acceptance-{_companyId:N}.envantertr.com");
        Assert.Equal(HttpStatusCode.Forbidden, wrongTenantChoice.StatusCode);
    }

    [Fact]
    public async Task Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain()
    {
        using (var correctTenant = await SendAsUserAsync(HttpMethod.Get, "/api/assets?page=1&pageSize=5",
            _userId, _companyId, host: $"acceptance-{_companyId:N}.envantertr.com"))
            Assert.Equal(HttpStatusCode.OK, correctTenant.StatusCode);

        using (var otherTenant = await SendAsUserAsync(HttpMethod.Get, "/api/assets?page=1&pageSize=5",
            _userId, _companyId, host: $"other-{_otherCompanyId:N}.envantertr.com"))
            Assert.Equal(HttpStatusCode.Unauthorized, otherTenant.StatusCode);

        using (var unknownTenant = await SendAsUserAsync(HttpMethod.Get, "/api/assets?page=1&pageSize=5",
            _userId, _companyId, host: "missing-company.envantertr.com"))
            Assert.Equal(HttpStatusCode.Unauthorized, unknownTenant.StatusCode);

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        using (var platformSession = await SendAsPlatformAdminRequestAsync(HttpMethod.Get, "/api/session",
            host: $"acceptance-{_companyId:N}.envantertr.com"))
            Assert.Equal(HttpStatusCode.Unauthorized, platformSession.StatusCode);
    }

    [Fact]
    public async Task Company_profile_rejects_reserved_and_duplicate_tenant_slugs_and_persists_a_valid_slug()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var uniqueSlug = $"new-{Guid.NewGuid():N}";

        using (var save = await SendAsRoleAsync(HttpMethod.Put, "/api/company/profile", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = "Security acceptance company", slug = uniqueSlug,
                contactEmail = "security@example.invalid", brandPrimaryColor = "#1769E0" }), requestToken, antiforgeryCookie))
        {
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            using var saved = JsonDocument.Parse(await save.Content.ReadAsStringAsync());
            Assert.Equal(uniqueSlug, saved.RootElement.GetProperty("slug").GetString());
        }

        using (var reserved = await SendAsRoleAsync(HttpMethod.Put, "/api/company/profile", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = "Security acceptance company", slug = "www",
                contactEmail = "security@example.invalid", brandPrimaryColor = "#1769E0" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, reserved.StatusCode);

        using (var tooLong = await SendAsRoleAsync(HttpMethod.Put, "/api/company/profile", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = "Security acceptance company", slug = new string('a', 64),
                contactEmail = "security@example.invalid", brandPrimaryColor = "#1769E0" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);

        using (var duplicate = await SendAsRoleAsync(HttpMethod.Put, "/api/company/profile", "admin",
            System.Net.Http.Json.JsonContent.Create(new { name = "Security acceptance company", slug = $"other-{_otherCompanyId:N}",
                contactEmail = "security@example.invalid", brandPrimaryColor = "#1769E0" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(uniqueSlug, await db.Companies.Where(company => company.Id == _companyId)
            .Select(company => company.Slug).SingleAsync());

        using var profileResponse = await SendAsAdminAsync("/api/company/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        using var profilePayload = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        Assert.Equal(uniqueSlug, profilePayload.RootElement.GetProperty("slug").GetString());
        Assert.Null(profilePayload.RootElement.GetProperty("tenantDomain").GetString());
    }

    private async Task<HttpResponseMessage> SendAnonymousLoginAsync(HttpClient client, string email, string password, string? twoFactorCode,
        string requestToken, string antiforgeryCookie, Guid? companyId = null, string? host = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { email, password, companyId, rememberMe = true, twoFactorCode })
        };
        request.Headers.Add("X-CSRF-TOKEN", requestToken);
        request.Headers.Add("Cookie", antiforgeryCookie);
        if (!string.IsNullOrWhiteSpace(host)) request.Headers.Host = host;
        return await client.SendAsync(request);
    }

    private static string GenerateAuthenticatorCode(string base32Secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>(); var buffer = 0; var bitCount = 0;
        foreach (var character in base32Secret.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bitCount += 5;
            if (bitCount >= 8) { bitCount -= 8; bytes.Add((byte)(buffer >> bitCount)); }
        }
        var counter = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        Span<byte> counterBytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(counterBytes, counter);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string DecodeMimeBody(string message)
    {
        var bodySeparator = message.IndexOf("\n\n", StringComparison.Ordinal);
        Assert.True(bodySeparator >= 0, "The captured SMTP message must contain a MIME body.");
        var headers = message[..bodySeparator];
        var encodedBody = message[(bodySeparator + 2)..].Replace("\r", "", StringComparison.Ordinal).Trim();
        if (headers.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(encodedBody.Where(character => !char.IsWhiteSpace(character)))));
        if (!headers.Contains("Content-Transfer-Encoding: quoted-printable", StringComparison.OrdinalIgnoreCase)) return encodedBody;

        using var decoded = new MemoryStream();
        for (var index = 0; index < encodedBody.Length; index++)
        {
            var character = encodedBody[index];
            if (character == '=' && index + 1 < encodedBody.Length && encodedBody[index + 1] == '\n') { index++; continue; }
            if (character == '=' && index + 2 < encodedBody.Length &&
                byte.TryParse(encodedBody.AsSpan(index + 1, 2), System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                decoded.WriteByte(value);
                index += 2;
                continue;
            }
            decoded.Write(Encoding.UTF8.GetBytes([character]));
        }
        return Encoding.UTF8.GetString(decoded.ToArray());
    }

    private async Task<HttpResponseMessage> SendProfilePhotoUploadAsync(byte[] bytes, string requestToken, string antiforgeryCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/photo");
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", "admin");
        request.Headers.Add("X-CSRF-TOKEN", requestToken);
        request.Headers.Add("Cookie", antiforgeryCookie);
        var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", "profile.png");
        request.Content = multipart;
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendLicenseAttachmentUploadAsync(Guid licenseId, byte[] bytes, string fileName,
        string requestToken, string antiforgeryCookie, bool isEulaDocument = false, string? eulaVersion = null)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", fileName);
        if (isEulaDocument) multipart.Add(new StringContent("true"), "isEulaDocument");
        if (eulaVersion is not null) multipart.Add(new StringContent(eulaVersion), "eulaVersion");
        return await SendAsUserAsync(HttpMethod.Post, $"/api/software-licenses/{licenseId}/attachments", _userId, _companyId,
            multipart, requestToken, antiforgeryCookie);
    }

    private async Task<HttpResponseMessage> SendMaintenanceAttachmentUploadAsync(Guid maintenanceId, byte[] bytes, string fileName,
        string requestToken, string antiforgeryCookie)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", fileName);
        return await SendAsUserAsync(HttpMethod.Post, $"/api/maintenances/{maintenanceId}/attachments", _userId, _companyId,
            multipart, requestToken, antiforgeryCookie);
    }

    private async Task<HttpResponseMessage> SendAssetAttachmentUploadAsync(Guid assetId, byte[] bytes, string fileName,
        string requestToken, string antiforgeryCookie, Guid? assetEventId = null)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", fileName);
        if (assetEventId is Guid eventId) multipart.Add(new StringContent(eventId.ToString()), "assetEventId");
        return await SendAsUserAsync(HttpMethod.Post, $"/api/assets/{assetId}/attachments", _userId, _companyId,
            multipart, requestToken, antiforgeryCookie);
    }

    private static byte[] CreateEmployeeImportWorkbook(params object[][] rows)
    {
        System.Xml.Linq.XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        System.Xml.Linq.XNamespace documentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        System.Xml.Linq.XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            void AddXml(string path, System.Xml.Linq.XDocument document)
            {
                var entry = archive.CreateEntry(path);
                using var stream = entry.Open();
                document.Save(stream);
            }

            AddXml("[Content_Types].xml", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement("{http://schemas.openxmlformats.org/package/2006/content-types}Types",
                    new System.Xml.Linq.XElement("{http://schemas.openxmlformats.org/package/2006/content-types}Default",
                        new System.Xml.Linq.XAttribute("Extension", "rels"), new System.Xml.Linq.XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new System.Xml.Linq.XElement("{http://schemas.openxmlformats.org/package/2006/content-types}Default",
                        new System.Xml.Linq.XAttribute("Extension", "xml"), new System.Xml.Linq.XAttribute("ContentType", "application/xml")),
                    new System.Xml.Linq.XElement("{http://schemas.openxmlformats.org/package/2006/content-types}Override",
                        new System.Xml.Linq.XAttribute("PartName", "/xl/workbook.xml"), new System.Xml.Linq.XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    new System.Xml.Linq.XElement("{http://schemas.openxmlformats.org/package/2006/content-types}Override",
                        new System.Xml.Linq.XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new System.Xml.Linq.XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
            AddXml("_rels/.rels", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(packageRelationships + "Relationships",
                    new System.Xml.Linq.XElement(packageRelationships + "Relationship",
                        new System.Xml.Linq.XAttribute("Id", "rId1"),
                        new System.Xml.Linq.XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new System.Xml.Linq.XAttribute("Target", "xl/workbook.xml")))));
            AddXml("xl/workbook.xml", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(spreadsheet + "workbook",
                    new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "r", documentRelationships),
                    new System.Xml.Linq.XElement(spreadsheet + "sheets",
                        new System.Xml.Linq.XElement(spreadsheet + "sheet",
                            new System.Xml.Linq.XAttribute("name", "Employees"), new System.Xml.Linq.XAttribute("sheetId", "1"),
                            new System.Xml.Linq.XAttribute(documentRelationships + "id", "rId1"))))));
            AddXml("xl/_rels/workbook.xml.rels", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(packageRelationships + "Relationships",
                    new System.Xml.Linq.XElement(packageRelationships + "Relationship",
                        new System.Xml.Linq.XAttribute("Id", "rId1"),
                        new System.Xml.Linq.XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                        new System.Xml.Linq.XAttribute("Target", "worksheets/sheet1.xml")))));
            string ColumnName(int column)
            {
                var name = "";
                while (column > 0) { column--; name = (char)('A' + column % 26) + name; column /= 26; }
                return name;
            }
            var sheetRows = rows.Select((row, rowIndex) => new System.Xml.Linq.XElement(spreadsheet + "row",
                new System.Xml.Linq.XAttribute("r", rowIndex + 1),
                row.Select((value, columnIndex) => value is double number
                    ? new System.Xml.Linq.XElement(spreadsheet + "c", new System.Xml.Linq.XAttribute("r", $"{ColumnName(columnIndex + 1)}{rowIndex + 1}"),
                        new System.Xml.Linq.XElement(spreadsheet + "v", number.ToString(CultureInfo.InvariantCulture)))
                    : new System.Xml.Linq.XElement(spreadsheet + "c", new System.Xml.Linq.XAttribute("r", $"{ColumnName(columnIndex + 1)}{rowIndex + 1}"),
                        new System.Xml.Linq.XAttribute("t", "inlineStr"), new System.Xml.Linq.XElement(spreadsheet + "is",
                            new System.Xml.Linq.XElement(spreadsheet + "t", value?.ToString() ?? ""))))));
            AddXml("xl/worksheets/sheet1.xml", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(spreadsheet + "worksheet", new System.Xml.Linq.XElement(spreadsheet + "sheetData", sheetRows))));
        }
        return output.ToArray();
    }

    private async Task<HttpResponseMessage> SendEmployeeDocumentUploadAsync(Guid employeeId, byte[] bytes, string fileName,
        string requestToken, string antiforgeryCookie)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", fileName);
        return await SendAsUserAsync(HttpMethod.Post, $"/api/employees/{employeeId}/documents", _userId, _companyId,
            multipart, requestToken, antiforgeryCookie);
    }

    private async Task<HttpResponseMessage> SendAccessoryPhotoUploadAsync(Guid accessoryId, byte[] bytes, string fileName,
        string requestToken, string antiforgeryCookie)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "file", fileName);
        return await SendAsUserAsync(HttpMethod.Post, $"/api/accessories/{accessoryId}/photo", _userId, _companyId,
            multipart, requestToken, antiforgeryCookie);
    }

    private async Task SetCompanyRoleAsync(string role)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await db.CompanyMemberships.SingleAsync(x => x.CompanyId == _companyId && x.UserId == _userId);
        membership.RoleKey = role;
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> SendAsAdminAsync(string path, Guid? companyId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", (companyId ?? _companyId).ToString());
        request.Headers.Add("X-Acceptance-Role", "admin");
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsPlatformAdminAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Platform-Admin", "true");
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsPlatformAdminRequestAsync(HttpMethod method, string path,
        HttpContent? content = null, string? requestToken = null, string? antiforgeryCookie = null, string? host = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Platform-Admin", "true");
        if (!string.IsNullOrWhiteSpace(host)) request.Headers.Host = host;
        if (!string.IsNullOrWhiteSpace(requestToken)) request.Headers.Add("X-CSRF-TOKEN", requestToken);
        if (!string.IsNullOrWhiteSpace(antiforgeryCookie)) request.Headers.Add("Cookie", antiforgeryCookie);
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsUserAsync(HttpMethod method, string path, Guid userId, Guid companyId,
        HttpContent? content = null, string? requestToken = null, string? antiforgeryCookie = null, string? role = null, string? host = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Acceptance-User", userId.ToString());
        request.Headers.Add("X-Acceptance-Company", companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", role ?? "admin");
        if (!string.IsNullOrWhiteSpace(host)) request.Headers.Host = host;
        if (!string.IsNullOrWhiteSpace(requestToken)) request.Headers.Add("X-CSRF-TOKEN", requestToken);
        if (!string.IsNullOrWhiteSpace(antiforgeryCookie)) request.Headers.Add("Cookie", antiforgeryCookie);
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsRoleAsync(HttpMethod method, string path, string role, HttpContent? content = null,
        string? requestToken = null, string? antiforgeryCookie = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", role);
        if (!string.IsNullOrWhiteSpace(requestToken)) request.Headers.Add("X-CSRF-TOKEN", requestToken);
        if (!string.IsNullOrWhiteSpace(antiforgeryCookie)) request.Headers.Add("Cookie", antiforgeryCookie);
        return await _client!.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsRoleWithHeadersAsync(HttpMethod method, string path, string role,
        HttpContent? content, string? requestToken, string? antiforgeryCookie, IReadOnlyDictionary<string, string> headers)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", role);
        if (!string.IsNullOrWhiteSpace(requestToken)) request.Headers.Add("X-CSRF-TOKEN", requestToken);
        if (!string.IsNullOrWhiteSpace(antiforgeryCookie)) request.Headers.Add("Cookie", antiforgeryCookie);
        foreach (var (name, value) in headers) request.Headers.Add(name, value);
        return await _client!.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendLoginAsync(string clientIp, string requestToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = new StringContent("{\"email\":\"missing@example.invalid\",\"password\":\"invalid\"}",
                System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Forwarded-For", clientIp);
        request.Headers.Add("X-CSRF-TOKEN", requestToken);
        return _client!.SendAsync(request);
    }

    private sealed class SyntheticIyzipayHandler : HttpMessageHandler
    {
        private string? _conversationId;
        public decimal InitializedAmount { get; private set; }
        public int RetrieveCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root = body.RootElement;
            if (request.RequestUri?.AbsolutePath == "/payment/iyzipos/checkoutform/initialize/auth/ecom")
            {
                InitializedAmount = root.GetProperty("price").GetDecimal();
                _conversationId = root.GetProperty("conversationId").GetString();
                return JsonResponse(new
                {
                    status = "success", conversationId = _conversationId, token = "synthetic-provider-token",
                    paymentPageUrl = "https://sandbox-api.iyzipay.com/checkoutform/synthetic-session"
                });
            }
            if (request.RequestUri?.AbsolutePath == "/payment/iyzipos/checkoutform/auth/ecom/detail")
            {
                RetrieveCount++;
                Assert.Equal("synthetic-provider-token", root.GetProperty("token").GetString());
                Assert.Equal(_conversationId, root.GetProperty("conversationId").GetString());
                return JsonResponse(new
                {
                    status = "success", token = "synthetic-provider-token", conversationId = _conversationId,
                    basketId = _conversationId, price = InitializedAmount, paidPrice = InitializedAmount,
                    currency = "TRY", fraudStatus = 1, paymentStatus = "SUCCESS", paymentId = "synthetic-payment-id"
                });
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage JsonResponse<T>(T payload) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }
}

internal sealed class AcceptanceHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Acceptance-User"], out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Acceptance-Role"].ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Acceptance User")
        };
        if (Guid.TryParse(Request.Headers["X-Acceptance-Company"], out var companyId))
            claims.Add(new Claim("company_id", companyId.ToString()));
        if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim("company_role", role));
        if (string.Equals(Request.Headers["X-Acceptance-Platform-Admin"], "true", StringComparison.OrdinalIgnoreCase))
            claims.Add(new Claim("platform_admin", "true"));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
