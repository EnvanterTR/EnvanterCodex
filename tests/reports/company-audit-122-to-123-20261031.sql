START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "UserSessions" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "TenantSubscriptions" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SupportTicketMessages" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SupportTicketEmailOutbox" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "StockPurchaseOrderLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "StockPurchaseOrderAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseEvents" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseEvents" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseEmployeeEulaAcceptances" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseEmployeeEulaAcceptances" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseAssignments" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "SoftwareLicenseAssignments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "PurchaseOrderReturns" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "PurchaseOrderReturnLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "PlatformAuditEntries" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "PlatformAuditEntries" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "OperationalNotificationStates" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "OperationalNotificationPreferences" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "OperationalNotificationEmailDigests" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "OperationalNotificationEmailDigests" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "LocalLicenseGrants" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "InventoryRequests" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "InventoryRequestKitFulfillmentLines" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "InventoryRequestKitFulfillmentLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "Employees" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "EmployeeHandoverDocuments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "EmployeeDocuments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "DashboardPreferences" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ConsumableMovements" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ConsumableMovements" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ComponentMovements" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ComponentMovements" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ComponentAllocations" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "ComponentAllocations" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMfaEnrollmentChallenges" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMemberships" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMembershipLocationAccesses" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMembershipLocationAccesses" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMembershipCompanyUnitAccesses" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyMembershipCompanyUnitAccesses" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "CompanyApiTokens" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "Companies" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AuditEntries" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AuditEntries" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetPurchaseOrderLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetPurchaseOrderAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetModels" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetModels" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetModelAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetMaintenanceAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetInstalledSoftware" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetInstalledSoftware" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetEvents" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetEvents" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetContractLicenses" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetContractAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetContractAssets" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetCategories" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetCategories" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetBrands" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetBrands" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AssetAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryPurchaseOrderLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryPurchaseOrderAttachments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryMovements" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryMovements" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryModels" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryModels" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryKitLines" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryKitLines" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryCategories" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryAssignments" ADD "CreatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    ALTER TABLE "AccessoryAssignments" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "UserSessions"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "TenantSubscriptions"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SupportTicketMessages"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SupportTicketEmailOutbox"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "StockPurchaseOrderLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "StockPurchaseOrderAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SoftwareLicenseEvents"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SoftwareLicenseEmployeeEulaAcceptances"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SoftwareLicenseAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "SoftwareLicenseAssignments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "PurchaseOrderReturns"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "PurchaseOrderReturnLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "PlatformAuditEntries"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "OperationalNotificationStates"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "OperationalNotificationPreferences"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "OperationalNotificationEmailDigests"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "LocalLicenseGrants"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "InventoryRequests"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "InventoryRequestKitFulfillmentLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "Employees"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "EmployeeHandoverDocuments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "EmployeeDocuments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "DashboardPreferences"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "ConsumableMovements"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "ComponentMovements"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "ComponentAllocations"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyMfaEnrollmentChallenges"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyMemberships"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyMembershipLocationAccesses"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyMembershipCompanyUnitAccesses"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyCheckoutOrders"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "CompanyApiTokens"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "Companies"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AuditEntries"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetPurchaseOrderLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetPurchaseOrderAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetModels"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetModelAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetMaintenanceAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetInstalledSoftware"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetEvents"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetContractLicenses"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetContractAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetContractAssets"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetCategories"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetBrands"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AssetAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryPurchaseOrderLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryPurchaseOrderAttachments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryMovements"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryModels"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryKitLines"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryCategories"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('0', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    CREATE TRIGGER "envtr_missing_audit_v123" BEFORE INSERT OR UPDATE ON "AccessoryAssignments"
    FOR EACH ROW EXECUTE FUNCTION "envtr_missing_audit_v123"('1', '1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030210807_AddMissingCompanyAuditTimestamps') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030210807_AddMissingCompanyAuditTimestamps', '10.0.0');
    END IF;
END $EF$;
COMMIT;
