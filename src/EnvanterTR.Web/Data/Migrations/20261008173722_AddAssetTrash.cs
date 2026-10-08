using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetTrash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_AssetTag",
                table: "Assets");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_AssetTag",
                table: "Assets",
                columns: new[] { "CompanyId", "AssetTag" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_DeletedAt_UpdatedAt",
                table: "Assets",
                columns: new[] { "CompanyId", "DeletedAt", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_AssetTag",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_DeletedAt_UpdatedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Assets");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_AssetTag",
                table: "Assets",
                columns: new[] { "CompanyId", "AssetTag" },
                unique: true);
        }
    }
}
