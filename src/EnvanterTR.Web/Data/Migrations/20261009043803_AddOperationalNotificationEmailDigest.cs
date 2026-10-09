using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalNotificationEmailDigest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailEnabledKindsJson",
                table: "OperationalNotificationPreferences",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "OperationalNotificationEmailDigests",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    LastError = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalNotificationEmailDigests", x => new { x.CompanyId, x.UserId, x.DeliveryDate });
                    table.CheckConstraint("CK_OperationalNotificationEmailDigests_Status", "\"Status\" IN ('Sending', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_OperationalNotificationEmailDigests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperationalNotificationEmailDigests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalNotificationEmailDigests_UserId",
                table: "OperationalNotificationEmailDigests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalNotificationEmailDigests");

            migrationBuilder.DropColumn(
                name: "EmailEnabledKindsJson",
                table: "OperationalNotificationPreferences");
        }
    }
}
