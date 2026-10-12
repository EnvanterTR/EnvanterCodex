using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryRequestKitReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KitReturnNote",
                table: "InventoryRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "KitReturnedAt",
                table: "InventoryRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InventoryRequests_CompanyId_Id",
                table: "InventoryRequests",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateTable(
                name: "InventoryRequestKitFulfillmentLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AccessoryAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComponentAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryRequestKitFulfillmentLines", x => x.Id);
                    table.CheckConstraint("CK_InventoryRequestKitFulfillmentLines_Link", "(\"ItemType\" = 'Accessory' AND \"AccessoryAssignmentId\" IS NOT NULL AND \"ComponentAllocationId\" IS NULL AND \"AssetId\" IS NULL) OR (\"ItemType\" = 'Consumable' AND \"AccessoryAssignmentId\" IS NULL AND \"ComponentAllocationId\" IS NULL AND \"AssetId\" IS NULL) OR (\"ItemType\" = 'Asset' AND \"AccessoryAssignmentId\" IS NULL AND \"ComponentAllocationId\" IS NULL AND \"AssetId\" = \"ItemId\") OR (\"ItemType\" = 'Component' AND \"AccessoryAssignmentId\" IS NULL AND \"ComponentAllocationId\" IS NOT NULL AND \"AssetId\" IS NOT NULL)");
                    table.CheckConstraint("CK_InventoryRequestKitFulfillmentLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_InventoryRequestKitFulfillmentLines_Returnable", "\"ReturnedAt\" IS NULL OR \"ItemType\" IN ('Accessory', 'Asset', 'Component')");
                    table.CheckConstraint("CK_InventoryRequestKitFulfillmentLines_Type", "\"ItemType\" IN ('Accessory', 'Consumable', 'Asset', 'Component')");
                    table.ForeignKey("FK_InventoryRequestKitFulfillmentLines_AccessoryAssignments_CompanyId_AccessoryAssignmentId", x => new { x.CompanyId, x.AccessoryAssignmentId }, "AccessoryAssignments", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_InventoryRequestKitFulfillmentLines_Assets_CompanyId_AssetId", x => new { x.CompanyId, x.AssetId }, "Assets", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_InventoryRequestKitFulfillmentLines_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_InventoryRequestKitFulfillmentLines_ComponentAllocations_CompanyId_ComponentAllocationId", x => new { x.CompanyId, x.ComponentAllocationId }, "ComponentAllocations", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_InventoryRequestKitFulfillmentLines_InventoryRequests_CompanyId_InventoryRequestId", x => new { x.CompanyId, x.InventoryRequestId }, "InventoryRequests", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryRequests_KitReturn",
                table: "InventoryRequests",
                sql: "\"KitReturnedAt\" IS NULL OR (\"ItemType\" = 'AccessoryKit' AND \"Status\" = 'Approved')");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequestKitFulfillmentLines_CompanyId_AccessoryAssignmentId",
                table: "InventoryRequestKitFulfillmentLines",
                columns: new[] { "CompanyId", "AccessoryAssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequestKitFulfillmentLines_CompanyId_AssetId",
                table: "InventoryRequestKitFulfillmentLines",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequestKitFulfillmentLines_CompanyId_ComponentAllocationId",
                table: "InventoryRequestKitFulfillmentLines",
                columns: new[] { "CompanyId", "ComponentAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequestKitFulfillmentLines_RequestItem",
                table: "InventoryRequestKitFulfillmentLines",
                columns: new[] { "CompanyId", "InventoryRequestId", "ItemType", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InventoryRequestKitFulfillmentLines");
            migrationBuilder.DropCheckConstraint(name: "CK_InventoryRequests_KitReturn", table: "InventoryRequests");
            migrationBuilder.DropUniqueConstraint(name: "AK_InventoryRequests_CompanyId_Id", table: "InventoryRequests");
            migrationBuilder.DropColumn(name: "KitReturnNote", table: "InventoryRequests");
            migrationBuilder.DropColumn(name: "KitReturnedAt", table: "InventoryRequests");
        }
    }
}
