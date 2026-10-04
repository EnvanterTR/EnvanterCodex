using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetWarrantyExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "WarrantyExpiresAt",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_WarrantyExpiresAt",
                table: "Assets",
                columns: new[] { "CompanyId", "WarrantyExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_WarrantyExpiresAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiresAt",
                table: "Assets");
        }
    }
}
