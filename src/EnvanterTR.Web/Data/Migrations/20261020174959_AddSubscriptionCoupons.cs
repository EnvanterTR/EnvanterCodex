using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "CompanyCheckoutOrders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CouponId",
                table: "CompanyCheckoutOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CompanyCheckoutOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "CompanyCheckoutOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE \"CompanyCheckoutOrders\" SET \"SubtotalAmount\" = \"Amount\";");

            migrationBuilder.CreateTable(
                name: "SubscriptionCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    MaxRedemptionsPerCustomer = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionCoupons", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionCoupons_Limits", "(\"MaxRedemptions\" IS NULL OR \"MaxRedemptions\" > 0) AND (\"MaxRedemptionsPerCustomer\" IS NULL OR \"MaxRedemptionsPerCustomer\" > 0)");
                    table.CheckConstraint("CK_SubscriptionCoupons_Period", "\"EndsAt\" IS NULL OR \"StartsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\"");
                    table.CheckConstraint("CK_SubscriptionCoupons_Type", "\"DiscountType\" IN ('Percent', 'Fixed')");
                    table.CheckConstraint("CK_SubscriptionCoupons_Value", "\"DiscountValue\" > 0 AND ((\"DiscountType\" = 'Percent' AND \"DiscountValue\" <= 100) OR (\"DiscountType\" = 'Fixed' AND \"Currency\" IS NOT NULL AND char_length(\"Currency\") = 3))");
                    table.ForeignKey(
                        name: "FK_SubscriptionCoupons_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionCoupons_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheckoutOrders_CouponId",
                table: "CompanyCheckoutOrders",
                column: "CouponId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CompanyCheckoutOrders_CouponSnapshot",
                table: "CompanyCheckoutOrders",
                sql: "(\"CouponId\" IS NULL AND \"CouponCode\" IS NULL AND \"DiscountAmount\" = 0) OR (\"CouponId\" IS NOT NULL AND \"CouponCode\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CompanyCheckoutOrders_Pricing",
                table: "CompanyCheckoutOrders",
                sql: "\"SubtotalAmount\" >= 0 AND \"DiscountAmount\" >= 0 AND \"Amount\" = \"SubtotalAmount\" - \"DiscountAmount\"");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCoupons_Code",
                table: "SubscriptionCoupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCoupons_CreatedByUserId",
                table: "SubscriptionCoupons",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCoupons_PlanId",
                table: "SubscriptionCoupons",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCheckoutOrders_SubscriptionCoupons_CouponId",
                table: "CompanyCheckoutOrders",
                column: "CouponId",
                principalTable: "SubscriptionCoupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCheckoutOrders_SubscriptionCoupons_CouponId",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropTable(
                name: "SubscriptionCoupons");

            migrationBuilder.DropIndex(
                name: "IX_CompanyCheckoutOrders_CouponId",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CompanyCheckoutOrders_CouponSnapshot",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CompanyCheckoutOrders_Pricing",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "CompanyCheckoutOrders");
        }
    }
}
