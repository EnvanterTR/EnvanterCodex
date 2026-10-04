using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetMaintenances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMaintenances", x => x.Id);
                    table.CheckConstraint("CK_AssetMaintenances_Completion", "(\"Status\" = 'InProgress' AND \"CompletedOn\" IS NULL) OR (\"Status\" = 'Completed' AND \"CompletedOn\" IS NOT NULL)");
                    table.CheckConstraint("CK_AssetMaintenances_Cost", "\"Cost\" IS NULL OR \"Cost\" >= 0");
                    table.CheckConstraint("CK_AssetMaintenances_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_AssetMaintenances_Period", "\"CompletedOn\" IS NULL OR \"CompletedOn\" >= \"StartedOn\"");
                    table.CheckConstraint("CK_AssetMaintenances_Status", "\"Status\" IN ('InProgress', 'Completed')");
                    table.ForeignKey(
                        name: "FK_AssetMaintenances_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetMaintenances_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetMaintenances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_ActorUserId",
                table: "AssetMaintenances",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "AssetId" },
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId_CreatedAt",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "AssetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenances_CompanyId_AssetId_Status",
                table: "AssetMaintenances",
                columns: new[] { "CompanyId", "AssetId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMaintenances");
        }
    }
}
