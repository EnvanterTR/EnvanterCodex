using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkSupplierCatalogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "SoftwareLicenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "InventoryComponents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "Consumables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "AssetMaintenances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "Accessories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_CompanyId_SupplierId",
                table: "SoftwareLicenses",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryComponents_CompanyId_SupplierId",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_CompanyId_SupplierId",
                table: "Consumables",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_SupplierId",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_SupplierId",
                table: "Accessories",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.Sql("""
                INSERT INTO "SupplierCatalogEntries" ("Id", "CompanyId", "Name", "NormalizedName", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), source."CompanyId", min(btrim(source."Supplier")), upper(btrim(source."Supplier")), TRUE, now(), now()
                FROM (
                    SELECT "CompanyId", "Supplier" FROM "Accessories"
                    UNION ALL SELECT "CompanyId", "Supplier" FROM "Consumables"
                    UNION ALL SELECT "CompanyId", "Supplier" FROM "InventoryComponents"
                    UNION ALL SELECT "CompanyId", "Supplier" FROM "SoftwareLicenses"
                    UNION ALL SELECT "CompanyId", "Supplier" FROM "AssetMaintenances"
                ) AS source
                WHERE source."Supplier" IS NOT NULL AND btrim(source."Supplier") <> ''
                GROUP BY source."CompanyId", upper(btrim(source."Supplier"))
                ON CONFLICT ("CompanyId", "NormalizedName") DO NOTHING;

                UPDATE "Accessories" AS records SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE records."CompanyId" = suppliers."CompanyId" AND records."Supplier" IS NOT NULL
                  AND suppliers."NormalizedName" = upper(btrim(records."Supplier"));
                UPDATE "Consumables" AS records SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE records."CompanyId" = suppliers."CompanyId" AND records."Supplier" IS NOT NULL
                  AND suppliers."NormalizedName" = upper(btrim(records."Supplier"));
                UPDATE "InventoryComponents" AS records SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE records."CompanyId" = suppliers."CompanyId" AND records."Supplier" IS NOT NULL
                  AND suppliers."NormalizedName" = upper(btrim(records."Supplier"));
                UPDATE "SoftwareLicenses" AS records SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE records."CompanyId" = suppliers."CompanyId" AND records."Supplier" IS NOT NULL
                  AND suppliers."NormalizedName" = upper(btrim(records."Supplier"));
                UPDATE "AssetMaintenances" AS records SET "SupplierId" = suppliers."Id"
                FROM "SupplierCatalogEntries" AS suppliers
                WHERE records."CompanyId" = suppliers."CompanyId" AND records."Supplier" IS NOT NULL
                  AND suppliers."NormalizedName" = upper(btrim(records."Supplier"));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "Accessories",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetMaintenances_SupplierCatalogEntries_CompanyId_Supplier~",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "Consumables",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryComponents_SupplierCatalogEntries_CompanyId_Suppli~",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenses_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "SoftwareLicenses",
                columns: new[] { "CompanyId", "SupplierId" },
                principalTable: "SupplierCatalogEntries",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "Accessories");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetMaintenances_SupplierCatalogEntries_CompanyId_Supplier~",
                table: "AssetMaintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumables_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "Consumables");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryComponents_SupplierCatalogEntries_CompanyId_Suppli~",
                table: "InventoryComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenses_SupplierCatalogEntries_CompanyId_SupplierId",
                table: "SoftwareLicenses");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenses_CompanyId_SupplierId",
                table: "SoftwareLicenses");

            migrationBuilder.DropIndex(
                name: "IX_InventoryComponents_CompanyId_SupplierId",
                table: "InventoryComponents");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_CompanyId_SupplierId",
                table: "Consumables");

            migrationBuilder.DropIndex(
                name: "IX_AssetMaintenances_CompanyId_SupplierId",
                table: "AssetMaintenances");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_SupplierId",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "InventoryComponents");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "AssetMaintenances");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Accessories");
        }
    }
}
