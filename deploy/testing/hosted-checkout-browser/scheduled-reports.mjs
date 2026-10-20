import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const scheduleId = '33333333-3333-4333-8333-333333333333';
const csrf = 'synthetic-scheduled-reports-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const scripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`, await readFile(`/checkout-site/${name}`, 'utf8')
])));
const requests = [];
let rows = [];
let nextVersion = 1;
let failNextWrite = false;
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  const send = (status, body) => { response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' }); response.end(JSON.stringify(body)); };
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' }); response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && scripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(scripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end(); return; }
  let body = null;
  if (!['GET', 'HEAD'].includes(request.method || 'GET')) {
    let raw = ''; for await (const chunk of request) raw += chunk;
    body = raw ? JSON.parse(raw) : null;
  }
  requests.push({ method: request.method, path: url.pathname, search: url.search, csrf: request.headers['x-csrf-token'], body });
  if (request.method !== 'GET' && request.headers['x-csrf-token'] !== csrf) { send(400, { message: 'CSRF required' }); return; }
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') { send(200, { requestToken: csrf }); return; }
  if (request.method === 'GET' && url.pathname === '/api/session') {
    send(200, { companyId, companyName: 'Sentetik Rapor Şirketi', userId, name: 'Sentetik Yönetici', role: 'admin', hasPhoto: false,
      subscriptionAccess: { mode: 'full', status: 'Active', planName: 'Sentetik plan' } }); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/status') { send(200, { status: 'ready', migrationCount: 117, pendingMigrationCount: 0 }); return; }
  if (request.method === 'GET' && url.pathname === '/api/setup/status') { send(200, { setupRequired: false }); return; }
  if (request.method === 'GET' && url.pathname === '/api/company/branding') { send(200, { name: 'Sentetik Rapor Şirketi', brandPrimaryColor: '#2563EB', hasLogo: false }); return; }
  if (request.method === 'GET' && url.pathname === '/api/appearance/preferences') { send(200, { theme: 'dark', language: 'tr' }); return; }
  if (request.method === 'GET' && url.pathname === '/api/reports/schedules') {
    send(200, { items: rows, emailAddress: 'verified@example.invalid', emailConfirmed: true, smtpConfigured: true, maintenanceEnabled: true }); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/reports/schedules') {
    if (failNextWrite) { failNextWrite = false; send(503, { message: 'Sentetik geçici hata' }); return; }
    const row = { id: scheduleId, ...body, version: nextVersion++, lastStatus: 'Idle', lastErrorCode: null,
      nextDueAt: '2026-10-21T06:00:00Z', lastSentAt: null, retryAfter: null };
    rows = [row]; send(201, row); return;
  }
  const match = url.pathname.match(/^\/api\/reports\/schedules\/([0-9a-f-]+)(?:\/(preview))?$/i);
  if (match && match[2] === 'preview' && request.method === 'GET') {
    const row = rows.find(item => item.id === match[1]); if (!row) { send(404, { message: 'Not found' }); return; }
    send(200, { id: row.id, name: row.name, language: row.language, generatedAt: '2026-10-20T10:00:00Z',
      headers: ['Varlık etiketi', 'Not'], rows: [['QA-001', '=1+1'], ['QA-002', 'Güncel'] ] }); return;
  }
  if (match && !match[2] && request.method === 'PUT') {
    if (failNextWrite) { failNextWrite = false; send(503, { message: 'Sentetik geçici hata' }); return; }
    const row = rows.find(item => item.id === match[1]); if (!row) { send(404, { message: 'Not found' }); return; }
    if (body.version !== row.version) { send(409, { message: 'stale' }); return; }
    Object.assign(row, body, { version: nextVersion++ }); send(200, row); return;
  }
  if (match && !match[2] && request.method === 'DELETE') {
    const row = rows.find(item => item.id === match[1]); if (!row) { send(404, { message: 'Not found' }); return; }
    if (url.searchParams.get('version') !== String(row.version)) { send(409, { message: 'stale' }); return; }
    rows = rows.filter(item => item.id !== match[1]); response.writeHead(204); response.end(); return;
  }
  send(200, { items: [], rows: [], entries: [], total: 0, page: 1, pageSize: 50, pageCount: 1 });
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1365, height: 1000 }, acceptDownloads: true });
page.setDefaultTimeout(10_000);
const errors = [];
page.on('pageerror', error => errors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => !document.body.classList.contains('auth-required'));
  await page.locator('#sidebar [data-group="reports"]').click();
  await page.locator('#sidebar [data-page="Hazır raporlar"]').click();
  await page.locator('#reportScheduleOpen').click();
  const dialog = page.locator('#scheduledReportsDialog');
  await dialog.waitFor({ state: 'visible' });
  await dialog.getByText('Henüz rapor takvimi yok.').waitFor();
  await dialog.locator('[data-new-schedule]').click();
  const form = dialog.locator('[data-schedule-form]');
  await form.locator('[name="name"]').fill('Günlük varlık özeti');
  await form.locator('[name="frequency"]').selectOption('daily');
  await form.locator('[name="time"]').fill('08:30');
  await form.locator('[name="timeZoneId"]').fill('Europe/Istanbul');
  await form.locator('[name="reportKind"]').selectOption('inventory');
  await form.locator('[name="language"]').selectOption('tr');
  await form.locator('[name="isEnabled"]').check();
  failNextWrite = true;
  await form.locator('[type="submit"]').click();
  await dialog.getByText('Sentetik geçici hata').waitFor();
  assert.equal(await form.locator('[name="name"]').inputValue(), 'Günlük varlık özeti', 'Failed save must retain entered form data.');
  assert.equal(rows.length, 0, 'Failed write must not create a schedule.');
  const post = requests.find(item => item.method === 'POST' && item.path === '/api/reports/schedules');
  assert.equal(post.csrf, csrf, 'Schedule creation must send CSRF.');
  await form.locator('[type="submit"]').click();
  await dialog.getByText('Rapor takvimi kaydedildi.').waitFor();
  await dialog.locator('[data-schedule-rows] tr').waitFor({ state: 'visible' });
  assert.match(await dialog.locator('[data-schedule-rows]').innerText(), /Günlük varlık özeti/, 'Saved schedule must appear in the refreshed list.');
  assert.equal(rows.length, 1);
  assert.equal(rows[0].frequency, 'daily');
  assert.equal(rows[0].localHour, 8);
  assert.equal(rows[0].localMinute, 30);
  assert.equal(rows[0].timeZoneId, 'Europe/Istanbul');

  const preview = dialog.locator('button[data-action="preview"]');
  await preview.click();
  await dialog.locator('[data-schedule-preview]:not([hidden])').waitFor();
  assert.equal(await dialog.locator('[data-preview-body] tr').count(), 2, 'Preview must render current report rows.');
  const [download] = await Promise.all([page.waitForEvent('download'), dialog.locator('[data-preview-csv]').click()]);
  const downloadPath = await download.path();
  const { readFile: readDownloadedFile } = await import('node:fs/promises');
  const csv = await readDownloadedFile(downloadPath, 'utf8');
  assert.match(csv, /'=1\+1/, 'CSV export must neutralize formula-like cells.');
  await dialog.locator('[data-preview-close]').click();

  await dialog.locator('button[data-action="edit"]').click();
  await form.locator('[name="name"]').fill('Haftalık envanter');
  await form.locator('[name="frequency"]').selectOption('weekly');
  await form.locator('[name="weekDay"]').selectOption('2');
  await form.locator('[type="submit"]').click();
  await dialog.getByText('Rapor takvimi kaydedildi.').waitFor();
  assert.equal(rows[0].name, 'Haftalık envanter');
  assert.equal(rows[0].frequency, 'weekly');
  assert.equal(rows[0].weekDay, 2);
  assert.ok(rows[0].version > 1, 'Edit must use and advance the concurrency version.');

  await dialog.locator('button[data-action="toggle"]').click();
  await dialog.getByText('Rapor takvimi güncellendi.').waitFor();
  assert.equal(rows[0].isEnabled, false, 'Toggle must persist the disabled state.');
  assert.equal(requests.filter(item => item.method === 'PUT').every(item => item.csrf === csrf), true, 'Every update must send CSRF.');

  await dialog.locator('button[data-action="ask-delete"]').click();
  await dialog.locator('button[data-action="cancel-delete"]').click();
  assert.equal(rows.length, 1, 'Canceling delete must not write.');
  assert.equal(requests.some(item => item.method === 'DELETE'), false, 'Delete confirmation must precede the request.');
  await dialog.locator('button[data-action="ask-delete"]').click();
  await dialog.locator('button[data-action="confirm-delete"]').click();
  await dialog.getByText('Rapor takvimi silindi.').waitFor();
  assert.equal(rows.length, 0);
  const deletion = requests.find(item => item.method === 'DELETE');
  assert.equal(deletion.csrf, csrf);
  assert.equal(deletion.search, '?version=3', 'Delete must send the last observed version.');
  assert.deepEqual(errors, [], 'Scheduled-report screens must not throw browser errors.');
  console.log('PASS: scheduled-report create/retry, preview, formula-safe CSV, edit, disable, confirmed delete, CSRF and optimistic concurrency work in Chromium.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
