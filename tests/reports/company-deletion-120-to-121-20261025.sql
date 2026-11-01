START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024212238_AddRecoverableCompanyDeletion') THEN
    ALTER TABLE "Companies" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024212238_AddRecoverableCompanyDeletion') THEN
    ALTER TABLE "Companies" ADD CONSTRAINT "CK_Companies_DeletedInactive" CHECK ("DeletedAt" IS NULL OR NOT "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024212238_AddRecoverableCompanyDeletion') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261024212238_AddRecoverableCompanyDeletion', '10.0.0');
    END IF;
END $EF$;
COMMIT;

