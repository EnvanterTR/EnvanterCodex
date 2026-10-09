-- One-time, additive local demo seed. Keeps every existing tenant untouched.
-- Run with: psql -v owner_user_id=<existing-owner-guid> -f tools/seed-santa-demo.sql
\set ON_ERROR_STOP on
BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "Companies" WHERE lower("Name") = 'santa') THEN
        RAISE EXCEPTION 'Santa demo tenant already exists; seed stopped without changes.';
    END IF;
END $$;

CREATE TEMP TABLE _santa ("Id" uuid PRIMARY KEY) ON COMMIT DROP;
WITH created AS (
    INSERT INTO "Companies" ("Id", "Name", "Slug", "ContactEmail", "BrandPrimaryColor", "IsActive", "CreatedAt")
    VALUES (gen_random_uuid(), 'Santa', 'santa-demo', 'ornek@santa.example.test', '#1769E0', TRUE, now())
    RETURNING "Id"
)
INSERT INTO _santa SELECT "Id" FROM created;

CREATE TEMP TABLE _loc ("Key" text PRIMARY KEY, "Id" uuid NOT NULL DEFAULT gen_random_uuid(), "Name" text NOT NULL, "ParentKey" text NULL) ON COMMIT DROP;
INSERT INTO _loc ("Key", "Name", "ParentKey") VALUES
('building','Merkez Bina',NULL),
('floor1','Kat 1','building'),('floor2','Kat 2','building'),('floor3','Kat 3','building'),
('server','Sunucu Odası','floor1'),('rack1','Kabin 1','server'),('rack2','Kabin 2','server'),
('itoffice','Bilgi İşlem Ofisi','floor1'),('store','Bilgi İşlem Deposu','floor1'),
('office2','Açık Ofis','floor2'),('meeting','Toplantı Odası','floor2'),
('office3','Açık Ofis','floor3'),('archive','Finans Arşivi','floor3');
INSERT INTO "CompanyLocations" ("Id", "CompanyId", "ParentLocationId", "Name", "NormalizedName", "IsDefault", "IsActive", "CreatedAt", "UpdatedAt")
SELECT l."Id", s."Id", p."Id", l."Name", upper(l."Name"), l."Key"='building', TRUE, now(), now()
FROM _loc l CROSS JOIN _santa s LEFT JOIN _loc p ON p."Key"=l."ParentKey";

CREATE TEMP TABLE _dept ("Key" text PRIMARY KEY, "Id" uuid NOT NULL DEFAULT gen_random_uuid(), "Name" text NOT NULL, "ParentKey" text NULL, "FloorKey" text NOT NULL) ON COMMIT DROP;
INSERT INTO _dept ("Key","Name","ParentKey","FloorKey") VALUES
('it','Bilgi Teknolojileri',NULL,'floor1'),('it-dev','Yazılım','it','floor1'),('it-sys','Sistem ve Ağ','it','floor1'),
('finance','Muhasebe',NULL,'floor3'),('finance-ops','Finans','finance','floor3'),('finance-ap','Ön Muhasebe','finance','floor3'),
('hr','İnsan Kaynakları',NULL,'floor2'),('hr-recruit','İşe Alım','hr','floor2'),('hr-personnel','Özlük İşleri','hr','floor2'),
('ops','Operasyon',NULL,'floor2'),('ops-logistics','Lojistik','ops','floor2'),('ops-field','Saha Operasyonları','ops','floor2'),
('sales','Satış ve Pazarlama',NULL,'floor2'),('sales-b2b','Kurumsal Satış','sales','floor2'),('sales-marketing','Pazarlama','sales','floor2'),
('admin','İdari İşler',NULL,'floor3'),('admin-buy','Satın Alma','admin','floor3'),('admin-office','Ofis Yönetimi','admin','floor3');
INSERT INTO "CompanyDepartments" ("Id","CompanyId","ParentDepartmentId","LocationCatalogId","Name","NormalizedName","IsActive","CreatedAt","UpdatedAt")
SELECT d."Id", s."Id", p."Id", l."Id", d."Name", upper(d."Name"), TRUE, now(), now()
FROM _dept d CROSS JOIN _santa s JOIN _loc l ON l."Key"=d."FloorKey" LEFT JOIN _dept p ON p."Key"=d."ParentKey";

