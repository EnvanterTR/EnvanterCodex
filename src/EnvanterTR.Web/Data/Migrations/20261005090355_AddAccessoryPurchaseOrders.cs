using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessoryPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AccessoryPurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryPurchaseOrders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AccessoryPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_AccessoryPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
                    table.CheckConstraint("CK_AccessoryPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_AccessoryPurchaseOrders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessoryPurchaseOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessoryPurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryPurchaseOrderLines", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryPurchaseOrderLines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AccessoryPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
                    table.CheckConstraint("CK_AccessoryPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_AccessoryPurchaseOrderLines_Accessories_CompanyId_Accessory~",
                        columns: x => new { x.CompanyId, x.AccessoryId },
                        principalTable: "Accessories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessoryPurchaseOrderLines_AccessoryPurchaseOrders_Company~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderId },
                        principalTable: "AccessoryPurchaseOrders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryPurchaseOrderLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrderLines_CompanyId_AccessoryId",
                table: "AccessoryPurchaseOrderLines",
                columns: new[] { "CompanyId", "AccessoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrderLines_CompanyId_PurchaseOrderId",
                table: "AccessoryPurchaseOrderLines",
                columns: new[] { "CompanyId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_OrderNumber",
                table: "AccessoryPurchaseOrders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_CompanyId_Status_OrderDate",
                table: "AccessoryPurchaseOrders",
                columns: new[] { "CompanyId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryPurchaseOrders_CreatedByUserId",
                table: "AccessoryPurchaseOrders",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessoryPurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "AccessoryPurchaseOrders");
        }
    }
}
