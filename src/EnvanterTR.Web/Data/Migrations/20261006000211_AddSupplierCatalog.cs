using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "AccessoryPurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierCatalogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCatalogEntries", x => x.Id);
                    table.UniqueConstraint("AK_SupplierCatalogEntries_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_SupplierCatalogEntries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_SupplierId",
                table: "AccessoryPurchaseOrders",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCatalogEntries_CompanyId_IsActive_Name",
                table: "SupplierCatalogEntries",
                columns: new[] { "CompanyId", "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCatalogEntries_CompanyId_NormalizedName",
                table: "SupplierCatalogEntries",
                columns: new[] { "CompanyId", "NormalizedName" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "SupplierCatalogEntries" ("Id", "CompanyId", "Name", "NormalizedName", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), source."CompanyId", min(btrim(source."Name")), upper(btrim(source."Name")), TRUE, now(), now()
                FROM (
                    SELECT "CompanyId", "Supplier" AS "Name" FROM "AccessoryPurchaseOrders"
                    UNION ALL
                    SELECT "CompanyId", "Supplier" AS "Name" FROM "Accessories" WHERE "Supplier" IS NOT NULL
                ) AS source
                WHERE source."Name" IS NOT NULL AND btrim(source."Name") <> ''
                GROUP BY source."CompanyId", upper(btrim(source."Name"));

                UPDATE "AccessoryPurchaseOrders" AS orders
                SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE suppliers."CompanyId" = orders."CompanyId"
                  AND suppliers."NormalizedName" = upper(btrim(orders."Supplier"));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryPurchaseOrders_SupplierCatalogEntries_CompanyId_Su~",
                table: "AccessoryPurchaseOrders",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryPurchaseOrders_SupplierCatalogEntries_CompanyId_Su~",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropTable(
                name: "SupplierCatalogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_SupplierId",
                table: "AccessoryPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "AccessoryPurchaseOrders");
        }
    }
}
