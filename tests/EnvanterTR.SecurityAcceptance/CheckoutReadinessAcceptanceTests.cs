using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets()
    {
        const string callback = "https://envantertr.com/api/checkout/callback";
        int publishedBefore;
        var addedPlan = new SubscriptionPlan { Code = $"health-{Guid.NewGuid():N}", Name = "Health acceptance plan", DeploymentKind = "cloud", IsActive = true, IsPublic = true };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            publishedBefore = await db.SubscriptionPlans.CountAsync(plan => plan.IsActive && plan.IsPublic && plan.DeploymentKind == "cloud");
            db.SubscriptionPlans.Add(addedPlan);
            await db.SaveChangesAsync();
        }
        using (var companyAdmin = await SendAsAdminAsync("/api/platform/system/health"))
            Assert.Equal(HttpStatusCode.Forbidden, companyAdmin.StatusCode);

        var options = _factory!.Services.GetRequiredService<IOptions<IyzipayOptions>>().Value;
        var cases = new[]
        {
            (Api: "https://sandbox-api.iyzipay.com", Callback: callback, Key: false, CallbackValid: true, ApiValid: true, Environment: "sandbox"),
            (Api: "https://sandbox-api.iyzipay.com", Callback: "", Key: true, CallbackValid: false, ApiValid: true, Environment: "sandbox"),
            (Api: "https://sandbox-api.iyzipay.com", Callback: "https://example.test/api/checkout/callback", Key: true, CallbackValid: false, ApiValid: true, Environment: "sandbox"),
            (Api: "https://api.iyzipay.com:444", Callback: callback, Key: true, CallbackValid: true, ApiValid: false, Environment: "unknown"),
            (Api: "https://api.iyzipay.com/?key=private-value", Callback: callback, Key: true, CallbackValid: true, ApiValid: false, Environment: "unknown"),
            (Api: "https://api.iyzipay.com/#fragment", Callback: callback, Key: true, CallbackValid: true, ApiValid: false, Environment: "unknown"),
            (Api: "https://sandbox-api.iyzipay.com", Callback: callback, Key: true, CallbackValid: true, ApiValid: true, Environment: "sandbox"),
            (Api: "https://api.iyzipay.com", Callback: callback, Key: true, CallbackValid: true, ApiValid: true, Environment: "production")
        };
        foreach (var item in cases)
        {
            options.ApiKey = item.Key ? "health-api-key-marker" : "";
            options.SecretKey = "health-secret-marker";
            options.CallbackUrl = item.Callback;
            options.BaseUrl = item.Api;
            using var response = await SendAsPlatformAdminAsync("/api/platform/system/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("health-api-key-marker", body, StringComparison.Ordinal);
            Assert.DoesNotContain("health-secret-marker", body, StringComparison.Ordinal);
            Assert.DoesNotContain("private-value", body, StringComparison.Ordinal);
            using var payload = JsonDocument.Parse(body);
            var readiness = payload.RootElement.GetProperty("integrations").GetProperty("checkout");
            Assert.Equal(item.Key, readiness.GetProperty("credentialsConfigured").GetBoolean());
            Assert.Equal(item.CallbackValid, readiness.GetProperty("callbackValid").GetBoolean());
            Assert.Equal(item.ApiValid, readiness.GetProperty("apiEndpointValid").GetBoolean());
            Assert.Equal(item.Environment, readiness.GetProperty("environment").GetString());
            Assert.Equal(publishedBefore + 1, readiness.GetProperty("publishedCloudPlans").GetInt32());
            Assert.True(readiness.GetProperty("catalogReady").GetBoolean());
            using var publicResponse = await SendAsAdminAsync("/api/checkout/status");
            Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
            using var publicPayload = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
            var available = publicPayload.RootElement.GetProperty("available").GetBoolean();
            Assert.Equal(item.Key && item.CallbackValid && item.ApiValid, available);
            Assert.Equal(available, readiness.GetProperty("configurationReady").GetBoolean());
            Assert.False(publicPayload.RootElement.TryGetProperty("checkout", out _));
        }
    }
}
