using System.Net;
using EnvanterTR.Web.Data;
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
}
