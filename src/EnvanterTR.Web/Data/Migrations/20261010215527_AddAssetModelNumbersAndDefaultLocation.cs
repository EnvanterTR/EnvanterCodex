using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetModelNumbersAndDefaultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultLocationCatalogId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelNumber",
                table: "AssetModels",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_DefaultLocationCatalogId",
                table: "Assets",
                columns: new[] { "CompanyId", "DefaultLocationCatalogId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId",
                table: "Assets",
                columns: new[] { "CompanyId", "DefaultLocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_DefaultLocationCatalogId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DefaultLocationCatalogId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ModelNumber",
                table: "AssetModels");
        }
    }
}
