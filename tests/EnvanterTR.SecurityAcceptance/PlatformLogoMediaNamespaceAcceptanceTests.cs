using System.Net;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Platform_company_logo_read_rejects_redirected_company_directory_and_file(bool directoryLink)
    {
        var name = Guid.NewGuid().ToString("N") + ".png";
        var ownDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"));
        var foreignDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"));
        Directory.CreateDirectory(ownDirectory);
        Directory.CreateDirectory(foreignDirectory);
        var ownFile = Path.Combine(ownDirectory, name);
        var foreignFile = Path.Combine(foreignDirectory, name);
        byte[] ownBytes = [137, 80, 78, 71, 13, 10, 26, 10, 1];
        byte[] foreignBytes = [137, 80, 78, 71, 13, 10, 26, 10, 9];
        await File.WriteAllBytesAsync(ownFile, ownBytes);
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Companies.SingleAsync(x => x.Id == _companyId)).BrandLogoPath = name;
            db.PlatformAdministrators.Add(new PlatformAdministrator { UserId = _userId });
            await db.SaveChangesAsync();
        }
        var route = $"/api/platform/companies/{_companyId}/logo";
        using (var denied = await SendAsAdminAsync(route)) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var normal = await SendAsPlatformAdminAsync(route))
        {
            Assert.Equal(HttpStatusCode.OK, normal.StatusCode);
            Assert.Equal("image/png", normal.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ownBytes, await normal.Content.ReadAsByteArrayAsync());
        }
        var link = directoryLink ? ownDirectory : ownFile;
        var original = link + ".original";
        if (directoryLink) { Directory.Move(link, original); Directory.CreateSymbolicLink(link, foreignDirectory); }
        else { File.Move(link, original); File.CreateSymbolicLink(link, foreignFile); }
        try
        {
            using var rejected = await SendAsPlatformAdminAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(name, (await db.Companies.SingleAsync(x => x.Id == _companyId)).BrandLogoPath);
        }
        finally
        {
            if (directoryLink) { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsPlatformAdminAsync(route);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
        Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
    }
}
