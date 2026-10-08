using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class TenantSessionAcceptanceTests : IAsyncLifetime
{
    private const string TestScheme = "SecurityAcceptance";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _otherCompanyId = Guid.NewGuid();
    private readonly Guid _ownAssetId = Guid.NewGuid();
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
    private readonly Guid _otherCompanyEmployeeId = Guid.NewGuid();
    private string? _previousConnectionString;
    private string? _previousForwardedHeadersSetting;
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
                    ["ForwardedHeaders:Enabled"] = "true"
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
            new Asset { Id = _ownAssetId, CompanyId = _companyId, AssetTag = "OWN-1", Name = "Own asset", CategoryId = ownCategory.Id },
            new Asset { Id = _otherCompanyAssetId, CompanyId = _otherCompanyId, AssetTag = "OTHER-1", Name = "Other tenant asset", CategoryId = otherCategory.Id });
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
            NormalizedUserName = $"ACCEPTANCE-{_userId:N}",
            Email = $"acceptance-{_userId:N}@example.invalid",
            NormalizedEmail = $"ACCEPTANCE-{_userId:N}@EXAMPLE.INVALID",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DisplayName = "Security Acceptance User"
        });
        db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _companyId, UserId = _userId, RoleKey = "admin" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Login_rate_limit_partitions_by_forwarded_client_ip()
    {
        using var csrfResponse = await _client!.GetAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
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
        Environment.SetEnvironmentVariable("ConnectionStrings__EnvanterTR", _previousConnectionString);
        Environment.SetEnvironmentVariable("ForwardedHeaders__Enabled", _previousForwardedHeadersSetting);
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

    private async Task<HttpResponseMessage> SendAsAdminAsync(string path, Guid? companyId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Acceptance-User", _userId.ToString());
        request.Headers.Add("X-Acceptance-Company", (companyId ?? _companyId).ToString());
        request.Headers.Add("X-Acceptance-Role", "admin");
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
