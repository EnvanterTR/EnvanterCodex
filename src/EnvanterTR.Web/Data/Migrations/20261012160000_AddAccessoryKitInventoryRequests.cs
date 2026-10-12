using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryKitInventoryRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryRequests_ItemType",
                table: "InventoryRequests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryRequests_ItemType",
                table: "InventoryRequests",
                sql: "\"ItemType\" IN ('Accessory', 'Consumable', 'Asset', 'AccessoryKit')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"InventoryRequests\" WHERE \"ItemType\" = 'AccessoryKit') THEN RAISE EXCEPTION 'Cannot remove accessory kit request support while kit requests exist.'; END IF; END $$;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryRequests_ItemType",
                table: "InventoryRequests");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryRequests_ItemType",
                table: "InventoryRequests",
                sql: "\"ItemType\" IN ('Accessory', 'Consumable', 'Asset')");
        }
    }
}
