using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportTicketEmailOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsSupportMailbox = table.Column<bool>(type: "boolean", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StatusSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastErrorType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketEmailOutbox", x => x.Id);
                    table.CheckConstraint("CK_SupportTicketEmailOutbox_AttemptCount", "\"AttemptCount\" >= 0");
                    table.CheckConstraint("CK_SupportTicketEmailOutbox_EventType", "\"EventType\" IN ('TicketCreated', 'CustomerReply', 'StaffReply', 'StatusChanged')");
                    table.CheckConstraint("CK_SupportTicketEmailOutbox_SentAt", "(\"Status\" = 'Sent' AND \"SentAt\" IS NOT NULL) OR (\"Status\" <> 'Sent' AND \"SentAt\" IS NULL)");
                    table.CheckConstraint("CK_SupportTicketEmailOutbox_Status", "\"Status\" IN ('Pending', 'Sending', 'Sent')");
                    table.ForeignKey(
                        name: "FK_SupportTicketEmailOutbox_SupportTickets_CompanyId_TicketId",
                        columns: x => new { x.CompanyId, x.TicketId },
                        principalTable: "SupportTickets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketEmailOutbox_CompanyId_TicketId_CreatedAt",
                table: "SupportTicketEmailOutbox",
                columns: new[] { "CompanyId", "TicketId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketEmailOutbox_Status_NextAttemptAt_CreatedAt",
                table: "SupportTicketEmailOutbox",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTicketEmailOutbox");
        }
    }
}