CREATE TEMP TABLE _emp ("N" int PRIMARY KEY, "Id" uuid NOT NULL DEFAULT gen_random_uuid(), "Name" text NOT NULL, "DeptKey" text NOT NULL, "FloorKey" text NOT NULL, "Title" text NOT NULL) ON COMMIT DROP;
INSERT INTO _emp ("N","Name","DeptKey","FloorKey","Title") VALUES
(1,'Deniz Arslan','it','floor1','Bilgi Teknolojileri Yöneticisi'),(2,'Ece Yıldız','it-dev','floor1','Yazılım Uzmanı'),(3,'Mert Kaya','it-dev','floor1','Uygulama Geliştirici'),(4,'Selin Demir','it-dev','floor1','Veritabanı Uzmanı'),(5,'Can Öztürk','it-sys','floor1','Sistem ve Ağ Uzmanı'),
(6,'Duru Aydın','finance','floor3','Muhasebe Yöneticisi'),(7,'Emre Şahin','finance-ops','floor3','Finans Uzmanı'),(8,'Elif Koç','finance-ops','floor3','Bütçe Analisti'),(9,'Bora Çelik','finance-ap','floor3','Ön Muhasebe Uzmanı'),(10,'Ada Kurt','finance-ap','floor3','Muhasebe Asistanı'),
(11,'Zeynep Aksoy','hr','floor2','İnsan Kaynakları Yöneticisi'),(12,'Arda Polat','hr-recruit','floor2','İşe Alım Uzmanı'),(13,'İrem Yalçın','hr-recruit','floor2','Yetenek Kazanım Uzmanı'),(14,'Eren Güneş','hr-personnel','floor2','Özlük İşleri Uzmanı'),(15,'Lara Bulut','hr-personnel','floor2','İK Asistanı'),
(16,'Kerem Eren','ops','floor2','Operasyon Yöneticisi'),(17,'Sude Kılıç','ops-logistics','floor2','Lojistik Uzmanı'),(18,'Ozan Doğan','ops-logistics','floor2','Depo Sorumlusu'),(19,'Naz Yılmaz','ops-field','floor2','Saha Operasyon Uzmanı'),(20,'Efe Aslan','ops-field','floor2','Saha Koordinatörü'),
(21,'Yasemin Taş','sales','floor2','Satış ve Pazarlama Yöneticisi'),(22,'Ali Ekinci','sales-b2b','floor2','Kurumsal Satış Uzmanı'),(23,'Mina Çetin','sales-b2b','floor2','Müşteri Yöneticisi'),(24,'Kaan Erdem','sales-marketing','floor2','Pazarlama Uzmanı'),(25,'Defne Işık','sales-marketing','floor2','İçerik Uzmanı'),
(26,'Barış Yalın','admin','floor3','İdari İşler Yöneticisi'),(27,'Aslı Toprak','admin-buy','floor3','Satın Alma Uzmanı'),(28,'Eylül Korkmaz','admin-buy','floor3','Tedarik Uzmanı'),(29,'Umut Sezer','admin-office','floor3','Ofis Yöneticisi'),(30,'Pelin Uçar','admin-office','floor3','İdari İşler Asistanı');
INSERT INTO "Employees" ("Id","CompanyId","EmployeeNumber","DisplayName","Email","Department","JobTitle","Phone","Location","LocationCatalogId","StartedOn","ManagerEmployeeId","IsActive","CreatedAt")
SELECT e."Id", s."Id", 'SNT-'||lpad(e."N"::text,3,'0'), e."Name", 'personel'||lpad(e."N"::text,2,'0')||'@santa.example.test', d."Name", e."Title",
       '+90 555 010 '||lpad(e."N"::text,4,'0'), 'Merkez Bina / '||f."Name", f."Id",
       date '2021-01-01' + ((e."N"*19)%1600), CASE WHEN e."N" IN (1,6,11,16,21,26) THEN NULL ELSE m."Id" END, TRUE, now()
