using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyApiTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyApiTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TokenPrefix = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopesJson = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyApiTokens", x => x.Id);
                    table.CheckConstraint("CK_CompanyApiTokens_Expiry", "\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_CompanyApiTokens_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyApiTokens_CompanyMemberships_CompanyId_UserId",
                        columns: x => new { x.CompanyId, x.UserId },
                        principalTable: "CompanyMemberships",
                        principalColumns: new[] { "CompanyId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyApiTokens_CompanyId_UserId_CreatedAt",
                table: "CompanyApiTokens",
                columns: new[] { "CompanyId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyApiTokens_TokenHash",
                table: "CompanyApiTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyApiTokens");
        }
    }
}
