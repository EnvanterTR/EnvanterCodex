using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Tenant_query_scopes_protect_bulk_update_delete_and_cleared_context()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var own = new AssetCategory { CompanyId = _companyId, Name = "Own bulk scope sentinel" };
        var foreign = new AssetCategory { CompanyId = _otherCompanyId, Name = "Foreign bulk scope sentinel" };
        db.AssetCategories.AddRange(own, foreign);
        await db.SaveChangesAsync();
        var originalForeignAssetName = await db.Assets.Where(row => row.Id == _otherCompanyAssetId)
            .Select(row => row.Name).SingleAsync();
        db.ChangeTracker.Clear();
        db.UseCompanyQueryScope(_companyId);

        Assert.False(await db.AssetCategories.AnyAsync(row => row.Id == foreign.Id));
        Assert.Equal(0, await db.AssetCategories.Where(row => row.Id == foreign.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Name, "Foreign mutation forbidden")));
        Assert.Equal(0, await db.AssetCategories.Where(row => row.Id == foreign.Id).ExecuteDeleteAsync());
        Assert.Equal(0, await db.Assets.IgnoreQueryFilters(["SoftDelete"]).Where(row => row.Id == _otherCompanyAssetId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Name, "Foreign asset forbidden")));
        Assert.Equal(1, await db.AssetCategories.Where(row => row.Id == own.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Name, "Own bulk update accepted")));

        db.ClearCompanyQueryScope();
        Assert.Equal(0, await db.AssetCategories.Where(row => row.Id == own.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Name, "Cleared scope forbidden")));
        Assert.Equal(0, await db.AssetCategories.Where(row => row.Id == own.Id).ExecuteDeleteAsync());
        Assert.Empty(await db.AssetCategories.ToListAsync());

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Foreign bulk scope sentinel", await verify.AssetCategories.Where(row => row.Id == foreign.Id)
            .Select(row => row.Name).SingleAsync());
        Assert.Equal("Own bulk update accepted", await verify.AssetCategories.Where(row => row.Id == own.Id)
            .Select(row => row.Name).SingleAsync());
        var foreignAssetName = await verify.Assets.Where(row => row.Id == _otherCompanyAssetId).Select(row => row.Name).SingleAsync();
        Assert.Equal(originalForeignAssetName, foreignAssetName);
        verify.UseCompanyQueryScope(_companyId);
        Assert.Equal(1, await verify.AssetCategories.Where(row => row.Id == own.Id).ExecuteDeleteAsync());
        verify.UseCompanyQueryScope(_otherCompanyId);
        Assert.True(await verify.AssetCategories.AnyAsync(row => row.Id == foreign.Id));
    }

    [Fact]
    public async Task Tenant_query_scopes_use_runtime_context_values_and_preserve_soft_delete()
    {
        await using var scopeA = _factory!.Services.CreateAsyncScope();
        await using var scopeB = _factory.Services.CreateAsyncScope();
        var a = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var b = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Same(a.Model, b.Model);
        Assert.False(a.Database.HasPendingModelChanges());
        a.UseCompanyQueryScope(_companyId);
        b.UseCompanyQueryScope(_otherCompanyId);
        Assert.Contains(await a.Assets.ToListAsync(), row => row.Id == _ownAssetId);
        Assert.All(await a.Assets.ToListAsync(), row => Assert.Equal(_companyId, row.CompanyId));
        Assert.All(await b.Assets.ToListAsync(), row => Assert.Equal(_otherCompanyId, row.CompanyId));
        Assert.True(await b.Assets.AnyAsync(row => row.Id == _otherCompanyAssetId));
        Assert.Equal(_companyId, (await a.Companies.SingleAsync()).Id);
        Assert.Equal(_otherCompanyId, (await b.Companies.SingleAsync()).Id);

        var asset = await a.Assets.SingleAsync(row => row.Id == _ownAssetId);
        asset.DeletedAt = DateTimeOffset.UtcNow;
        await a.SaveChangesAsync();
        Assert.False(await a.Assets.AnyAsync(row => row.Id == _ownAssetId));
        var includingDeleted = await a.Assets.IgnoreQueryFilters(["SoftDelete"]).ToListAsync();
        Assert.Contains(includingDeleted, row => row.Id == _ownAssetId);
        Assert.All(includingDeleted, row => Assert.Equal(_companyId, row.CompanyId));
        a.ClearCompanyQueryScope();
        Assert.Empty(await a.Assets.ToListAsync());
        Assert.Empty(await a.Companies.ToListAsync());
        Assert.True(await b.Assets.AnyAsync(row => row.Id == _otherCompanyAssetId));
    }

    [Fact]
    public async Task Tenant_query_scope_rejects_tracked_foreign_writes_without_saving_own_changes()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var foreign = await db.Assets.SingleAsync(row => row.Id == _otherCompanyAssetId);
        var own = await db.Assets.SingleAsync(row => row.Id == _ownAssetId);
        var originalOwnName = own.Name;
        var originalForeignName = foreign.Name;
        db.UseCompanyQueryScope(_companyId);
        own.Name = "Own change must not partially persist";
        foreign.Name = "Foreign change must be rejected";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(originalOwnName, await verify.Assets.Where(row => row.Id == own.Id).Select(row => row.Name).SingleAsync());
        Assert.Equal(originalForeignName, await verify.Assets.Where(row => row.Id == foreign.Id).Select(row => row.Name).SingleAsync());
    }
}