FROM _emp e CROSS JOIN _santa s JOIN _dept d ON d."Key"=e."DeptKey" JOIN _loc f ON f."Key"=e."FloorKey"
LEFT JOIN _emp m ON m."N" = CASE WHEN e."N" BETWEEN 2 AND 5 THEN 1 WHEN e."N" BETWEEN 7 AND 10 THEN 6 WHEN e."N" BETWEEN 12 AND 15 THEN 11 WHEN e."N" BETWEEN 17 AND 20 THEN 16 WHEN e."N" BETWEEN 22 AND 25 THEN 21 WHEN e."N" BETWEEN 27 AND 30 THEN 26 ELSE 0 END;
UPDATE "CompanyLocations" SET "ManagerEmployeeId"=(SELECT e."Id" FROM _emp e WHERE e."N"=1), "UpdatedAt"=now()
WHERE "CompanyId"=(SELECT "Id" FROM _santa) AND "Name"='Sunucu Odası';
INSERT INTO "CompanyMemberships" ("CompanyId","UserId","RoleKey","CreatedAt","EmployeeId")
VALUES ((SELECT "Id" FROM _santa), :'owner_user_id'::uuid, 'owner', now(), (SELECT "Id" FROM _emp WHERE "N"=1));

CREATE TEMP TABLE _cat ("Key" text PRIMARY KEY,"Id" uuid NOT NULL DEFAULT gen_random_uuid(),"Name" text NOT NULL) ON COMMIT DROP;
INSERT INTO _cat ("Key","Name") VALUES ('laptop','Dizüstü Bilgisayar'),('desktop','Masaüstü Bilgisayar'),('server','Sunucu'),('switch','Ağ Anahtarı'),('firewall','Güvenlik Duvarı'),('monitor','Monitör'),('printer','Yazıcı'),('phone','IP Telefon'),('ups','Kesintisiz Güç Kaynağı'),('ap','Kablosuz Erişim Noktası');
INSERT INTO "AssetCategories" ("Id","CompanyId","Name") SELECT c."Id",s."Id",c."Name" FROM _cat c CROSS JOIN _santa s;

CREATE TEMP TABLE _brand ("Key" text PRIMARY KEY,"Id" uuid NOT NULL DEFAULT gen_random_uuid(),"Name" text NOT NULL) ON COMMIT DROP;
INSERT INTO _brand ("Key","Name") VALUES ('lenovo','Lenovo'),('dell','Dell'),('hp','HP'),('cisco','Cisco'),('fortinet','Fortinet'),('samsung','Samsung'),('brother','Brother'),('yealink','Yealink'),('eaton','Eaton'),('ubiquiti','Ubiquiti');
INSERT INTO "AssetBrands" ("Id","CompanyId","Name") SELECT b."Id",s."Id",b."Name" FROM _brand b CROSS JOIN _santa s;

