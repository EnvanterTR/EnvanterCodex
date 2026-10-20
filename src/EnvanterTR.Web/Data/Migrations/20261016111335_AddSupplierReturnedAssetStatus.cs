using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierReturnedAssetStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets",
                sql: "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived', 'ReturnedToSupplier')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_Status",
                table: "Assets",
                sql: "\"Status\" IN ('Available', 'Deployed', 'Pending', 'Maintenance', 'Unusable', 'Archived')");
        }
    }
}
