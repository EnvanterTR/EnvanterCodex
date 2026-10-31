using System.Net;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData("brands", true)]
    [InlineData("brands", false)]
    [InlineData("suppliers", true)]
    [InlineData("suppliers", false)]
    [InlineData("models", true)]
    [InlineData("models", false)]
    [InlineData("accessory-models", true)]
    [InlineData("accessory-models", false)]
    [InlineData("accessories", true)]
    [InlineData("accessories", false)]
    public async Task Catalog_and_accessory_images_reject_foreign_storage_links_without_replacing_metadata(string kind, bool directoryLink)
    {
        var id = Guid.NewGuid();
        var imageKind = kind is "brands" or "suppliers" ? "logo" : "photo";
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            switch (kind)
            {
                case "brands": db.AssetBrands.Add(new AssetBrand { Id = id, CompanyId = _companyId, Name = "Synthetic brand" }); break;
                case "suppliers": db.SupplierCatalogEntries.Add(new SupplierCatalogEntry { Id = id, CompanyId = _companyId, Name = "Synthetic supplier", NormalizedName = "SYNTHETIC SUPPLIER" }); break;
                case "models": db.AssetModels.Add(new AssetModel { Id = id, CompanyId = _companyId, Name = "Synthetic model" }); break;
                case "accessory-models": db.AccessoryModels.Add(new AccessoryModel { Id = id, CompanyId = _companyId, Name = "Synthetic accessory model" }); break;
                case "accessories": db.Accessories.Add(new Accessory { Id = id, CompanyId = _companyId, Name = "Synthetic accessory" }); break;
            }
            await db.SaveChangesAsync();
        }
        async Task<string?> ReadNameAsync()
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return kind switch {
                "brands" => await db.AssetBrands.Where(x => x.Id == id && x.CompanyId == _companyId).Select(x => x.LogoFileName).SingleAsync(),
                "suppliers" => await db.SupplierCatalogEntries.Where(x => x.Id == id && x.CompanyId == _companyId).Select(x => x.LogoFileName).SingleAsync(),
                "models" => await db.AssetModels.Where(x => x.Id == id && x.CompanyId == _companyId).Select(x => x.PhotoFileName).SingleAsync(),
                "accessory-models" => await db.AccessoryModels.Where(x => x.Id == id && x.CompanyId == _companyId).Select(x => x.PhotoFileName).SingleAsync(),
                "accessories" => await db.Accessories.Where(x => x.Id == id && x.CompanyId == _companyId).Select(x => x.PhotoFileName).SingleAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfJson = System.Text.Json.JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfJson.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        byte[] ownBytes = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3];
        var route = $"/api/{kind}/{id}/{imageKind}";
        async Task<HttpResponseMessage> UploadAsync()
        {
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent(ownBytes), "file", "own.png");
            return await SendAsUserAsync(HttpMethod.Post, route, _userId, _companyId, multipart, token, cookie);
        }
        using var upload = await UploadAsync();
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var name = await ReadNameAsync();
        Assert.NotNull(name);
        var relative = kind == "accessories" ? Path.Combine("accessories", id.ToString("N"), "photo") : Path.Combine("catalog", kind, id.ToString("N"));
        var ownDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), relative);
        var ownFile = Path.Combine(ownDirectory, name);
        var foreignDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), relative);
        Directory.CreateDirectory(foreignDirectory);
        var foreignFile = Path.Combine(foreignDirectory, name);
        byte[] foreignBytes = [137, 80, 78, 71, 13, 10, 26, 10, 9, 8, 7];
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);
        var link = directoryLink ? ownDirectory : ownFile;
        var original = link + ".original";
        if (directoryLink) { Directory.Move(link, original); Directory.CreateSymbolicLink(link, foreignDirectory); }
        else { File.Move(link, original); File.CreateSymbolicLink(link, foreignFile); }
        try
        {
            using (var read = await SendAsAdminAsync(route)) Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using (var remove = await SendAsUserAsync(HttpMethod.Delete, route, _userId, _companyId,
                       requestToken: token, antiforgeryCookie: cookie)) Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
            using (var replace = await UploadAsync()) Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);
            Assert.Equal(name, await ReadNameAsync());
            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            Assert.Equal(foreignFile, Assert.Single(Directory.GetFiles(foreignDirectory)));
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.AuditEntries.AnyAsync(x => x.CompanyId == _companyId && x.TargetId == id &&
                (x.Action == "PhotoRemoved" || x.Action == "LogoRemoved")));
        }
        finally
        {
            if (directoryLink) { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsAdminAsync(route);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
        using var normalDelete = await SendAsUserAsync(HttpMethod.Delete, route, _userId, _companyId,
            requestToken: token, antiforgeryCookie: cookie);
        Assert.Equal(HttpStatusCode.NoContent, normalDelete.StatusCode);
        Assert.Null(await ReadNameAsync());
    }
}
