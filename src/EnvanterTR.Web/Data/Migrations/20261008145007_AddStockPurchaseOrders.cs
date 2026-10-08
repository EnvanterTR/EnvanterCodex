using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_StockPurchaseOrders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_StockPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_StockPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
                    table.CheckConstraint("CK_StockPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrders_SupplierCatalogEntries_CompanyId_Suppli~",
                        columns: x => new { x.CompanyId, x.SupplierId },
                        principalTable: "SupplierCatalogEntries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockPurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumableId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPurchaseOrderLines", x => x.Id);
                    table.UniqueConstraint("AK_StockPurchaseOrderLines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_StockPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
                    table.CheckConstraint("CK_StockPurchaseOrderLines_ExactlyOneStockItem", "(\"ConsumableId\" IS NOT NULL AND \"ComponentId\" IS NULL) OR (\"ConsumableId\" IS NULL AND \"ComponentId\" IS NOT NULL)");
                    table.CheckConstraint("CK_StockPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrderLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrderLines_Consumables_CompanyId_ConsumableId",
                        columns: x => new { x.CompanyId, x.ConsumableId },
                        principalTable: "Consumables",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrderLines_InventoryComponents_CompanyId_Compo~",
                        columns: x => new { x.CompanyId, x.ComponentId },
                        principalTable: "InventoryComponents",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPurchaseOrderLines_StockPurchaseOrders_CompanyId_Purch~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderId },
                        principalTable: "StockPurchaseOrders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrderLines_CompanyId_ComponentId",
                table: "StockPurchaseOrderLines",
                columns: new[] { "CompanyId", "ComponentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrderLines_CompanyId_ConsumableId",
                table: "StockPurchaseOrderLines",
                columns: new[] { "CompanyId", "ConsumableId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrderLines_CompanyId_PurchaseOrderId",
                table: "StockPurchaseOrderLines",
                columns: new[] { "CompanyId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_CompanyId_OrderNumber",
                table: "StockPurchaseOrders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_CompanyId_Status_OrderDate",
                table: "StockPurchaseOrders",
                columns: new[] { "CompanyId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_CompanyId_SupplierId",
                table: "StockPurchaseOrders",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPurchaseOrders_CreatedByUserId",
                table: "StockPurchaseOrders",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockPurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "StockPurchaseOrders");
        }
    }
}
