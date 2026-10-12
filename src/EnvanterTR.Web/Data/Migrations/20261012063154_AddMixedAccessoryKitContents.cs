using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMixedAccessoryKitContents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AccessoryId",
                table: "AccessoryKitLines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "AccessoryKitLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComponentId",
                table: "AccessoryKitLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConsumableId",
                table: "AccessoryKitLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "AccessoryKitLines",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Accessory");

            migrationBuilder.AlterColumn<string>(
                name: "ItemType",
                table: "AccessoryKitLines",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24,
                oldDefaultValue: "Accessory");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_AssetId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId", "AssetId" },
                unique: true,
                filter: "\"AssetId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ComponentId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId", "ComponentId" },
                unique: true,
                filter: "\"ComponentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ConsumableId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId", "ConsumableId" },
                unique: true,
                filter: "\"ConsumableId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ItemType",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AccessoryKitId", "ItemType" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_AssetId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_ComponentId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "ComponentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryKitLines_CompanyId_ConsumableId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "ConsumableId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryKitLines_ItemType",
                table: "AccessoryKitLines",
                sql: "(\"ItemType\" = 'Accessory' AND \"AccessoryId\" IS NOT NULL AND \"ConsumableId\" IS NULL AND \"AssetId\" IS NULL AND \"ComponentId\" IS NULL) OR (\"ItemType\" = 'Consumable' AND \"AccessoryId\" IS NULL AND \"ConsumableId\" IS NOT NULL AND \"AssetId\" IS NULL AND \"ComponentId\" IS NULL) OR (\"ItemType\" = 'Asset' AND \"AccessoryId\" IS NULL AND \"ConsumableId\" IS NULL AND \"AssetId\" IS NOT NULL AND \"ComponentId\" IS NULL) OR (\"ItemType\" = 'Component' AND \"AccessoryId\" IS NULL AND \"ConsumableId\" IS NULL AND \"AssetId\" IS NULL AND \"ComponentId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryKitLines_AssetQuantity",
                table: "AccessoryKitLines",
                sql: "\"ItemType\" <> 'Asset' OR \"Quantity\" = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryKitLines_Assets_CompanyId_AssetId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "AssetId" },
                principalTable: "Assets",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryKitLines_Consumables_CompanyId_ConsumableId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "ConsumableId" },
                principalTable: "Consumables",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessoryKitLines_InventoryComponents_CompanyId_ComponentId",
                table: "AccessoryKitLines",
                columns: new[] { "CompanyId", "ComponentId" },
                principalTable: "InventoryComponents",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"AccessoryKitLines\" WHERE \"ItemType\" <> 'Accessory' OR \"AccessoryId\" IS NULL) THEN RAISE EXCEPTION 'Cannot remove mixed kit support while non-accessory kit lines exist.'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryKitLines_Assets_CompanyId_AssetId",
                table: "AccessoryKitLines");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryKitLines_Consumables_CompanyId_ConsumableId",
                table: "AccessoryKitLines");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessoryKitLines_InventoryComponents_CompanyId_ComponentId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_AssetId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ComponentId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ConsumableId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_AccessoryKitId_ItemType",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_AssetId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_ComponentId",
                table: "AccessoryKitLines");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryKitLines_CompanyId_ConsumableId",
                table: "AccessoryKitLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryKitLines_ItemType",
                table: "AccessoryKitLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryKitLines_AssetQuantity",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "ComponentId",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "ConsumableId",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "AccessoryKitLines");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccessoryId",
                table: "AccessoryKitLines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
