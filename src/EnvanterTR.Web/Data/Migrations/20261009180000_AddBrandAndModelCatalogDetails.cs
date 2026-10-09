using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandAndModelCatalogDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AssetModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "AssetModels",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFileName",
                table: "AssetBrands",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                table: "AssetBrands",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportPhone",
                table: "AssetBrands",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportUrl",
                table: "AssetBrands",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyUrl",
                table: "AssetBrands",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "AssetBrands",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetModels_CompanyId_CategoryId",
                table: "AssetModels",
                columns: new[] { "CompanyId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetModels_AssetCategories_CompanyId_CategoryId",
                table: "AssetModels",
                columns: new[] { "CompanyId", "CategoryId" },
                principalTable: "AssetCategories",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetModels_AssetCategories_CompanyId_CategoryId",
                table: "AssetModels");

            migrationBuilder.DropIndex(
                name: "IX_AssetModels_CompanyId_CategoryId",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "LogoFileName",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "SupportPhone",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "SupportUrl",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "WarrantyUrl",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "AssetBrands");
        }
    }
}
