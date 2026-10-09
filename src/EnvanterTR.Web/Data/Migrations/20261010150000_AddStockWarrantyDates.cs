using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010150000_AddStockWarrantyDates")]
public sealed class AddStockWarrantyDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(name: "WarrantyExpiresAt", table: "Consumables", type: "date", nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "WarrantyExpiresAt", table: "InventoryComponents", type: "date", nullable: true);
        migrationBuilder.AddCheckConstraint("CK_Consumables_WarrantyAfterPurchase", "Consumables",
            "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
        migrationBuilder.AddCheckConstraint("CK_InventoryComponents_WarrantyAfterPurchase", "InventoryComponents",
            "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_Consumables_WarrantyAfterPurchase", "Consumables");
        migrationBuilder.DropCheckConstraint("CK_InventoryComponents_WarrantyAfterPurchase", "InventoryComponents");
        migrationBuilder.DropColumn("WarrantyExpiresAt", "Consumables");
        migrationBuilder.DropColumn("WarrantyExpiresAt", "InventoryComponents");
    }
}
