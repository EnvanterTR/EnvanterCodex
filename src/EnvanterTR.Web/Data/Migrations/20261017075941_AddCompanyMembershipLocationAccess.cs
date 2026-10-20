using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyMembershipLocationAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationAccessMode",
                table: "CompanyMemberships",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "all");

            migrationBuilder.CreateTable(
                name: "CompanyMembershipLocationAccesses",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMembershipLocationAccesses", x => new { x.CompanyId, x.UserId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_CompanyMembershipLocationAccesses_CompanyLocations_CompanyI~",
                        columns: x => new { x.CompanyId, x.LocationId },
                        principalTable: "CompanyLocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyMembershipLocationAccesses_CompanyMemberships_Compan~",
                        columns: x => new { x.CompanyId, x.UserId },
                        principalTable: "CompanyMemberships",
                        principalColumns: new[] { "CompanyId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMembershipLocationAccesses_CompanyId_LocationId",
                table: "CompanyMembershipLocationAccesses",
                columns: new[] { "CompanyId", "LocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyMembershipLocationAccesses");

            migrationBuilder.DropColumn(
                name: "LocationAccessMode",
                table: "CompanyMemberships");
        }
    }
}
