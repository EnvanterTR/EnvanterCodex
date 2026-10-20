using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeHandoverDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeHandoverDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecipientSignaturePngBase64 = table.Column<string>(type: "text", nullable: true),
                    RecipientSignatureSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RecipientSignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuerSignaturePngBase64 = table.Column<string>(type: "text", nullable: true),
                    IssuerSignatureSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IssuerSignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeHandoverDocuments", x => x.Id);
                    table.CheckConstraint("CK_EmployeeHandoverDocuments_SignaturePairs", "(\"RecipientSignaturePngBase64\" IS NULL) = (\"RecipientSignedAt\" IS NULL) AND (\"IssuerSignaturePngBase64\" IS NULL) = (\"IssuerSignedAt\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_EmployeeHandoverDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeHandoverDocuments_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHandoverDocuments_CompanyId_DocumentNumber",
                table: "EmployeeHandoverDocuments",
                columns: new[] { "CompanyId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHandoverDocuments_CompanyId_EmployeeId_CreatedAt",
                table: "EmployeeHandoverDocuments",
                columns: new[] { "CompanyId", "EmployeeId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeHandoverDocuments");
        }
    }
}
