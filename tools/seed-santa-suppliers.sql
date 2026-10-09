-- Adds a small purchasing catalog to the existing Santa sample tenant only.
-- Safe to rerun; the tenant-scoped normalized-name key prevents duplicates.
\set ON_ERROR_STOP on
BEGIN;
UPDATE "SupplierCatalogEntries" SET "Name"='Santa Bilgi Teknolojileri Tedarik', "NormalizedName"='SANTA BİLGİ TEKNOLOJİLERİ TEDARİK', "UpdatedAt"=now()
WHERE "CompanyId"=(SELECT "Id" FROM "Companies" WHERE "Slug"='santa-demo') AND "Name"='Santa Bilgi Teknolojileri Tedar';
WITH santa AS (SELECT "Id" FROM "Companies" WHERE "Slug"='santa-demo'), suppliers("Name","NormalizedName","Email","Phone","Website") AS (VALUES
 ('Santa Bilgi Teknolojileri Tedarik','SANTA BİLGİ TEKNOLOJİLERİ TEDARİK',NULL,NULL,'https://santa.example.test'),
 ('Penta Teknoloji','PENTA TEKNOLOJİ','info@penta.com.tr','+90 212 371 70 00','https://www.penta.com.tr'),
 ('Arena Bilgisayar','ARENA BİLGİSAYAR','info@arenabilgisayar.com','+90 212 364 64 64','https://www.arenabilgisayar.com'),
 ('Vatan Bilgisayar','VATAN BİLGİSAYAR','info@vatanbilgisayar.com','0850 222 56 56','https://www.vatanbilgisayar.com'),
 ('Teknosa','TEKNOSA','musteri.hizmetleri@teknosa.com','0850 222 55 99','https://www.teknosa.com'),
 ('Hepsiburada','HEPSİBURADA','destek@hepsiburada.com','0850 252 40 00','https://www.hepsiburada.com'),
 ('İncehesap','İNCEHESAP','destek@incehesap.com','0850 800 46 23','https://www.incehesap.com'),
 ('Amazon Türkiye','AMAZON TÜRKİYE','—','—','https://www.amazon.com.tr'))
INSERT INTO "SupplierCatalogEntries" ("Id","CompanyId","Name","NormalizedName","Email","Phone","Website","IsActive","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), santa."Id", suppliers."Name", suppliers."NormalizedName", NULLIF(suppliers."Email",'—'), NULLIF(suppliers."Phone",'—'), suppliers."Website", TRUE, now(), now()
FROM santa CROSS JOIN suppliers
ON CONFLICT ("CompanyId","NormalizedName") DO NOTHING;
UPDATE "Accessories" a SET "SupplierId"=s."Id", "Supplier"=s."Name", "UpdatedAt"=now()
FROM "SupplierCatalogEntries" s
WHERE s."CompanyId"=a."CompanyId" AND a."CompanyId"=(SELECT "Id" FROM "Companies" WHERE "Slug"='santa-demo')
  AND a."Supplier"='Santa Bilgi Teknolojileri Tedarik';
COMMIT;
