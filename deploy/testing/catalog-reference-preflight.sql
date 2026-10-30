-- Read-only preflight before adding company-scoped catalog foreign keys.
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
