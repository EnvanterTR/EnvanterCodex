using System.Net;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    private string? _previousConnectionString;
    private string? _previousForwardedHeadersSetting;
    private readonly SmtpCaptureServer _smtpCapture = new();
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), "envantertr-security-media-" + Guid.NewGuid().ToString("N"));
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

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
                    ["Email:PublicBaseUrl"] = "https://portal.example.invalid"
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
            new Company { Id = _companyId, Name = "Security acceptance company", IsActive = true },
            new Company { Id = _otherCompanyId, Name = "Other security acceptance company", IsActive = true });
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
        db.Accessories.AddRange(
            new Accessory { Id = _ownAccessoryId, CompanyId = _companyId, Name = $"OWN-SECURITY-ACCESSORY-{_userId:N}", TotalQuantity = 5 },
            new Accessory { Id = _otherCompanyAccessoryId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-ACCESSORY-{_userId:N}", TotalQuantity = 7 });
        db.Consumables.AddRange(
            new Consumable { Id = _ownConsumableId, CompanyId = _companyId, Name = $"OWN-SECURITY-CONSUMABLE-{_userId:N}", TotalQuantity = 5 },
            new Consumable { Id = _otherCompanyConsumableId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-CONSUMABLE-{_userId:N}", TotalQuantity = 7 });
        db.InventoryComponents.AddRange(
            new InventoryComponent { Id = _ownComponentId, CompanyId = _companyId, Name = $"OWN-SECURITY-COMPONENT-{_userId:N}", TotalQuantity = 5 },
            new InventoryComponent { Id = _otherCompanyComponentId, CompanyId = _otherCompanyId, Name = $"OTHER-SECURITY-COMPONENT-{_userId:N}", TotalQuantity = 7 });
        db.Employees.AddRange(
            new Employee { Id = _ownEmployeeId, CompanyId = _companyId, EmployeeNumber = $"OWN-{_userId:N}", DisplayName = $"OWN-SECURITY-EMPLOYEE-{_userId:N}" },
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
        await db.SaveChangesAsync();
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
            employeeName = "", location = "Merkez", sortBy = "name", sortDirection = "desc"
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
            ("/api/requests", HttpStatusCode.OK),
            ("/api/reports/warranties", HttpStatusCode.OK),
            ("/api/assets/export.csv", HttpStatusCode.OK)
        };

        foreach (var (path, expectedStatus) in endpoints)
        {
            using var response = await SendAsAdminAsync(path);
            var detail = response.StatusCode == expectedStatus ? "" : await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expectedStatus,
                $"{path} should respond with {(int)expectedStatus} when paging and optional filters are omitted, but returned {(int)response.StatusCode}: {detail}");
        }
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

        using var movementsExportResponse = await SendAsAdminAsync("/api/inventory/movements?export=true");
        Assert.Equal(HttpStatusCode.OK, movementsExportResponse.StatusCode);
        var movementsCsv = await movementsExportResponse.Content.ReadAsStringAsync();
        Assert.Contains($"OWN-SECURITY-ACCESSORY-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.Contains($"OWN-SECURITY-CONSUMABLE-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.Contains($"OWN-SECURITY-COMPONENT-{_userId:N}", movementsCsv, StringComparison.Ordinal);
        Assert.False(movementsCsv.Contains("OTHER-SECURITY-", StringComparison.Ordinal));
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
            System.Net.Http.Json.JsonContent.Create(new { approve = true, note = "Approved in acceptance" }), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var approvedPayload = JsonDocument.Parse(await approved.Content.ReadAsStringAsync());
        Assert.Equal("Approved", approvedPayload.RootElement.GetProperty("status").GetString());

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

        using var crossTenant = await SendAsAdminAsync("/api/reservations", _otherCompanyId);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenant.StatusCode);
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AssetReservations.CountAsync(x => x.CompanyId == _companyId && x.AssetId == _ownAssetId));
        Assert.Equal("Completed", await db.AssetReservations.Where(x => x.Id == immediateReservationId).Select(x => x.Status).SingleAsync());
        Assert.Equal(0, await db.AssetReservations.CountAsync(x => x.CompanyId == _otherCompanyId));
        var foreignReservationId = Guid.NewGuid();
        db.AssetReservations.Add(new AssetReservation { Id = foreignReservationId, CompanyId = _otherCompanyId,
            AssetId = _otherCompanyAssetId, EmployeeId = _otherCompanyEmployeeId, RequestedByUserId = _userId,
            StartsOn = today, EndsOn = today.AddDays(1), Status = "Approved" });
        await db.SaveChangesAsync();
        using (var foreignNotification = await SendAsRoleAsync(HttpMethod.Put, "/api/notifications/states", "admin",
                   System.Net.Http.Json.JsonContent.Create(new { kind = "reservation", recordId = foreignReservationId, action = "dismiss" }), requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignNotification.StatusCode);
        Assert.Equal(1, await db.AssetReservations.CountAsync(x => x.CompanyId == _otherCompanyId));
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
            db.AssetReservations.Add(new AssetReservation { CompanyId = _companyId, AssetId = _ownAssetId,
                EmployeeId = _ownEmployeeId, RequestedByUserId = _userId, StartsOn = reservationStart,
                EndsOn = reservationStart.AddDays(2), Status = "Approved" });
            await db.SaveChangesAsync();
            await emailService.RunOnceAsync(CancellationToken.None);
            var deliveredDigest = Assert.Single(_smtpCapture.Messages);
            Assert.Contains($"To: acceptance-{_userId:N}@example.invalid", deliveredDigest, StringComparison.OrdinalIgnoreCase);
            var digestBody = DecodeMimeBody(deliveredDigest);
            Assert.Contains("Yeni zimmet", digestBody, StringComparison.Ordinal);
            Assert.Contains("OWN-1", digestBody, StringComparison.Ordinal);
            Assert.Contains("OWN-SECURITY-EMPLOYEE", digestBody, StringComparison.Ordinal);
            Assert.Contains("Varlık rezervasyonu", digestBody, StringComparison.Ordinal);
            Assert.Contains(WebUtility.HtmlEncode("Kullanım ömrü sonu"), digestBody, StringComparison.Ordinal);
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

        var unsafeCsv = $"{headers},Asset Notes\r\n{row},must not be discarded";
        using var unsafePreview = await SendAsRoleAsync(HttpMethod.Post, "/api/assets/import/preview.csv", "admin",
            new StringContent(unsafeCsv, Encoding.UTF8, "text/csv"), requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsafePreview.StatusCode);
        Assert.Contains("Asset Notes", await unsafePreview.Content.ReadAsStringAsync(), StringComparison.Ordinal);
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

        using var exportResponse = await SendAsAdminAsync("/api/employees/export.csv");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var csv = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains($"OWN-SECURITY-EMPLOYEE-{_userId:N}", csv, StringComparison.Ordinal);
        Assert.False(csv.Contains($"OTHER-SECURITY-EMPLOYEE-{_userId:N}", StringComparison.Ordinal));
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
    public async Task Maintenance_attachments_are_validated_audited_and_included_in_company_archive()
    {
        var maintenanceId = Guid.NewGuid();
        var otherMaintenanceId = Guid.NewGuid();
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
                    OriginalFileName = "asset-own.pdf", StorageFileName = $"{ownAssetAttachmentId:N}.pdf", ContentType = "application/pdf", Length = assetPdfBytes.Length },
                new AssetAttachment { Id = otherCompanyAssetAttachmentId, CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId,
                    OriginalFileName = "asset-other.pdf", StorageFileName = $"{otherCompanyAssetAttachmentId:N}.pdf", ContentType = "application/pdf", Length = assetPdfBytes.Length });
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
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
                Status = "InProgress", Currency = "TRY", CreatedAt = DateTimeOffset.UtcNow,
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
            orderNumber = accessoryOrderNumber, supplier = "Accessory finance supplier", orderDate = today, currency = "TRY",
            taxRatePercent = 10m, discountAmount = 5m, freightAmount = 2m,
            items = new[] { new { accessoryId = _ownAccessoryId, quantity = 4, unitCost = 20m } }
        });
        using var accessoryOrderCreate = await SendAsRoleAsync(HttpMethod.Post, "/api/accessory-orders", "admin", accessoryOrderPayload, requestToken, antiforgeryCookie);
        Assert.Equal(HttpStatusCode.Created, accessoryOrderCreate.StatusCode);
        using var accessoryOrderCreated = JsonDocument.Parse(await accessoryOrderCreate.Content.ReadAsStringAsync());
        var accessoryOrderId = accessoryOrderCreated.RootElement.GetProperty("id").GetGuid();
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

    private async Task<HttpResponseMessage> SendAnonymousLoginAsync(HttpClient client, string email, string password, string? twoFactorCode,
        string requestToken, string antiforgeryCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { email, password, companyId = _companyId, rememberMe = true, twoFactorCode })
        };
        request.Headers.Add("X-CSRF-TOKEN", requestToken);
        request.Headers.Add("Cookie", antiforgeryCookie);
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

    private async Task<HttpResponseMessage> SendAsUserAsync(HttpMethod method, string path, Guid userId, Guid companyId,
        HttpContent? content = null, string? requestToken = null, string? antiforgeryCookie = null, string? role = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add("X-Acceptance-User", userId.ToString());
        request.Headers.Add("X-Acceptance-Company", companyId.ToString());
        request.Headers.Add("X-Acceptance-Role", role ?? "admin");
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
}

internal sealed class AcceptanceHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Acceptance-User"], out var userId) ||
            !Guid.TryParse(Request.Headers["X-Acceptance-Company"], out var companyId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Acceptance-Role"].ToString();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Acceptance User"),
            new("company_id", companyId.ToString()),
            new("company_role", role)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
