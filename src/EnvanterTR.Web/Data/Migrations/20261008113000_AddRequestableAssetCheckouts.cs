using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

public partial class AddRequestableAssetCheckouts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsRequestable", table: "Assets", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.DropCheckConstraint(name: "CK_InventoryRequests_ItemType", table: "InventoryRequests");
        migrationBuilder.AddCheckConstraint(name: "CK_InventoryRequests_ItemType", table: "InventoryRequests",
            sql: "\"ItemType\" IN ('Accessory', 'Consumable', 'Asset')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_InventoryRequests_ItemType", table: "InventoryRequests");
        migrationBuilder.AddCheckConstraint(name: "CK_InventoryRequests_ItemType", table: "InventoryRequests",
            sql: "\"ItemType\" IN ('Accessory', 'Consumable')");
        migrationBuilder.DropColumn(name: "IsRequestable", table: "Assets");
    }
}
