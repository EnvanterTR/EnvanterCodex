import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const syntheticCompanyId = '11111111-1111-4111-8111-111111111111';
const syntheticUserId = '22222222-2222-4222-8222-222222222222';
const appHtml = await readFile('/checkout-site/company-app.html');
const apiRequests = [];
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    response.end(appHtml);
    return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' });
    response.end('');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end('window.qrcodegen = window.qrcodegen || {};');
    return;
  }
  if (url.pathname.startsWith('/api/')) {
    apiRequests.push({ method: request.method, path: url.pathname });
    if (request.method !== 'GET') {
      response.writeHead(405, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ message: 'Read-only browser acceptance mock.' }));
      return;
    }
    const data = url.pathname === '/api/status'
      ? { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 }
      : url.pathname === '/api/setup/status'
        ? { setupRequired: false }
        : url.pathname === '/api/session/csrf'
          ? { requestToken: 'synthetic-viewer-csrf' }
          : url.pathname === '/api/session'
            ? {
              companyId: syntheticCompanyId,
              companyName: 'Synthetic Viewer Company',
              userId: syntheticUserId,
              name: 'Synthetic Viewer',
              role: 'viewer',
              hasPhoto: false,
              subscriptionAccess: { mode: 'full' }
            }
            : url.pathname === '/api/company/branding'
              ? { name: 'Synthetic Viewer Company', brandPrimaryColor: '#2563EB', hasLogo: false }
              : url.pathname === '/api/company/label-settings'
                ? { isConfigured: false, settings: null }
                : { items: [], total: 0, page: 1, pageSize: 50 };
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(JSON.stringify(data));
    return;
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
  response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));
page.on('console', message => { if (message.type() === 'error') console.error(`BROWSER ${message.text()}`); });

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  try {
    await page.waitForFunction(() => {
      const memberLink = document.querySelector('#membersNav');
      return memberLink?.hidden === true && !document.body.classList.contains('auth-required');
    }, null, { timeout: 15000 });
  } catch (error) {
    console.error(JSON.stringify({
      apiRequests,
      bodyClass: await page.locator('body').getAttribute('class'),
      memberHidden: await page.locator('#membersNav').evaluate(element => element.hidden),
      authError: await page.locator('#authError').innerText(),
      pageErrors
    }));
    throw error;
  }
  assert.ok(apiRequests.some(item => item.path === '/api/session' && item.method === 'GET'),
    'The app must initialize from the synthetic viewer session before checking its navigation.');

  await page.keyboard.press('Control+k');
  const dialog = page.locator('#commandPaletteDialog');
  await dialog.waitFor({ state: 'visible' });
  const search = page.locator('#commandPaletteSearch');
  await search.fill('Uygulama kullanıcıları');
  assert.equal(await page.locator('#commandPaletteResults .command-option').count(), 0,
    'The page must not expose company member administration to a viewer role through Ctrl+K.');
  assert.match(await page.locator('#commandPaletteResults [role="status"]').innerText(), /Eşleşen sayfa veya işlem bulunamadı/);

  await search.fill('Varlıklar');
  const assetDestination = page.locator('#commandPaletteResults .command-option').filter({ hasText: 'Varlıklar' }).first();
  await assetDestination.waitFor({ state: 'visible' });
  await assetDestination.click();
  await dialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#pageTitle')?.textContent === 'Varlık kayıtları');
  assert.deepEqual(pageErrors, [], 'The command palette and route navigation must not throw browser errors.');
  assert.equal(apiRequests.some(item => item.method !== 'GET'), false,
    'The browser acceptance may not modify company data.');
  console.log('PASS: real EnvanterTR UI initialized a synthetic viewer role; Ctrl+K omitted the hidden member destination, retained Varlıklar, and opened its route without writes or browser errors.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
