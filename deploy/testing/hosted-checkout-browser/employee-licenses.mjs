import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const assignmentId = '33333333-3333-4333-8333-333333333333';
const attachmentId = '44444444-4444-4444-8444-444444444444';
const csrfToken = 'synthetic-employee-license-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const records = [];
let accepted = false;
const license = {
  assignmentId, licenseId: '55555555-5555-4555-8555-555555555555', name: 'Synthetic Employee License',
  publisher: 'Synthetic Publisher', productVersion: '4.2', assignedAt: '2026-10-15T12:00:00Z', accepted,
  eulaAvailable: true, eulaAttachmentId: attachmentId, eulaVersion: '2.1',
  eulaSha256: 'a'.repeat(64), eulaUrl: `/api/session/software-licenses/${assignmentId}/eula`
};
const json = (response, status, body) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' }); response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': url.pathname.endsWith('.woff2') ? 'font/woff2' : 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/session/software-licenses') {
    json(response, 200, { linked: true, employeeName: 'Synthetic Employee', items: [{ ...license, accepted }] }); return;
  }
  if (request.method === 'GET' && url.pathname === `/api/session/software-licenses/${assignmentId}/eula`) {
    response.writeHead(200, { 'content-type': 'application/pdf', 'cache-control': 'no-store', pragma: 'no-cache', 'x-content-type-options': 'nosniff' });
    response.end('%PDF-1.4 synthetic EULA'); return;
  }
  if (request.method === 'POST' && url.pathname === `/api/session/software-licenses/${assignmentId}/eula/accept`) {
    let bodyText = ''; for await (const chunk of request) bodyText += chunk;
    const entry = { body: JSON.parse(bodyText), csrf: request.headers['x-csrf-token'] }; records.push(entry);
    if (entry.csrf !== csrfToken) { json(response, 400, { message: 'CSRF required' }); return; }
    if (entry.body.eulaAttachmentId !== attachmentId || entry.body.confirmed !== true) { json(response, 400, { message: 'Current document confirmation required' }); return; }
    accepted = true; json(response, 200, { accepted: true, assignmentId, eulaAttachmentId: attachmentId, eulaVersion: '2.1' }); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }
  if (request.method === 'GET') {
    if (url.pathname === '/api/status') return json(response, 200, { status: 'ready', migrationCount: 94, pendingMigrationCount: 0 });
    if (url.pathname === '/api/setup/status') return json(response, 200, { setupRequired: false });
    if (url.pathname === '/api/session/csrf') return json(response, 200, { requestToken: csrfToken });
    if (url.pathname === '/api/session') return json(response, 200, { companyId, companyName: 'Synthetic License Company', userId,
      name: 'Synthetic Employee', role: 'viewer', hasPhoto: false, availableCompanies: [{ id: companyId, name: 'Synthetic License Company', role: 'viewer' }],
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic plan' } });
    if (url.pathname === '/api/company/branding') return json(response, 200, { name: 'Synthetic License Company', brandPrimaryColor: '#2563EB', hasLogo: false });
    return json(response, 200, { items: [], total: 0, page: 1, pageSize: 50 });
  }
  json(response, 404, { message: 'Not found' });
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address(); assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await context.newPage();
const pageErrors = []; page.on('pageerror', error => pageErrors.push(error.message));
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="software"]').click();
  await page.locator('#sidebar [data-page="Lisanslarım"]').click();
  await page.locator('#myLicensesPage').waitFor({ state: 'visible' });
  const card = page.locator('#myLicensesRows .my-license-card');
  await card.getByText('Synthetic Employee License').waitFor();
  const accept = card.getByRole('button', { name: 'Kabul et' });
  assert.equal(await accept.isDisabled(), true, 'Acceptance starts disabled until both document-open and explicit consent actions occur.');
  await card.locator('input[type="checkbox"]').check();
  assert.equal(await accept.isDisabled(), true, 'Checking consent alone must not enable acceptance before opening the EULA.');
  const popupPromise = page.waitForEvent('popup');
  const documentResponsePromise = context.waitForEvent('response', response => {
    try { return new URL(response.url()).pathname === `/api/session/software-licenses/${assignmentId}/eula`; }
    catch { return false; }
  });
  await card.getByRole('link', { name: 'EULA belgesini aç' }).click();
  const popup = await popupPromise;
  const documentResponse = await documentResponsePromise;
  assert.equal(documentResponse.status(), 200, 'The scoped EULA link must return its document successfully.');
  assert.equal(new URL(documentResponse.url()).pathname, `/api/session/software-licenses/${assignmentId}/eula`);
  await popup.close();
  await accept.waitFor({ state: 'visible' });
  assert.equal(await accept.isDisabled(), false, 'Opening the scoped EULA and checking consent enables acceptance.');
  await accept.click();
  await card.getByText('Kabul edildi').waitFor();
  assert.equal(records.length, 1, 'Exactly one acceptance request must be sent.');
  assert.equal(records[0].csrf, csrfToken); assert.deepEqual(records[0].body, { eulaAttachmentId: attachmentId, confirmed: true });
  assert.equal(await card.getByRole('button', { name: 'Kabul et' }).count(), 0, 'An accepted current EULA must not offer duplicate acceptance.');
  assert.deepEqual(pageErrors, [], `Employee license screen must not throw browser errors: ${pageErrors.join(' | ')}`);
  console.log('PASS: employee license Chromium flow opens the scoped EULA, requires explicit consent, posts the current attachment with CSRF, refreshes accepted state, and prevents duplicate acceptance (synthetic API).');
} finally { await browser.close(); await new Promise(resolve => server.close(resolve)); }
