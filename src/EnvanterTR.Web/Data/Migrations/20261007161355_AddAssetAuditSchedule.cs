using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetAuditSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "NextAuditOn",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_NextAuditOn",
                table: "Assets",
                columns: new[] { "CompanyId", "NextAuditOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_NextAuditOn",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "NextAuditOn",
                table: "Assets");
        }
    }
}
