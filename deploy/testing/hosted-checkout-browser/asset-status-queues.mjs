import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const csrfToken = 'synthetic-status-queue-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const states = [
  ['Available', 'Kullanılabilir'], ['Deployed', 'Atanmış'], ['Pending', 'Beklemede'],
  ['Maintenance', 'Bakımda'], ['Unusable', 'Kullanılamaz'], ['Archived', 'Arşiv']
];
const ownAssets = states.map(([status], index) => ({
  id: `77777777-7777-4777-8777-${String(index + 1).padStart(12, '0')}`, companyId,
  assetTag: `QA-STATUS-QUEUE-${status}`, name: `Status queue ${status}`, status,
  companyName: 'Sentetik Durum Şirketi', categoryId: null, categoryName: 'Test',
  assignedEmployeeId: status === 'Deployed' ? '33333333-3333-4333-8333-333333333333' : null,
  assignedEmployeeName: status === 'Deployed' ? 'Sentetik Çalışan' : null,
  employeeNumber: status === 'Deployed' ? 'QA-001' : null, isByod: false
}));
const foreignAsset = { ...ownAssets[0], id: '99999999-9999-4999-8999-999999999999', companyId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', assetTag: 'QA-STATUS-QUEUE-FOREIGN', name: 'Foreign tenant status queue record' };
const allAssets = [...ownAssets, foreignAsset];
const requests = [];
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  const send = (status, body) => { response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' }); response.end(JSON.stringify(body)); };
  if (request.method === 'GET' && url.pathname === '/') { response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' }); response.end(appHtml); return; }
  if (url.pathname.startsWith('/assets/fonts/')) { response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return; }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { send(404, { message: 'Not found' }); return; }
  requests.push({ method: request.method, path: url.pathname, search: url.search, csrf: request.headers['x-csrf-token'] });
  if (request.method !== 'GET') { send(405, { message: 'Status queue acceptance is read-only.' }); return; }
  if (url.pathname === '/api/session/csrf') { send(200, { requestToken: csrfToken }); return; }
  if (url.pathname === '/api/session') { send(200, { companyId, companyName: 'Sentetik Durum Şirketi', userId, name: 'Sentetik Yönetici', role: 'admin', hasPhoto: false, subscriptionAccess: { mode: 'full', status: 'Active', planName: 'Sentetik plan' } }); return; }
  if (url.pathname === '/api/status') { send(200, { status: 'ready', migrationCount: 102, pendingMigrationCount: 0 }); return; }
  if (url.pathname === '/api/setup/status') { send(200, { setupRequired: false }); return; }
  if (url.pathname === '/api/company/branding') { send(200, { name: 'Sentetik Durum Şirketi', brandPrimaryColor: '#2563EB', hasLogo: false }); return; }
  if (url.pathname === '/api/company/label-settings') { send(200, { isConfigured: false, settings: null }); return; }
  if (url.pathname === '/api/appearance/preferences') { send(200, { theme: 'system', language: 'tr' }); return; }
  if (url.pathname === '/api/assets/custom-fields') { send(200, []); return; }
  if (url.pathname === '/api/assets/status-labels') { send(200, states.map(([status, label]) => ({ status, label }))); return; }
  if (url.pathname === '/api/assets/status-config') { send(200, { version: 1, labels: {}, customStatuses: [], transitions: {} }); return; }
  if (url.pathname === '/api/lookups/categories') { send(200, []); return; }
  if (url.pathname === '/api/lookups/brands') { send(200, []); return; }
  if (url.pathname === '/api/lookups/models') { send(200, []); return; }
  if (url.pathname === '/api/locations') { send(200, { items: [] }); return; }
  if (url.pathname === '/api/dashboard/summary') { send(200, { totalAssets: 6, assignedAssets: 1, availableAssets: 1, pendingAssets: 1, maintenanceAssets: 1, unusableAssets: 1, archivedAssets: 1, softwareLicenses: 0, accessories: 0, consumables: 0, components: 0, activeEmployees: 1 }); return; }
  if (url.pathname === '/api/assets') {
    const status = url.searchParams.get('status') || '';
    const query = (url.searchParams.get('q') || '').toLowerCase();
    const page = Math.max(1, Number(url.searchParams.get('page') || 1));
    const pageSize = Math.max(1, Number(url.searchParams.get('pageSize') || 50));
    const filtered = allAssets.filter(asset => asset.companyId === companyId && (!status || asset.status === status) && (!query || `${asset.assetTag} ${asset.name}`.toLowerCase().includes(query)));
    const total = filtered.length;
    send(200, { items: filtered.slice((page - 1) * pageSize, page * pageSize), page, pageSize, total }); return;
  }
  send(200, { items: [], rows: [], entries: [], total: 0, page: 1, pageSize: 50, pageCount: 1 });
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
page.setDefaultTimeout(10_000);
const errors = [];
page.on('pageerror', error => errors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => !document.body.classList.contains('auth-required'));
  const dashboardPending = page.locator('#dashboardStatusLegend button[aria-label="Beklemede: 1 varlığı listele"]');
  await dashboardPending.waitFor({ state: 'visible' });
  const pendingResponse = page.waitForResponse(value => {
    const url = new URL(value.url());
    return url.pathname === '/api/assets' && url.searchParams.get('status') === 'Pending';
  });
  await dashboardPending.click();
  await pendingResponse;
  await page.locator('#assetListPanel').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assetStatusFilter').inputValue(), 'Pending');
  await page.locator('#assetRows').getByText('QA-STATUS-QUEUE-Pending', { exact: true }).waitFor();
  assert.equal(await page.locator('#assetRows tr[data-id]').count(), 1, 'Dashboard pending queue must open the matching company-scoped asset list.');

  await page.locator('#sidebar [data-page="Genel Bakış"]').click();
  const dashboardUnusable = page.locator('#dashboardStatusLegend button[aria-label="Kullanılamaz: 1 varlığı listele"]');
  await dashboardUnusable.waitFor({ state: 'visible' });
  const unusableResponse = page.waitForResponse(value => {
    const url = new URL(value.url());
    return url.pathname === '/api/assets' && url.searchParams.get('status') === 'Unusable';
  });
  await dashboardUnusable.click();
  await unusableResponse;
  await page.locator('#assetListPanel').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#assetStatusFilter').inputValue(), 'Unusable');
  await page.locator('#assetRows').getByText('QA-STATUS-QUEUE-Unusable', { exact: true }).waitFor();
  assert.equal(await page.locator('#assetRows tr[data-id]').count(), 1, 'Dashboard unusable queue must open the matching company-scoped asset list.');

  await page.locator('#sidebar [data-group="inventory"]').click();
  await page.locator('#sidebar .sub-item[data-page="Varlıklar"]').click();
  await page.locator('#assetListPanel').waitFor({ state: 'visible' });
  const statusFilter = page.locator('#assetStatusFilter');
  assert.deepEqual(await statusFilter.locator('option').evaluateAll(options => options.map(option => option.value)), ['', ...states.map(([status]) => status)],
    'The asset status selector must expose each of the six supported lifecycle queues exactly once.');
  for (const [status, label] of states) {
    const response = page.waitForResponse(value => {
      const url = new URL(value.url());
      return url.pathname === '/api/assets' && url.searchParams.get('status') === status;
    });
    await statusFilter.selectOption(status);
    await response;
    const row = page.locator('#assetRows tr[data-id]');
    await row.getByText(`QA-STATUS-QUEUE-${status}`, { exact: true }).waitFor();
    assert.equal(await row.count(), 1, `${label} must render only its matching company record.`);
    assert.equal(await row.innerText().then(text => text.includes(label)), true);
    assert.equal(await page.locator('#assetCount').innerText(), '1 kayıt');
    assert.equal(requests.some(item => item.method !== 'GET'), false, 'Filtering queues must remain read-only.');
  }
  await statusFilter.selectOption('');
  await page.locator('#assetRows').getByText('QA-STATUS-QUEUE-Archived', { exact: true }).waitFor();
  assert.equal(await page.locator('#assetRows tr[data-id]').count(), 6);
  assert.equal(await page.locator('#assetRows').getByText('QA-STATUS-QUEUE-FOREIGN', { exact: true }).count(), 0,
    'The synthetic other-tenant record must never render in the selected company list.');
  assert.deepEqual(errors, []);
  console.log('PASS: real Chromium asset list exposes all six built-in status queues, sends the selected filter, renders only matching company records, and keeps filtering read-only.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
