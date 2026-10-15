import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const secondCompanyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab';
const userId = '22222222-2222-4222-8222-222222222222';
const assetId = '33333333-3333-4333-8333-333333333333';
const employeeId = '99999999-9999-4999-8999-999999999999';
const asset = {
  id: assetId, companyId, companyName: 'Synthetic Asset Company', assetTag: 'QA-ASSET-001',
  name: 'Synthetic audit target', status: 'Available', serialNumber: 'QA-SERIAL-001',
  categoryId: '55555555-5555-4555-8555-555555555555', location: 'Head Office', updatedAt: '2026-10-14T12:00:00Z', nextAuditOn: null,
  createdAt: '2026-10-01T12:00:00Z', purchaseCost: null, currency: 'TRY'
};
const importedAssets = [];
let bulkAssets = [];
const appHtml = await readFile('/checkout-site/company-app.html');
const requests = [];
const apiRequests = [];
const expiryLoginAttempts = [];
let syntheticSessionExpired = false;
let companySwitchLoginFlow = false;
let activeCompanyId = companyId;
let expireNextAssetPatch = false;
const assetHistory = [];
const assetAttachments = [];
const attachmentId = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
const attachmentBytes = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00]);
const maintenanceRows = [];
const maintenanceId = '77777777-7777-4777-8777-777777777777';
const restoreArchive = { formatVersion: 1, company: { name: 'Synthetic Restore Source' }, assets: [], employees: [] };
const restoreApplyWrites = [];
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
  notes: null
}];
const softwareLicenseAttachments = [{ id: softwareLicenseAttachmentId, originalFileName: 'synthetic-eula.pdf', contentType: 'application/pdf',
  length: 1024, createdAt: '2026-10-14T12:00:00Z', uploadedBy: 'Synthetic Owner', isEulaDocument: false }];
