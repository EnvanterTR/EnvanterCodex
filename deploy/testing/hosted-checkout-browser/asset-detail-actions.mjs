import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const assetId = '33333333-3333-4333-8333-333333333333';
const employeeId = '99999999-9999-4999-8999-999999999999';
const asset = {
  id: assetId, companyId, companyName: 'Synthetic Asset Company', assetTag: 'QA-ASSET-001',
  name: 'Synthetic audit target', status: 'Available', serialNumber: 'QA-SERIAL-001',
  categoryId: '55555555-5555-4555-8555-555555555555', location: 'Head Office', updatedAt: '2026-10-14T12:00:00Z', nextAuditOn: null,
  createdAt: '2026-10-01T12:00:00Z', purchaseCost: null, currency: 'TRY'
};
const appHtml = await readFile('/checkout-site/company-app.html');
const requests = [];
const apiRequests = [];
const expiryLoginAttempts = [];
let syntheticSessionExpired = false;
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
const contractAsset = { id: contractAssetId, assetTag: 'QA-CONTRACT-ASSET', name: 'Synthetic Contract Device', serialNumber: 'QA-CONTRACT-SERIAL' };
const contractLicense = { id: contractLicenseId, name: 'Synthetic Office License', licenseReference: 'QA-LICENSE-001', expirationDate: '2027-10-01' };
const json = (response, status = 200, body = null) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

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
  if (request.method !== 'GET') apiRequests.push(record);

  if (request.method === 'POST' && url.pathname === '/api/session/login') {
    expiryLoginAttempts.push(record);
    assert.equal(record.csrf, 'synthetic-asset-csrf', 'Session re-entry must use a fresh CSRF token.');
    assert.deepEqual(record.body, { email: 'qa-session@example.invalid', password: 'Synthetic-session-password', companyId: null, rememberMe: true, twoFactorCode: null });
    syntheticSessionExpired = false;
    json(response, 200, { companyId, companyName: 'Synthetic Asset Company', userId, name: 'Synthetic Owner', role: 'owner', hasPhoto: false,
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' } });
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
      companyId, companyName: 'Synthetic Asset Company', userId, name: 'Synthetic Owner', role: 'owner',
      hasPhoto: false, subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' }
    };
    else if (url.pathname === '/api/company/branding') result = { name: 'Synthetic Asset Company', brandPrimaryColor: '#2563EB', hasLogo: false };
    else if (url.pathname === '/api/company/label-settings') result = { isConfigured: false, settings: null };
    else if (url.pathname === '/api/dashboard/summary') result = {
      totalAssets: 1, assignedAssets: 0, availableAssets: 1, maintenanceAssets: 0,
      softwareLicenses: 0, accessories: 0, consumables: 0, components: 0, activeEmployees: 0
    };
    else if (url.pathname === '/api/assets') result = { items: asset.isDeleted ? [] : [asset], total: asset.isDeleted ? 0 : 1, page: 1, pageSize: 50 };
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
    else if (url.pathname.startsWith('/api/contracts/') && url.pathname.endsWith('/attachments')) result = { items: [] };
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
  await page.locator('#assignAssetButton').click();
  const assignDialog = page.locator('#assignDialog');
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
  await page.locator('#loginForm button[type="submit"]').click();
  await page.waitForFunction(() => !document.querySelector('#authLayer')?.classList.contains('show'));
  assert.equal(expiryLoginAttempts.length, 1, 'The synthetic session must be restored only after one deliberate login submission.');
  assert.equal(asset.name, nameBeforeExpiry, 'Re-authentication must not silently apply the unsaved form data.');
  assert.equal(apiRequests.filter(item => item.path === `/api/assets/${assetId}` && item.method === 'PATCH').length, failedWritesBeforeExpiry + 1,
    'Re-authentication must not automatically retry the previously rejected write.');
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
      await page.locator('#featureStatusAccountSecurityButton').click();
      await page.locator('#accountPhotoDialog').waitFor({ state: 'visible' });
      assert.equal(apiRequests.length, beforeSecurityShortcut, 'Opening account security must be read-only until a separate preference change is submitted.');
      await page.locator('#closeAccountPhoto').click();
      await page.locator('#accountPhotoDialog').waitFor({ state: 'hidden' });
    }
  }
  assert.equal(visitedDestinations.size, destinationCount);

  assert.deepEqual(pageErrors, [], `The asset detail UI must not throw: ${pageErrors.join(' | ')}`);
  console.log(`PASS: owner Ctrl+N; asset edit/status/trash/audit/maintenance/transfer/assignment; expired-session dialog closure, deliberate re-login, and no stale-write replay; company JSON restore preview and confirmations; company contract list/search/filter/detail/create/cancel/archive/restore with CSRF; all ${visitedDestinations.size} sidebar destinations render; ${placeholderDestinations.length} open the explicit feature/module status surface (${placeholderDestinations.join(', ')}); browser error-free completion.`);
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
