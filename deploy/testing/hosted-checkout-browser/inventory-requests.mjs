import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const csrfToken = 'synthetic-request-catalog-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const requests = [];
const catalogItems = [
  { id: '33333333-3333-4333-8333-333333333333', type: 'Asset', name: 'D18 synthetic asset', category: 'IT', modelNumber: 'ASSET-01', location: 'D18 Site A', available: 1 },
  { id: '44444444-4444-4444-8444-444444444444', type: 'Accessory', name: 'D18 synthetic accessory', category: 'Peripheral', modelNumber: 'ACC-01', location: 'D18 Site A', available: 2 },
  { id: '55555555-5555-4555-8555-555555555555', type: 'Consumable', name: 'D18 synthetic consumable', category: 'Office', modelNumber: 'STK-01', location: 'D18 Site A', available: 3 }
];
const json = (response, status, body) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': url.pathname.endsWith('.woff2') ? 'font/woff2' : 'text/css; charset=utf-8' });
    response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }
  requests.push({ method: request.method, path: url.pathname });
  if (request.method === 'GET') {
    if (url.pathname === '/api/status') return json(response, 200, { status: 'ready', migrationCount: 96, pendingMigrationCount: 0 });
    if (url.pathname === '/api/setup/status') return json(response, 200, { setupRequired: false });
    if (url.pathname === '/api/session/csrf') return json(response, 200, { requestToken: csrfToken });
    if (url.pathname === '/api/session') return json(response, 200, {
      companyId, companyName: 'D18 Synthetic Company', userId, name: 'D18 Synthetic Admin', role: 'admin', hasPhoto: false,
      availableCompanies: [{ id: companyId, name: 'D18 Synthetic Company', role: 'admin' }],
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic plan' }
    });
    if (url.pathname === '/api/company/branding') return json(response, 200, { name: 'D18 Synthetic Company', brandPrimaryColor: '#2563EB', hasLogo: false });
    if (url.pathname === '/api/requests/catalog') return json(response, 200, { items: catalogItems });
    if (url.pathname === '/api/requests') return json(response, 200, { items: [], total: 0, page: 1, pageSize: 25 });
    if (url.pathname === '/api/requests/employees') return json(response, 200, { items: [], selfEmployee: null });
    return json(response, 200, { items: [], total: 0, page: 1, pageSize: 50, summary: {} });
  }
  response.writeHead(405); response.end();
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address(); assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = []; page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="supplies"]').click();
  await page.locator('#sidebar [data-page="Stok talepleri"]').click();
  await page.locator('#inventoryRequestsPage').waitFor({ state: 'visible' });
  const rows = page.locator('#requestCatalogRows tr');
  await rows.nth(2).getByText('D18 Site A').waitFor();
  assert.equal(await rows.count(), 3, 'The request catalog must render each allowed synthetic item.');
  for (const name of ['D18 synthetic asset', 'D18 synthetic accessory', 'D18 synthetic consumable']) {
    const row = rows.filter({ hasText: name });
    assert.equal(await row.locator('td').nth(2).textContent(), 'D18 Site A');
  }
  assert.ok(requests.some(item => item.method === 'GET' && item.path === '/api/requests/catalog'), 'The page must load the scoped request catalog API.');
  assert.deepEqual(pageErrors, [], 'The inventory request page must not throw browser JavaScript exceptions.');
  console.log('PASS: real Chromium renders the request catalog location returned by the scoped API for assets, accessories, and consumables.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
