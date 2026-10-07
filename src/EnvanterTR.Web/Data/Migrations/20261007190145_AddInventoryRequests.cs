using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RequestNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryRequests", x => x.Id);
                    table.CheckConstraint("CK_InventoryRequests_ItemType", "\"ItemType\" IN ('Accessory', 'Consumable')");
                    table.CheckConstraint("CK_InventoryRequests_Quantity", "\"Quantity\" BETWEEN 1 AND 100000");
                    table.CheckConstraint("CK_InventoryRequests_ReviewedAt", "(\"Status\" = 'Pending' AND \"ReviewedAt\" IS NULL) OR (\"Status\" <> 'Pending' AND \"ReviewedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_InventoryRequests_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_InventoryRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryRequests_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequests_CompanyId_EmployeeId_CreatedAt",
                table: "InventoryRequests",
                columns: new[] { "CompanyId", "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequests_CompanyId_Status_CreatedAt",
                table: "InventoryRequests",
                columns: new[] { "CompanyId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequests_RequestedByUserId",
                table: "InventoryRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequests_ReviewedByUserId",
                table: "InventoryRequests",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryRequests");
        }
    }
}
