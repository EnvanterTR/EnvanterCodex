START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    ALTER TABLE "EmployeeHandoverDocuments" ADD CONSTRAINT "AK_EmployeeHandoverDocuments_CompanyId_EmployeeId_Id" UNIQUE ("CompanyId", "EmployeeId", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    CREATE TABLE "EmployeeHandoverEmailAttempts" (
        "Id" uuid NOT NULL,
        "CompanyId" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "RequestedByUserId" uuid NOT NULL,
        "IdempotencyKey" uuid NOT NULL,
        "RecipientEmail" character varying(254) NOT NULL,
        "SnapshotSha256" character varying(64) NOT NULL,
        "PdfSha256" character varying(64) NOT NULL,
        "Status" character varying(16) NOT NULL,
        "ErrorType" character varying(128),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "AcceptedAt" timestamp with time zone,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_EmployeeHandoverEmailAttempts" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_HandoverEmail_AcceptedAt" CHECK (("Status" = 'Accepted') = ("AcceptedAt" IS NOT NULL)),
        CONSTRAINT "CK_HandoverEmail_RequestKey" CHECK ("IdempotencyKey" <> '00000000-0000-0000-0000-000000000000'::uuid),
        CONSTRAINT "CK_HandoverEmail_Status" CHECK ("Status" IN ('Sending', 'Accepted', 'Uncertain')),
        CONSTRAINT "FK_EmployeeHandoverEmailAttempts_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_EmployeeHandoverEmailAttempts_EmployeeHandoverDocuments_Com~" FOREIGN KEY ("CompanyId", "EmployeeId", "DocumentId") REFERENCES "EmployeeHandoverDocuments" ("CompanyId", "EmployeeId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_EmployeeHandoverEmailAttempts_Employees_CompanyId_EmployeeId" FOREIGN KEY ("CompanyId", "EmployeeId") REFERENCES "Employees" ("CompanyId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    CREATE INDEX "IX_EmployeeHandoverEmailAttempts_CompanyId_DocumentId_CreatedAt" ON "EmployeeHandoverEmailAttempts" ("CompanyId", "DocumentId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    CREATE UNIQUE INDEX "IX_EmployeeHandoverEmailAttempts_CompanyId_DocumentId_Idempote~" ON "EmployeeHandoverEmailAttempts" ("CompanyId", "DocumentId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    CREATE INDEX "IX_EmployeeHandoverEmailAttempts_CompanyId_EmployeeId_Document~" ON "EmployeeHandoverEmailAttempts" ("CompanyId", "EmployeeId", "DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026083058_AddHandoverEmailAttempts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026083058_AddHandoverEmailAttempts', '10.0.0');
    END IF;
END $EF$;
COMMIT;

