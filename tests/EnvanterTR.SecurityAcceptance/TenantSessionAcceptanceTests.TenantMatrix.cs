using System.Net;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Tenant_read_matrix_hides_foreign_records_from_detail_and_collection_routes()
    {
        var cases = new (string Resource, Guid ForeignId, string CollectionPath, string DetailPath)[]
        {
            ("asset", _otherCompanyAssetId, "/api/assets?page=1&pageSize=50", $"/api/assets/{_otherCompanyAssetId}"),
            ("employee", _otherCompanyEmployeeId, "/api/employees?page=1&pageSize=50", $"/api/employees/{_otherCompanyEmployeeId}"),
            ("software license", _otherCompanyLicenseId, "/api/software-licenses?status=all", $"/api/software-licenses/{_otherCompanyLicenseId}"),
            ("accessory", _otherCompanyAccessoryId, "/api/accessories?includeArchived=true", $"/api/accessories/{_otherCompanyAccessoryId}"),
            ("consumable", _otherCompanyConsumableId, "/api/consumables?includeArchived=true", $"/api/consumables/{_otherCompanyConsumableId}"),
            ("component", _otherCompanyComponentId, "/api/components?includeArchived=true", $"/api/components/{_otherCompanyComponentId}")
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
        }
    }
}
