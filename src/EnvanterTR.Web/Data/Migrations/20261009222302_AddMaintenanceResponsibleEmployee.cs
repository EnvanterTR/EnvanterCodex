using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceResponsibleEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleEmployeeId",
                table: "AssetMaintenances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_ResponsibleEmployeeId_CreatedAt",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "ResponsibleEmployeeId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetMaintenances_Employees_CompanyId_ResponsibleEmployeeId",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "ResponsibleEmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetMaintenances_Employees_CompanyId_ResponsibleEmployeeId",
                table: "AssetMaintenances");

            migrationBuilder.DropIndex(
                name: "IX_AssetMaintenances_CompanyId_ResponsibleEmployeeId_CreatedAt",
                table: "AssetMaintenances");

            migrationBuilder.DropColumn(
                name: "ResponsibleEmployeeId",
                table: "AssetMaintenances");
        }
    }
}
