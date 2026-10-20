using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareLicenseEmployeeEulaAcceptances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_SoftwareLicenseAssignments_CompanyId_LicenseId_Id",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "Id" });

            migrationBuilder.CreateTable(
                name: "SoftwareLicenseEmployeeEulaAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedByEmailSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EulaAttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EulaVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EulaSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenseEmployeeEulaAcceptances", x => x.Id);
                    table.CheckConstraint("CK_SoftwareLicenseEmployeeEulaAcceptances_Email", "char_length(\"AcceptedByEmailSnapshot\") BETWEEN 3 AND 256");
                    table.CheckConstraint("CK_SoftwareLicenseEmployeeEulaAcceptances_Sha256", "\"EulaSha256\" ~ '^[a-fA-F0-9]{64}$'");
                    table.CheckConstraint("CK_SoftwareLicenseEmployeeEulaAcceptances_Version", "char_length(\"EulaVersion\") BETWEEN 1 AND 80");
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEmployeeEulaAcceptances_AspNetUsers_Accepted~",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEmployeeEulaAcceptances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEmployeeEulaAcceptances_Employees_CompanyId_~",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEmployeeEulaAcceptances_SoftwareLicenseAssig~",
                        columns: x => new { x.CompanyId, x.LicenseId, x.AssignmentId },
                        principalTable: "SoftwareLicenseAssignments",
                        principalColumns: new[] { "CompanyId", "LicenseId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEmployeeEulaAcceptances_SoftwareLicenseAttac~",
                        columns: x => new { x.CompanyId, x.LicenseId, x.EulaAttachmentId },
                        principalTable: "SoftwareLicenseAttachments",
                        principalColumns: new[] { "CompanyId", "LicenseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEmployeeEulaAcceptances_AcceptedByUserId",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEmployeeEulaAcceptances_CompanyId_Assignment~",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                columns: new[] { "CompanyId", "AssignmentId", "EulaAttachmentId", "AcceptedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEmployeeEulaAcceptances_CompanyId_EmployeeId~",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                columns: new[] { "CompanyId", "EmployeeId", "AcceptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEmployeeEulaAcceptances_CompanyId_LicenseId_~",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                columns: new[] { "CompanyId", "LicenseId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEmployeeEulaAcceptances_CompanyId_LicenseId~1",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                columns: new[] { "CompanyId", "LicenseId", "EulaAttachmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoftwareLicenseEmployeeEulaAcceptances");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SoftwareLicenseAssignments_CompanyId_LicenseId_Id",
                table: "SoftwareLicenseAssignments");
        }
    }
}
