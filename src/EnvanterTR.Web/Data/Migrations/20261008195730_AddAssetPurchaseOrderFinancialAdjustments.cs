using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetPurchaseOrderFinancialAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AssetPurchaseOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightAmount",
                table: "AssetPurchaseOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercent",
                table: "AssetPurchaseOrders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetPurchaseOrders_FinancialAdjustments",
                table: "AssetPurchaseOrders",
                sql: "\"TaxRatePercent\" BETWEEN 0 AND 100 AND \"DiscountAmount\" >= 0 AND \"FreightAmount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetPurchaseOrders_FinancialAdjustments",
                table: "AssetPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AssetPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FreightAmount",
                table: "AssetPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "AssetPurchaseOrders");
        }
    }
}
