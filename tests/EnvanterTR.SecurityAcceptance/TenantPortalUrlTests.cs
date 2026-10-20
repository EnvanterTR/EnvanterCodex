using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class TenantPortalUrlTests
{
    [Fact]
    public async Task TenantLoginPathRedirectsToPortalRootLoginPage()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("TenantRouting:RootDomain", "envantertr.com"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/giris");
        request.Headers.Host = "farma.envantertr.com";

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        using var portal = new HttpRequestMessage(HttpMethod.Get, "/");
        portal.Headers.Host = "farma.envantertr.com";
        using var portalResponse = await client.SendAsync(portal);
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        Assert.Contains("id=\"loginForm\"", await portalResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("farma", "envantertr.com", "https://farma.envantertr.com/")]
    [InlineData("FARMA-1", ".EnvanterTR.com.", "https://farma-1.envantertr.com/")]
    public void BuildPortalUrl_ValidatesTenantSlugAndCreatesHttpsUrl(string slug, string domain, string expected)
    {
        Assert.Equal(expected, TenantHostResolver.BuildPortalUrl(slug, domain));
    }

    [Theory]
    [InlineData("admin", "envantertr.com")]
    [InlineData("farma", "envantertr.com/path")]
    [InlineData("farma", "user@envantertr.com")]
    public void BuildPortalUrl_RejectsReservedOrUnsafeHostInput(string slug, string domain)
    {
        Assert.Throws<ArgumentException>(() => TenantHostResolver.BuildPortalUrl(slug, domain));
    }
}
