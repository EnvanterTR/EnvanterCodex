using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014150000_AddGlobalAuditTimelineIndex")]
public sealed class AddGlobalAuditTimelineIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // A failed CONCURRENTLY build can leave an invalid index behind. Remove it
        // outside a transaction so a later migration retry starts from a clean state.
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_AuditEntries_OccurredAt\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "CREATE INDEX CONCURRENTLY \"IX_AuditEntries_OccurredAt\" ON \"AuditEntries\" (\"OccurredAt\")",
            suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_AuditEntries_OccurredAt\"",
            suppressTransaction: true);
}
