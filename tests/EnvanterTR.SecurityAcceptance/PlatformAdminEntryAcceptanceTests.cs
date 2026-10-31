using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Root_admin_entry_redirects_to_existing_platform_login_while_tenant_entry_stays_hidden()
    {
        using var browser = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://envantertr.com"), AllowAutoRedirect = false
        });
        foreach (var host in new[] { "envantertr.com", "www.envantertr.com", "localhost" })
        foreach (var path in new[] { "/admin", "/admin/", "/admin?view=ignored" })
        {
            using var response = await browser.GetAsync($"https://{host}{path}");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/platform-admin.html", response.Headers.Location?.OriginalString);
            Assert.DoesNotContain("Set-Cookie", response.Headers.Select(h => h.Key));
        }
        foreach (var host in new[] { "north.envantertr.com", "south.envantertr.com" })
        foreach (var path in new[] { "/admin", "/admin/" })
        {
            using var response = await browser.GetAsync($"https://{host}{path}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
    }
}
