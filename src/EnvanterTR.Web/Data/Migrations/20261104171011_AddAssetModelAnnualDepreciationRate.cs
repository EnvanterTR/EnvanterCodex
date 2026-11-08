using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetModelAnnualDepreciationRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepreciationAnnualRatePercent",
                table: "AssetModels",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetModels_DepreciationAnnualRatePercent",
                table: "AssetModels",
                sql: "\"DepreciationAnnualRatePercent\" IS NULL OR \"DepreciationAnnualRatePercent\" BETWEEN 0.34 AND 100.00");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetModels_DepreciationPolicyExclusive",
                table: "AssetModels",
                sql: "\"DepreciationMonths\" IS NULL OR \"DepreciationAnnualRatePercent\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetModels_DepreciationAnnualRatePercent",
                table: "AssetModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetModels_DepreciationPolicyExclusive",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "DepreciationAnnualRatePercent",
                table: "AssetModels");
        }
    }
}
