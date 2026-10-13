using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCheckoutOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyCheckoutOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OwnerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermDays = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CheckoutTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CheckoutPageUrlProtected = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCheckoutOrders", x => x.Id);
                    table.CheckConstraint("CK_CompanyCheckoutOrders_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_CompanyCheckoutOrders_Period", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_CompanyCheckoutOrders_Status", "\"Status\" IN ('Pending', 'PaymentStarted', 'PaymentReview', 'PaymentFailed', 'Expired', 'Provisioned', 'ProvisioningFailed')");
                    table.CheckConstraint("CK_CompanyCheckoutOrders_Term", "\"TermDays\" IN (30, 365)");
                    table.ForeignKey(
                        name: "FK_CompanyCheckoutOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyCheckoutOrders_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_CheckoutTokenHash",
                table: "CompanyCheckoutOrders",
                column: "CheckoutTokenHash",
                unique: true,
                filter: "\"CheckoutTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_CompanyId",
                table: "CompanyCheckoutOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_PaymentReference",
                table: "CompanyCheckoutOrders",
                column: "PaymentReference",
                unique: true,
                filter: "\"PaymentReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_PlanId",
                table: "CompanyCheckoutOrders",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_Slug",
                table: "CompanyCheckoutOrders",
                column: "Slug",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'PaymentStarted', 'PaymentReview', 'ProvisioningFailed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCheckoutOrders");
        }
    }
}
