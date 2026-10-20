using EnvanterTR.Web.Data;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class TenantPortalUrlTests
{
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
