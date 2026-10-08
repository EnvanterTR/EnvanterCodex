using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetSoftwareLicenseAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EmployeeId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "SoftwareLicenseEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "SoftwareLicenseAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "SoftwareLicenseAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEvents_CompanyId_AssetId",
                table: "SoftwareLicenseEvents",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_AssetId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_AssetId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "AssetId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL AND \"AssetId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EmployeeId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "EmployeeId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL AND \"EmployeeId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftwareLicenseAssignments_Target",
                table: "SoftwareLicenseAssignments",
                sql: "(\"EmployeeId\" IS NOT NULL AND \"AssetId\" IS NULL) OR (\"EmployeeId\" IS NULL AND \"AssetId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenseAssignments_Assets_CompanyId_AssetId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "AssetId" },
                principalTable: "Assets",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenseEvents_Assets_CompanyId_AssetId",
                table: "SoftwareLicenseEvents",
                columns: new[] { "CompanyId", "AssetId" },
                principalTable: "Assets",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "SoftwareLicenseAssignments" WHERE "AssetId" IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM "SoftwareLicenseEvents" WHERE "AssetId" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot roll back asset software-license assignments while asset assignment or event history exists.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenseAssignments_Assets_CompanyId_AssetId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenseEvents_Assets_CompanyId_AssetId",
                table: "SoftwareLicenseEvents");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseEvents_CompanyId_AssetId",
                table: "SoftwareLicenseEvents");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_AssetId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_AssetId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EmployeeId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftwareLicenseAssignments_Target",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "SoftwareLicenseEvents");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "SoftwareLicenseAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EmployeeId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "EmployeeId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL");
        }
    }
}
