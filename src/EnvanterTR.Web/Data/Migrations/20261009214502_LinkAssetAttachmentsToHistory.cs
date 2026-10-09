using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkAssetAttachmentsToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssetEventId",
                table: "AssetAttachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AssetEvents_CompanyId_AssetId_Id",
                table: "AssetEvents",
                columns: new[] { "CompanyId", "AssetId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAttachments_CompanyId_AssetId_AssetEventId",
                table: "AssetAttachments",
                columns: new[] { "CompanyId", "AssetId", "AssetEventId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAttachments_AssetEvents_CompanyId_AssetId_AssetEventId",
                table: "AssetAttachments",
                columns: new[] { "CompanyId", "AssetId", "AssetEventId" },
                principalTable: "AssetEvents",
                principalColumns: new[] { "CompanyId", "AssetId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetAttachments_AssetEvents_CompanyId_AssetId_AssetEventId",
                table: "AssetAttachments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AssetEvents_CompanyId_AssetId_Id",
                table: "AssetEvents");

            migrationBuilder.DropIndex(
                name: "IX_AssetAttachments_CompanyId_AssetId_AssetEventId",
                table: "AssetAttachments");

            migrationBuilder.DropColumn(
                name: "AssetEventId",
                table: "AssetAttachments");
        }
    }
}
