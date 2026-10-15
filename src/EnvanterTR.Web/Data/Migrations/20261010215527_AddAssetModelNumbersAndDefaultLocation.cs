using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetModelNumbersAndDefaultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" ADD COLUMN IF NOT EXISTS \"DefaultLocationCatalogId\" uuid NULL");
            migrationBuilder.Sql(
                "ALTER TABLE \"AssetModels\" ADD COLUMN IF NOT EXISTS \"ModelNumber\" character varying(120) NULL");

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS \"IX_Assets_CompanyId_DefaultLocationCatalogId\"",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY \"IX_Assets_CompanyId_DefaultLocationCatalogId\" ON \"Assets\" (\"CompanyId\", \"DefaultLocationCatalogId\")",
                suppressTransaction: true);

            // Separate transactions ensure the short ADD CONSTRAINT lock is released
            // before PostgreSQL scans the existing Assets rows to validate the FK.
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" DROP CONSTRAINT IF EXISTS \"FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId\"",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" ADD CONSTRAINT \"FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId\" FOREIGN KEY (\"CompanyId\", \"DefaultLocationCatalogId\") REFERENCES \"CompanyLocations\" (\"CompanyId\", \"Id\") ON DELETE RESTRICT NOT VALID",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" VALIDATE CONSTRAINT \"FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId\"",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" DROP CONSTRAINT IF EXISTS \"FK_Assets_CompanyLocations_CompanyId_DefaultLocationCatalogId\"",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS \"IX_Assets_CompanyId_DefaultLocationCatalogId\"",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "ALTER TABLE \"Assets\" DROP COLUMN IF EXISTS \"DefaultLocationCatalogId\"",
                suppressTransaction: true);
            migrationBuilder.Sql(
                "ALTER TABLE \"AssetModels\" DROP COLUMN IF EXISTS \"ModelNumber\"",
                suppressTransaction: true);
        }
    }
}
