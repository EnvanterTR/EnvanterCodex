using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveSoftwareLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "SoftwareLicenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_CompanyId_IsArchived_Name",
                table: "SoftwareLicenses",
                columns: new[] { "CompanyId", "IsArchived", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenses_CompanyId_IsArchived_Name",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "SoftwareLicenses");
        }
    }
}
