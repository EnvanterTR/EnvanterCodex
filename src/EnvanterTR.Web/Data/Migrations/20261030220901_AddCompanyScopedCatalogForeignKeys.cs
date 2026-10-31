using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyScopedCatalogForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_BrandId",
                table: "Assets",
                columns: new[] { "CompanyId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_ModelId",
                table: "Assets",
                columns: new[] { "CompanyId", "ModelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetModels_AssetBrands_CompanyId_BrandId",
                table: "AssetModels",
                columns: new[] { "CompanyId", "BrandId" },
                principalTable: "AssetBrands",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetBrands_CompanyId_BrandId",
                table: "Assets",
                columns: new[] { "CompanyId", "BrandId" },
                principalTable: "AssetBrands",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetModels_CompanyId_ModelId",
                table: "Assets",
                columns: new[] { "CompanyId", "ModelId" },
                principalTable: "AssetModels",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetModels_AssetBrands_CompanyId_BrandId",
                table: "AssetModels");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetBrands_CompanyId_BrandId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetModels_CompanyId_ModelId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_BrandId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_ModelId",
                table: "Assets");
        }
    }
}
