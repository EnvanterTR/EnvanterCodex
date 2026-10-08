using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

public partial class AddPendingAndUnusableAssetStatuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Assets\" DROP CONSTRAINT \"CK_Assets_Status\";");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Assets_Status",
            table: "Assets",
            sql: "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Assets\" DROP CONSTRAINT \"CK_Assets_Status\";");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Assets_Status",
            table: "Assets",
            sql: "\"Status\" IN ('Available', 'Deployed', 'Maintenance', 'Archived')");
    }
}
