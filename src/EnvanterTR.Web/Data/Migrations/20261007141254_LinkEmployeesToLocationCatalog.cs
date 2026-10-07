using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkEmployeesToLocationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                WITH RECURSIVE location_paths AS (
                    SELECT "Id", "CompanyId", "ParentLocationId", CAST("Name" AS text) AS "Path", 0 AS "Depth"
                    FROM "CompanyLocations"
                    WHERE "ParentLocationId" IS NULL
                    UNION ALL
                    SELECT child."Id", child."CompanyId", child."ParentLocationId",
                           parent."Path" || ' / ' || child."Name", parent."Depth" + 1
                    FROM "CompanyLocations" AS child
                    INNER JOIN location_paths AS parent
                        ON child."CompanyId" = parent."CompanyId"
                        AND child."ParentLocationId" = parent."Id"
                    WHERE parent."Depth" < 2
                )
                UPDATE "Employees" AS employee
                SET "LocationCatalogId" = location_paths."Id"
                FROM location_paths
                WHERE employee."CompanyId" = location_paths."CompanyId"
                    AND employee."Location" IS NOT NULL
                    AND lower(btrim(employee."Location")) = lower(location_paths."Path");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_LocationCatalogId",
                table: "Employees",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Employees",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_CompanyLocations_CompanyId_LocationCatalogId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_LocationCatalogId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "Employees");
        }
    }
}
