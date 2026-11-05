using System.Net;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Tenant_read_matrix_hides_foreign_records_from_detail_and_collection_routes()
    {
        var cases = new (string Resource, Guid OwnId, Guid ForeignId, string CollectionPath, string DetailPath)[]
        {
            ("asset", _ownAssetId, _otherCompanyAssetId, "/api/assets?page=1&pageSize=50", $"/api/assets/{_otherCompanyAssetId}"),
            ("employee", _ownEmployeeId, _otherCompanyEmployeeId, "/api/employees?page=1&pageSize=50", $"/api/employees/{_otherCompanyEmployeeId}"),
            ("software license", _ownLicenseId, _otherCompanyLicenseId, "/api/software-licenses?status=all", $"/api/software-licenses/{_otherCompanyLicenseId}"),
            ("accessory", _ownAccessoryId, _otherCompanyAccessoryId, "/api/accessories?includeArchived=true", $"/api/accessories/{_otherCompanyAccessoryId}"),
            ("consumable", _ownConsumableId, _otherCompanyConsumableId, "/api/consumables?includeArchived=true", $"/api/consumables/{_otherCompanyConsumableId}"),
            ("component", _ownComponentId, _otherCompanyComponentId, "/api/components?includeArchived=true", $"/api/components/{_otherCompanyComponentId}")
        };

        foreach (var item in cases)
        {
            using var detail = await SendAsAdminAsync(item.DetailPath);
            Assert.True(detail.StatusCode == HttpStatusCode.NotFound,
                $"A company-admin direct read of another company's {item.Resource} must return 404, got {(int)detail.StatusCode}.");

            using var collection = await SendAsAdminAsync(item.CollectionPath);
            var body = await collection.Content.ReadAsStringAsync();
            Assert.True(collection.StatusCode == HttpStatusCode.OK,
                $"The {item.Resource} collection must remain readable, got {(int)collection.StatusCode}: {body}");
            Assert.DoesNotContain(item.ForeignId.ToString("D"), body, StringComparison.OrdinalIgnoreCase);

            var forgedCompanyPath = $"{item.CollectionPath}&companyId={_otherCompanyId:D}";
            using var forgedCollection = await SendAsAdminAsync(forgedCompanyPath);
            var forgedBody = await forgedCollection.Content.ReadAsStringAsync();
            Assert.True(forgedCollection.StatusCode == HttpStatusCode.OK,
                $"A client-supplied company selector must not disrupt the {item.Resource} collection, got {(int)forgedCollection.StatusCode}: {forgedBody}");
            Assert.Contains(item.OwnId.ToString("D"), forgedBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(item.ForeignId.ToString("D"), forgedBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Tenant_write_matrix_rejects_foreign_stock_archive_and_preserves_rows()
    {
        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = System.Text.Json.JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var foreignArchives = new (string Resource, string Path, Guid Id)[]
        {
            ("accessory", $"/api/accessories/{_otherCompanyAccessoryId}", _otherCompanyAccessoryId),
            ("consumable", $"/api/consumables/{_otherCompanyConsumableId}", _otherCompanyConsumableId),
            ("component", $"/api/components/{_otherCompanyComponentId}", _otherCompanyComponentId)
        };

        foreach (var item in foreignArchives)
        {
            using var response = await SendAsUserAsync(HttpMethod.Delete, item.Path, _userId, _companyId,
                requestToken: requestToken, antiforgeryCookie: antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Accessories.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyAccessoryId && x.IsArchived));
        Assert.False(await db.Consumables.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyConsumableId && x.IsArchived));
        Assert.False(await db.InventoryComponents.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == _otherCompanyComponentId && x.IsArchived));
        Assert.DoesNotContain(await db.AuditEntries.Where(x => x.CompanyId == _otherCompanyId && x.Action.Contains("archiv"))
            .Select(x => x.TargetId).ToListAsync(), id => foreignArchives.Any(item => item.Id == id));
    }

    [Fact]
    public async Task Tenant_attachment_matrix_hides_foreign_purchase_order_files_and_preserves_rows()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var suffix = Guid.NewGuid().ToString("N");
        var payload = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nforeign tenant invoice\n%%EOF");
        var cases = new[]
        {
            (Name: "stock", Route: "/api/stock-orders", Storage: "stock-purchase-orders"),
            (Name: "accessory", Route: "/api/accessory-orders", Storage: "accessory-purchase-orders"),
            (Name: "asset", Route: "/api/asset-orders", Storage: "asset-purchase-orders")
        }.Select(item => (item.Name, item.Route, item.Storage, OwnOrderId: Guid.NewGuid(), ForeignOrderId: Guid.NewGuid(), AttachmentId: Guid.NewGuid())).ToArray();

        foreach (var item in cases)
        {
            var directory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), item.Storage, item.ForeignOrderId.ToString("N"), "attachments");
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, $"{item.AttachmentId:N}.pdf"), payload);
        }

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var item in cases)
            {
                var ownOrder = new StockPurchaseOrder
                {
                    Id = item.OwnOrderId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}",
                    Supplier = "Own supplier", OrderDate = today
                };
                var foreignOrder = new StockPurchaseOrder
                {
                    Id = item.ForeignOrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}",
                    Supplier = "Foreign supplier", OrderDate = today
                };
                if (item.Name == "stock")
                {
                    db.StockPurchaseOrders.AddRange(ownOrder, foreignOrder);
                    db.StockPurchaseOrderAttachments.Add(new StockPurchaseOrderAttachment
                    {
                        Id = item.AttachmentId, CompanyId = _otherCompanyId, PurchaseOrderId = item.ForeignOrderId,
                        OriginalFileName = "foreign.pdf", StorageFileName = $"{item.AttachmentId:N}.pdf",
                        ContentType = "application/pdf", Length = payload.Length
                    });
                }
                else if (item.Name == "accessory")
                {
                    db.AccessoryPurchaseOrders.AddRange(
                        new AccessoryPurchaseOrder { Id = item.OwnOrderId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}", Supplier = "Own supplier", OrderDate = today },
                        new AccessoryPurchaseOrder { Id = item.ForeignOrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}", Supplier = "Foreign supplier", OrderDate = today });
                    db.AccessoryPurchaseOrderAttachments.Add(new AccessoryPurchaseOrderAttachment
                    {
                        Id = item.AttachmentId, CompanyId = _otherCompanyId, PurchaseOrderId = item.ForeignOrderId,
                        OriginalFileName = "foreign.pdf", StorageFileName = $"{item.AttachmentId:N}.pdf",
                        ContentType = "application/pdf", Length = payload.Length
                    });
                }
                else
                {
                    db.AssetPurchaseOrders.AddRange(
                        new AssetPurchaseOrder { Id = item.OwnOrderId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}", Supplier = "Own supplier", OrderDate = today },
                        new AssetPurchaseOrder { Id = item.ForeignOrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}", Supplier = "Foreign supplier", OrderDate = today });
                    db.AssetPurchaseOrderAttachments.Add(new AssetPurchaseOrderAttachment
                    {
                        Id = item.AttachmentId, CompanyId = _otherCompanyId, PurchaseOrderId = item.ForeignOrderId,
                        OriginalFileName = "foreign.pdf", StorageFileName = $"{item.AttachmentId:N}.pdf",
                        ContentType = "application/pdf", Length = payload.Length
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        using var csrfResponse = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        using var csrfPayload = System.Text.Json.JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrfResponse.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Contains('=') && value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        foreach (var item in cases)
        {
            using var foreignParentList = await SendAsAdminAsync($"{item.Route}/{item.ForeignOrderId}/attachments");
            Assert.Equal(HttpStatusCode.NotFound, foreignParentList.StatusCode);

            using var ownParentList = await SendAsAdminAsync($"{item.Route}/{item.OwnOrderId}/attachments");
            Assert.Equal(HttpStatusCode.OK, ownParentList.StatusCode);
            var ownParentBody = await ownParentList.Content.ReadAsStringAsync();
            Assert.DoesNotContain(item.AttachmentId.ToString("D"), ownParentBody, StringComparison.OrdinalIgnoreCase);

            var attachmentPath = $"{item.Route}/{item.OwnOrderId}/attachments/{item.AttachmentId}";
            using var foreignFile = await SendAsAdminAsync($"{attachmentPath}/file");
            Assert.Equal(HttpStatusCode.NotFound, foreignFile.StatusCode);

            using var foreignDelete = await SendAsUserAsync(HttpMethod.Delete, attachmentPath, _userId, _companyId,
                requestToken: requestToken, antiforgeryCookie: antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);
        }

        using (var archiveResponse = await SendAsAdminAsync("/api/company/export.zip"))
        {
            Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
            using var archive = new System.IO.Compression.ZipArchive(
                new MemoryStream(await archiveResponse.Content.ReadAsByteArrayAsync()), System.IO.Compression.ZipArchiveMode.Read);
            Assert.DoesNotContain(archive.Entries, entry => cases.Any(item => entry.FullName.Contains(item.AttachmentId.ToString("N"), StringComparison.OrdinalIgnoreCase)));
            var dataEntry = Assert.Single(archive.Entries, entry => entry.FullName == "company-data.json");
            await using var dataStream = dataEntry.Open();
            using var data = await System.Text.Json.JsonDocument.ParseAsync(dataStream);
            var exportedJson = data.RootElement.GetRawText();
            Assert.DoesNotContain(exportedJson, cases[0].AttachmentId.ToString("D"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(exportedJson, cases[1].AttachmentId.ToString("D"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(exportedJson, cases[2].AttachmentId.ToString("D"), StringComparison.OrdinalIgnoreCase);
        }

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var item in cases)
        {
            var directory = Path.Combine(_mediaPath, _otherCompanyId.ToString("N"), item.Storage, item.ForeignOrderId.ToString("N"), "attachments");
            Assert.True(File.Exists(Path.Combine(directory, $"{item.AttachmentId:N}.pdf")));
            var rowExists = item.Name switch
            {
                "stock" => await verifyDb.StockPurchaseOrderAttachments.AnyAsync(x => x.CompanyId == _otherCompanyId && x.PurchaseOrderId == item.ForeignOrderId && x.Id == item.AttachmentId),
                "accessory" => await verifyDb.AccessoryPurchaseOrderAttachments.AnyAsync(x => x.CompanyId == _otherCompanyId && x.PurchaseOrderId == item.ForeignOrderId && x.Id == item.AttachmentId),
                _ => await verifyDb.AssetPurchaseOrderAttachments.AnyAsync(x => x.CompanyId == _otherCompanyId && x.PurchaseOrderId == item.ForeignOrderId && x.Id == item.AttachmentId)
            };
            Assert.True(rowExists, $"Foreign {item.Name} attachment must remain unchanged after rejected requests.");
        }
    }
}
