using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPurchaseBudgetApprovalToAccessoryAndStockOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "StockPurchaseOrders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "StockPurchaseOrders",
                type: "text",
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "StockPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "StockPurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "AccessoryPurchaseOrders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "AccessoryPurchaseOrders",
                type: "text",
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "AccessoryPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "AccessoryPurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_CompanyId_ApprovalStatus_OrderDate",
                table: "StockPurchaseOrders",
                columns: new[] { "CompanyId", "ApprovalStatus", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_ReviewedByUserId",
                table: "StockPurchaseOrders",
                column: "ReviewedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockPurchaseOrders_ApprovalStatus",
                table: "StockPurchaseOrders",
                sql: "\"ApprovalStatus\" IN ('NotRequired', 'Pending', 'Approved', 'Rejected')");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_ApprovalStatus_OrderDate",
                table: "AccessoryPurchaseOrders",
                columns: new[] { "CompanyId", "ApprovalStatus", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_ReviewedByUserId",
                table: "AccessoryPurchaseOrders",
                column: "ReviewedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryPurchaseOrders_ApprovalStatus",
                table: "AccessoryPurchaseOrders",
                sql: "\"ApprovalStatus\" IN ('NotRequired', 'Pending', 'Approved', 'Rejected')");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryPurchaseOrders_AspNetUsers_ReviewedByUserId",
                table: "AccessoryPurchaseOrders",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockPurchaseOrders_AspNetUsers_ReviewedByUserId",
                table: "StockPurchaseOrders",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryPurchaseOrders_AspNetUsers_ReviewedByUserId",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_StockPurchaseOrders_AspNetUsers_ReviewedByUserId",
                table: "StockPurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_StockPurchaseOrders_CompanyId_ApprovalStatus_OrderDate",
                table: "StockPurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_StockPurchaseOrders_ReviewedByUserId",
                table: "StockPurchaseOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockPurchaseOrders_ApprovalStatus",
                table: "StockPurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_ApprovalStatus_OrderDate",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryPurchaseOrders_ReviewedByUserId",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryPurchaseOrders_ApprovalStatus",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "StockPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "StockPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "StockPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "StockPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "AccessoryPurchaseOrders");
        }
    }
}
