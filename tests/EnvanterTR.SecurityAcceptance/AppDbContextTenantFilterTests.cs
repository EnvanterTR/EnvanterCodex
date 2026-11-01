using System.Security.Claims;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class AppDbContextTenantFilterTests
{
    [Fact]
    public void Tenant_query_filter_is_present_and_reads_the_current_request_scope()
    {
        var firstCompanyId = Guid.NewGuid();
        var secondCompanyId = Guid.NewGuid();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=tenant_filter_test;Username=test;Password=test")
            .EnableSensitiveDataLogging()
            .Options;
        using var db = new AppDbContext(options, accessor);

        accessor.HttpContext!.User = TenantPrincipal(firstCompanyId);
        var firstTenantSql = db.Assets.AsNoTracking().ToQueryString();
        Assert.Contains(firstCompanyId.ToString(), firstTenantSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secondCompanyId.ToString(), firstTenantSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CompanyId", firstTenantSql, StringComparison.Ordinal);
        Assert.Contains("DeletedAt", firstTenantSql, StringComparison.Ordinal);

        accessor.HttpContext.User = TenantPrincipal(secondCompanyId);
        var secondTenantSql = db.Assets.AsNoTracking().ToQueryString();
        Assert.Contains(secondCompanyId.ToString(), secondTenantSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(firstCompanyId.ToString(), secondTenantSql, StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal TenantPrincipal(Guid companyId) => new(
        new ClaimsIdentity([new Claim("company_id", companyId.ToString())], "tenant-query-filter-test"));
}
