using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeCompanyUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyUnitId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_CompanyUnitId",
                table: "Employees",
                columns: new[] { "CompanyId", "CompanyUnitId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_CompanyUnits_CompanyId_CompanyUnitId",
                table: "Employees",
                columns: new[] { "CompanyId", "CompanyUnitId" },
                principalTable: "CompanyUnits",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_CompanyUnits_CompanyId_CompanyUnitId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_CompanyUnitId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyUnitId",
                table: "Employees");
        }
    }
}
