using EnvanterTR.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261016205446_AddAssetCustomFieldScopes")]
public partial class AddAssetCustomFieldScopes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GroupName",
            table: "AssetCustomFieldDefinitions",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "Genel");

        migrationBuilder.AddColumn<Guid>(
            name: "CategoryId",
            table: "AssetCustomFieldDefinitions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ModelId",
            table: "AssetCustomFieldDefinitions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AssetCustomFieldDefinitions_CompanyId_CategoryId",
            table: "AssetCustomFieldDefinitions",
            columns: new[] { "CompanyId", "CategoryId" });

        migrationBuilder.CreateIndex(
            name: "IX_AssetCustomFieldDefinitions_CompanyId_ModelId",
            table: "AssetCustomFieldDefinitions",
            columns: new[] { "CompanyId", "ModelId" });

        migrationBuilder.AddForeignKey(
            name: "FK_AssetCustomFieldDefinitions_AssetCategories_CompanyId_CategoryId",
            table: "AssetCustomFieldDefinitions",
            columns: new[] { "CompanyId", "CategoryId" },
            principalTable: "AssetCategories",
            principalColumns: new[] { "CompanyId", "Id" },
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_AssetCustomFieldDefinitions_AssetModels_CompanyId_ModelId",
            table: "AssetCustomFieldDefinitions",
            columns: new[] { "CompanyId", "ModelId" },
            principalTable: "AssetModels",
            principalColumns: new[] { "CompanyId", "Id" },
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_AssetCustomFieldDefinitions_AssetCategories_CompanyId_CategoryId", "AssetCustomFieldDefinitions");
        migrationBuilder.DropForeignKey("FK_AssetCustomFieldDefinitions_AssetModels_CompanyId_ModelId", "AssetCustomFieldDefinitions");
        migrationBuilder.DropIndex("IX_AssetCustomFieldDefinitions_CompanyId_CategoryId", "AssetCustomFieldDefinitions");
        migrationBuilder.DropIndex("IX_AssetCustomFieldDefinitions_CompanyId_ModelId", "AssetCustomFieldDefinitions");
        migrationBuilder.DropColumn("GroupName", "AssetCustomFieldDefinitions");
        migrationBuilder.DropColumn("CategoryId", "AssetCustomFieldDefinitions");
        migrationBuilder.DropColumn("ModelId", "AssetCustomFieldDefinitions");
    }
}
