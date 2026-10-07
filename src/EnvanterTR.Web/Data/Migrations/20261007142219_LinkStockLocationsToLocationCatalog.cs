using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkStockLocationsToLocationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "InventoryComponents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "Consumables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "Accessories",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                WITH RECURSIVE location_paths AS (
                    SELECT "Id", "CompanyId", "ParentLocationId", CAST("Name" AS text) AS "Path", 0 AS "Depth"
                    FROM "CompanyLocations" WHERE "ParentLocationId" IS NULL
                    UNION ALL
                    SELECT child."Id", child."CompanyId", child."ParentLocationId",
                           parent."Path" || ' / ' || child."Name", parent."Depth" + 1
                    FROM "CompanyLocations" AS child
                    INNER JOIN location_paths AS parent ON child."CompanyId" = parent."CompanyId"
                        AND child."ParentLocationId" = parent."Id"
                    WHERE parent."Depth" < 2
                )
                UPDATE "Accessories" AS stock
                SET "LocationCatalogId" = location_paths."Id"
                FROM location_paths
                WHERE stock."CompanyId" = location_paths."CompanyId" AND stock."Location" IS NOT NULL
                    AND lower(btrim(stock."Location")) = lower(location_paths."Path");

                WITH RECURSIVE location_paths AS (
                    SELECT "Id", "CompanyId", "ParentLocationId", CAST("Name" AS text) AS "Path", 0 AS "Depth"
                    FROM "CompanyLocations" WHERE "ParentLocationId" IS NULL
                    UNION ALL
                    SELECT child."Id", child."CompanyId", child."ParentLocationId",
                           parent."Path" || ' / ' || child."Name", parent."Depth" + 1
                    FROM "CompanyLocations" AS child
                    INNER JOIN location_paths AS parent ON child."CompanyId" = parent."CompanyId"
                        AND child."ParentLocationId" = parent."Id"
                    WHERE parent."Depth" < 2
                )
                UPDATE "Consumables" AS stock
                SET "LocationCatalogId" = location_paths."Id"
                FROM location_paths
                WHERE stock."CompanyId" = location_paths."CompanyId" AND stock."Location" IS NOT NULL
                    AND lower(btrim(stock."Location")) = lower(location_paths."Path");

                WITH RECURSIVE location_paths AS (
                    SELECT "Id", "CompanyId", "ParentLocationId", CAST("Name" AS text) AS "Path", 0 AS "Depth"
                    FROM "CompanyLocations" WHERE "ParentLocationId" IS NULL
                    UNION ALL
                    SELECT child."Id", child."CompanyId", child."ParentLocationId",
                           parent."Path" || ' / ' || child."Name", parent."Depth" + 1
                    FROM "CompanyLocations" AS child
                    INNER JOIN location_paths AS parent ON child."CompanyId" = parent."CompanyId"
                        AND child."ParentLocationId" = parent."Id"
                    WHERE parent."Depth" < 2
                )
                UPDATE "InventoryComponents" AS stock
                SET "LocationCatalogId" = location_paths."Id"
                FROM location_paths
                WHERE stock."CompanyId" = location_paths."CompanyId" AND stock."Location" IS NOT NULL
                    AND lower(btrim(stock."Location")) = lower(location_paths."Path");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryComponents_CompanyId_LocationCatalogId",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_CompanyId_LocationCatalogId",
                table: "Consumables",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_LocationCatalogId",
                table: "Accessories",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Accessories",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Consumables",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryComponents_CompanyLocations_CompanyId_LocationCata~",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Accessories");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumables_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Consumables");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryComponents_CompanyLocations_CompanyId_LocationCata~",
                table: "InventoryComponents");

            migrationBuilder.DropIndex(
                name: "IX_InventoryComponents_CompanyId_LocationCatalogId",
                table: "InventoryComponents");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_CompanyId_LocationCatalogId",
                table: "Consumables");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_LocationCatalogId",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "InventoryComponents");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "Accessories");
        }
    }
}
