START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023190000_AddExistingCompanyCheckoutContext') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "TargetCompanyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023190000_AddExistingCompanyCheckoutContext') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "RequestedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023190000_AddExistingCompanyCheckoutContext') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD CONSTRAINT "CK_CompanyCheckoutOrders_TargetContext" CHECK (("TargetCompanyId" IS NULL AND "RequestedByUserId" IS NULL) OR ("TargetCompanyId" IS NOT NULL AND "RequestedByUserId" IS NOT NULL AND "TermDays" = 365));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261023190000_AddExistingCompanyCheckoutContext') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261023190000_AddExistingCompanyCheckoutContext', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonatorUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonatorSessionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonatorSecurityStampHash" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonationReturnTokenHash" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonatorRememberMe" boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonationReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD "ImpersonationEndedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    CREATE UNIQUE INDEX "IX_UserSessions_ImpersonationReturnTokenHash" ON "UserSessions" ("ImpersonationReturnTokenHash") WHERE "ImpersonationReturnTokenHash" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    CREATE UNIQUE INDEX "IX_UserSessions_ImpersonatorSessionId" ON "UserSessions" ("ImpersonatorSessionId") WHERE "ImpersonatorSessionId" IS NOT NULL AND "ImpersonationEndedAt" IS NULL AND "RevokedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    ALTER TABLE "UserSessions" ADD CONSTRAINT "CK_UserSessions_Impersonation" CHECK (("ImpersonatorUserId" IS NULL AND "ImpersonatorSessionId" IS NULL AND "ImpersonatorSecurityStampHash" IS NULL AND "ImpersonationReturnTokenHash" IS NULL AND "ImpersonatorRememberMe" IS NULL AND "ImpersonationReason" IS NULL AND "ImpersonationEndedAt" IS NULL) OR ("ImpersonatorUserId" IS NOT NULL AND "ImpersonatorSessionId" IS NOT NULL AND "ImpersonatorSecurityStampHash" IS NOT NULL AND "ImpersonatorRememberMe" IS NOT NULL AND "ImpersonationReason" IS NOT NULL AND "CompanyId" IS NOT NULL AND NOT "PlatformAdmin" AND ("ImpersonationReturnTokenHash" IS NOT NULL OR "ImpersonationEndedAt" IS NOT NULL)));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261024010000_AddPlatformImpersonationSessions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261024010000_AddPlatformImpersonationSessions', '10.0.0');
    END IF;
END $EF$;
COMMIT;
