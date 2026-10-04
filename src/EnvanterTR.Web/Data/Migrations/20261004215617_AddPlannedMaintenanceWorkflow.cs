using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedMaintenanceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId",
                table: "AssetMaintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMaintenances_Completion",
                table: "AssetMaintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMaintenances_Status",
                table: "AssetMaintenances");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedOn",
                table: "AssetMaintenances",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "AssetId" },
                unique: true,
                filter: "\"Status\" IN ('InProgress', 'Scheduled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMaintenances_Completion",
                table: "AssetMaintenances",
                sql: "(\"Status\" IN ('Scheduled', 'Cancelled', 'InProgress') AND \"CompletedOn\" IS NULL) OR (\"Status\" = 'Completed' AND \"CompletedOn\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMaintenances_Planned",
                table: "AssetMaintenances",
                sql: "\"Status\" NOT IN ('Scheduled', 'Cancelled') OR \"PlannedOn\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMaintenances_Status",
                table: "AssetMaintenances",
                sql: "\"Status\" IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId",
                table: "AssetMaintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMaintenances_Completion",
                table: "AssetMaintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMaintenances_Planned",
                table: "AssetMaintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMaintenances_Status",
                table: "AssetMaintenances");

            migrationBuilder.DropColumn(
                name: "PlannedOn",
                table: "AssetMaintenances");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "AssetId" },
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMaintenances_Completion",
                table: "AssetMaintenances",
                sql: "(\"Status\" = 'InProgress' AND \"CompletedOn\" IS NULL) OR (\"Status\" = 'Completed' AND \"CompletedOn\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMaintenances_Status",
                table: "AssetMaintenances",
                sql: "\"Status\" IN ('InProgress', 'Completed')");
        }
    }
}
