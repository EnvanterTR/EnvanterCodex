using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetPurchaseOrderLineLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "AssetPurchaseOrderLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_LocationCatalogId",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetPurchaseOrderLines_CompanyLocations_CompanyId_Location~",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetPurchaseOrderLines_CompanyLocations_CompanyId_Location~",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_LocationCatalogId",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "AssetPurchaseOrderLines");
        }
    }
}
