using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportMappingFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedAssetImportMappings_CompanyId_UserId_NormalizedName",
                table: "SavedAssetImportMappings");

            migrationBuilder.AddColumn<string>(
                name: "Family",
                table: "SavedAssetImportMappings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "assets");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAssetImportMappings_CompanyId_UserId_Family_Normalized~",
                table: "SavedAssetImportMappings",
                columns: new[] { "CompanyId", "UserId", "Family", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedAssetImportMappings_CompanyId_UserId_Family_Normalized~",
                table: "SavedAssetImportMappings");

            migrationBuilder.DropColumn(
                name: "Family",
                table: "SavedAssetImportMappings");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAssetImportMappings_CompanyId_UserId_NormalizedName",
                table: "SavedAssetImportMappings",
                columns: new[] { "CompanyId", "UserId", "NormalizedName" },
                unique: true);
        }
    }
}
