using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalCalendarIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_Status_PlannedOn",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "Status", "PlannedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetMaintenances_CompanyId_Status_PlannedOn",
                table: "AssetMaintenances");
        }
    }
}
