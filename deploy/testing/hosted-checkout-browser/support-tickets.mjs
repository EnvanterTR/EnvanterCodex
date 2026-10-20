import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const csrfToken = 'synthetic-support-csrf';
const ticketIds = [
  '33333333-3333-4333-8333-333333333333',
  '44444444-4444-4444-8444-444444444444'
];
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const requests = [];
const externalRequests = [];
const tickets = [];
const messages = new Map();
const createdAt = new Date('2026-10-15T12:00:00.000Z').toISOString();
const auditToday = new Date(); auditToday.setUTCHours(0, 0, 0, 0);
const auditDay = offset => new Date(auditToday.getTime() + offset * 86400000).toISOString().slice(0, 10);
const auditItems = [
  { id: '55555555-5555-4555-8555-555555555551', assetTag: 'QA-AUDIT-OVERDUE', name: 'Overdue synthetic asset', nextAuditOn: auditDay(-5), status: 'Available', location: 'QA depot', employeeName: null },
  { id: '55555555-5555-4555-8555-555555555552', assetTag: 'QA-AUDIT-UPCOMING', name: 'Upcoming <img src=x onerror=alert(1)>', nextAuditOn: auditDay(7), status: 'Deployed', location: 'QA office', employeeName: 'Synthetic employee' },
  { id: '55555555-5555-4555-8555-555555555553', assetTag: 'QA-AUDIT-LATER', name: 'Later synthetic asset', nextAuditOn: auditDay(45), status: 'Available', location: 'QA depot', employeeName: null },
  { id: '55555555-5555-4555-8555-555555555554', assetTag: 'QA-AUDIT-UNSCHEDULED', name: 'Unscheduled synthetic asset', nextAuditOn: null, status: 'Available', location: 'QA depot', employeeName: null },
  ...Array.from({ length: 52 }, (_, index) => ({
    id: `66666666-6666-4666-8666-${String(index + 1).padStart(12, '0')}`,
    assetTag: `QA-AUDIT-PAGED-${String(index + 1).padStart(2, '0')}`,
    name: `Paged synthetic audit asset ${index + 1}`,
    nextAuditOn: auditDay(35 + index), status: 'Available', location: 'QA depot', employeeName: null
  }))
];
let failNextAuditAssetLoad = false;
let sessionRole = 'admin';
const completedAudits = Array.from({ length: 52 }, (_, index) => ({
  id: auditItems[0].id, auditEventId: `completed-${index}`, assetTag: `QA-COMPLETED-${String(index + 1).padStart(3, '0')}`,
  name: `Completed audit ${index + 1}`, lastAuditedAt: `${auditDay(-1)}T12:00:00Z`,
  outcome: index % 2 ? 'Passed' : 'Unacceptable',
  details: index === 0 ? 'audit-finding-marker <img src=x onerror=alert(1)>' : null,
  location: 'QA depot', employeeName: null
}));
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
    if (request.method === 'POST' && url.pathname === '/api/support/tickets') {
      const id = ticketIds[tickets.length];
      const ticket = { id, subject: String(record.body?.subject || '').trim(), category: record.body?.category || 'Other', status: 'Open', createdAt, updatedAt: createdAt, createdBy: 'Sentetik Yönetici', messageCount: 1 };
      tickets.unshift(ticket); messages.set(id, [{ id: `message-${tickets.length}-1`, body: String(record.body?.message || '').trim(), isSupportReply: false, author: 'Sentetik Yönetici', createdAt }]);
      send(201, { id, subject: ticket.subject, category: ticket.category, status: ticket.status, createdAt, updatedAt: createdAt }); return;
    }
    const replyMatch = url.pathname.match(/^\/api\/support\/tickets\/([0-9a-f-]+)\/messages$/i);
    if (request.method === 'POST' && replyMatch) {
      const ticket = tickets.find(item => item.id === replyMatch[1]);
      if (!ticket || ticket.status === 'Closed') { send(ticket ? 409 : 404, { message: 'Talep yanıtlanamıyor.' }); return; }
      const message = { id: `message-${ticket.id}-reply`, body: String(record.body?.body || '').trim(), isSupportReply: false, author: 'Sentetik Yönetici', createdAt: new Date(Date.now() + 1000).toISOString() };
      messages.get(ticket.id).push(message); ticket.messageCount++; ticket.updatedAt = message.createdAt;
      send(201, { id: message.id, body: message.body, isSupportReply: false, createdAt: message.createdAt }); return;
    }
    const closeMatch = url.pathname.match(/^\/api\/support\/tickets\/([0-9a-f-]+)\/close$/i);
    if (request.method === 'POST' && closeMatch) {
      const ticket = tickets.find(item => item.id === closeMatch[1]);
      if (!ticket) { send(404, { message: 'Talep bulunamadı.' }); return; }
      ticket.status = 'Closed'; ticket.updatedAt = new Date(Date.now() + 2000).toISOString(); send(200, { id: ticket.id, status: ticket.status, updatedAt: ticket.updatedAt }); return;
    }
    send(405, { message: 'Unsupported synthetic write.' }); return;
  }

  if (url.pathname === '/api/session/csrf') { send(200, { requestToken: csrfToken }); return; }
  if (url.pathname === '/api/session') {
    send(200, { companyId, companyName: 'Sentetik Destek Şirketi', userId, name: 'Sentetik Yönetici', role: sessionRole, hasPhoto: false,
      subscriptionAccess: { mode: 'full', status: 'Active', planName: 'Sentetik plan' } }); return;
  }
  if (url.pathname === '/api/status') { send(200, { status: 'ready', migrationCount: 93, pendingMigrationCount: 0 }); return; }
  if (url.pathname === '/api/setup/status') { send(200, { setupRequired: false }); return; }
  if (url.pathname === '/api/company/branding') { send(200, { name: 'Sentetik Destek Şirketi', brandPrimaryColor: '#2563EB', hasLogo: false }); return; }
  if (url.pathname === '/api/company/label-settings') { send(200, { isConfigured: false, settings: null }); return; }
  if (url.pathname === '/api/assets/custom-fields') { send(200, []); return; }
  if (url.pathname === '/api/appearance/preferences') { send(200, { theme: 'system', language: 'tr' }); return; }

  if (url.pathname === '/api/support/tickets') {
    const query = url.searchParams.get('q') || '';
    if (query === 'older') await wait(500);
    if (query === 'newer') await wait(20);
    const status = url.searchParams.get('status');
    const filtered = tickets.filter(ticket => (!status || ticket.status === status) && (!query ||
      (query === 'older' ? ticket.id === ticketIds[0] : query === 'newer' ? ticket.id === ticketIds[1] : ticket.subject.toLowerCase().includes(query.toLowerCase()))));
    send(200, { items: filtered.map(ticket => ({ ...ticket })), total: filtered.length, page: Number(url.searchParams.get('page') || 1), pageSize: 25, canViewAll: true }); return;
  }
  if (url.pathname === '/api/assets/audit-schedule') {
    const state = url.searchParams.get('state') || 'all';
    const through = url.searchParams.get('through') || auditDay(30);
    const page = Math.max(1, Number(url.searchParams.get('page') || 1));
    const pageSize = Math.min(100, Math.max(1, Number(url.searchParams.get('pageSize') || 50)));
    const query = (url.searchParams.get('q') || '').trim().toLowerCase();
    if (state === 'completed') {
      const since = url.searchParams.get('since');
      assert.equal(through, auditDay(0), 'Completed queue must end today.');
      assert.ok(since && since < through, 'Completed queue must send a retrospective interval.');
      const filtered = completedAudits.filter(item => !query || `${item.assetTag} ${item.name} ${item.details || ''}`.toLowerCase().includes(query));
      send(200, { today: auditDay(0), through, since, completed: filtered.length, total: filtered.length, page, pageSize,
        items: filtered.slice((page - 1) * pageSize, page * pageSize) }); return;
    }
    const overdue = auditItems.filter(item => item.nextAuditOn && item.nextAuditOn < auditDay(0));
    const dueSoon = auditItems.filter(item => item.nextAuditOn && item.nextAuditOn >= auditDay(0) && item.nextAuditOn <= through);
    const unscheduled = auditItems.filter(item => !item.nextAuditOn);
    let filtered = state === 'overdue' ? overdue : state === 'upcoming' ? dueSoon : state === 'unscheduled' ? unscheduled : auditItems.filter(item => item.nextAuditOn && item.nextAuditOn <= through);
    if (query) filtered = filtered.filter(item => `${item.assetTag} ${item.name} ${item.location} ${item.employeeName || ''}`.toLowerCase().includes(query));
    requests.push({ method: 'GET', path: url.pathname, search: url.search, state, through, page, pageSize, query });
    send(200, { today: auditDay(0), through, overdue: overdue.length, dueSoon: dueSoon.length, unscheduled: unscheduled.length, total: filtered.length, page, pageSize,
      items: filtered.slice((page - 1) * pageSize, page * pageSize) }); return;
  }
  const auditAsset = auditItems.find(item => url.pathname === `/api/assets/${item.id}`);
  if (auditAsset) {
    if (failNextAuditAssetLoad) { failNextAuditAssetLoad = false; send(503, { message: 'Synthetic asset load failure' }); return; }
    send(200, { asset: { ...auditAsset, companyId, updatedAt: createdAt, currency: 'TRY' }, references: {}, history: [], attachments: [] }); return;
  }
  const detailMatch = url.pathname.match(/^\/api\/support\/tickets\/([0-9a-f-]+)$/i);
  if (request.method === 'GET' && detailMatch) {
    const id = detailMatch[1];
    if (id === ticketIds[0]) await wait(500);
    if (id === ticketIds[1]) await wait(20);
    const ticket = tickets.find(item => item.id === id);
    if (!ticket) { send(404, { message: 'Talep bulunamadı.' }); return; }
    send(200, { ticket: { id: ticket.id, subject: ticket.subject, category: ticket.category, status: ticket.status, createdAt: ticket.createdAt, updatedAt: ticket.updatedAt }, messages: messages.get(id), canReply: ticket.status !== 'Closed' }); return;
  }
  send(200, { items: [], total: 0, page: 1, pageSize: 50 });
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
  await page.locator('#sidebar [data-page="Destek"]').click();
  const supportPage = page.locator('#supportPage');
  await supportPage.waitFor({ state: 'visible' });
  await page.locator('#supportRows').getByText('Filtreye uyan destek talebi yok.').waitFor();
  assert.equal(await page.locator('#supportNewButton').isVisible(), true);
  assert.equal(await page.locator('#supportForm [name="subject"]').getAttribute('required'), '');
  assert.equal(await page.locator('#supportForm [name="message"]').getAttribute('required'), '');

  await page.locator('#supportNewButton').click();
  await page.locator('#supportForm [name="subject"]').fill('Older support issue');
  await page.locator('#supportForm [name="category"]').selectOption('Data');
  await page.locator('#supportForm [name="message"]').fill('Initial customer description');
  await page.locator('#supportSaveButton').click();
  const firstId = ticketIds[0];
  await page.locator(`#supportRows [data-support-open="${firstId}"]`).waitFor();
  await page.locator('#supportDetailTitle').getByText('Older support issue').waitFor();
  assert.equal(await page.locator('#supportDetailPanel').isVisible(), true);
  assert.equal(await page.locator('#supportMessages').getByText('Initial customer description').count(), 1);

  await page.locator('#supportReplyForm textarea[name="body"]').fill('Additional information for support');
  await page.locator('#supportReplyButton').click();
  await page.locator('#supportMessages').getByText('Additional information for support').waitFor();
  assert.equal(await page.locator('#supportRows tr').first().locator('td').nth(5).innerText(), '2');

  await page.locator('#supportNewButton').click();
  await page.locator('#supportForm [name="subject"]').fill('Newer support issue');
  await page.locator('#supportForm [name="category"]').selectOption('Access');
  await page.locator('#supportForm [name="message"]').fill('Second customer description');
  await page.locator('#supportSaveButton').click();
  const secondId = ticketIds[1];
  await page.locator(`#supportRows [data-support-open="${secondId}"]`).waitFor();

  const olderSearch = page.waitForResponse(response => new URL(response.url()).pathname === '/api/support/tickets' && new URL(response.url()).searchParams.get('q') === 'older');
  await page.locator('#supportSearch').fill('older');
  await olderSearch;
  const newerSearch = page.waitForResponse(response => new URL(response.url()).pathname === '/api/support/tickets' && new URL(response.url()).searchParams.get('q') === 'newer');
  await page.locator('#supportSearch').fill('newer');
  await newerSearch;
  await page.locator('#supportRows').getByText('Newer support issue').waitFor();
  await wait(550);
  assert.equal(await page.locator('#supportRows tr').count(), 1,
    'A delayed older search result must not replace the newer filtered support list.');
  assert.equal(await page.locator('#supportRows tr').first().locator('td').first().innerText(), 'Newer support issue');

  await page.locator('#supportSearch').fill('');
  await page.locator(`#supportRows [data-support-open="${firstId}"]`).waitFor();
  const firstDetail = page.waitForResponse(response => new URL(response.url()).pathname === `/api/support/tickets/${firstId}`);
  await page.locator(`#supportRows [data-support-open="${firstId}"]`).click();
  await firstDetail;
  const secondDetail = page.waitForResponse(response => new URL(response.url()).pathname === `/api/support/tickets/${secondId}`);
  await page.locator(`#supportRows [data-support-open="${secondId}"]`).click();
  await secondDetail;
  await page.locator('#supportDetailTitle').getByText('Newer support issue').waitFor();
  await wait(550);
  assert.equal(await page.locator('#supportDetailTitle').innerText(), 'Newer support issue',
    'A delayed detail response for another ticket must not replace the selected ticket.');
  assert.equal(await page.locator('#supportMessages').getByText('Second customer description').count(), 1);

  const detailBeforeLeave = page.waitForResponse(response => new URL(response.url()).pathname === `/api/support/tickets/${firstId}`);
  await page.locator(`#supportRows [data-support-open="${firstId}"]`).click();
  await detailBeforeLeave;
  await page.locator('#sidebar [data-page="Genel Bakış"]').click();
  await page.locator('#overview').waitFor({ state: 'visible' });
  await page.locator('#sidebar [data-page="Destek"]').click();
  await supportPage.waitFor({ state: 'visible' });
  await page.locator(`#supportRows [data-support-open="${secondId}"]`).waitFor();
  await wait(550);
  assert.equal(await page.locator('#supportDetailPanel').isHidden(), true,
    'A pending ticket detail must not reopen after navigating away and back to the support page.');

  await page.locator(`#supportRows [data-support-open="${secondId}"]`).click();
  await page.locator('#supportDetailTitle').getByText('Newer support issue').waitFor();
  await page.locator('#supportCloseTicket').click();
  await page.locator('#supportDetailMeta').getByText('Kapatıldı').waitFor();
  assert.equal(await page.locator('#supportReplyForm').isHidden(), true, 'Closed tickets must not accept another customer reply.');
  await page.locator('#supportStatusFilter').selectOption('Closed');
  await page.locator(`#supportRows [data-support-open="${secondId}"]`).waitFor();
  assert.equal(await page.locator('#supportRows tr').count(), 1);
  await page.locator('#supportSearch').fill('no matching ticket');
  await page.locator('#supportRows').getByText('Filtreye uyan destek talebi yok.').waitFor();
  await page.locator('#supportRefresh').click();
  await page.locator('#supportRows').getByText('Filtreye uyan destek talebi yok.').waitFor();

  await page.locator('#sidebar [data-group="operations"]').click();
  await page.locator('#sidebar [data-page="Denetimler"]').click();
  await page.locator('#auditPage').waitFor({ state: 'visible' });
  await wait(350);
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-OVERDUE').waitFor();
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-UPCOMING').waitFor();
  assert.equal(await page.locator('#auditScheduleRows').getByText('QA-AUDIT-LATER').count(), 0, 'The default 30-day queue must exclude later audits.');
  assert.equal(await page.locator('#auditScheduleRows img').count(), 0, 'Asset names must render as text, not executable HTML.');
  assert.match(await page.locator('#auditScheduleSummary').innerText(), /1 gecikmiş · 1 yaklaşan · 1 plansız/);
  assert.equal(await page.locator('#auditScheduleCount').innerText(), '1–2 / 2 kayıt');

  const overdueRow = page.locator('#auditScheduleRows tr').filter({ hasText: 'QA-AUDIT-OVERDUE' });
  await overdueRow.getByRole('button', { name: 'Denetimi kaydet', exact: true }).click();
  await page.locator('#assetAuditDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#dialogTitle').innerText(), /Overdue synthetic asset/);
  await page.locator('[data-close-asset-audit]').first().click();
  await page.keyboard.press('Escape');
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  failNextAuditAssetLoad = true;
  const upcomingRow = page.locator('#auditScheduleRows tr').filter({ hasText: 'QA-AUDIT-UPCOMING' });
  const failedDetail = page.waitForResponse(value => value.url().endsWith(`/api/assets/${auditItems[1].id}`) && value.status() === 503);
  await upcomingRow.getByRole('button', { name: 'Denetimi kaydet', exact: true }).click();
  await failedDetail;
  await page.waitForFunction(() => !document.querySelector('#auditScheduleRows button:disabled'));
  assert.equal(await page.locator('#assetAuditDialog').isVisible(), false, 'A failed detail load must not audit the previously opened asset.');

  await page.locator('#auditScheduleState').selectOption('overdue');
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-OVERDUE').waitFor();
  assert.equal(await page.locator('#auditScheduleRows tr').count(), 1, 'The overdue queue must contain only overdue audits.');
  await page.locator('#auditScheduleState').selectOption('upcoming');
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-UPCOMING').waitFor();
  assert.equal(await page.locator('#auditScheduleRows tr').count(), 1, 'The upcoming queue must contain only audits within the selected horizon.');
  await page.locator('#auditScheduleState').selectOption('unscheduled');
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-UNSCHEDULED').waitFor();
  assert.equal(await page.locator('#auditScheduleRows tr').count(), 1, 'The unscheduled queue must remain separate from due audits.');
  await page.locator('#auditScheduleState').selectOption('all');
  await page.locator('#auditScheduleHorizon').selectOption('90');
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-LATER').waitFor();
  assert.equal(await page.locator('#auditScheduleCount').innerText(), '1–50 / 55 kayıt');
  assert.equal(await page.locator('#auditSchedulePageInfo').innerText(), '1 / 2 sayfa');
  const auditPageTwo = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets/audit-schedule' && url.searchParams.get('page') === '2';
  });
  await page.locator('#auditScheduleNext').click();
  await auditPageTwo;
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-PAGED-52').waitFor();
  assert.equal(await page.locator('#auditScheduleRows tr').count(), 5);
  assert.equal(await page.locator('#auditScheduleCount').innerText(), '51–55 / 55 kayıt');
  assert.equal(await page.locator('#auditSchedulePageInfo').innerText(), '2 / 2 sayfa');
  await page.locator('#auditSchedulePrev').click();
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-PAGED-01').waitFor();
  assert.equal(await page.locator('#auditSchedulePageInfo').innerText(), '1 / 2 sayfa');
  const auditSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return url.pathname === '/api/assets/audit-schedule' && url.searchParams.get('q') === 'QA-AUDIT-UPCOMING';
  });
  await page.locator('#auditScheduleSearch').fill('QA-AUDIT-UPCOMING');
  await auditSearchResponse;
  await page.waitForFunction(() => document.querySelectorAll('#auditScheduleRows tr').length === 1);
  assert.equal(await page.locator('#auditScheduleRows tr').count(), 1);
  assert.ok(requests.some(item => item.path === '/api/assets/audit-schedule' && item.state === 'overdue'));
  assert.ok(requests.some(item => item.path === '/api/assets/audit-schedule' && item.state === 'upcoming'));
  assert.ok(requests.some(item => item.path === '/api/assets/audit-schedule' && item.state === 'unscheduled'));
  assert.ok(requests.some(item => item.path === '/api/assets/audit-schedule' && item.through > auditDay(30)));
  assert.ok(requests.some(item => item.path === '/api/assets/audit-schedule' && item.query === 'qa-audit-upcoming'));

  const clearSearch = page.waitForResponse(value => new URL(value.url()).pathname === '/api/assets/audit-schedule' && !new URL(value.url()).searchParams.has('q'));
  await page.locator('#auditScheduleSearch').fill('');
  await clearSearch;
  await page.locator('#auditScheduleState').selectOption('completed');
  await page.locator('#auditScheduleRows').getByText('QA-COMPLETED-001', { exact: true }).waitFor();
  assert.equal(await page.locator('#auditScheduleDateHeading').innerText(), 'Denetim tarihi');
  assert.equal(await page.locator('#auditScheduleStateHeading').innerText(), 'Sonuç');
  assert.equal(await page.locator('#auditScheduleHorizonLabel').innerText(), 'Denetim tamamlanma aralığı');
  assert.equal(await page.locator('#auditScheduleCount').innerText(), '1–50 / 52 kayıt');
  assert.equal(await page.locator('#auditScheduleRows').getByRole('button', { name: 'Varlığı aç', exact: true }).count(), 50);
  assert.equal(await page.locator('#auditScheduleRows img').count(), 0);
  assert.match(await page.locator('#auditScheduleRows').innerText(), /Uygun değil \/ bulgu var/);
  assert.match(await page.locator('#auditScheduleRows').innerText(), /audit-finding-marker <img/);
  await page.locator('#auditScheduleNext').click();
  await page.locator('#auditScheduleRows').getByText('QA-COMPLETED-052', { exact: true }).waitFor();
  assert.equal(await page.locator('#auditScheduleCount').innerText(), '51–52 / 52 kayıt');
  await page.locator('#auditScheduleSearch').fill('audit-finding-marker');
  await page.locator('#auditScheduleRows').getByText('QA-COMPLETED-001', { exact: true }).waitFor();
  await page.waitForFunction(() => document.querySelector('#auditScheduleCount').textContent === '1–1 / 1 kayıt');
  assert.equal(await page.locator('#auditScheduleRows').getByRole('button', { name: 'Varlığı aç', exact: true }).count(), 1);
  const completedDetail = page.waitForResponse(value => value.url().endsWith(`/api/assets/${auditItems[0].id}`) && value.status() === 200);
  await page.locator('#auditScheduleRows').getByRole('button', { name: 'Varlığı aç', exact: true }).click();
  await completedDetail;
  await page.locator('#assetDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#dialogTitle').innerText(), /Overdue synthetic asset/);
  await page.keyboard.press('Escape');
  await page.locator('#assetDialog').waitFor({ state: 'hidden' });
  const completedRefresh = page.waitForResponse(value => new URL(value.url()).pathname === '/api/assets/audit-schedule' &&
    new URL(value.url()).searchParams.get('state') === 'completed' && new URL(value.url()).searchParams.get('q') === 'audit-finding-marker');
  await page.locator('#auditScheduleRefresh').click();
  await completedRefresh;
  assert.equal(await page.locator('#auditScheduleSearch').inputValue(), 'audit-finding-marker');
  sessionRole = 'viewer';
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => !document.body.classList.contains('auth-required'));
  await page.locator('#sidebar [data-group="operations"]').click();
  await page.locator('#sidebar [data-page="Denetimler"]').click();
  await page.locator('#auditScheduleRows').getByText('QA-AUDIT-OVERDUE', { exact: true }).waitFor();
  assert.equal(await page.locator('#auditScheduleDateHeading').innerText(), 'Plan tarihi');
  assert.equal(await page.locator('#auditScheduleRows').getByRole('button', { name: 'Denetimi kaydet', exact: true }).count(), 0,
    'Read-only users must not receive a quick audit write action.');

  const writes = requests.filter(item => item.method !== 'GET');
  assert.equal(writes.length, 4, 'Only two explicit ticket creations, one reply and one close may write.');
  assert.deepEqual(writes.map(item => item.path), [
    '/api/support/tickets', `/api/support/tickets/${firstId}/messages`,
    '/api/support/tickets', `/api/support/tickets/${secondId}/close`
  ]);
  assert.ok(writes.every(item => item.csrf === csrfToken), 'Every support write must include the synthetic CSRF token.');
  assert.deepEqual(writes[0].body, { subject: 'Older support issue', category: 'Data', message: 'Initial customer description' });
  assert.deepEqual(writes[1].body, { body: 'Additional information for support' });
  assert.deepEqual(pageErrors, [], 'The support UI must not throw browser errors.');
  assert.deepEqual(externalRequests, [], 'The support flow must not send company support content to an external origin.');
  console.log('PASS: Chromium audit queues split scheduled/completed history, page/search/refresh safely, open the correct asset, cancel quick audits without writes, reject stale assets after load failure, and hide write actions from viewers.');
  console.log('PASS: real Chromium support ticket create/reply/close, required form validation, category/status/search/refresh, CSRF write scope, and protection from stale list/detail responses.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
