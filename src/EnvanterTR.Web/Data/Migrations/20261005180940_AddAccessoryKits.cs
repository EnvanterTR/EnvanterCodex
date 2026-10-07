using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryKits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessoryKits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryKits", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryKits_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccessoryKits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessoryKitLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessoryKitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryKitLines", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryKitLines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AccessoryKitLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_AccessoryKitLines_Accessories_CompanyId_AccessoryId",
                        columns: x => new { x.CompanyId, x.AccessoryId },
                        principalTable: "Accessories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessoryKitLines_AccessoryKits_CompanyId_AccessoryKitId",
                        columns: x => new { x.CompanyId, x.AccessoryKitId },
                        principalTable: "AccessoryKits",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryKitLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_AccessoryId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId", "AccessoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKits_CompanyId_IsActive_Name",
                table: "AccessoryKits",
                columns: new[] { "CompanyId", "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKits_CompanyId_Name",
                table: "AccessoryKits",
                columns: new[] { "CompanyId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessoryKitLines");

            migrationBuilder.DropTable(
                name: "AccessoryKits");
        }
    }
}
