using System.Net;
using System.Threading.RateLimiting;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class TrustedClientIpTests
{
    private static readonly IPNetwork[] Ingress = TrustedClientIp.ParseNetworks("10.0.2.0/24");

    [Theory]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", "198.51.100.9", true, "198.51.100.9")]
    [InlineData("::ffff:10.0.2.4", "::ffff:173.245.48.12, ::ffff:10.0.2.5", "::ffff:198.51.100.9", true, "198.51.100.9")]
    [InlineData("10.0.2.4", "2606:4700::123, 10.0.2.5", "2001:db8::9", true, "2001:db8::9")]
    [InlineData("10.0.2.4", "203.0.113.8, 10.0.2.5", "198.51.100.9", true, "203.0.113.8")]
    [InlineData("10.0.2.4", "forged-left-prefix, 203.0.113.8, 10.0.2.5", "198.51.100.9", true, "203.0.113.8")]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", "198.51.100.9", false, "173.245.48.12")]
    [InlineData("203.0.113.8", "173.245.48.12, 10.0.2.5", "198.51.100.9", true, null)]
    [InlineData("10.0.3.4", "173.245.48.12, 10.0.2.5", "198.51.100.9", true, null)]
    [InlineData("10.0.2.4", "173.245.48.12, 203.0.113.8", "198.51.100.9", true, null)]
    [InlineData("10.0.2.4", "173.245.48.12", "198.51.100.9", true, null)]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", "198.51.100.9, 203.0.113.8", true, null)]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", "", true, null)]
    [InlineData("10.0.2.4", "bad, 10.0.2.5", "198.51.100.9", true, null)]
    public void Resolves_only_a_verified_ingress_chain(string peer, string forwarded, string connecting,
        bool cloudflare, string? expected)
    {
        Assert.Equal(expected, TrustedClientIp.Resolve(IPAddress.Parse(peer), forwarded, connecting, Ingress, cloudflare)?.ToString());
    }

    [Fact]
    public void Missing_network_configuration_disables_ip_forwarding()
    {
        Assert.Null(TrustedClientIp.Resolve(IPAddress.Parse("10.0.2.4"), "173.245.48.12, 10.0.2.5",
            "198.51.100.9", TrustedClientIp.ParseNetworks(null), true));
    }

    [Theory]
    [InlineData("0.0.0.0/0")]
    [InlineData("::/0")]
    [InlineData("invalid")]
    public void Rejects_global_or_invalid_trust(string network) =>
        Assert.Throws<InvalidOperationException>(() => TrustedClientIp.ParseNetworks(network));

    [Theory]
    [InlineData("login", 10, true)]
    [InlineData("login", 10, false)]
    [InlineData("step-up", 5, true)]
    [InlineData("step-up", 5, false)]
    [InlineData("customer-api", 120, true)]
    [InlineData("customer-api", 120, false)]
    [InlineData("checkout", 5, true)]
    [InlineData("checkout", 5, false)]
    public async Task Production_named_policy_uses_verified_client_and_ignores_raw_headers(string policy, int permitLimit, bool trustedCloudflare)
    {
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services => { services.AddRouting(); services.AddRateLimiter(ClientRateLimits.Configure); })
            .Configure(app =>
            {
                app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.2.4"); return next(); });
                app.UseTrustedClientIp(Ingress, true);
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(endpoints => endpoints.MapGet("/", context => context.Response.WriteAsync("ok")).RequireRateLimiting(policy));
            }));
        using var client = server.CreateClient();
        async Task<HttpResponseMessage> Send(string address)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Forwarded-For", trustedCloudflare ? "173.245.48.12, 10.0.2.5" : "203.0.113.9, 10.0.2.5");
            request.Headers.Add("CF-Connecting-IP", address);
            return await client.SendAsync(request);
        }
        for (var index = 0; index < permitLimit; index++)
        {
            using var allowed = await Send("198.51.100.1");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        using var secondClient = await Send("198.51.100.2");
        Assert.Equal(trustedCloudflare ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, secondClient.StatusCode);
        using var exhaustedClient = await Send("198.51.100.1");
        Assert.Equal(HttpStatusCode.TooManyRequests, exhaustedClient.StatusCode);
    }

    [Theory]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", true, "198.51.100.1", true)]
    [InlineData("203.0.113.9", "173.245.48.12, 10.0.2.5", true, "203.0.113.9", false)]
    [InlineData("10.0.2.4", "203.0.113.9, 10.0.2.5", true, "203.0.113.9", false)]
    [InlineData("10.0.2.4", "173.245.48.12, 10.0.2.5", false, "10.0.2.4", false)]
    public async Task Http_pipeline_and_rate_limits_use_only_the_verified_address(string peer, string forwarded,
        bool configureTrust, string expectedFirstAddress, bool separateClients)
    {
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services => services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress!.ToString(), _ =>
                        new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            }))
            .Configure(app =>
            {
                app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse(peer); return next(); });
                app.UseTrustedClientIp(configureTrust ? Ingress : [], true);
                app.UseRateLimiter();
                app.Run(context => context.Response.WriteAsync(context.Connection.RemoteIpAddress!.ToString()));
            }));
        using var client = server.CreateClient();
        async Task<HttpResponseMessage> Send(string address)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Forwarded-For", forwarded);
            request.Headers.Add("CF-Connecting-IP", address);
            return await client.SendAsync(request);
        }
        using var first = await Send("198.51.100.1");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(expectedFirstAddress, await first.Content.ReadAsStringAsync());
        using var second = await Send("198.51.100.2");
        Assert.Equal(separateClients ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, second.StatusCode);
        using var repeated = await Send("198.51.100.1");
        Assert.Equal(HttpStatusCode.TooManyRequests, repeated.StatusCode);
    }
}
