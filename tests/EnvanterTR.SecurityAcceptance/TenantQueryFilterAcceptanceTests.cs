using System.Security.Claims;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Company_scoped_entities_are_filtered_by_the_authenticated_tenant_context()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        accessor.HttpContext = new DefaultHttpContext();

        try
        {
            accessor.HttpContext.User = TenantPrincipal(_companyId);
            var firstTenantAssets = await db.Assets.AsNoTracking()
                .Where(asset => asset.Id == _ownAssetId || asset.Id == _otherCompanyAssetId)
                .Select(asset => asset.Id)
                .ToListAsync();
            Assert.Equal(new[] { _ownAssetId }, firstTenantAssets);

            accessor.HttpContext.User = TenantPrincipal(_otherCompanyId);
            var secondTenantAssets = await db.Assets.AsNoTracking()
                .Where(asset => asset.Id == _ownAssetId || asset.Id == _otherCompanyAssetId)
                .Select(asset => asset.Id)
                .ToListAsync();
            Assert.Equal(new[] { _otherCompanyAssetId }, secondTenantAssets);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    private static ClaimsPrincipal TenantPrincipal(Guid companyId) => new(
        new ClaimsIdentity([new Claim("company_id", companyId.ToString())], "tenant-query-filter-acceptance"));
}
