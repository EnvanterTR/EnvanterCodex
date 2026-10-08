using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareLicenseEulaAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresEulaAcceptance",
                table: "SoftwareLicenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EulaAcceptedAt",
                table: "SoftwareLicenseAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments",
                column: "EulaAcceptedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenseAssignments_AspNetUsers_EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments",
                column: "EulaAcceptedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenseAssignments_AspNetUsers_EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenseAssignments_EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "RequiresEulaAcceptance",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "EulaAcceptedAt",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "EulaAcceptedByUserId",
                table: "SoftwareLicenseAssignments");
        }
    }
}
