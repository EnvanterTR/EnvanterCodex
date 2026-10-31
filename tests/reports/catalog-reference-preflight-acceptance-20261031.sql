BEGIN;
CREATE TEMP TABLE "AssetBrands" ("Id" uuid, "CompanyId" uuid);
CREATE TEMP TABLE "AssetCategories" ("Id" uuid, "CompanyId" uuid);
CREATE TEMP TABLE "AssetModels" ("Id" uuid, "CompanyId" uuid, "BrandId" uuid, "CategoryId" uuid);
CREATE TEMP TABLE "Assets" ("Id" uuid, "CompanyId" uuid, "BrandId" uuid, "ModelId" uuid, "DeletedAt" timestamptz);
INSERT INTO "AssetBrands" VALUES ('00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000001');
INSERT INTO "AssetCategories" VALUES ('00000000-0000-0000-0000-000000000012','00000000-0000-0000-0000-000000000002');
INSERT INTO "AssetModels" VALUES
('00000000-0000-0000-0000-000000000021','00000000-0000-0000-0000-000000000001',NULL,NULL),
('00000000-0000-0000-0000-000000000022','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000099','00000000-0000-0000-0000-000000000012');
INSERT INTO "Assets" VALUES
('00000000-0000-0000-0000-000000000031','00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000021',NULL),
('00000000-0000-0000-0000-000000000032','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000099',now());

CREATE TEMP VIEW catalog_audit AS -- Read-only preflight before adding company-scoped catalog foreign keys.
-- Includes trashed assets. Returns identifiers only; zero rows is the expected gate.
SELECT 'Assets.BrandId' AS reference, a."Id" AS record_id, a."CompanyId" AS company_id, a."BrandId" AS target_id,
       CASE WHEN EXISTS (SELECT 1 FROM "AssetBrands" b WHERE b."Id" = a."BrandId") THEN 'CrossCompany' ELSE 'MissingTarget' END AS reason
FROM "Assets" a LEFT JOIN "AssetBrands" b ON b."Id" = a."BrandId" AND b."CompanyId" = a."CompanyId"
WHERE a."BrandId" IS NOT NULL AND b."Id" IS NULL
UNION ALL
SELECT 'Assets.ModelId', a."Id", a."CompanyId", a."ModelId",
       CASE WHEN EXISTS (SELECT 1 FROM "AssetModels" m WHERE m."Id" = a."ModelId") THEN 'CrossCompany' ELSE 'MissingTarget' END
FROM "Assets" a LEFT JOIN "AssetModels" m ON m."Id" = a."ModelId" AND m."CompanyId" = a."CompanyId"
WHERE a."ModelId" IS NOT NULL AND m."Id" IS NULL
UNION ALL
SELECT 'AssetModels.BrandId', m."Id", m."CompanyId", m."BrandId",
       CASE WHEN EXISTS (SELECT 1 FROM "AssetBrands" b WHERE b."Id" = m."BrandId") THEN 'CrossCompany' ELSE 'MissingTarget' END
FROM "AssetModels" m LEFT JOIN "AssetBrands" b ON b."Id" = m."BrandId" AND b."CompanyId" = m."CompanyId"
WHERE m."BrandId" IS NOT NULL AND b."Id" IS NULL
UNION ALL
SELECT 'AssetModels.CategoryId', m."Id", m."CompanyId", m."CategoryId",
       CASE WHEN EXISTS (SELECT 1 FROM "AssetCategories" c WHERE c."Id" = m."CategoryId") THEN 'CrossCompany' ELSE 'MissingTarget' END
FROM "AssetModels" m LEFT JOIN "AssetCategories" c ON c."Id" = m."CategoryId" AND c."CompanyId" = m."CompanyId"
WHERE m."CategoryId" IS NOT NULL AND c."Id" IS NULL
ORDER BY reference, record_id;

DO $check$
BEGIN
 IF (SELECT count(*) FROM catalog_audit) <> 4 THEN RAISE EXCEPTION 'Expected four violations'; END IF;
 IF (SELECT count(DISTINCT reference) FROM catalog_audit) <> 4 THEN RAISE EXCEPTION 'A reference path was missed'; END IF;
 IF (SELECT count(*) FROM catalog_audit WHERE reason = 'CrossCompany') <> 2 THEN RAISE EXCEPTION 'Expected two cross-company links'; END IF;
 IF (SELECT count(*) FROM catalog_audit WHERE reason = 'MissingTarget') <> 2 THEN RAISE EXCEPTION 'Expected two missing targets'; END IF;
 IF EXISTS(SELECT 1 FROM catalog_audit WHERE record_id = '00000000-0000-0000-0000-000000000031') THEN RAISE EXCEPTION 'Valid links falsely rejected'; END IF;
END;
$check$;
SELECT reference, reason FROM catalog_audit ORDER BY reference;
ROLLBACK;
