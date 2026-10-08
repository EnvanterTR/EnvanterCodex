using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetPurchaseOrders",
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
                    table.PrimaryKey("PK_AssetPurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_AssetPurchaseOrders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AssetPurchaseOrders_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_AssetPurchaseOrders_Dates", "\"ExpectedOn\" IS NULL OR \"ExpectedOn\" >= \"OrderDate\"");
                    table.CheckConstraint("CK_AssetPurchaseOrders_Status", "\"Status\" IN ('Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrders_SupplierCatalogEntries_CompanyId_Suppli~",
                        columns: x => new { x.CompanyId, x.SupplierId },
                        principalTable: "SupplierCatalogEntries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetPurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WarrantyExpiresAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPurchaseOrderLines", x => x.Id);
                    table.UniqueConstraint("AK_AssetPurchaseOrderLines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AssetPurchaseOrderLines_Cost", "\"UnitCost\" >= 0");
                    table.CheckConstraint("CK_AssetPurchaseOrderLines_Quantity", "\"Quantity\" > 0 AND \"QuantityReceived\" BETWEEN 0 AND \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrderLines_AssetBrands_CompanyId_BrandId",
                        columns: x => new { x.CompanyId, x.BrandId },
                        principalTable: "AssetBrands",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrderLines_AssetCategories_CompanyId_CategoryId",
                        columns: x => new { x.CompanyId, x.CategoryId },
                        principalTable: "AssetCategories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrderLines_AssetModels_CompanyId_ModelId",
                        columns: x => new { x.CompanyId, x.ModelId },
                        principalTable: "AssetModels",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrderLines_AssetPurchaseOrders_CompanyId_Purch~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderId },
                        principalTable: "AssetPurchaseOrders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetPurchaseOrderLines_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_BrandId",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_CategoryId",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_ModelId",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrderLines_CompanyId_PurchaseOrderId",
                table: "AssetPurchaseOrderLines",
                columns: new[] { "CompanyId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_CompanyId_OrderNumber",
                table: "AssetPurchaseOrders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_CompanyId_Status_OrderDate",
                table: "AssetPurchaseOrders",
                columns: new[] { "CompanyId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_CompanyId_SupplierId",
                table: "AssetPurchaseOrders",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPurchaseOrders_CreatedByUserId",
                table: "AssetPurchaseOrders",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetPurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "AssetPurchaseOrders");
        }
    }
}
