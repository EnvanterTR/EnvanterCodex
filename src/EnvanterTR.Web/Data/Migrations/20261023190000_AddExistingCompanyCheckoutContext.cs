using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261023190000_AddExistingCompanyCheckoutContext")]
public sealed class AddExistingCompanyCheckoutContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("TargetCompanyId", "CompanyCheckoutOrders", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("RequestedByUserId", "CompanyCheckoutOrders", type: "uuid", nullable: true);
        migrationBuilder.AddCheckConstraint("CK_CompanyCheckoutOrders_TargetContext", "CompanyCheckoutOrders",
            "(\"TargetCompanyId\" IS NULL AND \"RequestedByUserId\" IS NULL) OR (\"TargetCompanyId\" IS NOT NULL AND \"RequestedByUserId\" IS NOT NULL AND \"TermDays\" = 365)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_CompanyCheckoutOrders_TargetContext", "CompanyCheckoutOrders");
        migrationBuilder.DropColumn("RequestedByUserId", "CompanyCheckoutOrders");
        migrationBuilder.DropColumn("TargetCompanyId", "CompanyCheckoutOrders");
    }
}
