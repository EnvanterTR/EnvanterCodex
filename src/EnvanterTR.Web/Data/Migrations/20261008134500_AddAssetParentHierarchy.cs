using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetParentHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentAssetId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_ParentAssetId",
                table: "Assets",
                columns: new[] { "CompanyId", "ParentAssetId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assets_NotOwnParent",
                table: "Assets",
                sql: "\"ParentAssetId\" IS NULL OR \"ParentAssetId\" <> \"Id\"");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Assets_CompanyId_ParentAssetId",
                table: "Assets",
                columns: new[] { "CompanyId", "ParentAssetId" },
                principalTable: "Assets",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Assets" WHERE "ParentAssetId" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot remove asset hierarchy while parent-child relationships exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Assets_CompanyId_ParentAssetId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_ParentAssetId",
                table: "Assets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assets_NotOwnParent",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ParentAssetId",
                table: "Assets");
        }
    }
}
