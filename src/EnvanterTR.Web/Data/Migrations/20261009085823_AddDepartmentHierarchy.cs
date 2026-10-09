using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationCatalogId",
                table: "CompanyDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentDepartmentId",
                table: "CompanyDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDepartments_CompanyId_LocationCatalogId",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "LocationCatalogId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDepartments_CompanyId_ParentDepartmentId",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "ParentDepartmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyDepartments_CompanyDepartments_CompanyId_ParentDepar~",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "ParentDepartmentId" },
                principalTable: "CompanyDepartments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyDepartments_CompanyLocations_CompanyId_LocationCatal~",
                table: "CompanyDepartments",
                columns: new[] { "CompanyId", "LocationCatalogId" },
                principalTable: "CompanyLocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyDepartments_CompanyDepartments_CompanyId_ParentDepar~",
                table: "CompanyDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyDepartments_CompanyLocations_CompanyId_LocationCatal~",
                table: "CompanyDepartments");

            migrationBuilder.DropIndex(
                name: "IX_CompanyDepartments_CompanyId_LocationCatalogId",
                table: "CompanyDepartments");

            migrationBuilder.DropIndex(
                name: "IX_CompanyDepartments_CompanyId_ParentDepartmentId",
                table: "CompanyDepartments");

            migrationBuilder.DropColumn(
                name: "LocationCatalogId",
                table: "CompanyDepartments");

            migrationBuilder.DropColumn(
                name: "ParentDepartmentId",
                table: "CompanyDepartments");
        }
    }
}
