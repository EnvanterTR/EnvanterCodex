using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoryInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    MinimumQuantity = table.Column<int>(type: "integer", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsRequestable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.Id);
                    table.UniqueConstraint("AK_Accessories_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Accessories_Cost", "\"UnitCost\" IS NULL OR \"UnitCost\" >= 0");
                    table.CheckConstraint("CK_Accessories_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_Accessories_Quantities", "\"MinimumQuantity\" >= 0 AND \"TotalQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_Accessories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessoryAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckedOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryAssignments", x => x.Id);
                    table.UniqueConstraint("AK_AccessoryAssignments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AccessoryAssignments_Quantities", "\"Quantity\" > 0 AND \"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_AccessoryAssignments_Accessories_CompanyId_AccessoryId",
                        columns: x => new { x.CompanyId, x.AccessoryId },
                        principalTable: "Accessories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryAssignments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryAssignments_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryMovements", x => x.Id);
                    table.CheckConstraint("CK_AccessoryMovements_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_AccessoryMovements_Type", "\"MovementType\" IN ('Received', 'Adjustment', 'Checkout', 'Checkin')");
                    table.ForeignKey(
                        name: "FK_AccessoryMovements_Accessories_CompanyId_AccessoryId",
                        columns: x => new { x.CompanyId, x.AccessoryId },
                        principalTable: "Accessories",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryMovements_AccessoryAssignments_CompanyId_Assignmen~",
                        columns: x => new { x.CompanyId, x.AssignmentId },
                        principalTable: "AccessoryAssignments",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessoryMovements_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessoryMovements_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessoryMovements_Employees_CompanyId_EmployeeId",
                        columns: x => new { x.CompanyId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_Category_Location",
                table: "Accessories",
                columns: new[] { "CompanyId", "Category", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_CompanyId_Name",
                table: "Accessories",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryAssignments_CompanyId_AccessoryId_EmployeeId",
                table: "AccessoryAssignments",
                columns: new[] { "CompanyId", "AccessoryId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryAssignments_CompanyId_EmployeeId",
                table: "AccessoryAssignments",
                columns: new[] { "CompanyId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryMovements_ActorUserId",
                table: "AccessoryMovements",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryMovements_CompanyId_AccessoryId_OccurredAt",
                table: "AccessoryMovements",
                columns: new[] { "CompanyId", "AccessoryId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryMovements_CompanyId_AssignmentId",
                table: "AccessoryMovements",
                columns: new[] { "CompanyId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryMovements_CompanyId_EmployeeId",
                table: "AccessoryMovements",
                columns: new[] { "CompanyId", "EmployeeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessoryMovements");

            migrationBuilder.DropTable(
                name: "AccessoryAssignments");

            migrationBuilder.DropTable(
                name: "Accessories");
        }
    }
}
