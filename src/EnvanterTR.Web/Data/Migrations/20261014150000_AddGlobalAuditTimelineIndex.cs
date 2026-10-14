using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014150000_AddGlobalAuditTimelineIndex")]
public sealed class AddGlobalAuditTimelineIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(name: "IX_AuditEntries_OccurredAt", table: "AuditEntries", column: "OccurredAt");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(name: "IX_AuditEntries_OccurredAt", table: "AuditEntries");
}
