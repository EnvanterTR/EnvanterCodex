using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    MinimumQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryComponents", x => x.Id);
                    table.UniqueConstraint("AK_InventoryComponents_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_InventoryComponents_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
                    table.CheckConstraint("CK_InventoryComponents_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_InventoryComponents_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryComponents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RemovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentAllocations", x => x.Id);
                    table.UniqueConstraint("AK_ComponentAllocations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ComponentAllocations_Quantities", "\"Quantity\" > 0 AND \"RemovedQuantity\" BETWEEN 0 AND \"Quantity\"");
                    table.CheckConstraint("CK_ComponentAllocations_RemovedAt", "\"LastRemovedAt\" IS NULL OR \"LastRemovedAt\" >= \"AllocatedAt\"");
                    table.ForeignKey(
                        name: "FK_ComponentAllocations_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComponentAllocations_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentAllocations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentAllocations_InventoryComponents_CompanyId_Componen~",
                        columns: x => new { x.CompanyId, x.ComponentId },
                        principalTable: "InventoryComponents",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentMovements", x => x.Id);
                    table.CheckConstraint("CK_ComponentMovements_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_ComponentMovements_Type", "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");
                    table.ForeignKey(
                        name: "FK_ComponentMovements_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComponentMovements_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentMovements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentMovements_ComponentAllocations_CompanyId_Allocatio~",
                        columns: x => new { x.CompanyId, x.AllocationId },
                        principalTable: "ComponentAllocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentMovements_InventoryComponents_CompanyId_ComponentId",
                        columns: x => new { x.CompanyId, x.ComponentId },
                        principalTable: "InventoryComponents",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_ActorUserId",
                table: "ComponentAllocations",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_CompanyId_AssetId",
                table: "ComponentAllocations",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentAllocations_CompanyId_ComponentId_AssetId_LastRemo~",
                table: "ComponentAllocations",
                columns: new[] { "CompanyId", "ComponentId", "AssetId", "LastRemovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentMovements_ActorUserId",
                table: "ComponentMovements",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentMovements_CompanyId_AllocationId",
                table: "ComponentMovements",
                columns: new[] { "CompanyId", "AllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentMovements_CompanyId_AssetId",
                table: "ComponentMovements",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentMovements_CompanyId_ComponentId_OccurredAt",
                table: "ComponentMovements",
                columns: new[] { "CompanyId", "ComponentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryComponents_CompanyId_Name",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryComponents_CompanyId_SerialNumber",
                table: "InventoryComponents",
                columns: new[] { "CompanyId", "SerialNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentMovements");

            migrationBuilder.DropTable(
                name: "ComponentAllocations");

            migrationBuilder.DropTable(
                name: "InventoryComponents");
        }
    }
}
