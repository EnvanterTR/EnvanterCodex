START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104101113_AddStockCatalogArchiving') THEN
    ALTER TABLE "InventoryComponents" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104101113_AddStockCatalogArchiving') THEN
    ALTER TABLE "Consumables" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104101113_AddStockCatalogArchiving') THEN
    ALTER TABLE "Accessories" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104101113_AddStockCatalogArchiving') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104101113_AddStockCatalogArchiving', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104112743_ArchiveEmployeeRecords') THEN
    ALTER TABLE "Employees" ADD "ArchivedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104112743_ArchiveEmployeeRecords') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104112743_ArchiveEmployeeRecords', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104161606_AddAssetModelEndOfLife') THEN
    ALTER TABLE "AssetModels" ADD "EndOfLifeOn" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104161606_AddAssetModelEndOfLife') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104161606_AddAssetModelEndOfLife', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104171011_AddAssetModelAnnualDepreciationRate') THEN
    ALTER TABLE "AssetModels" ADD "DepreciationAnnualRatePercent" numeric(5,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104171011_AddAssetModelAnnualDepreciationRate') THEN
    ALTER TABLE "AssetModels" ADD CONSTRAINT "CK_AssetModels_DepreciationAnnualRatePercent" CHECK ("DepreciationAnnualRatePercent" IS NULL OR "DepreciationAnnualRatePercent" BETWEEN 0.34 AND 100.00);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104171011_AddAssetModelAnnualDepreciationRate') THEN
    ALTER TABLE "AssetModels" ADD CONSTRAINT "CK_AssetModels_DepreciationPolicyExclusive" CHECK ("DepreciationMonths" IS NULL OR "DepreciationAnnualRatePercent" IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104171011_AddAssetModelAnnualDepreciationRate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104171011_AddAssetModelAnnualDepreciationRate', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104190000_AddProtectedSoftwareLicenseKeys') THEN
    ALTER TABLE "SoftwareLicenses" ADD "ProtectedLicenseKey" character varying(12000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261104190000_AddProtectedSoftwareLicenseKeys') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261104190000_AddProtectedSoftwareLicenseKeys', '10.0.0');
    END IF;
END $EF$;
COMMIT;

