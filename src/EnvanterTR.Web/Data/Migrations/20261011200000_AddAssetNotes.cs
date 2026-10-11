using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    public partial class AddAssetNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "Notes", table: "Assets", type: "character varying(4000)", maxLength: 4000, nullable: true);
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "Notes", table: "Assets");
    }
}