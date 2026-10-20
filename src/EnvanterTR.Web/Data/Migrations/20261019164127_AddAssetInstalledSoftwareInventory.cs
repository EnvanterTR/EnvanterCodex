using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetInstalledSoftwareInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetInstalledSoftware",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Publisher = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Architecture = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InstallDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsPresent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetInstalledSoftware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetInstalledSoftware_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetInstalledSoftware_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetInstalledSoftware_CompanyId_AssetId_Fingerprint",
                table: "AssetInstalledSoftware",
                columns: new[] { "CompanyId", "AssetId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetInstalledSoftware_CompanyId_AssetId_IsPresent_Name",
                table: "AssetInstalledSoftware",
                columns: new[] { "CompanyId", "AssetId", "IsPresent", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetInstalledSoftware");
        }
    }
}
