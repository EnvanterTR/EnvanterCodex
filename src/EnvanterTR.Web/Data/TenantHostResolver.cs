using Microsoft.AspNetCore.Http;

namespace EnvanterTR.Web.Data;

public static class TenantHostResolver
{
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "app", "auth", "billing", "cdn", "help", "mail", "platform", "support"
    };

    public static string? ResolveSlug(HttpRequest request, string? configuredRootDomain)
    {
        var rootDomain = (configuredRootDomain ?? "envantertr.com").Trim().Trim('.');
        var host = request.Host.Host.TrimEnd('.');
        var suffix = $".{rootDomain}";
        if (rootDomain.Length == 0 || !host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;

        var slug = host[..^suffix.Length];
        if (slug.Equals("www", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!IsValidSlug(slug))
            return string.Empty;

        return slug.ToLowerInvariant();
    }

    public static bool IsValidSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length is < 2 or > 63 || slug[0] == '-' || slug[^1] == '-' ||
            ReservedSlugs.Contains(slug))
            return false;

        foreach (var character in slug)
        {
            if (!(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))
                return false;
        }

        return true;
    }
}
