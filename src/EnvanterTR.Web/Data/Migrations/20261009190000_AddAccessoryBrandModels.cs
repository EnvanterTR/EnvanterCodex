using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryBrandModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "Accessories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModelId",
                table: "Accessories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessoryModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PhotoFileName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryModels", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryModels_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccessoryModels_AssetBrands_CompanyId_BrandId",
                        columns: x => new { x.CompanyId, x.BrandId },
                        principalTable: "AssetBrands",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessoryModels_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_BrandId",
                table: "Accessories",
                columns: new[] { "CompanyId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_ModelId",
                table: "Accessories",
                columns: new[] { "CompanyId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryModels_CompanyId_BrandId_Name",
                table: "AccessoryModels",
                columns: new[] { "CompanyId", "BrandId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_AccessoryModels_CompanyId_ModelId",
                table: "Accessories",
                columns: new[] { "CompanyId", "ModelId" },
                principalTable: "AccessoryModels",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_AssetBrands_CompanyId_BrandId",
                table: "Accessories",
                columns: new[] { "CompanyId", "BrandId" },
                principalTable: "AssetBrands",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_AccessoryModels_CompanyId_ModelId",
                table: "Accessories");

            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_AssetBrands_CompanyId_BrandId",
                table: "Accessories");

            migrationBuilder.DropTable(
                name: "AccessoryModels");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_BrandId",
                table: "Accessories");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_ModelId",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Accessories");
        }
    }
}
