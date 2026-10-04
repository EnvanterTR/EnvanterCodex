using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccessoryCheckoutTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "AccessoryAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "AccessoryAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AccessoryAssignments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetType",
                table: "AccessoryAssignments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Employee");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryAssignments_CompanyId_AssetId",
                table: "AccessoryAssignments",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryAssignments_Target",
                table: "AccessoryAssignments",
                sql: "(\"TargetType\" = 'Employee' AND \"EmployeeId\" IS NOT NULL AND \"AssetId\" IS NULL AND \"Location\" IS NULL) OR (\"TargetType\" = 'Asset' AND \"AssetId\" IS NOT NULL AND \"EmployeeId\" IS NULL AND \"Location\" IS NULL) OR (\"TargetType\" = 'Location' AND \"Location\" IS NOT NULL AND \"EmployeeId\" IS NULL AND \"AssetId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryAssignments_Assets_CompanyId_AssetId",
                table: "AccessoryAssignments",
                columns: new[] { "CompanyId", "AssetId" },
                principalTable: "Assets",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryAssignments_Assets_CompanyId_AssetId",
                table: "AccessoryAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryAssignments_CompanyId_AssetId",
                table: "AccessoryAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryAssignments_Target",
                table: "AccessoryAssignments");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "AccessoryAssignments");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AccessoryAssignments");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "AccessoryAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "AccessoryAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
