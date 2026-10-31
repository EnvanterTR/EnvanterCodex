using System.IO.Compression;
using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Theory]
    [InlineData("accounts")]
    [InlineData("user")]
    [InlineData("file")]
    public async Task Account_photo_and_company_archive_reject_redirected_identity_storage(string linkKind)
    {
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfJson = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfJson.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        byte[] ownBytes = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3];
        using var upload = await SendProfilePhotoUploadAsync(ownBytes, token, cookie);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var accounts = Path.Combine(_mediaPath, "accounts");
        var ownDirectory = Path.Combine(accounts, _userId.ToString("N"));
        var ownFile = Path.Combine(ownDirectory, "profile.png");
        var foreignDirectory = linkKind == "accounts"
            ? Path.Combine(_mediaPath, "foreign-accounts", _userId.ToString("N"))
            : Path.Combine(accounts, _sameCompanyOtherUserId.ToString("N"));
        Directory.CreateDirectory(foreignDirectory);
        var foreignFile = Path.Combine(foreignDirectory, "profile.png");
        byte[] foreignBytes = [137, 80, 78, 71, 13, 10, 26, 10, 9, 8, 7];
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);
        int auditCount;
        await using (var scope = _factory!.Services.CreateAsyncScope())
            auditCount = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditEntries
                .CountAsync(x => x.CompanyId == _companyId && x.TargetType == "Account" && x.TargetId == _userId);
        var link = linkKind switch { "accounts" => accounts, "user" => ownDirectory, _ => ownFile };
        var target = linkKind switch { "accounts" => Path.GetDirectoryName(foreignDirectory)!, "user" => foreignDirectory, _ => foreignFile };
        var original = link + ".original";
        if (linkKind != "file") { Directory.Move(link, original); Directory.CreateSymbolicLink(link, target); }
        else { File.Move(link, original); File.CreateSymbolicLink(link, target); }
        try
        {
            using (var read = await SendAsAdminAsync("/api/session/photo")) Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using (var replace = await SendProfilePhotoUploadAsync(ownBytes, token, cookie)) Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);
            using (var remove = await SendAsUserAsync(HttpMethod.Delete, "/api/session/photo", _userId, _companyId,
                       requestToken: token, antiforgeryCookie: cookie)) Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
            using (var session = await SendAsAdminAsync("/api/session"))
            {
                Assert.Equal(HttpStatusCode.OK, session.StatusCode);
                using var json = JsonDocument.Parse(await session.Content.ReadAsStringAsync());
                Assert.False(json.RootElement.GetProperty("hasPhoto").GetBoolean());
            }
            using (var response = await SendAsAdminAsync("/api/company/export.zip"))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var zip = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
                Assert.DoesNotContain(zip.Entries, e => e.FullName == $"media/account-profiles/{_userId:N}/profile.png");
            }
            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            await using var scope = _factory!.Services.CreateAsyncScope();
            Assert.Equal(auditCount, await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditEntries
                .CountAsync(x => x.CompanyId == _companyId && x.TargetType == "Account" && x.TargetId == _userId));
        }
        finally
        {
            if (linkKind != "file") { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsAdminAsync("/api/session/photo");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
        using var normalExport = await SendAsAdminAsync("/api/company/export.zip");
        Assert.Equal(HttpStatusCode.OK, normalExport.StatusCode);
        using var normalZip = new ZipArchive(new MemoryStream(await normalExport.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        Assert.Contains(normalZip.Entries, e => e.FullName == $"media/account-profiles/{_userId:N}/profile.png");
    }
}
