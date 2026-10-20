using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReportKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LocalHour = table.Column<int>(type: "integer", nullable: false),
                    LocalMinute = table.Column<int>(type: "integer", nullable: false),
                    WeekDay = table.Column<int>(type: "integer", nullable: false),
                    MonthDay = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NextDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimToken = table.Column<Guid>(type: "uuid", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                    table.CheckConstraint("CK_ScheduledReports_Frequency", "\"Frequency\" IN ('daily', 'weekly', 'monthly')");
                    table.CheckConstraint("CK_ScheduledReports_Kind", "\"ReportKind\" IN ('inventory', 'maintenance', 'licenses', 'stock')");
                    table.CheckConstraint("CK_ScheduledReports_Language", "\"Language\" IN ('tr', 'en', 'ru', 'uk', 'mk')");
                    table.CheckConstraint("CK_ScheduledReports_Status", "\"LastStatus\" IN ('Idle', 'Sending', 'Sent', 'Failed', 'Skipped') AND \"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_ScheduledReports_Time", "\"LocalHour\" BETWEEN 0 AND 23 AND \"LocalMinute\" BETWEEN 0 AND 59 AND \"WeekDay\" BETWEEN 0 AND 6 AND \"MonthDay\" BETWEEN 1 AND 28");
                    table.ForeignKey(
                        name: "FK_ScheduledReports_CompanyMemberships_CompanyId_UserId",
                        columns: x => new { x.CompanyId, x.UserId },
                        principalTable: "CompanyMemberships",
                        principalColumns: new[] { "CompanyId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_CompanyId_UserId",
                table: "ScheduledReports",
                columns: new[] { "CompanyId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_IsEnabled_NextDueAt",
                table: "ScheduledReports",
                columns: new[] { "IsEnabled", "NextDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledReports");
        }
    }
}
