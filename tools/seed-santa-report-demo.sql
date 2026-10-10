-- Adds explicitly labeled, synthetic report examples to the local Santa demo tenant.
-- Safe to rerun; it never writes to another company or changes accessory on-hand stock.
\set ON_ERROR_STOP on
BEGIN;
DO $$
DECLARE
    company_id uuid;
    actor_id uuid;
    supplier_id uuid;
    accessory_id uuid;
    order_id uuid;
BEGIN
    SELECT "Id" INTO company_id FROM "Companies" WHERE "Slug"='santa-demo';
    IF company_id IS NULL THEN
        RAISE EXCEPTION 'Santa demo company is not seeded';
    END IF;
    SELECT "UserId" INTO actor_id FROM "CompanyMemberships"
      WHERE "CompanyId"=company_id AND "RoleKey"='owner' LIMIT 1;

    -- The current state on the asset determines whether this sample shows creation
    -- or assignment. Each row says Santa demo so it is not mistaken for live history.
    INSERT INTO "AssetEvents" ("Id","CompanyId","AssetId","ActorUserId","EventType","Details","EmployeeId","OccurredAt")
    SELECT gen_random_uuid(),a."CompanyId",a."Id",actor_id,
           CASE WHEN a."AssignedEmployeeId" IS NULL THEN 'Created' ELSE 'Assigned' END,
           CASE WHEN a."AssignedEmployeeId" IS NULL THEN 'Santa demo · örnek başlangıç kaydı' ELSE 'Santa demo · örnek çalışan zimmeti' END,
           a."AssignedEmployeeId",
           a."CreatedAt" - ((row_number() OVER (ORDER BY a."AssetTag")::int % 21) * interval '1 day')
    FROM "Assets" a
    WHERE a."CompanyId"=company_id AND a."DeletedAt" IS NULL AND a."AssetTag" LIKE 'SNT-%'
      AND NOT EXISTS (
          SELECT 1 FROM "AssetEvents" e WHERE e."CompanyId"=a."CompanyId" AND e."AssetId"=a."Id"
            AND e."Details" IN ('Santa demo · örnek başlangıç kaydı','Santa demo · örnek çalışan zimmeti')
      );

    SELECT "Id" INTO supplier_id FROM "SupplierCatalogEntries"
      WHERE "CompanyId"=company_id AND "NormalizedName"='PENTA TEKNOLOJİ' AND "IsActive" LIMIT 1;
    SELECT "Id" INTO accessory_id FROM "Accessories"
      WHERE "CompanyId"=company_id AND "ItemNumber"='SNT-ACC-002';
    IF accessory_id IS NULL THEN
        RAISE EXCEPTION 'Santa demo accessory SNT-ACC-002 is not seeded';
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
