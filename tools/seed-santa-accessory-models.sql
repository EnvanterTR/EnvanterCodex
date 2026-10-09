-- Add reusable manufacturer/model links to an existing Santa demo tenant.
-- Safe to rerun: existing brands/models are reused and assigned accessories are preserved.
\set ON_ERROR_STOP on
BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Companies" WHERE "Name"='Santa') THEN
        RAISE EXCEPTION 'Santa demo tenant was not found; no changes were made.';
    END IF;
END $$;

INSERT INTO "AssetBrands" ("Id","CompanyId","Name")
SELECT gen_random_uuid(), c."Id", maker."Name"
FROM "Companies" c
CROSS JOIN (VALUES ('Logitech'),('Samsonite')) AS maker("Name")
WHERE c."Name"='Santa'
  AND NOT EXISTS (SELECT 1 FROM "AssetBrands" b WHERE b."CompanyId"=c."Id" AND lower(b."Name")=lower(maker."Name"));

WITH model_seed ("ItemNumber","BrandName","Name") AS (
    VALUES
        ('SNT-ACC-001','Logitech','MK540 Kablosuz Klavye ve Fare Seti'),
        ('SNT-ACC-002','Lenovo','USB-C 7-in-1 Hub'),
        ('SNT-ACC-003','Dell','WD19S USB-C Dock'),
        ('SNT-ACC-004','Samsonite','GuardIT 2.0 15.6 inç Sırt Çantası'),
        ('SNT-ACC-005',NULL,'Cat6 UTP 1 m'),
        ('SNT-ACC-006',NULL,'Cat6 UTP 3 m'),
        ('SNT-ACC-007',NULL,'HDMI 2.0 2 m'),
        ('SNT-ACC-008',NULL,'DisplayPort 1.4 2 m'),
        ('SNT-ACC-009','Logitech','M185 Kablosuz Fare'),
        ('SNT-ACC-010',NULL,'USB-C 65 W Laptop Adaptörü')
)
INSERT INTO "AccessoryModels" ("Id","CompanyId","BrandId","Name")
SELECT gen_random_uuid(), c."Id", b."Id", s."Name"
FROM "Companies" c
CROSS JOIN model_seed s
LEFT JOIN "AssetBrands" b ON b."CompanyId"=c."Id" AND b."Name"=s."BrandName"
WHERE c."Name"='Santa'
  AND EXISTS (SELECT 1 FROM "Accessories" a WHERE a."CompanyId"=c."Id" AND a."ItemNumber"=s."ItemNumber")
  AND NOT EXISTS (
      SELECT 1 FROM "AccessoryModels" m
      WHERE m."CompanyId"=c."Id" AND m."BrandId" IS NOT DISTINCT FROM b."Id" AND m."Name"=s."Name"
  );

WITH model_seed ("ItemNumber","BrandName","Name") AS (
    VALUES
        ('SNT-ACC-001','Logitech','MK540 Kablosuz Klavye ve Fare Seti'),
        ('SNT-ACC-002','Lenovo','USB-C 7-in-1 Hub'),
        ('SNT-ACC-003','Dell','WD19S USB-C Dock'),
        ('SNT-ACC-004','Samsonite','GuardIT 2.0 15.6 inç Sırt Çantası'),
        ('SNT-ACC-005',NULL,'Cat6 UTP 1 m'),
        ('SNT-ACC-006',NULL,'Cat6 UTP 3 m'),
        ('SNT-ACC-007',NULL,'HDMI 2.0 2 m'),
        ('SNT-ACC-008',NULL,'DisplayPort 1.4 2 m'),
        ('SNT-ACC-009','Logitech','M185 Kablosuz Fare'),
        ('SNT-ACC-010',NULL,'USB-C 65 W Laptop Adaptörü')
)
UPDATE "Accessories" a
SET "BrandId"=b."Id", "ModelId"=m."Id"
FROM "Companies" c
JOIN model_seed s ON TRUE
LEFT JOIN "AssetBrands" b ON b."CompanyId"=c."Id" AND b."Name"=s."BrandName"
JOIN "AccessoryModels" m ON m."CompanyId"=c."Id" AND m."BrandId" IS NOT DISTINCT FROM b."Id" AND m."Name"=s."Name"
WHERE c."Name"='Santa'
  AND a."CompanyId"=c."Id"
  AND a."ItemNumber"=s."ItemNumber"
  AND a."ModelId" IS NULL;

COMMIT;
