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
DO $$
DECLARE
    company_id uuid := (SELECT "Id" FROM "Companies" WHERE "Slug"='santa-demo');
    actor_id uuid;
    supplier_id uuid;
    accessory_id uuid;
    order_id uuid;
BEGIN
    SELECT "UserId" INTO actor_id FROM "CompanyMemberships"
      WHERE "CompanyId"=company_id AND "RoleKey"='owner' LIMIT 1;
    SELECT "Id" INTO supplier_id FROM "SupplierCatalogEntries"
      WHERE "CompanyId"=company_id AND "NormalizedName"='PENTA TEKNOLOJİ' AND "IsActive" LIMIT 1;
    SELECT "Id" INTO accessory_id FROM "Accessories"
      WHERE "CompanyId"=company_id AND "ItemNumber"='SNT-ACC-002';

    IF company_id IS NULL OR accessory_id IS NULL THEN
        RAISE EXCEPTION 'Santa demo company and accessory catalog must be seeded first';
    END IF;

    INSERT INTO "AccessoryPurchaseOrders" (
        "Id","CompanyId","OrderNumber","Supplier","SupplierId","Status","OrderDate","ExpectedOn","Currency",
        "TaxRatePercent","DiscountAmount","FreightAmount","Notes","CreatedByUserId","CreatedAt","UpdatedAt")
    VALUES (gen_random_uuid(),company_id,'SNT-DEMO-ACC-PO-001','Penta Teknoloji',supplier_id,'PartiallyReceived',
            current_date-2,current_date+7,'TRY',20,0,0,
            'Santa demo · bekleyen teslimat raporu örneği; sevk veya stok girişi yapılmadı.',actor_id,now(),now())
    ON CONFLICT ("CompanyId","OrderNumber") DO NOTHING;

    SELECT "Id" INTO order_id FROM "AccessoryPurchaseOrders"
      WHERE "CompanyId"=company_id AND "OrderNumber"='SNT-DEMO-ACC-PO-001';
    INSERT INTO "AccessoryPurchaseOrderLines" (
        "Id","CompanyId","PurchaseOrderId","AccessoryId","Quantity","QuantityReceived","UnitCost","Notes","CreatedAt")
    SELECT gen_random_uuid(),company_id,order_id,accessory_id,12,3,2100,
           'Santa demo · 3 adet örnek teslim alındı; 9 adet bekliyor.',now()
    WHERE NOT EXISTS (
        SELECT 1 FROM "AccessoryPurchaseOrderLines" l
        WHERE l."CompanyId"=company_id AND l."PurchaseOrderId"=order_id AND l."AccessoryId"=accessory_id
    );
END $$;
COMMIT;