CREATE TEMP TABLE _model ("N" int PRIMARY KEY,"Id" uuid NOT NULL DEFAULT gen_random_uuid(),"Name" text NOT NULL,"CatKey" text NOT NULL,"BrandKey" text NOT NULL,"LocationKey" text NOT NULL,"AssignedGroup" int NOT NULL) ON COMMIT DROP;
INSERT INTO _model ("N","Name","CatKey","BrandKey","LocationKey","AssignedGroup") VALUES
(1,'ThinkPad T14 Gen 5','laptop','lenovo','itoffice',1),(2,'Latitude 5450','laptop','dell','office2',1),
(3,'OptiPlex 7020 SFF','desktop','dell','office2',1),(4,'ProDesk 400 G9','desktop','hp','office3',1),
(5,'PowerEdge R760','server','dell','rack1',1),(6,'ThinkSystem SR650 V3','server','lenovo','rack2',1),
(7,'Catalyst 9200L-24T','switch','cisco','rack1',2),(8,'FortiGate 100F','firewall','fortinet','rack1',2),
(9,'ThinkVision P24h-30','monitor','lenovo','itoffice',2),(10,'E24 G5','monitor','hp','office2',2),
(11,'HL-L5210DN','printer','brother','store',3),(12,'T46U','phone','yealink','office3',3),
(13,'5PX 1500i RT2U','ups','eaton','rack2',1),(14,'U6 Pro','ap','ubiquiti','itoffice',2),
(15,'PowerEdge R550','server','dell','rack2',1),(16,'Catalyst 9200L-48P','switch','cisco','rack2',2),
(17,'EliteBook 840 G11','laptop','hp','office3',3),(18,'ThinkCentre M90q Gen 4','desktop','lenovo','itoffice',3),
(19,'MFC-L5710DW','printer','brother','store',3),(20,'T54W','phone','yealink','office2',3);
INSERT INTO "AssetModels" ("Id","CompanyId","BrandId","Name","DepreciationMonths","DepreciationMinimum","DepreciationMinimumType","DepreciationMethod")
SELECT m."Id",s."Id",b."Id",m."Name",60,0,'amount','linear' FROM _model m CROSS JOIN _santa s JOIN _brand b ON b."Key"=m."BrandKey";

CREATE TEMP TABLE _asset_seed ("N" int PRIMARY KEY,"Id" uuid NOT NULL DEFAULT gen_random_uuid(),"ModelN" int NOT NULL,"CopyN" int NOT NULL) ON COMMIT DROP;
INSERT INTO _asset_seed ("N","ModelN","CopyN")
SELECT row_number() OVER (ORDER BY m."N", c."CopyN")::int, m."N", c."CopyN"
FROM _model m CROSS JOIN LATERAL generate_series(1, CASE WHEN m."N" <= 10 THEN 3 ELSE 2 END) AS c("CopyN");
INSERT INTO "Assets" ("Id","CompanyId","AssetTag","Name","CategoryId","BrandId","ModelId","Status","SerialNumber","Location","LocationCatalogId","AssignedEmployeeId","PurchaseDate","PurchaseCost","Currency","CreatedAt","UpdatedAt","IsRequestable","WarrantyExpiresAt","NextAuditOn")
SELECT a."Id",s."Id",'SNT-'||upper(m."CatKey")||'-'||lpad(a."N"::text,3,'0'),m."Name"||' · '||lpad(a."CopyN"::text,2,'0'),
       c."Id",b."Id",m."Id",CASE WHEN a."N"%5=0 THEN 'Deployed' ELSE 'Available' END,
       'SANTA-26-'||lpad(a."N"::text,4,'0'), CASE WHEN m."LocationKey" IN ('rack1','rack2') THEN 'Merkez Bina / Kat 1 / Sunucu Odası / '||CASE WHEN m."LocationKey"='rack1' THEN 'Kabin 1' ELSE 'Kabin 2' END WHEN m."LocationKey" IN ('office1','store') THEN 'Merkez Bina / Kat 1 / '||(SELECT "Name" FROM _loc WHERE "Key"=m."LocationKey") WHEN m."LocationKey"='office2' THEN 'Merkez Bina / Kat 2 / Açık Ofis' ELSE 'Merkez Bina / Kat 3 / Açık Ofis' END,
       l."Id", CASE WHEN a."N"%5=0 THEN (SELECT "Id" FROM _emp WHERE "N"=m."AssignedGroup"+((a."CopyN"-1)%4)+1) ELSE NULL END,
       date '2024-01-01'+(a."N"*7), 18000+(a."N"*1375), 'TRY',now(),now(),TRUE,date '2027-01-01'+(a."N"*11),current_date+((a."N"%20)+1)
