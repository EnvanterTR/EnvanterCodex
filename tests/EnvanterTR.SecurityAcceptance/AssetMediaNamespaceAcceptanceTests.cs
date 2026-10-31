using System.Net;
using System.Text;
using System.Text.Json;
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
    public async Task Asset_attachments_reject_linked_foreign_storage_without_reading_writing_or_removing_it(bool directoryLink)
    {
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfJson = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfJson.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var ownBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nOwn tenant document\n%%EOF");
        using var upload = await SendAssetAttachmentUploadAsync(_ownAssetId, ownBytes, "own.pdf", token, cookie);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var attachmentId = uploaded.RootElement.GetProperty("id").GetGuid();
        var name = $"{attachmentId:N}.pdf";
        var ownDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), "assets", _ownAssetId.ToString("N"));
        var ownFile = Path.Combine(ownDirectory, name);
        var foreignDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), "assets", _otherCompanyAssetId.ToString("N"));
        Directory.CreateDirectory(foreignDirectory);
        var foreignFile = Path.Combine(foreignDirectory, name);
        var foreignBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nForeign tenant document must remain private\n%%EOF");
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);

        var link = directoryLink ? ownDirectory : ownFile;
        var original = link + ".original";
        if (directoryLink)
        {
            Directory.Move(link, original);
            Directory.CreateSymbolicLink(link, foreignDirectory);
        }
        else
        {
            File.Move(link, original);
            File.CreateSymbolicLink(link, foreignFile);
        }
        try
        {
            using (var read = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/attachments/{attachmentId}/file"))
                Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using (var remove = await SendAsUserAsync(HttpMethod.Delete,
                       $"/api/assets/{_ownAssetId}/attachments/{attachmentId}", _userId, _companyId,
                       requestToken: token, antiforgeryCookie: cookie))
                Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
            using (var nextUpload = await SendAssetAttachmentUploadAsync(_ownAssetId, ownBytes, "next.pdf", token, cookie))
                Assert.Equal(directoryLink ? HttpStatusCode.Conflict : HttpStatusCode.Created, nextUpload.StatusCode);

            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            Assert.Equal(foreignFile, Assert.Single(Directory.GetFiles(foreignDirectory)));
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.AssetAttachments.AnyAsync(row => row.Id == attachmentId && row.CompanyId == _companyId));
            Assert.False(await db.AssetEvents.AnyAsync(row => row.CompanyId == _companyId && row.AssetId == _ownAssetId && row.EventType == "AttachmentRemoved"));
        }
        finally
        {
            if (directoryLink) { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsAdminAsync($"/api/assets/{_ownAssetId}/attachments/{attachmentId}/file");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
    }
}
