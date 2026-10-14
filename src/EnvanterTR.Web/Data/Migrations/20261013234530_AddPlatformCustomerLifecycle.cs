using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformCustomerLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerLifecycleStage",
                table: "Companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unclassified");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlatformNextFollowUpAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformOwnerUserId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CustomerLifecycleStage_PlatformNextFollowUpAt",
                table: "Companies",
                columns: new[] { "CustomerLifecycleStage", "PlatformNextFollowUpAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PlatformOwnerUserId",
                table: "Companies",
                column: "PlatformOwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_CustomerLifecycleStage_PlatformNextFollowUpAt",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_PlatformOwnerUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CustomerLifecycleStage",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformNextFollowUpAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformOwnerUserId",
                table: "Companies");
        }
    }
}