FROM _asset_seed a CROSS JOIN _santa s JOIN _model m ON m."N"=a."ModelN" JOIN _cat c ON c."Key"=m."CatKey" JOIN _brand b ON b."Key"=m."BrandKey" JOIN _loc l ON l."Key"=m."LocationKey";
INSERT INTO "AssetAttachments" ("Id","CompanyId","AssetId","UploadedByUserId","OriginalFileName","StorageFileName","ContentType","Length","CreatedAt")
SELECT '9f31a1c1-e39e-4f36-ab12-34cd56ef7890'::uuid, s."Id", a."Id", :'owner_user_id'::uuid,
       'Santa-sunucu-ornek.jpg','9f31a1c1e39e4f36ab1234cd56ef7890.jpg','image/jpeg',29526,now()
FROM _santa s JOIN "Assets" a ON a."CompanyId"=s."Id" AND a."AssetTag"='SNT-SERVER-013';

CREATE TEMP TABLE _accessory_seed ("N" int PRIMARY KEY,"Id" uuid NOT NULL DEFAULT gen_random_uuid(),"Name" text NOT NULL,"Category" text NOT NULL,"Model" text NOT NULL,"Qty" int NOT NULL,"MinQty" int NOT NULL,"Cost" numeric NOT NULL) ON COMMIT DROP;
INSERT INTO _accessory_seed ("N","Name","Category","Model","Qty","MinQty","Cost") VALUES
(1,'Kablosuz Klavye ve Fare Seti','Klavye / Fare','Logitech MK540',18,5,1450),(2,'USB-C Çoklayıcı','Bağlantı Adaptörü','Lenovo 7-in-1',12,4,2100),
(3,'USB-C Dock İstasyonu','Dock','Dell WD19S',8,2,6850),(4,'Dizüstü Bilgisayar Çantası','Çanta','Samsonite GuardIT 2.0',14,4,1850),
(5,'Cat6 Patch Kablo 1 m','Ağ Kablosu','Cat6 UTP 1m',40,10,85),(6,'Cat6 Patch Kablo 3 m','Ağ Kablosu','Cat6 UTP 3m',25,8,140),
(7,'HDMI Kablo 2 m','Görüntü Kablosu','HDMI 2.0 2m',16,5,190),(8,'DisplayPort Kablo 2 m','Görüntü Kablosu','DP 1.4 2m',10,3,260),
(9,'Kablosuz Optik Fare','Fare','Logitech M185',20,6,490),(10,'Yedek Laptop Adaptörü 65 W','Güç Adaptörü','USB-C 65W',9,3,1250);
INSERT INTO "Accessories" ("Id","CompanyId","Name","Category","ModelNumber","ItemNumber","Supplier","Location","LocationCatalogId","MinimumQuantity","TotalQuantity","UnitCost","Currency","PurchaseDate","Notes","IsRequestable","CreatedAt","UpdatedAt")
SELECT a."Id",s."Id",a."Name",a."Category",a."Model",'SNT-ACC-'||lpad(a."N"::text,3,'0'),'Santa Bilgi Teknolojileri Tedarik','Merkez Bina / Kat 1 / Bilgi İşlem Deposu',l."Id",a."MinQty",a."Qty",a."Cost",'TRY',date '2026-01-15','Örnek demo stoğu; miktar düzenlenebilir.',TRUE,now(),now()
FROM _accessory_seed a CROSS JOIN _santa s JOIN _loc l ON l."Key"='store';
UPDATE "Accessories" SET "PhotoFileName"='4c6d0102a36d452ea183013191f04f72.jpg'
WHERE "CompanyId"=(SELECT "Id" FROM _santa) AND "ItemNumber"='SNT-ACC-004';
INSERT INTO "AccessoryMovements" ("Id","CompanyId","AccessoryId","ActorUserId","MovementType","Quantity","Notes","OccurredAt")
SELECT gen_random_uuid(),s."Id",a."Id",:'owner_user_id'::uuid,'Received',a."Qty",'Santa örnek kurulumu · başlangıç stoku',now()
FROM _accessory_seed a CROSS JOIN _santa s;

COMMIT;
