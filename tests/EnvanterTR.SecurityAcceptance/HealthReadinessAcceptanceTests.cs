using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class HealthReadinessAcceptanceTests
{
    [Fact]
    public async Task ReadinessFailsClosedWhenDatabaseIsNotConfigured()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:EnvanterTR", string.Empty));
        using var client = factory.CreateClient();

        using var liveness = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        using var body = JsonDocument.Parse(await readiness.Content.ReadAsStringAsync());
        Assert.Equal("not-ready", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadinessFailsClosedWhenDatabaseCannotBeReached()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:EnvanterTR",
                "Host=127.0.0.1;Port=1;Database=readiness;Username=readiness;Password=unused;Timeout=1;Command Timeout=1"));
        using var client = factory.CreateClient();

        using var readiness = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        using var body = JsonDocument.Parse(await readiness.Content.ReadAsStringAsync());
        Assert.Equal("not-ready", body.RootElement.GetProperty("status").GetString());
    }
}
