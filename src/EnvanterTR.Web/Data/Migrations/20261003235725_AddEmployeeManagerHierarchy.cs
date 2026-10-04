using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeManagerHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerEmployeeId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_ManagerEmployeeId",
                table: "Employees",
                columns: new[] { "CompanyId", "ManagerEmployeeId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_NoSelfManager",
                table: "Employees",
                sql: "\"ManagerEmployeeId\" IS NULL OR \"ManagerEmployeeId\" <> \"Id\"");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_CompanyId_ManagerEmployeeId",
                table: "Employees",
                columns: new[] { "CompanyId", "ManagerEmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_CompanyId_ManagerEmployeeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_ManagerEmployeeId",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_NoSelfManager",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "Employees");
        }
    }
}
