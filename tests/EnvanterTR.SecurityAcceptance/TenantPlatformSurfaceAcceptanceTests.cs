using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Tenant_hosts_hide_platform_pages_assets_and_apis_before_static_files()
    {
        using var browser = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://envantertr.com"), AllowAutoRedirect = false
        });
        var pages = new[] { "/platform-admin.html", "/platform-setup.html" };
        foreach (var page in pages)
        {
            using var root = await browser.GetAsync(page);
            Assert.Equal(HttpStatusCode.OK, root.StatusCode);
            Assert.Contains("<html", await root.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
        foreach (var host in new[] { "north.envantertr.com", "south.envantertr.com", "north.envantertr.com." })
        foreach (var path in new[]
        {
            "/platform-admin.html", "/platform-setup.html", "/PLATFORM-ADMIN.HTML", "/admin", "/admin/companies",
            "/platform-admin-members.js", "/platform-admin-subscriptions.js", "/platform-admin-demo-requests.js",
            "/platform-admin-payment-follow-ups.js", "/api/platform/overview", "/api/platform/bootstrap/status"
        })
        {
            using var denied = await browser.GetAsync($"https://{host}{path}");
            Assert.True(denied.StatusCode == HttpStatusCode.NotFound, $"{host}{path}: expected404, actual{(int)denied.StatusCode}");
            Assert.DoesNotContain("<html", await denied.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
        using var companyLogin = await browser.GetAsync("https://north.envantertr.com/");
        Assert.Equal(HttpStatusCode.OK, companyLogin.StatusCode);
        Assert.Contains("loginForm", await companyLogin.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        foreach (var host in new[] { "envantertr.com", "north.envantertr.com", "localhost" })
        {
            using var setup = await browser.GetAsync($"https://{host}/api/setup/status");
            Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
            using var data = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
            Assert.Equal(host == "north.envantertr.com" ? "https://envantertr.com/platform-admin.html" : "/platform-admin.html",
                data.RootElement.GetProperty("platformLoginUrl").GetString());
        }
    }
}
