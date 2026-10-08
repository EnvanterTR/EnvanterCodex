using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

public partial class AddModelDepreciationPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("DepreciationMonths", "AssetModels", type: "integer", nullable: true);
        migrationBuilder.AddColumn<decimal>("DepreciationMinimum", "AssetModels", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true);
        migrationBuilder.AddColumn<string>("DepreciationMinimumType", "AssetModels", type: "character varying(12)", maxLength: 12, nullable: false, defaultValue: "amount");
        migrationBuilder.AddColumn<string>("DepreciationMethod", "AssetModels", type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "linear");
        migrationBuilder.AddCheckConstraint("CK_AssetModels_DepreciationMonths", "AssetModels", "\"DepreciationMonths\" IS NULL OR \"DepreciationMonths\" BETWEEN 1 AND 3600");
        migrationBuilder.AddCheckConstraint("CK_AssetModels_DepreciationMinimum", "AssetModels", "\"DepreciationMinimum\" IS NULL OR \"DepreciationMinimum\" >= 0");
        migrationBuilder.AddCheckConstraint("CK_AssetModels_DepreciationMinimumType", "AssetModels", "\"DepreciationMinimumType\" IN ('amount', 'percent')");
        migrationBuilder.AddCheckConstraint("CK_AssetModels_DepreciationMethod", "AssetModels", "\"DepreciationMethod\" IN ('linear', 'half_1', 'half_2')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_AssetModels_DepreciationMonths", "AssetModels");
        migrationBuilder.DropCheckConstraint("CK_AssetModels_DepreciationMinimum", "AssetModels");
        migrationBuilder.DropCheckConstraint("CK_AssetModels_DepreciationMinimumType", "AssetModels");
        migrationBuilder.DropCheckConstraint("CK_AssetModels_DepreciationMethod", "AssetModels");
        migrationBuilder.DropColumn("DepreciationMonths", "AssetModels");
        migrationBuilder.DropColumn("DepreciationMinimum", "AssetModels");
        migrationBuilder.DropColumn("DepreciationMinimumType", "AssetModels");
        migrationBuilder.DropColumn("DepreciationMethod", "AssetModels");
    }
}
