using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetInsuranceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "InsuranceExpiresAt",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurancePolicyNumber",
                table: "Assets",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "Assets",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_InsuranceExpiresAt",
                table: "Assets",
                columns: new[] { "CompanyId", "InsuranceExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_InsuranceExpiresAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InsuranceExpiresAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InsurancePolicyNumber",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                table: "Assets");
        }
    }
}
