import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const csrfToken = 'synthetic-return-queue-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const today = new Date(); today.setUTCHours(0, 0, 0, 0);
const day = offset => new Date(today.getTime() + offset * 86400000).toISOString().slice(0, 10);
const requests = [];
const externalRequests = [];
const openReturns = [
  { id: '77777777-7777-4777-8777-777777777701', assetTag: 'QA-RETURN-OVERDUE-1', name: 'First overdue asset', expectedReturnOn: day(-5), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  { id: '77777777-7777-4777-8777-777777777702', assetTag: 'QA-RETURN-OVERDUE-2', name: 'Second overdue asset', expectedReturnOn: day(-1), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  { id: '77777777-7777-4777-8777-777777777703', assetTag: 'QA-RETURN-DUE-TODAY', name: 'Today <img src=x onerror=alert(1)>', expectedReturnOn: day(0), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  { id: '77777777-7777-4777-8777-777777777704', assetTag: 'QA-RETURN-OLDER', name: 'Delayed search result', expectedReturnOn: day(7), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  { id: '77777777-7777-4777-8777-777777777705', assetTag: 'QA-RETURN-NEWER', name: 'Newest search result', expectedReturnOn: day(10), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  ...Array.from({ length: 51 }, (_, index) => ({
    id: `88888888-8888-4888-8888-${String(index + 1).padStart(12, '0')}`,
    assetTag: `QA-RETURN-PAGED-${String(index + 1).padStart(2, '0')}`,
    name: `Paged return ${index + 1}`,
    expectedReturnOn: day(11 + index % 20), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model'
  })),
  { id: '99999999-9999-4999-8999-999999999991', assetTag: 'QA-RETURN-UNDATED', name: 'Missing return date', expectedReturnOn: null, employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' },
  { id: '99999999-9999-4999-8999-999999999992', assetTag: 'QA-RETURN-LATER', name: 'Return beyond 30 days', expectedReturnOn: day(45), employeeName: 'Sentetik Çalışan', employeeNumber: 'QA-001', department: 'Operasyon', location: 'İstanbul', model: 'QA Model' }
];
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  const send = (status, value) => {
    response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(value === undefined ? '' : JSON.stringify(value));
  };
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' }); response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { send(404, { message: 'Not found' }); return; }

  const record = { method: request.method, path: url.pathname, search: url.search, csrf: request.headers['x-csrf-token'] };
  if (request.method !== 'GET') {
    let raw = ''; for await (const chunk of request) raw += chunk;
    record.body = raw ? JSON.parse(raw) : null;
  }
  requests.push(record);
  if (request.method !== 'GET') {
    if (record.csrf !== csrfToken) { send(400, { message: 'İstek doğrulaması başarısız.' }); return; }
    const returnMatch = url.pathname.match(/^\/api\/assets\/([0-9a-f-]+)\/return$/i);
    if (request.method === 'POST' && returnMatch) {
      const asset = openReturns.find(item => item.id === returnMatch[1]);
      if (!asset) { send(404, { message: 'Varlık bulunamadı.' }); return; }
      openReturns.splice(openReturns.indexOf(asset), 1);
      send(200, { id: asset.id, status: 'Available', returnedAt: new Date().toISOString(), note: record.body?.note ?? null }); return;
    }
    send(405, { message: 'Unsupported synthetic write.' }); return;
  }

  if (url.pathname === '/api/session/csrf') { send(200, { requestToken: csrfToken }); return; }
  if (url.pathname === '/api/session') {
    send(200, { companyId, companyName: 'Sentetik İade Şirketi', userId, name: 'Sentetik Yönetici', role: 'admin', hasPhoto: false,
      subscriptionAccess: { mode: 'full', status: 'Active', planName: 'Sentetik plan' } }); return;
  }
  if (url.pathname === '/api/status') { send(200, { status: 'ready', migrationCount: 102, pendingMigrationCount: 0 }); return; }
  if (url.pathname === '/api/setup/status') { send(200, { setupRequired: false }); return; }
  if (url.pathname === '/api/company/branding') { send(200, { name: 'Sentetik İade Şirketi', brandPrimaryColor: '#2563EB', hasLogo: false }); return; }
  if (url.pathname === '/api/company/label-settings') { send(200, { isConfigured: false, settings: null }); return; }
  if (url.pathname === '/api/assets/custom-fields') { send(200, []); return; }
  if (url.pathname === '/api/appearance/preferences') { send(200, { theme: 'system', language: 'tr' }); return; }
  if (url.pathname === '/api/assets/checkouts') {
    const state = url.searchParams.get('state') || 'all';
    const query = (url.searchParams.get('q') || '').trim().toLowerCase();
    const page = Math.max(1, Number(url.searchParams.get('page') || 1));
    const pageSize = Math.min(200, Math.max(1, Number(url.searchParams.get('pageSize') || 50)));
    if (query === 'qa-return-older') await wait(500);
    if (query === 'qa-return-newer') await wait(20);
    const filtered = openReturns.filter(item => {
      const overdue = item.expectedReturnOn && item.expectedReturnOn < day(0);
      const due30 = item.expectedReturnOn && item.expectedReturnOn >= day(0) && item.expectedReturnOn <= day(30);
      const later = item.expectedReturnOn && item.expectedReturnOn > day(30);
      const stateMatches = state === 'all' || state === 'overdue' && overdue || state === 'due30' && due30 || state === 'undated' && !item.expectedReturnOn || state === 'later' && later;
      const queryMatches = !query || `${item.assetTag} ${item.name} ${item.employeeName} ${item.employeeNumber} ${item.department} ${item.location} ${item.model}`.toLowerCase().includes(query);
      return stateMatches && queryMatches;
    });
    filtered.sort((a, b) => (a.expectedReturnOn || '9999-12-31').localeCompare(b.expectedReturnOn || '9999-12-31') ||
      a.employeeName.localeCompare(b.employeeName) || a.assetTag.localeCompare(b.assetTag));
    const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
    const currentPage = Math.min(page, pageCount);
    send(200, { items: filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize), total: filtered.length, page: currentPage, pageSize, pageCount, asOf: day(0) }); return;
  }
  send(200, { items: [], total: 0, page: 1, pageSize: 50, pageCount: 1 });
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
page.setDefaultTimeout(10_000);
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));
page.on('request', request => { if (new URL(request.url()).origin !== `http://127.0.0.1:${address.port}`) externalRequests.push(request.url()); });

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => !document.body.classList.contains('auth-required'));
  const waitForAssetTag = tag => page.locator('#returnsRows').getByText(tag, { exact: true }).first().waitFor();
  await page.locator('#sidebar [data-group="movement"]').click();
  await page.locator('#sidebar [data-page="İadeler"]').click();
  await page.locator('#returnsPage').waitFor({ state: 'visible' });
  await waitForAssetTag('QA-RETURN-OVERDUE-1');
  assert.equal(await page.locator('#returnsRows tr').count(), 2, 'The default queue must contain only overdue assignments.');
  assert.equal(await page.locator('#returnsCount').innerText(), '2 zimmet');

  await page.locator('#returnsState').selectOption('due30');
  await waitForAssetTag('QA-RETURN-DUE-TODAY');
  assert.equal(await page.locator('#returnsCount').innerText(), '54 zimmet', 'Today and upcoming returns through day 30 belong in the due queue.');
  assert.equal(await page.locator('#returnsRows img').count(), 0, 'Asset text must not execute markup supplied in a name.');
  await page.locator('#returnsState').selectOption('undated');
  await waitForAssetTag('QA-RETURN-UNDATED');
  assert.equal(await page.locator('#returnsRows tr').count(), 1);
  await page.locator('#returnsState').selectOption('later');
  await waitForAssetTag('QA-RETURN-LATER');
  assert.equal(await page.locator('#returnsRows tr').count(), 1);

  await page.locator('#returnsState').selectOption('all');
  await waitForAssetTag('QA-RETURN-PAGED-50');
  assert.equal(await page.locator('#returnsCount').innerText(), '58 zimmet');
  assert.equal(await page.locator('#returnsPageInfo').innerText(), '58 kayıt · Sayfa 1 / 2');
  const pageTwo = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets/checkouts' && url.searchParams.get('page') === '2' && url.searchParams.get('state') === 'all';
  });
  await page.locator('#returnsNext').click();
  await pageTwo;
  await waitForAssetTag('QA-RETURN-LATER');
  assert.equal(await page.locator('#returnsRows tr').count(), 8);
  assert.equal(await page.locator('#returnsPageInfo').innerText(), '58 kayıt · Sayfa 2 / 2');
  assert.equal(await page.locator('#returnsNext').isDisabled(), true);
  await page.locator('#returnsPrev').click();
  await waitForAssetTag('QA-RETURN-PAGED-50');
  assert.equal(await page.locator('#returnsPageInfo').innerText(), '58 kayıt · Sayfa 1 / 2');

  const olderRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname === '/api/assets/checkouts' && url.searchParams.get('q') === 'QA-RETURN-OLDER';
  });
  await page.locator('#returnsSearch').fill('QA-RETURN-OLDER');
  await olderRequest;
  const newerResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets/checkouts' && url.searchParams.get('q') === 'QA-RETURN-NEWER';
  });
  await page.locator('#returnsSearch').fill('QA-RETURN-NEWER');
  await newerResponse;
  await waitForAssetTag('QA-RETURN-NEWER');
  await wait(600);
  assert.equal(await page.locator('#returnsRows tr').count(), 1, 'A delayed search response must not replace the newest filter result.');
  assert.equal(await page.locator('#returnsRows').getByText('QA-RETURN-OLDER').count(), 0);

  const returnAction = page.locator('#returnsRows button').filter({ hasText: 'İade al' }).first();
  await returnAction.click();
  await page.locator('#returnAssetDialog').waitFor({ state: 'visible' });
  await page.keyboard.press('Escape');
  await page.locator('#returnAssetDialog').waitFor({ state: 'hidden' });
  assert.equal(requests.filter(item => item.method === 'POST').length, 0, 'Canceling the return must not write.');
  await page.locator('#returnsRows button').filter({ hasText: 'İade al' }).click();
  await page.locator('#returnAssetForm textarea[name="note"]').fill('Sentetik iade kabul notu');
  const savedReturn = page.waitForResponse(response => response.request().method() === 'POST' && new URL(response.url()).pathname === '/api/assets/77777777-7777-4777-8777-777777777705/return');
  await page.locator('#submitAssetReturn').click();
  const returned = await savedReturn;
  assert.equal(returned.status(), 200);
  await page.locator('#returnsRows').getByText('Filtrelerle eşleşen zimmet bulunamadı.').waitFor();
  assert.equal(requests.filter(item => item.method === 'POST').length, 1);
  const write = requests.find(item => item.method === 'POST');
  assert.equal(write.csrf, csrfToken);
  assert.equal(write.body.note, 'Sentetik iade kabul notu');
  assert.deepEqual(pageErrors, []);
  assert.deepEqual(externalRequests, []);
  console.log('PASS: returns queue separates overdue/due/undated/later states, searches and paginates safely, ignores stale responses, and performs cancel-safe CSRF returns that refresh the queue.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
