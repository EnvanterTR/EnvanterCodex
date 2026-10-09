using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

public partial class AddSupplierCatalogLogos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "LogoFileName", table: "SupplierCatalogEntries", type: "character varying(40)", maxLength: 40, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "LogoFileName", table: "SupplierCatalogEntries");
}
