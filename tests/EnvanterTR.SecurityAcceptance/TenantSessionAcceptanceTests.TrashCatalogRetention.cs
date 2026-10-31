using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData("categories")]
    [InlineData("brands")]
    [InlineData("models")]
    public async Task Trash_assets_retain_catalog_references_and_restore_without_dangling_links(string kind)
    {
        var lookupId = Guid.NewGuid();
        var foreignLookupId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new AssetCategory { Id = kind == "categories" ? lookupId : Guid.NewGuid(),
                CompanyId = _companyId, Name = "Trash catalog category" };
            db.AssetCategories.Add(category);
            if (kind == "categories") db.AssetCategories.Add(new AssetCategory
                { Id = foreignLookupId, CompanyId = _otherCompanyId, Name = "Foreign trash category" });
            if (kind == "brands") db.AssetBrands.AddRange(
                new AssetBrand { Id = lookupId, CompanyId = _companyId, Name = "Trash-only brand" },
                new AssetBrand { Id = foreignLookupId, CompanyId = _otherCompanyId, Name = "Foreign trash brand" });
            if (kind == "models") db.AssetModels.AddRange(
                new AssetModel { Id = lookupId, CompanyId = _companyId, Name = "Trash-only model" },
                new AssetModel { Id = foreignLookupId, CompanyId = _otherCompanyId, Name = "Foreign trash model" });
            db.Assets.Add(new Asset { Id = assetId, CompanyId = _companyId, CategoryId = category.Id,
                AssetTag = $"TRASH-LKP-{_userId:N}-{kind}", Name = "Recoverable catalog asset",
                Status = "Available", Currency = "TRY", DeletedAt = DateTimeOffset.UtcNow,
                DeletedByUserId = _userId, BrandId = kind == "brands" ? lookupId : null,
                ModelId = kind == "models" ? lookupId : null });
            await db.SaveChangesAsync();
        }

        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfJson = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfJson.RootElement.GetProperty("requestToken").GetString();
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        using var foreignDelete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/lookups/{kind}/{foreignLookupId}",
            "admin", null, token, cookie);
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);
        using var ownDelete = await SendAsRoleAsync(HttpMethod.Delete, $"/api/lookups/{kind}/{lookupId}",
            "admin", null, token, cookie);
        Assert.Equal(HttpStatusCode.Conflict, ownDelete.StatusCode);

        using var restore = await SendAsRoleAsync(HttpMethod.Post, $"/api/assets/{assetId}/restore-from-trash",
            "admin", JsonContent.Create(new { note = "Catalog retention acceptance" }), token, cookie);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        verify.UseCompanyQueryScope(_companyId);
        var restored = await verify.Assets.SingleAsync(asset => asset.Id == assetId);
        Assert.Null(restored.DeletedAt);
        if (kind == "categories") Assert.True(await verify.AssetCategories.AnyAsync(row => row.Id == restored.CategoryId));
        if (kind == "brands") Assert.True(await verify.AssetBrands.AnyAsync(row => row.Id == restored.BrandId));
        if (kind == "models") Assert.True(await verify.AssetModels.AnyAsync(row => row.Id == restored.ModelId));
    }
}
