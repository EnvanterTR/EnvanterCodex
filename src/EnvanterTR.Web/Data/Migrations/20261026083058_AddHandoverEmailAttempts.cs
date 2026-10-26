using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverEmailAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_EmployeeHandoverDocuments_CompanyId_EmployeeId_Id",
                table: "EmployeeHandoverDocuments",
                columns: new[] { "CompanyId", "EmployeeId", "Id" });

            migrationBuilder.CreateTable(
                name: "EmployeeHandoverEmailAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SnapshotSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PdfSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ErrorType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeHandoverEmailAttempts", x => x.Id);
                    table.CheckConstraint("CK_HandoverEmail_AcceptedAt", "(\"Status\" = 'Accepted') = (\"AcceptedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_HandoverEmail_RequestKey", "\"IdempotencyKey\" <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("CK_HandoverEmail_Status", "\"Status\" IN ('Sending', 'Accepted', 'Uncertain')");
                    table.ForeignKey(
                        name: "FK_EmployeeHandoverEmailAttempts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeHandoverEmailAttempts_EmployeeHandoverDocuments_Com~",
                        columns: x => new { x.CompanyId, x.EmployeeId, x.DocumentId },
                        principalTable: "EmployeeHandoverDocuments",
                        principalColumns: new[] { "CompanyId", "EmployeeId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeHandoverEmailAttempts_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHandoverEmailAttempts_CompanyId_DocumentId_CreatedAt",
                table: "EmployeeHandoverEmailAttempts",
                columns: new[] { "CompanyId", "DocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHandoverEmailAttempts_CompanyId_DocumentId_Idempote~",
                table: "EmployeeHandoverEmailAttempts",
                columns: new[] { "CompanyId", "DocumentId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHandoverEmailAttempts_CompanyId_EmployeeId_Document~",
                table: "EmployeeHandoverEmailAttempts",
                columns: new[] { "CompanyId", "EmployeeId", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // V1 preserves delivery/idempotency evidence. Use the verified recovery procedure.
            throw new NotSupportedException("Handover email delivery records must not be removed by a destructive downgrade.");
        }
    }
}
