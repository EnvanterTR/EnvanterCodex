using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010120000_AddAccessoryWarranty")]
public sealed class AddAccessoryWarranty : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "WarrantyExpiresAt",
            table: "Accessories",
            type: "date",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Accessories_WarrantyAfterPurchase",
            table: "Accessories",
            sql: "\"WarrantyExpiresAt\" IS NULL OR \"PurchaseDate\" IS NULL OR \"WarrantyExpiresAt\" >= \"PurchaseDate\"");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Accessories_WarrantyAfterPurchase",
            table: "Accessories");

        migrationBuilder.DropColumn(
            name: "WarrantyExpiresAt",
            table: "Accessories");
    }
}
