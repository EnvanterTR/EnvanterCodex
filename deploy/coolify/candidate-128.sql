START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107150423_AddCompanyWebhooks') THEN
    CREATE TABLE "CompanyWebhooks" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "Name" character varying(80) NOT NULL,
        "Kind" character varying(16) NOT NULL,
        "ProtectedUrl" character varying(4000) NOT NULL,
        "UrlHint" character varying(120) NOT NULL,
        "ProtectedSigningSecret" character varying(1000),
        "EventsJson" character varying(500) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CursorOccurredAt" timestamp with time zone NOT NULL,
        "CursorEventId" uuid NOT NULL,
        "LastDeliveryAt" timestamp with time zone,
        "LastStatusCode" integer,
        "LastError" character varying(300),
        "ConsecutiveFailures" integer NOT NULL,
        CONSTRAINT "PK_CompanyWebhooks" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_CompanyWebhooks_Kind" CHECK ("Kind" IN ('slack', 'teams', 'generic')),
        CONSTRAINT "FK_CompanyWebhooks_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107150423_AddCompanyWebhooks') THEN
    CREATE INDEX "IX_AssetEvents_CompanyId_OccurredAt_Id" ON "AssetEvents" ("CompanyId", "OccurredAt", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107150423_AddCompanyWebhooks') THEN
    CREATE INDEX "IX_CompanyWebhooks_CompanyId_CreatedAt" ON "CompanyWebhooks" ("CompanyId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107150423_AddCompanyWebhooks') THEN
    CREATE INDEX "IX_CompanyWebhooks_IsActive" ON "CompanyWebhooks" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107150423_AddCompanyWebhooks') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261107150423_AddCompanyWebhooks', '10.0.0');
    END IF;
END $EF$;
COMMIT;

