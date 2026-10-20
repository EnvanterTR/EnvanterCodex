using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockPurchaseOrderLines_Quantity",
                table: "StockPurchaseOrderLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetPurchaseOrderLines_Quantity",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryPurchaseOrderLines_Quantity",
                table: "AccessoryPurchaseOrderLines");

            migrationBuilder.AddColumn<int>(
                name: "QuantityReturned",
                table: "StockPurchaseOrderLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderLineId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityReturned",
                table: "AssetPurchaseOrderLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantityReturned",
                table: "AccessoryPurchaseOrderLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PurchaseOrderReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReturnedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderReturns", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrderReturns_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrderReturns_OrderType", "\"OrderType\" IN ('Asset', 'Accessory', 'Stock')");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReturns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReturns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderReturnLines", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrderReturnLines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrderReturnLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_PurchaseOrderReturnLines_Target", "(\"PurchaseOrderLineId\" IS NOT NULL AND \"AssetId\" IS NULL) OR (\"PurchaseOrderLineId\" IS NULL AND \"AssetId\" IS NOT NULL AND \"Quantity\" = 1)");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReturnLines_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReturnLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderReturnLines_PurchaseOrderReturns_CompanyId_Pur~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderReturnId },
                        principalTable: "PurchaseOrderReturns",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockPurchaseOrderLines_Quantity",
                table: "StockPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\" AND \"QuantityReturned\" BETWEEN 0 AND \"QuantityReceived\"");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CompanyId_PurchaseOrderLineId",
                table: "Assets",
                columns: new[] { "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetPurchaseOrderLines_Quantity",
                table: "AssetPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\" AND \"QuantityReturned\" BETWEEN 0 AND \"QuantityReceived\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryPurchaseOrderLines_Quantity",
                table: "AccessoryPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\" AND \"QuantityReturned\" BETWEEN 0 AND \"QuantityReceived\"");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturnLines_CompanyId_AssetId",
                table: "PurchaseOrderReturnLines",
                columns: new[] { "CompanyId", "AssetId" },
                unique: true,
                filter: "\"AssetId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturnLines_CompanyId_PurchaseOrderLineId",
                table: "PurchaseOrderReturnLines",
                columns: new[] { "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturnLines_CompanyId_PurchaseOrderReturnId",
                table: "PurchaseOrderReturnLines",
                columns: new[] { "CompanyId", "PurchaseOrderReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturns_CompanyId_OrderType_PurchaseOrderId_Cr~",
                table: "PurchaseOrderReturns",
                columns: new[] { "CompanyId", "OrderType", "PurchaseOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturns_CompanyId_OrderType_ReturnNumber",
                table: "PurchaseOrderReturns",
                columns: new[] { "CompanyId", "OrderType", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReturns_CreatedByUserId",
                table: "PurchaseOrderReturns",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetPurchaseOrderLines_CompanyId_PurchaseOrderLineId",
                table: "Assets",
                columns: new[] { "CompanyId", "PurchaseOrderLineId" },
                principalTable: "AssetPurchaseOrderLines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetPurchaseOrderLines_CompanyId_PurchaseOrderLineId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "PurchaseOrderReturnLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrderReturns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockPurchaseOrderLines_Quantity",
                table: "StockPurchaseOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CompanyId_PurchaseOrderLineId",
                table: "Assets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetPurchaseOrderLines_Quantity",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessoryPurchaseOrderLines_Quantity",
                table: "AccessoryPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "QuantityReturned",
                table: "StockPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderLineId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "QuantityReturned",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "QuantityReturned",
                table: "AccessoryPurchaseOrderLines");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockPurchaseOrderLines_Quantity",
                table: "StockPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetPurchaseOrderLines_Quantity",
                table: "AssetPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessoryPurchaseOrderLines_Quantity",
                table: "AccessoryPurchaseOrderLines",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
        }
    }
}
