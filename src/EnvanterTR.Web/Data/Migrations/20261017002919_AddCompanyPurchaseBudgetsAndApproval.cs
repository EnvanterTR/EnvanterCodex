using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    public partial class AddCompanyPurchaseBudgetsAndApproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ApprovalStatus", table: "AssetPurchaseOrders", type: "text", nullable: false, defaultValue: "NotRequired");
            migrationBuilder.AddColumn<string>(name: "ApprovalNote", table: "AssetPurchaseOrders", type: "character varying(2000)", maxLength: 2000, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "ReviewedByUserId", table: "AssetPurchaseOrders", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ReviewedAt", table: "AssetPurchaseOrders", type: "timestamp with time zone", nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyPurchaseBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetYear = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPurchaseBudgets", x => x.Id);
                    table.UniqueConstraint("AK_CompanyPurchaseBudgets_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_CompanyPurchaseBudgets_Year", "\"BudgetYear\" BETWEEN 2000 AND 2100");
                    table.CheckConstraint("CK_CompanyPurchaseBudgets_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_CompanyPurchaseBudgets_Amount", "\"Amount\" >= 0");
                    table.ForeignKey("FK_CompanyPurchaseBudgets_AspNetUsers_CreatedByUserId", x => x.CreatedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_CompanyPurchaseBudgets_AspNetUsers_UpdatedByUserId", x => x.UpdatedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_CompanyPurchaseBudgets_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_AssetPurchaseOrders_AspNetUsers_ReviewedByUserId",
                table: "AssetPurchaseOrders", column: "ReviewedByUserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetPurchaseOrders_ApprovalStatus", table: "AssetPurchaseOrders",
                sql: "\"ApprovalStatus\" IN ('NotRequired', 'Pending', 'Approved', 'Rejected')");
            migrationBuilder.CreateIndex(name: "IX_AssetPurchaseOrders_CompanyId_ApprovalStatus_OrderDate", table: "AssetPurchaseOrders", columns: new[] { "CompanyId", "ApprovalStatus", "OrderDate" });
            migrationBuilder.CreateIndex(name: "IX_AssetPurchaseOrders_ReviewedByUserId", table: "AssetPurchaseOrders", column: "ReviewedByUserId");
            migrationBuilder.CreateIndex(name: "IX_CompanyPurchaseBudgets_CompanyId_BudgetYear_Currency", table: "CompanyPurchaseBudgets", columns: new[] { "CompanyId", "BudgetYear", "Currency" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_CompanyPurchaseBudgets_CreatedByUserId", table: "CompanyPurchaseBudgets", column: "CreatedByUserId");
            migrationBuilder.CreateIndex(name: "IX_CompanyPurchaseBudgets_UpdatedByUserId", table: "CompanyPurchaseBudgets", column: "UpdatedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CompanyPurchaseBudgets");
            migrationBuilder.DropCheckConstraint(name: "CK_AssetPurchaseOrders_ApprovalStatus", table: "AssetPurchaseOrders");
            migrationBuilder.DropForeignKey(name: "FK_AssetPurchaseOrders_AspNetUsers_ReviewedByUserId", table: "AssetPurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_AssetPurchaseOrders_CompanyId_ApprovalStatus_OrderDate", table: "AssetPurchaseOrders");
            migrationBuilder.DropIndex(name: "IX_AssetPurchaseOrders_ReviewedByUserId", table: "AssetPurchaseOrders");
            migrationBuilder.DropColumn(name: "ApprovalStatus", table: "AssetPurchaseOrders");
            migrationBuilder.DropColumn(name: "ApprovalNote", table: "AssetPurchaseOrders");
            migrationBuilder.DropColumn(name: "ReviewedByUserId", table: "AssetPurchaseOrders");
            migrationBuilder.DropColumn(name: "ReviewedAt", table: "AssetPurchaseOrders");
        }
    }
}
