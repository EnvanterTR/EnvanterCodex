using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutConsentAndInitializationLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataProcessingAcknowledgedAt",
                table: "CompanyCheckoutOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataProcessingNoticeVersion",
                table: "CompanyCheckoutOrders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InitializationLeaseUntil",
                table: "CompanyCheckoutOrders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataProcessingAcknowledgedAt",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "DataProcessingNoticeVersion",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "InitializationLeaseUntil",
                table: "CompanyCheckoutOrders");
        }
    }
}
