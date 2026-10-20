START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "CouponCode" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "CouponId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD "SubtotalAmount" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    UPDATE "CompanyCheckoutOrders" SET "SubtotalAmount" = "Amount";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    CREATE TABLE "SubscriptionCoupons" (
        "Id" uuid NOT NULL,
        "Code" character varying(40) NOT NULL,
        "DiscountType" character varying(12) NOT NULL,
        "DiscountValue" numeric(18,2) NOT NULL,
        "Currency" character varying(3),
        "PlanId" uuid,
        "StartsAt" timestamp with time zone,
        "EndsAt" timestamp with time zone,
        "MaxRedemptions" integer,
        "MaxRedemptionsPerCustomer" integer,
        "IsActive" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SubscriptionCoupons" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_SubscriptionCoupons_Limits" CHECK (("MaxRedemptions" IS NULL OR "MaxRedemptions" > 0) AND ("MaxRedemptionsPerCustomer" IS NULL OR "MaxRedemptionsPerCustomer" > 0)),
        CONSTRAINT "CK_SubscriptionCoupons_Period" CHECK ("EndsAt" IS NULL OR "StartsAt" IS NULL OR "EndsAt" > "StartsAt"),
        CONSTRAINT "CK_SubscriptionCoupons_Type" CHECK ("DiscountType" IN ('Percent', 'Fixed')),
        CONSTRAINT "CK_SubscriptionCoupons_Value" CHECK ("DiscountValue" > 0 AND (("DiscountType" = 'Percent' AND "DiscountValue" <= 100) OR ("DiscountType" = 'Fixed' AND "Currency" IS NOT NULL AND char_length("Currency") = 3))),
        CONSTRAINT "FK_SubscriptionCoupons_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SubscriptionCoupons_SubscriptionPlans_PlanId" FOREIGN KEY ("PlanId") REFERENCES "SubscriptionPlans" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    CREATE INDEX "IX_CompanyCheckoutOrders_CouponId" ON "CompanyCheckoutOrders" ("CouponId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD CONSTRAINT "CK_CompanyCheckoutOrders_CouponSnapshot" CHECK (("CouponId" IS NULL AND "CouponCode" IS NULL AND "DiscountAmount" = 0) OR ("CouponId" IS NOT NULL AND "CouponCode" IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD CONSTRAINT "CK_CompanyCheckoutOrders_Pricing" CHECK ("SubtotalAmount" >= 0 AND "DiscountAmount" >= 0 AND "Amount" = "SubtotalAmount" - "DiscountAmount");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    CREATE UNIQUE INDEX "IX_SubscriptionCoupons_Code" ON "SubscriptionCoupons" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    CREATE INDEX "IX_SubscriptionCoupons_CreatedByUserId" ON "SubscriptionCoupons" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    CREATE INDEX "IX_SubscriptionCoupons_PlanId" ON "SubscriptionCoupons" ("PlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    ALTER TABLE "CompanyCheckoutOrders" ADD CONSTRAINT "FK_CompanyCheckoutOrders_SubscriptionCoupons_CouponId" FOREIGN KEY ("CouponId") REFERENCES "SubscriptionCoupons" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261020174959_AddSubscriptionCoupons') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261020174959_AddSubscriptionCoupons', '10.0.0');
    END IF;
END $EF$;
COMMIT;

