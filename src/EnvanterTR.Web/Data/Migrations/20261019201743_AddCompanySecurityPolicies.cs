using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySecurityPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecurityPasswordMinimumLength",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<bool>(
                name: "SecurityRequireMfa",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SecuritySessionDurationHours",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 720);

            migrationBuilder.CreateTable(
                name: "CompanyMfaEnrollmentChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RememberMe = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SetupStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMfaEnrollmentChallenges", x => x.Id);
                    table.CheckConstraint("CK_CompanyMfaEnrollmentChallenges_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_CompanyMfaEnrollmentChallenges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyMfaEnrollmentChallenges_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Companies_SecurityPasswordMinimumLength",
                table: "Companies",
                sql: "\"SecurityPasswordMinimumLength\" BETWEEN 12 AND 64");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Companies_SecuritySessionDurationHours",
                table: "Companies",
                sql: "\"SecuritySessionDurationHours\" BETWEEN 1 AND 720");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMfaEnrollmentChallenges_CompanyId",
                table: "CompanyMfaEnrollmentChallenges",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMfaEnrollmentChallenges_TicketHash",
                table: "CompanyMfaEnrollmentChallenges",
                column: "TicketHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMfaEnrollmentChallenges_UserId_CompletedAt_ExpiresAt",
                table: "CompanyMfaEnrollmentChallenges",
                columns: new[] { "UserId", "CompletedAt", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyMfaEnrollmentChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Companies_SecurityPasswordMinimumLength",
                table: "Companies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Companies_SecuritySessionDurationHours",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SecurityPasswordMinimumLength",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SecurityRequireMfa",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SecuritySessionDurationHours",
                table: "Companies");
        }
    }
}
