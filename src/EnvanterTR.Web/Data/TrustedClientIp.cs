using System.Net;

namespace EnvanterTR.Web.Data;

public static class TrustedClientIp
{
    // Official https://www.cloudflare.com/ips-v4 and /ips-v6, verified 2026-10-21.
    private static readonly IPNetwork[] CloudflareNetworks =
        ("173.245.48.0/20,103.21.244.0/22,103.22.200.0/22,103.31.4.0/22,141.101.64.0/18," +
         "108.162.192.0/18,190.93.240.0/20,188.114.96.0/20,197.234.240.0/22,198.41.128.0/17," +
         "162.158.0.0/15,104.16.0.0/13,104.24.0.0/14,172.64.0.0/13,131.0.72.0/22," +
         "2400:cb00::/32,2606:4700::/32,2803:f800::/32,2405:b500::/32,2405:8100::/32," +
         "2a06:98c0::/29,2c0f:f248::/32").Split(',').Select(IPNetwork.Parse).ToArray();

    public static IPNetwork[] ParseNetworks(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.TrimEntries)
            .Select(item => IPNetwork.TryParse(item, out var network) && network.PrefixLength > 0
                ? network : throw new InvalidOperationException("ClientIp:TrustedProxyNetworks requires explicit non-global CIDR networks."))
            .ToArray();

    public static IPAddress? Resolve(IPAddress? peer, string? forwardedFor, string? connectingIp,
        IReadOnlyList<IPNetwork> trustedNetworks, bool useCloudflare)
    {
        if (peer is null || !Contains(trustedNetworks, peer) || string.IsNullOrWhiteSpace(forwardedFor)) return null;
        var hops = forwardedFor.Split(',', StringSplitOptions.TrimEntries);
        // The verified deployment path is client/Cloudflare -> Traefik -> Nginx -> app.
        // Ignore any caller-supplied left prefix; require both internal ingress peers.
        if (hops.Length is < 2 or > 16 || !IPAddress.TryParse(hops[^1], out var proxy) ||
            !Contains(trustedNetworks, proxy) || !IPAddress.TryParse(hops[^2], out var edge)) return null;
        edge = Normalize(edge);
        if (useCloudflare && Contains(CloudflareNetworks, edge))
            return IPAddress.TryParse(connectingIp, out var client) ? Normalize(client) : null;
        return edge;
    }

    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    private static bool Contains(IReadOnlyList<IPNetwork> networks, IPAddress address) =>
        networks.Any(network => network.Contains(Normalize(address)));
}
