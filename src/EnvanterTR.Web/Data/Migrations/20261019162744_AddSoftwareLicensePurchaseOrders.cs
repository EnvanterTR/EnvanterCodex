using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareLicensePurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoftwareLicensePurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SeatsPurchased = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicensePurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_SoftwareLicensePurchaseOrders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SoftwareLicensePurchaseOrders_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
                    table.CheckConstraint("CK_SoftwareLicensePurchaseOrders_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_SoftwareLicensePurchaseOrders_Seats", "\"SeatsPurchased\" BETWEEN 1 AND 100000");
                    table.ForeignKey(
                        name: "FK_SoftwareLicensePurchaseOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicensePurchaseOrders_SoftwareLicenses_CompanyId_Li~",
                        columns: x => new { x.CompanyId, x.LicenseId },
                        principalTable: "SoftwareLicenses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftwareLicensePurchaseOrders_SupplierCatalogEntries_Compan~",
                        columns: x => new { x.CompanyId, x.SupplierId },
                        principalTable: "SupplierCatalogEntries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicensePurchaseOrders_CompanyId_LicenseId_IsArchive~",
                table: "SoftwareLicensePurchaseOrders",
                columns: new[] { "CompanyId", "LicenseId", "IsArchived", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicensePurchaseOrders_CompanyId_SupplierId_OrderNum~",
                table: "SoftwareLicensePurchaseOrders",
                columns: new[] { "CompanyId", "SupplierId", "OrderNumber" });

            // Preserve existing order references as searchable, relational history.
            migrationBuilder.Sql("""
                INSERT INTO "SoftwareLicensePurchaseOrders"
                    ("Id", "CompanyId", "LicenseId", "SupplierId", "OrderNumber", "OrderDate", "SeatsPurchased", "UnitCost", "Currency", "Notes", "IsArchived", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "CompanyId", "Id", "SupplierId", btrim("PurchaseOrderReference"), "PurchaseDate", "Seats", "UnitCost", "Currency",
                    'Mevcut lisans satın alma sipariş referansından aktarıldı.', FALSE, now(), now()
                FROM "SoftwareLicenses"
                WHERE NULLIF(btrim("PurchaseOrderReference"), '') IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoftwareLicensePurchaseOrders");
        }
    }
}
