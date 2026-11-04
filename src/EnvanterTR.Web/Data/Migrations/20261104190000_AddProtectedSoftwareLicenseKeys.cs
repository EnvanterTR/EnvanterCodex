using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using EnvanterTR.Web.Data;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261104190000_AddProtectedSoftwareLicenseKeys")]
    public partial class AddProtectedSoftwareLicenseKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtectedLicenseKey",
                table: "SoftwareLicenses",
                type: "character varying(12000)",
                maxLength: 12000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtectedLicenseKey",
                table: "SoftwareLicenses");
        }
    }
}
