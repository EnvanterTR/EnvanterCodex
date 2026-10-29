# Tenant media boundary inventory

Evidence date:29 October2026. This inventory tracks Hedef6.4/6.5; it does not mark those requirements or fullP1 complete.

## HTTP media providers

| Provider | Source | Storage namespace | Current evidence/state |
|---|---|---|---|
| Asset attachments | AssetEndpoints.cs | CompanyId/assets/assetId | CompanyMediaPathGuard; live, focused3/3 |
| License attachments | SoftwareLicenseEndpoints.cs | CompanyId/licenses/licenseId/attachments | CompanyMediaPathGuard; live, focused4/4 |
| Assigned employee EULA | SoftwareLicenseEmployeeEndpoints.cs | Same license attachment namespace | Full-path guard; live in c386113; version/privacy/CSRF/audit regression passed |
| Employee documents | OperationalEndpoints.cs | CompanyId/employees/employeeId/documents | Full-path guard; live, focused3/3 |
| Maintenance attachments | MaintenanceAttachmentEndpoints.cs | CompanyId/maintenances/id/attachments | Full-path guard; live in c386113 |
| Asset order attachments | AssetPurchaseOrderAttachmentEndpoints.cs | CompanyId/asset-purchase-orders/id/attachments | Full-path guard; live in c386113; combined maintenance/order5/5 |
| Contract attachments | AssetContractAttachmentEndpoints.cs | CompanyId/contracts/id/attachments | Full-path guard; live in c386113; content hash retained |
| Model attachments | AssetModelAttachmentEndpoints.cs | CompanyId/asset-models/id/attachments | Full-path guard; live in c386113 |
| Accessory order attachments | AccessoryPurchaseOrderAttachmentEndpoints.cs | CompanyId/accessory-purchase-orders/id/attachments | Full-path guard; live in c386113 |
| Stock order attachments | StockPurchaseOrderAttachmentEndpoints.cs | CompanyId/stock-purchase-orders/id/attachments | Full-path guard; live in c386113; combined last four providers9/9 |
| Brand/supplier logos and asset/accessory-model photos | CatalogImageEndpoints.cs | CompanyId/catalog/kind/id | Five-provider image package68f4d88; focused12/12 including accessory photos; CI40921546389/staging pending |
| Accessory photos | AccessoryPhotoEndpoints.cs | CompanyId/accessories/id/photo | Same68f4d88 candidate; not yet live |
| Company logo | OperationalEndpoints.cs | CompanyId/logo-file | Localf04d7a7; logo form500 fixed; combined company/employee photo6/6 passed; not pushed/deployed |
| Employee photo | OperationalEndpoints.cs | CompanyId/employees/id/profile.ext | Localf04d7a7; all supported profile extensions validated before mutation; same6/6; not pushed/deployed |
| Account profile photo | AccountEndpoints.cs:1302 | accounts/UserId | Account identity namespace, shared across memberships. Unchanged. Needs separate path/security evidence; not counted as a company-prefixed provider or closed requirement. |

The configured root is trusted. The common guard checks each existing component below it, including the final file. This is not a claim of race-proof protection against a privileged host attacker.

## Export, measurement and lifecycle

- DataExportEndpoints.cs:1907 constructs and confines the selected company directory; line1912 rejects a redirected company root. Its iterator at2067 skips reparse directories and final files. Account photos are added separately at1932/1936 with reparse checks. Complete archive-import/export namespace evidence still needs reconciliation with the requirement.
- CompanyMediaUsage.cs:11 rejects a redirected root and its enumeration options skip reparse points. Existing measurement tests remain separate evidence.
- PlatformEndpoints.cs:1656 constructs the selected company directory for the company lifecycle operation. Its complete lifecycle/restore evidence must remain part of the audit; directory construction alone does not prove the operation safe.

## Cache and queues

Source search for IMemoryCache, IDistributedCache, ConcurrentDictionary, Channel, AddMemoryCache and AddStackExchangeRedisCache found no implementations in src C# files. This is limited source-search evidence, not proof that no external service/cache exists in deployment.

Three Queue<Guid> uses were located: AssetEndpoints.cs:1324 and CompanyLocationAccess.cs:50/121. They are method-local hierarchy traversals. The inspected input queries in those methods filter by CompanyId. They are not shared background-job keys. Future shared cache/queue additions require their own tenant-prefix checks.

## Remaining completion evidence

Account identity media, full export/restore/lifecycle coverage, all-table audit fields, runtime services and real customer isolation acceptance remain open. A green test for one provider is not used to close the others.
