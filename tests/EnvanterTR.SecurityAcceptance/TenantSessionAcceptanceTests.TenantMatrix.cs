using System.Net;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Supplier_catalog_read_and_write_matrix_is_company_scoped_and_rejects_foreign_mutations()
    {
        var ownActive = new SupplierCatalogEntry
        {
            CompanyId = _companyId, Name = $"Own active {_userId:N}", NormalizedName = $"OWN ACTIVE {_userId:N}"
        };
        var ownArchived = new SupplierCatalogEntry
        {
            CompanyId = _companyId, Name = $"Own archived {_userId:N}", NormalizedName = $"OWN ARCHIVED {_userId:N}", IsActive = false
        };
        var foreignActive = new SupplierCatalogEntry
        {
            CompanyId = _otherCompanyId, Name = $"Foreign active {_userId:N}", NormalizedName = $"FOREIGN ACTIVE {_userId:N}"
        };
        var foreignArchived = new SupplierCatalogEntry
        {
            CompanyId = _otherCompanyId, Name = $"Foreign archived {_userId:N}", NormalizedName = $"FOREIGN ARCHIVED {_userId:N}", IsActive = false
        };

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupplierCatalogEntries.AddRange(ownActive, ownArchived, foreignActive, foreignArchived);
            var foreignMembership = await db.CompanyMemberships.SingleOrDefaultAsync(x => x.CompanyId == _otherCompanyId && x.UserId == _userId);
            if (foreignMembership is null)
                db.CompanyMemberships.Add(new CompanyMembership { CompanyId = _otherCompanyId, UserId = _userId, RoleKey = "admin" });
            else
                foreignMembership.RoleKey = "admin";
            await db.SaveChangesAsync();
        }

        using (var activeList = await SendAsAdminAsync("/api/suppliers"))
        {
            var body = await activeList.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, activeList.StatusCode);
            Assert.Contains(ownActive.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(ownArchived.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(foreignActive.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(foreignArchived.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        }

        using (var archivedList = await SendAsAdminAsync($"/api/suppliers?includeArchived=true&companyId={_otherCompanyId:D}"))
        {
            var body = await archivedList.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, archivedList.StatusCode);
            Assert.Contains(ownActive.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(ownArchived.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(foreignActive.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(foreignArchived.Id.ToString("D"), body, StringComparison.OrdinalIgnoreCase);
        }

        using (var ownDetail = await SendAsAdminAsync($"/api/suppliers/{ownActive.Id}"))
            Assert.Equal(HttpStatusCode.OK, ownDetail.StatusCode);
        using (var foreignDetail = await SendAsAdminAsync($"/api/suppliers/{foreignActive.Id}"))
            Assert.Equal(HttpStatusCode.NotFound, foreignDetail.StatusCode);

        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = System.Text.Json.JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        var updateBody = System.Net.Http.Json.JsonContent.Create(new
        {
            name = "Foreign supplier overwritten", contactName = (string?)null, email = (string?)null,
            phone = (string?)null, website = (string?)null, notes = "cross-company probe"
        });
        using (var foreignUpdate = await SendAsRoleAsync(HttpMethod.Put, $"/api/suppliers/{foreignActive.Id}", "admin",
            updateBody, requestToken, antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignUpdate.StatusCode);
        using (var foreignArchive = await SendAsRoleAsync(HttpMethod.Post, $"/api/suppliers/{foreignActive.Id}/archive", "admin",
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignArchive.StatusCode);
        using (var foreignRestore = await SendAsRoleAsync(HttpMethod.Post, $"/api/suppliers/{foreignArchived.Id}/restore", "admin",
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.NotFound, foreignRestore.StatusCode);
        await SetCompanyRoleAsync("viewer");
        using (var viewerArchive = await SendAsRoleAsync(HttpMethod.Post, $"/api/suppliers/{ownActive.Id}/archive", "viewer",
            requestToken: requestToken, antiforgeryCookie: antiforgeryCookie))
            Assert.Equal(HttpStatusCode.Forbidden, viewerArchive.StatusCode);

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verifyDb.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == _companyId && x.Id == ownActive.Id && x.IsActive && x.Name == ownActive.Name));
        Assert.True(await verifyDb.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == _companyId && x.Id == ownArchived.Id && !x.IsActive && x.Name == ownArchived.Name));
        Assert.True(await verifyDb.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == foreignActive.Id && x.IsActive && x.Name == foreignActive.Name));
        Assert.True(await verifyDb.SupplierCatalogEntries.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == foreignArchived.Id && !x.IsActive && x.Name == foreignArchived.Name));
        Assert.False(await verifyDb.AuditEntries.AnyAsync(x => x.CompanyId == _otherCompanyId &&
            x.TargetType == "Supplier" && (x.TargetId == foreignActive.Id || x.TargetId == foreignArchived.Id)));
    }

    [Fact]
    public async Task Purchase_order_read_matrix_hides_foreign_records_for_all_order_families()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var suffix = Guid.NewGuid().ToString("N");
        var cases = new[]
        {
            (Name: "stock", Route: "/api/stock-orders", OwnId: Guid.NewGuid(), ForeignId: Guid.NewGuid()),
            (Name: "accessory", Route: "/api/accessory-orders", OwnId: Guid.NewGuid(), ForeignId: Guid.NewGuid()),
            (Name: "asset", Route: "/api/asset-orders", OwnId: Guid.NewGuid(), ForeignId: Guid.NewGuid())
        };

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var item in cases)
            {
                switch (item.Name)
                {
                    case "stock":
                        db.StockPurchaseOrders.AddRange(
                            new StockPurchaseOrder { Id = item.OwnId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}", Supplier = "Own supplier", OrderDate = today },
                            new StockPurchaseOrder { Id = item.ForeignId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}", Supplier = "Foreign supplier", OrderDate = today });
                        break;
                    case "accessory":
                        db.AccessoryPurchaseOrders.AddRange(
                            new AccessoryPurchaseOrder { Id = item.OwnId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}", Supplier = "Own supplier", OrderDate = today },
                            new AccessoryPurchaseOrder { Id = item.ForeignId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}", Supplier = "Foreign supplier", OrderDate = today });
                        break;
                    default:
                        db.AssetPurchaseOrders.AddRange(
                            new AssetPurchaseOrder { Id = item.OwnId, CompanyId = _companyId, OrderNumber = $"OWN-{item.Name}-{suffix}", Supplier = "Own supplier", OrderDate = today },
                            new AssetPurchaseOrder { Id = item.ForeignId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}", Supplier = "Foreign supplier", OrderDate = today });
                        break;
                }
            }

            await db.SaveChangesAsync();
        }

        foreach (var item in cases)
        {
            using var ownDetail = await SendAsAdminAsync($"{item.Route}/{item.OwnId}");
            Assert.Equal(HttpStatusCode.OK, ownDetail.StatusCode);

            using var foreignDetail = await SendAsAdminAsync($"{item.Route}/{item.ForeignId}");
            Assert.Equal(HttpStatusCode.NotFound, foreignDetail.StatusCode);

            var listPath = $"{item.Route}?page=1&pageSize=100";
            using var collection = await SendAsAdminAsync(listPath);
            var collectionBody = await collection.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, collection.StatusCode);
            Assert.Contains(item.OwnId.ToString("D"), collectionBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(item.ForeignId.ToString("D"), collectionBody, StringComparison.OrdinalIgnoreCase);

            using var forgedCollection = await SendAsAdminAsync($"{listPath}&companyId={_otherCompanyId:D}");
            var forgedBody = await forgedCollection.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, forgedCollection.StatusCode);
            Assert.Contains(item.OwnId.ToString("D"), forgedBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(item.ForeignId.ToString("D"), forgedBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Purchase_order_write_matrix_rejects_foreign_cancel_and_approval_without_mutation()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var suffix = Guid.NewGuid().ToString("N");
        var cases = new[]
        {
            (Name: "stock", Route: "/api/stock-orders", OrderId: Guid.NewGuid()),
            (Name: "accessory", Route: "/api/accessory-orders", OrderId: Guid.NewGuid()),
            (Name: "asset", Route: "/api/asset-orders", OrderId: Guid.NewGuid())
        };

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var item in cases)
            {
                switch (item.Name)
                {
                    case "stock":
                        db.StockPurchaseOrders.Add(new StockPurchaseOrder
                        {
                            Id = item.OrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}",
                            Supplier = "Foreign supplier", OrderDate = today, Status = "Ordered", ApprovalStatus = "Pending"
                        });
                        break;
                    case "accessory":
                        db.AccessoryPurchaseOrders.Add(new AccessoryPurchaseOrder
                        {
                            Id = item.OrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}",
                            Supplier = "Foreign supplier", OrderDate = today, Status = "Ordered", ApprovalStatus = "Pending"
                        });
                        break;
                    default:
                        db.AssetPurchaseOrders.Add(new AssetPurchaseOrder
                        {
                            Id = item.OrderId, CompanyId = _otherCompanyId, OrderNumber = $"FOREIGN-{item.Name}-{suffix}",
                            Supplier = "Foreign supplier", OrderDate = today, Status = "Ordered", ApprovalStatus = "Pending"
                        });
                        break;
                }
            }

            await db.SaveChangesAsync();
        }

        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = System.Text.Json.JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        foreach (var item in cases)
        {
            using var cancel = await SendAsRoleAsync(HttpMethod.Post, $"{item.Route}/{item.OrderId}/cancel", "admin",
                requestToken: requestToken, antiforgeryCookie: antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);

            using var approval = await SendAsRoleAsync(HttpMethod.Post, $"{item.Route}/{item.OrderId}/approval", "admin",
                System.Net.Http.Json.JsonContent.Create(new { decision = "Approved", note = "Cross-company approval probe" }),
                requestToken, antiforgeryCookie);
            Assert.Equal(HttpStatusCode.NotFound, approval.StatusCode);
        }

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var item in cases)
        {
            var preserved = item.Name switch
            {
                "stock" => await verifyDb.StockPurchaseOrders.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == item.OrderId && x.Status == "Ordered" && x.ApprovalStatus == "Pending"),
                "accessory" => await verifyDb.AccessoryPurchaseOrders.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == item.OrderId && x.Status == "Ordered" && x.ApprovalStatus == "Pending"),
                _ => await verifyDb.AssetPurchaseOrders.AnyAsync(x => x.CompanyId == _otherCompanyId && x.Id == item.OrderId && x.Status == "Ordered" && x.ApprovalStatus == "Pending")
            };
            Assert.True(preserved, $"Foreign {item.Name} order must remain unchanged after rejected writes.");
            Assert.False(await verifyDb.AuditEntries.AnyAsync(x => x.CompanyId == _otherCompanyId && x.TargetId == item.OrderId));
        }
    }

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

    [Fact]
    public async Task Company_profile_and_branding_ignore_forged_foreign_company_selectors_and_writes()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var ownName = $"Tenant A profile {suffix}";
        var foreignName = $"Tenant B profile {suffix}";
        var ownSlug = $"tenant-a-{suffix}";
        var foreignSlug = $"tenant-b-{suffix}";

        await using (var seedScope = _factory!.Services.CreateAsyncScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var own = await seedDb.Companies.SingleAsync(x => x.Id == _companyId);
            var foreign = await seedDb.Companies.SingleAsync(x => x.Id == _otherCompanyId);
            own.Name = ownName;
            own.Slug = ownSlug;
            own.ContactEmail = $"tenant-a-{suffix}@example.invalid";
            own.BrandPrimaryColor = "#1769E0";
            foreign.Name = foreignName;
            foreign.Slug = foreignSlug;
            foreign.ContactEmail = $"tenant-b-{suffix}@example.invalid";
            foreign.BrandPrimaryColor = "#C62828";
            await seedDb.SaveChangesAsync();
        }

        using var csrf = await SendAsAdminAsync("/api/session/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        using var csrfPayload = System.Text.Json.JsonDocument.Parse(await csrf.Content.ReadAsStringAsync());
        var requestToken = csrfPayload.RootElement.GetProperty("requestToken").GetString()!;
        var antiforgeryCookie = csrf.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0])
            .First(value => value.Split('=', 2)[0].Contains("Antiforgery", StringComparison.OrdinalIgnoreCase));

        async Task<HttpResponseMessage> SendForgedAsync(HttpMethod method, string path, HttpContent? content = null)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.Add("X-Acceptance-User", _userId.ToString());
            request.Headers.Add("X-Acceptance-Company", _companyId.ToString());
            request.Headers.Add("X-Acceptance-Role", "admin");
            request.Headers.Add("X-Company-Id", _otherCompanyId.ToString());
            request.Headers.Add("X-CSRF-TOKEN", requestToken);
            request.Headers.Add("Cookie", antiforgeryCookie);
            return await _client!.SendAsync(request);
        }

        using (var profile = await SendForgedAsync(HttpMethod.Get, $"/api/company/profile?companyId={_otherCompanyId:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
            using var body = System.Text.Json.JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
            Assert.Equal(ownName, body.RootElement.GetProperty("name").GetString());
            Assert.Equal(ownSlug, body.RootElement.GetProperty("slug").GetString());
            Assert.DoesNotContain(foreignName, body.RootElement.GetRawText(), StringComparison.Ordinal);
        }

        using (var branding = await SendForgedAsync(HttpMethod.Get, $"/api/company/branding?companyId={_otherCompanyId:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, branding.StatusCode);
            using var body = System.Text.Json.JsonDocument.Parse(await branding.Content.ReadAsStringAsync());
            Assert.Equal(ownName, body.RootElement.GetProperty("name").GetString());
            Assert.Equal("#1769E0", body.RootElement.GetProperty("brandPrimaryColor").GetString());
            Assert.DoesNotContain(foreignName, body.RootElement.GetRawText(), StringComparison.Ordinal);
        }

        var updatedName = $"Tenant A updated {suffix}";
        var updatedSlug = $"tenant-a-updated-{suffix}";
        using (var update = await SendForgedAsync(HttpMethod.Put, $"/api/company/profile?companyId={_otherCompanyId:D}",
            System.Net.Http.Json.JsonContent.Create(new
            {
                name = updatedName,
                slug = updatedSlug,
                contactEmail = $"updated-a-{suffix}@example.invalid",
                brandPrimaryColor = "#22AA66"
            })))
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        await using var verifyScope = _factory!.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedOwn = await verifyDb.Companies.AsNoTracking().SingleAsync(x => x.Id == _companyId);
        var preservedForeign = await verifyDb.Companies.AsNoTracking().SingleAsync(x => x.Id == _otherCompanyId);
        Assert.Equal(updatedName, updatedOwn.Name);
        Assert.Equal(updatedSlug, updatedOwn.Slug);
        Assert.Equal("#22AA66", updatedOwn.BrandPrimaryColor);
        Assert.Equal(foreignName, preservedForeign.Name);
        Assert.Equal(foreignSlug, preservedForeign.Slug);
        Assert.Equal($"tenant-b-{suffix}@example.invalid", preservedForeign.ContactEmail);
        Assert.Equal("#C62828", preservedForeign.BrandPrimaryColor);
        Assert.True(await verifyDb.AuditEntries.AnyAsync(x => x.CompanyId == _companyId && x.TargetId == _companyId && x.Action == "ProfileUpdated"));
        Assert.False(await verifyDb.AuditEntries.AnyAsync(x => x.CompanyId == _otherCompanyId && x.TargetId == _otherCompanyId && x.Action == "ProfileUpdated"));
    }
}
