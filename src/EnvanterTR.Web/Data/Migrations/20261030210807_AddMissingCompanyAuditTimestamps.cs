using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingCompanyAuditTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "UserSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SupportTicketMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SupportTicketEmailOutbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "StockPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "StockPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SoftwareLicenseEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SoftwareLicenseEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SoftwareLicenseAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SoftwareLicenseAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SoftwareLicenseAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PurchaseOrderReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PurchaseOrderReturnLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PlatformAuditEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PlatformAuditEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "OperationalNotificationStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "OperationalNotificationPreferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "OperationalNotificationEmailDigests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "OperationalNotificationEmailDigests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "LocalLicenseGrants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "InventoryRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "InventoryRequestKitFulfillmentLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "InventoryRequestKitFulfillmentLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "EmployeeHandoverDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "EmployeeDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "DashboardPreferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ConsumableMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ConsumableMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ComponentMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ComponentMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ComponentAllocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ComponentAllocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyMfaEnrollmentChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyMemberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CompanyMembershipLocationAccesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyMembershipLocationAccesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CompanyMembershipCompanyUnitAccesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyMembershipCompanyUnitAccesses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyCheckoutOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CompanyApiTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AuditEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AuditEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AssetModels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetModels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetModelAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetMaintenanceAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AssetInstalledSoftware",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetInstalledSoftware",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AssetEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetContractLicenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetContractAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetContractAssets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AssetCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AssetBrands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetBrands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AssetAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AccessoryMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AccessoryModels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryModels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AccessoryKitLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryKitLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AccessoryAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AccessoryAssignments",
                type: "timestamp with time zone",
                nullable: true);

            // Add nullable columns first, without a historical backfill/default.
            // Row triggers cover bulk/direct SQL writes as well as tracked EF writes.
            var auditColumns = migrationBuilder.Operations.OfType<AddColumnOperation>().ToArray();
            migrationBuilder.Sql("""
                CREATE FUNCTION "envtr_missing_audit_v123"() RETURNS trigger LANGUAGE plpgsql AS $audit$
                BEGIN
                    IF TG_OP = 'INSERT' AND TG_ARGV[0] = '1' AND to_jsonb(NEW)->>'CreatedAt' IS NULL THEN
                        NEW := jsonb_populate_record(NEW, jsonb_build_object('CreatedAt', clock_timestamp()));
                    END IF;
                    IF TG_ARGV[1] = '1' THEN
                        NEW := jsonb_populate_record(NEW, jsonb_build_object('UpdatedAt', clock_timestamp()));
                    END IF;
                    RETURN NEW;
                END;
                $audit$;
                """);
            foreach (var table in auditColumns.GroupBy(column => column.Table))
            {
                var created = table.Any(column => column.Name == "CreatedAt") ? "1" : "0";
                var updated = table.Any(column => column.Name == "UpdatedAt") ? "1" : "0";
                migrationBuilder.Sql($"""
                    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "{table.Key}"
                    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('{created}', '{updated}');
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupportTicketMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupportTicketEmailOutbox");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StockPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StockPurchaseOrderAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SoftwareLicenseEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SoftwareLicenseEvents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SoftwareLicenseAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SoftwareLicenseAssignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseOrderReturns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseOrderReturnLines");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PlatformAuditEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PlatformAuditEntries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OperationalNotificationStates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OperationalNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OperationalNotificationEmailDigests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OperationalNotificationEmailDigests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LocalLicenseGrants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InventoryRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InventoryRequestKitFulfillmentLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InventoryRequestKitFulfillmentLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EmployeeHandoverDocuments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DashboardPreferences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ConsumableMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ConsumableMovements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ComponentMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ComponentMovements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ComponentAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ComponentAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyMfaEnrollmentChallenges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyMemberships");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CompanyMembershipLocationAccesses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyMembershipLocationAccesses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CompanyMembershipCompanyUnitAccesses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyMembershipCompanyUnitAccesses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyCheckoutOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyApiTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetPurchaseOrderAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetModels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetModelAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetMaintenanceAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetInstalledSoftware");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetInstalledSoftware");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetContractLicenses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetContractAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetContractAssets");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetBrands");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssetAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryPurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryPurchaseOrderAttachments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AccessoryMovements");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryMovements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AccessoryModels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryModels");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryKitLines");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AccessoryAssignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AccessoryAssignments");

            foreach (var table in migrationBuilder.Operations.OfType<DropColumnOperation>()
                         .Select(column => column.Table).Distinct().ToArray())
                migrationBuilder.Sql($"DROP TRIGGER \"envtr_missing_audit_v123\" ON \"{table}\";");
            migrationBuilder.Sql("DROP FUNCTION \"envtr_missing_audit_v123\"();");
        }
    }
}
