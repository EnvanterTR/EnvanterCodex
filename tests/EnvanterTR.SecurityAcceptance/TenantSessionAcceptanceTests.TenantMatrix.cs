using System.Net;
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
}
