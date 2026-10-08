using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryPurchaseOrderFinancialAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AccessoryPurchaseOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightAmount",
                table: "AccessoryPurchaseOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercent",
                table: "AccessoryPurchaseOrders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryPurchaseOrders_FinancialAdjustments",
                table: "AccessoryPurchaseOrders",
                sql: "\"TaxRatePercent\" BETWEEN 0 AND 100 AND \"DiscountAmount\" >= 0 AND \"FreightAmount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryPurchaseOrders_FinancialAdjustments",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FreightAmount",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "AccessoryPurchaseOrders");
        }
    }
}
