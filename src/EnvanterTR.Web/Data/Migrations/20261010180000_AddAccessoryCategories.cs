using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_Category_Location",
                table: "Accessories");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Accessories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessoryCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryCategories", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryCategories_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccessoryCategories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_CategoryId_Location",
                table: "Accessories",
                columns: new[] { "CompanyId", "CategoryId", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryCategories_CompanyId_Name",
                table: "AccessoryCategories",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_AccessoryCategories_CompanyId_CategoryId",
                table: "Accessories",
                columns: new[] { "CompanyId", "CategoryId" },
                principalTable: "AccessoryCategories",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO "AccessoryCategories" ("Id", "CompanyId", "Name", "IsArchived", "CreatedAt")
                SELECT gen_random_uuid(), grouped."CompanyId", grouped."Name", FALSE, now()
                FROM (
                    SELECT DISTINCT ON (a."CompanyId", lower(btrim(a."Category")))
                        a."CompanyId", btrim(a."Category") AS "Name"
                    FROM "Accessories" a
                    WHERE a."Category" IS NOT NULL AND btrim(a."Category") <> ''
                    ORDER BY a."CompanyId", lower(btrim(a."Category")), btrim(a."Category")
                ) grouped;

                UPDATE "Accessories" a
                SET "CategoryId" = c."Id"
                FROM "AccessoryCategories" c
                WHERE c."CompanyId" = a."CompanyId"
                  AND lower(c."Name") = lower(btrim(a."Category"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_AccessoryCategories_CompanyId_CategoryId",
                table: "Accessories");

            migrationBuilder.DropTable(
                name: "AccessoryCategories");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_CompanyId_CategoryId_Location",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Accessories");

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_Category_Location",
                table: "Accessories",
                columns: new[] { "CompanyId", "Category", "Location" });
        }
    }
}
