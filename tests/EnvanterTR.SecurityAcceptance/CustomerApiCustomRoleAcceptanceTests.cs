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
    public async Task Customer_api_custom_roles_apply_current_permissions_to_issuance_writes_and_field_capabilities()
    {
        var roleKey = "api_" + _userId.ToString("N");
        var roleId = Guid.NewGuid();
        var categoryId = await GetOwnCategoryIdAsync();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CompanyCustomRoles.Add(new CompanyCustomRole { Id = roleId, CompanyId = _companyId,
                RoleKey = roleKey, Name = "API custom writer", NormalizedName = "API CUSTOM WRITER",
                CreatedByUserId = _userId, PermissionsJson = CompanyRoleAuthorization.SerializePermissionKeys(["assets.read", "assets.write"]) });
            await db.CompanyMemberships.Where(member => member.CompanyId == _companyId && member.UserId == _userId)
                .ExecuteUpdateAsync(update => update.SetProperty(member => member.RoleKey, roleKey));
            db.AssetCustomFieldDefinitions.AddRange(
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = "api_label", Name = "API label", FieldType = "text" },
                new AssetCustomFieldDefinition { CompanyId = _companyId, Key = "api_secret", Name = "API secret", FieldType = "sensitive" });
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsUserAsync(HttpMethod.Get, "/api/session/csrf", _userId, _companyId, role: roleKey);
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var csrfToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using (var escalation = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/tokens", _userId, _companyId,
            JsonContent.Create(new { name = "Must not escalate", scopes = new[] { "people:read" } }), csrfToken, cookie, roleKey))
            Assert.Equal(HttpStatusCode.Forbidden, escalation.StatusCode);
        using var issued = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/tokens", _userId, _companyId,
            JsonContent.Create(new { name = "Custom writer", scopes = new[] { "assets:read", "assets:write", "accessories:read" } }), csrfToken, cookie, roleKey);
        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        using var issuedPayload = JsonDocument.Parse(await issued.Content.ReadAsStringAsync());
        var secret = issuedPayload.RootElement.GetProperty("secret").GetString()!;
        async Task<HttpResponseMessage> Bearer(HttpMethod method, string path, object? body = null)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await _client!.SendAsync(request);
        }
        using (var assets = await Bearer(HttpMethod.Get, "/api/v1/assets")) Assert.Equal(HttpStatusCode.OK, assets.StatusCode);
        using (var stock = await Bearer(HttpMethod.Get, "/api/v1/accessories")) Assert.Equal(HttpStatusCode.OK, stock.StatusCode);
        using (var people = await Bearer(HttpMethod.Get, "/api/v1/employees")) Assert.Equal(HttpStatusCode.Forbidden, people.StatusCode);
        using (var fields = await Bearer(HttpMethod.Get, "/api/v1/asset-custom-fields"))
        {
            Assert.Equal(HttpStatusCode.OK, fields.StatusCode);
            using var payload = JsonDocument.Parse(await fields.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.EnumerateArray().Single(field => field.GetProperty("key").GetString() == "api_label").GetProperty("canWriteValue").GetBoolean());
            Assert.False(payload.RootElement.EnumerateArray().Single(field => field.GetProperty("key").GetString() == "api_secret").GetProperty("canWriteValue").GetBoolean());
        }
        using (var sensitiveWrite = await Bearer(HttpMethod.Post, "/api/v1/assets", new { assetTag = "API-CUSTOM-SENSITIVE-DENIED",
            name = "Denied", categoryId, customFields = new Dictionary<string, string> { ["api_secret"] = "synthetic-value" } }))
            Assert.Equal(HttpStatusCode.Forbidden, sensitiveWrite.StatusCode);
        using var created = await Bearer(HttpMethod.Post, "/api/v1/assets", new { assetTag = "API-CUSTOM-WRITER",
            name = "Created by custom role", categoryId, customFields = new Dictionary<string, string> { ["api_label"] = "integration" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdPayload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var assetId = createdPayload.RootElement.GetProperty("id").GetGuid();
        var version = createdPayload.RootElement.GetProperty("updatedAt").GetDateTimeOffset();
        var updateBody = new { assetTag = "API-CUSTOM-WRITER", name = "Updated by custom role", categoryId, expectedUpdatedAt = version };
        using (var updated = await Bearer(HttpMethod.Put, $"/api/v1/assets/{assetId}", updateBody))
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using (var stale = await Bearer(HttpMethod.Put, $"/api/v1/assets/{assetId}", updateBody))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal("Updated by custom role", await db.Assets.Where(asset => asset.Id == assetId).Select(asset => asset.Name).SingleAsync());
            Assert.True(await db.AuditEntries.AnyAsync(entry => entry.TargetId == assetId && entry.Action == "asset.api-created"));
            Assert.False(await db.Assets.AnyAsync(asset => asset.AssetTag == "API-CUSTOM-SENSITIVE-DENIED"));
            await db.CompanyCustomRoles.Where(role => role.Id == roleId).ExecuteUpdateAsync(update =>
                update.SetProperty(role => role.PermissionsJson, CompanyRoleAuthorization.SerializePermissionKeys(new[] { "assets.read" })));
        }
        using (var denied = await Bearer(HttpMethod.Put, $"/api/v1/assets/{assetId}", updateBody))
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using (var fields = await Bearer(HttpMethod.Get, "/api/v1/asset-custom-fields"))
        {
            Assert.Equal(HttpStatusCode.OK, fields.StatusCode);
            using var payload = JsonDocument.Parse(await fields.Content.ReadAsStringAsync());
            Assert.All(payload.RootElement.EnumerateArray(), field => Assert.False(field.GetProperty("canWriteValue").GetBoolean()));
        }
        using (var deniedIssue = await SendAsUserAsync(HttpMethod.Post, "/api/integrations/tokens", _userId, _companyId,
            JsonContent.Create(new { name = "Must not regain write", scopes = new[] { "assets:write" } }), csrfToken, cookie, roleKey))
            Assert.Equal(HttpStatusCode.Forbidden, deniedIssue.StatusCode);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.CompanyCustomRoles.Where(role => role.Id == roleId).ExecuteUpdateAsync(update => update.SetProperty(role => role.IsActive, false));
            db.CompanyCustomRoles.Add(new CompanyCustomRole { CompanyId = _otherCompanyId, RoleKey = roleKey,
                Name = "Foreign role", NormalizedName = "FOREIGN ROLE", CreatedByUserId = _userId,
                PermissionsJson = CompanyRoleAuthorization.SerializePermissionKeys(["assets.read", "assets.write"]) });
            await db.SaveChangesAsync();
        }
        using var disabled = await Bearer(HttpMethod.Get, "/api/v1/assets");
        Assert.Equal(HttpStatusCode.Unauthorized, disabled.StatusCode);
    }
}
