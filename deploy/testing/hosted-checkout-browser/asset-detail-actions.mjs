import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const secondCompanyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab';
const userId = '22222222-2222-4222-8222-222222222222';
const assetId = '33333333-3333-4333-8333-333333333333';
const receivedAssetId = 'ab300000-0000-4000-8000-000000000001';
const hierarchyParentId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const hierarchyChildId = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';
const foreignParentId = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
const employeeId = '99999999-9999-4999-8999-999999999999';
const modelId = '66666666-6666-4666-8666-666666666666';
const brandId = '55555555-5555-4555-8555-555555555556';
const bulkEmployeeTwoId = '88888888-8888-4888-8888-888888888882';
const bulkEmployeeThreeId = '77777777-7777-4777-8777-777777777773';
const companyUnitRootId = '12121212-1212-4121-8121-121212121212';
const companyUnitBranchId = '13131313-1313-4131-8131-131313131313';
let companyUnitItems = [];
const companyUnitWrites = [];
const memberAccessTestUserId = '14141414-1414-4141-8141-141414141414';
let memberAccessTestEnabled = false;
let memberAccessState = { mode: 'all', locationIds: [], companyUnitMode: 'all', companyUnitIds: [] };
const memberAccessWrites = [];
const rolePermissionLabels = {
  'assets.read': 'Varlıkları görüntüleme', 'assets.write': 'Varlık ve stok kayıtlarını değiştirme',
  'people.read': 'Çalışan kayıtlarını görüntüleme', 'people.manage': 'Çalışan kayıtlarını yönetme',
  'people.documents': 'Çalışan belgelerine erişme', 'company.settings': 'Şirket kullanıcılarını, ayarlarını ve denetim kayıtlarını yönetme'
};
const companyRoleRows = [
  { roleKey: 'owner', name: 'Şirket sahibi', permissionKeys: Object.keys(rolePermissionLabels), builtIn: true, isActive: true },
  { roleKey: 'admin', name: 'Yönetici', permissionKeys: Object.keys(rolePermissionLabels), builtIn: true, isActive: true },
  { roleKey: 'asset_manager', name: 'Envanter yöneticisi', permissionKeys: ['assets.read', 'assets.write', 'people.read', 'people.manage'], builtIn: true, isActive: true },
  { roleKey: 'asset_operator', name: 'Envanter operatörü', permissionKeys: ['assets.read', 'assets.write', 'people.read'], builtIn: true, isActive: true },
  { roleKey: 'auditor', name: 'Denetçi', permissionKeys: ['assets.read', 'people.read'], builtIn: true, isActive: true },
  { roleKey: 'viewer', name: 'Salt okunur', permissionKeys: ['assets.read'], builtIn: true, isActive: true },
  { roleKey: 'hr_operator', name: 'İK operatörü', permissionKeys: ['people.read', 'people.manage', 'people.documents'], builtIn: true, isActive: true }
];
const statusBuiltIns = [
  { status: 'Available', label: 'Kullanılabilir', defaultLabel: 'Kullanılabilir', color: '#20A779', systemManaged: false },
  { status: 'Deployed', label: 'Atanmış', defaultLabel: 'Atanmış', color: '#3478F6', systemManaged: true },
  { status: 'Pending', label: 'Beklemede', defaultLabel: 'Beklemede', color: '#D99A22', systemManaged: false },
  { status: 'Maintenance', label: 'Bakımda', defaultLabel: 'Bakımda', color: '#D97706', systemManaged: false },
  { status: 'Unusable', label: 'Kullanılamaz', defaultLabel: 'Kullanılamaz', color: '#D64E5A', systemManaged: false },
  { status: 'Archived', label: 'Arşiv', defaultLabel: 'Arşiv', color: '#68758A', systemManaged: true }
];
const statusManualBuiltIns = ['Available', 'Pending', 'Maintenance', 'Unusable'];
const statusConfigurationWrites = [];
const statusLabelWrites = [];
let customAssetStatuses = [];
let assetStatusTransitions = statusManualBuiltIns.flatMap(from => statusManualBuiltIns.filter(to => to !== from).map(to => ({ from, to })));
const getAssetStatusLabels = () => [...statusBuiltIns.map(item => ({ ...item, custom: false })),
  ...customAssetStatuses.map(item => ({ ...item, defaultLabel: item.label, custom: true }))];
const getAssetStatusConfiguration = () => ({ builtInStatuses: statusBuiltIns.map(item => ({ ...item })),
  customStatuses: customAssetStatuses.map(item => ({ ...item })), transitions: assetStatusTransitions.map(item => ({ ...item })) });
const companyRoleWrites = [];
let customRoleSequence = 0;
const asset = {
  id: assetId, companyId, companyName: 'Synthetic Asset Company', assetTag: 'QA-ASSET-001',
  name: 'Synthetic audit target', status: 'Available', serialNumber: 'QA-SERIAL-001', companyUnitName: 'Synthetic QA Unit',
  categoryId: '55555555-5555-4555-8555-555555555555', brandId, modelId, parentAssetId: null, location: 'Head Office', updatedAt: '2026-10-14T12:00:00Z', nextAuditOn: null,
  createdAt: '2026-10-01T12:00:00Z', purchaseDate: '2025-01-15', warrantyExpiresAt: '2027-01-15',
  insuranceProvider: 'Synthetic Insurer', insurancePolicyNumber: 'QA-POLICY-001', insuranceExpiresAt: '2028-12-31',
  purchaseCost: 2500, currency: 'TRY', isRequestable: true, isByod: false,
  customFields: { inventory_reference: 'QA-SOURCE-REF', secret_key: 'source-sensitive-value' }
};
const installedSoftwareRows = [{ name: 'Synthetic Inventory Agent', publisher: 'EnvanterTR QA', version: '1.2.3', architecture: 'x64', installDate: '2026-10-01', lastSeenAt: '2026-10-19T12:00:00Z' }];
let assetValuation = { ageMonths: 21, depreciationMonths: 60, remainingMonths: 39, currentValue: 1625 };
const hierarchyParent = { id: hierarchyParentId, companyId, assetTag: 'QA-PARENT-001', name: 'Synthetic QA Parent', status: 'Available' };
const hierarchyChild = { id: hierarchyChildId, companyId, assetTag: 'QA-CHILD-001', name: 'Synthetic QA Child', status: 'Available', parentAssetId: assetId };
const assetAccessoryRelation = { id: '12121212-aaaa-4aaa-8aaa-121212121212', accessoryId: '34343434-aaaa-4aaa-8aaa-343434343434', accessoryName: 'Synthetic open headset', quantity: 3, returnedQuantity: 1, remaining: 2, checkedOutAt: '2026-10-10T10:00:00Z', notes: 'Headset and adapter' };
const assetComponentRelation = { id: '13131313-aaaa-4aaa-8aaa-131313131313', componentId: '45454545-aaaa-4aaa-8aaa-454545454545', componentName: 'Synthetic installed memory', quantity: 5, removedQuantity: 2, remaining: 3, allocatedAt: '2026-10-11T10:00:00Z', notes: 'Slot A1/A2' };
const printAssetRows = Array.from({ length: 205 }, (_, index) => ({
  ...asset,
  id: `f1000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
  assetTag: `QA-PRNT-${String(index + 1).padStart(3, '0')}`,
  name: `Synthetic prnt asset ${index + 1}`,
  serialNumber: `QA-PRNT-SERIAL-${index + 1}`
}));
const importedAssets = [];
const clonedAssets = [];
const assetCreateWrites = [];
const assetCustomFieldDefinitions = [
  { key: 'inventory_reference', name: 'Envanter referansı', groupName: 'Genel', fieldType: 'text', isActive: true, isRequired: false, options: [] },
  { key: 'secret_key', name: 'Gizli anahtar', groupName: 'Genel', fieldType: 'sensitive', isActive: true, isRequired: false, options: [] },
  { key: 'model_secret', name: 'Model güvenlik kodu', groupName: 'Güvenlik', categoryId: asset.categoryId, modelId,
    fieldType: 'sensitive', isActive: true, isRequired: false, options: [] }
];
let failNextAssetCreate = true;
let bulkAssets = [];
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const requests = [];
const apiRequests = [];
const reportCategoryId = '56565656-5656-4565-8565-565656565656';
const purchaseReportItems = Array.from({ length: 51 }, (_, index) => ({
  id: `67676767-6767-4676-8676-${String(index + 1).padStart(12, '0')}`,
  assetTag: `QA-REPORT-${String(index + 1).padStart(3, '0')}`,
  name: `Synthetic report asset ${index + 1}`,
  serialNumber: `QA-REPORT-SERIAL-${index + 1}`,
  purchaseDate: `2026-10-${String((index % 28) + 1).padStart(2, '0')}`,
  purchaseCost: 2500 + index,
  currency: index % 2 ? 'USD' : 'TRY',
  status: index % 3 ? 'Available' : 'Deployed',
  location: 'Synthetic report depot',
  category: 'Synthetic report category'
}));
const auditReportItems = Array.from({ length: 26 }, (_, index) => ({
  id: `a4000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
  assetId, occurredAt: `2026-10-${String(index + 1).padStart(2, '0')}T12:00:00Z`,
  assetTag: `QA-AUDIT-${String(index + 1).padStart(3, '0')}`, assetName: `Synthetic audit asset ${index + 1}`,
  location: 'QA Depot', actorName: 'QA Auditor', details: `Synthetic physical audit ${index + 1}`,
  outcome: index % 5 === 0 ? 'Unacceptable' : 'Accepted'
}));
const maintenanceReportItems = [
  { status: 'InProgress', startedOn: '2026-10-10', maintenanceType: 'Preventive', cost: 1200, currency: 'TRY' },
  { status: 'Completed', startedOn: '2026-10-11', maintenanceType: 'Repair', cost: 50, currency: 'USD' },
  { status: 'Completed', startedOn: '2026-10-12', maintenanceType: 'Repair', cost: 800, currency: 'TRY' },
  { status: 'Planned', startedOn: '2026-10-12', maintenanceType: 'Inspection', cost: 300, currency: 'TRY' }
];
const licenseReportToday = new Date();
licenseReportToday.setUTCHours(0, 0, 0, 0);
const licenseReportDay = offset => new Date(licenseReportToday.getTime() + offset * 86400000).toISOString().slice(0, 10);
const licenseExpiryReportItems = [
  { id: 'e1000000-0000-4000-8000-000000000001', name: 'QA-LICENSE-EXPIRED', publisher: 'QA Publisher', productVersion: '1.0', licenseType: 'Subscription', supplier: 'QA Supplier', seats: 5, expirationDate: licenseReportDay(-1), autoRenew: false, assigned: 2 },
  { id: 'e1000000-0000-4000-8000-000000000002', name: 'QA-LICENSE-UPCOMING', publisher: 'QA Publisher', productVersion: '2.0', licenseType: 'Perpetual', supplier: 'QA Supplier', seats: 8, expirationDate: licenseReportDay(10), autoRenew: true, assigned: 3 },
  { id: 'e1000000-0000-4000-8000-000000000003', name: 'QA-LICENSE-LATER', publisher: 'QA Publisher', productVersion: '3.0', licenseType: 'Device', supplier: 'QA Supplier', seats: 4, expirationDate: licenseReportDay(180), autoRenew: false, assigned: 0 },
  { id: 'e1000000-0000-4000-8000-000000000004', name: 'QA-LICENSE-MISSING', publisher: 'QA Publisher', productVersion: '4.0', licenseType: 'Other', supplier: 'QA Supplier', seats: 2, expirationDate: null, autoRenew: false, assigned: 0 },
  ...Array.from({ length: 51 }, (_, index) => ({ id: `e2000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
    name: `QA-LICENSE-PAGE-${String(index + 1).padStart(3, '0')}`, publisher: 'QA Publisher', productVersion: '5.0', licenseType: 'Site', supplier: 'QA Supplier',
    seats: 10, expirationDate: licenseReportDay(180), autoRenew: index % 2 === 0, assigned: index % 4 }))
];
const pendingDeliveryReportItems = [
  { orderType: 'Varlık', orderId: 'd1000000-0000-4000-8000-000000000001', orderNumber: 'QA-DELIVERY-OVERDUE', supplier: 'QA Supplier', status: 'PartiallyReceived', orderDate: '2026-09-01', expectedOn: '2026-10-01', currency: 'TRY', itemName: 'Synthetic overdue device', quantity: 5, quantityReceived: 2, remaining: 3, unitCost: 100 },
  { orderType: 'Aksesuar', orderId: 'd1000000-0000-4000-8000-000000000002', orderNumber: 'QA-DELIVERY-DUE30', supplier: 'QA Supplier', status: 'Ordered', orderDate: '2026-10-01', expectedOn: '2026-10-26', currency: 'TRY', itemName: 'Synthetic due accessory', quantity: 4, quantityReceived: 0, remaining: 4, unitCost: 25 },
  { orderType: 'Sarf', orderId: 'd1000000-0000-4000-8000-000000000003', orderNumber: 'QA-DELIVERY-UNDATED', supplier: 'QA Other Supplier', status: 'Ordered', orderDate: '2026-10-02', expectedOn: null, currency: 'USD', itemName: 'Synthetic undated consumable', quantity: 6, quantityReceived: 1, remaining: 5, unitCost: 8 },
  ...Array.from({ length: 51 }, (_, index) => ({
    orderType: index % 2 ? 'Bileşen' : 'Varlık', orderId: `d2000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
    orderNumber: `QA-DELIVERY-PAGE-${String(index + 1).padStart(3, '0')}`, supplier: 'QA Paging Supplier', status: 'Ordered',
    orderDate: '2026-10-01', expectedOn: '2027-02-20', currency: 'TRY', itemName: `Synthetic pending item ${index + 1}`,
    quantity: 10, quantityReceived: 0, remaining: 10, unitCost: 12
  }))
];
const depreciationReportItems = [
  { id: 'd3000000-0000-4000-8000-000000000001', assetTag: 'QA-DEPR-LINEAR', name: 'Synthetic depreciating asset', serialNumber: 'QA-DEPR-SERIAL', category: 'QA Hardware', model: 'QA Linear Model', location: 'QA Depot', status: 'Available', purchaseDate: '2025-10-16', endOfLife: '2030-10-16', purchaseCost: 12000, currency: 'TRY', monthlyDepreciation: 200, accumulatedDepreciation: 2400, bookValue: 9600, remainingMonths: 48, method: 'linear' },
  ...Array.from({ length: 51 }, (_, index) => ({
    id: `d4000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`, assetTag: `QA-DEPR-PAGE-${String(index + 1).padStart(3, '0')}`,
    name: `Synthetic depreciation asset ${index + 1}`, serialNumber: `QA-DEPR-PAGE-SERIAL-${index + 1}`, category: 'QA Hardware',
    model: 'QA Paging Model', location: 'QA Depot', status: 'Available', purchaseDate: '2026-01-01', endOfLife: '2031-01-01',
    purchaseCost: 6000, currency: index % 2 ? 'USD' : 'TRY', monthlyDepreciation: 100, accumulatedDepreciation: 900,
    bookValue: 5100, remainingMonths: 51, method: index % 2 ? 'half_1' : 'linear'
  }))
];
const customAssetReportPresets = [];
let customAssetReportSequence = 0;
const legacyLocationPath = 'QA Building / QA Floor / QA Room';
const locationPromotionWrites = [];
const locationCatalogWrites = [];
const locationDefaultWrites = [];
const locationManagerWrites = [];
let locationCatalogSequence = 0;
let locationDirectoryRows = [];
const reservationWrites = [];
const reservationAssets = [
  { id: 'a1000000-0000-4000-8000-000000000001', assetTag: 'QA-RES-001', name: 'Synthetic reservation device 1', location: 'Head Office' },
  { id: 'a1000000-0000-4000-8000-000000000002', assetTag: 'QA-RES-002', name: 'Synthetic reservation device 2', location: 'Head Office' },
  { id: 'a1000000-0000-4000-8000-000000000003', assetTag: 'QA-RES-003', name: 'Synthetic reservation device 3', location: 'Head Office' }
];
const reservationRows = [
  { id: 'a2000000-0000-4000-8000-000000000001', assetId: reservationAssets[0].id, assetTag: reservationAssets[0].assetTag, assetName: reservationAssets[0].name, employeeId, employeeName: 'Synthetic QA Employee', requestedByUserId: userId, requestedBy: 'Synthetic Owner', startsOn: '2026-10-22', endsOn: '2026-10-23', status: 'Pending', requestNote: 'Approve synthetic reservation', reviewNote: null },
  { id: 'a2000000-0000-4000-8000-000000000002', assetId: reservationAssets[1].id, assetTag: reservationAssets[1].assetTag, assetName: reservationAssets[1].name, employeeId, employeeName: 'Synthetic QA Employee', requestedByUserId: userId, requestedBy: 'Synthetic Owner', startsOn: '2026-10-24', endsOn: '2026-10-25', status: 'Pending', requestNote: 'Reject synthetic reservation', reviewNote: null }
];
const activityEntries = Array.from({ length: 14 }, (_, index) => ({
  id: `a3000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
  assetId, assetTag: index === 0 ? 'QA-TRANSFER-001' : `QA-ACTIVITY-${String(index + 1).padStart(3, '0')}`,
  eventType: index === 0 ? 'Transferred' : 'Created', details: index === 0 ? 'Transferred to Synthetic West Wing' : `Synthetic activity event ${index + 1}`,
  employeeId: index === 0 ? employeeId : null, employeeName: index === 0 ? 'Synthetic QA Employee' : null,
  actorUserId: userId, actorName: 'Synthetic Owner', occurredAt: `2026-10-15T${String(14 - Math.floor(index / 6)).padStart(2, '0')}:${String(55 - (index % 6) * 8).padStart(2, '0')}:00Z`
}));
const expiryLoginAttempts = [];
let syntheticSessionExpired = false;
let companySwitchLoginFlow = false;
let activeCompanyId = companyId;
let expireNextAssetPatch = false;
let failNextBulkAssetScan = true;
let failNextActivityLoad = false;
let syntheticNotificationPreferences = { enabledKinds: ['maintenance', 'license'], emailEnabledKinds: ['license'] };
const notificationPreferenceWrites = [];
const assetHistory = [];
const assetNoteWrites = [];
const assetAttachments = [];
const attachmentId = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
const attachmentBytes = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/mMcAAAAASUVORK5CYII=', 'base64');
const maintenanceRows = [];
const maintenanceId = '77777777-7777-4777-8777-777777777777';
const restoreArchive = { formatVersion: 1, company: { name: 'Synthetic Restore Source' }, assets: [], employees: [] };
const restoreApplyWrites = [];
const sessionRevocationWrites = [];
const syntheticDeviceSessions = [
  { id: '44444444-4444-4444-8444-444444444444', current: true, ipAddress: '127.0.0.1', userAgent: 'Synthetic Chromium', lastSeenAt: new Date().toISOString() },
  { id: '55555555-5555-4555-8555-555555555555', current: false, ipAddress: '192.0.2.23', userAgent: 'Synthetic Firefox', lastSeenAt: new Date().toISOString() }
];
const contractId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const contractAssetId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const contractLicenseId = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';
const contractRows = [{ id: contractId, name: 'Synthetic Cloud Support Agreement', contractNumber: 'QA-CONTRACT-001', contractType: 'Support', supplier: 'Synthetic Supplier', supplierId: null, startDate: '2026-10-01', endDate: '2027-10-01', cost: 1250, currency: 'TRY', notes: 'Synthetic contract note', isActive: true, status: 'active', assetCount: 1, licenseCount: 1, attachmentCount: 0 }];
const contractWrites = [];
const softwareLicenseId = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
const softwareLicenseAttachmentId = 'ffffffff-ffff-4fff-8fff-ffffffffffff';
const softwareLicenseRows = [{
  id: softwareLicenseId, name: 'Synthetic License Import Baseline', publisher: 'Synthetic Publisher', productVersion: '1',
  licenseType: 'Subscription', licenseReference: 'QA-LICENSE-BASELINE', supplier: 'Synthetic Supplier', seats: 3, assigned: 0,
  unitCost: null, currency: 'TRY', purchaseDate: null, expirationDate: null, autoRenew: false, requiresEulaAcceptance: false,
  notes: null, isArchived: false
}];
let softwareLicenseCurrentEula = null;
const softwareLicenseAssignments = [];
const softwareLicenseAssignmentWrites = [];
let includeBulkLicenseEmployees = false;
const softwareLicenseAttachments = [{ id: softwareLicenseAttachmentId, originalFileName: 'synthetic-eula.pdf', contentType: 'application/pdf',
  length: 1024, createdAt: '2026-10-14T12:00:00Z', uploadedBy: 'Synthetic Owner', isEulaDocument: false }];
const softwareLicenseWrites = [];
const softwareLicenseCrudWrites = [];
const softwareLicensePreviewRequests = [];
const softwareLicenseImportAttempts = [];
let failNextSoftwareLicenseImport = true;
const assetImportProfiles = [];
const assetImportProfileWrites = [];
const assetImportProfileDeleteWrites = [];
const assetImportPreviewRequests = [];
const assetImportApplicationWrites = [];
const contractAsset = { id: contractAssetId, assetTag: 'QA-CONTRACT-ASSET', name: 'Synthetic Contract Device', serialNumber: 'QA-CONTRACT-SERIAL' };
const contractLicense = { id: contractLicenseId, name: 'Synthetic Office License', licenseReference: 'QA-LICENSE-001', expirationDate: '2027-10-01' };
const unifiedPurchaseAssetId = 'ac000000-0000-4000-8000-000000000001';
const unifiedPurchaseAccessoryId = 'ac000000-0000-4000-8000-000000000002';
const unifiedPurchaseStockId = 'ac000000-0000-4000-8000-000000000003';
const unifiedPurchaseLicenseOrderId = 'ac000000-0000-4000-8000-000000000004';
const supplierAssetLineId = 'ac100000-0000-4000-8000-000000000001';
const supplierAssetReceivedId = 'ac200000-0000-4000-8000-000000000001';
const supplierAccessoryLineId = 'ac300000-0000-4000-8000-000000000001';
const supplierStockConsumableLineId = 'ac400000-0000-4000-8000-000000000001';
const supplierStockComponentLineId = 'ac400000-0000-4000-8000-000000000002';
const supplierReturnWrites = [];
const supplierReturnHistory = { asset: [], accessory: [], stock: [] };
const unifiedPurchaseRows = [
  { id: unifiedPurchaseAssetId, type: 'asset', typeLabel: 'Varlık', orderNumber: 'UPO-ASSET-001', supplier: 'Synthetic Alpha', status: 'Ordered', statusLabel: 'Sipariş edildi', orderDate: '2026-10-16', expectedOn: '2026-10-30', currency: 'TRY', itemCount: 1, quantity: 4, quantityReceived: 1, total: 400 },
  { id: 'ac000000-0000-4000-8000-000000000002', type: 'accessory', typeLabel: 'Aksesuar', orderNumber: 'UPO-ACCESSORY-001', supplier: 'Synthetic Beta', status: 'PartiallyReceived', statusLabel: 'Kısmi teslim', orderDate: '2026-10-15', expectedOn: null, currency: 'USD', itemCount: 2, quantity: 5, quantityReceived: 2, total: 75 },
  { id: 'ac000000-0000-4000-8000-000000000003', type: 'stock', typeLabel: 'Sarf / bileşen', orderNumber: 'UPO-STOCK-001', supplier: 'Synthetic Gamma', status: 'Received', statusLabel: 'Tamamlandı', orderDate: '2026-10-14', expectedOn: '2026-10-20', currency: 'EUR', itemCount: 3, quantity: 8, quantityReceived: 8, total: 48 },
  { id: unifiedPurchaseLicenseOrderId, relatedId: softwareLicenseId, type: 'license', typeLabel: 'Yazılım lisansı', orderNumber: 'UPO-LICENSE-001', supplier: 'Synthetic License Supplier', status: 'Recorded', statusLabel: 'Kaydedildi', orderDate: '2026-10-17', expectedOn: null, currency: 'USD', itemCount: 1, quantity: 5, quantityReceived: null, remainingQuantity: null, subtotal: null, discountAmount: null, taxAmount: null, freightAmount: null, total: null },
  ...Array.from({ length: 24 }, (_, index) => ({ id: `ac000000-0000-4000-8000-${String(index + 10).padStart(12, '0')}`, type: 'asset', typeLabel: 'Varlık', orderNumber: `UPO-ASSET-${String(index + 2).padStart(3, '0')}`, supplier: `Synthetic Supplier ${index + 2}`, status: 'Ordered', statusLabel: 'Sipariş edildi', orderDate: `2026-10-${String(13 - (index % 12)).padStart(2, '0')}`, expectedOn: null, currency: 'TRY', itemCount: 1, quantity: 1, quantityReceived: 0, total: 10 }))
];
const assetPurchaseOrderWrites = [];
const assetPurchaseOrderCancelWrites = [];
const assetPurchaseOrderReceiptWrites = [];
const assetPurchaseOrderAttachmentWrites = [];
const assetPurchaseOrderAttachmentDeleteWrites = [];
const assetPurchaseBudgetWrites = [];
const assetPurchaseApprovalWrites = [];
const assetPurchaseOrderRows = [];
const budgetApprovalOrderId = 'ab100000-0000-4000-8000-000000000003';
const budgetApprovalLineId = 'ab200000-0000-4000-8000-000000000003';
const budgetRows = [{ id: 'ab500000-0000-4000-8000-000000000001', budgetYear: 2026, currency: 'TRY', amount: 1000, committedAmount: 1200, remainingAmount: -200 }];
const sharedBudgetAccessoryId = 'ac600000-0000-4000-8000-000000000001';
const sharedBudgetAccessoryLineId = 'ac610000-0000-4000-8000-000000000001';
const sharedBudgetStockId = 'ac600000-0000-4000-8000-000000000002';
const sharedBudgetStockLineId = 'ac610000-0000-4000-8000-000000000002';
const sharedBudgetApprovalWrites = { accessory: [], stock: [] };
let sharedBudgetAccessoryStatus = 'Pending';
let sharedBudgetStockStatus = 'Pending';
assetPurchaseOrderRows.push({ id: budgetApprovalOrderId, orderNumber: 'QA-UI-BUDGET-001', supplier: 'Synthetic Asset Supplier',
  status: 'Ordered', approvalStatus: 'Pending', approvalNote: null, reviewedByUserId: null, reviewedAt: null,
  orderDate: '2026-10-16', expectedOn: '2026-10-31', currency: 'TRY', quantity: 1, quantityReceived: 0, lineCount: 1,
  subtotal: 1200, taxRatePercent: 0, discountAmount: 0, freightAmount: 0, taxAmount: 0, totalCost: 1200,
  lines: [{ id: budgetApprovalLineId, name: 'QA UI Budget Laptop', quantity: 1, quantityReceived: 0, unitCost: 1200 }], assets: [], attachments: [] });
const json = (response, status = 200, body = null) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};
async function expectScanFeedback(page, pattern) {
  await page.waitForFunction(({ source, flags }) => {
    const feedback = document.querySelector('#scanAssetFeedback');
    return feedback && new RegExp(source, flags).test(feedback.textContent || '');
  }, { source: pattern.source, flags: pattern.flags });
}

const makeXlsx = rows => {
  const crc32 = bytes => {
    let crc = 0xffffffff;
    for (const byte of bytes) {
      crc ^= byte;
      for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
    return (crc ^ 0xffffffff) >>> 0;
  };
  const xmlText = value => String(value ?? '').replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
  const columnName = index => String.fromCharCode(65 + index);
  const sheetRows = rows.map((row, rowIndex) => `<row r="${rowIndex + 1}">${row.map((value, columnIndex) =>
    `<c r="${columnName(columnIndex)}${rowIndex + 1}" t="inlineStr"><is><t>${xmlText(value)}</t></is></c>`).join('')}</row>`).join('');
  const entries = [
    ['[Content_Types].xml', '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'],
    ['_rels/.rels', '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'],
    ['xl/workbook.xml', '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Assets" sheetId="1" r:id="rId1"/></sheets></workbook>'],
    ['xl/_rels/workbook.xml.rels', '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'],
    ['xl/worksheets/sheet1.xml', `<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>${sheetRows}</sheetData></worksheet>`]
  ].map(([name, text]) => ({ name: Buffer.from(name), data: Buffer.from(text) }));
  const localParts = [], centralParts = [];
  let offset = 0;
  for (const entry of entries) {
    const crc = crc32(entry.data), local = Buffer.alloc(30);
    local.writeUInt32LE(0x04034b50, 0); local.writeUInt16LE(20, 4); local.writeUInt32LE(crc, 14);
    local.writeUInt32LE(entry.data.length, 18); local.writeUInt32LE(entry.data.length, 22); local.writeUInt16LE(entry.name.length, 26);
    localParts.push(local, entry.name, entry.data);
    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0); central.writeUInt16LE(20, 4); central.writeUInt16LE(20, 6);
    central.writeUInt32LE(crc, 16); central.writeUInt32LE(entry.data.length, 20); central.writeUInt32LE(entry.data.length, 24);
    central.writeUInt16LE(entry.name.length, 28); central.writeUInt32LE(offset, 42);
    centralParts.push(central, entry.name);
    offset += local.length + entry.name.length + entry.data.length;
  }
  const centralDirectory = Buffer.concat(centralParts), end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0); end.writeUInt16LE(entries.length, 8); end.writeUInt16LE(entries.length, 10);
  end.writeUInt32LE(centralDirectory.length, 12); end.writeUInt32LE(offset, 16);
  return Buffer.concat([...localParts, centralDirectory, end]);
};
const expectedAssetXlsx = makeXlsx([
  ['VendorTag', 'VendorName', 'VendorCategory', 'VendorUnused'],
  ['QA-XLSX-001', 'QA mapped XLSX asset', 'Synthetic equipment', 'ignore me']
]);

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(appHtml);
    return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end('window.qrcodegen = window.qrcodegen || { QrCode: { Ecc: { MEDIUM: 0 }, encodeText: () => ({ size: 1, getModule: () => true }) } };'); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) {
    response.writeHead(404); response.end('Not found'); return;
  }

  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    const chunks = []; request.on('data', chunk => chunks.push(Buffer.from(chunk)));
    request.on('end', () => {
      const raw = Buffer.concat(chunks), contentType = request.headers['content-type'] || '';
      if (!raw.length) return resolve(null);
      if (contentType.includes('application/json')) return resolve(JSON.parse(raw.toString('utf8')));
        if (url.pathname.endsWith('.xlsx')) return resolve(raw);
      if (!contentType.includes('multipart/form-data')) return resolve(raw.toString('utf8'));
      const text = raw.toString('latin1'), fields = {};
      const boundary = contentType.match(/boundary=(?:"([^"]+)"|([^;]+))/i);
      const delimiter = boundary?.[1] || boundary?.[2]?.trim();
      assert.ok(delimiter, 'Multipart upload must declare its boundary.');
      for (const part of text.split(`--${delimiter}`).slice(1)) {
        if (part.startsWith('--')) continue;
        const normalized = part.replace(/^\r\n/, '').replace(/\r\n$/, '');
        const [headers, ...content] = normalized.split('\r\n\r\n');
        if (!headers || !content.length) continue;
        const name = headers.match(/name="([^"]+)"/)?.[1];
        const filename = headers.match(/filename="([^"]*)"/)?.[1];
        const value = content.join('\r\n').replace(/\r\n$/, '');
        if (name) fields[name] = filename === undefined ? value : { filename, contentType: headers.match(/Content-Type:\s*([^\r\n]+)/i)?.[1] || '', length: Buffer.byteLength(value, 'latin1') };
      }
      resolve({ contentType, ...fields });
    });
  });
  const record = { method: request.method, path: url.pathname, query: Object.fromEntries(url.searchParams.entries()), body, csrf: request.headers['x-csrf-token'] };
  requests.push(record);
  if (request.method !== 'GET' && !url.pathname.startsWith('/api/assets/import-mappings') && !url.pathname.includes('/import/preview.') && !/^\/api\/assets\/import\.(csv|xlsx)$/.test(url.pathname)) apiRequests.push(record);

  const memberAccessRoute = url.pathname.match(/^\/api\/members\/([0-9a-f-]+)\/location-access$/i);
  if (memberAccessTestEnabled && memberAccessRoute && request.method === 'GET') {
    assert.equal(memberAccessRoute[1], memberAccessTestUserId);
    json(response, 200, { userId: memberAccessTestUserId, ...memberAccessState,
      locations: [{ id: '15151515-1515-4151-8151-151515151515', name: 'Unit test location', path: 'QA Legal Entity / Unit test location', isActive: true }],
      companyUnits: companyUnitItems.map(item => ({ id: item.id, name: item.name, code: item.code, unitType: item.unitType,
        parentCompanyUnitId: item.parentCompanyUnitId || null, isActive: item.isActive })) });
    return;
  }
  if (memberAccessTestEnabled && memberAccessRoute && request.method === 'PUT') {
    assert.equal(memberAccessRoute[1], memberAccessTestUserId);
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Member unit-scope save must carry CSRF.');
    assert.equal(record.body.mode, 'all');
    assert.deepEqual(record.body.locationIds, []);
    assert.equal(record.body.companyUnitMode, 'restricted');
    assert.deepEqual(record.body.companyUnitIds, [companyUnitBranchId]);
    memberAccessState = { ...record.body };
    memberAccessWrites.push(record);
    json(response, 200, { userId: memberAccessTestUserId, ...memberAccessState, changed: true });
    return;
  }

  if (request.method === 'GET' && url.pathname === '/api/reports/warranties/export.csv') {
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-warranties.csv', 'cache-control': 'no-store' });
    response.end('Etiket;Varlık;Durum;Garanti bitişi;Konum;Zimmetli çalışan\r\nQA-WARRANTY-051;Synthetic warranty asset;Available;2027-04-15;QA Depot;\r\n');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/session/data-export') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'content-disposition': 'attachment; filename=envantertr-account-data-20261016.json', 'cache-control': 'no-store, private' });
    response.end(JSON.stringify({ schemaVersion: 1, account: { email: 'synthetic@example.invalid' }, exclusions: ['secrets omitted'] }));
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/assets/export-all.csv') {
    const query = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const status = url.searchParams.get('status') || '';
    const columns = (url.searchParams.get('columns') || '').split(',').filter(Boolean);
    const rows = [
      { tag: 'QA-CUSTOM-001', name: 'Synthetic custom report first', status: 'Available' },
      { tag: 'QA-CUSTOM-002', name: 'Synthetic custom report second', status: 'Deployed' }
    ].filter(item => (!query || Object.values(item).some(value => value.toLocaleLowerCase('tr-TR').includes(query)))
      && (!status || item.status === status));
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-custom-assets.csv', 'X-Total-Count': String(rows.length), 'cache-control': 'no-store' });
    response.end(`${columns.join(';')}\r\n${rows.map(item => columns.map(column => item[column] ?? '').join(';')).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/purchasing/orders/export.csv') {
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR'), type = url.searchParams.get('type') || '', status = url.searchParams.get('status') || '';
    const rows = unifiedPurchaseRows.filter(item => (!type || item.type === type) && (!status || item.status === status)
      && (!q || [item.orderNumber, item.supplier].some(value => value.toLocaleLowerCase('tr-TR').includes(q))));
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-purchasing.csv', 'cache-control': 'no-store' });
    response.end(`Tür;Sipariş no;Tedarikçi\r\n${rows.map(item => `${item.typeLabel};${item.orderNumber};${item.supplier}`).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/pending-deliveries/export.csv') {
    const state = url.searchParams.get('state') || 'all', q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const asOf = url.searchParams.get('asOf') || '2026-10-16';
    const matching = pendingDeliveryReportItems.filter(item => {
      const matchesSearch = !q || [item.orderNumber, item.supplier, item.itemName].some(value => value.toLocaleLowerCase('tr-TR').includes(q));
      const matchesState = state === 'all' || state === 'overdue' ? (state === 'all' || !!item.expectedOn && item.expectedOn < asOf)
        : state === 'due30' ? !!item.expectedOn && item.expectedOn >= asOf && item.expectedOn <= new Date(new Date(`${asOf}T00:00:00Z`).getTime() + 30 * 86400000).toISOString().slice(0, 10)
          : !item.expectedOn;
      return matchesSearch && matchesState;
    });
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-pending-deliveries.csv', 'cache-control': 'no-store' });
    response.end(`Sipariş türü;Sipariş no;Tedarikçi;Durum;Sipariş tarihi;Beklenen teslim;Para birimi;Kalem;Sipariş adedi;Teslim alınan;Kalan;Birim bedeli\r\n${matching.map(item => `${item.orderType};${item.orderNumber};${item.supplier};${item.status};${item.orderDate};${item.expectedOn || ''};${item.currency};${item.itemName};${item.quantity};${item.quantityReceived};${item.remaining};${item.unitCost}`).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/depreciation/export.csv') {
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const asOf = url.searchParams.get('asOf') || '2026-10-16';
    const matching = depreciationReportItems.filter(item => !q || [item.assetTag, item.name, item.serialNumber, item.category, item.model, item.location].some(value => value?.toLocaleLowerCase('tr-TR').includes(q)));
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': `attachment; filename=synthetic-depreciation-${asOf.replaceAll('-', '')}.csv`, 'cache-control': 'no-store' });
    response.end(`Etiket;Varlık;Seri no;Kategori;Model;Konum;Durum;Alış tarihi;Ömür sonu;Para birimi;Alış bedeli;Aylık amortisman;Birikmiş amortisman;Defter değeri;Kalan ay;Yöntem\r\n${matching.map(item => `${item.assetTag};${item.name};${item.serialNumber};${item.category};${item.model};${item.location};${item.status};${item.purchaseDate};${item.endOfLife};${item.currency};${item.purchaseCost};${item.monthlyDepreciation};${item.accumulatedDepreciation};${item.bookValue};${item.remainingMonths};${item.method}`).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/depreciation') {
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const asOf = url.searchParams.get('asOf') || '2026-10-16';
    const matching = depreciationReportItems.filter(item => !q || [item.assetTag, item.name, item.serialNumber, item.category, item.model, item.location].some(value => value?.toLocaleLowerCase('tr-TR').includes(q)));
    const pageSize = Number(url.searchParams.get('pageSize') || 50), pageCount = Math.max(1, Math.ceil(matching.length / pageSize));
    const page = Math.max(1, Math.min(Number(url.searchParams.get('page') || 1), pageCount));
    const totalsByCurrency = [...new Set(matching.map(item => item.currency))].sort().map(currency => {
      const rows = matching.filter(item => item.currency === currency);
      return { currency, assetCount: rows.length, acquisitionCost: rows.reduce((sum, item) => sum + item.purchaseCost, 0),
        monthlyDepreciation: rows.reduce((sum, item) => sum + item.monthlyDepreciation, 0),
        accumulatedDepreciation: rows.reduce((sum, item) => sum + item.accumulatedDepreciation, 0),
        bookValue: rows.reduce((sum, item) => sum + item.bookValue, 0) };
    });
    json(response, 200, { asOf, total: matching.length, page, pageSize, pageCount, totalsByCurrency,
      items: matching.slice((page - 1) * pageSize, page * pageSize) });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/pending-deliveries') {
    const state = url.searchParams.get('state') || 'all', q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const asOf = url.searchParams.get('asOf') || '2026-10-16';
    const due30End = new Date(new Date(`${asOf}T00:00:00Z`).getTime() + 30 * 86400000).toISOString().slice(0, 10);
    const matching = pendingDeliveryReportItems.filter(item => {
      const matchesSearch = !q || [item.orderNumber, item.supplier, item.itemName].some(value => value.toLocaleLowerCase('tr-TR').includes(q));
      const matchesState = state === 'all' || state === 'overdue' ? (state === 'all' || !!item.expectedOn && item.expectedOn < asOf)
        : state === 'due30' ? !!item.expectedOn && item.expectedOn >= asOf && item.expectedOn <= due30End
          : !item.expectedOn;
      return matchesSearch && matchesState;
    });
    const pageSize = Number(url.searchParams.get('pageSize') || 50), pageCount = Math.max(1, Math.ceil(matching.length / pageSize));
    const page = Math.max(1, Math.min(Number(url.searchParams.get('page') || 1), pageCount));
    const overdue = pendingDeliveryReportItems.filter(item => item.expectedOn && item.expectedOn < asOf);
    json(response, 200, { asOf, state, total: matching.length, page, pageSize, pageCount,
      summary: { overdueOrders: new Set(overdue.map(item => item.orderId)).size, overdueLines: overdue.length,
        undatedLines: pendingDeliveryReportItems.filter(item => !item.expectedOn).length,
        remainingUnits: pendingDeliveryReportItems.reduce((sum, item) => sum + item.remaining, 0) },
      items: matching.slice((page - 1) * pageSize, page * pageSize) });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/software-licenses/export.csv') {
    const state = url.searchParams.get('state') || 'upcoming', q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const through = url.searchParams.get('through') || licenseReportDay(90);
    const matching = licenseExpiryReportItems.filter(item => {
      const matchesSearch = !q || [item.name, item.publisher, item.productVersion, item.supplier].some(value => value?.toLocaleLowerCase('tr-TR').includes(q));
      const matchesState = state === 'all' || state === 'missing' ? state === 'all' || !item.expirationDate
        : state === 'expired' ? !!item.expirationDate && item.expirationDate < licenseReportDay(0)
          : state === 'later' ? !!item.expirationDate && item.expirationDate > through
            : !!item.expirationDate && item.expirationDate >= licenseReportDay(0) && item.expirationDate <= through;
      return matchesSearch && matchesState;
    });
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-license-expiry.csv', 'cache-control': 'no-store' });
    response.end(`Lisans;Yayıncı;Sürüm;Tür;Tedarikçi;Bitiş;Kalan gün;Atanmış;Koltuk;Boş;Otomatik yenileme\r\n${matching.map(item => `${item.name};${item.publisher};${item.productVersion};${item.licenseType};${item.supplier};${item.expirationDate || ''};${item.expirationDate ? Math.round((new Date(`${item.expirationDate}T00:00:00Z`) - licenseReportToday) / 86400000) : ''};${item.assigned};${item.seats};${Math.max(0, item.seats - item.assigned)};${item.autoRenew ? 'Evet' : 'Hayır'}`).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/assets') {
    const purchaseDateFrom = url.searchParams.get('purchaseDateFrom') || null;
    const purchaseDateTo = url.searchParams.get('purchaseDateTo') || null;
    const items = purchaseReportItems.filter(item => (!purchaseDateFrom || item.purchaseDate >= purchaseDateFrom) && (!purchaseDateTo || item.purchaseDate <= purchaseDateTo));
    json(response, 200, {
      generatedAt: '2026-10-16T05:00:00Z', purchaseDateFrom, purchaseDateTo, totalAssets: items.length,
      pricedAssetCount: items.length, withoutPurchaseCostCount: 0,
      statusCounts: [{ status: 'Available', count: 34 }, { status: 'Deployed', count: 17 }],
      categories: [{ categoryId: reportCategoryId, name: 'Synthetic report category', assetCount: items.length, assignedCount: 17, availableCount: 34, maintenanceCount: 0 }],
      purchaseCostByCurrency: [{ currency: 'TRY', recordedAssetCount: 26, recordedPurchaseCost: 65000, recordedPurchaseCostExact: '65000' }, { currency: 'USD', recordedAssetCount: 25, recordedPurchaseCost: 63750, recordedPurchaseCostExact: '63750' }]
    });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/components') {
    json(response, 200, {
      componentCount: 2, categoryCount: 2, totalQuantity: 17, allocatedQuantity: 4, availableQuantity: 13, lowStockCount: 1,
      categories: [
        { category: 'Peripherals', componentCount: 1, totalQuantity: 10, allocatedQuantity: 4, lowStockCount: 0 },
        { category: 'Adapters', componentCount: 1, totalQuantity: 7, allocatedQuantity: 0, lowStockCount: 1 }
      ]
    });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/audits') {
    const from = url.searchParams.get('from'), to = url.searchParams.get('to');
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const matching = auditReportItems.filter(item => (!from || item.occurredAt.slice(0, 10) >= from)
      && (!to || item.occurredAt.slice(0, 10) <= to)
      && (!q || [item.assetTag, item.assetName, item.details].some(value => value.toLocaleLowerCase('tr-TR').includes(q))));
    const pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
    json(response, 200, { total: matching.length, acceptedCount: matching.filter(item => item.outcome !== 'Unacceptable').length,
      unacceptableCount: matching.filter(item => item.outcome === 'Unacceptable').length, page: pageNumber, pageSize, pageCount: Math.ceil(matching.length / pageSize),
      items: matching.slice((pageNumber - 1) * pageSize, pageNumber * pageSize) });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/audits/export.csv') {
    const from = url.searchParams.get('from'), to = url.searchParams.get('to'), q = url.searchParams.get('q') || '';
    const matching = auditReportItems.filter(item => (!from || item.occurredAt.slice(0, 10) >= from)
      && (!to || item.occurredAt.slice(0, 10) <= to)
      && (!q || [item.assetTag, item.assetName, item.details].some(value => value.toLocaleLowerCase('tr-TR').includes(q.toLocaleLowerCase('tr-TR')))));
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-physical-audits.csv', 'cache-control': 'no-store' });
    response.end(`Tarih;Sonuç;Etiket;Varlık;Konum;Denetimi yapan;Açıklama\r\n${matching.map(item => `${item.occurredAt};${item.outcome === 'Unacceptable' ? 'Uygun değil' : 'Uygun'};${item.assetTag};${item.assetName};${item.location};${item.actorName};${item.details}`).join('\r\n')}\r\n`);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/maintenances') {
    const from = url.searchParams.get('from'), to = url.searchParams.get('to');
    const matching = maintenanceReportItems.filter(item => (item.status === 'InProgress' || item.status === 'Completed')
      && (!from || item.startedOn >= from) && (!to || item.startedOn <= to));
    const byType = [...new Set(matching.map(item => item.maintenanceType))].map(name => ({ name,
      count: matching.filter(item => item.maintenanceType === name).length }))
      .sort((left, right) => right.count - left.count || left.name.localeCompare(right.name));
    const costByCurrency = [...new Set(matching.filter(item => item.cost !== null).map(item => item.currency))].sort().map(currency => {
      const rows = matching.filter(item => item.cost !== null && item.currency === currency);
      const totalCost = rows.reduce((sum, item) => sum + item.cost, 0);
      return { currency, recordCount: rows.length, totalCost, totalCostExact: String(totalCost) };
    });
    json(response, 200, { generatedAt: '2026-10-16T06:00:00Z', dateFrom: from, dateTo: to,
      total: matching.length, inProgress: matching.filter(item => item.status === 'InProgress').length,
      completed: matching.filter(item => item.status === 'Completed').length, byType, costByCurrency });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/components/export.csv') {
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-component-stock.csv', 'cache-control': 'no-store' });
    response.end('"Bileşen","Kategori","Model","Ürün no","Seri no","Tedarikçi","Konum","Toplam","Atanmış","Kullanılabilir","Minimum","Birim maliyeti","Para birimi","Alış tarihi","Garanti bitiş tarihi"\r\n"Synthetic USB-C hub","Adapters","Model X","QA-COMP-001","QA-SERIAL-COMP-001","Synthetic Supplier","QA Depot","7","0","7","2","25.00","TRY","2026-10-02","2027-10-02"\r\n');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/assets/purchases') {
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const from = url.searchParams.get('from'), to = url.searchParams.get('to');
    let items = purchaseReportItems.filter(item => (!from || item.purchaseDate >= from) && (!to || item.purchaseDate <= to)
      && (!q || [item.assetTag, item.name, item.serialNumber, item.category].some(value => value.toLocaleLowerCase('tr-TR').includes(q))));
    const total = items.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
    items = items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
    json(response, 200, { total, page: pageNumber, pageSize, pageCount: Math.ceil(total / pageSize), items });
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/reports/assets/purchases/export.csv') {
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-asset-purchases.csv', 'cache-control': 'no-store' });
    response.end('Alış tarihi;Etiket;Varlık;Seri no;Kategori;Durum;Bedel;Para birimi\r\n2026-10-02;QA-REPORT-002;Synthetic report asset 2;QA-REPORT-SERIAL-2;Synthetic report category;Available;2501;USD\r\n');
    return;
  }

  if (request.method === 'POST' && url.pathname === '/api/session/login') {
    expiryLoginAttempts.push(record);
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Session re-entry must use a fresh CSRF token.');
    assert.equal(record.body.email, 'qa-session@example.invalid');
    assert.equal(record.body.password, 'Synthetic-session-password');
    assert.equal(record.body.rememberMe, true);
    assert.equal(record.body.twoFactorCode, null);
    if (companySwitchLoginFlow && !record.body.companyId) {
      json(response, 409, { message: 'Choose a company.', companies: [
        { id: companyId, name: 'Synthetic Asset Company' }, { id: secondCompanyId, name: 'Synthetic Second Company' }
      ] });
      return;
    }
    if (companySwitchLoginFlow) {
      assert.equal(record.body.companyId, secondCompanyId, 'The re-login must use the company explicitly chosen in the company picker.');
      activeCompanyId = secondCompanyId;
    } else {
      assert.equal(record.body.companyId, null);
    }
    syntheticSessionExpired = false;
    json(response, 200, { companyId: activeCompanyId, companyName: activeCompanyId === companyId ? 'Synthetic Asset Company' : 'Synthetic Second Company', userId, name: 'Synthetic Owner', role: 'owner', hasPhoto: false,
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' } });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/session/logout') {
    companySwitchLoginFlow = true;
    syntheticSessionExpired = true;
    response.writeHead(204); response.end();
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/company-units') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Company-unit creation must use CSRF protection.');
    assert.ok(['organizational', 'legal_entity', 'branch'].includes(record.body.unitType), 'Company-unit type must be explicit and valid.');
    const parent = companyUnitItems.find(item => item.id === record.body.parentCompanyUnitId);
    const item = { id: record.body.unitType === 'legal_entity' ? companyUnitRootId : companyUnitBranchId,
      name: record.body.name, code: record.body.code, unitType: record.body.unitType,
      parentCompanyUnitId: record.body.parentCompanyUnitId || null, parentName: parent?.name || null,
      path: parent ? `${parent.path} / ${record.body.name}` : record.body.name, isActive: true };
    companyUnitItems.push(item); companyUnitWrites.push(record);
    json(response, 201, item); return;
  }
  const companyUnitUpdateMatch = url.pathname.match(/^\/api\/company-units\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && companyUnitUpdateMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Company-unit updates must use CSRF protection.');
    const item = companyUnitItems.find(value => value.id === companyUnitUpdateMatch[1]);
    assert.ok(item, 'The edited company unit must belong to the synthetic active company.');
    const parent = companyUnitItems.find(value => value.id === record.body.parentCompanyUnitId);
    Object.assign(item, { name: record.body.name, code: record.body.code, unitType: record.body.unitType,
      parentCompanyUnitId: record.body.parentCompanyUnitId || null, parentName: parent?.name || null,
      path: parent ? `${parent.path} / ${record.body.name}` : record.body.name });
    companyUnitWrites.push(record); json(response, 200, item); return;
  }
  const sessionRevokeMatch = url.pathname.match(/^\/api\/session\/devices\/([0-9a-f-]+)\/revoke$/i);
  if (request.method === 'POST' && sessionRevokeMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Session revocation must use CSRF protection.');
    const session = syntheticDeviceSessions.find(item => item.id === sessionRevokeMatch[1]);
    assert.ok(session && !session.current, 'The synthetic Security page may revoke only the selected remote session.');
    syntheticDeviceSessions.splice(syntheticDeviceSessions.indexOf(session), 1);
    sessionRevocationWrites.push(record);
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Cloning must create through the CSRF-protected company asset API.');
    assert.equal(record.body.assetTag, 'QA-CLONE-001');
    assert.equal(record.body.serialNumber, 'QA-CLONE-SERIAL-001');
    assert.equal(record.body.name, asset.name);
    assert.equal(record.body.categoryId, asset.categoryId);
    assert.equal(record.body.location, asset.location);
    assert.equal(record.body.purchaseDate, asset.purchaseDate);
    assert.equal(record.body.warrantyExpiresAt, asset.warrantyExpiresAt);
    assert.equal(record.body.purchaseCost, asset.purchaseCost);
    assert.deepEqual(record.body.customFields, { inventory_reference: 'QA-SOURCE-REF' },
      'The clone must copy ordinary custom fields but never carry encrypted sensitive values.');
    assert.equal(Object.hasOwn(record.body, 'assignedEmployeeId'), false, 'A clone must not inherit an assignment.');
    assert.equal(Object.hasOwn(record.body, 'status'), false, 'A clone must be created in the default available state.');
    assetCreateWrites.push(record);
    if (failNextAssetCreate) {
      failNextAssetCreate = false;
      json(response, 503, { message: 'Synthetic asset service unavailable.' });
      return;
    }
    const created = { ...asset, ...record.body, id: '88888888-8888-4888-8888-888888888888', status: 'Available', createdAt: '2026-10-15T12:00:00Z', updatedAt: '2026-10-15T12:00:00Z' };
    clonedAssets.push(created);
    json(response, 201, { id: created.id, assetTag: created.assetTag, name: created.name, status: created.status });
    return;
  }
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}` && expireNextAssetPatch) {
    expireNextAssetPatch = false;
    syntheticSessionExpired = true;
    json(response, 401, { message: 'Synthetic session expired.' });
    return;
  }
  if (request.method === 'PUT' && url.pathname === '/api/notifications/preferences') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Notification preferences must be saved with CSRF.');
    assert.ok(Array.isArray(record.body.enabledKinds) && Array.isArray(record.body.emailEnabledKinds));
    syntheticNotificationPreferences = { enabledKinds: record.body.enabledKinds, emailEnabledKinds: record.body.emailEnabledKinds };
    notificationPreferenceWrites.push(record);
    json(response, 200, syntheticNotificationPreferences);
    return;
  }
  if (request.method === 'PUT' && url.pathname === '/api/assets/status-config') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Company status configuration must be saved with CSRF protection.');
    assert.ok(Array.isArray(body?.customStatuses) && Array.isArray(body?.transitions));
    assert.equal(body.customStatuses.length, 1);
    assert.equal(body.customStatuses[0].label, 'Kalibrasyonda');
    assert.match(body.customStatuses[0].status, /^Custom_[a-f0-9]{8}$/);
    assert.equal(body.customStatuses[0].color, '#6b5bd2', 'Native color inputs serialize CSS colors in lowercase hex.');
    assert.ok(body.transitions.some(edge => edge.from === 'Available' && edge.to === body.customStatuses[0].status));
    assert.ok(body.transitions.some(edge => edge.from === body.customStatuses[0].status && edge.to === 'Available'));
    assert.ok(!body.transitions.some(edge => edge.to === 'Archived' || edge.from === 'Archived'),
      'Archiving/restoring must stay outside the editable manual transition matrix.');
    customAssetStatuses = body.customStatuses.map(item => ({ ...item, color: item.color.toUpperCase() }));
    assetStatusTransitions = body.transitions.map(item => ({ ...item }));
    statusConfigurationWrites.push(record);
    json(response, 200, getAssetStatusConfiguration());
    return;
  }
  if (request.method === 'PUT' && url.pathname === '/api/assets/status-labels') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Company status labels must be saved with CSRF protection.');
    assert.equal(body.labels.length, 6, 'Every fixed status label must be submitted together.');
    assert.equal(new Set(body.labels.map(item => item.status)).size, 6, 'Each fixed status must be submitted exactly once.');
    for (const item of body.labels) {
      const existing = statusBuiltIns.find(status => status.status === item.status);
      assert.ok(existing, `Status ${item.status} must be one of the fixed company statuses.`);
      assert.ok(item.label.length >= 1 && item.label.length <= 40, 'Status labels must respect the required 1–40 character range.');
      existing.label = item.label;
    }
    statusLabelWrites.push(record);
    json(response, 200, getAssetStatusLabels());
    return;
  }

  if (request.method === 'GET') {
    if (url.pathname === '/api/session' && syntheticSessionExpired) {
      json(response, 401, { message: 'Synthetic session expired.' });
      return;
    }
    if (url.pathname === '/api/assets' && url.searchParams.get('q') === 'QA-BULK-1' && failNextBulkAssetScan) {
      failNextBulkAssetScan = false;
      json(response, 503, { message: 'Synthetic scan lookup unavailable.' });
      return;
    }
    let result;
    if (url.pathname === `/api/assets/${assetId}/attachments/${attachmentId}/file`) {
      response.writeHead(200, { 'content-type': 'image/png', 'content-length': attachmentBytes.length, 'cache-control': 'no-store' }); response.end(attachmentBytes); return;
    }
    else if (url.pathname === '/api/status') result = { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 };
    else if (url.pathname === '/api/setup/status') result = { setupRequired: false };
    else if (url.pathname === '/api/session/csrf') result = { requestToken: 'synthetic-asset-csrf' };
    else if (url.pathname === '/api/company-units') result = { items: companyUnitItems.map(item => ({ ...item })), total: companyUnitItems.length };
    else if (url.pathname === '/api/assets/status-labels') result = getAssetStatusLabels();
    else if (url.pathname === '/api/assets/status-config') result = getAssetStatusConfiguration();
    else if (url.pathname === '/api/session') result = {
      companyId: activeCompanyId, companyName: activeCompanyId === companyId ? 'Synthetic Asset Company' : 'Synthetic Second Company',
      availableCompanies: [{ id: companyId, name: 'Synthetic Asset Company', role: 'owner' }, { id: secondCompanyId, name: 'Synthetic Second Company', role: 'viewer' }],
      userId, name: 'Synthetic Owner', role: 'owner',
      hasPhoto: false, subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' }
    };
    else if (url.pathname === '/api/session/2fa') result = { enabled: true };
    else if (url.pathname === '/api/session/devices') result = { sessions: syntheticDeviceSessions.map(item => ({ ...item })), truncated: false };
    else if (url.pathname === '/api/notifications/preferences') result = syntheticNotificationPreferences;
    else if (url.pathname === '/api/company/branding') result = { name: 'Synthetic Asset Company', brandPrimaryColor: '#2563EB', hasLogo: false };
    else if (url.pathname === '/api/company-roles') result = ({ permissions: rolePermissionLabels, roles: companyRoleRows.map(role => ({ ...role })) });
    else if (url.pathname === '/api/members') result = ({ members: memberAccessTestEnabled ? [{ userId: memberAccessTestUserId,
      roleKey: 'viewer', createdAt: '2026-10-01T12:00:00Z', locationAccessMode: memberAccessState.mode,
      companyUnitAccessMode: memberAccessState.companyUnitMode, employeeId: null, isCurrentUser: false,
      name: 'Synthetic scoped member', email: 'unit-scope@example.invalid', isInvitationPending: false }] : [],
      total: memberAccessTestEnabled ? 1 : 0,
      roles: ['admin', 'asset_manager', 'asset_operator', 'auditor', 'viewer', 'hr_operator', ...companyRoleRows.filter(role => !role.builtIn && role.isActive).map(role => role.roleKey)] });
    else if (url.pathname === '/api/company/label-settings') result = { isConfigured: false, settings: null };
    else if (url.pathname === '/api/dashboard/summary') result = {
      totalAssets: (asset.isDeleted ? 0 : 1) + importedAssets.length + clonedAssets.length + bulkAssets.length, assignedAssets: 0, availableAssets: (asset.isDeleted ? 0 : 1) + importedAssets.length + clonedAssets.length + bulkAssets.length, maintenanceAssets: 0,
      softwareLicenses: 0, accessories: 0, consumables: 0, components: 0, activeEmployees: 0
    };
    else if (url.pathname === '/api/reservations/options') result = {
      employees: [{ id: employeeId, displayName: 'Synthetic QA Employee', employeeNumber: 'QA-001' }],
      assets: reservationAssets
    };
    else if (url.pathname === '/api/reservations') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const status = url.searchParams.get('status') || '';
      const from = url.searchParams.get('from') || '', to = url.searchParams.get('to') || '';
      let items = reservationRows.filter(item => (!status || item.status === status)
        && (!from || item.endsOn >= from) && (!to || item.startsOn <= to)
        && (!q || [item.assetTag, item.assetName, item.employeeName, item.requestNote, item.reviewNote].some(value => value?.toLocaleLowerCase('tr-TR').includes(q))));
      const total = items.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
      items = items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      result = { items, total, page: pageNumber, pageSize, canReview: true, currentUserId: userId };
    }
    else if (url.pathname === '/api/purchasing/orders') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR'), type = url.searchParams.get('type') || '', status = url.searchParams.get('status') || '';
      const from = url.searchParams.get('from') || '', to = url.searchParams.get('to') || '';
      let items = unifiedPurchaseRows.filter(item => (!type || item.type === type) && (!status || item.status === status)
        && (!from || item.orderDate >= from) && (!to || item.orderDate <= to)
        && (!q || [item.orderNumber, item.supplier].some(value => value.toLocaleLowerCase('tr-TR').includes(q))));
      const total = items.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
      items = items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      result = { items, total, page: pageNumber, pageSize: pageSize, totalPages: Math.ceil(total / pageSize) };
    }
    else if (url.pathname === `/api/asset-orders/${unifiedPurchaseAssetId}`) result = {
      order: { id: unifiedPurchaseAssetId, orderNumber: 'UPO-ASSET-001', supplier: 'Synthetic Alpha', status: 'Ordered', orderDate: '2026-10-16', expectedOn: '2026-10-30', currency: 'TRY', taxRatePercent: 0, discountAmount: 0, freightAmount: 0 },
      quantity: 4, quantityReceived: 1, subtotal: 400, taxAmount: 0, totalCost: 400,
      lines: [{ id: supplierAssetLineId, name: 'Synthetic laptop', categoryName: 'Synthetic equipment', quantity: 4, quantityReceived: 1, remaining: 3, unitCost: 400, landedUnitCost: 400 }],
      assets: [{ id: supplierAssetReceivedId, purchaseOrderLineId: supplierAssetLineId, assetTag: 'QA-SUPPLIER-ASSET-001', name: 'Synthetic returned laptop', status: supplierReturnHistory.asset.length ? 'ReturnedToSupplier' : 'Available', assignedEmployeeId: null, serialNumber: 'QA-RETURN-SERIAL-001', purchaseDate: '2026-10-16' }], attachments: []
    };
    else if (/^\/api\/asset-orders\/[0-9a-f-]+$/i.test(url.pathname)) {
      const orderId = url.pathname.split('/').at(-1), order = assetPurchaseOrderRows.find(item => item.id === orderId);
      if (!order) { json(response, 404, { message: 'Order not found.' }); return; }
      result = { order: { ...order, notes: null }, quantity: order.quantity, quantityReceived: order.quantityReceived,
        subtotal: order.subtotal, taxAmount: order.taxAmount, totalCost: order.totalCost,
        budget: order.id === budgetApprovalOrderId ? { amount: 1000, committedBeforeThisOrder: 1200, projectedCommitted: 2400, exceeds: true } : null,
        lines: order.lines.map(line => ({ ...line, categoryName: 'Synthetic equipment', quantityReceived: line.quantityReceived,
          remaining: line.quantity - line.quantityReceived, landedUnitCost: line.unitCost })), assets: order.assets || [],
        attachments: order.attachments || [], maxAttachmentSizeBytes: 10 * 1024 * 1024 };
    }
    else if (url.pathname === `/api/accessory-orders/${unifiedPurchaseAccessoryId}`) result = {
      order: { id: unifiedPurchaseAccessoryId, orderNumber: 'UPO-ACCESSORY-001', supplier: 'Synthetic Beta', status: 'PartiallyReceived', orderDate: '2026-10-15', expectedOn: null, currency: 'USD', taxRatePercent: 0, discountAmount: 0, freightAmount: 0, notes: null },
      quantity: 5, quantityReceived: 2, subtotal: 75, taxAmount: 0, totalCost: 75,
      lines: [{ id: supplierAccessoryLineId, accessoryName: 'Synthetic USB-C adapter', itemNumber: 'QA-ACC-001', quantity: 5, quantityReceived: 2, quantityReturned: supplierReturnHistory.accessory.reduce((sum, item) => sum + item.lines.reduce((n, line) => n + line.quantity, 0), 0), remaining: 3, unitCost: 15, landedUnitCost: 15, totalCost: 75 }], attachments: []
    };
    else if (url.pathname === `/api/accessory-orders/${sharedBudgetAccessoryId}`) result = {
      order: { id: sharedBudgetAccessoryId, orderNumber: 'QA-UI-SHARED-ACC-001', supplier: 'Synthetic Shared Supplier', status: 'Ordered', approvalStatus: 'NotRequired',
        approvalNote: null, reviewedByUserId: null, reviewedAt: null, orderDate: '2026-10-17', expectedOn: null, currency: 'TRY', taxRatePercent: 0, discountAmount: 0, freightAmount: 0, notes: null },
      quantity: 4, quantityReceived: 0, subtotal: 80, taxAmount: 0, totalCost: 80, budget: { amount: 100, committedBeforeThisOrder: 0, projectedCommitted: 80, exceeds: false },
      lines: [{ id: sharedBudgetAccessoryLineId, accessoryName: 'Synthetic USB-C adapter', itemNumber: 'QA-ACC-BUDGET', quantity: 4, quantityReceived: 0,
        quantityReturned: 0, remaining: 4, unitCost: 20, landedUnitCost: 20, totalCost: 80 }], attachments: []
    };
    else if (url.pathname === `/api/stock-orders/${unifiedPurchaseStockId}`) result = {
      order: { id: unifiedPurchaseStockId, orderNumber: 'UPO-STOCK-001', supplier: 'Synthetic Gamma', status: 'Received', orderDate: '2026-10-14', expectedOn: '2026-10-20', currency: 'EUR', taxRatePercent: 0, discountAmount: 0, freightAmount: 0, notes: null },
      quantity: 8, quantityReceived: 8, subtotal: 48, taxAmount: 0, totalCost: 48, attachments: [],
      lines: [
        { id: supplierStockConsumableLineId, itemType: 'Consumable', itemName: 'Synthetic labels', itemNumber: 'QA-CONS-001', quantity: 4, quantityReceived: 4, quantityReturned: 0, remaining: 0, unitCost: 2, landedUnitCost: 2, totalCost: 8 },
        { id: supplierStockComponentLineId, itemType: 'Component', itemName: 'Synthetic memory module', itemNumber: 'QA-COMP-001', quantity: 4, quantityReceived: 4, quantityReturned: 0, remaining: 0, unitCost: 10, landedUnitCost: 10, totalCost: 40 }
      ]
    };
    else if (url.pathname === `/api/stock-orders/${sharedBudgetStockId}`) result = {
      order: { id: sharedBudgetStockId, orderNumber: 'QA-UI-SHARED-STOCK-001', supplier: 'Synthetic Shared Supplier', status: 'Ordered', approvalStatus: sharedBudgetStockStatus,
        approvalNote: sharedBudgetStockStatus === 'Approved' ? 'Ortak bütçe incelendi ve onaylandı.' : null,
        reviewedByUserId: sharedBudgetStockStatus === 'Approved' ? '88888888-8888-4888-8888-888888888888' : null,
        reviewedAt: sharedBudgetStockStatus === 'Approved' ? '2026-10-17T00:00:00Z' : null,
        orderDate: '2026-10-17', expectedOn: null, currency: 'TRY', taxRatePercent: 0, discountAmount: 0, freightAmount: 0, notes: null },
      quantity: 1, quantityReceived: 0, subtotal: 30, taxAmount: 0, totalCost: 30, budget: { amount: 100, committedBeforeThisOrder: 80, projectedCommitted: 110, exceeds: true }, attachments: [],
      lines: [{ id: sharedBudgetStockLineId, itemType: 'Consumable', itemId: 'ac620000-0000-4000-8000-000000000001', itemName: 'Synthetic labels', itemNumber: 'QA-STOCK-BUDGET',
        quantity: 1, quantityReceived: 0, quantityReturned: 0, remaining: 1, unitCost: 30, landedUnitCost: 30, totalCost: 30 }]
    };
    else if (url.pathname === `/api/stock-orders/${sharedBudgetStockId}`) result = {
      order: { id: sharedBudgetStockId, orderNumber: 'QA-UI-SHARED-STOCK-001', supplier: 'Synthetic Shared Supplier', status: 'Ordered', approvalStatus: sharedBudgetStockStatus,
        approvalNote: sharedBudgetStockStatus === 'Approved' ? 'Ortak bütçe incelendi ve onaylandı.' : null,
        reviewedByUserId: sharedBudgetStockStatus === 'Approved' ? '88888888-8888-4888-8888-888888888888' : null,
        reviewedAt: sharedBudgetStockStatus === 'Approved' ? '2026-10-17T00:00:00Z' : null,
        orderDate: '2026-10-17', expectedOn: null, currency: 'TRY', taxRatePercent: 0, discountAmount: 0, freightAmount: 0, notes: null },
      quantity: 1, quantityReceived: 0, subtotal: 30, taxAmount: 0, totalCost: 30, budget: { amount: 100, committedBeforeThisOrder: 80, projectedCommitted: 110, exceeds: true }, attachments: [],
      lines: [{ id: sharedBudgetStockLineId, itemType: 'Consumable', itemId: 'ac620000-0000-4000-8000-000000000001', itemName: 'Synthetic labels', itemNumber: 'QA-STOCK-BUDGET',
        quantity: 1, quantityReceived: 0, quantityReturned: 0, remaining: 1, unitCost: 30, landedUnitCost: 30, totalCost: 30 }]
    };
    else if (url.pathname === `/api/asset-orders/${unifiedPurchaseAssetId}/returns`) result = supplierReturnHistory.asset;
    else if (url.pathname === `/api/accessory-orders/${unifiedPurchaseAccessoryId}/returns`) result = supplierReturnHistory.accessory;
    else if (url.pathname === `/api/stock-orders/${unifiedPurchaseStockId}/returns`) result = supplierReturnHistory.stock;
    else if (url.pathname === '/api/reports/warranties') {
      const today = new Date(); today.setUTCHours(0, 0, 0, 0);
      const through = url.searchParams.get('through') || new Date(today.getTime() + 90 * 86400000).toISOString().slice(0, 10);
      const throughDate = new Date(`${through}T00:00:00Z`);
      const isoDay = offset => new Date(today.getTime() + offset * 86400000).toISOString().slice(0, 10);
      const warrantyItems = [
        { id: assetId, assetTag: 'QA-WARRANTY-EXPIRED', name: 'Synthetic expired warranty', status: 'Available', warrantyExpiresAt: isoDay(-1), location: 'QA Depot', employeeName: null },
        { id: assetId, assetTag: 'QA-WARRANTY-UPCOMING', name: 'Synthetic upcoming warranty', status: 'Maintenance', warrantyExpiresAt: isoDay(10), location: 'QA Floor 2', employeeName: 'Synthetic QA Employee' },
        { id: assetId, assetTag: 'QA-WARRANTY-LATER', name: '<img src=x onerror=alert(1)>', status: 'Available', warrantyExpiresAt: isoDay(180), location: null, employeeName: null },
        { id: assetId, assetTag: 'QA-WARRANTY-MISSING', name: 'Synthetic warranty date missing', status: 'Available', warrantyExpiresAt: null, location: null, employeeName: null },
        ...Array.from({ length: 51 }, (_, index) => ({ id: assetId, assetTag: `QA-WARRANTY-${String(index + 1).padStart(3, '0')}`,
          name: `Synthetic warranty page ${index + 1}`, status: 'Available', warrantyExpiresAt: isoDay(180), location: 'QA Depot', employeeName: null }))
      ];
      const state = url.searchParams.get('state') || 'upcoming', q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      let items = warrantyItems.filter(item => {
        const expires = item.warrantyExpiresAt ? new Date(`${item.warrantyExpiresAt}T00:00:00Z`) : null;
        const matchesState = state === 'all' || state === 'missing' ? state === 'all' || !expires
          : state === 'expired' ? !!expires && expires < today
          : state === 'later' ? !!expires && expires > throughDate
          : !!expires && expires >= today && expires <= throughDate;
        const matchesSearch = !q || [item.assetTag, item.name, item.location, item.employeeName].some(value => value?.toLocaleLowerCase('tr-TR').includes(q));
        return matchesState && matchesSearch;
      });
      items.sort((left, right) => (left.warrantyExpiresAt || '9999-12-31').localeCompare(right.warrantyExpiresAt || '9999-12-31') || left.assetTag.localeCompare(right.assetTag));
      const total = items.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
      items = items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      result = { today: today.toISOString().slice(0, 10), through, state, expired: 1, upcoming: 1, withoutDate: 1,
        items, total, page: pageNumber, pageSize, pageCount: Math.ceil(total / pageSize) };
    }
    else if (url.pathname === '/api/reports/software-licenses') {
      const today = licenseReportDay(0), through = url.searchParams.get('through') || licenseReportDay(90);
      const state = url.searchParams.get('state') || 'upcoming', q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const searched = licenseExpiryReportItems.filter(item => !q || [item.name, item.publisher, item.productVersion, item.supplier].some(value => value?.toLocaleLowerCase('tr-TR').includes(q)));
      const matchesState = item => state === 'all' || state === 'missing' ? state === 'all' || !item.expirationDate
        : state === 'expired' ? !!item.expirationDate && item.expirationDate < today
          : state === 'later' ? !!item.expirationDate && item.expirationDate > through
            : !!item.expirationDate && item.expirationDate >= today && item.expirationDate <= through;
      const matching = searched.filter(matchesState).sort((left, right) => (left.expirationDate || '9999-12-31').localeCompare(right.expirationDate || '9999-12-31') || left.name.localeCompare(right.name));
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50), total = matching.length;
      result = { today, through, state, expired: searched.filter(item => item.expirationDate && item.expirationDate < today).length,
        upcoming: searched.filter(item => item.expirationDate && item.expirationDate >= today && item.expirationDate <= through).length,
        withoutDate: searched.filter(item => !item.expirationDate).length, total, page, pageSize, pageCount: Math.max(1, Math.ceil(total / pageSize)),
        items: matching.slice((page - 1) * pageSize, page * pageSize).map(item => ({ ...item,
          available: Math.max(0, item.seats - item.assigned),
          daysUntilExpiration: item.expirationDate ? Math.round((new Date(`${item.expirationDate}T00:00:00Z`) - new Date(`${today}T00:00:00Z`)) / 86400000) : null
        })) };
    }
    else if (url.pathname === `/api/models/${modelId}/details`) result = {
      model: { id: modelId, name: 'Synthetic Model Detail', modelNumber: 'MD-TEST', brandName: 'Synthetic Brand', categoryName: 'Synthetic equipment', depreciationMonths: 36, depreciationMinimum: null, depreciationMinimumType: 'amount', depreciationMethod: 'linear' },
      summary: { assetTotal: 1, statuses: [{ status: 'Available', count: 1 }] },
      assets: [{ id: assetId, assetTag: asset.assetTag, name: asset.name, status: asset.status, serialNumber: asset.serialNumber, location: asset.location, employeeName: null, updatedAt: asset.updatedAt }],
      assetTotal: 1, assetListLimit: 100,
      orders: [{ id: '88888888-8888-4888-8888-888888888888', orderNumber: 'PO-MODEL-001', supplier: 'Synthetic Supplier', status: 'PartiallyReceived', orderDate: '2026-10-01', expectedOn: '2026-10-30', quantity: 4, quantityReceived: 2 }],
      orderTotal: 1, orderListLimit: 25,
      history: [{ action: 'PhotoUpdated', details: 'Model image updated', occurredAt: '2026-10-12T10:00:00Z' }]
    };
    else if (url.pathname === '/api/assets') {
      const query = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const allItems = query === 'qa-prnt' ? printAssetRows
        : query === 'synthetic qa parent' ? [hierarchyParent]
          : query === 'synthetic qa child' ? [hierarchyChild]
            : [...(asset.isDeleted ? [] : [asset]), ...importedAssets, ...clonedAssets, ...bulkAssets];
      const matchingItems = allItems.filter(item => !query || [item.assetTag, item.serialNumber, item.name, item.companyUnitName].some(value => value?.toLocaleLowerCase('tr-TR').includes(query)))
        .map(item => item.id === assetId ? { ...item, photoAttachmentId: [...assetAttachments].reverse().find(file => file.contentType?.startsWith('image/'))?.id || null, modelPhotoUrl: null } : item);
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
      const items = query === 'qa-prnt' ? matchingItems.slice((page - 1) * pageSize, page * pageSize) : matchingItems;
      result = { items, total: matchingItems.length, page, pageSize };
    }
    else if (url.pathname === `/api/assets/${receivedAssetId}`) {
      const order = assetPurchaseOrderRows.find(item => item.orderNumber === 'QA-UI-RECEIVE-001');
      const receivedAsset = order?.assets?.find(item => item.id === receivedAssetId);
      if (!order || !receivedAsset) { json(response, 404, { message: 'Received asset not found.' }); return; }
      result = { asset: { ...asset, ...receivedAsset, id: receivedAssetId, purchaseOrderId: order.id,
        assetTag: receivedAsset.assetTag, name: receivedAsset.name, purchaseCost: 400, currency: 'TRY' },
        references: { category: 'Synthetic equipment', brand: 'Synthetic Brand', model: 'Synthetic Model Detail', modelNumber: 'MD-TEST', employee: null,
          employeeNumber: null, defaultLocation: 'Head Office' },
        history: [], attachments: [], accessories: [], components: [], softwareLicenses: [], relatedAssets: [], valuation: {},
        purchaseOrder: { id: order.id, orderNumber: order.orderNumber, supplier: order.supplier } };
    }
    else if (url.pathname === `/api/assets/${assetId}`) result = {
      asset: { ...asset }, references: { category: 'Synthetic equipment', brand: 'Synthetic Brand', model: 'Synthetic Model Detail', modelNumber: 'MD-TEST', employee: asset.assignedEmployeeName || null,
        employeeNumber: asset.assignedEmployeeId ? 'QA-001' : null, defaultLocation: 'Head Office', companyUnit: asset.companyUnitName },
      history: [...assetHistory], attachments: [...assetAttachments], accessories: [assetAccessoryRelation], components: [assetComponentRelation],
      softwareLicenses: softwareLicenseAssignments.filter(item => item.assetId === assetId && !item.revokedAt).map(item => ({
        licenseName: item.license, licenseType: item.licenseType, expirationDate: item.expirationDate,
        assignedAt: item.assignedAt, notes: item.notes
      })),
      relatedAssets: [
        ...(asset.parentAssetId === hierarchyParentId ? [{ ...hierarchyParent, relationship: 'Parent' }] : []),
        ...(hierarchyChild.parentAssetId === assetId ? [{ ...hierarchyChild, relationship: 'Child' }] : [])
      ],
      valuation: { ...assetValuation }, purchaseOrder: null
    };
    else if (bulkAssets.some(item => url.pathname === `/api/assets/${item.id}`)) {
      const scanned = bulkAssets.find(item => url.pathname === `/api/assets/${item.id}`);
      result = { asset: { ...scanned }, references: { model: null, modelNumber: null, employee: scanned.assignedEmployeeName || null,
        employeeNumber: scanned.assignedEmployeeId ? 'QA-001' : null, defaultLocation: scanned.location },
        history: [], attachments: [], accessories: [], components: [], softwareLicenses: [], relatedAssets: [], valuation: {}, purchaseOrder: null };
    }
    else if (url.pathname === `/api/assets/${assetId}/installed-software`) result = { items: installedSoftwareRows, lastSeen: '2026-10-19T12:00:00Z' };
    else if (url.pathname === `/api/assets/${assetId}/maintenances`) result = maintenanceRows;
    else if (url.pathname === '/api/contracts') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase();
      const status = url.searchParams.get('status') || '';
      let items = contractRows.filter(item => (!q || [item.name, item.contractNumber, item.contractType, item.supplier, item.notes].some(value => value?.toLocaleLowerCase().includes(q)))
        && (!status || (status === 'archived' ? !item.isActive : status === 'active' ? item.isActive : true)));
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
      const total = items.length; items = items.slice((page - 1) * pageSize, page * pageSize);
      result = { items, total, page, pageSize };
    }
    else if (url.pathname === '/api/contracts/lookups') result = { assets: [contractAsset], licenses: [contractLicense], suppliers: [] };
    else if (url.pathname === `/api/contracts/${contractId}` || contractRows.some(item => url.pathname === `/api/contracts/${item.id}`)) {
      const id = url.pathname.split('/').at(-1), item = contractRows.find(contract => contract.id === id);
      result = item ? { contract: { ...item }, assets: [contractAsset], licenses: [contractLicense] } : { message: 'Synthetic contract not found.' };
    }
    else if (url.pathname === '/api/software-licenses') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase();
      const status = url.searchParams.get('status') || 'active';
      const items = softwareLicenseRows.filter(item => (status === 'all' || (status === 'archived' ? item.isArchived : !item.isArchived))
        && (!q || [item.name, item.publisher, item.supplier, item.licenseReference].some(value => value?.toLocaleLowerCase().includes(q))));
      result = { items, total: items.length, page: 1, pageSize: 25 };
    }
    else if (softwareLicenseRows.some(item => url.pathname === `/api/software-licenses/${item.id}`)) {
      const licenseId = url.pathname.split('/').at(-1), license = softwareLicenseRows.find(item => item.id === licenseId);
      result = { license: { ...license }, assignments: softwareLicenseAssignments.filter(item => item.licenseId === licenseId).map(item => ({ ...item })), events: [],
        attachments: licenseId === softwareLicenseId ? softwareLicenseAttachments.map(item => ({ ...item })) : [],
        currentEula: licenseId === softwareLicenseId && softwareLicenseCurrentEula ? { ...softwareLicenseCurrentEula } : null };
    }
    else if (url.pathname === '/api/software-license-assignments') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const active = url.searchParams.get('active') || '';
      let items = softwareLicenseAssignments.filter(item => (!active || (active === 'true' ? !item.revokedAt : Boolean(item.revokedAt)))
        && (!q || [item.license, item.employee, item.employeeNumber, item.assetTag, item.assetName].some(value => value?.toLocaleLowerCase('tr-TR').includes(q))));
      const total = items.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
      items = items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      result = { items, total, page: pageNumber, pageSize };
    }
    else if (url.pathname === '/api/software-licenses/import-mappings') result = { items: [] };
    else if (url.pathname.startsWith('/api/contracts/') && url.pathname.endsWith('/attachments')) result = { items: [] };
    else if (url.pathname === '/api/assets/import-mappings') result = { items: assetImportProfiles.map(profile => ({ ...profile, mappings: { ...profile.mappings } })) };
    else if (url.pathname === '/api/assets/custom-fields') result = assetCustomFieldDefinitions;
    else if (url.pathname === '/api/assets/custom-reports') result = { items: customAssetReportPresets.map(item => ({ ...item, definition: { ...item.definition } })) };
    else if (url.pathname === '/api/employees' || url.pathname.startsWith('/api/employees?')) {
      const items = [
        { id: employeeId, displayName: 'Synthetic QA Employee', employeeNumber: 'QA-001' },
        ...(includeBulkLicenseEmployees ? [
          { id: bulkEmployeeTwoId, displayName: 'Synthetic Bulk Employee Two', employeeNumber: 'QA-002' },
          { id: bulkEmployeeThreeId, displayName: 'Synthetic Bulk Employee Three', employeeNumber: 'QA-003' }
        ] : [])
      ];
      includeBulkLicenseEmployees = false;
      result = { items, total: items.length, page: 1, pageSize: 200 };
    }
    else if (url.pathname === '/api/lookups/categories') result = [{ id: asset.categoryId, name: 'Synthetic equipment', assetCount: 1 }];
    else if (url.pathname === '/api/lookups/brands') result = [{ id: brandId, name: 'Synthetic Brand', assetCount: 1 }];
    else if (url.pathname === '/api/lookups/models') result = [{ id: modelId, name: 'Synthetic Model Detail', modelNumber: 'MD-TEST', brandId, brandName: 'Synthetic Brand', categoryId: asset.categoryId, categoryName: 'Synthetic equipment', hasPhoto: false, attachmentCount: 2, depreciationMonths: 36, depreciationMinimum: null, depreciationMinimumType: 'amount', depreciationMethod: 'linear', assetCount: 1 }];
    else if (url.pathname === '/api/locations') result = { items: locationDirectoryRows.map(item => ({ ...item })) };
    else if (url.pathname === '/api/suppliers') result = { items: [{ id: 'ab000000-0000-4000-8000-000000000001', name: 'Synthetic Asset Supplier', isActive: true }] };
    else if (/^\/api\/asset-purchase-budgets\/\d{4}$/.test(url.pathname)) result = {
      year: Number(url.pathname.split('/').at(-1)), items: budgetRows.map(item => ({ ...item }))
    };
    else if (url.pathname === '/api/accessory-orders') result = {
      items: [{ id: sharedBudgetAccessoryId, orderNumber: 'QA-UI-SHARED-ACC-001', supplier: 'Synthetic Shared Supplier', status: 'Ordered', approvalStatus: 'NotRequired',
        orderDate: '2026-10-17', expectedOn: null, currency: 'TRY', lineCount: 1, quantity: 4, quantityReceived: 0, subtotal: 80, taxRatePercent: 0,
        discountAmount: 0, freightAmount: 0, totalCost: 80 }], page: 1, pageSize: 25, total: 1
    };
    else if (url.pathname === '/api/stock-orders') result = {
      items: [{ id: sharedBudgetStockId, orderNumber: 'QA-UI-SHARED-STOCK-001', supplier: 'Synthetic Shared Supplier', status: 'Ordered', approvalStatus: sharedBudgetStockStatus,
        orderDate: '2026-10-17', expectedOn: null, currency: 'TRY', lineCount: 1, quantity: 1, quantityReceived: 0, subtotal: 30, taxRatePercent: 0,
        discountAmount: 0, freightAmount: 0, totalCost: 30 }], page: 1, pageSize: 25, total: 1
    };
    else if (url.pathname === '/api/asset-orders') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR'), status = url.searchParams.get('status') || '';
      let items = assetPurchaseOrderRows.filter(item => (!status || item.status === status)
        && (!q || [item.orderNumber, item.supplier].some(value => value.toLocaleLowerCase('tr-TR').includes(q))));
      const total = items.length, page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25);
      items = items.slice((page - 1) * pageSize, page * pageSize);
      result = { items, total, page, pageSize };
    }
    else if (url.pathname.startsWith('/api/assets/audit-schedule')) result = { items: [], total: 0, overdue: 0, dueSoon: 0 };
    else if (url.pathname === '/api/activity') {
      if (failNextActivityLoad) {
        failNextActivityLoad = false;
        json(response, 503, { message: 'Synthetic activity service unavailable.' });
        return;
      }
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const eventType = url.searchParams.get('eventType') || '';
      const from = url.searchParams.get('from') || '', to = url.searchParams.get('to') || '';
      let entries = activityEntries.filter(item => (!eventType || item.eventType === eventType)
        && (!from || item.occurredAt >= from) && (!to || item.occurredAt <= to)
        && (!q || [item.assetTag, item.details, item.employeeName, item.actorName].some(value => value?.toLocaleLowerCase('tr-TR').includes(q))));
      entries.sort((left, right) => right.occurredAt.localeCompare(left.occurredAt));
      const total = entries.length, pageNumber = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
      entries = entries.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
      result = { entries, total, page: pageNumber, pageSize };
    }
    else if (url.pathname === '/api/calendar') result = { items: [], total: 0 };
    else result = { items: [], total: 0, page: 1, pageSize: 50 };
    if (url.pathname === '/api/software-license-assignments'
      && url.searchParams.get('q') === 'Synthetic Slow Pagination Employee') {
      await new Promise(resolve => setTimeout(resolve, 700));
    }
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(JSON.stringify(result));
    return;
  }

  if (request.headers['x-csrf-token'] !== 'synthetic-asset-csrf') {
    response.writeHead(400, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ message: 'Missing synthetic CSRF token.' })); return;
  }
  const budgetWriteMatch = url.pathname.match(/^\/api\/asset-purchase-budgets\/(\d{4})\/(TRY|USD|EUR)$/);
  if (request.method === 'PUT' && budgetWriteMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Changing a company purchasing budget must use CSRF protection.');
    assert.ok(Number.isFinite(body.amount) && body.amount >= 0);
    const [existing] = budgetRows.filter(item => item.budgetYear === Number(budgetWriteMatch[1]) && item.currency === budgetWriteMatch[2]);
    if (existing) existing.amount = body.amount;
    else budgetRows.push({ id: 'ab500000-0000-4000-8000-000000000002', budgetYear: Number(budgetWriteMatch[1]), currency: budgetWriteMatch[2], amount: body.amount, committedAmount: 0, remainingAmount: body.amount });
    assetPurchaseBudgetWrites.push(record);
    json(response, 200, { budgetYear: Number(budgetWriteMatch[1]), currency: budgetWriteMatch[2], amount: body.amount, committedAmount: existing?.committedAmount || 0, remainingAmount: body.amount - (existing?.committedAmount || 0) });
    return;
  }
  const approvalMatch = url.pathname.match(/^\/api\/asset-orders\/([0-9a-f-]+)\/approval$/i);
  if (request.method === 'POST' && approvalMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Budget decisions must use CSRF protection.');
    const order = assetPurchaseOrderRows.find(item => item.id === approvalMatch[1]);
    assert.equal(order?.approvalStatus, 'Pending', 'Only a pending budget exception may be reviewed.');
    assert.ok(['Approved', 'Rejected'].includes(body.decision));
    assert.ok(body.note.trim().length >= 10);
    order.approvalStatus = body.decision; order.approvalNote = body.note; order.reviewedByUserId = '88888888-8888-4888-8888-888888888888';
    assetPurchaseApprovalWrites.push(record);
    json(response, 200, { id: order.id, orderNumber: order.orderNumber, approvalStatus: order.approvalStatus, approvalNote: order.approvalNote });
    return;
  }
  const accessoryBudgetApprovalMatch = url.pathname.match(/^\/api\/accessory-orders\/([0-9a-f-]+)\/approval$/i);
  if (request.method === 'POST' && accessoryBudgetApprovalMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(accessoryBudgetApprovalMatch[1], sharedBudgetAccessoryId);
    assert.equal(body.decision, 'Approved');
    sharedBudgetAccessoryStatus = body.decision; sharedBudgetApprovalWrites.accessory.push(record);
    json(response, 200, { id: sharedBudgetAccessoryId, approvalStatus: sharedBudgetAccessoryStatus, approvalNote: body.note });
    return;
  }
  const stockBudgetApprovalMatch = url.pathname.match(/^\/api\/stock-orders\/([0-9a-f-]+)\/approval$/i);
  if (request.method === 'POST' && stockBudgetApprovalMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(stockBudgetApprovalMatch[1], sharedBudgetStockId);
    assert.equal(sharedBudgetStockStatus, 'Pending');
    assert.equal(body.decision, 'Approved');
    assert.ok(body.note.trim().length >= 10);
    sharedBudgetStockStatus = body.decision; sharedBudgetApprovalWrites.stock.push(record);
    json(response, 200, { id: sharedBudgetStockId, approvalStatus: sharedBudgetStockStatus, approvalNote: body.note });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/asset-orders') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Creating a multi-line asset purchase order must use CSRF protection.');
    assert.ok(['QA-UI-MULTI-001', 'QA-UI-RECEIVE-001'].includes(body.orderNumber));
    assert.equal(body.supplier, 'Synthetic Asset Supplier');
    assert.equal(body.currency, 'TRY');
    if (body.orderNumber === 'QA-UI-MULTI-001') {
      assert.equal(body.taxRatePercent, 20); assert.equal(body.discountAmount, 1500); assert.equal(body.freightAmount, 600);
      assert.deepEqual(body.items.map(item => ({ name: item.name, categoryId: item.categoryId, quantity: item.quantity, unitCost: item.unitCost })), [
        { name: 'QA UI Laptop', categoryId: asset.categoryId, quantity: 3, unitCost: 12500 },
        { name: 'QA UI Monitor', categoryId: asset.categoryId, quantity: 2, unitCost: 2500 }
      ], 'The UI must submit every visible asset order line without collapsing or losing quantities.');
    } else {
      assert.equal(body.taxRatePercent, 0); assert.equal(body.discountAmount, 0); assert.equal(body.freightAmount, 0);
      assert.deepEqual(body.items.map(item => ({ name: item.name, categoryId: item.categoryId, quantity: item.quantity, unitCost: item.unitCost })), [
        { name: 'QA UI Receipt Laptop', categoryId: asset.categoryId, quantity: 3, unitCost: 100 }
      ]);
    }
    assetPurchaseOrderWrites.push(record);
    const id = body.orderNumber === 'QA-UI-MULTI-001' ? 'ab100000-0000-4000-8000-000000000001' : 'ab100000-0000-4000-8000-000000000002';
    const lines = body.items.map((item, index) => ({ id: `ab200000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`, name: item.name,
      quantity: item.quantity, quantityReceived: 0, unitCost: item.unitCost }));
    const subtotal = body.items.reduce((sum, item) => sum + item.quantity * item.unitCost, 0);
    const taxAmount = Math.round(((subtotal - body.discountAmount) * body.taxRatePercent / 100) * 100) / 100;
    const totalCost = subtotal - body.discountAmount + taxAmount + body.freightAmount;
    assetPurchaseOrderRows.unshift({ id, lines, assets: [], orderNumber: body.orderNumber, supplier: body.supplier,
      status: 'Ordered', orderDate: body.orderDate, expectedOn: body.expectedOn, currency: body.currency, lineCount: body.items.length,
      quantity: body.items.reduce((sum, item) => sum + item.quantity, 0), quantityReceived: 0, subtotal,
      taxRatePercent: body.taxRatePercent, discountAmount: body.discountAmount, freightAmount: body.freightAmount, taxAmount, totalCost });
    json(response, 201, { id, orderNumber: body.orderNumber });
    return;
  }
  const assetOrderCancelMatch = url.pathname.match(/^\/api\/asset-orders\/([0-9a-f-]+)\/cancel$/i);
  if (request.method === 'POST' && assetOrderCancelMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Canceling an asset order must use CSRF protection.');
    const order = assetPurchaseOrderRows.find(item => item.id === assetOrderCancelMatch[1]);
    assert.ok(order, 'The cancellation must target a visible company order.');
    assert.ok(['Ordered', 'PartiallyReceived'].includes(order.status), 'Only open orders can be cancelled.');
    assetPurchaseOrderCancelWrites.push(record);
    order.status = 'Cancelled';
    json(response, 200, { status: order.status });
    return;
  }
  const assetOrderReceiveMatch = url.pathname.match(/^\/api\/asset-orders\/([0-9a-f-]+)\/receive$/i);
  if (request.method === 'POST' && assetOrderReceiveMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Receiving an asset order must use CSRF protection.');
    const order = assetPurchaseOrderRows.find(item => item.id === assetOrderReceiveMatch[1]);
    assert.ok(order, 'Receipt must target a visible company order.');
    assert.ok(Array.isArray(body.items) && body.items.length > 0);
    let receivedQuantity = 0;
    for (const requested of body.items) {
      const line = order.lines.find(item => item.id === requested.lineId);
      assert.ok(line, 'Receipt line must belong to the selected order.');
      assert.ok(Number.isInteger(requested.quantity) && requested.quantity > 0 && requested.quantity <= line.quantity - line.quantityReceived,
        'Receipt quantity must be positive and within the remaining order quantity.');
      line.quantityReceived += requested.quantity;
      receivedQuantity += requested.quantity;
      for (let index = 0; index < requested.quantity; index++) order.assets.push({ id: order.assets.length === 0 ? receivedAssetId : `ab300000-0000-4000-8000-${String(order.assets.length + 1).padStart(12, '0')}`,
        purchaseOrderLineId: line.id, assetTag: `QA-UI-RECEIVED-${String(order.assets.length + 1).padStart(3, '0')}`, name: line.name,
        status: 'Available', serialNumber: null, purchaseDate: body.receivedOn });
    }
    order.quantityReceived += receivedQuantity;
    order.status = order.quantityReceived === order.quantity ? 'Received' : 'PartiallyReceived';
    assetPurchaseOrderReceiptWrites.push(record);
    json(response, 200, { status: order.status, receivedQuantity, orderQuantityReceived: order.quantityReceived, orderQuantity: order.quantity });
    return;
  }
  const assetOrderAttachmentMatch = url.pathname.match(/^\/api\/asset-orders\/([0-9a-f-]+)\/attachments$/i);
  if (request.method === 'POST' && assetOrderAttachmentMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Uploading an asset order attachment must use CSRF protection.');
    const order = assetPurchaseOrderRows.find(item => item.id === assetOrderAttachmentMatch[1]);
    assert.ok(order, 'Attachment must target a visible company order.');
    assert.ok(body?.file?.filename && body.file.length > 0, 'Upload must include the selected file.');
    assert.ok(['application/pdf', 'image/png', 'image/jpeg', 'image/webp'].includes(body.file.contentType));
    order.attachments ||= [];
    const attachment = { id: `ab400000-0000-4000-8000-${String(order.attachments.length + 1).padStart(12, '0')}`,
      originalFileName: body.file.filename, contentType: body.file.contentType, length: body.file.length,
      createdAt: '2026-10-17T00:00:00Z', uploadedBy: 'Synthetic Owner' };
    order.attachments.push(attachment);
    assetPurchaseOrderAttachmentWrites.push(record);
    json(response, 201, attachment);
    return;
  }
  const assetOrderAttachmentDeleteMatch = url.pathname.match(/^\/api\/asset-orders\/([0-9a-f-]+)\/attachments\/([0-9a-f-]+)$/i);
  if (request.method === 'DELETE' && assetOrderAttachmentDeleteMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Deleting an asset order attachment must use CSRF protection.');
    const order = assetPurchaseOrderRows.find(item => item.id === assetOrderAttachmentDeleteMatch[1]);
    const attachment = order?.attachments?.find(item => item.id === assetOrderAttachmentDeleteMatch[2]);
    assert.ok(attachment, 'Delete must target the selected attachment.');
    order.attachments.splice(order.attachments.indexOf(attachment), 1);
    assetPurchaseOrderAttachmentDeleteWrites.push(record);
    response.writeHead(204); response.end();
    return;
  }
  const assetNoteMatch = url.pathname.match(/^\/api\/assets\/([0-9a-f-]+)\/notes$/i);
  if (request.method === 'POST' && assetNoteMatch) {
    assert.equal(assetNoteMatch[1], assetId, 'Asset note must target the selected company asset.');
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Asset history notes must use CSRF protection.');
    const note = typeof body?.note === 'string' ? body.note.trim() : '';
    if (!note || note.length > 2000) {
      json(response, 400, { message: 'Not 1 ile 2000 karakter arasında olmalıdır.' });
      return;
    }
    const event = { id: 'abababab-abab-4bab-8bab-000000000001', eventType: 'NoteAdded', occurredAt: new Date().toISOString(),
      details: note, actorName: 'Synthetic Owner', attachmentCount: 0 };
    assetHistory.unshift(event);
    assetNoteWrites.push(record);
    json(response, 201, { id: event.id, assetId, note, occurredAt: event.occurredAt });
    return;
  }
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}/parent`) {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    if (body?.parentAssetId === hierarchyChildId || body?.parentAssetId === foreignParentId) {
      response.writeHead(400, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ message: body.parentAssetId === foreignParentId ? 'Üst varlık aynı şirkette bulunamadı.' : 'Varlık hiyerarşisinde döngü oluşturulamaz.' }));
      return;
    }
    asset.parentAssetId = body?.parentAssetId || null;
    asset.updatedAt = '2026-10-16T21:00:00Z';
    assetHistory.unshift({ id: 'abababab-abab-4bab-8bab-abababababab', eventType: 'ParentChanged', occurredAt: asset.updatedAt,
      details: asset.parentAssetId ? 'Üst varlık QA-PARENT-001 olarak ayarlandı.' : 'Üst varlık bağlantısı kaldırıldı.', actorName: 'Synthetic Owner', attachmentCount: 0 });
    json(response, 200, { id: assetId, parentAssetId: asset.parentAssetId, updatedAt: asset.updatedAt, changed: true });
    return;
  }
  const supplierReturnMatch = url.pathname.match(/^\/api\/(asset|accessory|stock)-orders\/([0-9a-f-]+)\/returns$/i);
  if (request.method === 'POST' && supplierReturnMatch) {
    const [, type, orderId] = supplierReturnMatch;
    const expectedOrderId = { asset: unifiedPurchaseAssetId, accessory: unifiedPurchaseAccessoryId, stock: unifiedPurchaseStockId }[type];
    assert.equal(orderId, expectedOrderId, 'Supplier return must target the selected tenant purchase order.');
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Supplier return must use company CSRF protection.');
    assert.ok(record.body.returnedOn && record.body.reason?.trim(), 'Supplier return must require a date and reason.');
    if (type === 'asset') {
      assert.deepEqual(record.body.assetIds, [supplierAssetReceivedId], 'Only the selected received purchase-order asset may be returned.');
      supplierReturnHistory.asset.unshift({ id: 'ac500000-0000-4000-8000-000000000001', returnNumber: 'SR-UI-ASSET-001', returnedOn: record.body.returnedOn, reason: record.body.reason, notes: record.body.notes, lines: [{ assetId: supplierAssetReceivedId, quantity: 1 }] });
      supplierReturnWrites.push(record); json(response, 201, { returnedAssets: 1 }); return;
    }
    const expectedLineIds = type === 'accessory' ? [supplierAccessoryLineId] : [supplierStockConsumableLineId, supplierStockComponentLineId];
    assert.deepEqual(record.body.items.map(item => item.lineId), expectedLineIds, 'The return request must contain the selected received lines in purchase-order order.');
    assert.ok(record.body.items.every(item => item.quantity > 0), 'Zero quantities must not be posted as return lines.');
    const lines = record.body.items.map(item => ({ purchaseOrderLineId: item.lineId, quantity: item.quantity }));
    supplierReturnHistory[type].unshift({ id: `ac500000-0000-4000-8000-00000000000${supplierReturnHistory[type].length + 2}`, returnNumber: `SR-UI-${type.toUpperCase()}-001`, returnedOn: record.body.returnedOn, reason: record.body.reason, notes: record.body.notes, lines });
    supplierReturnWrites.push(record); json(response, 201, { returnedQuantity: lines.reduce((sum, item) => sum + item.quantity, 0) }); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/custom-reports') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Custom report preset creation must send CSRF.');
    const saved = { id: `c5000000-0000-4000-8000-${String(++customAssetReportSequence).padStart(12, '0')}`,
      name: record.body.name, definition: { columns: [...record.body.columns], q: record.body.q || '', status: record.body.status || '',
        warranty: record.body.warranty || '', sortBy: record.body.sortBy || 'tag', sortDirection: record.body.sortDirection || 'asc' } };
    customAssetReportPresets.push(saved);
    json(response, 200, saved); return;
  }
  const customAssetReportMatch = url.pathname.match(/^\/api\/assets\/custom-reports\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && customAssetReportMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Custom report preset update must send CSRF.');
    const saved = customAssetReportPresets.find(item => item.id === customAssetReportMatch[1]);
    assert.ok(saved, 'Custom report update must target the selected saved preset.');
    saved.name = record.body.name;
    saved.definition = { columns: [...record.body.columns], q: record.body.q || '', status: record.body.status || '',
      warranty: record.body.warranty || '', sortBy: record.body.sortBy || 'tag', sortDirection: record.body.sortDirection || 'asc' };
    json(response, 200, saved); return;
  }
  if (request.method === 'DELETE' && customAssetReportMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Custom report preset deletion must send CSRF.');
    const index = customAssetReportPresets.findIndex(item => item.id === customAssetReportMatch[1]);
    assert.ok(index >= 0, 'Custom report deletion must target the selected saved preset.');
    customAssetReportPresets.splice(index, 1);
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/locations/catalog') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Creating a catalog location must send CSRF.');
    assert.equal(typeof record.body.name, 'string');
    const parent = record.body.parentLocationId ? locationDirectoryRows.find(item => item.locationId === record.body.parentLocationId) : null;
    assert.ok(!record.body.parentLocationId || parent, 'A nested catalog location must use a listed company parent.');
    const depth = parent ? parent.depth + 1 : 0;
    assert.ok(depth < 6, 'The catalog UI must never submit a seventh location level.');
    const id = `f2000000-0000-4000-8000-${String(++locationCatalogSequence).padStart(12, '0')}`;
    const name = parent ? `${parent.name} / ${record.body.name}` : record.body.name;
    const companyUnit = companyUnitItems.find(item => item.id === record.body.companyUnitId);
    const created = { name, locationId: id, parentLocationId: parent?.locationId || null, depth,
      companyUnitId: companyUnit?.id || null, companyUnitName: companyUnit?.name || null,
      isCatalogEntry: true, isActive: true, isDefault: false, managerEmployeeId: null, managerName: null,
      assetCount: 0, assignedCount: 0, availableCount: 0, maintenanceCount: 0, accessoryCount: 0,
      accessoryAssignmentCount: 0, consumableCount: 0, componentCount: 0 };
    locationDirectoryRows.push(created);
    locationCatalogWrites.push(record);
    json(response, 201, { id, parentLocationId: created.parentLocationId, companyUnitId: created.companyUnitId, name: record.body.name,
      displayPath: name, depth, isActive: true });
    return;
  }
  const locationCatalogUpdateMatch = url.pathname.match(/^\/api\/locations\/catalog\/([0-9a-f-]+)$/i);
  if (request.method === 'PATCH' && locationCatalogUpdateMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Updating a catalog location must send CSRF.');
    const location = locationDirectoryRows.find(item => item.locationId === locationCatalogUpdateMatch[1]);
    assert.ok(location?.isCatalogEntry, 'A catalog update must target the selected company location.');
    const unit = companyUnitItems.find(item => item.id === record.body.companyUnitId);
    location.name = location.parentLocationId
      ? `${locationDirectoryRows.find(item => item.locationId === location.parentLocationId)?.name || ''} / ${record.body.name}`
      : record.body.name;
    location.companyUnitId = record.body.clearCompanyUnit ? null : unit?.id || null;
    location.companyUnitName = record.body.clearCompanyUnit ? null : unit?.name || null;
    locationCatalogWrites.push(record);
    json(response, 200, { id: location.locationId, name: record.body.name, companyUnitId: location.companyUnitId, isActive: true });
    return;
  }
  const locationDefaultMatch = url.pathname.match(/^\/api\/locations\/catalog\/([0-9a-f-]+)\/default$/i);
  if (request.method === 'POST' && locationDefaultMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Changing the default location must send CSRF.');
    const location = locationDirectoryRows.find(item => item.locationId === locationDefaultMatch[1]);
    assert.ok(location?.isCatalogEntry && location.isActive, 'Only an active company catalog location can be made default.');
    for (const item of locationDirectoryRows) item.isDefault = item.locationId === location.locationId && record.body.isDefault === true;
    locationDefaultWrites.push(record);
    json(response, 200, { id: location.locationId, isDefault: location.isDefault });
    return;
  }
  const locationManagerMatch = url.pathname.match(/^\/api\/locations\/catalog\/([0-9a-f-]+)\/manager$/i);
  if (request.method === 'POST' && locationManagerMatch) {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Changing the location manager must send CSRF.');
    const location = locationDirectoryRows.find(item => item.locationId === locationManagerMatch[1]);
    assert.ok(location?.isCatalogEntry && location.isActive, 'Only an active company catalog location can have a manager.');
    const selectedEmployee = record.body.employeeId ? { id: employeeId, displayName: 'Synthetic QA Employee' } : null;
    assert.equal(record.body.employeeId || null, selectedEmployee?.id || null, 'The manager assignment must use the selected active company employee.');
    location.managerEmployeeId = selectedEmployee?.id || null;
    location.managerName = selectedEmployee?.displayName || null;
    locationManagerWrites.push(record);
    json(response, 200, { id: location.locationId, managerEmployeeId: location.managerEmployeeId, managerName: location.managerName });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/locations/catalog/from-path') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Legacy location promotion must use the company CSRF token.');
    assert.equal(record.body.path, legacyLocationPath, 'Promotion must submit the complete free-text location path.');
    locationPromotionWrites.push(record);
    const segments = legacyLocationPath.split(' / ');
    locationDirectoryRows = segments.map((segment, index) => ({
      name: segments.slice(0, index + 1).join(' / '), locationId: `f1000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
      parentLocationId: index ? `f1000000-0000-4000-8000-${String(index).padStart(12, '0')}` : null,
      depth: index, isCatalogEntry: true, isActive: true, isDefault: false, managerEmployeeId: null, managerName: null,
      assetCount: index === 2 ? 1 : 0, assignedCount: 0, availableCount: index === 2 ? 1 : 0, maintenanceCount: 0,
      accessoryCount: 0, accessoryAssignmentCount: 0, consumableCount: 0, componentCount: 0
    }));
    json(response, 200, { id: locationDirectoryRows.at(-1).locationId, name: 'QA Room', displayPath: legacyLocationPath, createdCount: 3, isActive: true });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/company-roles') {
    assert.equal(typeof body?.name, 'string', 'Creating a custom role must submit its name.');
    assert.ok(Array.isArray(body?.permissionKeys), 'Creating a custom role must submit selected permissions.');
    assert.equal(companyRoleRows.some(role => role.name.toLocaleUpperCase('tr-TR') === body.name.toLocaleUpperCase('tr-TR')), false,
      'The UI must not submit a duplicate role name.');
    const created = { roleKey: `custom_browser_${++customRoleSequence}`, name: body.name,
      permissionKeys: [...body.permissionKeys], builtIn: false, isActive: true };
    companyRoleRows.push(created); companyRoleWrites.push(record);
    json(response, 201, created); return;
  }
  const customRoleRoute = url.pathname.match(/^\/api\/company-roles\/(custom_browser_\d+)(?:\/(active))?$/);
  if (customRoleRoute && request.method === 'PUT') {
    const role = companyRoleRows.find(item => item.roleKey === customRoleRoute[1]);
    assert.ok(role, 'Editing must target the current company custom role.');
    role.name = body.name; role.permissionKeys = [...body.permissionKeys]; companyRoleWrites.push(record);
    response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify(role)); return;
  }
  if (customRoleRoute && request.method === 'PATCH' && customRoleRoute[2] === 'active') {
    const role = companyRoleRows.find(item => item.roleKey === customRoleRoute[1]);
    assert.ok(role, 'Activation must target the current company custom role.');
    role.isActive = body.isActive; companyRoleWrites.push(record);
    json(response, 200, { roleKey: role.roleKey, isActive: role.isActive }); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/software-licenses') {
    assert.equal(typeof body?.name, 'string', 'A new license must submit its name.');
    const created = { ...body, id: 'a5555555-5555-4555-8555-555555555555', assigned: 0, isArchived: false };
    softwareLicenseRows.push(created);
    softwareLicenseCrudWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    json(response, 201, { id: created.id });
    return;
  }
  const licenseStateMatch = url.pathname.match(/^\/api\/software-licenses\/([0-9a-f-]+)\/(archive|restore)$/i);
  if (request.method === 'POST' && licenseStateMatch) {
    const license = softwareLicenseRows.find(item => item.id === licenseStateMatch[1]);
    assert.ok(license, 'Archive and restore must target an existing synthetic license.');
    if (licenseStateMatch[2] === 'archive' && license.assigned > 0) {
      json(response, 409, { message: 'Active assignments prevent archiving.' }); return;
    }
    license.isArchived = licenseStateMatch[2] === 'archive';
    softwareLicenseCrudWrites.push({ method: request.method, path: url.pathname, csrf: request.headers['x-csrf-token'] });
    response.writeHead(204); response.end(); return;
  }
  const licenseUpdateMatch = url.pathname.match(/^\/api\/software-licenses\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && licenseUpdateMatch) {
    const license = softwareLicenseRows.find(item => item.id === licenseUpdateMatch[1]);
    assert.ok(license, 'An update must target a synthetic company license.');
    assert.ok(body.seats >= license.assigned, 'License updates must not lower total seats below active assignments.');
    Object.assign(license, body);
    softwareLicenseCrudWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    response.writeHead(204); response.end();
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/reservations') {
    const selectedAsset = reservationAssets.find(item => item.id === body?.assetId);
    assert.ok(selectedAsset, 'A reservation must reference one of the available synthetic assets.');
    const created = { id: `a2000000-0000-4000-8000-${String(reservationRows.length + 1).padStart(12, '0')}`,
      assetId: selectedAsset.id, assetTag: selectedAsset.assetTag, assetName: selectedAsset.name,
      employeeId: body.employeeId, employeeName: 'Synthetic QA Employee', requestedByUserId: userId,
      requestedBy: 'Synthetic Owner', startsOn: body.startsOn, endsOn: body.endsOn,
      status: 'Pending', requestNote: body.requestNote || null, reviewNote: null };
    reservationRows.push(created);
    reservationWrites.push(record);
    json(response, 201, { id: created.id, status: created.status });
    return;
  }
  const reservationAction = url.pathname.match(/^\/api\/reservations\/([0-9a-f-]+)\/(decision|cancel)$/i);
  if (request.method === 'POST' && reservationAction) {
    const row = reservationRows.find(item => item.id === reservationAction[1]);
    assert.ok(row, 'Reservation action must reference a synthetic reservation.');
    reservationWrites.push(record);
    if (reservationAction[2] === 'decision') {
      row.status = body.approve ? 'Approved' : 'Rejected';
      row.reviewNote = body.note;
    } else row.status = 'Cancelled';
    json(response, 200, { id: row.id, status: row.status });
    return;
  }
  const singleLicenseAssignmentMatch = url.pathname.match(/^\/api\/software-licenses\/([0-9a-f-]+)\/assign$/i);
  if (request.method === 'POST' && singleLicenseAssignmentMatch) {
    const licenseId = singleLicenseAssignmentMatch[1], license = softwareLicenseRows.find(item => item.id === licenseId);
    assert.ok(license, 'A single assignment must target a synthetic company license.');
    assert.equal(body.eulaAccepted, license.requiresEulaAcceptance, 'Assignment must reflect the configured EULA requirement.');
    if (body.eulaAccepted) assert.ok(softwareLicenseCurrentEula, 'An EULA-required license must have a current EULA document.');
    const isAssetAssignment = Boolean(body.assetId);
    assert.equal(isAssetAssignment ? body.employeeId : body.employeeId === employeeId, isAssetAssignment ? null : true,
      'The submitted target must contain exactly one selected employee or asset.');
    if (isAssetAssignment) assert.equal(body.assetId, assetId, 'Device assignment must use the selected company asset.');
    const duplicate = softwareLicenseAssignments.some(item => item.licenseId === licenseId && !item.revokedAt
      && (isAssetAssignment ? item.assetId === body.assetId : item.employeeId === body.employeeId));
    assert.equal(duplicate, false, 'The same active employee or asset assignment must not be duplicated.');
    assert.ok(license.assigned < license.seats, 'A license assignment must have an available seat.');
    const assigned = { id: `e1000000-0000-4000-8000-${String(softwareLicenseAssignments.length + 1).padStart(12, '0')}`, licenseId,
      license: license.name, licenseType: license.licenseType, employeeId: isAssetAssignment ? null : body.employeeId,
      employee: isAssetAssignment ? null : 'Synthetic QA Employee', employeeNumber: isAssetAssignment ? null : 'QA-001',
      assetId: isAssetAssignment ? body.assetId : null, assetTag: isAssetAssignment ? asset.assetTag : null, assetName: isAssetAssignment ? asset.name : null,
      assignedAt: '2026-10-15T12:00:00Z', expirationDate: null, revokedAt: null,
      eulaAttachmentId: body.eulaAccepted ? softwareLicenseCurrentEula.id : null,
      eulaVersion: body.eulaAccepted ? softwareLicenseCurrentEula.eulaVersion : null,
      eulaAcceptedAt: body.eulaAccepted ? '2026-10-15T14:05:00Z' : null,
      eulaAcceptedBy: body.eulaAccepted ? 'Synthetic Owner' : null, notes: body.notes };
    softwareLicenseAssignments.push(assigned);
    license.assigned += 1;
    softwareLicenseAssignmentWrites.push(record);
    json(response, 201, { id: assigned.id });
    return;
  }
  if (request.method === 'POST' && url.pathname === `/api/software-licenses/${softwareLicenseId}/assign/bulk`) {
    const license = softwareLicenseRows.find(item => item.id === softwareLicenseId);
    assert.deepEqual(body.employeeIds, [bulkEmployeeTwoId, bulkEmployeeThreeId], 'Bulk assignment must submit only the selected active employees in list order.');
    assert.equal(body.notes, 'Synthetic bulk assignment note');
    assert.equal(body.eulaAccepted, true, 'Bulk EULA assignment must include explicit administrator confirmation.');
    assert.ok(softwareLicenseCurrentEula, 'Bulk EULA assignment requires the currently versioned document.');
    assert.equal(body.employeeIds.some(id => softwareLicenseAssignments.some(item => item.licenseId === softwareLicenseId && item.employeeId === id && !item.revokedAt)), false,
      'Bulk assignment must not duplicate an active employee seat.');
    assert.ok(license.assigned + body.employeeIds.length <= license.seats, 'Bulk assignment must respect the available seat count.');
    const created = body.employeeIds.map((id, index) => ({
      id: `e4000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
      licenseId: softwareLicenseId, license: license.name, licenseType: license.licenseType,
      employeeId: id, employee: id === bulkEmployeeTwoId ? 'Synthetic Bulk Employee Two' : 'Synthetic Bulk Employee Three',
      employeeNumber: id === bulkEmployeeTwoId ? 'QA-002' : 'QA-003', assetId: null, assetTag: null, assetName: null,
      assignedAt: '2026-10-15T14:10:00Z', expirationDate: null, revokedAt: null,
      eulaAttachmentId: softwareLicenseCurrentEula.id, eulaVersion: softwareLicenseCurrentEula.eulaVersion,
      eulaAcceptedAt: '2026-10-15T14:10:00Z', eulaAcceptedBy: 'Synthetic Owner', notes: body.notes
    }));
    softwareLicenseAssignments.push(...created);
    license.assigned += created.length;
    softwareLicenseAssignmentWrites.push(record);
    await new Promise(resolve => setTimeout(resolve, 250));
    json(response, 201, { assignments: created.map(item => ({ id: item.id, employeeId: item.employeeId, assignedAt: item.assignedAt })),
      assigned: license.assigned, available: license.seats - license.assigned });
    return;
  }
  const licenseRevokeMatch = url.pathname.match(/^\/api\/software-licenses\/([0-9a-f-]+)\/revoke$/i);
  if (request.method === 'POST' && licenseRevokeMatch) {
    const licenseId = licenseRevokeMatch[1], license = softwareLicenseRows.find(item => item.id === licenseId);
    const assignment = softwareLicenseAssignments.find(item => item.id === body.assignmentId && item.licenseId === licenseId && !item.revokedAt);
    assert.ok(assignment, 'Only the active selected assignment may be revoked.');
    assert.ok(license, 'Revocation must target an existing company license.');
    assignment.revokedAt = '2026-10-15T13:00:00Z';
    license.assigned -= 1;
    softwareLicenseAssignmentWrites.push(record);
    response.writeHead(204); response.end();
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/software-licenses/import/preview.csv') {
    assert.equal(typeof body, 'string', 'The license preview must submit the selected CSV as text.');
    assert.equal(body, 'Lisans,Yayıncı,Ürün sürümü,Tür,Tedarikçi,Koltuk,Birim maliyeti,Para birimi,Alış tarihi,Bitiş tarihi,Otomatik yenileme,Notlar\nSynthetic Imported License,Synthetic Publisher,2,Subscription,Synthetic Import Supplier,5,,TRY,,,false,Synthetic import note\n');
    softwareLicensePreviewRequests.push({ path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    json(response, 200, { isValid: true, rowCount: 1, counts: { licenses: 1 }, errors: [],
      message: '1 yeni lisans doğrulandı; 1 yeni tedarikçi yalnızca aktarımı onaylarsanız kataloğa eklenecek. Önizleme kayıt eklemedi.' });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/software-licenses/import.csv') {
    softwareLicenseImportAttempts.push({ path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    if (failNextSoftwareLicenseImport) {
      failNextSoftwareLicenseImport = false;
      json(response, 503, { message: 'Synthetic supplier service unavailable.' });
      return;
    }
    const created = {
      id: '12121212-1212-4212-8212-121212121212', name: 'Synthetic Imported License', publisher: 'Synthetic Publisher',
      productVersion: '2', licenseType: 'Subscription', licenseReference: null, supplier: 'Synthetic Import Supplier',
      seats: 5, assigned: 0, unitCost: null, currency: 'TRY', purchaseDate: null, expirationDate: null,
      autoRenew: false, requiresEulaAcceptance: false, notes: 'Synthetic import note'
    };
    softwareLicenseRows.push(created);
    softwareLicenseWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'], created });
    json(response, 200, { message: '1 lisans ve denetim geçmişi eklendi; 1 yeni tedarikçi kataloğa bağlandı.' });
    return;
  }
  const softwareLicenseAttachmentDelete = url.pathname.match(/^\/api\/software-licenses\/([0-9a-f-]+)\/attachments\/([0-9a-f-]+)$/i);
  if (request.method === 'POST' && url.pathname === `/api/software-licenses/${softwareLicenseId}/attachments`) {
    assert.equal(body?.file?.filename, 'synthetic-eula-v2.pdf');
    assert.equal(body?.isEulaDocument, 'true');
    assert.equal(body?.eulaVersion, '2.0');
    const attachment = { id: 'abababab-abab-4bab-8bab-abababababab', originalFileName: body.file.filename,
      contentType: 'application/pdf', length: body.file.length, createdAt: '2026-10-15T14:00:00Z',
      uploadedBy: 'Synthetic Owner', isEulaDocument: true, isCurrentEula: true, eulaVersion: body.eulaVersion };
    for (const old of softwareLicenseAttachments) { if (old.isCurrentEula) old.isCurrentEula = false; }
    softwareLicenseAttachments.push(attachment);
    softwareLicenseCurrentEula = { id: attachment.id, originalFileName: attachment.originalFileName, contentType: attachment.contentType, eulaVersion: attachment.eulaVersion };
    softwareLicenseWrites.push(record);
    json(response, 201, { id: attachment.id, originalFileName: attachment.originalFileName, contentType: attachment.contentType,
      length: attachment.length, createdAt: attachment.createdAt, isEulaDocument: true, isCurrentEula: true, eulaVersion: attachment.eulaVersion });
    return;
  }
  if (request.method === 'DELETE' && softwareLicenseAttachmentDelete) {
    assert.equal(softwareLicenseAttachmentDelete[1], softwareLicenseId);
    assert.equal(softwareLicenseAttachmentDelete[2], softwareLicenseAttachmentId);
    softwareLicenseAttachments.splice(0, softwareLicenseAttachments.length);
    softwareLicenseWrites.push({ method: request.method, path: url.pathname, csrf: request.headers['x-csrf-token'] });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && /^\/api\/assets\/import\/preview\.(csv|xlsx)$/.test(url.pathname)) {
    assetImportPreviewRequests.push({ path: url.pathname, headers: request.headers, body });
    const encoded = request.headers['x-asset-import-mapping'];
    if (!encoded) {
      json(response, 400, { message: 'Unknown columns need mapping.', unmappedColumns: ['VendorTag', 'VendorName', 'VendorCategory', 'VendorUnused'],
        mappingTargets: [{ key: 'assetTag', label: 'Etiket' }, { key: 'name', label: 'Varlık adı' }, { key: 'category', label: 'Kategori' }, { key: 'ignore', label: 'Bu sütunu yoksay' }] });
      return;
    }
    json(response, 200, { isValid: true, rowCount: 1, counts: { assets: 1 }, errors: [], message: 'Synthetic mapped asset preview.' });
    return;
  }
  if (request.method === 'POST' && /^\/api\/assets\/import\.(csv|xlsx)$/.test(url.pathname)) {
    const isXlsx = url.pathname.endsWith('.xlsx');
    const mappings = JSON.parse(Buffer.from(request.headers['x-asset-import-mapping'], 'base64').toString('utf8'));
    assert.deepEqual(mappings, { VendorTag: 'assetTag', VendorName: 'name', VendorCategory: 'category', VendorUnused: 'ignore' },
      'The real import action must reuse the persisted mapping with API-supported target keys.');
    if (isXlsx) {
      assert.equal(Buffer.isBuffer(body), true, 'The explicit XLSX apply must preserve binary workbook bytes.');
      assert.deepEqual(body, expectedAssetXlsx, 'The explicit XLSX apply action must submit the unchanged selected workbook.');
      assert.match(request.headers['content-type'], /application\/vnd\.openxmlformats-officedocument\.spreadsheetml\.sheet/);
    } else {
      assert.equal(body, 'VendorTag,VendorName,VendorCategory,VendorUnused\nQA-NEW-001,QA mapped asset,Synthetic equipment,ignore me\n',
        'The explicit apply action must submit the unchanged selected CSV file.');
    }
    const created = { ...asset, id: isXlsx ? '77777777-6666-4666-8666-666666666666' : '66666666-6666-4666-8666-666666666666', assetTag: isXlsx ? 'QA-XLSX-001' : 'QA-NEW-001', name: isXlsx ? 'QA mapped XLSX asset' : 'QA mapped asset' };
    importedAssets.push(created);
    assetImportApplicationWrites.push({ method: request.method, path: url.pathname, body, mappings, csrf: request.headers['x-csrf-token'], created });
    json(response, 200, { message: '1 synthetic asset imported.' });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/import-mappings') {
    assert.equal(typeof body?.name, 'string');
    const profile = { id: 'abababab-abab-4bab-8bab-abababababab', name: body.name, mappings: { ...body.mappings } };
    assetImportProfiles.splice(0, assetImportProfiles.length, profile);
    assetImportProfileWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    json(response, 201, profile);
    return;
  }
  const assetImportProfileDeleteMatch = url.pathname.match(/^\/api\/assets\/import-mappings\/([0-9a-f-]+)$/i);
  if (request.method === 'DELETE' && assetImportProfileDeleteMatch) {
    assetImportProfiles.splice(0, assetImportProfiles.length, ...assetImportProfiles.filter(profile => profile.id !== assetImportProfileDeleteMatch[1]));
    assetImportProfileDeleteWrites.push({ method: request.method, path: url.pathname, csrf: request.headers['x-csrf-token'] });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/attachments`) {
    assert.ok(body?.file && body.file.filename === 'synthetic-proof.png', 'Upload must carry the selected file.');
    const latestEvent = [...assetHistory].sort((left, right) => Date.parse(right.occurredAt) - Date.parse(left.occurredAt))[0];
    assert.equal(body.assetEventId, latestEvent?.id || '');
    const item = { id: attachmentId, originalFileName: body.file.filename, contentType: 'image/png', length: attachmentBytes.length,
      createdAt: '2026-10-15T12:00:00Z', uploadedBy: 'Synthetic Owner', assetEventId: body.assetEventId || null,
      assetEventType: body.assetEventId ? assetHistory.find(event => event.id === body.assetEventId)?.eventType || null : null };
    assetAttachments.push(item);
    const linkedEvent = assetHistory.find(event => event.id === body.assetEventId);
    if (linkedEvent) linkedEvent.attachmentCount = (linkedEvent.attachmentCount || 0) + 1;
    response.writeHead(201, { 'content-type': 'application/json' }); response.end(JSON.stringify({ ...item, id: attachmentId })); return;
  }
  if (request.method === 'DELETE' && url.pathname === `/api/assets/${assetId}/attachments/${attachmentId}`) {
    assetAttachments.splice(0, assetAttachments.length);
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/contracts') {
    const item = { ...body, id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee', isActive: true, status: 'active', assetCount: body.assetIds?.length || 0, licenseCount: body.licenseIds?.length || 0, attachmentCount: 0 };
    contractRows.push(item); contractWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    response.writeHead(201, { 'content-type': 'application/json' }); response.end(JSON.stringify({ id: item.id, name: item.name })); return;
  }
  const contractArchiveMatch = url.pathname.match(/^\/api\/contracts\/([0-9a-f-]+)\/(archive|restore)$/i);
  if (request.method === 'POST' && contractArchiveMatch) {
    const item = contractRows.find(contract => contract.id === contractArchiveMatch[1]);
    if (item) { item.isActive = contractArchiveMatch[2] === 'restore'; item.status = item.isActive ? 'active' : 'archived'; }
    contractWrites.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/company/import/preview') {
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ isValid: true, restoreTargetEmpty: true, sourceCompany: 'Synthetic Restore Source', targetCompany: 'Synthetic Asset Company', counts: { assets: 2, employees: 1 }, existingMatches: {}, errors: [], warning: 'Synthetic preview only.' }));
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/company/import/apply') {
    restoreApplyWrites.push({ body, csrf: request.headers['x-csrf-token'] });
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ message: 'Synthetic company archive restored.', counts: { assets: 2, employees: 1 } }));
    return;
  }
  if (request.method === 'PUT' && url.pathname === `/api/assets/${assetId}/audit-schedule`) {
    asset.nextAuditOn = body?.nextAuditOn || null;
    response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify({ nextAuditOn: asset.nextAuditOn }));
    return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/audit`) {
    asset.nextAuditOn = body?.nextAuditOn || null;
    asset.updatedAt = new Date().toISOString();
    const unacceptable = body?.outcome === 'Unacceptable';
    assetHistory.unshift({
      id: `44444444-4444-4444-8444-${String(assetHistory.length + 1).padStart(12, '0')}`, eventType: unacceptable ? 'AuditUnacceptable' : 'Audited', occurredAt: asset.updatedAt,
      details: `${unacceptable ? `Fiziksel denetimde uygun bulunmadı · Bulgu: ${body.issueDescription}` : 'Fiziksel denetim tamamlandı'} · ${body.auditDate}${body.notes ? ` · ${body.notes}` : ''}`,
      actorName: 'Synthetic Owner', attachmentCount: 0
    });
    json(response, 200, { id: assetId, outcome: unacceptable ? 'Unacceptable' : 'Accepted', issueDescription: body?.issueDescription || null, auditDate: body.auditDate, nextAuditOn: asset.nextAuditOn });
    return;
  }
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}`) {
    if (body?.expectedUpdatedAt !== asset.updatedAt) {
      response.writeHead(409, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ message: 'Asset changed since the form was opened.' })); return;
    }
    asset.assetTag = body.assetTag;
    asset.name = body.name;
    asset.categoryId = body.categoryId;
    asset.serialNumber = body.serialNumber;
    asset.location = body.location;
    asset.insuranceProvider = body.insuranceProvider;
    asset.insurancePolicyNumber = body.insurancePolicyNumber;
    asset.insuranceExpiresAt = body.insuranceExpiresAt;
    asset.updatedAt = '2026-10-14T13:00:00Z';
    assetHistory.unshift({ id: '66666666-6666-4666-8666-666666666666', eventType: 'Updated', occurredAt: asset.updatedAt,
      details: `Varlık bilgileri güncellendi · ${asset.name}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify({ changed: true }));
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-status') {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(body.status, 'Maintenance');
    assert.equal(body.note, 'Quarterly bulk inspection');
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected synthetic assets.');
    for (const target of targets) {
      target.status = body.status;
      target.updatedAt = '2026-10-15T10:00:00Z';
    }
    json(response, 200, { updated: targets.length, unchanged: 0 });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-update') {
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Bulk edit must use CSRF protection.');
    assert.deepEqual(record.body.fields, ['name', 'serialNumber'], 'Only explicitly selected fields may be applied.');
    assert.equal(record.body.name, 'Quarterly Refresh');
    assert.equal(record.body.serialNumber, 'QA-BULK-UPDATED');
    assert.equal(record.body.notes, null, 'Unchecked fields must not be included as updates.');
    assert.equal(record.body.note, 'Quarterly asset refresh');
    const selectedIds = record.body.assets.map(item => item.id);
    assert.deepEqual(selectedIds, ['12121212-1212-4121-8121-121212121212', '13131313-1313-4131-8131-131313131313']);
    const targets = bulkAssets.filter(item => selectedIds.includes(item.id));
    assert.equal(targets.length, 2);
    assert.ok(targets.every((item, index) => item.updatedAt === record.body.assets[index].updatedAt), 'Bulk edit must submit current asset versions.');
    for (const target of targets) {
      target.name = record.body.name;
      target.serialNumber = record.body.serialNumber;
      target.updatedAt = '2026-10-16T20:00:00Z';
    }
    json(response, 200, { updated: targets.length, unchanged: 0, fields: record.body.fields });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-return') {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(body.note, 'Quarterly device return');
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected assigned synthetic assets.');
    assert.ok(targets.every(item => item.assignedEmployeeId === employeeId && item.status === 'Deployed'));
    for (const target of targets) {
      target.status = 'Available';
      target.updatedAt = '2026-10-15T10:15:00Z';
      delete target.assignedEmployeeId;
      delete target.assignedEmployeeName;
      delete target.expectedReturnOn;
    }
    json(response, 200, { updated: targets.length });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-assign') {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(body.employeeId, employeeId);
    assert.equal(body.note, 'Quarterly device allocation');
    assert.equal(body.expectedReturnOn, '2026-12-31');
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected synthetic assets.');
    assert.ok(targets.every(item => item.status === 'Available' && !item.assignedEmployeeId));
    for (const target of targets) {
      target.status = 'Deployed';
      target.updatedAt = '2026-10-15T10:30:00Z';
      target.assignedEmployeeId = employeeId;
      target.assignedEmployeeName = 'Synthetic QA Employee';
      target.expectedReturnOn = body.expectedReturnOn;
    }
    json(response, 200, { updated: targets.length, employeeId, employeeName: 'Synthetic QA Employee' });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-transfer') {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.equal(body.targetLocation, 'West Wing');
    assert.equal(body.notes, 'Quarterly floor move');
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected synthetic assets.');
    assert.ok(targets.every(item => item.status === 'Available' && !item.assignedEmployeeId));
    assert.ok(targets.every(item => item.location !== body.targetLocation));
    for (const target of targets) {
      target.location = body.targetLocation;
      target.updatedAt = '2026-10-15T10:45:00Z';
    }
    json(response, 200, { updated: targets.length, targetLocation: body.targetLocation });
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/assets/bulk-audit') {
    assert.equal(record.csrf, 'synthetic-asset-csrf');
    assert.ok(body.notes === 'Quarterly physical audit' || body.notes === 'Cracked casing found');
    assert.match(body.auditDate, /^\d{4}-\d{2}-\d{2}$/);
    assert.ok(body.outcome === 'Accepted' || body.outcome === 'Unacceptable');
    if (body.outcome === 'Unacceptable') assert.ok(body.issueDescription?.trim(), 'An unacceptable bulk audit must include a finding.');
    else assert.equal(body.issueDescription, null);
    if (body.nextAuditOn) assert.match(body.nextAuditOn, /^\d{4}-\d{2}-\d{2}$/);
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected synthetic assets.');
    const auditEvents = [];
    for (const target of targets) {
      target.nextAuditOn = body.nextAuditOn;
      target.updatedAt = '2026-10-15T11:00:00Z';
      auditEvents.push({ assetId: target.id, auditDate: body.auditDate, notes: body.notes, outcome: body.outcome,
        issueDescription: body.issueDescription, eventType: body.outcome === 'Unacceptable' ? 'AuditUnacceptable' : 'Audited' });
    }
    record.responseBody = { recorded: targets.length, auditDate: body.auditDate, nextAuditOn: body.nextAuditOn,
      outcome: body.outcome, auditEvents };
    json(response, 200, record.responseBody);
    return;
  }
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}/status`) {
    asset.status = body.status;
    asset.updatedAt = '2026-10-14T13:30:00Z';
    assetHistory.unshift({ id: '55555555-5555-4555-8555-555555555555', eventType: 'StatusChanged', occurredAt: asset.updatedAt,
      details: `Varlık durumu güncellendi · ${asset.status}${body.note ? ` · ${body.note}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/restore`) {
    assert.equal(asset.status, 'Archived', 'Synthetic restore must only restore an archived asset.');
    asset.status = 'Available';
    assetHistory.unshift({ id: '55555555-5555-4555-8555-555555555557', eventType: 'Restored', occurredAt: '2026-10-14T13:40:00Z',
      details: `Varlık arşivden geri alındı${body.note ? ` · ${body.note}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/trash`) {
    asset.isDeleted = true;
    assetHistory.unshift({ id: '55555555-5555-4555-8555-555555555556', eventType: 'Trashed', occurredAt: '2026-10-14T13:45:00Z',
      details: `Varlık çöp kutusuna taşındı${body.note ? ` · ${body.note}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/maintenances/schedule`) {
    maintenanceRows.unshift({ id: maintenanceId, maintenanceType: body.maintenanceType, supplier: body.supplier,
      description: body.description, plannedOn: body.plannedOn, startedOn: body.plannedOn, status: 'Scheduled',
      cost: body.cost, currency: body.currency, notes: body.notes, actorName: 'Synthetic Owner' });
    response.writeHead(201, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ id: maintenanceId, status: 'Scheduled', plannedOn: body.plannedOn })); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/maintenances/${maintenanceId}/start`) {
    maintenanceRows[0].status = 'InProgress';
    maintenanceRows[0].startedOn = new Date().toISOString().slice(0, 10);
    asset.status = 'Maintenance';
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/maintenances/${maintenanceId}/complete`) {
    maintenanceRows[0].status = 'Completed';
    maintenanceRows[0].completedOn = body.completedOn;
    maintenanceRows[0].completionNotes = body.notes;
    asset.status = 'Available';
    asset.updatedAt = '2026-10-14T14:00:00Z';
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/transfer`) {
    asset.location = body.targetLocation;
    assetHistory.unshift({ id: '88888888-8888-4888-8888-888888888888', eventType: 'Transferred', occurredAt: '2026-10-14T15:00:00Z',
      details: `Konum transferi: Head Office → ${asset.location}${body.notes ? ` · ${body.notes}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/assign`) {
    asset.assignedEmployeeId = body.employeeId;
    asset.assignedEmployeeName = body.employeeId === employeeId ? 'Synthetic QA Employee' : 'Unknown synthetic employee';
    asset.expectedReturnOn = body.expectedReturnOn;
    asset.status = 'Deployed';
    assetHistory.unshift({ id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', eventType: 'Assigned', occurredAt: '2026-10-14T16:00:00Z',
      details: `Varlık ${asset.assignedEmployeeName} çalışanına zimmetlendi${body.note ? ` · ${body.note}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ id: assetId, assetTag: asset.assetTag, status: asset.status, assignedEmployeeId: employeeId,
      expectedReturnOn: asset.expectedReturnOn, updatedAt: '2026-10-14T16:00:00Z' })); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/return`) {
    assert.equal(asset.assignedEmployeeId, employeeId, 'Synthetic return must only be accepted for the currently assigned employee.');
    asset.status = 'Available';
    delete asset.assignedEmployeeId;
    delete asset.assignedEmployeeName;
    delete asset.expectedReturnOn;
    assetHistory.unshift({ id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', eventType: 'Returned', occurredAt: '2026-10-14T17:00:00Z',
      details: `Varlık iade alındı${body.note ? ` · ${body.note}` : ''}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(204); response.end(); return;
  }
  response.writeHead(405, { 'content-type': 'application/json' });
  response.end(JSON.stringify({ message: 'Unexpected write in asset-detail browser acceptance.' }));
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.context().addInitScript(() => {
  window.__syntheticClipboard = '';
  Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async text => { window.__syntheticClipboard = String(text); } } });
  window.bwipjs = { toCanvas(canvas) { canvas.width = 120; canvas.height = 40; const context = canvas.getContext('2d'); if (context) { context.fillStyle = '#fff'; context.fillRect(0, 0, 120, 40); } } };
  window.print = () => { document.documentElement.dataset.syntheticPrintInvoked = 'true'; };
});
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  if (!await page.locator('[data-page="Varlık modelleri"]').isVisible()) await page.locator('[data-group="inventory"]').click();
  await page.locator('[data-page="Varlıklar"]').first().click();
  try {
    await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).waitFor({ state: 'visible', timeout: 15000 });
  } catch (error) {
    console.error('ASSET_LIST_STATE', JSON.stringify({
      bodyClass: await page.locator('body').getAttribute('class'),
      authError: await page.locator('#authError').innerText().catch(() => ''),
      assetRows: await page.locator('#assetRows').innerText().catch(() => ''),
      pageTitle: await page.locator('#pageTitle').innerText().catch(() => ''),
      requests: requests.map(({ method, path }) => ({ method, path })), pageErrors
    }));
    throw error;
  }
  await page.setViewportSize({ width: 1920, height: 1000 });
  const desktopTableLayout = await page.evaluate(() => {
    const main = document.querySelector('main');
    const panel = document.querySelector('#assetListPanel');
    const tableWrap = panel?.querySelector('.table-wrap');
    const mainBounds = main?.getBoundingClientRect();
    const panelBounds = panel?.getBoundingClientRect();
    return {
      leftInset: panelBounds && mainBounds ? panelBounds.left - mainBounds.left : Infinity,
      rightInset: panelBounds && mainBounds ? mainBounds.right - panelBounds.right : Infinity,
      tableScrollsInsideWrapper: !!tableWrap && tableWrap.scrollWidth > tableWrap.clientWidth && getComputedStyle(tableWrap).overflowX === 'auto'
    };
  });
  assert.ok(desktopTableLayout.leftInset <= 20 && desktopTableLayout.rightInset <= 20,
    `Desktop inventory should use the main content width without wide empty side gutters: ${JSON.stringify(desktopTableLayout)}`);
  assert.equal(desktopTableLayout.tableScrollsInsideWrapper, true, 'The wide asset table must scroll inside its own wrapper.');
  await page.setViewportSize({ width: 390, height: 844 });
  const mobileOverflow = await page.evaluate(() => ({
    documentWidth: document.documentElement.scrollWidth,
    viewportWidth: document.documentElement.clientWidth,
    wrapperScrolls: (() => { const element = document.querySelector('#assetListPanel .table-wrap'); return !!element && getComputedStyle(element).overflowX === 'auto'; })()
  }));
  assert.ok(mobileOverflow.documentWidth <= mobileOverflow.viewportWidth,
    `Narrow inventory should keep horizontal scrolling inside the table: ${JSON.stringify(mobileOverflow)}`);
  assert.equal(mobileOverflow.wrapperScrolls, true, 'The narrow asset table must remain horizontally scrollable without widening the page.');
  console.log(`PASS: inventory uses desktop width (left/right insets ${desktopTableLayout.leftInset}/${desktopTableLayout.rightInset}px); table scroll stays local at desktop and 390px mobile width.`);
  await page.locator('#assetListSearch').fill('QA-PRNT');
  await page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/assets' && requestUrl.searchParams.get('q') === 'QA-PRNT';
  });
  await page.locator('#assetRows').getByRole('button', { name: 'Synthetic prnt asset 1', exact: true }).waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetCount').innerText(), /205/,
    `The filtered list must report all 205 assets before printing; got "${await page.locator('#assetCount').innerText()}" with ${JSON.stringify(requests.filter(item => item.path === '/api/assets').slice(-3).map(item => item.query))}.`);
  const printApiRequestCount = requests.filter(item => item.method === 'GET' && item.path === '/api/assets').length;
  const printPopupPromise = page.waitForEvent('popup');
  await page.locator('#assetPrintButton').click();
  const printPopup = await printPopupPromise;
  await printPopup.locator('.print-status').waitFor({ state: 'visible' });
  await printPopup.waitForFunction(() => document.querySelector('#printStatus')?.classList.contains('ready') || document.querySelector('#printStatus')?.classList.contains('error'));
  assert.equal(await printPopup.locator('#printStatus').getAttribute('class'), 'print-status ready',
    `The print preview must finish successfully before printing: ${await printPopup.locator('#printStatus').innerText()}; requests=${JSON.stringify(requests.filter(item => item.path === '/api/assets').slice(-5).map(item => item.query))}`);
  assert.match(await printPopup.locator('#printStatus').innerText(), /Yazdırmaya hazır: 205 varlık/,
    'The print preview must report the complete server-filtered asset count across multiple pages.');
  assert.equal(await printPopup.locator('tbody tr').count(), 205,
    'The print preview must include every matching asset across the 200-row API page boundary.');
  assert.match(await printPopup.locator('tbody').innerText(), /QA-PRNT-001/);
  assert.match(await printPopup.locator('tbody').innerText(), /QA-PRNT-205/,
    'The print preview must render the final asset from the next API page.');
  assert.equal(await printPopup.locator('thead th').count(), 18,
    'The print preview must respect the selected default data columns and exclude selection/actions.');
  assert.equal(await printPopup.locator('table').isVisible(), true);
  await printPopup.waitForFunction(() => document.documentElement.dataset.syntheticPrintInvoked === 'true');
  const printRequest = requests.filter(item => item.method === 'GET' && item.path === '/api/assets').slice(printApiRequestCount)
    .filter(item => item.query.pageSize === '200' && item.query.q === 'QA-PRNT');
  assert.deepEqual(printRequest.map(item => item.query.page).sort(), ['1', '2'],
    'Printing must independently request every filtered server page, not reuse only the visible page.');
  assert.equal(await page.locator('#assetPrintButton').isDisabled(), false,
    'The print action must become available after preview generation.');
  await printPopup.close();
  await page.locator('#assetFullscreenButton').click();
  await page.waitForFunction(() => document.fullscreenElement?.id === 'assetListPanel');
  assert.equal(await page.locator('#assetFullscreenButton').getAttribute('aria-pressed'), 'true',
    'Entering fullscreen must update the view control state for assistive technology.');
  await page.locator('#assetFullscreenButton').click();
  await page.waitForFunction(() => document.fullscreenElement === null);
  assert.equal(await page.locator('#assetFullscreenButton').getAttribute('aria-pressed'), 'false',
    'Leaving fullscreen must restore the control state.');
  console.log('PASS: asset list print paginates all 205 filtered assets across the 200-row boundary, preserves visible columns, invokes print; fullscreen enters/exits and synchronizes its accessible state.');
  const resetAssetSearch = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/assets' && !requestUrl.searchParams.has('q');
  });
  await page.locator('#assetListSearch').fill('');
  await resetAssetSearch;
  await page.locator(`#assetRows tr[data-id="${assetId}"]`).waitFor({ state: 'visible' });
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.locator('#sidebar button[data-page="Genel Bakış"]').click();

  await page.locator('#overviewActions summary').click();
  await page.locator('#importPreviewButton').click();
  const assetImportDialog = page.locator('#importPreviewDialog');
  await assetImportDialog.waitFor({ state: 'visible' });
  const assetCsv = { name: 'synthetic-vendor.csv', mimeType: 'text/csv', buffer: Buffer.from('VendorTag,VendorName,VendorCategory,VendorUnused\nQA-NEW-001,QA mapped asset,Synthetic equipment,ignore me\n') };
  await page.locator('#importPreviewFile').setInputFiles(assetCsv);
  await page.locator('#runImportPreview').click();
  await page.locator('#assetImportMappingPanel').waitFor({ state: 'visible' });
  for (const [header, target] of Object.entries({ VendorTag: 'assetTag', VendorName: 'name', VendorCategory: 'category', VendorUnused: 'ignore' })) {
    await page.locator(`#assetImportMappingFields select[data-source-header="${header}"]`).selectOption(target);
  }
  await page.locator('#applyAssetImportMapping').click();
  await page.waitForFunction(() => document.querySelector('#applyCsvImport')?.hidden === false);
  assert.match(await page.locator('#importPreviewResult').innerText(), /Synthetic mapped asset preview/);
  await page.locator('#assetImportProfileName').fill('Synthetic vendor map');
  await page.locator('#saveAssetImportProfile').click();
  await page.waitForFunction(() => document.querySelector('#assetImportProfile')?.value === 'abababab-abab-4bab-8bab-abababababab');
  assert.equal(assetImportProfileWrites.length, 1, 'Saving the mapping must persist exactly one synthetic profile.');
  assert.deepEqual(assetImportProfileWrites[0].body, { name: 'Synthetic vendor map', mappings: { VendorTag: 'assetTag', VendorName: 'name', VendorCategory: 'category', VendorUnused: 'ignore' } });
  assert.equal(assetImportProfileWrites[0].csrf, 'synthetic-asset-csrf');
  const previewCountBeforeClose = assetImportPreviewRequests.length;
  await page.locator('#closeImportPreview').click();
  await assetImportDialog.waitFor({ state: 'hidden' });
  assert.equal(assetImportPreviewRequests.length, previewCountBeforeClose, 'Closing after save must not start another preview or apply the import.');
  await page.locator('#overviewActions summary').click();
  await page.locator('#importPreviewButton').click();
  await assetImportDialog.waitFor({ state: 'visible' });
  await page.locator('#importPreviewFile').setInputFiles(assetCsv);
  await page.locator('#assetImportProfile').selectOption('abababab-abab-4bab-8bab-abababababab');
  await page.waitForFunction(() => document.querySelector('#applyCsvImport')?.hidden === false);
  assert.equal(assetImportPreviewRequests.length, previewCountBeforeClose + 1, 'Selecting the saved mapping with a file must re-run preview, not import.');
  assert.deepEqual(JSON.parse(Buffer.from(assetImportPreviewRequests.at(-1).headers['x-asset-import-mapping'], 'base64').toString('utf8')),
    assetImportProfileWrites[0].body.mappings, 'Reloaded saved profile must send its exact field mapping to preview.');
  assert.equal(assetImportProfileWrites.length, 1, 'Reusing a profile must not create another saved profile.');
  assert.equal(assetImportProfileWrites.length + restoreApplyWrites.length, 1, 'The acceptance must not apply CSV or restore data.');
  const applyResponsePromise = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/assets/import.csv');
  await page.locator('#applyCsvImport').click();
  const applyResponse = await applyResponsePromise;
  assert.equal(applyResponse.status(), 200, 'Explicitly applying a valid preview must call the CSV import endpoint successfully.');
  await page.locator('#assetRows tr[data-id="66666666-6666-4666-8666-666666666666"]').waitFor({ state: 'visible' });
  assert.equal(assetImportApplicationWrites.length, 1, 'The explicit apply action must create exactly one synthetic asset.');
  assert.deepEqual(assetImportApplicationWrites[0].mappings, assetImportProfileWrites[0].body.mappings);
  assert.equal(assetImportApplicationWrites[0].csrf, 'synthetic-asset-csrf');
  assert.match(await page.locator('#assetRows tr[data-id="66666666-6666-4666-8666-666666666666"]').innerText(), /QA mapped asset[\s\S]*QA-NEW-001/);
  const xlsxFile = { name: 'synthetic-vendor.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', buffer: expectedAssetXlsx };
  await page.locator('#importPreviewFile').setInputFiles(xlsxFile);
  await page.locator('#assetImportProfile').selectOption('');
  const xlsxPreviewPromise = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/assets/import/preview.xlsx');
  await page.locator('#assetImportProfile').selectOption('abababab-abab-4bab-8bab-abababababab');
  const xlsxPreview = await xlsxPreviewPromise;
  assert.equal(xlsxPreview.status(), 200, 'Selecting a saved mapping for XLSX must preview successfully.');
  const xlsxPreviewWrite = assetImportPreviewRequests.at(-1);
  assert.equal(xlsxPreviewWrite.path, '/api/assets/import/preview.xlsx');
  assert.deepEqual(xlsxPreviewWrite.body, expectedAssetXlsx, 'XLSX preview must preserve the selected OOXML bytes.');
  assert.match(xlsxPreviewWrite.headers['content-type'], /application\/vnd\.openxmlformats-officedocument\.spreadsheetml\.sheet/);
  const xlsxApplyPromise = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/assets/import.xlsx');
  await page.locator('#applyCsvImport').click();
  const xlsxApply = await xlsxApplyPromise;
  assert.equal(xlsxApply.status(), 200, 'Explicitly applying a valid XLSX preview must reach the XLSX endpoint.');
  await page.locator('#assetRows tr[data-id="77777777-6666-4666-8666-666666666666"]').waitFor({ state: 'visible' });
  assert.equal(assetImportApplicationWrites.length, 2, 'CSV and XLSX should each create exactly one synthetic row.');
  assert.equal(assetImportApplicationWrites[1].path, '/api/assets/import.xlsx');
  assert.equal(assetImportApplicationWrites[1].csrf, 'synthetic-asset-csrf');
  assert.match(await page.locator('#assetRows tr[data-id="77777777-6666-4666-8666-666666666666"]').innerText(), /QA mapped XLSX asset[\s\S]*QA-XLSX-001/);
  await page.locator('#deleteAssetImportProfile').click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#employeeActionSummary').innerText(), /Seçili eşleme profili silinsin mi/);
  await page.locator('[data-close-employee-action]').last().click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
  assert.equal(assetImportProfileDeleteWrites.length, 0, 'Canceling profile deletion must not delete the saved mapping.');
  assert.equal(await page.locator('#assetImportProfile').inputValue(), 'abababab-abab-4bab-8bab-abababababab', 'Cancel must retain the selected profile.');
  await page.locator('#deleteAssetImportProfile').click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
  await page.locator('#employeeActionSubmit').click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetImportProfile')?.options.length === 1);
  assert.deepEqual(assetImportProfileDeleteWrites, [{ method: 'DELETE', path: '/api/assets/import-mappings/abababab-abab-4bab-8bab-abababababab', csrf: 'synthetic-asset-csrf' }]);
  assert.equal(await page.locator('#deleteAssetImportProfile').isDisabled(), true, 'Deleted profile must no longer be selected or deletable.');
  await page.locator('#closeImportPreview').click();
  await assetImportDialog.waitFor({ state: 'hidden' });
  console.log('PASS: asset CSV and XLSX map/profile/preview/apply routes preserve the selected files, use CSRF and refresh both synthetic rows; profile deletion cancel/confirm is safe.');

  await page.locator('#pageTitle').click();
  await page.keyboard.press('Control+n');
  const newAssetDialog = page.locator('#newAssetDialog');
  await newAssetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#newAssetButton').getAttribute('aria-keyshortcuts'), 'Control+N');
  await page.keyboard.press('Escape');
  await newAssetDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening then canceling owner Ctrl+N must not create an asset.');

  const assetRow = page.locator(`#assetRows tr[data-id="${assetId}"]`);
  assert.match(await assetRow.innerText(), /Synthetic QA Unit/,
    'The existing location column must show the asset’s linked company unit.');
  const unitSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/assets' && url.searchParams.get('q') === 'Synthetic QA Unit';
  });
  await page.locator('#assetListSearch').fill('Synthetic QA Unit');
  await unitSearchResponse;
  assert.equal(await page.locator(`#assetRows tr[data-id="${assetId}"]`).count(), 1,
    'Searching the asset list by company unit must retain its linked assets.');
  const clearUnitSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/assets' && !url.searchParams.get('q');
  });
  await page.locator('#assetListSearch').fill('');
  await clearUnitSearchResponse;
  await assetRow.locator('[data-asset-action="view"]').waitFor({ state: 'visible' });
  await assetRow.locator('[data-asset-action="view"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#dialogTitle').innerText(), asset.name,
    'The row detail shortcut must open the selected asset, not the surrounding row or a different record.');
  assert.equal(await page.locator('#detailCompanyUnit').innerText(), 'Synthetic QA Unit',
    'Asset details must show the linked company unit from the tenant-scoped API response.');
  assert.equal(await page.locator('#detailInsuranceProvider').innerText(), asset.insuranceProvider,
    'Asset details must show the insurer from the company-scoped API response.');
  assert.equal(await page.locator('#detailInsurancePolicyNumber').innerText(), asset.insurancePolicyNumber,
    'Asset details must show the policy number from the company-scoped API response.');
  assert.notEqual(await page.locator('#detailInsuranceExpiresAt').innerText(), 'Belirtilmedi',
    'Asset details must format the insurance expiry date.');
  assert.equal(await page.locator('#detailAssetAge').innerText(), 'Yıl: 1, Ay: 9',
    'Asset details must show elapsed service age from the API valuation in years and months.');
  assert.equal(await page.locator('#detailDepreciationProgress').innerText(), 'Geçen 21 / 60 ay · kalan 39 ay',
    'The asset overview must render elapsed and remaining months from the server valuation.');
  assert.match(await page.locator('#detailCurrentValue').innerText(), /1[.\u00a0\u202f]?625,00/,
    'The asset overview must show the estimated book value in the asset currency.');
  assert.match(await page.locator('#detailCurrentValueNote').innerText(), /tahmini defter değeri/,
    'The UI must label the amount as an estimate, not accounting or market value.');
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening and closing row detail must remain read-only.');

  assetValuation = { ageMonths: 21, depreciationMonths: null, remainingMonths: null, currentValue: null };
  await assetRow.locator('.asset-name').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#detailDepreciationProgress').innerText(), 'Model amortismanı tanımlanmamış',
    'The asset overview must explain when its model has no depreciation policy.');
  assert.equal(await page.locator('#detailCurrentValue').innerText(), 'Hesaplanamıyor',
    'The asset overview must not invent a value when required depreciation inputs are missing.');
  assert.match(await page.locator('#detailCurrentValueNote').innerText(), /alış tarihi, maliyet ve model amortismanı gerekir/);
  assetValuation = { ageMonths: null, depreciationMonths: 60, remainingMonths: null, currentValue: null };
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  await assetRow.locator('.asset-name').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#detailAssetAge').innerText(), 'Alış tarihi yok');
  assert.equal(await page.locator('#detailDepreciationProgress').innerText(), 'Alış tarihi yok');
  assert.equal(await page.locator('#detailCurrentValue').innerText(), 'Hesaplanamıyor');
  assetValuation = { ageMonths: 21, depreciationMonths: 60, remainingMonths: 39, currentValue: 1625 };
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });

  await assetRow.locator('[data-asset-action="edit"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const rowEditDialog = page.locator('#editAssetDialog');
  await rowEditDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#editAssetForm [name="assetName"]').inputValue(), asset.name,
    'The row edit shortcut must load the selected asset data.');
  await page.locator('[data-close-edit-asset]').first().click();
  await rowEditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Canceling row edit must not change the asset.');
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });

  await assetRow.locator('[data-asset-action="label"]').click();
  const rowLabelPreview = page.locator('#assetLabelPreviewDialog');
  await rowLabelPreview.waitFor({ state: 'visible' });
  await page.waitForFunction(() => !document.querySelector('#assetLabelPreviewPrint')?.disabled);
  assert.match(await page.locator('#assetLabelPreviewFrame').getAttribute('srcdoc'), /QA-ASSET-001/,
    'The row label shortcut must render the selected asset tag in its printable preview.');
  await page.locator('#assetLabelPreviewClose').click();
  await rowLabelPreview.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Previewing a row label must not write to the API or launch the printer.');

  await assetRow.locator('[data-asset-action="assign"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const rowAssignmentDialog = page.locator('#assignDialog');
  await rowAssignmentDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assignForm [name="employeeId"] option').count(), 2, 'The row assignment shortcut must open the same company-scoped employee choices as the detail action.');
  await page.locator('[data-close-assign]').first().click();
  await rowAssignmentDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Canceling row assignment must not write.');
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });

  await assetRow.locator('[data-asset-action="audit"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const rowAuditDialog = page.locator('#assetAuditDialog');
  await rowAuditDialog.waitFor({ state: 'visible' });
  assert.equal(await rowAuditDialog.getAttribute('data-bulk-mode'), 'false');
  assert.equal(await page.locator('#assetAuditForm').evaluate(form => form.querySelector('input[name="auditDate"]')?.value?.length), 10,
    'The row audit shortcut must prepare a valid single-asset audit date.');
  const beforeRowAuditCancel = apiRequests.length;
  await page.locator('[data-close-asset-audit]').first().click();
  await rowAuditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeRowAuditCancel, 'Canceling the row audit shortcut must not record an audit.');
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });

  await assetRow.click({ button: 'right' });
  const contextMenu = page.locator('#assetContextMenu');
  await contextMenu.waitFor({ state: 'visible' });
  const contextItems = await contextMenu.getByRole('menuitem').allTextContents();
  assert.ok(contextItems.includes('Varlığı düzenle'));
  assert.ok(contextItems.includes('Çalışana zimmetle'));
  assert.ok(contextItems.includes('Varlığı kopyala'));
  const menuBounds = await contextMenu.boundingBox();
  assert.ok(menuBounds && menuBounds.x >= 0 && menuBounds.y >= 0 && menuBounds.x + menuBounds.width <= 1280 && menuBounds.y + menuBounds.height <= 900,
    'Right-click menu must be positioned inside the viewport.');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Escape');
  await contextMenu.waitFor({ state: 'hidden' });
  assert.equal(await assetRow.evaluate(row => row === document.activeElement), true, 'Escape should restore focus to the invoking asset row.');
  await assetRow.focus();
  await page.keyboard.press('Shift+F10');
  await contextMenu.waitFor({ state: 'visible' });
  await contextMenu.getByRole('menuitem', { name: 'Varlığı düzenle' }).click();
  const contextEditDialog = page.locator('#editAssetDialog');
  await contextEditDialog.waitFor({ state: 'visible' });
  await page.locator('[data-close-edit-asset]').first().click();
  await contextEditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening and canceling a context-menu edit must not write.');
  await page.locator('#assetDialog').locator('#closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  await assetRow.click({ button: 'right' });
  await contextMenu.waitFor({ state: 'visible' });
  await contextMenu.getByRole('menuitem', { name: 'Varlığı kopyala' }).click();
  await newAssetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#newAssetForm [name="assetName"]').inputValue(), asset.name, 'The row clone shortcut must seed the source asset name.');
  assert.equal(await page.locator('#newAssetForm [name="assetTag"]').inputValue(), '', 'A clone must not reuse the source unique asset tag.');
  assert.equal(await page.locator('#newAssetForm [name="serialNumber"]').inputValue(), '', 'A clone must not reuse the source serial number.');
  assert.equal(await page.locator('#newAssetForm [name="insuranceProvider"]').inputValue(), '', 'A clone must not reuse the source insurer.');
  assert.equal(await page.locator('#newAssetForm [name="insurancePolicyNumber"]').inputValue(), '', 'A clone must not reuse the source policy number.');
  assert.equal(await page.locator('#newAssetForm [name="insuranceExpiresAt"]').inputValue(), '', 'A clone must not reuse the source policy expiry.');
  await page.locator('#newAssetDialog [data-close-new]').first().click();
  await newAssetDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening and canceling a row clone must not create an asset.');

  await assetRow.click({ button: 'right' });
  await contextMenu.waitFor({ state: 'visible' });
  await contextMenu.getByRole('menuitem', { name: 'Varlığı kopyala' }).click();
  await newAssetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#newAssetForm [name="purchaseDate"]').inputValue(), asset.purchaseDate);
  assert.equal(await page.locator('#newAssetForm [name="warrantyExpiresAt"]').inputValue(), asset.warrantyExpiresAt);
  assert.equal(await page.locator('#newAssetForm [name="insuranceProvider"]').inputValue(), '', 'Policy-specific insurance data must be cleared on clone.');
  assert.equal(await page.locator('#newAssetForm [name="insurancePolicyNumber"]').inputValue(), '', 'Policy-specific insurance data must be cleared on clone.');
  assert.equal(await page.locator('#newAssetForm [name="insuranceExpiresAt"]').inputValue(), '', 'Policy-specific insurance data must be cleared on clone.');
  assert.equal(await page.locator('#newAssetForm [name="purchaseCost"]').inputValue(), String(asset.purchaseCost));
  assert.equal(await page.locator('#newAssetForm [name="customField:inventory_reference"]').inputValue(), 'QA-SOURCE-REF');
  assert.equal(await page.locator('#newAssetForm [name="customField:secret_key"]').inputValue(), '',
    'Encrypted sensitive source values must not be prefilled into a cloned asset.');
  assert.equal(await page.locator('#newAssetForm .asset-custom-field-group h3').filter({ hasText: 'Güvenlik' }).count(), 1,
    'A model-scoped custom field must render under its configured group for a matching category and model.');
  assert.equal(await page.locator('#newAssetForm [name="customField:model_secret"]').count(), 1,
    'The matching model-scoped custom field must be available in the clone form.');
  await page.locator('#newAssetForm [name="assetTag"]').fill('QA-CLONE-001');
  await page.locator('#newAssetForm [name="serialNumber"]').fill('QA-CLONE-SERIAL-001');
  const beforeCloneSubmit = apiRequests.length;
  await page.locator('#newAssetForm button[type="submit"]').click();
  await page.waitForFunction(() => document.querySelector('#newAssetDialog')?.open === true &&
    document.querySelector('#newAssetForm [name="assetTag"]')?.value === 'QA-CLONE-001' &&
    document.querySelector('#newAssetForm button[type="submit"]')?.disabled === false);
  assert.equal(apiRequests.length, beforeCloneSubmit + 1, 'A failed clone save must issue exactly one create request.');
  assert.equal(await page.locator('#newAssetForm [name="serialNumber"]').inputValue(), 'QA-CLONE-SERIAL-001',
    'A failed create must preserve the clone form so the user can retry.');
  await page.locator('#newAssetForm button[type="submit"]').click();
  await newAssetDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeCloneSubmit + 2, 'Retrying an explicit failed clone create must issue one additional create request.');
  assert.deepEqual(assetCreateWrites.slice(-2).map(item => item.csrf), ['synthetic-asset-csrf', 'synthetic-asset-csrf']);
  assert.equal(clonedAssets.length, 1, 'The confirmed retry must create one synthetic asset.');
  assert.equal(assetCreateWrites.at(-1).body.insuranceProvider, null);
  assert.equal(assetCreateWrites.at(-1).body.insurancePolicyNumber, null);
  assert.equal(assetCreateWrites.at(-1).body.insuranceExpiresAt, null);
  await page.waitForFunction(() => [...document.querySelectorAll('#assetRows tr')].some(row => row.innerText.includes('QA-CLONE-001')));
  assert.equal(await page.locator('#assetRows tr').filter({ hasText: 'QA-CLONE-001' }).count(), 1,
    'The cloned asset must appear in the refreshed company inventory.');

  await assetRow.locator('[data-asset-action="archive"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const rowArchiveDialog = page.locator('#assetTrashConfirmDialog');
  await rowArchiveDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetTrashConfirmSummary').innerText(), /QA-ASSET-001/,
    'The row archive shortcut must require explicit confirmation for the selected asset.');
  const beforeRowArchiveCancel = apiRequests.length;
  await page.locator('#cancelAssetTrashConfirm').click();
  await rowArchiveDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeRowArchiveCancel, 'Canceling the row archive shortcut must not archive the asset.');
  await page.locator('#assetDialog #closeDialog').click();

  await assetRow.locator('.asset-name').click();
  const assetDialog = page.locator('#assetDialog');
  await assetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#dialogTitle').innerText(), asset.name);
  await page.locator('#copyAssetTag').click();
  await page.waitForFunction(() => window.__syntheticClipboard === 'QA-ASSET-001');
  assert.equal(await page.evaluate(() => window.__syntheticClipboard), 'QA-ASSET-001', 'Copy-tag action must place the selected asset tag on the clipboard.');
  const beforeDetailLabelPreview = apiRequests.length;
  await page.locator('#printAssetLabel').click();
  const detailLabelPreview = page.locator('#assetLabelPreviewDialog');
  await detailLabelPreview.waitFor({ state: 'visible' });
  await page.waitForFunction(() => !document.querySelector('#assetLabelPreviewPrint')?.disabled);
  const detailLabelHtml = await page.locator('#assetLabelPreviewFrame').getAttribute('srcdoc');
  assert.match(detailLabelHtml, /QA-ASSET-001/, 'Detail label action must print the selected asset tag.');
  assert.match(detailLabelHtml, /Synthetic audit target/, 'Detail label action must print the selected asset name.');
  await page.locator('#assetLabelPreviewClose').click();
  await detailLabelPreview.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeDetailLabelPreview, 'Detail label preview must not write to the API or start printing.');
  const statusDialog = page.locator('#assetStatusDialog');
  const beforeStatusCancel = apiRequests.length;
  await page.locator('#assetStatusAction').selectOption('Unusable');
  await statusDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetStatusSummary').innerText(), /QA-ASSET-001[\s\S]*Kullanılamaz/);
  await page.locator('#cancelAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeStatusCancel, 'Canceling an asset status change must not write.');
  await page.locator('#assetStatusAction').selectOption('Unusable');
  await statusDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusForm [name="note"]').fill('Synthetic status reason');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  await assetRow.locator('.asset-name').waitFor({ state: 'visible' });
  const statusWrite = apiRequests.at(-1);
  assert.equal(statusWrite.method, 'PATCH');
  assert.equal(statusWrite.path, `/api/assets/${assetId}/status`);
  assert.deepEqual(statusWrite.body, { status: 'Unusable', note: 'Synthetic status reason' });
  assert.equal(statusWrite.csrf, 'synthetic-asset-csrf');
  assert.match(await page.locator('#assetRows').innerText(), /Kullanılamaz/);
  assert.equal(asset.status, 'Unusable');
  await assetRow.locator('.asset-name').click();
  await assetDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusAction').selectOption('Available');
  await statusDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusForm [name="note"]').fill('Reset synthetic test fixture');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  await assetRow.locator('.asset-name').waitFor({ state: 'visible' });
  assert.equal(apiRequests.at(-1).path, `/api/assets/${assetId}/status`);
  assert.deepEqual(apiRequests.at(-1).body, { status: 'Available', note: 'Reset synthetic test fixture' });
  assert.equal(asset.status, 'Available', 'The status action must allow an authorized return to Available.');
  await assetRow.locator('.asset-name').click();
  await assetDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusAction').selectOption('Archived');
  await statusDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusForm [name="note"]').fill('Synthetic archive before restore test');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  assert.equal(asset.status, 'Archived');
  await assetRow.locator('.asset-name').click();
  await assetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#restoreAssetButton').isVisible(), true, 'The restore action must appear only on an archived asset.');
  await page.locator('#restoreAssetButton').click();
  await statusDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetStatusSummary').innerText(), /QA-ASSET-001[\s\S]*arşiv/);
  await page.locator('#assetStatusForm [name="note"]').fill('Synthetic restore verified');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#detailStatus')?.textContent === 'Kullanılabilir');
  const restoreWrite = apiRequests.at(-1);
  assert.equal(restoreWrite.method, 'POST');
  assert.equal(restoreWrite.path, `/api/assets/${assetId}/restore`);
  assert.deepEqual(restoreWrite.body, { status: 'Available', note: 'Synthetic restore verified' });
  assert.equal(restoreWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.status, 'Available', 'Restoring the archived asset must persist the available state in the synthetic backend.');
  const tabCases = [
    ['relations', '#assetTabRelations', /Bağlantılı kayıtlar/],
    ['maintenance', '#assetTabMaintenance', /Bakım kayıtları/],
    ['history', '#assetTabHistory', /İşlem geçmişi/],
    ['attachments', '#assetTabAttachments', /Fotoğraflar ve ek dosyalar/],
    ['software', '#assetTabSoftware', /Kurulu yazılımlar/]
  ];
  for (const [tab, panel, heading] of tabCases) {
    const tabButton = page.locator(`[data-asset-tab="${tab}"]`);
    await tabButton.click();
    assert.equal(await tabButton.getAttribute('aria-selected'), 'true', `${tab} tab must become selected.`);
    assert.equal(await page.locator(panel).isVisible(), true, `${tab} panel must be visible.`);
    assert.match(await page.locator(panel).innerText(), heading);
  }
  await page.locator('#assetSoftwareInventoryRows').getByText('Synthetic Inventory Agent').waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetSoftwareInventoryRows').innerText(), /Synthetic Inventory Agent/,
    'The installed software tab must load the asset-scoped inventory rows.');
  assert.ok(requests.some(item => item.method === 'GET' && item.path === `/api/assets/${assetId}/installed-software`),
    'Opening the installed software tab must request inventory for the current asset.');
  await page.locator('#assetTabOverviewButton').focus();
  await page.keyboard.press('End');
  assert.equal(await page.locator('#assetTabSoftwareButton').getAttribute('aria-selected'), 'true', 'End must select the last asset detail tab.');
  await page.keyboard.press('Home');
  assert.equal(await page.locator('#assetTabOverviewButton').getAttribute('aria-selected'), 'true', 'Home must select the first asset detail tab.');

  const editDialog = page.locator('#editAssetDialog');
  const beforeEditCancel = apiRequests.length;
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  const editForm = page.locator('#editAssetForm');
  assert.equal(await editForm.locator('[name="assetName"]').inputValue(), 'Synthetic audit target');
  assert.equal(await editForm.locator('[name="categoryId"]').inputValue(), asset.categoryId);
  assert.equal(await editForm.locator('[name="insuranceProvider"]').inputValue(), asset.insuranceProvider);
  assert.equal(await editForm.locator('[name="insurancePolicyNumber"]').inputValue(), asset.insurancePolicyNumber);
  assert.equal(await editForm.locator('[name="insuranceExpiresAt"]').inputValue(), asset.insuranceExpiresAt);
  await page.locator('[data-close-edit-asset]').first().click();
  await editDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeEditCancel, 'Canceling asset edit must not write.');

  const editedName = 'Synthetic asset edit persisted';
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  await editForm.locator('[name="assetName"]').fill(editedName);
  await editForm.locator('[name="insuranceProvider"]').fill('Updated Synthetic Insurer');
  await editForm.locator('[name="insurancePolicyNumber"]').fill('QA-POLICY-002');
  await editForm.locator('[name="insuranceExpiresAt"]').fill('2029-06-30');
  assert.equal(await editForm.locator('[name="assetName"]').evaluate(input => input.checkValidity()), true);
  await editForm.locator('button[type="submit"]').click();
  await editDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(apiRequests.length, beforeEditCancel + 1, 'Only the explicit asset edit should write before the audit flows.');
  const editWrite = apiRequests.at(-1);
  assert.equal(editWrite.method, 'PATCH');
  assert.equal(editWrite.path, `/api/assets/${assetId}`);
  assert.equal(editWrite.body.name, editedName);
  assert.equal(editWrite.body.insuranceProvider, 'Updated Synthetic Insurer');
  assert.equal(editWrite.body.insurancePolicyNumber, 'QA-POLICY-002');
  assert.equal(editWrite.body.insuranceExpiresAt, '2029-06-30');
  assert.equal(editWrite.body.expectedUpdatedAt, '2026-10-14T13:30:00Z');
  assert.equal(editWrite.csrf, 'synthetic-asset-csrf');
  await page.locator('#assetDialog').locator('#closeDialog').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).waitFor({ state: 'visible' });
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(asset.name, editedName, 'Reloaded list and detail must reflect the persisted server result.');
  assert.equal(asset.insuranceProvider, 'Updated Synthetic Insurer', 'The synthetic server must persist the insurer field.');
  assert.equal(asset.insurancePolicyNumber, 'QA-POLICY-002', 'The synthetic server must persist the policy number.');
  assert.equal(asset.insuranceExpiresAt, '2029-06-30', 'The synthetic server must persist the insurance expiry.');

  await page.locator('#scheduleAssetAuditButton').click();
  const scheduleDialog = page.locator('#assetAuditScheduleDialog');
  await scheduleDialog.waitFor({ state: 'visible' });
  const beforeScheduleCancel = apiRequests.length;
  await page.locator('[data-close-audit-schedule]').first().click();
  await scheduleDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeScheduleCancel, 'Canceling the schedule must not send a write request.');

  await page.locator('#scheduleAssetAuditButton').click();
  const date = new Date(); date.setDate(date.getDate() + 7);
  const nextAuditOn = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').fill(nextAuditOn);
  assert.equal(await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').inputValue(), nextAuditOn);
  assert.equal(await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').evaluate(input => input.checkValidity()), true);
  await page.locator('#assetAuditScheduleForm button[type="submit"]').click();
  try { await scheduleDialog.waitFor({ state: 'hidden', timeout: 10000 }); }
  catch (error) {
    console.error('AUDIT_SCHEDULE_STATE', JSON.stringify({
      apiRequests, requests: requests.map(({ method, path, body }) => ({ method, path, body })),
      dateValue: await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').inputValue(),
      feedback: await page.locator('[role="status"]').allTextContents(), pageErrors
    }));
    throw error;
  }
  await page.waitForFunction(expected => document.querySelector('#detailNextAuditOn')?.textContent !== 'Planlanmadı' && document.querySelector('#assetDialog')?.open, nextAuditOn);

  assert.equal(apiRequests.length, beforeScheduleCancel + 1, 'Only the explicit asset edit and schedule should write.');
  assert.equal(apiRequests.at(-1).method, 'PUT');
  assert.equal(apiRequests.at(-1).path, `/api/assets/${assetId}/audit-schedule`);
  assert.deepEqual(apiRequests.at(-1).body, { nextAuditOn });
  assert.equal(apiRequests.at(-1).csrf, 'synthetic-asset-csrf');
  assert.equal(asset.nextAuditOn, nextAuditOn, 'Reopened detail must reflect the persisted server result.');

  const beforeAuditCancel = apiRequests.length;
  await page.locator('#auditAssetButton').click();
  const auditDialog = page.locator('#assetAuditDialog');
  await auditDialog.waitFor({ state: 'visible' });
  await page.locator('[data-close-asset-audit]').first().click();
  await auditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeAuditCancel, 'Canceling a physical audit must not send a write request.');

  await page.locator('#auditAssetButton').click();
  const auditDate = await page.evaluate(() => {
    const date = new Date(); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); return date.toISOString().slice(0, 10);
  });
  const auditNotes = 'Synthetic audit saved from Chromium';
  await page.locator('#assetAuditForm [name="auditDate"]').fill(auditDate);
  await page.locator('#assetAuditForm [name="nextAuditOn"]').fill(nextAuditOn);
  await page.locator('#assetAuditForm [name="notes"]').fill(auditNotes);
  await page.locator('#assetAuditForm button[type="submit"]').click();
  await auditDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetHistory .asset-history-item')?.textContent.includes('Synthetic audit saved from Chromium'));
  assert.equal(apiRequests.length, beforeAuditCancel + 1, 'Only the explicitly saved physical audit should write.');
  const auditRequest = apiRequests.at(-1);
  assert.equal(auditRequest.method, 'POST');
  assert.equal(auditRequest.path, `/api/assets/${assetId}/audit`);
  assert.deepEqual(auditRequest.body, { auditDate, nextAuditOn, notes: auditNotes, outcome: 'Accepted', issueDescription: null });
  assert.equal(auditRequest.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.nextAuditOn, nextAuditOn);
  assert.equal(assetHistory[0].eventType, 'Audited');
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic audit saved from Chromium/);

  await page.locator('#auditAssetButton').click();
  await auditDialog.waitFor({ state: 'visible' });
  await page.locator('#auditOutcome').selectOption('Unacceptable');
  const issueDescription = 'Synthetic cracked casing finding';
  assert.equal(await page.locator('#auditIssueDescription').isVisible(), true, 'Unacceptable outcomes must reveal the required finding field.');
  assert.equal(await page.locator('#auditIssueDescription').getAttribute('required'), '', 'The finding description must be browser-required.');
  await page.locator('#auditIssueDescription').fill(issueDescription);
  await page.locator('#assetAuditForm button[type="submit"]').click();
  await auditDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetHistory .asset-history-item')?.textContent.includes('Synthetic cracked casing finding'));
  const unacceptableAuditWrite = apiRequests.at(-1);
  assert.deepEqual(unacceptableAuditWrite.body, { auditDate, nextAuditOn: null, notes: null, outcome: 'Unacceptable', issueDescription });
  assert.equal(unacceptableAuditWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(assetHistory[0].eventType, 'AuditUnacceptable');
  assert.equal(asset.status, 'Available', 'An unacceptable audit finding is recorded without silently changing asset status.');

  const beforeMaintenanceCancel = apiRequests.length;
  await page.locator('#addMaintenanceButton').click();
  const maintenanceDialog = page.locator('#maintenanceStartDialog');
  await maintenanceDialog.waitFor({ state: 'visible' });
  await page.locator('[data-close-maintenance-start]').first().click();
  await maintenanceDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeMaintenanceCancel, 'Canceling maintenance setup must not write.');

  await page.locator('#addMaintenanceButton').click();
  await maintenanceDialog.waitFor({ state: 'visible' });
  const maintenanceForm = page.locator('#maintenanceStartForm');
  await maintenanceForm.locator('[name="maintenanceType"]').fill('Synthetic scheduled inspection');
  await maintenanceForm.locator('[name="supplier"]').fill('QA service partner');
  await maintenanceForm.locator('[name="mode"]').selectOption('scheduled');
  const plannedOn = await maintenanceForm.locator('[name="plannedOn"]').inputValue();
  await maintenanceForm.locator('[name="notes"]').fill('Synthetic maintenance note');
  assert.equal(await maintenanceForm.locator('[name="plannedOn"]').evaluate(input => input.checkValidity()), true);
  await maintenanceForm.locator('button[type="submit"]').click();
  await maintenanceDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetTabMaintenance')?.hidden === false);
  assert.equal(maintenanceRows[0].status, 'Scheduled');
  assert.match(await page.locator('#assetMaintenanceRows').innerText(), /Synthetic scheduled inspection/);
  assert.match(await page.locator('#assetMaintenanceRows').innerText(), /Planlandı/);
  const scheduleWrite = apiRequests.at(-1);
  assert.equal(scheduleWrite.method, 'POST');
  assert.equal(scheduleWrite.path, `/api/assets/${assetId}/maintenances/schedule`);
  assert.deepEqual(scheduleWrite.body, { maintenanceType: 'Synthetic scheduled inspection', supplier: 'QA service partner',
    description: null, plannedOn, cost: null, currency: 'TRY', notes: 'Synthetic maintenance note', responsibleEmployeeId: null });
  assert.equal(scheduleWrite.csrf, 'synthetic-asset-csrf');

  await page.locator('#assetMaintenanceRows').getByRole('button', { name: 'Bakımı başlat' }).click();
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetMaintenanceRows')?.textContent.includes('Devam ediyor'));
  assert.equal(asset.status, 'Maintenance');
  const startWrite = apiRequests.at(-1);
  assert.equal(startWrite.method, 'POST');
  assert.equal(startWrite.path, `/api/assets/${assetId}/maintenances/${maintenanceId}/start`);
  assert.equal(startWrite.csrf, 'synthetic-asset-csrf');

  await page.locator('#assetMaintenanceRows').getByRole('button', { name: 'Bakımı tamamla' }).click();
  const completeDialog = page.locator('#maintenanceCompleteDialog');
  await completeDialog.waitFor({ state: 'visible' });
  const beforeCompleteCancel = apiRequests.length;
  await page.locator('[data-close-maintenance-complete]').first().click();
  await completeDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeCompleteCancel, 'Canceling maintenance completion must not write.');
  await page.locator('#assetMaintenanceRows').getByRole('button', { name: 'Bakımı tamamla' }).click();
  await completeDialog.waitFor({ state: 'visible' });
  const completedOn = await page.locator('#maintenanceCompleteForm [name="completedOn"]').inputValue();
  await page.locator('#maintenanceCompleteForm [name="notes"]').fill('Synthetic completion note');
  await page.locator('#maintenanceCompleteForm button[type="submit"]').click();
  await completeDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetMaintenanceRows')?.textContent.includes('Tamamlandı'));
  assert.equal(asset.status, 'Available');
  assert.match(await page.locator('#assetMaintenanceRows').innerText(), /Synthetic completion note/);
  const completeWrite = apiRequests.at(-1);
  assert.equal(completeWrite.method, 'POST');
  assert.equal(completeWrite.path, `/api/assets/${assetId}/maintenances/${maintenanceId}/complete`);
  assert.deepEqual(completeWrite.body, { completedOn, notes: 'Synthetic completion note' });
  assert.equal(completeWrite.csrf, 'synthetic-asset-csrf');

  const beforeTransferCancel = apiRequests.length;
  await page.locator('#transferAssetButton').click();
  const transferDialog = page.locator('#assetTransferDialog');
  await transferDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#transferSourceLocation').inputValue(), 'Head Office');
  await page.locator('[data-close-asset-transfer]').first().click();
  await transferDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeTransferCancel, 'Canceling a location transfer must not write.');
  await page.locator('#transferAssetButton').click();
  await transferDialog.waitFor({ state: 'visible' });
  const targetLocation = 'Synthetic QA Annex';
  await page.locator('#assetTransferForm [name="targetLocation"]').fill(targetLocation);
  await page.locator('#assetTransferForm [name="notes"]').fill('Synthetic transfer note');
  await page.locator('#assetTransferForm button[type="submit"]').click();
  await transferDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(location => document.querySelector('#assetDialog')?.open && document.querySelector('#detailLocation')?.textContent === location, targetLocation);
  assert.equal(asset.location, targetLocation);
  const transferWrite = apiRequests.at(-1);
  assert.equal(transferWrite.method, 'POST');
  assert.equal(transferWrite.path, `/api/assets/${assetId}/transfer`);
  assert.deepEqual(transferWrite.body, { targetLocation, notes: 'Synthetic transfer note' });
  assert.equal(transferWrite.csrf, 'synthetic-asset-csrf');
  await page.locator('#assetTabHistoryButton').click();
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic QA Annex/);

  const beforeAssignCancel = apiRequests.length;
  const assignDialog = page.locator('#assignDialog');
  await page.locator('#assignAssetButton').click();
  await assignDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assignForm [name="employeeId"] option').count(), 2, 'Assignment dialog must load the company active-employee choice.');
  await page.locator('[data-close-assign]').first().click();
  await assignDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeAssignCancel, 'Canceling assignment must not write.');
  await page.locator('#assignAssetButton').click();
  await assignDialog.waitFor({ state: 'visible' });
  const expectedReturnOn = await page.evaluate(() => {
    const date = new Date(); date.setDate(date.getDate() + 30); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  });
  await page.locator('#assignForm [name="employeeId"]').selectOption(employeeId);
  await page.locator('#assignForm [name="expectedReturnOn"]').fill(expectedReturnOn);
  await page.locator('#assignForm [name="note"]').fill('Synthetic assignment note');
  await page.locator('#assignForm button[type="submit"]').click();
  await assignDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).waitFor({ state: 'visible' });
  const assignmentWrite = apiRequests.at(-1);
  assert.equal(assignmentWrite.method, 'POST');
  assert.equal(assignmentWrite.path, `/api/assets/${assetId}/assign`);
  assert.deepEqual(assignmentWrite.body, { employeeId, note: 'Synthetic assignment note', expectedReturnOn });
  assert.equal(assignmentWrite.csrf, 'synthetic-asset-csrf');
  assert.match(await page.locator('#assetRows').innerText(), /Synthetic QA Employee/);
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#detailEmployee')?.textContent.includes('Synthetic QA Employee'));
  assert.equal(asset.status, 'Deployed');

  const detailReferenceFilters = [
    { selector: '#detailCategory', label: 'Synthetic equipment', queryKey: 'categoryId', value: asset.categoryId },
    { selector: '#detailBrand', label: 'Synthetic Brand', queryKey: 'brandId', value: brandId },
    { selector: '#detailModel', label: 'Synthetic Model Detail', queryKey: 'modelId', value: modelId },
    { selector: '#detailEmployee', label: 'Synthetic QA Employee', queryKey: 'employeeId', value: employeeId },
    { selector: '#detailLocation', label: targetLocation, queryKey: 'location', value: targetLocation }
  ];
  for (const [index, reference] of detailReferenceFilters.entries()) {
    if (index > 0) {
      await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
      await page.waitForFunction(() => document.querySelector('#assetDialog')?.open);
    }
    const referenceLink = page.locator(reference.selector).getByRole('button');
    assert.equal(await referenceLink.innerText(), reference.label, `${reference.selector} must render its linked company reference.`);
    const filteredList = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname === '/api/assets' && url.searchParams.get(reference.queryKey) === reference.value;
    });
    await referenceLink.click();
    assert.equal((await filteredList).status(), 200, `${reference.selector} must navigate to a successful filtered asset list.`);
    await page.waitForFunction(label => document.querySelector('#assetRelationFilter')?.textContent.includes(label), reference.label);
    const matchingQuery = requests.filter(item => item.method === 'GET' && item.path === '/api/assets').at(-1)?.query;
    assert.equal(matchingQuery?.[reference.queryKey], reference.value, `${reference.selector} must apply the correct ${reference.queryKey} filter.`);
    const clearFilters = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname === '/api/assets' && url.searchParams.get(reference.queryKey) === null;
    });
    await page.locator('#clearAssetRelationFilter').click();
    assert.equal((await clearFilters).status(), 200, 'Clearing a detail reference filter must restore the unfiltered asset list.');
    await page.waitForFunction(() => document.querySelector('#clearAssetRelationFilter')?.hidden === true);
  }
  console.log('PASS: asset detail category, brand, model, employee, and location references each open the matching company-scoped filter; clearing restores the complete asset list.');
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open);
  await page.locator('#assetTabRelationsButton').click();
  const parentLookup = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets' && url.searchParams.get('q') === 'Synthetic QA Parent';
  });
  await page.locator('#assetParentSearch').fill('Synthetic QA Parent');
  assert.equal((await parentLookup).status(), 200, 'Same-company parent search must succeed.');
  const parentSelect = page.locator('#assetParentSelect');
  assert.equal(await parentSelect.locator('option').count(), 2, 'Only the empty option and matching same-company parent should be offered.');
  assert.equal(await parentSelect.locator('option').nth(1).textContent(), 'QA-PARENT-001 · Synthetic QA Parent');
  await parentSelect.selectOption(hierarchyParentId);
  const parentSave = page.waitForResponse(response => response.url().includes(`/api/assets/${assetId}/parent`));
  await page.locator('#saveAssetParent').click();
  assert.equal((await parentSave).status(), 200, 'Saving a valid parent link must succeed.');
  const parentWrite = apiRequests.at(-1);
  assert.equal(parentWrite.method, 'PATCH');
  assert.equal(parentWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(parentWrite.body.parentAssetId, hierarchyParentId);
  assert.ok(parentWrite.body.expectedUpdatedAt, 'Parent update must include the asset version.');
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetLinkedRows')?.innerText.includes('Synthetic QA Parent') && document.querySelector('#assetLinkedRows')?.innerText.includes('Synthetic QA Child'));
  assert.equal(await page.locator('#assetParentSelect').inputValue(), hierarchyParentId, 'Saved parent must remain selected after the detail refresh.');

  await page.locator('#assetTabRelationsButton').click();
  const childLookup = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets' && url.searchParams.get('q') === 'Synthetic QA Child';
  });
  await page.locator('#assetParentSearch').fill('Synthetic QA Child');
  assert.equal((await childLookup).status(), 200, 'Descendant lookup must return the company-scoped candidate for the cycle rejection check.');
  assert.equal(await parentSelect.locator('option').count(), 2, 'A same-company descendant is searchable as a candidate, then rejected safely by the API.');
  await parentSelect.selectOption(hierarchyChildId);
  const cycleWrite = page.waitForResponse(response => response.url().includes(`/api/assets/${assetId}/parent`));
  await page.locator('#saveAssetParent').click();
  assert.equal((await cycleWrite).status(), 400, 'A descendant cannot be assigned as its own ancestor.');
  await page.locator('#toast').waitFor({ state: 'visible' });
  assert.match(await page.locator('#toast').innerText(), /döngü/i, 'The cycle rejection must be visible to the user.');
  assert.equal(asset.parentAssetId, hierarchyParentId, 'Rejected cycle must preserve the prior valid hierarchy.');
  assert.match(await page.locator('#assetLinkedRows').innerText(), /Synthetic QA Parent/);
  console.log('PASS: asset hierarchy search, CSRF/version-checked parent save, refreshed parent/child relationships, and visible cycle rejection preserving the previous link.');
  await page.locator('#assetTabRelationsButton').click();
  assert.equal(await page.locator('#assetAccessoryCount').innerText(), '1 açık');
  assert.equal(await page.locator('#assetComponentCount').innerText(), '1 takılı');
  const accessoryRows = page.locator('#assetAccessoryAssignments tr');
  assert.equal(await accessoryRows.count(), 1, 'Only currently outstanding accessory assignments belong in the asset detail.');
  assert.match(await accessoryRows.innerText(), /Synthetic open headset/);
  assert.match(await accessoryRows.innerText(), /2 \/ 3/);
  assert.match(await accessoryRows.innerText(), /Headset and adapter/);
  const componentRows = page.locator('#assetComponentAllocations tr');
  assert.equal(await componentRows.count(), 1, 'Only components still installed on the asset belong in the detail.');
  assert.match(await componentRows.innerText(), /Synthetic installed memory/);
  assert.match(await componentRows.innerText(), /3 \/ 5/);
  assert.match(await componentRows.innerText(), /Slot A1\/A2/);
  console.log('PASS: asset relations tab renders only active accessory deliveries and installed components with remaining/total quantities, dates, notes, and counts.');
  await page.locator('#assetTabHistoryButton').click();
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic assignment note/);
  const maxLengthNote = 'N'.repeat(2000);
  const assetNoteInput = page.locator('#assetNoteInput');
  assert.equal(await assetNoteInput.getAttribute('maxlength'), '2000', 'The asset history note editor must expose the documented 2,000 character boundary.');
  await assetNoteInput.fill(maxLengthNote);
  const noteResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/assets/${assetId}/notes`);
  await page.locator('#assetNoteForm [type="submit"]').click();
  assert.equal((await noteResponse).status(), 201, 'A note at the 2,000 character limit must be accepted.');
  await page.waitForFunction(() => document.querySelector('#assetNoteInput')?.value === ''
    && document.querySelector('#assetHistory .asset-history-item')?.textContent.includes('N'.repeat(2000)));
  assert.equal(assetNoteWrites.at(-1).csrf, 'synthetic-asset-csrf');
  assert.deepEqual(assetNoteWrites.at(-1).body, { note: maxLengthNote });
  assert.equal(await assetNoteInput.inputValue(), '', 'A saved note must clear the editor after the history refresh.');
  assert.ok((await page.locator('#assetHistory .asset-history-item').first().innerText()).includes(maxLengthNote),
    'A successfully saved note must appear as a real NoteAdded movement in refreshed asset history.');
  console.log('PASS: asset history note saves a CSRF-protected NoteAdded movement, accepts exactly 2,000 characters, refreshes the timeline, and clears the editor.');

  const latestEvent = [...assetHistory].sort((left, right) => Date.parse(right.occurredAt) - Date.parse(left.occurredAt))[0];
  assert.ok(latestEvent?.id, 'Synthetic assignment must create a history event that can receive a document.');
  await page.locator('#assetHistory .asset-history-item').first().getByRole('button', { name: /Bu harekete belge ekle/ }).click();
  assert.equal(await page.locator('#assetTabAttachmentsButton').getAttribute('aria-selected'), 'true', 'History shortcut must navigate to attachments.');
  assert.equal(await page.locator('#assetAttachmentEvent').inputValue(), latestEvent.id, 'History shortcut must bind upload to the selected event.');
  const uploadForm = page.locator('#assetAttachmentForm');
  assert.equal(await uploadForm.isVisible(), true);
  const darkAttachmentSurfaces = await page.evaluate(() => {
    document.documentElement.dataset.theme = 'dark';
    const color = selector => getComputedStyle(document.querySelector(selector)).backgroundColor;
    return {
      dialog: color('#assetDialog'), tabs: color('#assetDialog .asset-tabs'),
      form: color('#assetAttachmentForm'), footer: color('#assetActions')
    };
  });
  assert.deepEqual(darkAttachmentSurfaces, {
    dialog: 'rgb(17, 27, 42)', tabs: 'rgb(21, 31, 48)', form: 'rgb(24, 38, 58)', footer: 'rgb(21, 31, 48)'
  }, 'The full asset detail dialog, attachment uploader, tabs, and action footer must keep a consistent dark theme.');
  const lightAttachmentSurfaces = await page.evaluate(() => {
    document.documentElement.dataset.theme = 'light';
    return ['#assetDialog .asset-tabs', '#assetAttachmentForm', '#assetActions']
      .map(selector => getComputedStyle(document.querySelector(selector)).backgroundColor);
  });
  assert.deepEqual(lightAttachmentSurfaces, ['rgb(255, 255, 255)', 'rgb(255, 255, 255)', 'rgb(255, 255, 255)'],
    'The attachment uploader, tabs, and action footer must retain their light theme.');
  assert.match(await page.locator('#assetAttachmentInput').getAttribute('accept'), /application\/pdf/);
  const beforeUpload = apiRequests.length;
  await page.locator('#assetAttachmentInput').setInputFiles({ name: 'synthetic-proof.png', mimeType: 'image/png', buffer: attachmentBytes });
  assert.equal(apiRequests.length, beforeUpload, 'Selecting a file must not upload it before explicit submit.');
  await page.locator('#uploadAssetAttachment').click();
  await page.locator('#assetAttachmentGrid').getByText('synthetic-proof.png').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assetAttachmentCount').innerText(), '(1)');
  const uploadWrite = apiRequests.at(-1);
  assert.equal(uploadWrite.method, 'POST');
  assert.equal(uploadWrite.path, `/api/assets/${assetId}/attachments`);
  assert.match(uploadWrite.body.contentType, /^multipart\/form-data; boundary=/);
  assert.deepEqual(uploadWrite.body.file, { filename: 'synthetic-proof.png', contentType: 'image/png', length: attachmentBytes.length });
  assert.equal(uploadWrite.body.assetEventId, latestEvent.id);
  assert.equal(uploadWrite.csrf, 'synthetic-asset-csrf');
  await page.locator('#assetAttachmentGrid img').waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetAttachmentGrid a').getAttribute('href'), /\/file\?download=true$/);
  await page.waitForFunction(() => document.querySelector('#assetHistory .asset-history-item')?.textContent.includes('Belge ekle · 1'));
  const fileTimelineEntry = page.locator('#assetHistory .asset-file-history-item');
  assert.equal(await fileTimelineEntry.count(), 1, 'Uploaded files must appear in the unified chronological history.');
  const fileTimelineText = await fileTimelineEntry.innerText();
  assert.match(fileTimelineText, /synthetic-proof\.png[\s\S]*Synthetic Owner/,
    'The file timeline entry must show its safe filename and uploader.');
  const latestEventTitle = await page.locator('#assetHistory .asset-history-item:not(.asset-file-history-item) strong').first().innerText();
  assert.ok(fileTimelineText.includes(`Harekete bağlı: ${latestEventTitle}`),
    'The file timeline entry must identify the same activity selected for its upload.');
  assert.match(await fileTimelineEntry.locator('a').getAttribute('href'),
    new RegExp(`/api/assets/${assetId}/attachments/${attachmentId}/file\\?download=true`),
    'The timeline download action must target the same company-scoped asset attachment endpoint as the file tab.');

  await page.locator('#closeDialog').click();
  const photoListResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/assets' && requestUrl.searchParams.get('q') === asset.assetTag;
  });
  await page.locator('#assetListSearch').fill(asset.assetTag);
  await photoListResponse;
  const photoRow = page.locator(`#assetRows tr[data-id="${assetId}"]`);
  const rowThumbnail = photoRow.locator('.asset-photo-link img');
  await rowThumbnail.waitFor({ state: 'visible' });
  assert.match(await rowThumbnail.getAttribute('src'), new RegExp(`/api/assets/${assetId}/attachments/${attachmentId}/file$`),
    'The asset list must use the uploaded image attachment returned by the company asset-list API.');
  assert.deepEqual(await rowThumbnail.evaluate(image => { const { width, height } = image.getBoundingClientRect(); return { width, height, naturalWidth: image.naturalWidth }; }),
    { width: 54, height: 44, naturalWidth: 1 },
    'The list photo must render as a small thumbnail with the loaded image content.');
  await rowThumbnail.click();
  await page.locator('#imagePreviewDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#imagePreview').getAttribute('src'), new RegExp(`/api/assets/${assetId}/attachments/${attachmentId}/file$`),
    'Clicking the list thumbnail must open the same image in the preview dialog.');
  await page.locator('#closeImagePreview').click();
  await page.locator(`#assetRows tr[data-id="${assetId}"] .asset-name`).click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#assetTabAttachmentsButton').click();
  await page.locator('#assetAttachmentGrid').getByText('synthetic-proof.png').waitFor({ state: 'visible' });

  const beforeRemoveCancel = apiRequests.length;
  await page.locator('#assetAttachmentGrid button').filter({ hasText: 'Sil' }).click();
  const attachmentRemoveDialog = page.locator('#assetAttachmentRemoveDialog');
  await attachmentRemoveDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetAttachmentRemoveSummary').innerText(), /synthetic-proof\.png/);
  await page.locator('#cancelAssetAttachmentRemove').click();
  await attachmentRemoveDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeRemoveCancel, 'Canceling attachment removal must not delete a file.');
  assert.equal(await page.locator('#assetAttachmentGrid').getByText('synthetic-proof.png').isVisible(), true);
  await page.locator('#assetAttachmentGrid button').filter({ hasText: 'Sil' }).click();
  await page.locator('#submitAssetAttachmentRemove').click();
  await attachmentRemoveDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetAttachmentGrid').getByText(/henüz fotoğraf veya dosya eklenmemiş/).waitFor({ state: 'visible' });
  const deleteWrite = apiRequests.at(-1);
  assert.equal(deleteWrite.method, 'DELETE');
  assert.equal(deleteWrite.path, `/api/assets/${assetId}/attachments/${attachmentId}`);
  assert.equal(deleteWrite.csrf, 'synthetic-asset-csrf');

  await page.locator('#closeDialog').click();
  const clearPhotoSearch = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/assets' && !requestUrl.searchParams.has('q');
  });
  await page.locator('#assetListSearch').fill('');
  await clearPhotoSearch;
  await page.locator('#assetRows [data-asset-action="return"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const rowReturnDialog = page.locator('#returnAssetDialog');
  await rowReturnDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#returnAssetDescription').innerText(), /Synthetic QA Employee/,
    'The assigned row shortcut must target the current synthetic assignee.');
  const beforeBackdropReturn = apiRequests.length;
  await page.mouse.click(5, 5);
  await rowReturnDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeBackdropReturn, 'Clicking outside the return dialog must cancel without writing.');
  await page.locator('#returnAssetButton').click();
  await rowReturnDialog.waitFor({ state: 'visible' });
  const beforeRowReturnCancel = apiRequests.length;
  await page.locator('#cancelAssetReturnAction').click();
  await rowReturnDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeRowReturnCancel, 'Canceling the row return shortcut must not return the asset.');
  await page.locator('#returnAssetButton').click();
  await rowReturnDialog.waitFor({ state: 'visible' });
  await page.locator('#returnAssetForm [name="note"]').fill('Synthetic return condition checked');
  await page.locator('#submitAssetReturn').click();
  await rowReturnDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  const returnWrite = apiRequests.at(-1);
  assert.equal(returnWrite.method, 'POST');
  assert.equal(returnWrite.path, `/api/assets/${assetId}/return`);
  assert.deepEqual(returnWrite.body, { note: 'Synthetic return condition checked' });
  assert.equal(returnWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.status, 'Available');
  assert.equal(asset.assignedEmployeeId, undefined);
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#detailEmployee').innerText(), /—|atanmamış/i,
    'After a successful return, reopening the asset must not show the previous assignee.');
  await page.locator('#assetDialog #closeDialog').click();

  await page.locator('[data-group="data"]').click();
  await page.locator('[data-page="Geri yükle"]').click();
  assert.equal(await page.locator('#dataRestorePanel').isVisible(), true, 'The company owner must be able to reach the company-restore page from the sidebar.');
  await page.locator('#dataValidateFile').click();
  const restoreDialog = page.locator('#importPreviewDialog');
  await restoreDialog.waitFor({ state: 'visible' });
  const restoreFile = page.locator('#importPreviewFile');
  assert.match(await restoreFile.getAttribute('accept'), /\.json/);
  assert.doesNotMatch(await restoreFile.getAttribute('accept'), /\.csv|\.xlsx/);
  assert.equal(await page.locator('#assetImportProfile').evaluate(element => element.closest('.table-tools').hidden), true,
    'Restore-only preview must hide asset-import mapping profiles.');
  await restoreFile.setInputFiles({ name: 'synthetic-company.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(restoreArchive)) });
  await page.locator('#runImportPreview').click();
  await page.waitForFunction(() => document.querySelector('#applyCompanyImport')?.hidden === false);
  assert.match(await page.locator('#importPreviewResult').innerText(), /Synthetic Restore Source/);
  assert.match(await page.locator('#importPreviewResult').innerText(), /Varlıklar: 2/);
  assert.equal(restoreApplyWrites.length, 0, 'Preview must not apply any company data.');

  const restoreButton = page.locator('#applyCompanyImport');
  for (const cancelMode of ['escape', 'button', 'backdrop']) {
    await restoreButton.click();
    const confirmDialog = page.locator('#componentActionDialog');
    await confirmDialog.waitFor({ state: 'visible' });
    assert.equal(await page.locator('#componentActionTitle').innerText(), 'Şirket arşivi geri yüklensin mi?');
    assert.match(await page.locator('#componentActionSummary').innerText(), /boş olmalıdır[\s\S]*Fotoğraf ve belgeler[\s\S]*geri alınamaz/);
    if (cancelMode === 'escape') await page.keyboard.press('Escape');
    else if (cancelMode === 'button') await page.locator('[data-close-component-action]').first().click();
    else await page.mouse.click(2, 2);
    await confirmDialog.waitFor({ state: 'hidden' });
    assert.equal(restoreApplyWrites.length, 0, `Canceling restore confirmation via ${cancelMode} must not write company data.`);
  }
  await restoreButton.click();
  await page.locator('#componentActionDialog').waitFor({ state: 'visible' });
  await page.locator('#componentActionSubmit').click();
  await page.waitForFunction(() => document.querySelector('#importPreviewResult')?.textContent.includes('Synthetic company archive restored.'));
  assert.equal(restoreApplyWrites.length, 1, 'Only the explicit final confirmation may apply the synthetic company archive.');
  assert.deepEqual(restoreApplyWrites[0].body, restoreArchive);
  assert.equal(restoreApplyWrites[0].csrf, 'synthetic-asset-csrf');
  await page.locator('#closeImportPreview').click();
  await restoreDialog.waitFor({ state: 'hidden' });

  await page.locator('[data-group="software"]').click();
  await page.locator('[data-page="Sözleşmeler"]').click();
  const contractList = page.locator('#contractRows');
  await contractList.locator('tr[data-id]').filter({ hasText: 'Synthetic Cloud Support Agreement' }).waitFor({ state: 'visible' });
  assert.match(await contractList.innerText(), /Synthetic Supplier[\s\S]*1 varlık · 1 lisans · 0 dosya/);
  const contractSearchResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/contracts'
    && new URL(response.url()).searchParams.get('q') === 'QA-CONTRACT-001');
  await page.locator('#contractSearch').fill('QA-CONTRACT-001');
  await contractSearchResponse;
  await page.waitForFunction(() => document.querySelectorAll('#contractRows tr[data-id]').length === 1);
  assert.ok(requests.some(item => item.path === '/api/contracts' && item.query.q === 'QA-CONTRACT-001'));
  await page.locator('#contractSearch').fill('');
  await page.locator('#contractStatus').selectOption('active');
  await page.locator('#contractRows tr[data-id]').filter({ hasText: 'Synthetic Cloud Support Agreement' }).waitFor({ state: 'visible' });
  await page.locator('#contractRows tr[data-id]').filter({ hasText: 'Synthetic Cloud Support Agreement' }).locator('.contract-open').first().click();
  const contractDetail = page.locator('#contractDetailDialog');
  await contractDetail.waitFor({ state: 'visible' });
  assert.match(await page.locator('#contractDetailFields').innerText(), /QA-CONTRACT-001[\s\S]*Synthetic Supplier[\s\S]*Synthetic contract note/);
  assert.match(await page.locator('#contractDetailLinks').innerText(), /QA-CONTRACT-ASSET[\s\S]*Synthetic Office License/);
  await page.locator('#closeContractDetail').click();
  await contractDetail.waitFor({ state: 'hidden' });

  const beforeContractCancel = contractWrites.length;
  await page.locator('#newContractButton').click();
  const contractFormDialog = page.locator('#contractFormDialog');
  await contractFormDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#contractForm [name="assetIds"] option').count(), 1);
  assert.equal(await page.locator('#contractForm [name="licenseIds"] option').count(), 1);
  await page.locator('[data-close-contract]').filter({ hasText: 'Vazgeç' }).click();
  await contractFormDialog.waitFor({ state: 'hidden' });
  assert.equal(contractWrites.length, beforeContractCancel, 'Canceling a new contract must not write.');

  await page.locator('#newContractButton').click();
  await contractFormDialog.waitFor({ state: 'visible' });
  const createdContractName = 'Synthetic New Maintenance Contract';
  await page.locator('#contractForm [name="name"]').fill(createdContractName);
  await page.locator('#contractForm [name="contractNumber"]').fill('QA-CONTRACT-002');
  await page.locator('#contractForm [name="contractType"]').fill('Maintenance');
  await page.locator('#contractForm [name="supplier"]').fill('Synthetic New Supplier');
  await page.locator('#contractForm [name="startDate"]').fill('2026-10-15');
  await page.locator('#contractForm [name="endDate"]').fill('2027-10-15');
  await page.locator('#contractForm [name="cost"]').fill('2400');
  await page.locator('#contractForm [name="currency"]').selectOption('EUR');
  await page.locator('#contractForm [name="notes"]').fill('Synthetic contract creation note');
  await page.locator('#contractAssetIds').selectOption(contractAssetId);
  await page.locator('#contractLicenseIds').selectOption(contractLicenseId);
  await page.locator('#contractForm button[type="submit"]').click();
  await contractFormDialog.waitFor({ state: 'hidden' });
  await contractList.locator('tr[data-id]').filter({ hasText: createdContractName }).waitFor({ state: 'visible' });
  const contractCreateWrite = contractWrites.at(-1);
  assert.equal(contractCreateWrite.method, 'POST');
  assert.equal(contractCreateWrite.path, '/api/contracts');
  assert.deepEqual(contractCreateWrite.body, { name: createdContractName, contractNumber: 'QA-CONTRACT-002', contractType: 'Maintenance', supplierId: null,
    supplier: 'Synthetic New Supplier', startDate: '2026-10-15', endDate: '2027-10-15', cost: 2400, currency: 'EUR', notes: 'Synthetic contract creation note',
    assetIds: [contractAssetId], licenseIds: [contractLicenseId] });
  assert.equal(contractCreateWrite.csrf, 'synthetic-asset-csrf');

  const createdContractRow = contractList.locator('tr[data-id]').filter({ hasText: createdContractName });
  const contractArchiveButton = createdContractRow.locator('.contract-toggle');
  const beforeContractArchiveCancel = contractWrites.length;
  await contractArchiveButton.click();
  const contractActionDialog = page.locator('#contractActionDialog');
  await contractActionDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#contractActionTitle').innerText(), 'Sözleşmeyi arşivle');
  assert.match(await page.locator('#contractActionMessage').innerText(), /[Kk]ayıt ve dosyaları korunur/);
  await page.locator('#contractActionCancel').click();
  await contractActionDialog.waitFor({ state: 'hidden' });
  assert.equal(contractWrites.length, beforeContractArchiveCancel, 'Canceling contract archive must not write.');
  await contractArchiveButton.click();
  await contractActionDialog.waitFor({ state: 'visible' });
  await page.locator('#contractActionSubmit').click();
  await contractActionDialog.waitFor({ state: 'hidden' });
  await createdContractRow.waitFor({ state: 'hidden' });
  assert.equal(contractWrites.at(-1).path, '/api/contracts/eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee/archive');
  assert.equal(contractWrites.at(-1).csrf, 'synthetic-asset-csrf');
  await page.locator('#contractStatus').selectOption('archived');
  await contractList.locator('tr[data-id]').filter({ hasText: createdContractName }).waitFor({ state: 'visible' });
  await contractList.locator('tr[data-id]').filter({ hasText: createdContractName }).locator('.contract-toggle').click();
  await page.locator('#contractStatus').selectOption('active');
  await contractList.locator('tr[data-id]').filter({ hasText: createdContractName }).waitFor({ state: 'visible' });
  assert.equal(contractWrites.at(-1).path, '/api/contracts/eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee/restore');
  assert.equal(contractWrites.at(-1).csrf, 'synthetic-asset-csrf');

  await page.locator('#sidebar button[data-page="Yazılım lisansları"]').click();
  const licenseList = page.locator('#licenseRows');
  const baselineLicenseRow = licenseList.locator(`tr[data-id="${softwareLicenseId}"]`);
  await baselineLicenseRow.waitFor({ state: 'visible' });
  await baselineLicenseRow.locator('.license-open').first().click();
  const licenseDetail = page.locator('#licenseDialog');
  await licenseDetail.waitFor({ state: 'visible' });
  const licenseActionDialog = page.locator('#licenseActionDialog');
  const beforeLicenseAttachmentCancel = softwareLicenseWrites.length;
  await page.locator('#licenseAttachments .license-attachment-remove').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#licenseActionTitle').innerText(), 'Lisans dosyasını sil');
  await page.locator('#licenseActionCancel').click();
  await licenseActionDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseWrites.length, beforeLicenseAttachmentCancel, 'Canceling license attachment deletion must not write.');
  assert.equal(softwareLicenseAttachments.length, 1, 'Canceling deletion must preserve the synthetic attachment.');
  await page.locator('#licenseAttachments .license-attachment-remove').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  const licenseAttachmentDeleteResponse = page.waitForResponse(response => response.request().method() === 'DELETE'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}/attachments/${softwareLicenseAttachmentId}`);
  await page.locator('#licenseActionSubmit').click();
  assert.equal((await licenseAttachmentDeleteResponse).status(), 204, 'Confirmed attachment deletion must call the scoped API.');
  await page.locator('#licenseAttachments').getByText('Bu lisansa henüz dosya eklenmemiş.').waitFor({ state: 'visible' });
  assert.equal(softwareLicenseAttachments.length, 0, 'The API-backed attachment deletion must persist in refreshed license detail.');
  assert.deepEqual(softwareLicenseWrites.at(-1), { method: 'DELETE', path: `/api/software-licenses/${softwareLicenseId}/attachments/${softwareLicenseAttachmentId}`, csrf: 'synthetic-asset-csrf' });
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });

  await page.locator('#licenseImportOpen').click();
  const licenseImportDialog = page.locator('#licenseImportDialog');
  await licenseImportDialog.waitFor({ state: 'visible' });
  const licenseCsv = { name: 'synthetic-license-import.csv', mimeType: 'text/csv', buffer: Buffer.from(
    'Lisans,Yayıncı,Ürün sürümü,Tür,Tedarikçi,Koltuk,Birim maliyeti,Para birimi,Alış tarihi,Bitiş tarihi,Otomatik yenileme,Notlar\n' +
    'Synthetic Imported License,Synthetic Publisher,2,Subscription,Synthetic Import Supplier,5,,TRY,,,false,Synthetic import note\n') };
  await page.locator('#licenseImportFile').setInputFiles(licenseCsv);
  const licensePreviewResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/import/preview.csv');
  await page.locator('#previewLicenseImport').click();
  assert.equal((await licensePreviewResponse).status(), 200, 'License import must validate through the preview endpoint.');
  await page.locator('#licenseImportFeedback').getByText('1 yeni lisans doğrulandı; 1 yeni tedarikçi yalnızca aktarımı onaylarsanız kataloğa eklenecek. Önizleme kayıt eklemedi.').waitFor({ state: 'visible' });
  assert.equal(softwareLicensePreviewRequests.length, 1);
  assert.equal(softwareLicensePreviewRequests[0].csrf, 'synthetic-asset-csrf');
  assert.equal(softwareLicenseWrites.length, 1, 'Preview must not create the license or supplier.');
  assert.equal(await licenseList.locator('tr[data-id]').count(), 1, 'Preview must leave the visible license list unchanged.');

  await page.locator('#commitLicenseImport').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#licenseActionTitle').innerText(), 'Lisans kayıtlarını ekle');
  await page.locator('#licenseImportFile').setInputFiles({ name: 'replaced-license-import.csv', mimeType: 'text/csv', buffer: Buffer.from('replacement') });
  await page.locator('#licenseActionSubmit').click();
  await page.locator('#licenseActionError').getByText('Dosya değişti. Önce yeniden doğrulayın.').waitFor({ state: 'visible' });
  assert.equal(softwareLicenseWrites.length, 1, 'Replacing the file after preview must block stale confirmation without an API write.');
  await page.locator('#licenseActionCancel').click();
  await licenseActionDialog.waitFor({ state: 'hidden' });
  await page.locator('#licenseImportFile').setInputFiles(licenseCsv);
  const retryPreviewResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/import/preview.csv');
  await page.locator('#previewLicenseImport').click();
  assert.equal((await retryPreviewResponse).status(), 200);
  const licenseWritesBeforeImportCancel = softwareLicenseWrites.length;
  await page.locator('#commitLicenseImport').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  await page.locator('#licenseActionCancel').click();
  await licenseActionDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseWrites.length, licenseWritesBeforeImportCancel, 'Canceling confirmed license import must not write.');
  assert.equal(softwareLicenseRows.length, 1, 'Canceling import must preserve the original license list.');

  await page.locator('#commitLicenseImport').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  const failedLicenseImportResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/import.csv');
  await page.locator('#licenseActionSubmit').click();
  assert.equal((await failedLicenseImportResponse).status(), 503, 'The synthetic server failure must reach the dialog as an HTTP error.');
  await page.locator('#licenseActionError').getByText('Synthetic supplier service unavailable.').waitFor({ state: 'visible' });
  assert.equal(softwareLicenseWrites.length, licenseWritesBeforeImportCancel, 'A failed import response must not be reported as a successful write.');
  assert.equal(await page.locator('#licenseActionSubmit').isEnabled(), true, 'A failed import must permit deliberate retry.');
  const licenseImportResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/import.csv');
  await page.locator('#licenseActionSubmit').click();
  assert.equal((await licenseImportResponse).status(), 200, 'Only an explicit successful confirmation may submit the import.');
  await licenseActionDialog.waitFor({ state: 'hidden' });
  await licenseList.locator('tr[data-id="12121212-1212-4212-8212-121212121212"]').waitFor({ state: 'visible' });
  assert.equal(softwareLicenseWrites.length, licenseWritesBeforeImportCancel + 1, 'Confirmed import must make exactly one API write.');
  assert.equal(softwareLicenseRows.length, 2, 'The imported license must remain visible after API list refresh.');
  assert.equal(softwareLicenseWrites.at(-1).method, 'POST');
  assert.equal(softwareLicenseWrites.at(-1).path, '/api/software-licenses/import.csv');
  assert.equal(softwareLicenseWrites.at(-1).body, licenseCsv.buffer.toString('utf8'));
  assert.equal(softwareLicenseWrites.at(-1).csrf, 'synthetic-asset-csrf');
  assert.equal(softwareLicenseImportAttempts.length, 2, 'The failed attempt and deliberate retry must be the only import requests.');
  assert.ok(softwareLicenseImportAttempts.every(attempt => attempt.path === '/api/software-licenses/import.csv'
    && attempt.body === licenseCsv.buffer.toString('utf8') && attempt.csrf === 'synthetic-asset-csrf'));
  await page.locator('#closeLicenseImport').click();
  await licenseImportDialog.waitFor({ state: 'hidden' });

  await licenseList.locator(`tr[data-id="${softwareLicenseId}"] .license-open`).first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  const licenseAssignmentDialog = page.locator('#licenseAssignDialog');
  await page.locator('#assignLicenseButton').click();
  await licenseAssignmentDialog.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#licenseAssignForm [name="employeeId"]')?.options.length > 1);
  await page.locator('#licenseAssignForm [name="employeeId"]').selectOption(employeeId);
  await page.locator('#licenseAssignForm [name="notes"]').fill('Synthetic assigned seat');
  const licenseAssignmentWriteCountBeforeAssign = softwareLicenseAssignmentWrites.length;
  await page.locator('#licenseAssignForm button[type="submit"]').click();
  await licenseAssignmentDialog.waitFor({ state: 'hidden' });
  await page.locator('#licenseAssignments').getByText('Synthetic QA Employee').waitFor();
  const licenseAssignmentWrite = softwareLicenseAssignmentWrites.at(-1);
  assert.equal(softwareLicenseAssignmentWrites.length, licenseAssignmentWriteCountBeforeAssign + 1);
  assert.equal(licenseAssignmentWrite?.method, 'POST');
  assert.equal(licenseAssignmentWrite?.path, `/api/software-licenses/${softwareLicenseId}/assign`);
  assert.deepEqual(licenseAssignmentWrite?.body, { employeeId, assetId: null, notes: 'Synthetic assigned seat', eulaAccepted: false });
  assert.equal(licenseAssignmentWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(softwareLicenseRows.find(item => item.id === softwareLicenseId).assigned, 1,
    'The refreshed license details must reflect its assigned seat.');
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });

  const softwareGroupToggle = page.locator('[data-group="software"]');
  if (await softwareGroupToggle.getAttribute('aria-expanded') !== 'true') await softwareGroupToggle.click();
  await page.locator('#sidebar [data-page="Lisans atamaları"]').click();
  await page.locator('#licenseAssignmentsPage').waitFor({ state: 'visible' });
  const activeLicenseAssignment = page.locator('#licenseAssignmentRows tr').filter({ hasText: 'Synthetic QA Employee' });
  await activeLicenseAssignment.waitFor();
  assert.match(await activeLicenseAssignment.innerText(), /Synthetic License Import Baseline[\s\S]*Aktif/);
  const assignmentSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/software-license-assignments' && url.searchParams.get('q') === 'Synthetic QA Employee';
  });
  await page.locator('#licenseAssignmentSearch').fill('Synthetic QA Employee');
  await assignmentSearchResponse;
  assert.ok(requests.some(item => item.path === '/api/software-license-assignments' && item.query.q === 'Synthetic QA Employee'));
  const revokeButton = activeLicenseAssignment.locator('.assignment-revoke');
  const revokeDialog = page.locator('#licenseRevokeDialog');
  const revokeWritesBeforeCancel = softwareLicenseAssignmentWrites.length;
  await revokeButton.click();
  await revokeDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#licenseRevokeSummary').innerText(), /Synthetic License Import Baseline[\s\S]*Synthetic QA Employee/);
  await page.locator('#licenseRevokeCancel').click();
  await revokeDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseAssignmentWrites.length, revokeWritesBeforeCancel, 'Canceling list revocation must not write.');
  assert.equal(softwareLicenseAssignments[0].revokedAt, null, 'Canceling must preserve the active license assignment.');
  await activeLicenseAssignment.locator('.assignment-revoke').click();
  const licenseRevocationResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}/revoke`);
  await page.locator('#licenseRevokeSubmit').click();
  assert.equal((await licenseRevocationResponse).status(), 204, 'Confirmed list revocation must reach the scoped API.');
  await revokeDialog.waitFor({ state: 'hidden' });
  await page.locator('#licenseAssignmentRows').getByText('Henüz atama yok.').waitFor();
  assert.equal(softwareLicenseAssignments[0].revokedAt, '2026-10-15T13:00:00Z', 'Confirmed revocation must persist in the synthetic server state.');
  assert.equal(softwareLicenseRows.find(item => item.id === softwareLicenseId).assigned, 0);
  const licenseRevocationWrite = softwareLicenseAssignmentWrites.at(-1);
  assert.equal(licenseRevocationWrite?.method, 'POST');
  assert.equal(licenseRevocationWrite?.path, `/api/software-licenses/${softwareLicenseId}/revoke`);
  assert.deepEqual(licenseRevocationWrite?.body, { assignmentId: 'e1000000-0000-4000-8000-000000000001' });
  assert.equal(licenseRevocationWrite?.csrf, 'synthetic-asset-csrf');
  await page.locator('#licenseAssignmentState').selectOption('false');
  await page.locator('#licenseAssignmentRows').getByText('Synthetic QA Employee').waitFor();
  await page.locator('#licenseAssignmentRows').getByText('Geri alındı').waitFor();
  console.log('PASS: software license employee assignment updates seat usage and detail; assignment list search, cancel-safe and confirmed revocation, inactive history filter, and CSRF are verified.');

  await page.locator('#sidebar button[data-page="Yazılım lisansları"]').click();
  await licenseList.locator(`tr[data-id="${softwareLicenseId}"] .license-open`).first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  await page.locator('#editLicenseButton').click();
  const eulaLicenseFormDialog = page.locator('#licenseFormDialog');
  await eulaLicenseFormDialog.waitFor({ state: 'visible' });
  await page.locator('#licenseForm [name="requiresEulaAcceptance"]').check();
  const requiredEulaUpdate = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}`);
  await page.locator('#licenseForm button[type="submit"]').click();
  assert.equal((await requiredEulaUpdate).status(), 204);
  await eulaLicenseFormDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseRows.find(item => item.id === softwareLicenseId).requiresEulaAcceptance, true,
    'The license EULA requirement must persist after saving its settings.');

  const eulaPdf = { name: 'synthetic-eula-v2.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.7\nSynthetic EULA v2\n%%EOF') };
  await page.locator('#licenseAttachmentForm [name="file"]').setInputFiles(eulaPdf);
  await page.locator('#licenseAttachmentForm [name="isEulaDocument"]').check();
  assert.equal(await page.locator('#licenseEulaVersionWrap').isVisible(), true,
    'Marking an attachment as EULA must reveal its required version field.');
  await page.locator('#licenseAttachmentForm [name="eulaVersion"]').fill('2.0');
  const eulaUploadResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}/attachments`);
  await page.locator('#licenseAttachmentForm button[type="submit"]').click();
  assert.equal((await eulaUploadResponse).status(), 201, 'A versioned EULA document must be saved through the company API.');
  const currentEulaAttachment = page.locator('#licenseAttachments .employee-document-row').filter({ hasText: 'synthetic-eula-v2.pdf' });
  await currentEulaAttachment.waitFor({ state: 'visible' });
  assert.match(await currentEulaAttachment.innerText(), /EULA 2\.0 · Güncel sürüm/);
  assert.equal(softwareLicenseCurrentEula?.id, 'abababab-abab-4bab-8bab-abababababab');
  assert.equal(softwareLicenseWrites.at(-1).csrf, 'synthetic-asset-csrf');

  await page.locator('#assignLicenseButton').click();
  await licenseAssignmentDialog.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#licenseAssignForm [name="employeeId"]')?.options.length > 1);
  const eulaLink = page.locator('#licenseEulaDocumentLink');
  assert.equal(await eulaLink.isVisible(), true, 'The assignment dialog must expose the current EULA before consent.');
  assert.match(await eulaLink.innerText(), /synthetic-eula-v2\.pdf · 2\.0/);
  assert.equal(await eulaLink.getAttribute('href'), `/api/software-licenses/${softwareLicenseId}/attachments/abababab-abab-4bab-8bab-abababababab/file`);
  assert.equal(await page.locator('#licenseEulaAccepted').isVisible(), true);
  assert.equal(await page.locator('#licenseEulaAccepted').isChecked(), false);
  assert.equal(await page.locator('#licenseEulaAccepted').evaluate(input => input.required), true,
    'The EULA confirmation must be a required form control for this license.');
  await page.locator('#licenseAssignForm [name="employeeId"]').selectOption(employeeId);
  const eulaAssignmentWritesBefore = softwareLicenseAssignmentWrites.length;
  await page.locator('#licenseAssignForm button[type="submit"]').click();
  assert.equal(await licenseAssignmentDialog.isVisible(), true, 'An unchecked EULA confirmation must keep the assignment form open.');
  assert.equal(softwareLicenseAssignmentWrites.length, eulaAssignmentWritesBefore, 'An unchecked EULA must not create an assignment request.');
  await page.locator('#licenseEulaAccepted').check();
  const eulaAssignmentResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}/assign`);
  const refreshedEulaDetailResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}`);
  await page.locator('#licenseAssignForm button[type="submit"]').click();
  assert.equal((await eulaAssignmentResponse).status(), 201, 'Explicit EULA consent must permit the assignment.');
  await licenseAssignmentDialog.waitFor({ state: 'hidden' });
  assert.equal((await refreshedEulaDetailResponse).status(), 200, 'The refreshed license detail must reload after an accepted EULA assignment.');
  const acceptedEulaAssignment = softwareLicenseAssignments.at(-1);
  assert.equal(acceptedEulaAssignment.eulaAttachmentId, softwareLicenseCurrentEula.id);
  assert.equal(acceptedEulaAssignment.eulaVersion, '2.0');
  assert.equal(acceptedEulaAssignment.eulaAcceptedBy, 'Synthetic Owner');
  assert.match(await page.locator('#licenseAssignments').innerText(), /2\.0[\s\S]*Synthetic Owner/,
    'The refreshed license detail must show the accepted EULA version and confirmer.');
  assert.deepEqual(softwareLicenseAssignmentWrites.at(-1).body,
    { employeeId, assetId: null, notes: null, eulaAccepted: true });
  assert.equal(softwareLicenseAssignmentWrites.at(-1).csrf, 'synthetic-asset-csrf');
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });
  console.log('PASS: software license EULA settings, versioned document upload, required consent, cancel-safe rejection, accepted-version attribution, and CSRF are verified.');

  const paginationLicenseId = 'e3000000-0000-4000-8000-000000000001';
  const paginationAssignments = Array.from({ length: 25 }, (_, index) => {
    const ordinal = String(index + 1).padStart(3, '0');
    return {
      id: `e3000000-0000-4000-8001-${String(index + 1).padStart(12, '0')}`,
      licenseId: paginationLicenseId,
      license: 'Synthetic Pagination License',
      licenseType: 'Subscription',
      employeeId,
      employee: index === 24 ? 'Synthetic Slow Pagination Employee' : `Synthetic Pagination Employee ${ordinal}`,
      employeeNumber: `QA-PAGE-${ordinal}`,
      assetId: null,
      assetTag: null,
      assetName: null,
      assignedAt: '2026-10-15T12:00:00Z',
      expirationDate: null,
      revokedAt: '2026-10-15T14:00:00Z'
    };
  });
  softwareLicenseAssignments.push(...paginationAssignments);
  await page.locator('#sidebar button[data-page="Lisans atamaları"]').click();
  await page.locator('#licenseAssignmentsPage').waitFor({ state: 'visible' });
  const assignmentPage = page.locator('#licenseAssignmentPageInfo');
  const assignmentRows = page.locator('#licenseAssignmentRows tr');
  const waitAssignmentList = (predicate) => page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/software-license-assignments' && predicate(url.searchParams);
  });
  const waitForAssignmentFilter = async (value, predicate) => {
    const response = waitAssignmentList(predicate);
    await page.locator('#licenseAssignmentState').selectOption(value);
    assert.equal((await response).status(), 200);
  };
  await page.locator('#licenseAssignmentSearch').fill('');
  await waitForAssignmentFilter('', params => !params.has('active') && params.get('page') === '1' && params.get('pageSize') === '25');
  assert.match(await page.locator('#licenseAssignmentCount').innerText(), /27/);
  assert.equal(await assignmentRows.count(), 25, 'The all-assignments view must render the first 25 of 27 rows.');
  assert.match(await assignmentPage.innerText(), /1\s*\/\s*2/);
  assert.equal(await page.locator('#licenseAssignmentPrev').isDisabled(), true);
  assert.equal(await page.locator('#licenseAssignmentNext').isDisabled(), false);
  const assignmentNextResponse = waitAssignmentList(params => !params.has('active') && params.get('page') === '2');
  await page.locator('#licenseAssignmentNext').click();
  assert.equal((await assignmentNextResponse).status(), 200);
  assert.equal(await assignmentRows.count(), 2, 'The second page must render only the two remaining assignments.');
  assert.match(await assignmentPage.innerText(), /2\s*\/\s*2/);
  assert.equal(await page.locator('#licenseAssignmentPrev').isDisabled(), false);
  assert.equal(await page.locator('#licenseAssignmentNext').isDisabled(), true);
  const assignmentPrevResponse = waitAssignmentList(params => !params.has('active') && params.get('page') === '1');
  await page.locator('#licenseAssignmentPrev').click();
  assert.equal((await assignmentPrevResponse).status(), 200);
  assert.equal(await assignmentRows.count(), 25);
  const assignmentNextAgainResponse = waitAssignmentList(params => !params.has('active') && params.get('page') === '2');
  await page.locator('#licenseAssignmentNext').click();
  assert.equal((await assignmentNextAgainResponse).status(), 200);
  const activeAssignmentsResponse = waitAssignmentList(params => params.get('active') === 'true' && params.get('page') === '1');
  await page.locator('#licenseAssignmentState').selectOption('true');
  assert.equal((await activeAssignmentsResponse).status(), 200);
  await page.waitForFunction(() => document.querySelectorAll('#licenseAssignmentRows tr').length === 1
    && document.querySelector('#licenseAssignmentRows')?.textContent?.includes('Aktif'));
  assert.equal(await assignmentRows.count(), 1, 'Active filter must exclude all 26 revoked history rows.');
  await assignmentRows.getByText('Aktif').waitFor();
  const revokedAssignmentsResponse = waitAssignmentList(params => params.get('active') === 'false' && params.get('page') === '1');
  await page.locator('#licenseAssignmentState').selectOption('false');
  assert.equal((await revokedAssignmentsResponse).status(), 200);
  await page.waitForFunction(() => /26/.test(document.querySelector('#licenseAssignmentCount')?.textContent || ''));
  assert.match(await page.locator('#licenseAssignmentCount').innerText(), /26/);
  const employeeSearchResponse = waitAssignmentList(params => params.get('active') === 'false' && params.get('q') === 'QA-PAGE-025' && params.get('page') === '1');
  await page.locator('#licenseAssignmentSearch').fill('QA-PAGE-025');
  assert.equal((await employeeSearchResponse).status(), 200);
  assert.equal(await assignmentRows.count(), 1, 'Searching by employee number must return the matching assignment.');
  await assignmentRows.getByText('QA-PAGE-025').waitFor();
  const licenseSearchResponse = waitAssignmentList(params => params.get('active') === 'false' && params.get('q') === 'Synthetic Pagination License' && params.get('page') === '1');
  await page.locator('#licenseAssignmentSearch').fill('Synthetic Pagination License');
  assert.equal((await licenseSearchResponse).status(), 200);
  await page.waitForFunction(() => document.querySelectorAll('#licenseAssignmentRows tr').length === 25);
  assert.equal(await assignmentRows.count(), 25, 'Searching by license name must return matching assignments across the list.');
  assert.match(await assignmentPage.innerText(), /1\s*\/\s*1/);
  const emptyAssignmentSearchResponse = waitAssignmentList(params => params.get('q') === 'No matching synthetic assignment');
  await page.locator('#licenseAssignmentSearch').fill('No matching synthetic assignment');
  assert.equal((await emptyAssignmentSearchResponse).status(), 200);
  await assignmentRows.getByText('Henüz atama yok.').waitFor();
  assert.equal(await page.locator('#licenseAssignmentPrev').isDisabled(), true);
  assert.equal(await page.locator('#licenseAssignmentNext').isDisabled(), true);
  const slowAssignmentSearchResponse = waitAssignmentList(params => params.get('q') === 'Synthetic Slow Pagination Employee');
  const slowAssignmentSearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/software-license-assignments' && url.searchParams.get('q') === 'Synthetic Slow Pagination Employee';
  });
  await page.locator('#licenseAssignmentSearch').fill('Synthetic Slow Pagination Employee');
  await slowAssignmentSearchRequest;
  const latestAssignmentSearchResponse = waitAssignmentList(params => params.get('q') === 'QA-PAGE-024');
  await page.locator('#licenseAssignmentSearch').fill('QA-PAGE-024');
  assert.equal((await latestAssignmentSearchResponse).status(), 200);
  await assignmentRows.getByText('QA-PAGE-024').waitFor();
  assert.equal((await slowAssignmentSearchResponse).status(), 200);
  assert.equal(await assignmentRows.count(), 1, 'A late stale response must not replace the latest search result.');
  await assignmentRows.getByText('QA-PAGE-024').waitFor();
  softwareLicenseAssignments.splice(softwareLicenseAssignments.indexOf(paginationAssignments[0]), paginationAssignments.length);
  console.log('PASS: software license assignment list active/revoked/all filters, employee-number search, empty state, 25-row pagination, and stale-response protection are verified.');

  await page.locator('#sidebar button[data-page="Yazılım lisansları"]').click();
  const bulkAssignmentLicenseRow = page.locator(`#licenseRows tr[data-id="${softwareLicenseId}"]`);
  await bulkAssignmentLicenseRow.waitFor({ state: 'visible' });
  await bulkAssignmentLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  includeBulkLicenseEmployees = true;
  await page.locator('#assignLicenseButton').click();
  await licenseAssignmentDialog.waitFor({ state: 'visible' });
  const bulkEmployeeSelect = page.locator('#licenseAssignForm [name="employeeId"]');
  await page.waitForFunction(() => document.querySelector('#licenseAssignForm [name="employeeId"]')?.options.length >= 4);
  await page.locator('#licenseBulkEmployeeMode').check();
  assert.equal(await bulkEmployeeSelect.evaluate(select => select.multiple), true, 'Bulk mode must enable multi-select employee assignment.');
  assert.equal(await page.locator('#licenseBulkEmployeeHelp').isVisible(), true, 'Bulk mode must explain selection and atomic behavior.');
  await bulkEmployeeSelect.selectOption([bulkEmployeeTwoId, bulkEmployeeThreeId]);
  assert.equal(await bulkEmployeeSelect.locator('option:checked').count(), 2);
  const assignmentWritesBeforeBulkCancel = softwareLicenseAssignmentWrites.length;
  await page.locator('#licenseAssignForm [data-close-license-assign]').filter({ hasText: 'Vazgeç' }).click();
  await licenseAssignmentDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseAssignmentWrites.length, assignmentWritesBeforeBulkCancel, 'Canceling bulk assignment must not write.');
  assert.equal(softwareLicenseRows.find(item => item.id === softwareLicenseId).assigned, 1, 'Canceled bulk assignment must preserve seat usage.');

  await bulkAssignmentLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  includeBulkLicenseEmployees = true;
  await page.locator('#assignLicenseButton').click();
  await licenseAssignmentDialog.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#licenseAssignForm [name="employeeId"]')?.options.length >= 4);
  await page.locator('#licenseBulkEmployeeMode').check();
  await bulkEmployeeSelect.selectOption([bulkEmployeeTwoId, bulkEmployeeThreeId]);
  await page.locator('#licenseAssignForm [name="notes"]').fill('Synthetic bulk assignment note');
  const assignmentWritesBeforeMissingEula = softwareLicenseAssignmentWrites.length;
  await page.locator('#licenseAssignForm button[type="submit"]').click();
  assert.equal(await licenseAssignmentDialog.isVisible(), true, 'Bulk submission without required EULA confirmation must stay open.');
  assert.equal(softwareLicenseAssignmentWrites.length, assignmentWritesBeforeMissingEula, 'Missing bulk EULA consent must not write.');
  await page.locator('#licenseEulaAccepted').check();
  const bulkAssignmentResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}/assign/bulk`);
  const bulkAssignmentRequest = page.waitForRequest(request => request.method() === 'POST'
    && new URL(request.url()).pathname === `/api/software-licenses/${softwareLicenseId}/assign/bulk`);
  const bulkAssignmentSubmit = page.locator('#licenseAssignForm button[type="submit"]');
  await bulkAssignmentSubmit.click();
  await bulkAssignmentRequest;
  assert.equal(await bulkAssignmentSubmit.isDisabled(), true, 'The submit control must block duplicate bulk requests while saving.');
  assert.equal((await bulkAssignmentResponse).status(), 201);
  await licenseAssignmentDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseAssignmentWrites.length, assignmentWritesBeforeMissingEula + 1, 'One confirmed bulk submit must produce one API write.');
  const bulkAssignmentWrite = softwareLicenseAssignmentWrites.at(-1);
  assert.equal(bulkAssignmentWrite.path, `/api/software-licenses/${softwareLicenseId}/assign/bulk`);
  assert.deepEqual(bulkAssignmentWrite.body, { employeeIds: [bulkEmployeeTwoId, bulkEmployeeThreeId], notes: 'Synthetic bulk assignment note', eulaAccepted: true });
  assert.equal(bulkAssignmentWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(softwareLicenseRows.find(item => item.id === softwareLicenseId).assigned, 3);
  assert.equal(softwareLicenseAssignments.filter(item => item.licenseId === softwareLicenseId && !item.revokedAt).length, 3,
    'Both bulk seats and the prior active assignment must be present after success.');
  for (const id of [bulkEmployeeTwoId, bulkEmployeeThreeId]) {
    const assigned = softwareLicenseAssignments.find(item => item.licenseId === softwareLicenseId && item.employeeId === id && !item.revokedAt);
    assert.equal(assigned.eulaAttachmentId, softwareLicenseCurrentEula.id);
    assert.equal(assigned.eulaVersion, '2.0');
    assert.equal(assigned.eulaAcceptedBy, 'Synthetic Owner');
  }
  await page.locator('#licenseAssignments').getByText('Synthetic Bulk Employee Two').waitFor();
  await page.locator('#licenseAssignments').getByText('Synthetic Bulk Employee Three').waitFor();
  await page.locator('#licenseDetailFields').getByText('3 / 3').waitFor();
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });
  console.log('PASS: bulk software license employee assignment supports multi-select, safe cancel, required EULA consent, single CSRF-protected atomic request, duplicate-submit blocking, capacity refresh, and accepted-version attribution.');

  const licenseFormDialog = page.locator('#licenseFormDialog');
  const licenseCrudWriteCountBefore = softwareLicenseCrudWrites.length;
  await page.locator('#newLicenseButton').click();
  await licenseFormDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#licenseForm [name="name"]').evaluate(input => input.required && !input.checkValidity()), true,
    'A license cannot be submitted without its required name.');
  await page.locator('#licenseForm [data-close-license]').filter({ hasText: 'Vazgeç' }).click();
  await licenseFormDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseCrudWrites.length, licenseCrudWriteCountBefore, 'Canceling a new license must not write.');

  await page.locator('#newLicenseButton').click();
  await licenseFormDialog.waitFor({ state: 'visible' });
  const createdLicenseName = 'Synthetic CRUD License';
  await page.locator('#licenseForm [name="name"]').fill(createdLicenseName);
  await page.locator('#licenseForm [name="publisher"]').fill('Synthetic QA Publisher');
  await page.locator('#licenseForm [name="productVersion"]').fill('4.2');
  await page.locator('#licenseForm [name="licenseType"]').selectOption('Subscription');
  await page.locator('#licenseForm [name="licenseReference"]').fill('QA-LICENSE-CRUD-001');
  await page.locator('#licenseForm [name="supplier"]').fill('Synthetic QA Supplier');
  await page.locator('#licenseForm [name="seats"]').fill('2');
  await page.locator('#licenseForm [name="unitCost"]').fill('45.5');
  await page.locator('#licenseForm [name="currency"]').selectOption('USD');
  await page.locator('#licenseForm [name="purchaseDate"]').fill('2026-10-15');
  await page.locator('#licenseForm [name="expirationDate"]').fill('2027-10-15');
  await page.locator('#licenseForm [name="autoRenew"]').check();
  await page.locator('#licenseForm [name="notes"]').fill('Synthetic CRUD creation note');
  const licenseCreateResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses');
  await page.locator('#licenseForm button[type="submit"]').click();
  assert.equal((await licenseCreateResponse).status(), 201, 'Creating a software license must reach the company API.');
  await licenseFormDialog.waitFor({ state: 'hidden' });
  const createdLicenseRow = licenseList.locator('tr[data-id="a5555555-5555-4555-8555-555555555555"]');
  await createdLicenseRow.waitFor({ state: 'visible' });
  const licenseCreateWrite = softwareLicenseCrudWrites.at(-1);
  assert.deepEqual(licenseCreateWrite, { method: 'POST', path: '/api/software-licenses', body: {
    name: createdLicenseName, publisher: 'Synthetic QA Publisher', productVersion: '4.2', licenseType: 'Subscription',
    licenseReference: 'QA-LICENSE-CRUD-001', supplier: 'Synthetic QA Supplier', purchaseOrderReference: null, seats: 2, unitCost: 45.5, currency: 'USD',
    purchaseDate: '2026-10-15', expirationDate: '2027-10-15', autoRenew: true, requiresEulaAcceptance: false, notes: 'Synthetic CRUD creation note'
  }, csrf: 'synthetic-asset-csrf' });

  await createdLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  await page.locator('#assignLicenseButton').click();
  await licenseAssignmentDialog.waitFor({ state: 'visible' });
  const assignmentTargetType = page.locator('#licenseAssignForm [name="targetType"]');
  await assignmentTargetType.selectOption('asset');
  await page.waitForFunction(id => document.querySelector('#licenseAssignForm [name="assetId"]')?.querySelector(`option[value="${id}"]`), assetId);
  assert.equal(await page.locator('#licenseEmployeeTarget').isHidden(), true, 'Selecting an asset target must hide the employee control.');
  assert.equal(await page.locator('#licenseAssetTarget').isVisible(), true, 'Selecting an asset target must show asset search and selection.');
  assert.equal(await page.locator('#licenseBulkEmployeeOption').isHidden(), true, 'Employee bulk mode must not remain available for a device target.');
  await page.locator('#licenseAssignForm [name="assetId"]').selectOption(assetId);
  await page.locator('#licenseAssignForm [name="notes"]').fill('Synthetic device license assignment');
  const assignedAssetName = asset.name;
  const deviceLicenseDetailRefreshResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555');
  const deviceLicenseAssignmentResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555/assign');
  await page.locator('#licenseAssignForm button[type="submit"]').click();
  assert.equal((await deviceLicenseAssignmentResponse).status(), 201, 'Device assignment must submit to the tenant license endpoint.');
  await licenseAssignmentDialog.waitFor({ state: 'hidden' });
  const deviceAssignmentWrite = softwareLicenseAssignmentWrites.at(-1);
  assert.deepEqual(deviceAssignmentWrite.body, { employeeId: null, assetId, notes: 'Synthetic device license assignment', eulaAccepted: false });
  assert.equal(deviceAssignmentWrite.csrf, 'synthetic-asset-csrf');
  const deviceLicenseAssignment = softwareLicenseAssignments.at(-1);
  assert.equal(deviceLicenseAssignment.assetId, assetId);
  assert.equal(deviceLicenseAssignment.employeeId, null);
  await deviceLicenseDetailRefreshResponse;
  const renderedDeviceAssignments = await page.locator('#licenseAssignments').innerText();
  assert.ok(renderedDeviceAssignments.includes(`QA-ASSET-001 · ${assignedAssetName}`),
    `The refreshed license detail must display the assigned asset; rendered rows: ${renderedDeviceAssignments}`);
  await page.locator('#licenseAssignments [data-license-asset]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#assetTabRelationsButton').click();
  const assetLicenseRows = page.locator('#assetLicenseAssignments tr');
  assert.equal(await assetLicenseRows.count(), 1, 'The assigned device license must appear in its asset relations before revocation.');
  assert.match(await assetLicenseRows.innerText(), /Synthetic CRUD License/);
  assert.match(await assetLicenseRows.innerText(), /Synthetic device license assignment/);
  assert.match(await assetLicenseRows.innerText(), /Süresiz/);
  await page.locator('#closeDialog').click();
  await createdLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  const deviceRevokeButton = page.locator(`#licenseAssignments button.license-revoke[data-id="${deviceLicenseAssignment.id}"]`);
  const deviceRevokeWritesBeforeCancel = softwareLicenseAssignmentWrites.length;
  await deviceRevokeButton.click();
  await revokeDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#licenseRevokeSummary').innerText(), /Synthetic CRUD License[\s\S]*QA-ASSET-001/);
  await page.locator('#licenseRevokeCancel').click();
  await revokeDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseAssignmentWrites.length, deviceRevokeWritesBeforeCancel, 'Canceling device assignment revocation must not write.');
  const confirmDeviceRevokeResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555/revoke');
  await page.locator(`#licenseAssignments button.license-revoke[data-id="${deviceLicenseAssignment.id}"]`).click();
  await page.locator('#licenseRevokeSubmit').click();
  assert.equal((await confirmDeviceRevokeResponse).status(), 204);
  await page.locator('#licenseAssignments').getByText('Geri alındı').waitFor();
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').assigned, 0,
    'Revoking a device target must free its license seat before archive or reassignment.');
  await page.locator('#licenseAssignments [data-license-asset]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#assetTabRelationsButton').click();
  assert.equal(await page.locator('#assetLicenseAssignments tr').count(), 1, 'The asset license table must render its empty-state row after its only assignment is revoked.');
  assert.match(await page.locator('#assetLicenseAssignments').innerText(), /Cihaza atanmış lisans yok\./);
  await page.locator('#closeDialog').click();
  await createdLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  await page.locator('#editLicenseButton').click();
  await licenseFormDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#licenseForm [name="name"]').inputValue(), createdLicenseName,
    'The edit form must be populated from the selected license.');
  const editedLicenseName = 'Synthetic CRUD License Updated';
  await page.locator('#licenseForm [name="name"]').fill('Canceled Synthetic License Edit');
  await page.locator('#licenseForm [data-close-license]').filter({ hasText: 'Vazgeç' }).click();
  await licenseFormDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseCrudWrites.length, licenseCrudWriteCountBefore + 1, 'Canceling license edits must not write.');
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').name, createdLicenseName,
    'Canceling license edits must preserve the stored synthetic record.');

  await createdLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  await page.locator('#editLicenseButton').click();
  await licenseFormDialog.waitFor({ state: 'visible' });
  await page.locator('#licenseForm [name="name"]').fill(editedLicenseName);
  await page.locator('#licenseForm [name="seats"]').fill('3');
  await page.locator('#licenseForm [name="autoRenew"]').uncheck();
  await page.locator('#licenseForm [name="notes"]').fill('Synthetic CRUD edit note');
  const licenseUpdateResponse = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555');
  await page.locator('#licenseForm button[type="submit"]').click();
  assert.equal((await licenseUpdateResponse).status(), 204, 'Updating a software license must reach its company-scoped API.');
  await licenseFormDialog.waitFor({ state: 'hidden' });
  await page.locator('#licenseDetailFields').getByText('0 / 3').waitFor();
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').name, editedLicenseName,
    'The confirmed license edit must persist after the detail is reloaded from the synthetic API.');
  const licenseUpdateWrite = softwareLicenseCrudWrites.at(-1);
  assert.equal(licenseUpdateWrite.method, 'PUT');
  assert.deepEqual(licenseUpdateWrite.body, { name: editedLicenseName, publisher: 'Synthetic QA Publisher', productVersion: '4.2',
    licenseType: 'Subscription', licenseReference: 'QA-LICENSE-CRUD-001', supplier: 'Synthetic QA Supplier', purchaseOrderReference: null, seats: 3,
    unitCost: 45.5, currency: 'USD', purchaseDate: '2026-10-15', expirationDate: '2027-10-15', autoRenew: false,
    requiresEulaAcceptance: false, notes: 'Synthetic CRUD edit note' });
  assert.equal(licenseUpdateWrite.csrf, 'synthetic-asset-csrf');
  await page.locator('.license-archive-toggle').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#licenseActionSummary').textContent(), /etkin ataması varsa işlem reddedilir/i,
    'The archive confirmation must explain the active-assignment constraint.');
  await page.locator('#licenseActionCancel').click();
  await licenseActionDialog.waitFor({ state: 'hidden' });
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').isArchived, false,
    'Canceling archive must leave the license active.');
  await page.locator('.license-archive-toggle').click();
  const licenseArchiveResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555/archive');
  await page.locator('#licenseActionSubmit').click();
  assert.equal((await licenseArchiveResponse).status(), 204, 'Confirmed archive must reach the tenant-scoped archive endpoint.');
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });
  await page.locator('#licenseStatusFilter').selectOption('archived');
  await createdLicenseRow.waitFor({ state: 'visible' });
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').isArchived, true);
  await createdLicenseRow.locator('.license-open').first().click();
  await licenseDetail.waitFor({ state: 'visible' });
  await page.locator('#licenseDetailFields').getByText('Arşivlendi').waitFor();
  assert.equal(await page.locator('#editLicenseButton').isVisible(), false, 'Archived licenses must not expose edit.');
  assert.equal(await page.locator('#assignLicenseButton').isVisible(), false, 'Archived licenses must not expose assignment.');
  assert.equal(await page.locator('#licenseAttachmentForm').isVisible(), false, 'Archived licenses must not expose file upload.');
  await page.locator('.license-archive-toggle').click();
  await licenseActionDialog.waitFor({ state: 'visible' });
  const licenseRestoreResponse = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/software-licenses/a5555555-5555-4555-8555-555555555555/restore');
  await page.locator('#licenseActionSubmit').click();
  assert.equal((await licenseRestoreResponse).status(), 204, 'Confirmed restore must reach the tenant-scoped restore endpoint.');
  assert.equal(softwareLicenseRows.find(item => item.id === 'a5555555-5555-4555-8555-555555555555').isArchived, false);
  await page.locator('#licenseStatusFilter').selectOption('active');
  await createdLicenseRow.waitFor({ state: 'visible' });
  assert.equal(softwareLicenseCrudWrites.filter(write => write.path?.includes('/archive') || write.path?.includes('/restore')).length, 2,
    'Only the explicitly confirmed archive and restore actions may write.');
  await page.locator('#closeLicenseDetail').click();
  await licenseDetail.waitFor({ state: 'hidden' });
  const licenseSearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/software-licenses' && url.searchParams.get('q') === 'QA-LICENSE-CRUD-001';
  });
  await page.locator('#licenseSearch').fill('QA-LICENSE-CRUD-001');
  await licenseSearchRequest;
  await page.locator('#licenseRows').getByText(editedLicenseName).waitFor();
  assert.ok(requests.some(item => item.path === '/api/software-licenses' && item.query.q === 'QA-LICENSE-CRUD-001'),
    'The license list search must find the newly persisted record through the API.');
  assert.equal(softwareLicenseCrudWrites.length, licenseCrudWriteCountBefore + 4,
    'Only the explicitly confirmed license creation, update, archive, and restore may write.');
  console.log('PASS: software license create/edit required-field validation, cancel-safe dialogs, CSRF-scoped writes, detail edit prefill, archive/restore confirmation, active/archive filters, archived action locks, persisted list/detail refresh, and search.');

  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  await assetRow.locator('.asset-name').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#editAssetButton').click();
  const expiryEditDialog = page.locator('#editAssetDialog');
  await expiryEditDialog.waitFor({ state: 'visible' });
  const nameBeforeExpiry = asset.name;
  const unsavedExpiryName = 'Must not submit after re-login';
  await page.locator('#editAssetForm [name="assetName"]').fill(unsavedExpiryName);
  expireNextAssetPatch = true;
  const failedWritesBeforeExpiry = apiRequests.filter(item => item.path === `/api/assets/${assetId}` && item.method === 'PATCH').length;
  await page.locator('#editAssetForm button[type="submit"]').click();
  await page.locator('#authLayer.show').waitFor({ state: 'visible' });
  assert.equal(await expiryEditDialog.evaluate(dialog => dialog.open), false,
    'An expired session must close the open edit dialog instead of leaving it behind the login screen.');
  assert.equal(await page.locator('#assetDialog').evaluate(dialog => dialog.open), false,
    'An expired session must also close the parent asset detail dialog.');
  assert.equal(asset.name, nameBeforeExpiry, 'The synthetic server must reject the expired-session edit without persisting it.');
  assert.equal(apiRequests.filter(item => item.path === `/api/assets/${assetId}` && item.method === 'PATCH').length, failedWritesBeforeExpiry + 1,
    'The session-expiry action must produce exactly one failed edit attempt.');
  await page.locator('#loginForm [name="email"]').fill('qa-session@example.invalid');
  await page.locator('#loginForm [name="password"]').fill('Synthetic-session-password');
  await page.evaluate(() => {
    window.__postLoginOverviewReady = false;
    document.addEventListener('click', event => {
      if (event.target.closest('#sidebar [data-page="Genel Bakış"]')) window.__postLoginOverviewReady = true;
    }, { capture: true });
  });
  await page.locator('#loginForm button[type="submit"]').click();
  await page.waitForFunction(() => !document.querySelector('#authLayer')?.classList.contains('show'));
  await page.waitForFunction(() => window.__postLoginOverviewReady === true);
  assert.equal(expiryLoginAttempts.length, 1, 'The synthetic session must be restored only after one deliberate login submission.');
  assert.equal(asset.name, nameBeforeExpiry, 'Re-authentication must not silently apply the unsaved form data.');
  assert.equal(apiRequests.filter(item => item.path === `/api/assets/${assetId}` && item.method === 'PATCH').length, failedWritesBeforeExpiry + 1,
    'Re-authentication must not automatically retry the previously rejected write.');
  assert.equal(await page.locator('#companySwitchButton').isVisible(), true,
    'A user with multiple active company memberships must have a company-switch action.');
  await page.locator('#companySwitchButton').click();
  const companySwitchDialog = page.locator('#companySwitchDialog');
  await companySwitchDialog.waitFor({ state: 'visible' });
  assert.equal(await companySwitchDialog.evaluate(dialog => dialog.open), true, 'Opening the company picker must create a live native dialog.');
  assert.equal(await page.locator('#companySwitchTarget option').count(), 1,
    'The company picker must offer only another company from the authenticated membership list.');
  assert.equal(await companySwitchDialog.evaluate(dialog => dialog.open), true, 'Reading company options must not dismiss the picker.');
  assert.equal(await page.locator('#companySwitchTarget').inputValue(), secondCompanyId);
  assert.equal(await companySwitchDialog.evaluate(dialog => dialog.open), true, 'Reading the selected company must not dismiss the picker.');
  await page.locator('#confirmCompanySwitch').click();
  await page.locator('#authLayer.show').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#loginForm [name="email"]').inputValue(), 'qa-session@example.invalid',
    'Switching company should retain the account identifier so the next login can list its memberships.');
  assert.equal(await page.locator('#loginForm [name="password"]').inputValue(), '',
    'A company switch must clear the prior password and require re-entry.');
  const logoutWrite = apiRequests.findLast(item => item.path === '/api/session/logout');
  assert.equal(logoutWrite?.method, 'POST');
  assert.equal(logoutWrite?.csrf, 'synthetic-asset-csrf', 'Company switch logout must use CSRF protection.');
  await page.locator('#loginForm [name="password"]').fill('Synthetic-session-password');
  await page.locator('#loginForm button[type="submit"]').click();
  await page.locator('#loginCompanyField:not([hidden])').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#loginCompanyId option').count(), 3,
    'The existing login flow must return both authorized memberships after credentials are re-entered.');
  await page.locator('#loginCompanyId').selectOption(secondCompanyId);
  await page.evaluate(() => { window.__postLoginOverviewReady = false; });
  await page.locator('#loginForm button[type="submit"]').click();
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.waitForFunction(() => window.__postLoginOverviewReady === true);
  assert.equal(await page.locator('#companyName').innerText(), 'Synthetic Second Company',
    'After re-authentication, the workspace header must show the selected company.');
  assert.equal(expiryLoginAttempts.length, 3,
    'The switch flow must use one deliberate company-list response and one explicit selected-company login.');
  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: nameBeforeExpiry }).click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#editAssetButton').click();
  await expiryEditDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#editAssetForm [name="assetName"]').inputValue(), nameBeforeExpiry,
    'Reopening the editor must load the persisted asset, not the discarded session-expiry draft.');
  await page.locator('[data-close-edit-asset]').first().click();
  await expiryEditDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetDialog').locator('#closeDialog').click();

  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).waitFor({ state: 'visible' });
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  const trashDialog = page.locator('#assetTrashConfirmDialog');
  const beforeTrashCancel = apiRequests.length;
  await page.locator('#trashAssetButton').click();
  await trashDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetTrashConfirmSummary').innerText(), /QA-ASSET-001/);
  await page.locator('#cancelAssetTrashConfirm').click();
  await trashDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeTrashCancel, 'Canceling asset trash must not write.');
  assert.equal(await assetRow.locator('.asset-name').isVisible(), true);
  await page.locator('#trashAssetButton').click();
  await trashDialog.waitFor({ state: 'visible' });
  await page.locator('#assetTrashConfirmForm [name="note"]').fill('Synthetic archive reason');
  await page.locator('#submitAssetTrashConfirm').click();
  await trashDialog.waitFor({ state: 'hidden' });
  await assetRow.locator('.asset-name').waitFor({ state: 'detached' });
  const trashWrite = apiRequests.at(-1);
  assert.equal(trashWrite.method, 'POST');
  assert.equal(trashWrite.path, `/api/assets/${assetId}/trash`);
  assert.deepEqual(trashWrite.body, { note: 'Synthetic archive reason' });
  assert.equal(trashWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.isDeleted, true, 'Only the explicitly confirmed action may move the synthetic asset to trash.');

  const bulkAssetIds = ['12121212-1212-4121-8121-121212121212', '13131313-1313-4131-8131-131313131313'];
  bulkAssets = bulkAssetIds.map((id, index) => ({
    ...asset, companyId: activeCompanyId, id, assetTag: `QA-BULK-${index + 1}`, name: `Synthetic bulk status ${index + 1}`,
    status: 'Available', updatedAt: '2026-10-15T09:00:00Z', assignedEmployeeId: null, nextAuditOn: null
  }));
  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  const firstBulkRow = page.locator(`#assetRows tr[data-id="${bulkAssetIds[0]}"]`);
  const secondBulkRow = page.locator(`#assetRows tr[data-id="${bulkAssetIds[1]}"]`);
  await firstBulkRow.waitFor({ state: 'visible' });
  await secondBulkRow.waitFor({ state: 'visible' });
  await page.locator('#scanAssetButton').click();
  const scannerDialog = page.locator('#scanAssetDialog');
  await scannerDialog.waitFor({ state: 'visible' });
  await page.locator('#scanAssetAction').selectOption('collect');
  assert.equal(await page.locator('#finishAssetBatchScan').isVisible(), true,
    'Multi-scan mode must expose an explicit finish action.');
  assert.match(await page.locator('#scanAssetFeedback').innerText(), /sırayla okutun/i);
  const scanForm = page.locator('#scanAssetForm');
  const codeField = page.locator('#scanAssetCode');
  await codeField.fill('QA-BULK');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /Birden fazla kayıt eşleşti/);
  assert.equal(await page.locator('#selectedAssetCount').isVisible(), false,
    'An ambiguous code must not add any asset to the batch.');
  await codeField.fill('QA-NOT-FOUND');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /eşleşen varlık bulunamadı/i);
  assert.equal(await page.locator('#selectedAssetCount').isVisible(), false,
    'An unknown code must not add any asset to the batch.');
  await codeField.fill('QA-BULK-1');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /Synthetic scan lookup unavailable/);
  assert.equal(await page.locator('#selectedAssetCount').isVisible(), false,
    'A failed search request must leave selection unchanged.');
  await codeField.fill('QA-BULK-1');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /QA-BULK-1 .* seçime eklendi/);
  assert.equal(await firstBulkRow.locator('.asset-row-select').isChecked(), true,
    'A matched scanned asset must immediately become selected in the table.');
  await codeField.fill('QA-BULK-1');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /zaten tarandı/i);
  assert.equal(await page.locator('#selectedAssetCount').innerText(), '1 varlık seçildi',
    'Scanning the same code twice must not duplicate the selected asset.');
  const qrAssetUrl = await page.evaluate(id => `${location.origin}/?assetId=${id}`, bulkAssetIds[0]);
  await codeField.fill(qrAssetUrl);
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /zaten seçili/i);
  assert.equal(await page.locator('#selectedAssetCount').innerText(), '1 varlık seçildi',
    'A QR link to an already selected asset must not duplicate its selection.');
  await codeField.fill('QA-BULK-2');
  await scanForm.locator('button[type="submit"]').click();
  await expectScanFeedback(page, /QA-BULK-2 .* seçime eklendi/);
  assert.equal(await page.locator('#selectedAssetCount').innerText(), '2 varlık seçildi');
  await page.locator('#finishAssetBatchScan').click();
  await scannerDialog.waitFor({ state: 'hidden' });
  assert.equal(await firstBulkRow.locator('.asset-row-select').isChecked(), true);
  assert.equal(await secondBulkRow.locator('.asset-row-select').isChecked(), true);
  console.log('PASS: multi-scan rejects ambiguous/missing codes, surfaces lookup failure and permits retry, de-duplicates repeated tag/QR scans, synchronizes row checks, and preserves selection after explicit completion.');

  const bulkToolbar = page.locator('#bulkAssetToolbar');
  await bulkToolbar.waitFor({ state: 'visible' });
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/);
  await page.locator('#bulkAssetStatus').selectOption('Maintenance');
  const bulkActionDialog = page.locator('dialog[aria-labelledby="bulkAssetActionTitle"]');
  const bulkStatusWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-status').length;
  await page.locator('#applyBulkAssetStatus').click();
  await bulkActionDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#bulkAssetActionDescription').innerText(), /2 varlığın durumu/);
  await page.locator('#cancelBulkAssetAction').click();
  await bulkActionDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-status').length, bulkStatusWritesBeforeCancel,
    'Canceling the bulk status dialog must not write.');
  assert.ok(bulkAssets.every(item => item.status === 'Available'), 'Canceling must leave every selected asset unchanged.');
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/,
    'Canceling must preserve the selection so the user can deliberately retry.');
  await page.locator('#applyBulkAssetStatus').click();
  await bulkActionDialog.waitFor({ state: 'visible' });
  await page.locator('#bulkAssetActionForm [name="note"]').fill('Quarterly bulk inspection');
  await page.locator('#submitBulkAssetAction').click();
  await bulkActionDialog.waitFor({ state: 'hidden' });
  await firstBulkRow.getByText('Bakımda').waitFor();
  await secondBulkRow.getByText('Bakımda').waitFor();
  const bulkStatusWrite = apiRequests.find(item => item.path === '/api/assets/bulk-status');
  assert.equal(bulkStatusWrite?.method, 'POST');
  assert.deepEqual(bulkStatusWrite?.body, { assetIds: bulkAssetIds, status: 'Maintenance', note: 'Quarterly bulk inspection' });
  assert.equal(bulkStatusWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(bulkAssets.every(item => item.status === 'Maintenance'), 'The synthetic server state must persist the new status for both selected assets.');
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk update must clear the selection and hide the bulk toolbar.');
  assert.equal(await page.locator('#bulkAssetStatus').inputValue(), '', 'A successful bulk update must reset the status selector.');
  console.log('PASS: bulk asset status change confirms two selected records, cancellation performs no write, confirmed CSRF submission persists both statuses and refreshes the list.');

  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  const unselectedInsuranceValues = bulkAssets.map(item => item.insuranceProvider);
  const bulkUpdateWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-update').length;
  await page.locator('#bulkEditSelectedAssets').click();
  const bulkEditDialog = page.locator('#bulkEditAssetDialog');
  await bulkEditDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#bulkEditAssetCount').innerText(), '2 varlık seçildi');
  assert.equal(await page.locator('#bulkEditAssetForm [data-bulk-field-enable="name"]').isChecked(), false,
    'Bulk edit must start with every field opt-in disabled.');
  await page.locator('[data-close-bulk-asset-edit]').filter({ hasText: 'Vazgeç' }).click();
  await bulkEditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-update').length, bulkUpdateWritesBeforeCancel,
    'Canceling bulk edit must not mutate selected assets.');
  await page.locator('#bulkEditSelectedAssets').click();
  await bulkEditDialog.waitFor({ state: 'visible' });
  await page.locator('#bulkEditAssetForm [data-bulk-field-enable="name"]').check();
  await page.locator('#bulkEditAssetForm [name="name"]').fill('Quarterly Refresh');
  await page.locator('#bulkEditAssetForm [data-bulk-field-enable="serialNumber"]').check();
  await page.locator('#bulkEditAssetForm [name="serialNumber"]').fill('QA-BULK-UPDATED');
  await page.locator('#bulkEditAssetForm [name="note"]').fill('Quarterly asset refresh');
  const bulkEditResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/bulk-update'
    && response.request().method() === 'POST');
  await page.locator('#bulkEditAssetForm button[type="submit"]').click();
  assert.equal((await bulkEditResponse).status(), 200);
  await bulkEditDialog.waitFor({ state: 'hidden' });
  await firstBulkRow.getByText('Quarterly Refresh').waitFor();
  await secondBulkRow.getByText('Quarterly Refresh').waitFor();
  const bulkEditWrite = apiRequests.find(item => item.path === '/api/assets/bulk-update');
  assert.deepEqual(bulkEditWrite?.body.fields, ['name', 'serialNumber']);
  assert.equal(bulkEditWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(bulkAssets.every((item, index) => item.name === 'Quarterly Refresh' && item.serialNumber === 'QA-BULK-UPDATED'
    && item.insuranceProvider === unselectedInsuranceValues[index]), 'Only selected fields may change; unselected insurance data must be preserved.');
  assert.equal(await bulkToolbar.isHidden(), true, 'Successful bulk edit must clear the selection.');
  console.log('PASS: multi-field asset edit is opt-in, cancel-safe, versioned, CSRF-protected, persisted to both selected records, preserves unchecked fields, and refreshes the list.');

  for (const item of bulkAssets) {
    item.status = 'Deployed';
    item.assignedEmployeeId = employeeId;
    item.assignedEmployeeName = 'Synthetic QA Employee';
    item.expectedReturnOn = '2026-10-20';
  }
  await page.reload({ waitUntil: 'domcontentloaded' });
  await firstBulkRow.waitFor({ state: 'visible' });
  await secondBulkRow.waitFor({ state: 'visible' });
  assert.match(await firstBulkRow.innerText(), /Synthetic QA Employee/);
  assert.match(await secondBulkRow.innerText(), /Synthetic QA Employee/);
  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  await bulkToolbar.waitFor({ state: 'visible' });
  const bulkReturnButton = page.locator('#bulkReturnSelectedAssets');
  assert.equal(await bulkReturnButton.isDisabled(), false, 'Bulk return must be enabled when every selected asset is assigned.');
  const bulkReturnWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-return').length;
  await bulkReturnButton.click();
  await bulkActionDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#bulkAssetActionDescription').innerText(), /2 seçili varlık/);
  await page.locator('#cancelBulkAssetAction').click();
  await bulkActionDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-return').length, bulkReturnWritesBeforeCancel,
    'Canceling the bulk return dialog must not write.');
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/);
  assert.ok(bulkAssets.every(item => item.status === 'Deployed' && item.assignedEmployeeId === employeeId),
    'Canceling must preserve every assignment and status.');
  await bulkReturnButton.click();
  await bulkActionDialog.waitFor({ state: 'visible' });
  await page.locator('#bulkAssetActionForm [name="note"]').fill('Quarterly device return');
  await page.locator('#submitBulkAssetAction').click();
  await bulkActionDialog.waitFor({ state: 'hidden' });
  await firstBulkRow.getByText('Kullanılabilir').waitFor();
  await secondBulkRow.getByText('Kullanılabilir').waitFor();
  assert.doesNotMatch(await firstBulkRow.innerText(), /Synthetic QA Employee/);
  assert.doesNotMatch(await secondBulkRow.innerText(), /Synthetic QA Employee/);
  const bulkReturnWrite = apiRequests.find(item => item.path === '/api/assets/bulk-return');
  assert.equal(bulkReturnWrite?.method, 'POST');
  assert.deepEqual(bulkReturnWrite?.body, { assetIds: bulkAssetIds, note: 'Quarterly device return' });
  assert.equal(bulkReturnWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(bulkAssets.every(item => item.status === 'Available' && !item.assignedEmployeeId && !item.expectedReturnOn),
    'Confirmed bulk return must clear both assignments and expected-return dates in synthetic state.');
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk return must clear the selection and hide the bulk toolbar.');
  console.log('PASS: bulk asset return preserves assignments on cancel, submits selected records with CSRF and note, and refreshes both returned rows.');

  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  const bulkAssignButton = page.locator('#bulkAssignSelectedAssets');
  assert.equal(await bulkAssignButton.isDisabled(), false, 'Bulk assignment must be enabled for all selected available assets.');
  const bulkAssignWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-assign').length;
  await bulkAssignButton.click();
  const bulkAssignDialogRef = page.locator('#assignDialog');
  await bulkAssignDialogRef.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assignDialog h2').innerText(), /Seçili varlıkları zimmetle/);
  await page.locator('[data-close-assign]').first().click();
  await bulkAssignDialogRef.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-assign').length, bulkAssignWritesBeforeCancel,
    'Closing bulk assignment must not write.');
  assert.ok(bulkAssets.every(item => item.status === 'Available' && !item.assignedEmployeeId),
    'Closing must leave every selected asset available and unassigned.');
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/);
  await bulkAssignButton.click();
  await bulkAssignDialogRef.waitFor({ state: 'visible' });
  const assignForm = page.locator('#assignForm');
  await assignForm.locator('[name="employeeId"]').selectOption(employeeId);
  await assignForm.locator('[name="expectedReturnOn"]').fill('2026-12-31');
  await assignForm.locator('[name="note"]').fill('Quarterly device allocation');
  await assignForm.locator('button[type="submit"]').click();
  await bulkAssignDialogRef.waitFor({ state: 'hidden' });
  await firstBulkRow.getByText('Synthetic QA Employee').waitFor();
  await secondBulkRow.getByText('Synthetic QA Employee').waitFor();
  const bulkAssignWrite = apiRequests.find(item => item.path === '/api/assets/bulk-assign');
  assert.equal(bulkAssignWrite?.method, 'POST');
  assert.deepEqual(bulkAssignWrite?.body, {
    assetIds: bulkAssetIds, employeeId, note: 'Quarterly device allocation', expectedReturnOn: '2026-12-31'
  });
  assert.equal(bulkAssignWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(bulkAssets.every(item => item.status === 'Deployed' && item.assignedEmployeeId === employeeId && item.expectedReturnOn === '2026-12-31'));
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk assignment must clear the selection and hide the bulk toolbar.');
  console.log('PASS: bulk asset assignment dialog cancellation writes nothing; confirmed assignment persists both rows with employee, due date, note, and CSRF.');

  for (const item of bulkAssets) {
    item.status = 'Available';
    item.assignedEmployeeId = null;
    item.assignedEmployeeName = null;
    item.expectedReturnOn = null;
    item.location = 'Head Office';
  }
  await page.reload({ waitUntil: 'domcontentloaded' });
  await firstBulkRow.waitFor({ state: 'visible' });
  await secondBulkRow.waitFor({ state: 'visible' });
  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  await bulkToolbar.waitFor({ state: 'visible' });
  const bulkTransferButton = page.locator('#bulkTransferSelectedAssets');
  assert.equal(await bulkTransferButton.isDisabled(), false, 'Bulk transfer must be enabled for available unassigned assets.');
  const bulkTransferWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-transfer').length;
  await bulkTransferButton.click();
  const bulkTransferDialog = page.locator('#assetTransferDialog');
  await bulkTransferDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetTransferDialog h2').innerText(), /Seçili varlıkları yeni konuma aktar/);
  assert.equal(await page.locator('#transferSourceLocation').inputValue(), 'Seçili kayıtların kaynak konumları');
  await page.locator('[data-close-asset-transfer]').first().click();
  await bulkTransferDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-transfer').length, bulkTransferWritesBeforeCancel,
    'Canceling bulk transfer must not write.');
  assert.ok(bulkAssets.every(item => item.location === 'Head Office'), 'Canceling must leave locations unchanged.');
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/);
  await bulkTransferButton.click();
  await bulkTransferDialog.waitFor({ state: 'visible' });
  const transferForm = page.locator('#assetTransferForm');
  await transferForm.locator('[name="targetLocation"]').fill('West Wing');
  await transferForm.locator('[name="notes"]').fill('Quarterly floor move');
  await transferForm.locator('button[type="submit"]').click();
  await bulkTransferDialog.waitFor({ state: 'hidden' });
  await firstBulkRow.getByText('West Wing').waitFor();
  await secondBulkRow.getByText('West Wing').waitFor();
  const bulkTransferWrite = apiRequests.find(item => item.path === '/api/assets/bulk-transfer');
  assert.equal(bulkTransferWrite?.method, 'POST');
  assert.deepEqual(bulkTransferWrite?.body, { assetIds: bulkAssetIds, targetLocation: 'West Wing', notes: 'Quarterly floor move' });
  assert.equal(bulkTransferWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(bulkAssets.every(item => item.location === 'West Wing'));
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk transfer must clear the selection and hide the bulk toolbar.');
  console.log('PASS: bulk asset transfer cancellation preserves locations; confirmed transfer moves both selected rows with note and CSRF.');

  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  const bulkAuditButton = page.locator('#bulkAuditSelectedAssets');
  assert.equal(await bulkAuditButton.isDisabled(), false, 'Bulk audit must be enabled for selected assets.');
  const bulkAuditWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/assets/bulk-audit').length;
  await bulkAuditButton.click();
  const bulkAuditDialog = page.locator('#assetAuditDialog');
  await bulkAuditDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#assetAuditDialog h2').innerText(), /Seçili varlıkları denetle/);
  assert.equal(await page.locator('#assetAuditForm [name="auditDate"]').inputValue(),
    await page.evaluate(() => { const date = new Date(); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); return date.toISOString().slice(0, 10); }));
  await page.locator('[data-close-asset-audit]').first().click();
  await bulkAuditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-audit').length, bulkAuditWritesBeforeCancel,
    'Canceling bulk audit must not write.');
  assert.ok(bulkAssets.every(item => !item.nextAuditOn), 'Canceling must not change the next-audit schedule.');
  assert.match(await page.locator('#selectedAssetCount').innerText(), /2 varlık seçildi/);
  await bulkAuditButton.click();
  await bulkAuditDialog.waitFor({ state: 'visible' });
  const bulkAuditDate = await page.locator('#assetAuditForm [name="auditDate"]').inputValue();
  const bulkNextAuditOn = await page.evaluate(() => {
    const date = new Date(); date.setDate(date.getDate() + 60); date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    return date.toISOString().slice(0, 10);
  });
  await page.locator('#assetAuditForm [name="nextAuditOn"]').fill(bulkNextAuditOn);
  await page.locator('#assetAuditForm [name="notes"]').fill('Quarterly physical audit');
  await page.locator('#assetAuditForm button[type="submit"]').click();
  await bulkAuditDialog.waitFor({ state: 'hidden' });
  assert.ok(bulkAssets.every(item => item.nextAuditOn === bulkNextAuditOn),
    'Confirmed bulk audit must persist next-audit schedules for all selected assets.');
  const bulkAuditWrite = apiRequests.find(item => item.path === '/api/assets/bulk-audit');
  assert.equal(bulkAuditWrite?.method, 'POST');
  assert.deepEqual(bulkAuditWrite?.body, {
    assetIds: bulkAssetIds, auditDate: bulkAuditDate, nextAuditOn: bulkNextAuditOn, notes: 'Quarterly physical audit',
    outcome: 'Accepted', issueDescription: null
  });
  assert.equal(bulkAuditWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk audit must clear the selection and hide the bulk toolbar.');
  console.log('PASS: bulk asset audit cancellation writes nothing; confirmed audit records date/note and persists the next schedule for both selected assets.');

  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
  await bulkAuditButton.click();
  await bulkAuditDialog.waitFor({ state: 'visible' });
  await page.locator('#auditOutcome').selectOption('Unacceptable');
  const auditFinding = page.locator('#auditIssueDescription');
  assert.equal(await auditFinding.isVisible(), true, 'Unacceptable bulk audits must show the finding field.');
  assert.equal(await auditFinding.getAttribute('required'), '', 'An unacceptable bulk audit must require a finding.');
  const unacceptableWritesBeforeRequiredField = apiRequests.filter(item => item.path === '/api/assets/bulk-audit').length;
  await page.locator('#assetAuditForm button[type="submit"]').click();
  assert.equal(apiRequests.filter(item => item.path === '/api/assets/bulk-audit').length, unacceptableWritesBeforeRequiredField,
    'An unacceptable bulk audit without a finding must not write.');
  await auditFinding.fill('Battery cover cracked; remove from service.');
  await page.locator('#assetAuditForm [name="notes"]').fill('Cracked casing found');
  await page.locator('#assetAuditForm button[type="submit"]').click();
  await bulkAuditDialog.waitFor({ state: 'hidden' });
  const unacceptableBulkAuditWrite = apiRequests.filter(item => item.path === '/api/assets/bulk-audit').at(-1);
  assert.equal(unacceptableBulkAuditWrite?.method, 'POST');
  assert.deepEqual(unacceptableBulkAuditWrite?.body, {
    assetIds: bulkAssetIds, auditDate: bulkAuditDate, nextAuditOn: null, notes: 'Cracked casing found',
    outcome: 'Unacceptable', issueDescription: 'Battery cover cracked; remove from service.'
  });
  assert.equal(unacceptableBulkAuditWrite?.csrf, 'synthetic-asset-csrf');
  assert.ok(unacceptableBulkAuditWrite.responseBody.auditEvents.every(item => item.eventType === 'AuditUnacceptable' &&
    item.outcome === 'Unacceptable' && item.issueDescription === 'Battery cover cracked; remove from service.'),
  'The unacceptable outcome and finding must be recorded for each selected asset.');
  assert.ok(bulkAssets.every(item => item.nextAuditOn === null), 'An omitted next-audit date must clear the previous schedule.');
  console.log('PASS: bulk unacceptable audit requires a finding, preserves CSRF, records findings for every selected asset, and supports clearing its schedule.');

  const sidebarGroups = page.locator('#sidebar [data-group]');
  for (const group of await sidebarGroups.all()) {
    if (await group.getAttribute('aria-expanded') !== 'true') await group.click();
  }
  const movementGroupToggle = page.locator('[data-group="movement"]');
  if (await movementGroupToggle.getAttribute('aria-expanded') !== 'true') await movementGroupToggle.click();
  await page.locator('#sidebar [data-page="Rezervasyonlar"]').click();
  await page.locator('#reservationsPage').waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#reservationAsset')?.options.length > 1
    && document.querySelector('#reservationEmployee')?.options.length > 1);
  const reservationForm = page.locator('#reservationForm');
  await reservationForm.locator('[name="assetId"]').selectOption(reservationAssets[2].id);
  await reservationForm.locator('[name="employeeId"]').selectOption(employeeId);
  await reservationForm.locator('[name="startsOn"]').fill('2026-10-27');
  await reservationForm.locator('[name="endsOn"]').fill('2026-10-26');
  await reservationForm.locator('[name="requestNote"]').fill('Synthetic date validation');
  const reservationWritesBeforeInvalidDate = reservationWrites.length;
  await reservationForm.locator('#reservationSubmit').click();
  assert.equal(reservationWrites.length, reservationWritesBeforeInvalidDate, 'An end date before the start date must not create a reservation.');
  await reservationForm.locator('[name="endsOn"]').fill('2026-10-28');
  await reservationForm.locator('[name="requestNote"]').fill('Synthetic reservation request');
  await reservationForm.locator('#reservationSubmit').click();
  await page.locator('#reservationRows tr').filter({ hasText: 'QA-RES-003' }).waitFor();
  const createdReservationWrite = reservationWrites.find(item => item.path === '/api/reservations' && item.body?.assetId === reservationAssets[2].id);
  assert.equal(createdReservationWrite?.method, 'POST');
  assert.deepEqual(createdReservationWrite?.body, {
    assetId: reservationAssets[2].id, employeeId, startsOn: '2026-10-27', endsOn: '2026-10-28', requestNote: 'Synthetic reservation request'
  });
  assert.equal(createdReservationWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(await reservationForm.locator('[name="assetId"]').inputValue(), '', 'Successful creation must reset the form.');

  const approvedRow = page.locator('#reservationRows tr').filter({ hasText: 'QA-RES-001' });
  await approvedRow.getByRole('button', { name: 'Onayla' }).click();
  const reservationDialog = page.locator('#reservationActionDialog');
  await reservationDialog.waitFor({ state: 'visible' });
  assert.match(await page.locator('#reservationActionSummary').innerText(), /QA-RES-001/);
  await page.locator('#reservationActionCancel').click();
  await reservationDialog.waitFor({ state: 'hidden' });
  assert.equal(reservationWrites.filter(item => item.path.endsWith('/decision')).length, 0, 'Dismissing a decision dialog must not write.');
  assert.equal(reservationRows[0].status, 'Pending', 'Dismissing the dialog must preserve the pending reservation.');
  await approvedRow.getByRole('button', { name: 'Onayla' }).click();
  await page.locator('#reservationReviewNote').fill('Synthetic approval note');
  await page.locator('#reservationActionSubmit').click();
  await reservationDialog.waitFor({ state: 'hidden' });
  await approvedRow.getByText('Onaylandı').waitFor();
  const approvalWrite = reservationWrites.find(item => item.path.endsWith('/decision') && item.body?.approve === true);
  assert.equal(approvalWrite?.method, 'POST');
  assert.deepEqual(approvalWrite?.body, { approve: true, note: 'Synthetic approval note' });
  assert.equal(approvalWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(reservationRows[0].reviewNote, 'Synthetic approval note', 'The decision note must remain in the refreshed row.');

  const rejectedRow = page.locator('#reservationRows tr').filter({ hasText: 'QA-RES-002' });
  await rejectedRow.getByRole('button', { name: 'Reddet' }).click();
  await page.locator('#reservationReviewNote').fill('Synthetic rejection note');
  await page.locator('#reservationActionSubmit').click();
  await rejectedRow.getByText('Reddedildi').waitFor();
  assert.equal(reservationRows[1].status, 'Rejected');
  assert.equal(reservationRows[1].reviewNote, 'Synthetic rejection note');
  const rejectionWrite = reservationWrites.find(item => item.path.endsWith('/decision') && item.body?.approve === false);
  assert.equal(rejectionWrite?.csrf, 'synthetic-asset-csrf');

  await page.locator('#reservationStatus').selectOption('');
  const createdRow = page.locator('#reservationRows tr').filter({ hasText: 'QA-RES-003' });
  await createdRow.waitFor();
  const reservationCancelWritesBeforeDismiss = reservationWrites.filter(item => item.path.endsWith('/cancel')).length;
  await createdRow.getByRole('button', { name: 'İptal et' }).click();
  await reservationDialog.waitFor({ state: 'visible' });
  await page.locator('#reservationActionCancel').click();
  await reservationDialog.waitFor({ state: 'hidden' });
  assert.equal(reservationWrites.filter(item => item.path.endsWith('/cancel')).length, reservationCancelWritesBeforeDismiss,
    'Dismissing a reservation cancellation dialog must not write.');
  await createdRow.getByRole('button', { name: 'İptal et' }).click();
  await page.locator('#reservationActionSubmit').click();
  await createdRow.getByText('İptal edildi').waitFor();
  const cancellationWrite = reservationWrites.find(item => item.path.endsWith('/cancel'));
  assert.equal(cancellationWrite?.method, 'POST');
  assert.equal(cancellationWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(reservationRows[2].status, 'Cancelled');

  await page.locator('#reservationStatus').selectOption('Approved');
  await page.locator('#reservationRows tr').filter({ hasText: 'QA-RES-001' }).waitFor();
  assert.equal(await page.locator('#reservationRows tr').count(), 1, 'Status filtering must return only matching reservations.');
  assert.ok(requests.some(item => item.path === '/api/reservations' && item.query.status === 'Approved' && item.query.page === '1'),
    'Changing the status filter must reload page one with the selected status.');
  const reservationSearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/reservations' && url.searchParams.get('q') === 'QA-RES-001';
  });
  await page.locator('#reservationSearch').fill('QA-RES-001');
  await reservationSearchRequest;
  await page.waitForFunction(() => document.querySelector('#reservationRows')?.innerText.includes('QA-RES-001'));
  assert.ok(requests.some(item => item.path === '/api/reservations' && item.query.q === 'QA-RES-001'), 'Reservation search must reach the API query.');
  const reservationWritesBeforeRefresh = reservationWrites.length;
  await page.locator('#reservationRefresh').click();
  assert.equal(reservationWrites.length, reservationWritesBeforeRefresh, 'Refreshing reservations must remain read-only.');
  console.log('PASS: reservations reject invalid date ranges without writing; create with CSRF; approve/reject notes persist; approve/cancel dismissal writes nothing; confirmed cancellation uses CSRF and persists; status/search filters reload correctly; refresh is read-only.');

  const writesBeforeActivityAcceptance = apiRequests.length;
  await page.locator('#sidebar [data-page="Transferler"]').click();
  await page.locator('#activityPage').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#activityHeading').innerText(), 'Varlık transferleri');
  assert.equal(await page.locator('#activityType').inputValue(), 'Transferred');
  await page.locator('#activityTableRows').getByText('QA-TRANSFER-001').waitFor();
  assert.ok(requests.some(item => item.path === '/api/activity' && item.query.eventType === 'Transferred'),
    'Transferler must query only transferred activity events.');

  await page.locator('#sidebar [data-page="Hareket geçmişi"]').click();
  assert.equal(await page.locator('#activityType').inputValue(), '', 'The general history route must clear the transfer-only filter.');
  await page.locator('#activityTableRows').getByText('QA-ACTIVITY-002').waitFor();
  const activitySearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/activity' && url.searchParams.get('q') === 'QA-ACTIVITY-014';
  });
  await page.locator('#activitySearch').fill('QA-ACTIVITY-014');
  await activitySearchRequest;
  const activitySearchRow = page.locator('#activityTableRows tr').filter({ hasText: 'QA-ACTIVITY-014' });
  await activitySearchRow.waitFor();
  assert.ok(requests.some(item => item.path === '/api/activity' && item.query.q === 'QA-ACTIVITY-014'),
    'Activity search must send its query to the API.');
  await activitySearchRow.press('Enter');
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#detailTagTop').innerText(), asset.assetTag, 'Keyboard activation must open the linked asset details.');
  await page.locator('#closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });

  const activityFromRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/activity' && Boolean(url.searchParams.get('from'));
  });
  await page.locator('#activityFrom').fill('2026-10-15');
  await activityFromRequest;
  const activityDateResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/activity' && Boolean(url.searchParams.get('from')) && Boolean(url.searchParams.get('to'));
  });
  await page.locator('#activityTo').fill('2026-10-15');
  assert.equal((await activityDateResponse).status(), 200);
  const dateRequest = requests.findLast(item => item.path === '/api/activity' && item.query.from && item.query.to);
  assert.ok(dateRequest, 'Activity date controls must send both date boundaries.');
  assert.match(dateRequest.query.from, /^2026-10-15T/);
  assert.match(dateRequest.query.to, /^2026-10-15T/);

  const resetSearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/activity' && !url.searchParams.has('q');
  });
  await page.locator('#activitySearch').fill('');
  await resetSearchRequest;
  await page.locator('#activityNext').click();
  await page.locator('#activityTableRows').getByText('QA-ACTIVITY-014').waitFor();
  assert.ok(requests.some(item => item.path === '/api/activity' && item.query.page === '2'),
    'Activity next-page control must request the second page.');
  await page.locator('#activityPrev').click();
  await page.locator('#activityTableRows').getByText('QA-ACTIVITY-002').waitFor();
  const activityWritesBeforeRefresh = apiRequests.length;
  failNextActivityLoad = true;
  await page.locator('#activityRefresh').click();
  await page.locator('#activityTableRows').getByText('Hareket geçmişi yüklenemedi').waitFor();
  assert.equal(await page.locator('#activityNext').isDisabled(), true, 'An activity API error must disable paging on its empty error state.');
  await page.locator('#activityRefresh').click();
  await page.locator('#activityTableRows').getByText('QA-ACTIVITY-002').waitFor();
  assert.equal(apiRequests.length, activityWritesBeforeRefresh, 'Filtering, paging, opening a linked asset and retrying history must not write data.');
  assert.ok(requests.some(item => item.path === '/api/activity' && item.query.page === '1'), 'Activity retry must reload the current page.');
  console.log('PASS: Transferler preset and general history routes; activity search/date/paging, keyboard-linked asset opening, empty/error recovery, retry, and read-only behavior.');

  const writesBeforeWarrantyAcceptance = apiRequests.length;
  await page.locator('#sidebar [data-page="Garanti takibi"]').click();
  await page.locator('#warrantyReportRows tr').filter({ hasText: 'QA-WARRANTY-UPCOMING' }).waitFor();
  assert.equal(await page.locator('#warrantyReportRows tr').count(), 1, 'The default warranty route must show only upcoming warranties in its default horizon.');
  assert.equal(await page.locator('#warrantyReportNext').isDisabled(), true);
  await page.locator('#warrantyReportState').selectOption('expired');
  await page.locator('#warrantyReportRows tr').filter({ hasText: 'QA-WARRANTY-EXPIRED' }).waitFor();
  assert.ok(requests.some(item => item.path === '/api/reports/warranties' && item.query.state === 'expired' && item.query.page === '1'),
    'The expired warranty filter must reload page one through the company report API.');
  await page.locator('#warrantyReportState').selectOption('upcoming');
  const warrantyHorizonResponse = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/reports/warranties' && url.searchParams.get('state') === 'upcoming'
      && url.searchParams.get('through') !== null;
  });
  const expectedWarrantyThrough = await page.evaluate(() => {
    const today = new Date();
    return new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() + 30)).toISOString().slice(0, 10);
  });
  await page.locator('#warrantyReportHorizon').selectOption('30');
  const warrantyHorizonRequest = await warrantyHorizonResponse;
  const warrantyThrough = new URL(warrantyHorizonRequest.url()).searchParams.get('through');
  const expectedNextDay = new Date(`${expectedWarrantyThrough}T00:00:00Z`); expectedNextDay.setUTCDate(expectedNextDay.getUTCDate() + 1);
  assert.ok(warrantyThrough === expectedWarrantyThrough || warrantyThrough === expectedNextDay.toISOString().slice(0, 10),
    'Selecting 30 days must send a matching horizon to the API.');
  const warrantySearchRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/reports/warranties' && url.searchParams.get('q') === 'QA-WARRANTY-UPCOMING';
  });
  await page.locator('#warrantyReportSearch').fill('QA-WARRANTY-UPCOMING');
  await warrantySearchRequest;
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-UPCOMING').waitFor();
  assert.ok(requests.some(item => item.path === '/api/reports/warranties' && item.query.q === 'QA-WARRANTY-UPCOMING'),
    'Warranty search must reach the report API.');

  await page.locator('#warrantyReportSearch').fill('');
  const allWarrantiesResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/warranties' && url.searchParams.get('state') === 'all'
      && url.searchParams.get('page') === '1' && !url.searchParams.has('q');
  });
  await page.locator('#warrantyReportState').selectOption('all');
  await allWarrantiesResponse;
  await page.locator('#warrantyReportRows tr').filter({ hasText: 'QA-WARRANTY-001' }).waitFor();
  assert.equal(await page.locator('#warrantyReportNext').isDisabled(), false, 'The warranty report must enable paging when more than 50 records match.');
  const warrantyNextPageResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/warranties' && url.searchParams.get('state') === 'all' && url.searchParams.get('page') === '2';
  });
  await page.locator('#warrantyReportNext').click();
  await warrantyNextPageResponse;
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-051').waitFor();
  assert.ok(requests.some(item => item.path === '/api/reports/warranties' && item.query.page === '2' && item.query.state === 'all'),
    'Warranty pagination must request the next page with current filters.');
  const warrantyPreviousPageResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/warranties' && url.searchParams.get('state') === 'all' && url.searchParams.get('page') === '1';
  });
  await page.locator('#warrantyReportPrev').click();
  await warrantyPreviousPageResponse;
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-001').waitFor();
  await page.locator('#warrantyReportState').selectOption('later');
  await page.locator('#warrantyReportSearch').fill('QA-WARRANTY-LATER');
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-LATER').waitFor();
  assert.equal(await page.locator('#warrantyReportRows img').count(), 0, 'Warranty asset names must be rendered as text, not executable HTML.');
  await page.locator('#warrantyReportState').selectOption('missing');
  await page.locator('#warrantyReportSearch').fill('QA-WARRANTY-MISSING');
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-MISSING').waitFor();
  await page.locator('#warrantyReportState').selectOption('expired');
  await page.locator('#warrantyReportSearch').fill('QA-WARRANTY-EXPIRED');
  const [warrantyDownload] = await Promise.all([
    page.waitForEvent('download'), page.locator('#warrantyReportExport').click()
  ]);
  assert.equal(warrantyDownload.suggestedFilename(), 'synthetic-warranties.csv');
  const warrantyExport = requests.findLast(item => item.path === '/api/reports/warranties/export.csv');
  assert.ok(warrantyExport && warrantyExport.query.state === 'expired' && warrantyExport.query.q === 'QA-WARRANTY-EXPIRED' && warrantyExport.query.through,
    'Warranty CSV export must preserve filters and horizon.');
  assert.equal(warrantyExport.query.page, undefined, 'Warranty CSV export must not include pagination.');
  await page.locator('#warrantyReportSearch').fill('');
  await page.locator('#warrantyReportState').selectOption('upcoming');
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-UPCOMING').waitFor();
  await page.locator('#warrantyReportRows').getByText('QA-WARRANTY-UPCOMING').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  await page.locator('#warrantyReportRefresh').click();
  assert.equal(apiRequests.length, writesBeforeWarrantyAcceptance, 'Warranty filters, linked asset opening, CSV export, and refresh must not write company data.');
  console.log('PASS: warranty route, default/state/horizon/search filters, 50-row pagination, safe text rendering, filtered CSV download, asset-detail shortcut, refresh, and read-only behavior.');

  const reportsPageResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/reports/assets');
  const licenseExpiryDefaultResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'upcoming' && url.searchParams.get('page') === '1';
  });
  await page.locator('#sidebar button[data-page="Hazır raporlar"]').click();
  await Promise.all([reportsPageResponse, licenseExpiryDefaultResponse]);
  await page.locator('#reportSummary .stat strong').first().getByText('51').waitFor();
  await page.locator('#assetPurchaseReportRows').getByText('QA-REPORT-001').waitFor();
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-UPCOMING').waitFor();
  assert.deepEqual(await page.locator('#licenseReportSummary .stat strong').allInnerTexts(), ['1', '1', '1'],
    'License expiry summary must show expired, upcoming, and missing-date counts independently of the selected state.');
  const defaultLicenseRequest = requests.findLast(item => item.path === '/api/reports/software-licenses' && item.query.state === 'upcoming');
  assert.equal(defaultLicenseRequest?.query.pageSize, '50');
  assert.equal(new Date(`${defaultLicenseRequest.query.through}T00:00:00Z`).getTime() - new Date(`${licenseReportDay(0)}T00:00:00Z`).getTime(), 90 * 86400000,
    'The default license expiry horizon must be 90 days.');
  const thirtyDayLicenseResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('through') === licenseReportDay(30);
  });
  await page.locator('#licenseReportHorizon').selectOption('30');
  await thirtyDayLicenseResponse;
  const expiredLicenseResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'expired';
  });
  await page.locator('#licenseReportState').selectOption('expired');
  await expiredLicenseResponse;
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-EXPIRED').waitFor();
  assert.equal(await page.locator('#licenseReportRows tr').count(), 1, 'Expired-license state must exclude future and missing expiration dates.');
  const publisherSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('q') === 'QA Publisher';
  });
  await page.locator('#licenseReportSearch').fill('QA Publisher');
  await publisherSearchResponse;
  assert.ok(requests.some(item => item.path === '/api/reports/software-licenses' && item.query.state === 'expired' && item.query.q === 'QA Publisher'),
    'License search must include publisher text and preserve the selected state.');
  const missingLicenseResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'missing';
  });
  await page.locator('#licenseReportState').selectOption('missing');
  await missingLicenseResponse;
  await page.locator('#licenseReportSearch').fill('');
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-MISSING').waitFor();
  assert.match(await page.locator('#licenseReportRows').innerText(), /Süre belirtilmemiş/);
  const pagedLicenseResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'all' && !url.searchParams.has('q');
  });
  await page.locator('#licenseReportState').selectOption('all');
  await pagedLicenseResponse;
  const reportLicenseSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'all' && url.searchParams.get('q') === 'QA-LICENSE-PAGE';
  });
  await page.locator('#licenseReportSearch').fill('QA-LICENSE-PAGE');
  await reportLicenseSearchResponse;
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-PAGE-001').waitFor();
  assert.equal(await page.locator('#licenseReportNext').isDisabled(), false, 'License report must enable pagination when more than 50 records match.');
  const licenseNextPageResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'all' && url.searchParams.get('q') === 'QA-LICENSE-PAGE' && url.searchParams.get('page') === '2';
  });
  await page.locator('#licenseReportNext').click();
  await licenseNextPageResponse;
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-PAGE-051').waitFor();
  assert.equal(await page.locator('#licenseReportPageInfo').innerText(), 'Sayfa 2 / 2 · 51 kayıt');
  await page.locator('#licenseReportPrev').click();
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-PAGE-001').waitFor();
  await page.locator('#licenseReportState').selectOption('expired');
  const expiredLicenseSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'expired' && url.searchParams.get('q') === 'QA-LICENSE-PAGE';
  });
  await expiredLicenseSearchResponse;
  const expiredTagSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/reports/software-licenses' && url.searchParams.get('state') === 'expired' && url.searchParams.get('q') === 'QA-LICENSE-EXPIRED';
  });
  await page.locator('#licenseReportSearch').fill('QA-LICENSE-EXPIRED');
  await expiredTagSearchResponse;
  const [licenseReportDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#licenseReportExport').click()]);
  assert.equal(licenseReportDownload.suggestedFilename(), 'synthetic-license-expiry.csv');
  const licenseReportStream = await licenseReportDownload.createReadStream(), licenseReportChunks = [];
  for await (const chunk of licenseReportStream) licenseReportChunks.push(Buffer.from(chunk));
  const licenseExpiryCsv = Buffer.concat(licenseReportChunks).toString('utf8');
  assert.match(licenseExpiryCsv, /QA-LICENSE-EXPIRED/);
  assert.doesNotMatch(licenseExpiryCsv, /QA-LICENSE-UPCOMING/);
  const licenseReportExport = requests.findLast(item => item.path === '/api/reports/software-licenses/export.csv');
  assert.equal(licenseReportExport?.query.state, 'expired');
  assert.equal(licenseReportExport?.query.through, licenseReportDay(30));
  assert.equal(licenseReportExport?.query.q, 'QA-LICENSE-EXPIRED', 'License CSV must preserve the current search filter.');
  assert.equal(licenseReportExport?.query.page, undefined, 'License CSV must not include pagination parameters.');
  const licenseReportWritesBeforeRefresh = apiRequests.length;
  await page.locator('#licenseReportRefresh').click();
  await page.locator('#licenseReportRows').getByText('QA-LICENSE-EXPIRED').waitFor();
  assert.equal(apiRequests.length, licenseReportWritesBeforeRefresh, 'License report search, filters, pagination, export, and refresh must remain read-only.');
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-OVERDUE').waitFor();
  assert.deepEqual(await page.locator('#pendingDeliverySummary .stat strong').allInnerTexts(), ['1', '1', '522'],
    'Pending delivery summary must show overdue orders, undated lines, and remaining units.');
  assert.match(await page.locator('#pendingDeliveryRows').innerText(), /Synthetic overdue device/);
  const overdueDeliveries = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'overdue';
  });
  await page.locator('#pendingDeliveryState').selectOption('overdue'); await overdueDeliveries;
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-DUE30').waitFor({ state: 'detached' });
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-UNDATED').waitFor({ state: 'detached' });
  assert.equal(await page.locator('#pendingDeliveryRows tr').count(), 1, 'Overdue filter must exclude due and undated lines.');
  const dueDeliveries = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'due30';
  });
  await page.locator('#pendingDeliveryState').selectOption('due30'); await dueDeliveries;
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-OVERDUE').waitFor({ state: 'detached' });
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-DUE30').waitFor();
  assert.equal(await page.locator('#pendingDeliveryRows tr').count(), 1, 'Next-30-days filter must include dates through day 30 and exclude undated lines.');
  const undatedDeliveries = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'undated';
  });
  await page.locator('#pendingDeliveryState').selectOption('undated'); await undatedDeliveries;
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-UNDATED').waitFor();
  assert.match(await page.locator('#pendingDeliveryRows').innerText(), /Tarih belirtilmemiş/);
  const allDeliveries = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'all';
  });
  await page.locator('#pendingDeliveryState').selectOption('all'); await allDeliveries;
  const deliverySearch = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('q') === 'QA-DELIVERY-PAGE';
  });
  await page.locator('#pendingDeliverySearch').fill('QA-DELIVERY-PAGE'); await deliverySearch;
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-PAGE-001').waitFor();
  assert.equal(await page.locator('#pendingDeliveryNext').isDisabled(), false, 'Pending delivery report must enable pagination with more than 50 matches.');
  const deliveryPageTwo = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('q') === 'QA-DELIVERY-PAGE' && url.searchParams.get('page') === '2';
  });
  await page.locator('#pendingDeliveryNext').click(); await deliveryPageTwo;
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-PAGE-051').waitFor();
  assert.match(await page.locator('#pendingDeliveryPageInfo').innerText(), /Sayfa 2 \/ 2/);
  const deliveryFirstPage = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('q') === 'QA-DELIVERY-PAGE' && url.searchParams.get('page') === '1';
  });
  await page.locator('#pendingDeliveryPrev').click(); await deliveryFirstPage;
  const dueStateAgain = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'due30' && url.searchParams.get('q') === 'QA-DELIVERY-PAGE';
  });
  await page.locator('#pendingDeliveryState').selectOption('due30'); await dueStateAgain;
  const filteredDeliverySearch = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/pending-deliveries' && url.searchParams.get('state') === 'due30' && url.searchParams.get('q') === 'QA-DELIVERY-DUE30';
  });
  await page.locator('#pendingDeliverySearch').fill('QA-DELIVERY-DUE30'); await filteredDeliverySearch;
  const [deliveryDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#pendingDeliveryExport').click()]);
  assert.equal(deliveryDownload.suggestedFilename(), 'synthetic-pending-deliveries.csv');
  const deliveryStream = await deliveryDownload.createReadStream(), deliveryChunks = [];
  for await (const chunk of deliveryStream) deliveryChunks.push(Buffer.from(chunk));
  const deliveryCsv = Buffer.concat(deliveryChunks).toString('utf8');
  assert.match(deliveryCsv, /QA-DELIVERY-DUE30/);
  assert.doesNotMatch(deliveryCsv, /QA-DELIVERY-OVERDUE/);
  const deliveryExport = requests.findLast(item => item.path === '/api/reports/pending-deliveries/export.csv');
  assert.equal(deliveryExport?.query.state, 'due30');
  assert.equal(deliveryExport?.query.q, 'QA-DELIVERY-DUE30');
  assert.equal(deliveryExport?.query.page, undefined, 'Pending delivery CSV must export all filtered rows, not just the current page.');
  const deliveryWritesBeforeRefresh = apiRequests.length;
  await page.locator('#pendingDeliveryRefresh').click();
  await page.locator('#pendingDeliveryRows').getByText('QA-DELIVERY-DUE30').waitFor();
  assert.equal(apiRequests.length, deliveryWritesBeforeRefresh, 'Pending delivery filters, paging, CSV and refresh must remain read-only.');
  await page.locator('#depreciationReportRows').getByText('QA-DEPR-LINEAR').waitFor();
  assert.equal(await page.locator('#depreciationReportRows tr').count(), 50, 'Depreciation report must initially page at 50 rows.');
  assert.match(await page.locator('#depreciationReportRows').innerText(), /Doğrusal/);
  assert.match(await page.locator('#depreciationReportRows').innerText(), /9\.600/);
  assert.match(await page.locator('#depreciationReportSummary').innerText(), /TRY/);
  assert.match(await page.locator('#depreciationReportSummary').innerText(), /USD/);
  const changedDepreciationDate = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation' && url.searchParams.get('asOf') === '2026-11-16';
  });
  await page.locator('#depreciationReportAsOf').fill('2026-11-16'); await changedDepreciationDate;
  assert.match(await page.locator('#depreciationReportNote').innerText(), /2026/);
  const depreciationSearch = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation' && url.searchParams.get('q') === 'QA-DEPR-LINEAR';
  });
  await page.locator('#depreciationReportSearch').fill('QA-DEPR-LINEAR'); await depreciationSearch;
  await page.locator('#depreciationReportRows').getByText('QA-DEPR-LINEAR').waitFor();
  assert.equal(await page.locator('#depreciationReportRows tr').count(), 1, 'Depreciation search must filter asset tag and associated report values.');
  const allDepreciationSearch = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation' && url.searchParams.get('q') === 'QA-DEPR-PAGE';
  });
  await page.locator('#depreciationReportSearch').fill('QA-DEPR-PAGE'); await allDepreciationSearch;
  assert.equal(await page.locator('#depreciationReportNext').isDisabled(), false, 'Depreciation report must enable paging when more than 50 rows match.');
  const depreciationPageTwo = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation' && url.searchParams.get('q') === 'QA-DEPR-PAGE' && url.searchParams.get('page') === '2';
  });
  await page.locator('#depreciationReportNext').click(); await depreciationPageTwo;
  await page.locator('#depreciationReportRows').getByText('QA-DEPR-PAGE-051').waitFor();
  assert.match(await page.locator('#depreciationReportPageInfo').innerText(), /Sayfa 2 \/ 2/);
  const depreciationPageOne = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation' && url.searchParams.get('q') === 'QA-DEPR-PAGE' && url.searchParams.get('page') === '1';
  });
  await page.locator('#depreciationReportPrev').click(); await depreciationPageOne;
  const filteredDepreciation = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/depreciation/export.csv';
  });
  const [depreciationDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#depreciationReportExport').click()]);
  await filteredDepreciation;
  assert.equal(depreciationDownload.suggestedFilename(), 'synthetic-depreciation-20261116.csv');
  const depreciationStream = await depreciationDownload.createReadStream(), depreciationChunks = [];
  for await (const chunk of depreciationStream) depreciationChunks.push(Buffer.from(chunk));
  const depreciationCsv = Buffer.concat(depreciationChunks).toString('utf8');
  assert.match(depreciationCsv, /QA-DEPR-PAGE-001/);
  assert.doesNotMatch(depreciationCsv, /QA-DEPR-LINEAR/);
  const depreciationExport = requests.findLast(item => item.path === '/api/reports/depreciation/export.csv');
  assert.equal(depreciationExport?.query.asOf, '2026-11-16', 'Depreciation CSV must preserve the report date.');
  assert.equal(depreciationExport?.query.q, 'QA-DEPR-PAGE', 'Depreciation CSV must preserve the search filter.');
  assert.equal(depreciationExport?.query.page, undefined, 'Depreciation CSV must export all filtered records, not only the visible page.');
  const depreciationWritesBeforeRefresh = apiRequests.length;
  await page.locator('#depreciationReportRefresh').click();
  await page.locator('#depreciationReportRows').getByText('QA-DEPR-PAGE-001').waitFor();
  assert.equal(apiRequests.length, depreciationWritesBeforeRefresh, 'Depreciation filters, paging, CSV and refresh must remain read-only.');
  await page.locator('#componentReportRows').getByText('Peripherals').waitFor();
  assert.equal(await page.locator('#componentReportRows tr').count(), 2, 'Component report must render each category from the report API.');
  assert.deepEqual(await page.locator('#componentReportSummary .stat strong').allInnerTexts(), ['2', '17', '4', '13'],
    'Component report summary must show component count, total, allocated and available stock.');
  assert.match(await page.locator('#componentReportNote').innerText(), /2 kategori/);
  const [componentReportDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#componentReportExport').click()]);
  assert.equal(componentReportDownload.suggestedFilename(), 'synthetic-component-stock.csv');
  const componentReportStream = await componentReportDownload.createReadStream(), componentReportChunks = [];
  for await (const chunk of componentReportStream) componentReportChunks.push(Buffer.from(chunk));
  const componentReportCsv = Buffer.concat(componentReportChunks).toString('utf8');
  assert.match(componentReportCsv, /QA-COMP-001/);
  assert.ok(requests.some(item => item.path === '/api/reports/components'), 'Component report summary must load from its API.');
  assert.ok(requests.some(item => item.path === '/api/components/export.csv'), 'Component stock export must use the company CSV endpoint.');
  await page.locator('#auditReportRows').getByText('QA-AUDIT-001').waitFor();
  assert.equal(await page.locator('#auditReportSummary .stat strong').first().innerText(), '26', 'Audit report summary must show all records for current filters.');
  assert.equal(await page.locator('#auditReportSummary .stat strong').nth(1).innerText(), '20', 'Audit report must summarize acceptable results.');
  assert.equal(await page.locator('#auditReportSummary .stat strong').nth(2).innerText(), '6', 'Audit report must summarize unacceptable findings.');
  assert.equal(await page.locator('#auditReportRows tr td:nth-child(2)').filter({ hasText: 'Uygun değil / bulgu var' }).count(), 5,
    'Audit result table must visibly identify unacceptable items on the current page.');
  assert.equal(await page.locator('#auditReportRows tr').count(), 25, 'Audit report must honor its 25-row page size.');
  assert.equal(await page.locator('#auditReportNext').isDisabled(), false, 'Audit report pagination must enable when a second page exists.');
  const auditSecondPage = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/audits' && url.searchParams.get('page') === '2';
  });
  await page.locator('#auditReportNext').click(); await auditSecondPage;
  await page.locator('#auditReportRows').getByText('QA-AUDIT-026').waitFor();
  assert.equal(await page.locator('#auditReportRows tr').count(), 1, 'Last audit report page must show the single remaining row.');
  const auditFirstPage = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/audits' && url.searchParams.get('page') === '1';
  });
  await page.locator('#auditReportPrev').click(); await auditFirstPage;
  await page.locator('#auditReportFrom').fill('2026-10-10');
  await page.locator('#auditReportTo').fill('2026-10-12');
  await page.locator('#auditReportSearch').fill('QA-AUDIT-011');
  const auditFiltered = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/audits' && url.searchParams.get('q') === 'QA-AUDIT-011';
  });
  await page.locator('#auditReportApply').click(); await auditFiltered;
  assert.equal(await page.locator('#auditReportRows tr').count(), 1, 'Audit report must apply date range and asset search together.');
  assert.equal(await page.locator('#auditReportSummary .stat strong').first().innerText(), '1');
  const [auditReportDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#auditReportExport').click()]);
  assert.equal(auditReportDownload.suggestedFilename(), 'synthetic-physical-audits.csv');
  const auditReportStream = await auditReportDownload.createReadStream(), auditReportChunks = [];
  for await (const chunk of auditReportStream) auditReportChunks.push(Buffer.from(chunk));
  assert.match(Buffer.concat(auditReportChunks).toString('utf8'), /QA-AUDIT-011/);
  const auditExportRequest = requests.findLast(item => item.path === '/api/reports/audits/export.csv');
  assert.equal(auditExportRequest?.query.from, '2026-10-10');
  assert.equal(auditExportRequest?.query.to, '2026-10-12');
  assert.equal(auditExportRequest?.query.q, 'QA-AUDIT-011', 'Audit CSV must preserve the date and search filters.');
  assert.equal(apiRequests.length, writesBeforeWarrantyAcceptance, 'Report filters and exports must remain read-only.');
  await page.locator('#maintenanceReportSummary .stat strong').first().getByText('3').waitFor();
  assert.deepEqual(await page.locator('#maintenanceReportSummary .stat strong').allInnerTexts(), ['3', '1', '2'],
    'Maintenance report must count started/completed records while excluding planned work.');
  assert.equal(await page.locator('#maintenanceReportTypes tr').count(), 2, 'Maintenance report must aggregate maintenance types.');
  assert.match(await page.locator('#maintenanceReportCosts').innerText(), /TRY/);
  assert.match(await page.locator('#maintenanceReportCosts').innerText(), /USD/);
  await page.locator('#maintenanceReportFrom').fill('2026-10-10');
  await page.locator('#maintenanceReportTo').fill('2026-10-11');
  const filteredMaintenanceReport = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/maintenances'
      && url.searchParams.get('from') === '2026-10-10' && url.searchParams.get('to') === '2026-10-11';
  });
  await page.locator('#applyMaintenanceReportFilter').click(); await filteredMaintenanceReport;
  await page.waitForFunction(() => JSON.stringify([...document.querySelectorAll('#maintenanceReportSummary .stat strong')].map(node => node.textContent.trim())) === JSON.stringify(['2', '1', '1']));
  assert.deepEqual(await page.locator('#maintenanceReportSummary .stat strong').allInnerTexts(), ['2', '1', '1']);
  assert.match(await page.locator('#maintenanceReportNote').innerText(), /2026-10-10.*2026-10-11/);
  const clearedMaintenanceReport = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/maintenances' && !url.searchParams.has('from') && !url.searchParams.has('to');
  });
  await page.locator('#clearMaintenanceReportFilter').click(); await clearedMaintenanceReport;
  await page.waitForFunction(() => JSON.stringify([...document.querySelectorAll('#maintenanceReportSummary .stat strong')].map(node => node.textContent.trim())) === JSON.stringify(['3', '1', '2']));
  assert.deepEqual(await page.locator('#maintenanceReportSummary .stat strong').allInnerTexts(), ['3', '1', '2']);
  const refreshMaintenanceReport = page.waitForResponse(response => new URL(response.url()).pathname === '/api/reports/maintenances');
  await page.locator('#maintenanceReportRefresh').click(); await refreshMaintenanceReport;
  assert.equal(apiRequests.length, writesBeforeWarrantyAcceptance, 'Maintenance report filter, clear and refresh must remain read-only.');
  assert.equal(await page.locator('#assetPurchaseReportNext').isDisabled(), false, 'Purchase-report pagination must enable when more than 50 rows match.');
  const purchaseSecondPage = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets/purchases' && url.searchParams.get('page') === '2';
  });
  await page.locator('#assetPurchaseReportNext').click(); await purchaseSecondPage;
  await page.locator('#assetPurchaseReportRows').getByText('QA-REPORT-051').waitFor();
  const purchaseFirstPage = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets/purchases' && url.searchParams.get('page') === '1';
  });
  await page.locator('#assetPurchaseReportPrev').click(); await purchaseFirstPage;
  const purchaseSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets/purchases' && url.searchParams.get('q') === 'QA-REPORT-002';
  });
  await page.locator('#assetPurchaseReportSearch').fill('QA-REPORT-002'); await purchaseSearchResponse;
  await page.locator('#assetPurchaseReportRows').getByText('QA-REPORT-002').waitFor();
  assert.equal(await page.locator('#assetPurchaseReportRows tr').count(), 1, 'Purchase-report search must filter the rendered rows.');
  const [purchaseReportDownload] = await Promise.all([
    page.waitForEvent('download'), page.locator('#assetPurchaseReportExport').click()
  ]);
  assert.equal(purchaseReportDownload.suggestedFilename(), 'synthetic-asset-purchases.csv');
  const purchaseExportRequest = requests.findLast(item => item.path === '/api/reports/assets/purchases/export.csv');
  assert.equal(purchaseExportRequest?.query.q, 'QA-REPORT-002', 'Purchase-report CSV must preserve the active search.');
  assert.equal(purchaseExportRequest?.query.page, undefined, 'Purchase-report CSV must export the full filtered result, not only the visible page.');
  await page.locator('#assetPurchaseReportSearch').fill('');
  await page.locator('#reportDateFrom').fill('2026-10-05');
  await page.locator('#reportDateTo').fill('2026-10-10');
  const filteredAssetReport = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets' && url.searchParams.get('purchaseDateFrom') === '2026-10-05' && url.searchParams.get('purchaseDateTo') === '2026-10-10';
  });
  const filteredPurchaseReportResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets/purchases' && url.searchParams.get('from') === '2026-10-05' && url.searchParams.get('to') === '2026-10-10';
  });
  await page.locator('#applyReportDateFilter').click(); await Promise.all([filteredAssetReport, filteredPurchaseReportResponse]);
  const filteredPurchaseReport = requests.findLast(item => item.path === '/api/reports/assets/purchases');
  assert.equal(filteredPurchaseReport?.query.from, '2026-10-05');
  assert.equal(filteredPurchaseReport?.query.to, '2026-10-10');
  assert.match(await page.locator('#reportDateNote').innerText(), /2026-10-05.*2026-10-10/);
  const clearedAssetReport = page.waitForResponse(response => {
    const url = new URL(response.url()); return url.pathname === '/api/reports/assets' && !url.searchParams.has('purchaseDateFrom') && !url.searchParams.has('purchaseDateTo');
  });
  await page.locator('#clearReportDateFilter').click(); await clearedAssetReport;
  const [assetReportDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#reportExport').click()]);
  assert.match(assetReportDownload.suggestedFilename(), /^envantertr-envanter-raporu.*\.csv$/);
  const assetReportStream = await assetReportDownload.createReadStream();
  const assetReportChunks = [];
  for await (const chunk of assetReportStream) assetReportChunks.push(Buffer.from(chunk));
  const assetReportCsv = Buffer.concat(assetReportChunks).toString('utf8');
  assert.match(assetReportCsv, /Synthetic report category/, 'The locally generated prepared-report CSV must contain the current report data.');
  assert.match(assetReportCsv, /65000/);
  const reportPrintCases = [
    ['#reportPrint', /Synthetic report category/],
    ['#assetPurchaseReportPrint', /QA-REPORT-002/],
    ['#componentReportPrint', /Peripherals/],
    ['#auditReportPrint', /QA-AUDIT-011/],
    ['#warrantyReportPrint', /QA-WARRANTY-051/],
    ['#maintenanceReportPrint', /Repair/],
    ['#licenseReportPrint', /QA-LICENSE-/],
    ['#pendingDeliveryPrint', /QA-DELIVERY-/],
    ['#depreciationReportPrint', /QA-DEPR-PAGE-001/]
  ];
  for (const [buttonSelector, expectedContent] of reportPrintCases) {
    const popupPromise = page.waitForEvent('popup');
    await page.locator(buttonSelector).click();
    const preview = await popupPromise;
    await preview.locator('.status').waitFor({ state: 'visible' });
    await preview.waitForFunction(() => document.querySelector('.status')?.textContent.includes('yazdırmaya hazır'));
    assert.ok(await preview.locator('thead th').count() > 0, `${buttonSelector} must render report column headers.`);
    assert.ok(await preview.locator('tbody tr').count() > 0, `${buttonSelector} must render report rows.`);
    assert.match(await preview.locator('tbody').innerText(), expectedContent,
      `${buttonSelector} must render current report data in the print preview.`);
    await preview.locator('.tools select').nth(0).selectOption('A3');
    await preview.locator('.tools select').nth(1).selectOption('portrait');
    assert.match(await preview.locator('head style').nth(1).innerText(), /A3 portrait/,
      `${buttonSelector} must apply selected paper size and orientation to print CSS.`);
    const pdfBytes = await preview.pdf({ format: 'A3', printBackground: true });
    assert.ok(pdfBytes.subarray(0, 5).equals(Buffer.from('%PDF-')) && pdfBytes.length > 1_000,
      `${buttonSelector} must render a non-empty PDF document from the print preview.`);
    await preview.locator('.tools button').first().click();
    await preview.waitForFunction(() => document.documentElement.dataset.syntheticPrintInvoked === 'true');
    await preview.close();
  }
  console.log(`PASS: ${reportPrintCases.length} prepared report PDF previews render data, apply A3 portrait settings and invoke the browser print action.`);
  const csvParserResults = await page.evaluate(async () => {
    const read = source => window.EnvanterReportPrint.readCsv(new Response(source), () => true);
    const quoted = await read('\uFEFF"Ad, soyad";"Açıklama; not";"Satır"\r\n"Öztürk, Ayşe";"Cihaz; ""Mavi""";"ilk satır\r\nikinci satır"\r\n\r\n');
    const comma = await read('"Name, team",Description\n"Synthetic asset, laptop","Needs, service"\n');
    const abandoned = await window.EnvanterReportPrint.readCsv(new Response('A;B\n1;2\n'), () => false);
    const failure = async source => {
      try { await read(source); return null; }
      catch (error) { return error.message; }
    };
    const malformedQuote = await failure('A;B\n1;"unfinished');
    const unevenColumns = await failure('A;B\n1;2;3\n');
    const rowLimit = await failure(`A\n${Array.from({ length: 50001 }, () => 'x').join('\n')}\n`);
    return { quoted, comma, abandoned, malformedQuote, unevenColumns, rowLimit };
  });
  assert.deepEqual(csvParserResults.quoted, {
    headers: ['Ad, soyad', 'Açıklama; not', 'Satır'],
    rows: [['Öztürk, Ayşe', 'Cihaz; "Mavi"', 'ilk satır\r\nikinci satır']]
  }, 'CSV reading must preserve BOM-stripped UTF-8 text, delimiters and quotes inside fields, escaped quotes, and embedded newlines.');
  assert.deepEqual(csvParserResults.comma, {
    headers: ['Name, team', 'Description'], rows: [['Synthetic asset, laptop', 'Needs, service']]
  }, 'CSV delimiter detection must ignore commas inside a quoted header.');
  assert.equal(csvParserResults.abandoned, null, 'A closed or stale report preview must stop reading and return no partial report.');
  assert.match(csvParserResults.malformedQuote, /tamamlanmadı/);
  assert.match(csvParserResults.unevenColumns, /sütunları okunamadı/);
  assert.match(csvParserResults.rowLimit, /50\.000 kayıt/);
  console.log('PASS: report CSV parser preserves UTF-8/BOM, escaped quotes, embedded separators and newlines; rejects malformed, uneven and over-50,000-row data; cancels stale reads.');
  console.log('PASS: prepared asset/component reports and CSVs; pending-delivery states/search/paging/CSV/refresh; depreciation date/search/paging/per-currency summaries/filtered CSV/refresh; audit-report filters, 25-row paging, filtered CSV; maintenance aggregates, cost currencies, date filter/clear and refresh; purchase reports and summary export.');

  const customFieldsResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/custom-fields');
  const customPresetsResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/custom-reports' && response.request().method() === 'GET');
  await page.locator('#sidebar button[data-page="Özel rapor oluştur"]').click();
  await Promise.all([customFieldsResponse, customPresetsResponse]);
  assert.equal(await page.locator('#customReportColumns [data-custom-report-field]').count(), 1,
    'Only active non-sensitive company custom fields may be offered as custom report columns.');
  const customReportChecks = page.locator('#customReportColumns input[type="checkbox"]');
  await customReportChecks.evaluateAll(inputs => inputs.forEach(input => { input.checked = false; input.dispatchEvent(new Event('change', { bubbles: true })); }));
  await page.locator('#customReportName').fill('QA Custom Assets');
  const customWritesBeforeInvalidSave = apiRequests.length;
  await page.locator('#customReportSavePreset').click();
  assert.match(await page.locator('#customReportFeedback').innerText(), /En az bir rapor sütunu seçin/);
  assert.equal(apiRequests.length, customWritesBeforeInvalidSave, 'Custom report validation must prevent saving without a selected column.');
  await page.locator('#customReportColumns input[value="tag"]').check();
  await page.locator('#customReportColumns input[value="name"]').check();
  await page.locator('#customReportColumns input[value="custom:inventory_reference"]').check();
  await page.locator('#customReportSearch').fill('QA-CUSTOM-002');
  await page.locator('#customReportStatus').selectOption('Deployed');
  await page.locator('#customReportWarranty').selectOption('active');
  await page.locator('#customReportSort').selectOption('brand:asc');
  const customPresetCreate = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/custom-reports' && response.request().method() === 'POST');
  await page.locator('#customReportSavePreset').click(); await customPresetCreate;
  await page.waitForFunction(() => document.querySelector('#customReportFeedback')?.textContent.includes('diğer cihazlarınızla eşitlenecek'));
  assert.match(await page.locator('#customReportFeedback').innerText(), /diğer cihazlarınızla eşitlenecek/);
  const customCreateRequest = requests.findLast(item => item.path === '/api/assets/custom-reports' && item.method === 'POST');
  assert.equal(customCreateRequest?.csrf, 'synthetic-asset-csrf');
  assert.deepEqual(customCreateRequest?.body.columns, ['tag', 'name', 'custom:inventory_reference']);
  assert.equal(customCreateRequest?.body.q, 'QA-CUSTOM-002');
  assert.equal(customCreateRequest?.body.status, 'Deployed');
  assert.equal(customCreateRequest?.body.warranty, 'active');
  assert.equal(customCreateRequest?.body.sortBy, 'brand');
  assert.equal(customCreateRequest?.body.sortDirection, 'asc');
  const customPresetId = customAssetReportPresets[0].id;
  assert.equal(await page.locator('#customReportPreset').inputValue(), customPresetId);
  await page.locator('#customReportName').fill('Updated QA Custom Assets');
  await page.locator('#customReportSearch').fill('UPDATED-QUERY');
  const customPresetUpdate = page.waitForResponse(response => new URL(response.url()).pathname === `/api/assets/custom-reports/${customPresetId}` && response.request().method() === 'PUT');
  await page.locator('#customReportSavePreset').click(); await customPresetUpdate;
  assert.equal(requests.findLast(item => item.path === `/api/assets/custom-reports/${customPresetId}` && item.method === 'PUT')?.csrf, 'synthetic-asset-csrf');
  await page.locator('#customReportName').fill('Unsaved change');
  await page.locator('#customReportSearch').fill('Unsaved query');
  const customPresetSelect = page.locator('#customReportPreset');
  await customPresetSelect.selectOption(customPresetId);
  await page.locator('#customReportLoadPreset').click();
  assert.equal(await page.locator('#customReportName').inputValue(), 'Updated QA Custom Assets');
  assert.equal(await page.locator('#customReportSearch').inputValue(), 'UPDATED-QUERY');
  assert.equal(await page.locator('#customReportStatus').inputValue(), 'Deployed');
  assert.equal(await page.locator('#customReportSort').inputValue(), 'brand:asc');
  assert.equal(await page.locator('#customReportColumns input[value="custom:inventory_reference"]').isChecked(), true);
  await page.locator('#customReportSearch').fill('QA-CUSTOM-002');
  const customExportWriteCount = apiRequests.length;
  const [customReportDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#customReportExport').click()]);
  assert.match(customReportDownload.suggestedFilename(), /^Updated-QA-Custom-Assets-.*\.csv$/);
  const customCsvStream = await customReportDownload.createReadStream(), customCsvChunks = [];
  for await (const chunk of customCsvStream) customCsvChunks.push(Buffer.from(chunk));
  const customCsv = Buffer.concat(customCsvChunks).toString('utf8');
  assert.match(customCsv, /QA-CUSTOM-002/);
  assert.doesNotMatch(customCsv, /QA-CUSTOM-001/);
  const customExportRequest = requests.findLast(item => item.path === '/api/assets/export-all.csv');
  assert.equal(customExportRequest?.query.q, 'QA-CUSTOM-002');
  assert.equal(customExportRequest?.query.status, 'Deployed');
  assert.equal(customExportRequest?.query.warranty, 'active');
  assert.equal(customExportRequest?.query.sortBy, 'brand');
  assert.deepEqual(customExportRequest?.query.columns.split(','), ['tag', 'name', 'custom:inventory_reference']);
  assert.equal(apiRequests.length, customExportWriteCount, 'Custom asset report export must not write company records.');
  const customPrintPopupPromise = page.waitForEvent('popup');
  await page.locator('#customReportPrint').click();
  const customPrintPreview = await customPrintPopupPromise;
  await customPrintPreview.locator('.status').waitFor({ state: 'visible' });
  await customPrintPreview.waitForFunction(() => document.querySelector('.status')?.textContent.includes('yazdırmaya hazır'));
  assert.equal(await customPrintPreview.locator('thead th').count(), 3,
    'Custom report print must preserve the selected safe columns.');
  assert.equal(await customPrintPreview.locator('tbody tr').count(), 1,
    'Custom report print must preserve the active search filter.');
  assert.match(await customPrintPreview.locator('tbody').innerText(), /QA-CUSTOM-002/);
  const customPdfBytes = await customPrintPreview.pdf({ format: 'A4', printBackground: true });
  assert.ok(customPdfBytes.subarray(0, 5).equals(Buffer.from('%PDF-')) && customPdfBytes.length > 1_000,
    'Custom report preview must render a non-empty PDF document.');
  await customPrintPreview.locator('.tools button').first().click();
  await customPrintPreview.waitForFunction(() => document.documentElement.dataset.syntheticPrintInvoked === 'true');
  await customPrintPreview.close();
  const customPresetDelete = page.waitForResponse(response => new URL(response.url()).pathname === `/api/assets/custom-reports/${customPresetId}` && response.request().method() === 'DELETE');
  await page.locator('#customReportDeletePreset').click(); await customPresetDelete;
  await page.waitForFunction(() => document.querySelector('#customReportFeedback')?.textContent.includes('taslağı hesabınızdan silindi'));
  assert.match(await page.locator('#customReportFeedback').innerText(), /taslağı hesabınızdan silindi/);
  assert.equal(customAssetReportPresets.length, 0, 'Deleting the saved preset must refresh the account-scoped preset list.');
  console.log('PASS: custom asset report safe-column discovery, empty-column validation, CSRF preset create/update/load/delete, filter/sort persistence, filtered CSV content and read-only export.');

  const sidebarDestinations = page.locator('#sidebar button[data-page]');
  const destinationCount = await sidebarDestinations.count();
  assert.equal(destinationCount, 52, 'The owner navigation smoke acceptance must cover all 52 documented sidebar destinations, including the unified purchasing overview.');
  const expectedSurfaceByDestination = {
    'Genel Bakış': 'overview', 'Varlıklar': 'overview',
    'Tüm satın almalar': 'purchaseOverviewPage',
    'Varlık siparişleri': 'assetOrdersPage', 'Varlık modelleri': 'catalogPage', 'Markalar': 'catalogPage', 'Kategoriler': 'catalogPage',
    'Durum etiketleri': 'assetStatusLabelsPage', 'QR ve barkodlar': 'labelSettingsPage',
    'Zimmetler': 'assignmentsPage', 'İadeler': 'returnsPage', 'Transferler': 'activityPage', 'Rezervasyonlar': 'reservationsPage', 'Hareket geçmişi': 'activityPage',
    'Yazılım lisansları': 'licensesPage', 'Lisans atamaları': 'licenseAssignmentsPage', 'Lisanslarım': 'myLicensesPage', 'Sözleşmeler': 'contractsPage', 'Garanti takibi': 'reportPage',
    'Aksesuarlar': 'accessoriesPage', 'Siparişler': 'accessoryOrdersPage', 'Sarf malzemeleri': 'consumablesPage', 'Stok talepleri': 'inventoryRequestsPage',
    'Bileşenler': 'componentsPage', 'Hazır kitler': 'accessoryKitsPage', 'Stok hareketleri': 'inventoryMovementsPage',
    'Çalışan kayıtları': 'employeesPage', 'Uygulama kullanıcıları': 'membersPage', 'Departmanlar': 'directoryPage', 'Yetki rolleri': 'rolesPage',
    'Lokasyonlar': 'directoryPage', 'Binalar ve odalar': 'directoryPage', 'Şirket birimleri': 'companyUnitsPage',
    'Bakım kayıtları': 'maintenancePage', 'Bakım planları': 'calendarPage', 'Denetimler': 'auditPage', 'Takvim': 'calendarPage', 'Görevler': 'tasksPage',
    'Hazır raporlar': 'reportPage', 'Özel rapor oluştur': 'customReportPage', 'Dışa aktarımlar': 'dataToolsPage',
    'Tablodan içe aktar': 'dataToolsPage', 'Verileri dışa aktar': 'dataToolsPage', 'Yedekleme geçmişi': 'dataToolsPage', 'Geri yükle': 'dataToolsPage',
    'Firma profili': 'companySettingsPage', 'Logo ve tema': 'companySettingsPage', 'Bildirimler': 'notificationPreferencesPage', 'Entegrasyonlar': 'integrationsPage',
    'Güvenlik': 'featureStatusPage', 'Denetim kayıtları': 'auditPage', 'Destek': 'supportPage'
  };
  assert.equal(Object.keys(expectedSurfaceByDestination).length, destinationCount, 'Every documented sidebar destination must have an exact expected screen surface.');
  const visitedDestinations = new Set();
  const placeholderDestinations = [];
  for (const destination of await sidebarDestinations.all()) {
    const name = await destination.getAttribute('data-page');
    assert.ok(name && !visitedDestinations.has(name), `Sidebar destination must be unique and named: ${name}`);
    visitedDestinations.add(name);
    if (name === 'Lokasyonlar') {
      companyUnitItems = [
        { id: companyUnitRootId, name: 'QA Legal Entity', path: 'QA Legal Entity', unitType: 'legal_entity', isActive: true },
        { id: companyUnitBranchId, name: 'QA Branch Office', path: 'QA Legal Entity / QA Branch Office', unitType: 'branch', parentCompanyUnitId: companyUnitRootId, isActive: true }
      ];
      locationDirectoryRows = [{ name: legacyLocationPath, locationId: null, parentLocationId: null, depth: 0,
        isCatalogEntry: false, isActive: true, isDefault: false, managerEmployeeId: null, managerName: null,
        assetCount: 1, assignedCount: 0, availableCount: 1, maintenanceCount: 0, accessoryCount: 0,
        accessoryAssignmentCount: 0, consumableCount: 0, componentCount: 0 }];
    }
    await destination.click();
    await page.waitForFunction(expected => {
      const item = [...document.querySelectorAll('#sidebar .nav-item, #sidebar .sub-item')].find(element => element.dataset.page === expected);
      return item?.classList.contains('active') || item?.classList.contains('selected');
    }, name);
    const expectedSurface = expectedSurfaceByDestination[name];
    assert.ok(expectedSurface, `Sidebar destination ${name} must have an explicit expected workspace mapping.`);
    const routeState = await page.evaluate(() => ({
      title: document.querySelector('#pageTitle')?.textContent?.trim() || '',
      visibleSurfaces: [...new Set([...document.querySelectorAll('#overview, .workspace-page')]
        .filter(element => !element.hidden).map(element => element.id))],
      placeholderVisible: ['moduleStatusPage', 'featureStatusPage'].some(id => document.querySelector(`#${id}`)?.hidden === false)
    }));
    assert.ok(routeState.title, `Selecting ${name} must render a page title.`);
    assert.deepEqual(routeState.visibleSurfaces, [expectedSurface], `Selecting ${name} must show only its expected ${expectedSurface} surface.`);
    if (routeState.placeholderVisible) placeholderDestinations.push(name);
    if (name === 'Lokasyonlar') {
      const promotion = page.getByRole('button', { name: `${legacyLocationPath} konumunu kataloğa al` });
      await promotion.click();
      await page.getByText(`“${legacyLocationPath}” kataloğa eklendi; mevcut kayıtlar korundu.`).waitFor();
      assert.equal(locationPromotionWrites.length, 1, 'Promoting a nested legacy location must perform one catalog write.');
      assert.equal(locationPromotionWrites[0].path, '/api/locations/catalog/from-path');
      assert.equal(locationPromotionWrites[0].csrf, 'synthetic-asset-csrf');
      assert.deepEqual(locationPromotionWrites[0].body, { path: legacyLocationPath });
      const catalogRow = page.locator('#directoryRows tr').filter({ hasText: legacyLocationPath });
      await catalogRow.getByRole('button', { name: 'Varsayılan yap' }).waitFor({ state: 'visible' });
      assert.equal(await promotion.isVisible(), false, 'The stale free-text promotion action must disappear after refresh.');

      const locationLevels = ['QA Campus', 'Floor 2', 'Zone A', 'Room 204', 'Cabinet Left', 'Rack 3'];
      const createdLocationIds = [];
      const createdLocationPaths = [];
      for (const [index, name] of locationLevels.entries()) {
        await page.locator('#addLocationButton').click();
        await page.locator('#createLocationDialog').waitFor({ state: 'visible' });
        const form = page.locator('#createLocationForm');
        const parentSelect = form.locator('[name="parentLocationId"]');
        const parentId = index ? createdLocationIds[index - 1] : '';
        await parentSelect.selectOption(parentId);
        await form.locator('[name="name"]').fill(name);
        const path = [...locationLevels.slice(0, index + 1)].join(' / ');
        assert.equal(await page.locator('#createLocationPreview').innerText(), `Konum yolu: ${path}`,
          `The location preview must show all ancestors at level ${index + 1}.`);
        if (index === 0) {
          await page.locator('#createLocationForm [name="companyUnitId"]').selectOption(companyUnitRootId);
        }
        const createResponsePromise = page.waitForResponse(response => response.request().method() === 'POST'
          && new URL(response.url()).pathname === '/api/locations/catalog');
        await page.locator('#saveLocation').click();
        const createResponse = await createResponsePromise;
        assert.equal(createResponse.status(), 201, `Location level ${index + 1} must be created.`);
        const createWrite = locationCatalogWrites.at(-1);
        assert.equal(createWrite?.csrf, 'synthetic-asset-csrf');
        assert.deepEqual(createWrite?.body, { name, parentLocationId: parentId || null,
          companyUnitId: index === 0 ? companyUnitRootId : null });
        const created = await createResponse.json();
        createdLocationIds.push(created.id);
        createdLocationPaths.push(created.displayPath);
        assert.equal(created.displayPath, path);
        await page.locator('#createLocationDialog').waitFor({ state: 'hidden' });
        await page.locator('#directoryRows tr').filter({ hasText: path }).waitFor();
      }
      const firstLocationRow = page.locator('#directoryRows tr').filter({
        has: page.getByRole('button', { name: locationLevels[0], exact: true })
      });
      assert.equal((await firstLocationRow.locator('td').nth(9).innerText()).trim(), 'QA Legal Entity',
        'A location linked at creation must show its company unit in the directory column.');
      await firstLocationRow.getByRole('button', { name: 'Adı düzenle' }).click();
      await page.locator('#directoryRenameDialog').waitFor({ state: 'visible' });
      await page.locator('#directoryRenameForm [name="companyUnitId"]').selectOption(companyUnitBranchId);
      const reassignLocation = page.waitForResponse(response => response.request().method() === 'PATCH'
        && new URL(response.url()).pathname === `/api/locations/catalog/${createdLocationIds[0]}`);
      await page.locator('#saveDirectoryRename').click();
      assert.equal((await reassignLocation).status(), 200);
      assert.deepEqual(locationCatalogWrites.at(-1)?.body, { name: locationLevels[0], companyUnitId: companyUnitBranchId, clearCompanyUnit: false });
      const reassignedLocationRow = page.locator('#directoryRows tr').filter({
        has: page.getByRole('button', { name: locationLevels[0], exact: true })
      });
      await reassignedLocationRow.waitFor({ state: 'visible' });
      assert.equal(locationDirectoryRows.find(item => item.locationId === createdLocationIds[0])?.companyUnitName, 'QA Branch Office',
        'Editing a location must persist and refresh its company-unit assignment.');
      assert.equal((await reassignedLocationRow.locator('td').nth(9).innerText()).trim(), 'QA Branch Office',
        'A reassigned location must render the new company unit after refresh.');
      console.log('PASS: location create/edit forms bind catalog locations to company units and send the selected unit with CSRF.');
      await page.locator('#addLocationButton').click();
      const finalParentOptions = await page.locator('#createLocationForm [name="parentLocationId"] option').evaluateAll(options => options.map(option => option.value));
      assert.equal(finalParentOptions.includes(createdLocationIds[5]), false,
        'A sixth-level location cannot be offered as a parent because that would create an unsupported seventh level.');
      assert.deepEqual(createdLocationPaths, locationLevels.reduce((paths, name, index) => {
        paths.push(index ? `${paths[index - 1]} / ${name}` : name); return paths;
      }, []));
      await page.locator('#cancelCreateLocation').click();
      await page.locator('#createLocationDialog').waitFor({ state: 'hidden' });
      console.log('PASS: location catalog creates six nested levels with complete path previews, CSRF and refreshed rows; the sixth level is excluded as a parent so UI cannot offer level seven.');

      const deepestRow = page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] });
      const setDeepDefault = page.waitForResponse(response => response.request().method() === 'POST'
        && new URL(response.url()).pathname === `/api/locations/catalog/${createdLocationIds[5]}/default`);
      await deepestRow.getByRole('button', { name: 'Varsayılan yap' }).click();
      assert.equal((await setDeepDefault).status(), 200);
      assert.deepEqual(locationDefaultWrites.at(-1)?.body, { isDefault: true });
      assert.equal(locationDefaultWrites.at(-1)?.csrf, 'synthetic-asset-csrf');
      await page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] }).getByRole('button', { name: 'Varsayılanı kaldır' }).waitFor();

      const legacyRow = page.locator('#directoryRows tr').filter({ hasText: legacyLocationPath });
      const legacyLeafId = locationDirectoryRows.find(item => item.name === legacyLocationPath)?.locationId;
      assert.ok(legacyLeafId, 'The promoted legacy location leaf must retain its catalog identifier.');
      const switchDefault = page.waitForResponse(response => response.request().method() === 'POST'
        && new URL(response.url()).pathname === `/api/locations/catalog/${legacyLeafId}/default`);
      await legacyRow.getByRole('button', { name: 'Varsayılan yap' }).click();
      assert.equal((await switchDefault).status(), 200);
      assert.equal(locationDirectoryRows.filter(item => item.isDefault).length, 1, 'Setting a new default location must clear the former company default.');
      await page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] }).getByRole('button', { name: 'Varsayılan yap' }).waitFor();

      const managerRow = page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] });
      await managerRow.getByRole('button', { name: 'Sorumlu ata' }).click();
      await page.waitForFunction(id => [...document.querySelectorAll('#locationManagerEmployee option')].some(option => option.value === id), employeeId);
      await page.locator('#locationManagerEmployee').selectOption(employeeId);
      const assignManager = page.waitForResponse(response => response.request().method() === 'POST'
        && new URL(response.url()).pathname === `/api/locations/catalog/${createdLocationIds[5]}/manager`);
      await page.locator('#saveLocationManager').click();
      assert.equal((await assignManager).status(), 200);
      assert.equal(locationManagerWrites.at(-1)?.csrf, 'synthetic-asset-csrf');
      assert.deepEqual(locationManagerWrites.at(-1)?.body, { employeeId });
      await page.locator('#locationManagerDialog').waitFor({ state: 'hidden' });
      await page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] }).getByRole('button', { name: 'Synthetic QA Employee' }).waitFor();

      await page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] }).getByRole('button', { name: 'Synthetic QA Employee' }).click();
      await page.locator('#locationManagerEmployee').selectOption('');
      const clearManager = page.waitForResponse(response => response.request().method() === 'POST'
        && new URL(response.url()).pathname === `/api/locations/catalog/${createdLocationIds[5]}/manager`);
      await page.locator('#saveLocationManager').click();
      assert.equal((await clearManager).status(), 200);
      assert.deepEqual(locationManagerWrites.at(-1)?.body, { employeeId: null });
      assert.equal(locationManagerWrites.at(-1)?.csrf, 'synthetic-asset-csrf');
      await page.locator('#locationManagerDialog').waitFor({ state: 'hidden' });
      await page.locator('#directoryRows tr').filter({ hasText: createdLocationPaths[5] }).getByRole('button', { name: 'Sorumlu ata' }).waitFor();
      console.log('PASS: location default can be set/switched with a single company default; active employee can be assigned/removed as location manager with CSRF and refreshed rows.');
    }
    if (name === 'Durum etiketleri') {
      await page.waitForFunction(() => document.querySelectorAll('#assetStatusLabelRows input[data-status]').length === 6
        && document.querySelector('#assetStatusTransitionMatrix table'));
      assert.equal(await page.locator('#assetStatusTransitionMatrix th[title="Archived"]').count(), 0,
        'Archive and restore must remain separate protected workflows, outside manual status transitions.');
      const availableLabel = page.locator('#assetStatusLabelRows input[data-status="Available"]');
      await availableLabel.fill('Dağıtıma hazır');
      const statusLabelSave = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/status-labels'
        && response.request().method() === 'PUT');
      await page.locator('#assetStatusLabelsSave').click();
      const savedLabelsResponse = await statusLabelSave;
      assert.equal(savedLabelsResponse.status(), 200, 'Saving a company-specific fixed status label must persist successfully.');
      await page.getByText('Durum etiketleri kaydedildi.').waitFor();
      assert.equal(statusLabelWrites.length, 1, 'Saving fixed labels must issue exactly one API write.');
      assert.equal(statusLabelWrites[0].csrf, 'synthetic-asset-csrf');
      assert.equal(statusLabelWrites[0].body.labels.find(item => item.status === 'Available')?.label, 'Dağıtıma hazır');
      for (const selector of ['#assetStatusFilter', '#bulkAssetStatus', '#customReportStatus', '#assetStatusAction']) {
        assert.equal(await page.locator(`${selector} option[value="Available"]`).innerText(), 'Dağıtıma hazır',
          `Saving a label must refresh the visible option in ${selector}.`);
      }
      assert.equal(await availableLabel.inputValue(), 'Dağıtıma hazır', 'The saved company label must remain visible in settings.');
      await page.locator('#addAssetCustomStatus').click();
      const customStatusRow = page.locator('#assetCustomStatusRows .status-label-setting-row').filter({ has: page.locator('strong') });
      const customStatusCode = (await customStatusRow.locator('strong').innerText()).trim();
      assert.match(customStatusCode, /^Custom_[a-f0-9]{8}$/);
      await customStatusRow.locator('input[data-custom-status-label]').fill('Kalibrasyonda');
      await customStatusRow.locator('input[type="color"]').evaluate(input => { input.value = '#6B5BD2'; input.dispatchEvent(new Event('input', { bubbles: true })); input.dispatchEvent(new Event('change', { bubbles: true })); });
      const restrictedTransition = page.locator('#assetStatusTransitionMatrix input[data-transition-from="Available"][data-transition-to="Maintenance"]');
      await restrictedTransition.uncheck();
      const statusConfigSave = page.waitForResponse(response => new URL(response.url()).pathname === '/api/assets/status-config'
        && response.request().method() === 'PUT');
      await page.locator('#assetStatusConfigSave').click();
      const savedConfigResponse = await statusConfigSave;
      assert.equal(savedConfigResponse.status(), 200, 'Saving a custom status and transition matrix must persist successfully.');
      await page.getByText('Durum ve geçiş kuralları kaydedildi.').waitFor();
      assert.equal(statusConfigurationWrites.length, 1, 'Saving the custom status configuration must issue exactly one API write.');
      assert.deepEqual(statusConfigurationWrites[0].body.customStatuses, [{ status: customStatusCode, label: 'Kalibrasyonda', color: '#6b5bd2' }]);
      assert.equal(statusConfigurationWrites[0].csrf, 'synthetic-asset-csrf');
      assert.equal(statusConfigurationWrites[0].body.transitions.some(edge => edge.from === 'Available' && edge.to === 'Maintenance'), false,
        'The saved transition matrix must exclude the manually disabled edge.');
      for (const selector of ['#assetStatusFilter', '#bulkAssetStatus', '#customReportStatus', '#assetStatusAction']) {
        assert.equal(await page.locator(`${selector} option[value="${customStatusCode}"]`).count(), 1,
          `The new status must be available in ${selector}.`);
      }
      assert.equal(await page.locator('#assetStatusTransitionMatrix input[data-transition-from="Available"][data-transition-to="Maintenance"]').isChecked(), false,
        'The rendered matrix must reflect the saved disallowed transition.');
      console.log('PASS: fixed status label edit, CSRF persistence, all refreshed status selectors, custom status create/color, transition matrix, protected archive boundary, and refreshed inventory/bulk/report/detail selectors.');
    }
    if (name === 'Bildirimler') {
      const preferencesForm = page.locator('#companyNotificationPreferencesForm');
      assert.equal(routeState.placeholderVisible, false, 'The notifications menu must render its own working preferences screen.');
      await page.getByText('Bildirim tercihleri yüklendi.').waitFor();
      const maintenancePreference = preferencesForm.locator('input[name="kind"][value="maintenance"]');
      const warrantyPreference = preferencesForm.locator('input[name="kind"][value="warranty"]');
      const warrantyEmailPreference = preferencesForm.locator('input[name="emailKind"][value="warranty"]');
      const returnEmailPreference = preferencesForm.locator('input[name="emailKind"][value="return"]');
      assert.equal(await maintenancePreference.isChecked(), true, 'Saved notification types must load into the dedicated page.');
      await warrantyPreference.check(); await warrantyEmailPreference.check(); await returnEmailPreference.check();
      await page.locator('#companyNotificationPreferencesReset').click();
      assert.equal(await warrantyPreference.isChecked(), false, 'Reset must discard unsaved preference changes.');
      assert.equal(await returnEmailPreference.isChecked(), false, 'Reset must discard an unsaved return reminder email preference.');
      assert.equal(notificationPreferenceWrites.length, 0, 'Reset must not write preferences.');
      await warrantyPreference.check(); await warrantyEmailPreference.check(); await returnEmailPreference.check();
      await page.locator('#companyNotificationPreferencesSave').click();
      await page.getByText('Bildirim tercihleri kaydedildi.').waitFor();
      assert.deepEqual(notificationPreferenceWrites.at(-1)?.body, { enabledKinds: ['maintenance', 'license', 'warranty'], emailEnabledKinds: ['license', 'warranty', 'return'] });
      const persistedNotificationGet = page.waitForRequest(request => request.url().includes('/api/notifications/preferences') && request.method() === 'GET');
      await page.locator('#companyNotificationPreferencesRefresh').click(); await persistedNotificationGet;
      assert.equal(await warrantyPreference.isChecked(), true, 'Refreshing must reload the saved notification preferences.');
      await page.setViewportSize({ width: 390, height: 844 });
      assert.equal(await page.locator('#notificationPreferencesPage').evaluate(element => element.scrollWidth <= element.clientWidth), true,
        'Notification preferences must fit the phone-width workspace without creating an inner horizontal overflow.');
      assert.equal(await page.locator('#companyNotificationPreferencesSave').isVisible(), true, 'The save action must remain available on a phone viewport.');
      await page.setViewportSize({ width: 1280, height: 900 });
    }
    if (name === 'Yetki rolleri') {
      const roleName = 'Synthetic custom asset role';
      const beforeRoleWrites = companyRoleWrites.length;
      await page.locator('#createCompanyRoleButton').click();
      await page.locator('#companyRoleDialog').waitFor({ state: 'visible' });
      assert.equal(await page.locator('#companyRoleForm input[name="permission"][value="company.settings"]').isDisabled(), false,
        'The company owner can choose the elevated company-settings permission.');
      await page.locator('#companyRoleForm [name="name"]').fill(roleName);
      await page.locator('#companyRoleForm input[name="permission"][value="assets.read"]').check();
      await page.locator('#saveCompanyRoleButton').click();
      const roleRow = () => page.locator('#roleAccessRows tr').filter({ hasText: roleName });
      await roleRow().waitFor({ state: 'visible' });
      assert.equal(companyRoleWrites.length, beforeRoleWrites + 1);
      assert.equal(companyRoleWrites.at(-1).method, 'POST');
      assert.equal(companyRoleWrites.at(-1).csrf, 'synthetic-asset-csrf');
      assert.deepEqual(companyRoleWrites.at(-1).body.permissionKeys, ['assets.read']);

      await roleRow().getByRole('button', { name: 'Düzenle' }).click();
      await page.locator('#companyRoleForm [name="name"]').fill(`${roleName} updated`);
      await page.locator('#companyRoleForm input[name="permission"][value="assets.write"]').check();
      await page.locator('#saveCompanyRoleButton').click();
      await page.getByText(`${roleName} updated`, { exact: true }).waitFor({ state: 'visible' });
      assert.equal(companyRoleWrites.at(-1).method, 'PUT');
      assert.equal(companyRoleWrites.at(-1).csrf, 'synthetic-asset-csrf');
      assert.deepEqual(companyRoleWrites.at(-1).body.permissionKeys, ['assets.read', 'assets.write']);

      const updatedRoleRow = () => page.locator('#roleAccessRows tr').filter({ hasText: `${roleName} updated` });
      await updatedRoleRow().getByRole('button', { name: 'Devre dışı bırak' }).click();
      await page.locator('#deactivateCompanyRoleDialog').waitFor({ state: 'visible' });
      await page.locator('#confirmDeactivateCompanyRole').click();
      await updatedRoleRow().getByRole('button', { name: 'Etkinleştir' }).waitFor({ state: 'visible' });
      assert.equal(companyRoleWrites.at(-1).method, 'PATCH');
      assert.equal(companyRoleWrites.at(-1).csrf, 'synthetic-asset-csrf');
      assert.deepEqual(companyRoleWrites.at(-1).body, { isActive: false });
      await updatedRoleRow().getByRole('button', { name: 'Etkinleştir' }).click();
      await updatedRoleRow().getByRole('button', { name: 'Devre dışı bırak' }).waitFor({ state: 'visible' });
      assert.deepEqual(companyRoleWrites.at(-1).body, { isActive: true });
      console.log('PASS: Chromium custom-role create, owner-only permission choice, CSRF-protected edit, deactivation, reactivation and refreshed role matrix.');
    }
    if (name === 'Güvenlik') {
      const beforeSecurityShortcut = apiRequests.length;
      assert.equal(await page.locator('#featureStatusAccountSecurityButton').isVisible(), true,
        'The Security menu must keep the boundary explanation and account-security shortcut visible.');
      assert.equal(await page.locator('#securityOverviewPanel').isVisible(), true, 'Security must show the MFA/session overview on its own page.');
      assert.equal(await page.locator('#securitySessionsPanel').isVisible(), true, 'Security must expose the account session list on its own page.');
      await page.waitForFunction(() => document.querySelector('#securityOverviewMfa')?.textContent === 'Etkin'
        && document.querySelector('#securityOverviewSessions')?.textContent === '2');
      await page.getByText('Synthetic Firefox').waitFor();
      assert.equal(await page.locator('#securitySessionsList button').count(), 2, 'Each listed session must have a revoke control.');
      const desktopViewport = page.viewportSize();
      await page.setViewportSize({ width: 390, height: 844 });
      const mobileSecurityLayout = await page.evaluate(() => {
        const button = document.querySelector('#securitySessionsList article button');
        const bounds = button?.getBoundingClientRect();
        return { viewport: window.innerWidth, documentWidth: document.documentElement.scrollWidth, buttonHeight: bounds?.height ?? 0, buttonWidth: bounds?.width ?? 0 };
      });
      assert.ok(mobileSecurityLayout.documentWidth <= mobileSecurityLayout.viewport,
        `Security page must not overflow horizontally at 390px (${mobileSecurityLayout.documentWidth}px document width).`);
      assert.ok(mobileSecurityLayout.buttonHeight >= 44 && mobileSecurityLayout.buttonWidth > 0,
        'Session revocation controls must remain visible and touch-sized on mobile.');
      await page.setViewportSize(desktopViewport);
      assert.equal(await page.locator('#accountPhotoDialog').isVisible(), false, 'Selecting Security must not open the account dialog automatically.');
      assert.equal(apiRequests.length, beforeSecurityShortcut, 'Loading and refreshing the security overview must not perform writes.');
      await page.locator('#securityOverviewRefresh').click();
      await page.waitForFunction(() => document.querySelector('#securityOverviewMfa')?.textContent === 'Etkin'
        && document.querySelector('#securityOverviewSessions')?.textContent === '2');
      assert.equal(apiRequests.length, beforeSecurityShortcut, 'Refreshing the security overview must remain read-only.');
      await page.locator('#securitySessionsList article').filter({ hasText: 'Synthetic Firefox' }).getByRole('button', { name: 'Oturumu kapat' }).click();
      await page.locator('#componentActionDialog').waitFor({ state: 'visible' });
      await page.locator('#componentActionSubmit').click();
      await page.waitForFunction(() => document.querySelector('#securityOverviewSessions')?.textContent === '1'
        && document.querySelector('#securitySessionsList')?.textContent.includes('Synthetic Chromium'));
      assert.equal(sessionRevocationWrites.length, 1, 'Revoking a remote session from Security must make one server write.');
      assert.equal(sessionRevocationWrites[0].path, '/api/session/devices/55555555-5555-4555-8555-555555555555/revoke');
      assert.equal(sessionRevocationWrites[0].csrf, 'synthetic-asset-csrf');
      const beforeOpenAccountSecurity = apiRequests.length;
      await page.locator('#featureStatusAccountSecurityButton').click();
      await page.locator('#accountPhotoDialog').waitFor({ state: 'visible' });
      const personalExport = page.locator('#downloadAccountData');
      assert.equal(await personalExport.getAttribute('href'), '/api/session/data-export');
      const [personalDataDownload] = await Promise.all([
        page.waitForEvent('download'), personalExport.click()
      ]);
      assert.match(personalDataDownload.suggestedFilename(), /^envantertr-account-data-\d{8}\.json$/);
      assert.equal(requests.findLast(item => item.path === '/api/session/data-export')?.method, 'GET',
        'Personal data export must be a read-only authenticated download.');
      assert.equal(apiRequests.length, beforeOpenAccountSecurity, 'Opening account security must be read-only until a separate preference change is submitted.');
      await page.locator('#closeAccountPhoto').click();
      await page.locator('#accountPhotoDialog').waitFor({ state: 'hidden' });
    }
    if (name === 'Varlık siparişleri') {
      assert.equal(routeState.placeholderVisible, false, 'Asset purchasing must render its working order list.');
      if (!locationDirectoryRows.some(item => item.locationId && item.isCatalogEntry && item.isActive)) {
        await page.locator('#newAssetOrderButton').click();
        await page.getByText('Sipariş oluşturmadan önce şirket konum kataloğuna etkin bir konum ekleyin.').waitFor({ state: 'visible' });
        assert.equal(await page.locator('#assetOrderCreateDialog').isVisible(), false,
          'Asset order creation must remain closed until an active target location exists.');
        locationDirectoryRows = [{ name: 'Synthetic Asset Order Depot', locationId: 'f0000000-0000-4000-8000-000000000001',
          parentLocationId: null, depth: 0, isCatalogEntry: true, isActive: true, isDefault: true, managerEmployeeId: null, managerName: null,
          assetCount: 0, assignedCount: 0, availableCount: 0, maintenanceCount: 0, accessoryCount: 0,
          accessoryAssignmentCount: 0, consumableCount: 0, componentCount: 0 }];
      }
      const assetOrderLocationId = locationDirectoryRows.find(item => item.locationId && item.isCatalogEntry && item.isActive).locationId;
      await page.locator('#assetOrderBudgetButton').click();
      const budgetDialog = page.locator('#assetOrderBudgetDialog'); await budgetDialog.waitFor({ state: 'visible' });
      assert.match(await page.locator('#assetOrderBudgetRows').innerText(), /Bütçe.*Taahhüt.*Kalan/s,
        'The company purchasing budget list must expose budget, committed and remaining values.');
      const budgetForm = page.locator('#assetOrderBudgetForm');
      await budgetForm.locator('[name="amount"]').fill('1500');
      const budgetResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/asset-purchase-budgets/${new Date().getFullYear()}/TRY`
        && response.request().method() === 'PUT');
      await budgetForm.locator('button[type="submit"]').click();
      assert.equal((await budgetResponse).status(), 200);
      await page.getByText(/Kaydedildi · taahhüt/).waitFor({ state: 'visible' });
      assert.equal(assetPurchaseBudgetWrites.length, 1, 'Saving a budget must make one explicit CSRF-protected request.');
      assert.equal(assetPurchaseBudgetWrites[0].csrf, 'synthetic-asset-csrf');
      await page.locator('[data-close-asset-order-budget]').last().click(); await budgetDialog.waitFor({ state: 'hidden' });

      const budgetApprovalRow = page.locator('#assetOrderRows tr').filter({ hasText: 'QA-UI-BUDGET-001' });
      await budgetApprovalRow.getByText('Bütçe onayı bekliyor', { exact: true }).waitFor({ state: 'visible' });
      assert.equal(await budgetApprovalRow.locator('.asset-order-receive').count(), 0,
        'A pending budget approval must not expose the list-level receipt shortcut.');
      await budgetApprovalRow.locator('.asset-order-open').first().click();
      const approvalDialog = page.locator('#assetOrderDetailDialog'); await approvalDialog.waitFor({ state: 'visible' });
      await page.locator('#assetOrderApprovalPanel').waitFor({ state: 'visible' });
      assert.match(await page.locator('#assetOrderBudgetSummary').innerText(), /bütçe aşımı/i);
      assert.equal(await page.locator('#receiveAssetOrderFromDetail').isVisible(), false,
        'An order that is waiting for approval must not show the receipt action.');
      await page.locator('#assetOrderApprovalNote').fill('Bütçe istisnası kontrol edildi ve uygun bulundu.');
      const approvalResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/asset-orders/${budgetApprovalOrderId}/approval`
        && response.request().method() === 'POST');
      await page.locator('#approveAssetOrderApproval').click();
      assert.equal((await approvalResponse).status(), 200);
      await page.locator('#assetOrderApprovalActions').waitFor({ state: 'hidden' });
      assert.equal(assetPurchaseApprovalWrites.length, 1, 'Approving a budget exception must write once.');
      assert.equal(assetPurchaseApprovalWrites[0].csrf, 'synthetic-asset-csrf');
      assert.equal(assetPurchaseApprovalWrites[0].body.decision, 'Approved');
      assert.ok(assetPurchaseApprovalWrites[0].body.note.length >= 10);
      await approvalDialog.locator('[data-close-asset-order-detail]').last().click(); await approvalDialog.waitFor({ state: 'hidden' });

      const modalVisible = page.locator('#assetOrderCreateDialog').waitFor({ state: 'visible' });
      await Promise.all([modalVisible, page.locator('#newAssetOrderButton').click()]);
      const orderForm = page.locator('#assetOrderCreateForm');
      await orderForm.locator('[name="orderNumber"]').fill('QA-UI-MULTI-001');
      await orderForm.locator('[name="supplier"]').fill('Synthetic Asset Supplier');
      await orderForm.locator('[name="orderDate"]').fill('2026-10-16');
      await orderForm.locator('[name="expectedOn"]').fill('2026-10-31');
      await orderForm.locator('[name="taxRatePercent"]').fill('20');
      await orderForm.locator('[name="discountAmount"]').fill('1500');
      await orderForm.locator('[name="freightAmount"]').fill('600');
      const lines = page.locator('#assetOrderCreateLines .order-line-row');
      await lines.nth(0).locator('[data-asset-order-name]').fill('QA UI Laptop');
      await lines.nth(0).locator('[data-asset-order-category]').selectOption(asset.categoryId);
      await lines.nth(0).locator('[data-asset-order-location]').selectOption(assetOrderLocationId);
      await lines.nth(0).locator('[data-asset-order-quantity]').fill('3');
      await lines.nth(0).locator('[data-asset-order-cost]').fill('12500');
      await page.locator('#addAssetOrderLine').click();
      assert.equal(await lines.count(), 2, 'The add-line control must create a second editable line.');
      await lines.nth(1).locator('[data-asset-order-name]').fill('QA UI Monitor');
      await lines.nth(1).locator('[data-asset-order-category]').selectOption(asset.categoryId);
      await lines.nth(1).locator('[data-asset-order-location]').selectOption(assetOrderLocationId);
      await lines.nth(1).locator('[data-asset-order-quantity]').fill('2');
      await lines.nth(1).locator('[data-asset-order-cost]').fill('2500');
      assert.match(await page.locator('#assetOrderCreateTotal').innerText(), /49\.800,00/, 'The multi-line preview must include tax, discount and freight.');
      const createOrder = page.waitForResponse(response => new URL(response.url()).pathname === '/api/asset-orders'
        && response.request().method() === 'POST');
      await orderForm.locator('button[type="submit"]').click();
      const createdOrderResponse = await createOrder;
      assert.equal(createdOrderResponse.status(), 201, 'Creating the multi-line order from the UI must succeed.');
      await page.locator('#assetOrderCreateDialog').waitFor({ state: 'hidden' });
      await page.getByText('QA-UI-MULTI-001', { exact: true }).waitFor({ state: 'visible' });
      assert.equal(assetPurchaseOrderWrites.length, 1, 'The complete UI form must issue one order write.');
      assert.equal(assetPurchaseOrderRows[0].quantity, 5, 'The refreshed order list must show the sum of both line quantities.');
      const createdOrderRow = page.locator('#assetOrderRows tr').filter({ hasText: 'QA-UI-MULTI-001' });
      await createdOrderRow.locator('.asset-order-cancel').click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
      assert.match(await page.locator('#employeeActionSummary').innerText(), /yeniden teslim alınamaz/i,
        'The confirmation must explain the irreversible receipt consequence.');
      await page.locator('[data-close-employee-action]').last().click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
      assert.equal(assetPurchaseOrderCancelWrites.length, 0, 'Dismissing cancellation confirmation must not write.');
      await createdOrderRow.locator('.asset-order-cancel').click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
      const cancelResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/asset-orders/ab100000-0000-4000-8000-000000000001/cancel'
        && response.request().method() === 'POST');
      await page.locator('#employeeActionSubmit').click();
      assert.equal((await cancelResponse).status(), 200, 'Confirming cancellation must call the cancellation API.');
      await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
      await page.locator('#assetOrderRows tr').filter({ hasText: 'QA-UI-MULTI-001' }).getByText('İptal edildi', { exact: true }).waitFor({ state: 'visible' });
      assert.equal(assetPurchaseOrderCancelWrites.length, 1);
      assert.equal(assetPurchaseOrderCancelWrites[0].csrf, 'synthetic-asset-csrf');
      assert.equal(assetPurchaseOrderRows[0].status, 'Cancelled');

      const receiptCreateDialog = page.locator('#assetOrderCreateDialog');
      const receiptFormVisible = receiptCreateDialog.waitFor({ state: 'visible' });
      await Promise.all([receiptFormVisible, page.locator('#newAssetOrderButton').click()]);
      const receiptCreateForm = page.locator('#assetOrderCreateForm');
      await receiptCreateForm.locator('[name="orderNumber"]').fill('QA-UI-RECEIVE-001');
      await receiptCreateForm.locator('[name="supplier"]').fill('Synthetic Asset Supplier');
      await receiptCreateForm.locator('[name="orderDate"]').fill('2026-10-16');
      await receiptCreateForm.locator('[data-asset-order-name]').fill('QA UI Receipt Laptop');
      await receiptCreateForm.locator('[data-asset-order-category]').selectOption(asset.categoryId);
      await receiptCreateForm.locator('[data-asset-order-location]').selectOption(assetOrderLocationId);
      await receiptCreateForm.locator('[data-asset-order-quantity]').fill('3');
      await receiptCreateForm.locator('[data-asset-order-cost]').fill('100');
      const receiptCreateResponsePromise = page.waitForResponse(response => new URL(response.url()).pathname === '/api/asset-orders'
        && response.request().method() === 'POST' && response.request().postData()?.includes('QA-UI-RECEIVE-001'));
      await receiptCreateForm.locator('button[type="submit"]').click();
      assert.equal((await receiptCreateResponsePromise).status(), 201);
      await receiptCreateDialog.waitFor({ state: 'hidden' });
      const receiptOrderRow = page.locator('#assetOrderRows tr').filter({ hasText: 'QA-UI-RECEIVE-001' });
      await receiptOrderRow.waitFor({ state: 'visible' });
      await receiptOrderRow.locator('.asset-order-receive').click();
      const receiptDialog = page.locator('#assetOrderReceiveDialog');
      await receiptDialog.waitFor({ state: 'visible' });
      const receiptQuantity = page.locator('#assetOrderReceiveLines [data-asset-receive-line]');
      assert.equal(await receiptQuantity.count(), 1);
      await receiptQuantity.fill('1');
      await page.locator('#assetOrderReceiveDialog').getByRole('button', { name: 'Vazgeç' }).click();
      await receiptDialog.waitFor({ state: 'hidden' });
      assert.equal(assetPurchaseOrderReceiptWrites.length, 0, 'Dismissing receipt must not create asset records.');
      assert.equal(assetPurchaseOrderRows.find(item => item.orderNumber === 'QA-UI-RECEIVE-001').quantityReceived, 0);
      await receiptOrderRow.locator('.asset-order-receive').click();
      await receiptDialog.waitFor({ state: 'visible' });
      await page.locator('#assetOrderReceiveLines [data-asset-receive-line]').fill('1');
      await page.locator('#assetOrderReceiveForm [name="notes"]').fill('Sentetik kısmi teslim kabulü');
      const receiptResponsePromise = page.waitForResponse(response => new URL(response.url()).pathname === '/api/asset-orders/ab100000-0000-4000-8000-000000000002/receive'
        && response.request().method() === 'POST');
      await page.locator('#assetOrderReceiveForm button[type="submit"]').click();
      const savedReceiptResponse = await receiptResponsePromise;
      assert.equal(savedReceiptResponse.status(), 200);
      await receiptDialog.waitFor({ state: 'hidden' });
      const receiptOrderUpdated = page.locator('#assetOrderRows tr').filter({ hasText: 'QA-UI-RECEIVE-001' });
      await receiptOrderUpdated.getByText('Kısmi teslim alındı', { exact: true }).waitFor({ state: 'visible' });
      assert.match(await receiptOrderUpdated.innerText(), /1 \/ 3 adet/);
      assert.equal(assetPurchaseOrderReceiptWrites.length, 1);
      assert.equal(assetPurchaseOrderReceiptWrites[0].csrf, 'synthetic-asset-csrf');
      assert.deepEqual(assetPurchaseOrderReceiptWrites[0].body.items, [
        { lineId: 'ab200000-0000-4000-8000-000000000001', quantity: 1 }
      ]);
      const receivedOrder = assetPurchaseOrderRows.find(item => item.orderNumber === 'QA-UI-RECEIVE-001');
      assert.equal(receivedOrder.assets.length, 1, 'A confirmed received unit must create one linked asset record.');
      assert.equal(receivedOrder.assets[0].purchaseOrderLineId, receivedOrder.lines[0].id);
      await receiptOrderUpdated.locator('.asset-order-open').first().click();
      const orderDetailDialog = page.locator('#assetOrderDetailDialog');
      await orderDetailDialog.waitFor({ state: 'visible' });
      const selectedOrderFiles = [
        { name: 'qa-invoice.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.7\nsynthetic invoice') },
        { name: 'qa-photo.png', mimeType: 'image/png', buffer: Buffer.from([137, 80, 78, 71, 13, 10, 26, 10, 0]) },
        { name: 'qa-photo.jpg', mimeType: 'image/jpeg', buffer: Buffer.from([0xff, 0xd8, 0xff, 0xe0, 0, 1]) },
        { name: 'qa-photo.webp', mimeType: 'image/webp', buffer: Buffer.from('RIFF0000WEBP') }
      ];
      const attachmentInput = page.locator('#assetOrderAttachmentFiles');
      const attachmentWriteCountBeforeSelection = assetPurchaseOrderAttachmentWrites.length;
      await attachmentInput.setInputFiles(selectedOrderFiles);
      assert.equal(assetPurchaseOrderAttachmentWrites.length, attachmentWriteCountBeforeSelection,
        'Selecting purchase-order documents must not upload before the explicit button action.');
      await page.locator('#uploadAssetOrderAttachments').click();
      for (const file of selectedOrderFiles) await page.locator('#assetOrderAttachments').getByText(file.name, { exact: true }).waitFor({ state: 'visible' });
      assert.equal(assetPurchaseOrderAttachmentWrites.length, 4, 'All four selected supported document formats must upload.');
      assert.deepEqual(assetPurchaseOrderAttachmentWrites.map(item => item.body.file.contentType),
        ['application/pdf', 'image/png', 'image/jpeg', 'image/webp']);
      assert.ok(assetPurchaseOrderAttachmentWrites.every(item => item.csrf === 'synthetic-asset-csrf'));
      const firstAttachmentRemove = page.locator('#assetOrderAttachments .asset-order-attachment-remove').first();
      await firstAttachmentRemove.click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
      await page.locator('[data-close-employee-action]').last().click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
      assert.equal(assetPurchaseOrderAttachmentDeleteWrites.length, 0, 'Dismissing attachment deletion must preserve the document.');
      await page.locator('#assetOrderAttachments .asset-order-attachment-remove').first().click();
      await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
      const attachmentDeleteResponsePromise = page.waitForResponse(response => new URL(response.url()).pathname.startsWith('/api/asset-orders/ab100000-0000-4000-8000-000000000002/attachments/')
        && response.request().method() === 'DELETE');
      await page.locator('#employeeActionSubmit').click();
      assert.equal((await attachmentDeleteResponsePromise).status(), 204);
      await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
      await page.locator('#assetOrderAttachments').getByText('qa-invoice.pdf', { exact: true }).waitFor({ state: 'hidden' });
      assert.equal(assetPurchaseOrderAttachmentDeleteWrites.length, 1);
      assert.equal(assetPurchaseOrderAttachmentDeleteWrites[0].csrf, 'synthetic-asset-csrf');
      assert.equal(receivedOrder.attachments.length, 3, 'Confirmed deletion must refresh the order attachment list.');
      const receivedAssetLink = orderDetailDialog.locator('.openAssetFromPurchaseOrder').filter({ hasText: 'QA-UI-RECEIVED-001' });
      await receivedAssetLink.waitFor({ state: 'visible' });
      await receivedAssetLink.click();
      await orderDetailDialog.waitFor({ state: 'hidden' });
      const linkedAssetDialog = page.locator('#assetDialog');
      await linkedAssetDialog.waitFor({ state: 'visible' });
      await page.locator('#detailPurchaseOrderField').waitFor({ state: 'visible' });
      assert.match(await page.locator('#detailPurchaseOrder').innerText(), /QA-UI-RECEIVE-001 · Synthetic Asset Supplier/);
      const assetPurchaseOrderResponsePromise = page.waitForResponse(response => new URL(response.url()).pathname === `/api/asset-orders/${receivedOrder.id}`
        && response.request().method() === 'GET');
      await page.locator('#detailPurchaseOrder').click();
      const linkedOrderResponse = await assetPurchaseOrderResponsePromise;
      assert.equal(linkedOrderResponse.status(), 200);
      await linkedAssetDialog.waitFor({ state: 'hidden' });
      await orderDetailDialog.waitFor({ state: 'visible' });
      assert.match(await page.locator('#assetOrderDetailTitle').innerText(), /QA-UI-RECEIVE-001/);
      await orderDetailDialog.locator('[data-close-asset-order-detail]').first().click();
      await orderDetailDialog.waitFor({ state: 'hidden' });
      console.log('PASS: hosted Chromium creates a priced order, safe cancel/partial receipt flows, and four explicit CSRF-protected purchase document uploads with cancel-safe deletion and refreshed list.');
    }
    if (name === 'Siparişler') {
      const accessoryRow = page.locator('#accessoryOrderRows tr').filter({ hasText: 'QA-UI-SHARED-ACC-001' });
      await accessoryRow.waitFor({ state: 'visible' });
      assert.equal(await accessoryRow.locator('.accessory-order-receive').count(), 1, 'An accessory order below the shared budget remains receivable.');
      await accessoryRow.locator('.accessory-order-open').first().click();
      const accessoryDialog = page.locator('#accessoryOrderDetailDialog'); await accessoryDialog.waitFor({ state: 'visible' });
      assert.match(await page.locator('#accessoryOrderBudgetSummary').innerText(), /Ortak yıllık satın alma bütçesi.*100.*80/s,
        'Accessory detail must display the shared budget and its projected commitment.');
      assert.equal(await page.locator('#accessoryOrderApprovalActions').isVisible(), false,
        'An accessory order under budget must not show manager decision controls.');
      await accessoryDialog.locator('[data-close-order-detail]').last().click(); await accessoryDialog.waitFor({ state: 'hidden' });

      await page.locator('#openStockOrders').click();
      const stockRow = page.locator('#stockOrderRows tr').filter({ hasText: 'QA-UI-SHARED-STOCK-001' });
      await stockRow.waitFor({ state: 'visible' });
      await stockRow.getByText('Bütçe onayı bekliyor', { exact: true }).waitFor({ state: 'visible' });
      assert.equal(await stockRow.locator('.stock-order-receive').count(), 0,
        'Combined 80 accessory + 30 stock commitments must hide receipt over the 100 budget.');
      await stockRow.locator('.stock-order-open').first().click();
      const stockDialog = page.locator('#stockOrderDetailDialog'); await stockDialog.waitFor({ state: 'visible' });
      assert.match(await page.locator('#stockOrderBudgetSummary').innerText(), /100.*80.*110.*bütçe aşımı/i,
        'Stock detail must show the combined commitment exceeding the shared budget.');
      assert.equal(await page.locator('#receiveStockOrderFromDetail').isVisible(), false,
        'Pending stock approval must hide detail-level receipt.');
      assert.equal(await page.locator('#stockOrderApprovalActions').isVisible(), true,
        'A company manager must see the pending decision controls.');
      await page.locator('#stockOrderApprovalNote').fill('Ortak satın alma bütçesi incelendi ve onaylandı.');
      const sharedApprovalResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/stock-orders/${sharedBudgetStockId}/approval`
        && response.request().method() === 'POST');
      await page.locator('#approveStockOrderApproval').click();
      assert.equal((await sharedApprovalResponse).status(), 200);
      assert.equal(sharedBudgetApprovalWrites.stock.length, 1, 'Approving the stock exception must make one write.');
      assert.equal(sharedBudgetApprovalWrites.stock[0].csrf, 'synthetic-asset-csrf');
      assert.equal(sharedBudgetApprovalWrites.stock[0].body.decision, 'Approved');
      await page.locator('#stockOrderApprovalActions').waitFor({ state: 'hidden' });
      assert.equal(await page.locator('#receiveStockOrderFromDetail').isVisible(), true,
        'Approval must unlock detail-level receipt.');
      await stockDialog.locator('[data-close-stock-order-detail]').last().click(); await stockDialog.waitFor({ state: 'hidden' });
      const refreshedOrders = page.waitForResponse(response => new URL(response.url()).pathname === '/api/stock-orders'
        && response.request().method() === 'GET');
      await page.locator('#stockOrdersRefresh').click(); await refreshedOrders;
      const approvedRow = page.locator('#stockOrderRows tr').filter({ hasText: 'QA-UI-SHARED-STOCK-001' });
      await approvedRow.getByText('Bütçe istisnası onaylandı', { exact: true }).waitFor({ state: 'visible' });
      assert.equal(await approvedRow.locator('.stock-order-receive').count(), 1,
        'Approval must expose the list-level receipt shortcut after refresh.');
      console.log('PASS: Chromium shows shared accessory commitments, blocks over-budget stock receipt, records a justified CSRF approval, and unlocks receipt after refresh.');
    }
  }
  assert.equal(visitedDestinations.size, destinationCount);

  if (!await page.locator('[data-page="Varlık modelleri"]').isVisible()) await page.locator('[data-group="inventory"]').click();
  await page.locator('[data-page="Varlık modelleri"]').click();
  await page.locator(`[data-model-details="${modelId}"]`).click();
  await page.locator('#modelDetailsDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#modelDetailsSummary').innerText(), /MD-TEST/);
  assert.match(await page.locator('#modelDetailsSummary').innerText(), /Teknik dosyalar/);
  assert.match(await page.locator('#modelDetailsAssets').innerText(), /QA-ASSET-001/);
  assert.match(await page.locator('#modelDetailsOrders').innerText(), /PO-MODEL-001/);
  assert.match(await page.locator('#modelDetailsOrders').innerText(), /2/);
  assert.match(await page.locator('#modelDetailsHistory').innerText(), /Model image updated/);
  const modelDetailRequest = requests.findLast(item => item.path === `/api/models/${modelId}/details`);
  assert.equal(modelDetailRequest?.method, 'GET', 'Model detail must be read-only.');
  const modelAssetDialog = page.locator('#modelDetailsAssets').getByRole('button', { name: 'QA-ASSET-001' });
  await modelAssetDialog.click();
  await page.locator('#modelDetailsDialog').waitFor({ state: 'hidden' });
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  await page.locator('#closeDialog').click();
  console.log('PASS: model detail shows scoped summary, linked assets, purchase orders, history and technical attachments; linked asset opens and modal closes safely.');

  if (!await page.locator('[data-page="Tüm satın almalar"]').isVisible()) await page.locator('[data-group="inventory"]').click();
  const purchaseWritesBefore = apiRequests.length;
  await page.locator('[data-page="Tüm satın almalar"]').click();
  await page.locator('#purchaseOverviewRows tr').first().waitFor({ state: 'visible' });
  assert.equal(await page.locator('#purchaseOverviewRows tr').count(), 25, 'The unified purchasing list must show the first 25 of 28 fixture records.');
  const firstPageText = await page.locator('#purchaseOverviewRows').innerText();
  assert.match(firstPageText, /UPO-ASSET-001/); assert.match(firstPageText, /UPO-ACCESSORY-001/); assert.match(firstPageText, /UPO-STOCK-001/); assert.match(firstPageText, /UPO-LICENSE-001/);
  assert.equal(await page.locator('#purchaseOverviewNext').isDisabled(), false, 'More than one page must enable Next.');
  await page.locator('#purchaseOverviewNext').click();
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewPageInfo')?.textContent.includes('2. sayfa'));
  assert.equal(await page.locator('#purchaseOverviewRows tr').count(), 3, 'The second page must contain the three remaining fixture records.');
  await page.locator('#purchaseOverviewPrev').click();
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewPageInfo')?.textContent.includes('1. sayfa'));
  await page.locator('#purchaseOverviewType').selectOption('accessory');
  await page.waitForFunction(() => document.querySelectorAll('#purchaseOverviewRows tr').length === 1 && document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-ACCESSORY-001'));
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders')?.query.type, 'accessory', 'Type selection must reach the API query.');
  await page.locator('#purchaseOverviewStatus').selectOption('PartiallyReceived');
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-ACCESSORY-001'));
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders')?.query.status, 'PartiallyReceived');
  const unifiedPurchaseSearchResponse = page.waitForResponse(response => { const url = new URL(response.url()); return url.pathname === '/api/purchasing/orders' && url.searchParams.get('q') === 'Synthetic Beta'; });
  await page.locator('#purchaseOverviewSearch').fill('Synthetic Beta'); await unifiedPurchaseSearchResponse;
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders')?.query.q, 'Synthetic Beta');
  await page.locator('#purchaseOverviewClear').click();
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewCount')?.textContent === '28 sipariş');
  await page.locator('#purchaseOverviewType').selectOption('asset');
  await page.locator('#purchaseOverviewSearch').fill('UPO-ASSET-001');
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-ASSET-001'));
  const [purchaseDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#purchaseOverviewExport').click()]);
  assert.equal(purchaseDownload.suggestedFilename(), 'synthetic-purchasing.csv');
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders/export.csv')?.query.type, 'asset', 'CSV download must preserve the active type filter.');
  await page.locator('#purchaseOverviewClear').click();
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewCount')?.textContent === '28 sipariş');
  await page.locator('#purchaseOverviewType').selectOption('license');
  await page.locator('#purchaseOverviewStatus').selectOption('Recorded');
  await page.locator('#purchaseOverviewSearch').fill('UPO-LICENSE-001');
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-LICENSE-001'));
  const [licensePurchaseDownload] = await Promise.all([page.waitForEvent('download'), page.locator('#purchaseOverviewExport').click()]);
  assert.equal(licensePurchaseDownload.suggestedFilename(), 'synthetic-purchasing.csv');
  const licenseExportRequest = requests.findLast(item => item.path === '/api/purchasing/orders/export.csv');
  assert.equal(licenseExportRequest?.query.type, 'license');
  assert.equal(licenseExportRequest?.query.status, 'Recorded');
  assert.equal(licenseExportRequest?.query.q, 'UPO-LICENSE-001');
  await page.locator('#purchaseOverviewClear').click();
  await page.waitForFunction(() => document.querySelector('#purchaseOverviewCount')?.textContent === '28 sipariş');
  await page.locator('#purchaseOverviewRows .purchase-overview-open[data-type="asset"]').filter({ hasText: 'UPO-ASSET-001' }).first().click();
  await page.locator('#assetOrderDetailDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assetOrderDetailTitle').textContent(), 'UPO-ASSET-001', 'The unified row action must open the matching existing asset-order detail.');
  await page.locator('[data-close-asset-order-detail]').first().click();
  await page.locator('#assetOrderDetailDialog').waitFor({ state: 'hidden' });
  await page.locator('#purchaseOverviewType').selectOption('license');
  await page.locator('#purchaseOverviewStatus').selectOption('Recorded');
  await page.waitForFunction(() => document.querySelectorAll('#purchaseOverviewRows tr').length === 1 && document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-LICENSE-001'));
  const licensePurchaseRow = page.locator('#purchaseOverviewRows tr').filter({ hasText: 'UPO-LICENSE-001' });
  assert.match(await licensePurchaseRow.innerText(), /5 lisans koltuğu.*—.*Kaydedildi/s, 'License rows must show seats and avoid suggesting a price or receipt status.');
  assert.equal(await licensePurchaseRow.locator('.purchase-overview-open[data-type="license"]').first().getAttribute('data-related-id'), softwareLicenseId,
    'License purchase rows must point to their parent license for the detail action.');
  const licenseDetailRequest = page.waitForResponse(response => new URL(response.url()).pathname === `/api/software-licenses/${softwareLicenseId}`);
  await licensePurchaseRow.locator('.purchase-overview-open[data-type="license"]').first().click(); await licenseDetailRequest;
  await page.locator('#licenseDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#licenseDetailTitle').textContent(), 'Synthetic License Import Baseline', 'A license purchase row must open its parent license details.');
  await page.locator('#closeLicenseDetail').click();
  await page.locator('#licenseDialog').waitFor({ state: 'hidden' });
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders')?.query.type, 'license');
  assert.equal(requests.findLast(item => item.path === '/api/purchasing/orders')?.query.status, 'Recorded');
  assert.equal(apiRequests.length, purchaseWritesBefore, 'Filtering, paging, refresh display and CSV export must remain read-only.');
  assert.deepEqual(pageErrors, [], `Unified purchasing must not throw: ${pageErrors.join(' | ')}`);
  console.log('PASS: Unified company purchases show asset, accessory, stock, and license records; license seat/status/unknown-cost display, filters, 25-row paging, CSV, asset detail, parent-license detail routing, and read-only behavior.');

  // Exercise the real purchase-order form without changing product data.
  const licenseOrders = [], licenseOrderWrites = [];
  let rejectLicenseOrder = false, releaseOrderWrite;
  const licenseDetailPattern = `**/api/software-licenses/${softwareLicenseId}`;
  const licenseOrderPattern = `**/api/software-licenses/${softwareLicenseId}/purchase-orders**`;
  await page.route(licenseDetailPattern, async route => {
    const response = await route.fetch(), payload = await response.json();
    await route.fulfill({ response, json: { ...payload, purchaseOrders: licenseOrders } });
  });
  await page.route(licenseOrderPattern, async route => {
    const request = route.request(), path = new URL(request.url()).pathname;
    licenseOrderWrites.push({ path, method: request.method(), csrf: request.headers()['x-csrf-token'], body: request.postDataJSON() });
    if (releaseOrderWrite) await new Promise(resolve => { releaseOrderWrite.resolve = resolve; });
    if (rejectLicenseOrder) return route.fulfill({ status: 422, json: { error: 'Synthetic purchase validation failure' } });
    if (path.endsWith('/archive') || path.endsWith('/restore')) {
      licenseOrders[0].isArchived = path.endsWith('/archive');
    } else if (request.method() === 'POST') {
      licenseOrders.push({ ...request.postDataJSON(), id: 'bbbbbbbb-1234-4234-8234-123456789abc', supplier: 'Synthetic Asset Supplier', isArchived: false });
    } else Object.assign(licenseOrders[0], request.postDataJSON());
    await route.fulfill({ status: 200, json: { id: licenseOrders[0].id } });
  });
  await licensePurchaseRow.locator('.purchase-overview-open[data-type="license"]').first().click();
  await page.locator('#licenseDialog').waitFor({ state: 'visible' });
  await page.locator('#newLicensePurchaseOrderButton').click();
  await page.locator('#licensePurchaseOrderFormDialog').waitFor({ state: 'visible' });
  await page.locator('[data-close-license-purchase-order]').first().click();
  assert.equal(licenseOrderWrites.length, 0, 'Cancel must not write a purchase order.');
  await page.locator('#newLicensePurchaseOrderButton').click();
  const purchaseForm = page.locator('#licensePurchaseOrderForm');
  await purchaseForm.locator('[name="supplierId"]').selectOption('ab000000-0000-4000-8000-000000000001');
  await purchaseForm.locator('[name="orderNumber"]').fill('LICENSE-PO-BROWSER');
  await purchaseForm.locator('[name="seatsPurchased"]').fill('3');
  rejectLicenseOrder = true;
  await purchaseForm.locator('[type="submit"]').click();
  await page.waitForFunction(() => document.querySelector('#licensePurchaseOrderForm').dataset.saving === 'false');
  assert.equal(await page.locator('#licensePurchaseOrderFormDialog').evaluate(dialog => dialog.open), true);
  assert.equal(await purchaseForm.locator('[name="orderNumber"]').inputValue(), 'LICENSE-PO-BROWSER', 'Validation failure must preserve entered data.');
  rejectLicenseOrder = false; releaseOrderWrite = {};
  await purchaseForm.locator('[type="submit"]').click();
  await page.waitForFunction(() => document.querySelector('#licensePurchaseOrderForm').dataset.saving === 'true');
  await purchaseForm.evaluate(form => form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
  await page.waitForFunction(() => document.querySelector('#licensePurchaseOrderForm [type="submit"]').disabled);
  const writeDeadline = Date.now() + 5000;
  while (!releaseOrderWrite.resolve && Date.now() < writeDeadline) await new Promise(resolve => setTimeout(resolve, 10));
  assert.ok(releaseOrderWrite.resolve, 'Purchase write must reach the route within 5 seconds.');
  assert.equal(licenseOrderWrites.length, 2, 'A pending submission must not send a duplicate request.');
  releaseOrderWrite.resolve(); releaseOrderWrite = null;
  await page.locator('#licensePurchaseOrderFormDialog').waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#licensePurchaseOrderRows').textContent.includes('LICENSE-PO-BROWSER'));
  assert.equal(licenseOrderWrites.at(-1).csrf, 'synthetic-asset-csrf');
  assert.equal(licenseOrderWrites.at(-1).body.unitCost, null, 'Unknown cost must remain null.');
  await page.locator('.license-purchase-order-edit').click();
  await purchaseForm.locator('[name="seatsPurchased"]').fill('4');
  await purchaseForm.locator('[type="submit"]').click();
  await page.locator('#licensePurchaseOrderFormDialog').waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#licensePurchaseOrderRows').textContent.includes('4'));
  assert.equal(licenseOrderWrites.at(-1).method, 'PUT');
  assert.equal(licenseOrderWrites.at(-1).body.seatsPurchased, 4);
  await page.locator('.license-purchase-order-toggle').click();
  await page.waitForFunction(() => document.querySelector('.license-purchase-order-toggle')?.dataset.action === 'restore');
  assert.equal(await page.locator('.license-purchase-order-edit').isDisabled(), true);
  await page.locator('.license-purchase-order-toggle').click();
  await page.waitForFunction(() => document.querySelector('.license-purchase-order-toggle')?.dataset.action === 'archive');
  // A supplier response arriving after the parent detail closes must not reopen a stale form.
  let releaseSuppliers;
  await page.route('**/api/suppliers?includeArchived=true', async route => {
    await new Promise(resolve => { releaseSuppliers = resolve; }); await route.continue();
  });
  await page.locator('#newLicensePurchaseOrderButton').click();
  await page.locator('#closeLicenseDetail').click();
  const supplierDeadline = Date.now() + 5000;
  while (!releaseSuppliers && Date.now() < supplierDeadline) await new Promise(resolve => setTimeout(resolve, 10));
  assert.ok(releaseSuppliers, 'Supplier load must reach the route within 5 seconds.');
  const supplierResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/suppliers');
  releaseSuppliers(); await supplierResponse;
  assert.equal(await page.locator('#licensePurchaseOrderFormDialog').evaluate(dialog => dialog.open), false);
  await page.unroute('**/api/suppliers?includeArchived=true');
  await page.unroute(licenseOrderPattern); await page.unroute(licenseDetailPattern);
  assert.deepEqual(pageErrors, []);
  console.log('PASS: License purchase form cancel, failed-save data retention, create/edit/archive/restore, CSRF, null cost, duplicate-submit protection and stale supplier response protection.');

  await page.locator('#purchaseOverviewType').selectOption('asset');
  await page.locator('#purchaseOverviewStatus').selectOption('');
  await page.locator('#purchaseOverviewSearch').fill('UPO-ASSET-001');
  await page.waitForFunction(() => document.querySelectorAll('#purchaseOverviewRows tr').length === 1 && document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-ASSET-001'));
  await page.locator('#purchaseOverviewRows tr').filter({ hasText: 'UPO-ASSET-001' }).locator('.purchase-overview-open[data-type="asset"]').first().click();
  await page.locator('#assetOrderDetailDialog').waitFor({ state: 'visible' });
  await page.locator('#returnAssetOrderFromDetail').click();
  await page.locator('#purchaseOrderReturnDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#purchaseOrderReturnLines [data-return-asset]').count(), 1, 'Received assets linked to a purchase line must be selectable for supplier return.');
  await page.locator('#purchaseOrderReturnLines [data-return-asset]').check();
  await page.locator('#purchaseOrderReturnForm [name="reason"]').fill('Sentetik varlık iade nedeni');
  const assetReturnResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/asset-orders/${unifiedPurchaseAssetId}/returns` && response.request().method() === 'POST');
  await page.locator('#submitPurchaseOrderReturn').click(); await assetReturnResponse;
  assert.equal(supplierReturnWrites.at(-1).csrf, 'synthetic-asset-csrf');
  await page.waitForFunction(() => document.querySelector('#assetOrderReturnHistory')?.textContent.includes('SR-UI-ASSET-001'));
  assert.match(await page.locator('#assetOrderReturnHistory').innerText(), /SR-UI-ASSET-001.*Sentetik varlık iade nedeni/s, 'Successful asset return must refresh and render its history.');
  assert.equal(await page.locator('#purchaseOrderReturnDialog').isVisible(), false, 'The return modal must close after successful save.');
  await page.locator('[data-close-asset-order-detail]').first().click();

  await page.locator('#purchaseOverviewType').selectOption('accessory');
  await page.locator('#purchaseOverviewSearch').fill('UPO-ACCESSORY-001');
  await page.waitForFunction(() => document.querySelectorAll('#purchaseOverviewRows tr').length === 1 && document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-ACCESSORY-001'));
  await page.locator('#purchaseOverviewRows tr').filter({ hasText: 'UPO-ACCESSORY-001' }).locator('.purchase-overview-open[data-type="accessory"]').first().click();
  await page.locator('#accessoryOrderDetailDialog').waitFor({ state: 'visible' });
  await page.locator('#returnAccessoryOrderFromDetail').click();
  await page.locator('#purchaseOrderReturnDialog').waitFor({ state: 'visible' });
  await page.locator(`#purchaseOrderReturnLines [data-return-line="${supplierAccessoryLineId}"]`).fill('1');
  await page.locator('#purchaseOrderReturnForm [name="reason"]').fill('Sentetik aksesuar iade nedeni');
  const accessoryReturnResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/accessory-orders/${unifiedPurchaseAccessoryId}/returns` && response.request().method() === 'POST');
  await page.locator('#submitPurchaseOrderReturn').click(); await accessoryReturnResponse;
  assert.equal(supplierReturnWrites.at(-1).body.items[0].quantity, 1);
  await page.waitForFunction(() => document.querySelector('#accessoryOrderReturnHistory')?.textContent.includes('SR-UI-ACCESSORY-001'));
  assert.match(await page.locator('#accessoryOrderReturnHistory').innerText(), /SR-UI-ACCESSORY-001.*Sentetik aksesuar iade nedeni/s);
  await page.locator('[data-close-order-detail]').last().click();

  await page.locator('#purchaseOverviewType').selectOption('stock');
  await page.locator('#purchaseOverviewSearch').fill('UPO-STOCK-001');
  await page.waitForFunction(() => document.querySelectorAll('#purchaseOverviewRows tr').length === 1 && document.querySelector('#purchaseOverviewRows')?.textContent.includes('UPO-STOCK-001'));
  await page.locator('#purchaseOverviewRows tr').filter({ hasText: 'UPO-STOCK-001' }).locator('.purchase-overview-open[data-type="stock"]').first().click();
  await page.locator('#stockOrderDetailDialog').waitFor({ state: 'visible' });
  await page.locator('#returnStockOrderFromDetail').click();
  await page.locator('#purchaseOrderReturnDialog').waitFor({ state: 'visible' });
  await page.locator(`#purchaseOrderReturnLines [data-return-line="${supplierStockConsumableLineId}"]`).fill('1');
  await page.locator(`#purchaseOrderReturnLines [data-return-line="${supplierStockComponentLineId}"]`).fill('2');
  await page.locator('#purchaseOrderReturnForm [name="reason"]').fill('Sentetik sarf ve bileşen iade nedeni');
  const stockReturnResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/stock-orders/${unifiedPurchaseStockId}/returns` && response.request().method() === 'POST');
  await page.locator('#submitPurchaseOrderReturn').click(); await stockReturnResponse;
  assert.deepEqual(supplierReturnWrites.at(-1).body.items.map(item => item.quantity), [1, 2]);
  await page.waitForFunction(() => document.querySelector('#stockOrderReturnHistory')?.textContent.includes('SR-UI-STOCK-001'));
  assert.match(await page.locator('#stockOrderReturnHistory').innerText(), /SR-UI-STOCK-001.*Sentetik sarf ve bileşen iade nedeni/s);
  await page.locator('[data-close-stock-order-detail]').first().click();
  assert.equal(supplierReturnWrites.length, 3, 'Asset, accessory, and stock return forms must each send exactly one API write.');
  assert.deepEqual(pageErrors, [], `Supplier return UI must not throw: ${pageErrors.join(' | ')}`);
  console.log('PASS: Chromium opens all three purchase-order detail and return modals, validates selectable return rows, posts date/reason/quantity with CSRF once, closes on success, and renders refreshed return history.');

  if (!await page.locator('[data-page="Şirket birimleri"]').isVisible()) await page.locator('[data-group="places"]').click();
  await page.locator('[data-page="Şirket birimleri"]').click();
  companyUnitItems = [];
  await page.locator('#companyUnitRows').waitFor({ state: 'visible' });
  await page.locator('#addCompanyUnitButton').click();
  await page.locator('#companyUnitDialog').waitFor({ state: 'visible' });
  await page.locator('#companyUnitForm [name="name"]').fill('QA Legal Entity');
  await page.locator('#companyUnitForm [name="code"]').fill('QA-LEGAL-001');
  await page.locator('#companyUnitForm [name="unitType"]').selectOption('legal_entity');
  const legalEntityCreate = page.waitForResponse(response => new URL(response.url()).pathname === '/api/company-units' && response.request().method() === 'POST');
  await page.locator('#companyUnitForm button[type="submit"]').click(); await legalEntityCreate;
  const legalEntityRow = page.locator('#companyUnitRows tr').filter({ has: page.locator(`[data-company-unit-edit="${companyUnitRootId}"]`) });
  await legalEntityRow.waitFor({ state: 'visible' });
  assert.equal(companyUnitWrites.at(-1).body.unitType, 'legal_entity');
  assert.match(await page.locator('#companyUnitRows').innerText(), /QA Legal Entity.*Tüzel kişilik/s,
    'The company-unit table must render the persisted legal-entity type.');

  await page.locator('#addCompanyUnitButton').click();
  await page.locator('#companyUnitForm [name="name"]').fill('QA Branch Office');
  await page.locator('#companyUnitForm [name="code"]').fill('QA-BRANCH-001');
  await page.locator('#companyUnitForm [name="unitType"]').selectOption('branch');
  await page.locator('#companyUnitForm [name="parentCompanyUnitId"]').selectOption(companyUnitRootId);
  const branchCreate = page.waitForResponse(response => new URL(response.url()).pathname === '/api/company-units' && response.request().method() === 'POST');
  await page.locator('#companyUnitForm button[type="submit"]').click(); await branchCreate;
  const branchRow = page.locator('#companyUnitRows tr').filter({ has: page.locator(`[data-company-unit-edit="${companyUnitBranchId}"]`) });
  await branchRow.waitFor({ state: 'visible' });
  assert.equal(companyUnitWrites.at(-1).body.unitType, 'branch');
  assert.equal(companyUnitWrites.at(-1).body.parentCompanyUnitId, companyUnitRootId,
    'The branch form must preserve its selected legal-entity parent.');
  assert.match(await branchRow.innerText(), /Şube.*QA Legal Entity \/ QA Branch Office/s,
    'The company-unit table must show the branch type and full hierarchy path.');

  await branchRow.getByRole('button', { name: 'Düzenle' }).click();
  await page.locator('#companyUnitForm [name="code"]').fill('QA-BRANCH-002');
  const branchUpdate = page.waitForResponse(response => new URL(response.url()).pathname === `/api/company-units/${companyUnitBranchId}` && response.request().method() === 'PUT');
  await page.locator('#companyUnitForm button[type="submit"]').click(); await branchUpdate;
  await page.locator('#companyUnitRows tr').filter({ hasText: 'QA-BRANCH-002' }).waitFor({ state: 'visible' });
  assert.equal(companyUnitWrites.at(-1).body.unitType, 'branch', 'Editing a branch must retain its type.');
  assert.equal(companyUnitWrites.at(-1).body.parentCompanyUnitId, companyUnitRootId, 'Editing a branch must retain its legal-entity parent.');
  assert.deepEqual(pageErrors, [], 'Company-unit create, branch hierarchy, edit, and list rendering must not throw.');
  console.log('PASS: Chromium company-unit workflow creates a legal entity and child branch, posts selected types with CSRF, displays the hierarchy, and preserves type/parent on edit.');

  memberAccessTestEnabled = true;
  if (!await page.locator('[data-page="Uygulama kullanıcıları"]').isVisible()) await page.locator('[data-group="people"]').click();
  await page.locator('[data-page="Uygulama kullanıcıları"]').click();
  await page.locator('#membersRows tr').filter({ hasText: 'Synthetic scoped member' }).waitFor({ state: 'visible' });
  await page.getByRole('button', { name: 'Şirket birimi erişimi' }).click();
  await page.locator('#memberLocationAccessDialog').waitFor({ state: 'visible' });
  const unitMode = page.locator('#memberCompanyUnitAccessMode');
  await unitMode.selectOption('restricted');
  const branchScope = page.locator(`#memberCompanyUnitAccessList input[value="${companyUnitBranchId}"]`);
  await branchScope.check();
  await page.locator('#memberLocationAccessForm button[type="submit"]').click();
  await page.waitForFunction(() => document.querySelector('#memberLocationAccessDialog')?.open === false);
  assert.equal(memberAccessWrites.length, 1, 'Saving unit access must perform exactly one API write.');
  assert.equal(memberAccessWrites[0].csrf, 'synthetic-asset-csrf');
  assert.equal(memberAccessState.companyUnitMode, 'restricted');
  assert.deepEqual(memberAccessState.companyUnitIds, [companyUnitBranchId]);
  await page.getByRole('button', { name: 'Birim erişimini düzenle' }).click();
  await page.locator('#memberLocationAccessDialog').waitFor({ state: 'visible' });
  assert.equal(await unitMode.inputValue(), 'restricted', 'Reloading the dialog must preserve the server-selected unit scope.');
  assert.equal(await branchScope.isChecked(), true, 'Reloading the dialog must restore the selected company unit.');
  await page.locator('#closeMemberLocationAccess').click();
  await page.locator('#memberLocationAccessDialog').waitFor({ state: 'hidden' });
  console.log('PASS: Chromium member access dialog saves a CSRF-protected company-unit restriction, then reloads the persisted unit and hierarchy selection.');

  assert.deepEqual(pageErrors, [], `The asset detail UI must not throw: ${pageErrors.join(' | ')}`);
  console.log(`PASS: owner Ctrl+N; asset edit/status/trash/audit/maintenance/transfer/assignment; expired-session dialog closure, deliberate re-login, and no stale-write replay; company JSON restore preview and confirmations; company contract list/search/filter/detail/create/cancel/archive/restore with CSRF; license attachment delete cancel/confirm persistence; license CSV preview, stale-file rejection, import cancel, error retry, confirmed CSRF apply and list persistence; all ${visitedDestinations.size} sidebar destinations render; ${placeholderDestinations.length} open the explicit feature/module status surface (${placeholderDestinations.join(', ')}); browser error-free completion.`);
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
