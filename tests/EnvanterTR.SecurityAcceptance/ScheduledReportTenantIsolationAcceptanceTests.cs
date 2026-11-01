using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed partial class TenantSessionAcceptanceTests
{
    [Fact]
    public async Task Background_scheduled_report_data_is_limited_to_the_report_company()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var inventory = await ScheduledReportData.BuildAsync(db, new ScheduledReport
        {
            CompanyId = _companyId,
            UserId = _userId,
            Name = "Own inventory",
            ReportKind = "inventory"
        }, CancellationToken.None);

        var licenses = await ScheduledReportData.BuildAsync(db, new ScheduledReport
        {
            CompanyId = _companyId,
            UserId = _userId,
            Name = "Own licenses",
            ReportKind = "licenses"
        }, CancellationToken.None);

        var stock = await ScheduledReportData.BuildAsync(db, new ScheduledReport
        {
            CompanyId = _companyId,
            UserId = _userId,
            Name = "Own stock",
            ReportKind = "stock"
        }, CancellationToken.None);

        // The background context has no request tenant, so EF's request-scoped
        // global filters are intentionally inactive. These assertions prove the
        // report queries themselves still keep every aggregate inside CompanyId.
        Assert.Equal(1, inventory.Rows[0][2]);
        Assert.Equal(1, licenses.Rows[0][2]);
        Assert.Equal(5L, stock.Rows[0][2]);
        Assert.Equal(5L, stock.Rows[3][2]);
        Assert.Equal(5L, stock.Rows[6][2]);
    }
}
