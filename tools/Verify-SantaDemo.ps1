param(
    [string]$ComposeFile = (Join-Path (Split-Path $PSScriptRoot -Parent) 'deploy/local/compose.yaml')
)

$ErrorActionPreference = 'Stop'
$composePath = (Resolve-Path -LiteralPath $ComposeFile).Path

$query = @'
SELECT c."Id",
 (SELECT count(*) FROM "Employees" e WHERE e."CompanyId"=c."Id" AND e."IsActive"),
 (SELECT count(*) FROM "Assets" a WHERE a."CompanyId"=c."Id" AND a."DeletedAt" IS NULL),
 (SELECT count(DISTINCT a."CategoryId") FROM "Assets" a WHERE a."CompanyId"=c."Id" AND a."DeletedAt" IS NULL),
 (SELECT count(DISTINCT a."ModelId") FROM "Assets" a WHERE a."CompanyId"=c."Id" AND a."DeletedAt" IS NULL AND a."ModelId" IS NOT NULL),
 (SELECT count(*) FROM "AssetModels" m WHERE m."CompanyId"=c."Id" AND m."PhotoFileName" IS NOT NULL),
 (SELECT count(*) FROM "Accessories" a WHERE a."CompanyId"=c."Id"),
 (SELECT coalesce(sum(a."TotalQuantity"),0) FROM "Accessories" a WHERE a."CompanyId"=c."Id"),
 (SELECT count(*) FROM "AssetBrands" b WHERE b."CompanyId"=c."Id" AND b."LogoFileName" IS NOT NULL),
 (SELECT count(*) FROM "AccessoryModels" m WHERE m."CompanyId"=c."Id" AND m."PhotoFileName" IS NOT NULL),
 (SELECT count(*) FROM "SupplierCatalogEntries" s WHERE s."CompanyId"=c."Id" AND s."LogoFileName" IS NOT NULL),
 (SELECT count(*) FROM "AssetEvents" e WHERE e."CompanyId"=c."Id" AND e."Details" LIKE 'Santa demo%'),
 (SELECT count(*) FROM "Assets" a JOIN "AssetModels" m ON m."CompanyId"=a."CompanyId" AND m."Id"=a."ModelId"
  WHERE a."CompanyId"=c."Id" AND a."DeletedAt" IS NULL AND a."PurchaseDate" IS NOT NULL
    AND a."PurchaseCost" IS NOT NULL AND m."DepreciationMonths" IS NOT NULL),
 (SELECT coalesce(sum(l."Quantity"-l."QuantityReceived"),0) FROM "AccessoryPurchaseOrders" p
  JOIN "AccessoryPurchaseOrderLines" l ON l."CompanyId"=p."CompanyId" AND l."PurchaseOrderId"=p."Id"
  WHERE p."CompanyId"=c."Id" AND p."OrderNumber"='SNT-DEMO-ACC-PO-001' AND p."Status"<>'Cancelled')
FROM "Companies" c WHERE c."Slug"='santa-demo';
'@

$recordOutput = $query | & docker compose -f $composePath exec -T database psql -U envantertr -d envantertr -v ON_ERROR_STOP=1 -t -A -F '|' -f - 2>&1
if ($LASTEXITCODE -ne 0) { throw "Could not read the Santa demo records: $($recordOutput -join ' ')" }
$recordLine = @($recordOutput | Where-Object { $_ -match '^[0-9a-fA-F-]{36}\|' } | Select-Object -Last 1)
if ($recordLine.Count -ne 1) { throw 'The Santa demo company is missing or the company query returned an unexpected result.' }
$fields = $recordLine[0].Split('|')
if ($fields.Count -ne 14) { throw 'The Santa demo database returned an unexpected number of count fields.' }
$companyKey = $fields[0].Replace('-', '')
if ($companyKey -notmatch '^[0-9a-fA-F]{32}$') { throw 'The Santa demo company identifier is invalid.' }

$mediaScript = @'
root=/var/lib/envantertr/company-media/__COMPANY__
printf 'employee_photos='; find "$root/employees" -mindepth 2 -maxdepth 2 -type f -name 'profile.*' 2>/dev/null | wc -l
printf 'asset_model_photos='; find "$root/catalog/models" -mindepth 2 -maxdepth 2 -type f 2>/dev/null | wc -l
printf 'brand_logos='; find "$root/catalog/brands" -mindepth 2 -maxdepth 2 -type f 2>/dev/null | wc -l
printf 'accessory_model_photos='; find "$root/catalog/accessory-models" -mindepth 2 -maxdepth 2 -type f 2>/dev/null | wc -l
printf 'supplier_logos='; find "$root/catalog/suppliers" -mindepth 2 -maxdepth 2 -type f 2>/dev/null | wc -l
'@
$mediaScript = $mediaScript.Replace('__COMPANY__', $companyKey)
$mediaOutput = & docker compose -f $composePath exec -T web sh -c $mediaScript 2>&1
if ($LASTEXITCODE -ne 0) { throw "Could not read the Santa demo media store: $($mediaOutput -join ' ')" }
$mediaCounts = @{}
foreach ($line in $mediaOutput) {
    if ($line -match '^([a-z_]+)=(\d+)\s*$') { $mediaCounts[$Matches[1]] = [int]$Matches[2] }
}

$actual = [ordered]@{
    ActiveEmployees = [int]$fields[1]
    ActiveAssets = [int]$fields[2]
    AssetCategories = [int]$fields[3]
    UsedAssetModels = [int]$fields[4]
    ModelsWithPhotos = [int]$fields[5]
    AccessoryRecords = [int]$fields[6]
    AccessoryUnits = [int]$fields[7]
    BrandsWithLogos = [int]$fields[8]
    AccessoryModelsWithPhotos = [int]$fields[9]
    SuppliersWithLogos = [int]$fields[10]
    DemoActivityEntries = [int]$fields[11]
    DepreciationEligibleAssets = [int]$fields[12]
    PendingAccessoryDeliveryUnits = [int]$fields[13]
    EmployeePhotoFiles = $mediaCounts['employee_photos']
    AssetModelPhotoFiles = $mediaCounts['asset_model_photos']
    BrandLogoFiles = $mediaCounts['brand_logos']
    AccessoryModelPhotoFiles = $mediaCounts['accessory_model_photos']
    SupplierLogoFiles = $mediaCounts['supplier_logos']
}
$expected = @{
    ActiveEmployees = 30; ActiveAssets = 50; AssetCategories = 10; UsedAssetModels = 20
    ModelsWithPhotos = 20; AccessoryRecords = 10; AccessoryUnits = 172; BrandsWithLogos = 12
    AccessoryModelsWithPhotos = 10; SuppliersWithLogos = 8; DemoActivityEntries = 50
    DepreciationEligibleAssets = 50; PendingAccessoryDeliveryUnits = 9; EmployeePhotoFiles = 30
    BrandLogoFiles = 12; SupplierLogoFiles = 8
}
$minimum = @{ AssetModelPhotoFiles = 20; AccessoryModelPhotoFiles = 10 }
foreach ($name in $expected.Keys) {
    if ($actual[$name] -ne $expected[$name]) { throw "$name mismatch: expected $($expected[$name]), found $($actual[$name])." }
}
foreach ($name in $minimum.Keys) {
    if ($actual[$name] -lt $minimum[$name]) { throw "$name below minimum: expected at least $($minimum[$name]), found $($actual[$name])." }
}

[pscustomobject]$actual | Format-List
Write-Output 'Santa demo records and linked media verified.'
