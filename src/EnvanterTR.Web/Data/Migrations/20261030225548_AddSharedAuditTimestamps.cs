using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedAuditTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SubscriptionPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SubscriptionPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PublicSiteContents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PlatformFollowUpEmailDigests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PlatformFollowUpEmailDigests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PlatformAuditRetentionPolicies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PlatformAdministrators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PlatformAdministrators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUserTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetUserTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUserRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetUserRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUserLogins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetUserLogins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetUserClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetUserClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AspNetRoleClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AspNetRoleClaims",
                type: "timestamp with time zone",
                nullable: true);

            // Keep direct SQL and bulk writers covered too. Existing rows remain
            // unknown; only future INSERT/UPDATE operations receive timestamps.
            var auditColumns = migrationBuilder.Operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().ToArray();
            foreach (var table in auditColumns.GroupBy(column => column.Table))
            {
                var created = table.Any(column => column.Name == "CreatedAt") ? "1" : "0";
                var updated = table.Any(column => column.Name == "UpdatedAt") ? "1" : "0";
                migrationBuilder.Sql($"""
                    CREATE TRIGGER "envtr_shared_audit_v125" BEFORE INSERT OR UPDATE ON "{table.Key}"
                    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('{created}', '{updated}');
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[]
                     {
                         "SubscriptionPlans", "PublicSiteContents", "PlatformFollowUpEmailDigests",
                         "PlatformAuditRetentionPolicies", "PlatformAdministrators", "AspNetUserTokens",
                         "AspNetUsers", "AspNetUserRoles", "AspNetUserLogins", "AspNetUserClaims",
                         "AspNetRoles", "AspNetRoleClaims"
                     })
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS \"envtr_shared_audit_v125\" ON \"{table}\";");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PublicSiteContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PlatformFollowUpEmailDigests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PlatformFollowUpEmailDigests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PlatformAuditRetentionPolicies");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PlatformAdministrators");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PlatformAdministrators");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUserTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUserTokens");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUserLogins");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetRoleClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetRoleClaims");
        }
    }
}
