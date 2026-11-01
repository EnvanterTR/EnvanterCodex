using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Smtp_readiness_matches_trial_gate_rejects_malformed_settings_and_exposes_no_secrets()
    {
        var plan = await SeedTrialPlan();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        var configuration = _factory!.Services.GetRequiredService<IConfiguration>();
        var baseline = new Dictionary<string, string?> {
            ["ASPNETCORE_ENVIRONMENT"] = "Production", ["CloudTrials:Enabled"] = "true", ["Email:Smtp:Host"] = "smtp-marker.example.invalid",
            ["Email:Smtp:Port"] = "587", ["Email:Smtp:EnableSsl"] = "true",
            ["Email:FromAddress"] = "sender-marker@example.invalid", ["Email:PublicBaseUrl"] = "https://portal.example.invalid",
            ["Email:Smtp:Username"] = "", ["Email:Smtp:Password"] = ""
        };
        var previous = baseline.Keys.ToDictionary(key => key, key => configuration[key]);
        var cases = new[] {
            (Key: "Email:Smtp:Host", Value: "smtp://invalid", Flag: "hostValid"),
            (Key: "Email:Smtp:Port", Value: "invalid", Flag: "portValid"),
            (Key: "Email:Smtp:Port", Value: "0", Flag: "portValid"),
            (Key: "Email:FromAddress", Value: "not-an-email", Flag: "senderValid"),
            (Key: "Email:PublicBaseUrl", Value: "http://portal.example.invalid", Flag: "publicBaseUrlValid"),
            (Key: "Email:PublicBaseUrl", Value: "https://user:private-marker@portal.example.invalid", Flag: "publicBaseUrlValid"),
            (Key: "Email:Smtp:Username", Value: "smtp-user-marker", Flag: "authenticationConsistent"),
            (Key: "Email:Smtp:Password", Value: "smtp-password-marker", Flag: "authenticationConsistent"),
            (Key: "Email:Smtp:EnableSsl", Value: "invalid", Flag: "tlsSettingValid"),
            (Key: "", Value: "", Flag: "")
        };
        try
        {
            foreach (var item in cases)
            {
                foreach (var entry in baseline) configuration[entry.Key] = entry.Value;
                if (item.Key.Length > 0) configuration[item.Key] = item.Value;
                var expected = item.Key.Length == 0;
                using var status = await _client!.GetAsync("/api/trials/status");
                Assert.Equal(HttpStatusCode.OK, status.StatusCode);
                using var publicPayload = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
                Assert.Equal(expected, publicPayload.RootElement.GetProperty("available").GetBoolean());
                using var health = await SendAsPlatformAdminAsync("/api/platform/system/health");
                Assert.Equal(HttpStatusCode.OK, health.StatusCode);
                var body = await health.Content.ReadAsStringAsync();
                foreach (var marker in new[] { "smtp-marker", "sender-marker", "smtp-user-marker", "smtp-password-marker", "private-marker" })
                    Assert.DoesNotContain(marker, body, StringComparison.Ordinal);
                using var healthPayload = JsonDocument.Parse(body);
                var integrations = healthPayload.RootElement.GetProperty("integrations");
                Assert.Equal(expected, integrations.GetProperty("smtpConfigured").GetBoolean());
                Assert.Equal(expected, integrations.GetProperty("smtp").GetProperty("configurationReady").GetBoolean());
                if (!expected) Assert.False(integrations.GetProperty("smtp").GetProperty(item.Flag).GetBoolean());
                if (item.Key == "Email:Smtp:Port" && item.Value == "invalid")
                {
                    await ConfigureTrialCsrf(_client);
                    using var denied = await _client.PostAsJsonAsync("/api/trials/request", new RequestCloudTrial(
                        "Closed trial", $"smtp-{Guid.NewGuid():N}", "Trial owner", "closed@example.invalid", plan.Id, true, CloudTrialEndpoints.NoticeVersion));
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);
                }
            }
            Assert.Empty(_smtpCapture.Messages);
        }
        finally { foreach (var entry in previous) configuration[entry.Key] = entry.Value; }
    }
}
