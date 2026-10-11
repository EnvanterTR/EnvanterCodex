using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContractType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetContracts", x => x.Id);
                    table.UniqueConstraint("AK_AssetContracts_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AssetContracts_Cost", "\"Cost\" IS NULL OR \"Cost\" >= 0");
                    table.CheckConstraint("CK_AssetContracts_Currency", "char_length(\"Currency\") = 3");
                    table.CheckConstraint("CK_AssetContracts_Dates", "\"StartDate\" IS NULL OR \"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_AssetContracts_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetContracts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetContracts_SupplierCatalogEntries_CompanyId_SupplierId",
                        columns: x => new { x.CompanyId, x.SupplierId },
                        principalTable: "SupplierCatalogEntries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetContractAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetContractAssets", x => x.Id);
                    table.UniqueConstraint("AK_AssetContractAssets_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssetContractAssets_AssetContracts_CompanyId_ContractId",
                        columns: x => new { x.CompanyId, x.ContractId },
                        principalTable: "AssetContracts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetContractAssets_Assets_CompanyId_AssetId",
                        columns: x => new { x.CompanyId, x.AssetId },
                        principalTable: "Assets",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetContractAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageFileName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetContractAttachments", x => x.Id);
                    table.UniqueConstraint("AK_AssetContractAttachments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AssetContractAttachments_Length", "\"Length\" BETWEEN 1 AND 10485760");
                    table.ForeignKey(
                        name: "FK_AssetContractAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssetContractAttachments_AssetContracts_CompanyId_ContractId",
                        columns: x => new { x.CompanyId, x.ContractId },
                        principalTable: "AssetContracts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetContractLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetContractLicenses", x => x.Id);
                    table.UniqueConstraint("AK_AssetContractLicenses_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssetContractLicenses_AssetContracts_CompanyId_ContractId",
                        columns: x => new { x.CompanyId, x.ContractId },
                        principalTable: "AssetContracts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetContractLicenses_SoftwareLicenses_CompanyId_LicenseId",
                        columns: x => new { x.CompanyId, x.LicenseId },
                        principalTable: "SoftwareLicenses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractAssets_CompanyId_AssetId",
                table: "AssetContractAssets",
                columns: new[] { "CompanyId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractAssets_CompanyId_ContractId_AssetId",
                table: "AssetContractAssets",
                columns: new[] { "CompanyId", "ContractId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractAttachments_CompanyId_ContractId_CreatedAt",
                table: "AssetContractAttachments",
                columns: new[] { "CompanyId", "ContractId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractAttachments_UploadedByUserId",
                table: "AssetContractAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractLicenses_CompanyId_ContractId_LicenseId",
                table: "AssetContractLicenses",
                columns: new[] { "CompanyId", "ContractId", "LicenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetContractLicenses_CompanyId_LicenseId",
                table: "AssetContractLicenses",
                columns: new[] { "CompanyId", "LicenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContracts_CompanyId_ContractNumber",
                table: "AssetContracts",
                columns: new[] { "CompanyId", "ContractNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContracts_CompanyId_IsActive_EndDate_Name",
                table: "AssetContracts",
                columns: new[] { "CompanyId", "IsActive", "EndDate", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContracts_CompanyId_SupplierId",
                table: "AssetContracts",
                columns: new[] { "CompanyId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetContracts_CreatedByUserId",
                table: "AssetContracts",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetContractAssets");

            migrationBuilder.DropTable(
                name: "AssetContractAttachments");

            migrationBuilder.DropTable(
                name: "AssetContractLicenses");

            migrationBuilder.DropTable(
                name: "AssetContracts");
        }
    }
}
