using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkCompanyMembershipToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "CompanyMemberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMemberships_CompanyId_EmployeeId",
                table: "CompanyMemberships",
                columns: new[] { "CompanyId", "EmployeeId" },
                unique: true,
                filter: "\"EmployeeId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyMemberships_Employees_CompanyId_EmployeeId",
                table: "CompanyMemberships",
                columns: new[] { "CompanyId", "EmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyMemberships_Employees_CompanyId_EmployeeId",
                table: "CompanyMemberships");

            migrationBuilder.DropIndex(
                name: "IX_CompanyMemberships_CompanyId_EmployeeId",
                table: "CompanyMemberships");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "CompanyMemberships");
        }
    }
}
