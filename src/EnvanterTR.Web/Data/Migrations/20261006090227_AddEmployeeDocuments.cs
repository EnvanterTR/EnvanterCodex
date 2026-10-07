using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageFileName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.UniqueConstraint("AK_EmployeeDocuments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_EmployeeDocuments_Length", "\"Length\" BETWEEN 1 AND 10485760");
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_CompanyId_EmployeeId_CreatedAt",
                table: "EmployeeDocuments",
                columns: new[] { "CompanyId", "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_UploadedByUserId",
                table: "EmployeeDocuments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDocuments");
        }
    }
}
