using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkAssetsToPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_PurchaseOrderId",
                table: "Assets",
                columns: new[] { "CompanyId", "PurchaseOrderId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetPurchaseOrders_CompanyId_PurchaseOrderId",
                table: "Assets",
                columns: new[] { "CompanyId", "PurchaseOrderId" },
                principalTable: "AssetPurchaseOrders",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetPurchaseOrders_CompanyId_PurchaseOrderId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_PurchaseOrderId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "Assets");
        }
    }
}
