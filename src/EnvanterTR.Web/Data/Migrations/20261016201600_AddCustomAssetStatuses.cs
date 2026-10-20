using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    public partial class AddCustomAssetStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Assets_Status", table: "Assets");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets",
                sql: "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived', 'ReturnedToSupplier') OR \"Status\" ~ '^Custom_[a-f0-9]{8}$'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Assets_Status", table: "Assets");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets",
                sql: "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived', 'ReturnedToSupplier')");
        }
    }
}