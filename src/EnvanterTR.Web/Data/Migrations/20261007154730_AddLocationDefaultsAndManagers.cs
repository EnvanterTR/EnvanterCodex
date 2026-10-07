using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationDefaultsAndManagers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "CompanyLocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerEmployeeId",
                table: "CompanyLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLocations_CompanyId",
                table: "CompanyLocations",
                column: "CompanyId",
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLocations_CompanyId_ManagerEmployeeId",
                table: "CompanyLocations",
                columns: new[] { "CompanyId", "ManagerEmployeeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyLocations_Employees_CompanyId_ManagerEmployeeId",
                table: "CompanyLocations",
                columns: new[] { "CompanyId", "ManagerEmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyLocations_Employees_CompanyId_ManagerEmployeeId",
                table: "CompanyLocations");

            migrationBuilder.DropIndex(
                name: "IX_CompanyLocations_CompanyId",
                table: "CompanyLocations");

            migrationBuilder.DropIndex(
                name: "IX_CompanyLocations_CompanyId_ManagerEmployeeId",
                table: "CompanyLocations");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "CompanyLocations");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "CompanyLocations");
        }
    }
}
