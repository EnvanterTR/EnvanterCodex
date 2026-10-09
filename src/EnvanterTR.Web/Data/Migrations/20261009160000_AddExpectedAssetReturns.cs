using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpectedAssetReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpectedReturnOn",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_ExpectedReturnOn",
                table: "Assets",
                columns: new[] { "CompanyId", "ExpectedReturnOn" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_ExpectedReturn",
                table: "Assets",
                sql: "\"ExpectedReturnOn\" IS NULL OR (\"Status\" = 'Deployed' AND \"AssignedEmployeeId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Assets_ExpectedReturn",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_ExpectedReturnOn",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ExpectedReturnOn",
                table: "Assets");
        }
    }
}
