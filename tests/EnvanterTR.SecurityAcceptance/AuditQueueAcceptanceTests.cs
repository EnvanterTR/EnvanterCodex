using System.Net;
using System.Text.Json;
using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Completed_audit_queue_preserves_history_filters_tenants_dates_and_pages_stably()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var occurred = new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));
        var categoryId = await GetOwnCategoryIdAsync();
        var archived = new Asset { CompanyId = _companyId, CategoryId = categoryId, AssetTag = "ZZ-AUDIT-ARCHIVE",
            Name = "Archived audited asset", Status = AssetStatuses.Archived };
        var deleted = new Asset { CompanyId = _companyId, CategoryId = categoryId, AssetTag = "DELETED-AUDIT",
            Name = "Deleted audited asset", DeletedAt = DateTimeOffset.UtcNow };
        var orderedIds = Enumerable.Range(1, 3).Select(index => Guid.Parse($"aaaaaaaa-aaaa-4aaa-8aaa-{index:000000000000}")).ToArray();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // The shared fixture includes an audit report example; isolate this queue's exact paging population.
            await db.AssetEvents.Where(item => item.CompanyId == _companyId &&
                (item.EventType == "Audited" || item.EventType == "AuditUnacceptable")).ExecuteDeleteAsync();
            db.Assets.AddRange(archived, deleted);
            foreach (var id in orderedIds.Reverse())
                db.AssetEvents.Add(new AssetEvent { Id = id, CompanyId = _companyId, AssetId = _ownAssetId,
                    EventType = id == orderedIds[2] ? "AuditUnacceptable" : "Audited", OccurredAt = occurred,
                    Details = id == orderedIds[0] ? "queue-completed-marker" : "inspection finding" });
            db.AssetEvents.AddRange(
                new AssetEvent { CompanyId = _companyId, AssetId = archived.Id, EventType = "Audited", OccurredAt = occurred },
                new AssetEvent { CompanyId = _companyId, AssetId = deleted.Id, EventType = "Audited", OccurredAt = occurred },
                new AssetEvent { CompanyId = _otherCompanyId, AssetId = _otherCompanyAssetId, EventType = "Audited", OccurredAt = occurred },
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, EventType = "Audited", OccurredAt = occurred.AddDays(-8) },
                new AssetEvent { CompanyId = _companyId, AssetId = _ownAssetId, EventType = "Created", OccurredAt = occurred });
            await db.SaveChangesAsync();
        }
        var path = $"/api/assets/audit-schedule?state=completed&since={today.AddDays(-7):yyyy-MM-dd}&through={today:yyyy-MM-dd}";
        using (var response = await SendAsAdminAsync(path))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(body);
            Assert.Equal(4, payload.RootElement.GetProperty("completed").GetInt32());
            Assert.Equal(4, payload.RootElement.GetProperty("total").GetInt32());
            var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(items, item => item.GetProperty("id").GetGuid() == archived.Id);
            Assert.Contains(items, item => item.GetProperty("outcome").GetString() == "Unacceptable");
            Assert.DoesNotContain(items, item => item.GetProperty("id").GetGuid() == deleted.Id || item.GetProperty("id").GetGuid() == _otherCompanyAssetId);
            Assert.DoesNotContain("DELETED-AUDIT", body, StringComparison.Ordinal);
            Assert.Equal(orderedIds, items.Take(3).Select(item => item.GetProperty("auditEventId").GetGuid()).ToArray());
        }
        for (var page = 1; page <= 3; page++)
        {
            using var response = await SendAsAdminAsync(path + $"&page={page}&pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(orderedIds[page - 1], payload.RootElement.GetProperty("items")[0].GetProperty("auditEventId").GetGuid());
        }
        using (var search = await SendAsAdminAsync(path + "&q=queue-completed-marker"))
        {
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            using var payload = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
            Assert.Equal(1, payload.RootElement.GetProperty("total").GetInt32());
            Assert.Equal(orderedIds[0], payload.RootElement.GetProperty("items")[0].GetProperty("auditEventId").GetGuid());
        }
        using (var viewer = await SendAsRoleAsync(HttpMethod.Get, path, "viewer"))
            Assert.Equal(HttpStatusCode.OK, viewer.StatusCode);
        using (var invalid = await SendAsAdminAsync($"/api/assets/audit-schedule?state=completed&since={today.AddDays(-367):yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (var future = await SendAsAdminAsync($"/api/assets/audit-schedule?state=completed&since={today.AddDays(1):yyyy-MM-dd}"))
            Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
    }
}
