using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareLicenseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoftwareLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Publisher = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ProductVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LicenseType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LicenseReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenses", x => x.Id);
                    table.UniqueConstraint("AK_SoftwareLicenses_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SoftwareLicenses_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
                    table.CheckConstraint("CK_SoftwareLicenses_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_SoftwareLicenses_Seats", "\"Seats\" >= 0");
                    table.CheckConstraint("CK_SoftwareLicenses_Type", "\"LicenseType\" IN ('Subscription', 'Perpetual', 'Device', 'Site', 'Trial', 'Other')");
                    table.ForeignKey(
                        name: "FK_SoftwareLicenses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareLicenseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenseAssignments", x => x.Id);
                    table.UniqueConstraint("AK_SoftwareLicenseAssignments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SoftwareLicenseAssignments_Revocation", "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"AssignedAt\"");
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseAssignments_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseAssignments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseAssignments_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseAssignments_SoftwareLicenses_CompanyId_Licen~",
                        columns: x => new { x.CompanyId, x.LicenseId },
                        principalTable: "SoftwareLicenses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareLicenseEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEvents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEvents_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenseEvents_SoftwareLicenses_CompanyId_LicenseId",
                        columns: x => new { x.CompanyId, x.LicenseId },
                        principalTable: "SoftwareLicenses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_ActorUserId",
                table: "SoftwareLicenseAssignments",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_EmployeeId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_EmployeeId",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "EmployeeId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseAssignments_CompanyId_LicenseId_RevokedAt",
                table: "SoftwareLicenseAssignments",
                columns: new[] { "CompanyId", "LicenseId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEvents_ActorUserId",
                table: "SoftwareLicenseEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEvents_CompanyId_EmployeeId",
                table: "SoftwareLicenseEvents",
                columns: new[] { "CompanyId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenseEvents_CompanyId_LicenseId_OccurredAt",
                table: "SoftwareLicenseEvents",
                columns: new[] { "CompanyId", "LicenseId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_CompanyId_ExpirationDate",
                table: "SoftwareLicenses",
                columns: new[] { "CompanyId", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_CompanyId_Name",
                table: "SoftwareLicenses",
                columns: new[] { "CompanyId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoftwareLicenseAssignments");

            migrationBuilder.DropTable(
                name: "SoftwareLicenseEvents");

            migrationBuilder.DropTable(
                name: "SoftwareLicenses");
        }
    }
}
