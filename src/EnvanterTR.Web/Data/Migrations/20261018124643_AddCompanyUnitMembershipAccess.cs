using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyUnitMembershipAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyUnitAccessMode",
                table: "CompanyMemberships",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "all");

            migrationBuilder.CreateTable(
                name: "CompanyMembershipCompanyUnitAccesses",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyUnitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMembershipCompanyUnitAccesses", x => new { x.CompanyId, x.UserId, x.CompanyUnitId });
                    table.ForeignKey(
                        name: "FK_CompanyMembershipCompanyUnitAccesses_CompanyMemberships_Com~",
                        columns: x => new { x.CompanyId, x.UserId },
                        principalTable: "CompanyMemberships",
                        principalColumns: new[] { "CompanyId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyMembershipCompanyUnitAccesses_CompanyUnits_CompanyId~",
                        columns: x => new { x.CompanyId, x.CompanyUnitId },
                        principalTable: "CompanyUnits",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMembershipCompanyUnitAccesses_CompanyId_CompanyUnitId",
                table: "CompanyMembershipCompanyUnitAccesses",
                columns: new[] { "CompanyId", "CompanyUnitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyMembershipCompanyUnitAccesses");

            migrationBuilder.DropColumn(
                name: "CompanyUnitAccessMode",
                table: "CompanyMemberships");
        }
    }
}
