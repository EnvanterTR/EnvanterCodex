using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetModelAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetModelAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageFileName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetModelAttachments", x => x.Id);
                    table.UniqueConstraint("AK_AssetModelAttachments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AssetModelAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760");
                    table.ForeignKey(
                        name: "FK_AssetModelAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetModelAttachments_AssetModels_CompanyId_ModelId",
                        columns: x => new { x.CompanyId, x.ModelId },
                        principalTable: "AssetModels",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetModelAttachments_CompanyId_ModelId_CreatedAt",
                table: "AssetModelAttachments",
                columns: new[] { "CompanyId", "ModelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetModelAttachments_UploadedByUserId",
                table: "AssetModelAttachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetModelAttachments");
        }
    }
}
