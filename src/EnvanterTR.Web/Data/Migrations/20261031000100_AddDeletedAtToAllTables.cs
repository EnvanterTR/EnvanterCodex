using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAtToAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Accessories",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryAssignments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryCategories",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryKitLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryKits",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryModels",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryMovements",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetRoleClaims",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetRoles",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUserClaims",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUserLogins",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUserRoles",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AspNetUserTokens",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetBrands",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetCategories",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetContractAssets",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetContractAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetContractLicenses",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetContracts",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetCustomFieldDefinitions",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetEvents",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetInstalledSoftware",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetMaintenanceAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetMaintenances",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetModelAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetModels",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AssetReservations",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AuditEntries",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyApiTokens",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyCheckoutOrders",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyCustomRoles",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyDepartments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyLocations",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyMembershipCompanyUnitAccesses",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyMembershipLocationAccesses",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyMemberships",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyMfaEnrollmentChallenges",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyPurchaseBudgets",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyTasks",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CompanyUnits",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ComponentAllocations",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ComponentMovements",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ConsumableMovements",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Consumables",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CustomAssetReports",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "DashboardPreferences",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "EmployeeDocuments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "EmployeeHandoverDocuments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "InventoryComponents",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "InventoryRequestKitFulfillmentLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "InventoryRequests",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "LocalLicenseGrants",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "OperationalNotificationEmailDigests",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "OperationalNotificationPreferences",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "OperationalNotificationStates",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PlatformAdministrators",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PlatformAuditEntries",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PlatformAuditRetentionPolicies",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PlatformDemoRequests",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PlatformFollowUpEmailDigests",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PublicSiteContents",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PurchaseOrderReturnLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PurchaseOrderReturns",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SavedAssetImportMappings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SavedAssetSearches",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ScheduledReports",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicenseAssignments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicenseAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicenseEvents",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicensePurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SoftwareLicenses",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "StockPurchaseOrderAttachments",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "StockPurchaseOrderLines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "StockPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SubscriptionCoupons",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SubscriptionPlans",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SupplierCatalogEntries",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SupportTicketEmailOutbox",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SupportTicketMessages",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SupportTickets",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "UserSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Accessories");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryAssignments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryCategories");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryKitLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryKits");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryModels");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryMovements");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrderAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrderLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccessoryPurchaseOrders");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetRoleClaims");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetRoles");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUserClaims");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUserLogins");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUserRoles");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUsers");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AspNetUserTokens");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetBrands");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetCategories");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetContractAssets");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetContractAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetContractLicenses");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetContracts");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetCustomFieldDefinitions");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetEvents");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetInstalledSoftware");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetMaintenanceAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetMaintenances");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetModelAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetModels");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetPurchaseOrderAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetPurchaseOrderLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetPurchaseOrders");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssetReservations");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AuditEntries");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyApiTokens");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyCheckoutOrders");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyCustomRoles");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyDepartments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyLocations");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyMembershipCompanyUnitAccesses");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyMembershipLocationAccesses");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyMemberships");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyMfaEnrollmentChallenges");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyPurchaseBudgets");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyTasks");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyUnits");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ComponentAllocations");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ComponentMovements");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ConsumableMovements");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Consumables");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomAssetReports");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DashboardPreferences");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeDocuments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeHandoverDocuments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryComponents");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryRequestKitFulfillmentLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryRequests");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LocalLicenseGrants");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OperationalNotificationEmailDigests");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OperationalNotificationPreferences");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OperationalNotificationStates");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlatformAdministrators");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlatformAuditEntries");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlatformAuditRetentionPolicies");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlatformDemoRequests");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PlatformFollowUpEmailDigests");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PublicSiteContents");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PurchaseOrderReturnLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PurchaseOrderReturns");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SavedAssetImportMappings");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SavedAssetSearches");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ScheduledReports");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenseAssignments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenseAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenseEmployeeEulaAcceptances");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenseEvents");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicensePurchaseOrders");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenses");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockPurchaseOrderAttachments");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockPurchaseOrderLines");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockPurchaseOrders");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SubscriptionCoupons");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SubscriptionPlans");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupplierCatalogEntries");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupportTicketEmailOutbox");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupportTicketMessages");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupportTickets");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TenantSubscriptions");
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserSessions");
        }
    }
}
