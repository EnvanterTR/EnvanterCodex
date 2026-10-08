using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedSoftwareLicenseEula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EulaVersion",
                table: "SoftwareLicenseAttachments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentEula",
                table: "SoftwareLicenseAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEulaDocument",
                table: "SoftwareLicenseAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "SoftwareLicenseAttachments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EulaAttachmentId",
                table: "SoftwareLicenseAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EulaVersion",
                table: "SoftwareLicenseAssignments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SoftwareLicenseAttachments_CompanyId_LicenseId_Id",
                table: "SoftwareLicenseAttachments",
                columns: new[] { "CompanyId", "LicenseId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAttachments_CompanyId_LicenseId",
                table: "SoftwareLicenseAttachments",
                columns: new[] { "CompanyId", "LicenseId" },
                unique: true,
                filter: "\"IsCurrentEula\" = TRUE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftwareLicenseAttachments_CurrentEula",
                table: "SoftwareLicenseAttachments",
                sql: "\"IsCurrentEula\" = FALSE OR \"IsEulaDocument\" = TRUE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftwareLicenseAttachments_Eula",
                table: "SoftwareLicenseAttachments",
                sql: "(\"IsEulaDocument\" = TRUE AND \"EulaVersion\" IS NOT NULL AND char_length(\"EulaVersion\") BETWEEN 1 AND 80 AND \"Sha256\" IS NOT NULL) OR (\"IsEulaDocument\" = FALSE AND \"IsCurrentEula\" = FALSE AND \"EulaVersion\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EulaAttachme~",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "EulaAttachmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenseAssignments_SoftwareLicenseAttachments_Compa~",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "EulaAttachmentId" },
                principalTable: "SoftwareLicenseAttachments",
                principalColumns: new[] { "CompanyId", "LicenseId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenseAssignments_SoftwareLicenseAttachments_Compa~",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SoftwareLicenseAttachments_CompanyId_LicenseId_Id",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAttachments_CompanyId_LicenseId",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftwareLicenseAttachments_CurrentEula",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftwareLicenseAttachments_Eula",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EulaAttachme~",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "EulaVersion",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropColumn(
                name: "IsCurrentEula",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropColumn(
                name: "IsEulaDocument",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropColumn(
                name: "EulaAttachmentId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "EulaVersion",
                table: "SoftwareLicenseAssignments");
        }
    }
}
