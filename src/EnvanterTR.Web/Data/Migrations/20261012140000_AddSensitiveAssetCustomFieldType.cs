using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261012140000_AddSensitiveAssetCustomFieldType")]
public sealed class AddSensitiveAssetCustomFieldType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AssetCustomFieldDefinitions_Type",
            table: "AssetCustomFieldDefinitions");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AssetCustomFieldDefinitions_Type",
            table: "AssetCustomFieldDefinitions",
            sql: "\"FieldType\" IN ('text', 'number', 'date', 'checkbox', 'select', 'sensitive')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AssetCustomFieldDefinitions_Type",
            table: "AssetCustomFieldDefinitions");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AssetCustomFieldDefinitions_Type",
            table: "AssetCustomFieldDefinitions",
            sql: "\"FieldType\" IN ('text', 'number', 'date', 'checkbox', 'select')");
    }
}
