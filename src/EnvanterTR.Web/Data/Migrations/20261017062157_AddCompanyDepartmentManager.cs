using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDepartmentManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerEmployeeId",
                table: "CompanyDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDepartments_CompanyId_ManagerEmployeeId",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "ManagerEmployeeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyDepartments_Employees_CompanyId_ManagerEmployeeId",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "ManagerEmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyDepartments_Employees_CompanyId_ManagerEmployeeId",
                table: "CompanyDepartments");

            migrationBuilder.DropIndex(
                name: "IX_CompanyDepartments_CompanyId_ManagerEmployeeId",
                table: "CompanyDepartments");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "CompanyDepartments");
        }
    }
}
