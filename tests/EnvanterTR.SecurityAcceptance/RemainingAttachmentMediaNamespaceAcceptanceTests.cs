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
    [InlineData("stock", true)]
    [InlineData("stock", false)]
    [InlineData("accessory", true)]
    [InlineData("accessory", false)]
    [InlineData("model", true)]
    [InlineData("model", false)]
    [InlineData("contract", true)]
    [InlineData("contract", false)]
    public async Task Remaining_attachments_reject_foreign_directory_and_file_links(string kind, bool directoryLink)
    {
        var id = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        var (route, storage, targetType) = kind switch {
            "stock" => ("stock-orders", "stock-purchase-orders", "StockPurchaseOrder"),
            "accessory" => ("accessory-orders", "accessory-purchase-orders", "AccessoryPurchaseOrder"),
            "model" => ("models", "asset-models", "AssetModel"),
            "contract" => ("contracts", "contracts", "AssetContract"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            switch (kind)
            {
                case "stock": db.StockPurchaseOrders.Add(new StockPurchaseOrder { Id = id, CompanyId = _companyId,
                    OrderNumber = "NAMESPACE-TEST", Supplier = "Synthetic supplier", OrderDate = DateOnly.FromDateTime(DateTime.UtcNow) }); break;
                case "accessory": db.AccessoryPurchaseOrders.Add(new AccessoryPurchaseOrder { Id = id, CompanyId = _companyId,
                    OrderNumber = "NAMESPACE-TEST", Supplier = "Synthetic supplier", OrderDate = DateOnly.FromDateTime(DateTime.UtcNow) }); break;
                case "model": db.AssetModels.Add(new AssetModel { Id = id, CompanyId = _companyId, Name = "Synthetic model" }); break;
                case "contract": db.AssetContracts.Add(new AssetContract { Id = id, CompanyId = _companyId, Name = "Synthetic contract" }); break;
            }
            await db.SaveChangesAsync();
        }
        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfJson = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var token = csrfJson.RootElement.GetProperty("requestToken").GetString()!;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));
        var ownBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nOwn tenant attachment\n%%EOF");
        async Task<HttpResponseMessage> UploadAsync()
        {
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent(ownBytes), "file", "own.pdf");
            return await SendAsUserAsync(HttpMethod.Post, $"/api/{route}/{id}/attachments", _userId, _companyId,
                multipart, token, cookie);
        }
        using var upload = await UploadAsync();
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using var uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var attachmentId = uploaded.RootElement.GetProperty("id").GetGuid();
        var name = $"{attachmentId:N}.pdf";
        var ownDirectory = Path.Combine(_mediaPath, _companyId.ToString("N"), storage, id.ToString("N"), "attachments");
        var ownFile = Path.Combine(ownDirectory, name);
        var foreignDirectory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), storage, foreignId.ToString("N"), "attachments");
        Directory.CreateDirectory(foreignDirectory);
        var foreignFile = Path.Combine(foreignDirectory, name);
        var foreignBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nForeign tenant sentinel\n%%EOF");
        await File.WriteAllBytesAsync(foreignFile, foreignBytes);
        var link = directoryLink ? ownDirectory : ownFile;
        var original = link + ".original";
        if (directoryLink) { Directory.Move(link, original); Directory.CreateSymbolicLink(link, foreignDirectory); }
        else { File.Move(link, original); File.CreateSymbolicLink(link, foreignFile); }
        try
        {
            using (var read = await SendAsAdminAsync($"/api/{route}/{id}/attachments/{attachmentId}/file"))
                Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
            using (var remove = await SendAsUserAsync(HttpMethod.Delete, $"/api/{route}/{id}/attachments/{attachmentId}",
                       _userId, _companyId, requestToken: token, antiforgeryCookie: cookie))
                Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
            using (var nextUpload = await UploadAsync())
                Assert.Equal(directoryLink ? HttpStatusCode.Conflict : HttpStatusCode.Created, nextUpload.StatusCode);
            Assert.Equal(foreignBytes, await File.ReadAllBytesAsync(foreignFile));
            Assert.Equal(foreignFile, Assert.Single(Directory.GetFiles(foreignDirectory)));
            await using var scope = _factory!.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(kind switch {
                "stock" => await db.StockPurchaseOrderAttachments.AnyAsync(row => row.Id == attachmentId && row.CompanyId == _companyId),
                "accessory" => await db.AccessoryPurchaseOrderAttachments.AnyAsync(row => row.Id == attachmentId && row.CompanyId == _companyId),
                "model" => await db.AssetModelAttachments.AnyAsync(row => row.Id == attachmentId && row.CompanyId == _companyId),
                "contract" => await db.AssetContractAttachments.AnyAsync(row => row.Id == attachmentId && row.CompanyId == _companyId),
                _ => false
            });
            Assert.False(await db.AuditEntries.AnyAsync(row => row.CompanyId == _companyId && row.TargetId == id
                && row.TargetType == targetType && (row.Action == "AttachmentRemoved" || row.Action == "contract.attachment-removed")));
        }
        finally
        {
            if (directoryLink) { Directory.Delete(link); Directory.Move(original, link); }
            else { File.Delete(link); File.Move(original, link); }
        }
        using var restored = await SendAsAdminAsync($"/api/{route}/{id}/attachments/{attachmentId}/file");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(ownBytes, await restored.Content.ReadAsByteArrayAsync());
        using var normalDelete = await SendAsUserAsync(HttpMethod.Delete, $"/api/{route}/{id}/attachments/{attachmentId}",
            _userId, _companyId, requestToken: token, antiforgeryCookie: cookie);
        Assert.Equal(HttpStatusCode.NoContent, normalDelete.StatusCode);
    }
}
