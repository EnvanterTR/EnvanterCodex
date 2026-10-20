using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierReturnedStockMovementTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ConsumableMovements_Type",
                table: "ConsumableMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ComponentMovements_Type",
                table: "ComponentMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryMovements_Type",
                table: "AccessoryMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConsumableMovements_Type",
                table: "ConsumableMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'SupplierReturned')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ComponentMovements_Type",
                table: "ComponentMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin', 'SupplierReturned')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryMovements_Type",
                table: "AccessoryMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin', 'SupplierReturned')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ConsumableMovements_Type",
                table: "ConsumableMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ComponentMovements_Type",
                table: "ComponentMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryMovements_Type",
                table: "AccessoryMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConsumableMovements_Type",
                table: "ConsumableMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ComponentMovements_Type",
                table: "ComponentMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryMovements_Type",
                table: "AccessoryMovements",
                sql: "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");
        }
    }
}