const softwareLicenseWrites = [];
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
const json = (response, status = 200, body = null) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

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
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}` && expireNextAssetPatch) {
    expireNextAssetPatch = false;
    syntheticSessionExpired = true;
    json(response, 401, { message: 'Synthetic session expired.' });
    return;
  }

  if (request.method === 'GET') {
    if (url.pathname === '/api/session' && syntheticSessionExpired) {
      json(response, 401, { message: 'Synthetic session expired.' });
      return;
    }
    let result;
    if (url.pathname === `/api/assets/${assetId}/attachments/${attachmentId}/file`) {
      response.writeHead(200, { 'content-type': 'image/png', 'content-length': attachmentBytes.length, 'cache-control': 'no-store' }); response.end(attachmentBytes); return;
    }
    else if (url.pathname === '/api/status') result = { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 };
    else if (url.pathname === '/api/setup/status') result = { setupRequired: false };
    else if (url.pathname === '/api/session/csrf') result = { requestToken: 'synthetic-asset-csrf' };
    else if (url.pathname === '/api/session') result = {
      companyId: activeCompanyId, companyName: activeCompanyId === companyId ? 'Synthetic Asset Company' : 'Synthetic Second Company',
      availableCompanies: [{ id: companyId, name: 'Synthetic Asset Company', role: 'owner' }, { id: secondCompanyId, name: 'Synthetic Second Company', role: 'viewer' }],
      userId, name: 'Synthetic Owner', role: 'owner',
      hasPhoto: false, subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' }
    };
    else if (url.pathname === '/api/company/branding') result = { name: 'Synthetic Asset Company', brandPrimaryColor: '#2563EB', hasLogo: false };
    else if (url.pathname === '/api/company/label-settings') result = { isConfigured: false, settings: null };
    else if (url.pathname === '/api/dashboard/summary') result = {
      totalAssets: (asset.isDeleted ? 0 : 1) + importedAssets.length + bulkAssets.length, assignedAssets: 0, availableAssets: (asset.isDeleted ? 0 : 1) + importedAssets.length + bulkAssets.length, maintenanceAssets: 0,
      softwareLicenses: 0, accessories: 0, consumables: 0, components: 0, activeEmployees: 0
    };
    else if (url.pathname === '/api/assets') { const items = [...(asset.isDeleted ? [] : [asset]), ...importedAssets, ...bulkAssets]; result = { items, total: items.length, page: 1, pageSize: 50 }; }
    else if (url.pathname === `/api/assets/${assetId}`) result = {
      asset: { ...asset }, references: { model: null, modelNumber: null, employee: asset.assignedEmployeeName || null,
        employeeNumber: asset.assignedEmployeeId ? 'QA-001' : null, defaultLocation: 'Head Office' },
      history: [...assetHistory], attachments: [...assetAttachments], accessories: [], components: [], softwareLicenses: [], relatedAssets: [],
      valuation: {}, purchaseOrder: null
    };
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
      const items = softwareLicenseRows.filter(item => !q || [item.name, item.publisher, item.supplier, item.licenseReference]
        .some(value => value?.toLocaleLowerCase().includes(q)));
      result = { items, total: items.length, page: 1, pageSize: 25 };
    }
    else if (url.pathname === `/api/software-licenses/${softwareLicenseId}`) result = {
      license: { ...softwareLicenseRows.find(item => item.id === softwareLicenseId) }, assignments: [], events: [],
      attachments: softwareLicenseAttachments.map(item => ({ ...item })), currentEula: null
    };
    else if (url.pathname === '/api/software-licenses/import-mappings') result = { items: [] };
    else if (url.pathname.startsWith('/api/contracts/') && url.pathname.endsWith('/attachments')) result = { items: [] };
    else if (url.pathname === '/api/assets/import-mappings') result = { items: assetImportProfiles.map(profile => ({ ...profile, mappings: { ...profile.mappings } })) };
    else if (url.pathname === '/api/assets/custom-fields') result = [];
    else if (url.pathname === '/api/employees' || url.pathname.startsWith('/api/employees?')) result = {
      items: [{ id: employeeId, displayName: 'Synthetic QA Employee', employeeNumber: 'QA-001' }], total: 1, page: 1, pageSize: 200
    };
    else if (url.pathname === '/api/lookups/categories') result = [{ id: asset.categoryId, name: 'Synthetic equipment', assetCount: 1 }];
    else if (url.pathname === '/api/lookups/brands' || url.pathname === '/api/lookups/models') result = [];
    else if (url.pathname === '/api/locations') result = { items: [] };
    else if (url.pathname.startsWith('/api/assets/audit-schedule')) result = { items: [], total: 0, overdue: 0, dueSoon: 0 };
    else if (url.pathname === '/api/activity') result = { entries: [], total: 0 };
    else if (url.pathname === '/api/calendar') result = { items: [], total: 0 };
    else result = { items: [], total: 0, page: 1, pageSize: 50 };
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(JSON.stringify(result));
    return;
  }

  if (request.headers['x-csrf-token'] !== 'synthetic-asset-csrf') {
    response.writeHead(400, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ message: 'Missing synthetic CSRF token.' })); return;
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
    assert.equal(body.assetEventId, assetHistory[0]?.id || '');
    const item = { id: attachmentId, originalFileName: body.file.filename, contentType: 'image/png', length: attachmentBytes.length,
      createdAt: '2026-10-15T12:00:00Z', uploadedBy: 'Synthetic Owner', assetEventType: assetHistory[0]?.eventType || null };
    assetAttachments.push(item);
    if (assetHistory[0]) assetHistory[0].attachmentCount = (assetHistory[0].attachmentCount || 0) + 1;
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
    assetHistory.unshift({
      id: '44444444-4444-4444-8444-444444444444', eventType: 'Audited', occurredAt: asset.updatedAt,
      details: `Fiziksel denetim tamamlandı · ${body.auditDate}${body.notes ? ` · ${body.notes}` : ''}`,
      actorName: 'Synthetic Owner', attachmentCount: 0
    });
    response.writeHead(204); response.end();
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
    assert.equal(body.notes, 'Quarterly physical audit');
    assert.match(body.auditDate, /^\d{4}-\d{2}-\d{2}$/);
    assert.match(body.nextAuditOn, /^\d{4}-\d{2}-\d{2}$/);
    const selected = new Set(body.assetIds);
    const targets = bulkAssets.filter(item => selected.has(item.id));
    assert.equal(targets.length, 2, 'The UI must send exactly the two selected synthetic assets.');
    const auditEvents = [];
    for (const target of targets) {
      target.nextAuditOn = body.nextAuditOn;
      target.updatedAt = '2026-10-15T11:00:00Z';
      auditEvents.push({ assetId: target.id, auditDate: body.auditDate, notes: body.notes });
    }
    json(response, 200, { recorded: targets.length, auditDate: body.auditDate, nextAuditOn: body.nextAuditOn, auditEvents });
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
await page.addInitScript(() => {
  window.__syntheticClipboard = '';
  Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async text => { window.__syntheticClipboard = String(text); } } });
  window.bwipjs = { toCanvas(canvas) { canvas.width = 120; canvas.height = 40; const context = canvas.getContext('2d'); if (context) { context.fillStyle = '#fff'; context.fillRect(0, 0, 120, 40); } } };
});
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('[data-group="inventory"]').click();
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
  await assetRow.locator('[data-asset-action="view"]').click();
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#dialogTitle').innerText(), asset.name,
    'The row detail shortcut must open the selected asset, not the surrounding row or a different record.');
  await page.locator('#assetDialog #closeDialog').click();
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening and closing row detail must remain read-only.');

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
  await page.locator('#newAssetDialog [data-close-new]').first().click();
  await newAssetDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Opening and canceling a row clone must not create an asset.');

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

  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
  const assetDialog = page.locator('#assetDialog');
  await assetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#dialogTitle').innerText(), asset.name);
  await page.locator('#copyAssetTag').click();
  await page.waitForFunction(() => window.__syntheticClipboard === 'QA-ASSET-001');
  assert.equal(await page.evaluate(() => window.__syntheticClipboard), 'QA-ASSET-001', 'Copy-tag action must place the selected asset tag on the clipboard.');
  await page.locator('#printAssetLabel').click();
  const detailLabelPreview = page.locator('#assetLabelPreviewDialog');
  await detailLabelPreview.waitFor({ state: 'visible' });
  await page.waitForFunction(() => !document.querySelector('#assetLabelPreviewPrint')?.disabled);
  const detailLabelHtml = await page.locator('#assetLabelPreviewFrame').getAttribute('srcdoc');
  assert.match(detailLabelHtml, /QA-ASSET-001/, 'Detail label action must print the selected asset tag.');
  assert.match(detailLabelHtml, /Synthetic audit target/, 'Detail label action must print the selected asset name.');
  await page.locator('#assetLabelPreviewClose').click();
  await detailLabelPreview.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Detail label preview must not write to the API or start printing.');
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
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).waitFor({ state: 'visible' });
  const statusWrite = apiRequests.at(-1);
  assert.equal(statusWrite.method, 'PATCH');
  assert.equal(statusWrite.path, `/api/assets/${assetId}/status`);
  assert.deepEqual(statusWrite.body, { status: 'Unusable', note: 'Synthetic status reason' });
  assert.equal(statusWrite.csrf, 'synthetic-asset-csrf');
  assert.match(await page.locator('#assetRows').innerText(), /Kullanılamaz/);
  assert.equal(asset.status, 'Unusable');
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
  await assetDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusAction').selectOption('Available');
  await statusDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusForm [name="note"]').fill('Reset synthetic test fixture');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).waitFor({ state: 'visible' });
  assert.equal(apiRequests.at(-1).path, `/api/assets/${assetId}/status`);
  assert.deepEqual(apiRequests.at(-1).body, { status: 'Available', note: 'Reset synthetic test fixture' });
  assert.equal(asset.status, 'Available', 'The status action must allow an authorized return to Available.');
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
  await assetDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusAction').selectOption('Archived');
  await statusDialog.waitFor({ state: 'visible' });
  await page.locator('#assetStatusForm [name="note"]').fill('Synthetic archive before restore test');
  await page.locator('#submitAssetStatus').click();
  await statusDialog.waitFor({ state: 'hidden' });
  assert.equal(asset.status, 'Archived');
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
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
    ['attachments', '#assetTabAttachments', /Fotoğraflar ve ek dosyalar/]
  ];
  for (const [tab, panel, heading] of tabCases) {
    const tabButton = page.locator(`[data-asset-tab="${tab}"]`);
    await tabButton.click();
    assert.equal(await tabButton.getAttribute('aria-selected'), 'true', `${tab} tab must become selected.`);
    assert.equal(await page.locator(panel).isVisible(), true, `${tab} panel must be visible.`);
    assert.match(await page.locator(panel).innerText(), heading);
  }
  await page.locator('#assetTabOverviewButton').focus();
  await page.keyboard.press('End');
  assert.equal(await page.locator('#assetTabAttachmentsButton').getAttribute('aria-selected'), 'true', 'End must select the last asset detail tab.');
  await page.keyboard.press('Home');
  assert.equal(await page.locator('#assetTabOverviewButton').getAttribute('aria-selected'), 'true', 'Home must select the first asset detail tab.');

  const editDialog = page.locator('#editAssetDialog');
  const beforeEditCancel = apiRequests.length;
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  const editForm = page.locator('#editAssetForm');
  assert.equal(await editForm.locator('[name="assetName"]').inputValue(), 'Synthetic audit target');
  assert.equal(await editForm.locator('[name="categoryId"]').inputValue(), asset.categoryId);
  await page.locator('[data-close-edit-asset]').first().click();
  await editDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeEditCancel, 'Canceling asset edit must not write.');

  const editedName = 'Synthetic asset edit persisted';
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  await editForm.locator('[name="assetName"]').fill(editedName);
  assert.equal(await editForm.locator('[name="assetName"]').evaluate(input => input.checkValidity()), true);
  await editForm.locator('button[type="submit"]').click();
  await editDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(apiRequests.length, beforeEditCancel + 1, 'Only the explicit asset edit should write before the audit flows.');
  const editWrite = apiRequests.at(-1);
  assert.equal(editWrite.method, 'PATCH');
  assert.equal(editWrite.path, `/api/assets/${assetId}`);
  assert.equal(editWrite.body.name, editedName);
  assert.equal(editWrite.body.expectedUpdatedAt, '2026-10-14T13:30:00Z');
  assert.equal(editWrite.csrf, 'synthetic-asset-csrf');
  await page.locator('#assetDialog').locator('#closeDialog').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).waitFor({ state: 'visible' });
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(asset.name, editedName, 'Reloaded list and detail must reflect the persisted server result.');

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
  assert.deepEqual(auditRequest.body, { auditDate, nextAuditOn, notes: auditNotes });
  assert.equal(auditRequest.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.nextAuditOn, nextAuditOn);
  assert.equal(assetHistory[0].eventType, 'Audited');
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic audit saved from Chromium/);

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
  await page.locator('#assetTabHistoryButton').click();
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic assignment note/);

  const latestEvent = assetHistory[0];
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

  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
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
  assert.equal(await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).isVisible(), true);
  await page.locator('#trashAssetButton').click();
  await trashDialog.waitFor({ state: 'visible' });
  await page.locator('#assetTrashConfirmForm [name="note"]').fill('Synthetic archive reason');
  await page.locator('#submitAssetTrashConfirm').click();
  await trashDialog.waitFor({ state: 'hidden' });
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).waitFor({ state: 'detached' });
  const trashWrite = apiRequests.at(-1);
  assert.equal(trashWrite.method, 'POST');
  assert.equal(trashWrite.path, `/api/assets/${assetId}/trash`);
  assert.deepEqual(trashWrite.body, { note: 'Synthetic archive reason' });
  assert.equal(trashWrite.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.isDeleted, true, 'Only the explicitly confirmed action may move the synthetic asset to trash.');

  const bulkAssetIds = ['12121212-1212-4121-8121-121212121212', '13131313-1313-4131-8131-131313131313'];
  bulkAssets = bulkAssetIds.map((id, index) => ({
    ...asset, id, assetTag: `QA-BULK-${index + 1}`, name: `Synthetic bulk status ${index + 1}`,
    status: 'Available', updatedAt: '2026-10-15T09:00:00Z', assignedEmployeeId: null, nextAuditOn: null
  }));
  await page.locator('#sidebar button[data-page="Varlıklar"]').click();
  const firstBulkRow = page.locator(`#assetRows tr[data-id="${bulkAssetIds[0]}"]`);
  const secondBulkRow = page.locator(`#assetRows tr[data-id="${bulkAssetIds[1]}"]`);
  await firstBulkRow.waitFor({ state: 'visible' });
  await secondBulkRow.waitFor({ state: 'visible' });
  await firstBulkRow.locator('.asset-row-select').check();
  await secondBulkRow.locator('.asset-row-select').check();
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
    assetIds: bulkAssetIds, auditDate: bulkAuditDate, nextAuditOn: bulkNextAuditOn, notes: 'Quarterly physical audit'
  });
  assert.equal(bulkAuditWrite?.csrf, 'synthetic-asset-csrf');
  assert.equal(await bulkToolbar.isHidden(), true, 'A successful bulk audit must clear the selection and hide the bulk toolbar.');
  console.log('PASS: bulk asset audit cancellation writes nothing; confirmed audit records date/note and persists the next schedule for both selected assets.');

  const sidebarGroups = page.locator('#sidebar [data-group]');
  for (const group of await sidebarGroups.all()) {
    if (await group.getAttribute('aria-expanded') !== 'true') await group.click();
  }
  const sidebarDestinations = page.locator('#sidebar button[data-page]');
  const destinationCount = await sidebarDestinations.count();
  assert.equal(destinationCount, 50, 'The owner navigation smoke acceptance must cover all 50 documented sidebar destinations.');
  const expectedSurfaceByDestination = {
    'Genel Bakış': 'overview', 'Varlıklar': 'overview',
    'Varlık siparişleri': 'assetOrdersPage', 'Varlık modelleri': 'catalogPage', 'Markalar': 'catalogPage', 'Kategoriler': 'catalogPage',
    'Durum etiketleri': 'assetStatusLabelsPage', 'QR ve barkodlar': 'labelSettingsPage',
    'Zimmetler': 'assignmentsPage', 'İadeler': 'returnsPage', 'Transferler': 'activityPage', 'Rezervasyonlar': 'reservationsPage', 'Hareket geçmişi': 'activityPage',
    'Yazılım lisansları': 'licensesPage', 'Lisans atamaları': 'licenseAssignmentsPage', 'Sözleşmeler': 'contractsPage', 'Garanti takibi': 'reportPage',
    'Aksesuarlar': 'accessoriesPage', 'Siparişler': 'accessoryOrdersPage', 'Sarf malzemeleri': 'consumablesPage', 'Stok talepleri': 'inventoryRequestsPage',
    'Bileşenler': 'componentsPage', 'Hazır kitler': 'accessoryKitsPage', 'Stok hareketleri': 'inventoryMovementsPage',
    'Çalışan kayıtları': 'employeesPage', 'Uygulama kullanıcıları': 'membersPage', 'Departmanlar': 'directoryPage', 'Yetki rolleri': 'rolesPage',
    'Lokasyonlar': 'directoryPage', 'Binalar ve odalar': 'directoryPage', 'Şirket birimleri': 'companyUnitsPage',
    'Bakım kayıtları': 'maintenancePage', 'Bakım planları': 'calendarPage', 'Denetimler': 'auditPage', 'Takvim': 'calendarPage', 'Görevler': 'tasksPage',
    'Hazır raporlar': 'reportPage', 'Özel rapor oluştur': 'customReportPage', 'Dışa aktarımlar': 'dataToolsPage',
    'Tablodan içe aktar': 'dataToolsPage', 'Verileri dışa aktar': 'dataToolsPage', 'Yedekleme geçmişi': 'dataToolsPage', 'Geri yükle': 'dataToolsPage',
    'Firma profili': 'companySettingsPage', 'Logo ve tema': 'companySettingsPage', 'Bildirimler': 'featureStatusPage', 'Entegrasyonlar': 'integrationsPage',
    'Güvenlik': 'featureStatusPage', 'Denetim kayıtları': 'auditPage', 'Destek': 'supportPage'
  };
  assert.equal(Object.keys(expectedSurfaceByDestination).length, destinationCount, 'Every documented sidebar destination must have an exact expected screen surface.');
  const visitedDestinations = new Set();
  const placeholderDestinations = [];
  for (const destination of await sidebarDestinations.all()) {
    const name = await destination.getAttribute('data-page');
    assert.ok(name && !visitedDestinations.has(name), `Sidebar destination must be unique and named: ${name}`);
    visitedDestinations.add(name);
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
    if (name === 'Bildirimler') {
      const beforeNotificationShortcut = apiRequests.length;
      await page.locator('#featureStatusNotificationButton').click();
      await page.locator('#notificationPanel').waitFor({ state: 'visible' });
      await page.locator('#notificationPreferencesForm').waitFor({ state: 'visible' });
      assert.equal(await page.locator('#notificationButton').getAttribute('aria-expanded'), 'true');
      await page.mouse.click(1200, 800);
      await page.locator('#notificationPanel').waitFor({ state: 'hidden' });
      assert.equal(await page.locator('#notificationButton').getAttribute('aria-expanded'), 'false');
      assert.equal(apiRequests.length, beforeNotificationShortcut, 'Opening and dismissing the notification shortcut must not write preferences.');
    }
    if (name === 'Güvenlik') {
      const beforeSecurityShortcut = apiRequests.length;
      assert.equal(await page.locator('#featureStatusAccountSecurityButton').isVisible(), true,
        'The Security menu must keep the boundary explanation and account-security shortcut visible.');
      await page.locator('#accountPhotoDialog').waitFor({ state: 'visible' });
      assert.equal(apiRequests.length, beforeSecurityShortcut, 'Opening account security must be read-only until a separate preference change is submitted.');
      await page.locator('#closeAccountPhoto').click();
      await page.locator('#accountPhotoDialog').waitFor({ state: 'hidden' });
    }
  }
  assert.equal(visitedDestinations.size, destinationCount);

  assert.deepEqual(pageErrors, [], `The asset detail UI must not throw: ${pageErrors.join(' | ')}`);
  console.log(`PASS: owner Ctrl+N; asset edit/status/trash/audit/maintenance/transfer/assignment; expired-session dialog closure, deliberate re-login, and no stale-write replay; company JSON restore preview and confirmations; company contract list/search/filter/detail/create/cancel/archive/restore with CSRF; license attachment delete cancel/confirm persistence; license CSV preview, stale-file rejection, import cancel, error retry, confirmed CSRF apply and list persistence; all ${visitedDestinations.size} sidebar destinations render; ${placeholderDestinations.length} open the explicit feature/module status surface (${placeholderDestinations.join(', ')}); browser error-free completion.`);
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
