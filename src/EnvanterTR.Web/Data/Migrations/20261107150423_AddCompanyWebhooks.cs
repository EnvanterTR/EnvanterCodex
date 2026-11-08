using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProtectedUrl = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    UrlHint = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProtectedSigningSecret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EventsJson = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CursorOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CursorEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastStatusCode = table.Column<int>(type: "integer", nullable: true),
                    LastError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyWebhooks", x => x.Id);
                    table.CheckConstraint("CK_CompanyWebhooks_Kind", "\"Kind\" IN ('slack', 'teams', 'generic')");
                    table.ForeignKey(
                        name: "FK_CompanyWebhooks_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_CompanyId_OccurredAt_Id",
                table: "AssetEvents",
                columns: new[] { "CompanyId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWebhooks_CompanyId_CreatedAt",
                table: "CompanyWebhooks",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWebhooks_IsActive",
                table: "CompanyWebhooks",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyWebhooks");

            migrationBuilder.DropIndex(
                name: "IX_AssetEvents_CompanyId_OccurredAt_Id",
                table: "AssetEvents");
        }
    }
}
