START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    CREATE INDEX "IX_Assets_CompanyId_BrandId" ON "Assets" ("CompanyId", "BrandId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    CREATE INDEX "IX_Assets_CompanyId_ModelId" ON "Assets" ("CompanyId", "ModelId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    ALTER TABLE "AssetModels" ADD CONSTRAINT "FK_AssetModels_AssetBrands_CompanyId_BrandId" FOREIGN KEY ("CompanyId", "BrandId") REFERENCES "AssetBrands" ("CompanyId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    ALTER TABLE "Assets" ADD CONSTRAINT "FK_Assets_AssetBrands_CompanyId_BrandId" FOREIGN KEY ("CompanyId", "BrandId") REFERENCES "AssetBrands" ("CompanyId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    ALTER TABLE "Assets" ADD CONSTRAINT "FK_Assets_AssetModels_CompanyId_ModelId" FOREIGN KEY ("CompanyId", "ModelId") REFERENCES "AssetModels" ("CompanyId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030220901_AddCompanyScopedCatalogForeignKeys') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030220901_AddCompanyScopedCatalogForeignKeys', '10.0.0');
    END IF;
END $EF$;
COMMIT;
