import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/platform-site/platform-admin.html');
const scripts = new Map(await Promise.all(['platform-admin-subscriptions.js', 'platform-admin-members.js', 'platform-admin-demo-requests.js', 'platform-admin-payment-follow-ups.js']
  .map(async name => [`/${name}`, await readFile(`/platform-site/${name}`)])));
const csrf = 'synthetic-platform-support-csrf';
const ids = ['11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222'];
const records = [
  { id: ids[0], companyId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', companyName: 'Older synthetic company', subject: 'Older support issue', category: 'Data', status: 'Open', requester: 'Synthetic requester', createdAt: '2026-10-15T10:00:00Z', updatedAt: '2026-10-15T10:00:00Z', messageCount: 1 },
  { id: ids[1], companyId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', companyName: 'Newer synthetic company', subject: 'Newer support issue', category: 'Access', status: 'Open', requester: 'Synthetic requester', createdAt: '2026-10-15T11:00:00Z', updatedAt: '2026-10-15T11:00:00Z', messageCount: 1 }
];
const messages = new Map(ids.map((id, index) => [id, [{ id: `message-${index}`, body: `<img src=x onerror="alert(1)"> initial description`, isSupportReply: false, author: 'Synthetic requester', createdAt: '2026-10-15T10:00:00Z' }]]));
const requests = [];
const json = (response, body, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/platform-admin.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' }); response.end(html); return;
  }
  if (request.method === 'GET' && scripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(scripts.get(url.pathname)); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/platform/bootstrap/status') return json(response, { setupRequired: false, setupTokenConfigured: false });
  if (request.method === 'GET' && url.pathname === '/api/status') return json(response, { status: 'ready', migrationCount: 96, pendingMigrationCount: 0 });
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'set-cookie': 'Antiforgery=synthetic; Path=/; SameSite=Lax' });
    response.end(JSON.stringify({ requestToken: csrf })); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/session') return json(response, { platformAdmin: true, name: 'Synthetic support operator' });
  if (request.method === 'GET' && url.pathname === '/api/session/2fa') return json(response, { enabled: true });
  if (request.method === 'GET' && url.pathname === '/api/platform/overview') return json(response, {
    companies: { total: 0, active: 0, suspended: 0 }, subscriptions: { paidActive: 0, customerTrials: 0, demos: 0, expired: 0, renewalsDue: 0 },
    checkout: { paymentPending: 0, paymentReview: 0, paymentFailed: 0, provisioningFailed: 0 }, recurringRevenue: { currencies: [] }
  });
  if (request.method === 'GET' && url.pathname === '/api/platform/companies/renewals') return json(response, { items: [], total: 0 });
  if (request.method === 'GET' && url.pathname === '/api/platform/companies') return json(response, { items: [], total: 0 });
  if (request.method === 'GET' && url.pathname === '/api/platform/demo-requests') return json(response, { items: [], total: 0 });
  if (url.pathname.startsWith('/api/platform/support/tickets')) {
    if (request.method === 'GET' && url.pathname === '/api/platform/support/tickets') {
      const query = url.searchParams.get('q') || '';
      const status = url.searchParams.get('status') || '';
      requests.push({ method: 'GET', path: url.pathname, query, status, page: url.searchParams.get('page') });
      if (query === 'older') await wait(500);
      if (query === 'newer') await wait(20);
      const filtered = records.filter(ticket => (!query || ticket.subject.toLowerCase().includes(query)) && (!status || ticket.status === status));
      return json(response, { items: filtered, total: filtered.length, page: Number(url.searchParams.get('page') || 1), pageSize: 25 });
    }
    const detail = url.pathname.match(/^\/api\/platform\/support\/tickets\/([0-9a-f-]+)(?:\/messages)?$/i);
    if (request.method === 'GET' && detail && !url.pathname.endsWith('/messages')) {
      const id = detail[1]; requests.push({ method: 'GET', path: url.pathname });
      if (id === ids[0]) await wait(500);
      if (id === ids[1]) await wait(20);
      const ticket = records.find(item => item.id === id);
      if (!ticket) return json(response, { message: 'Not found' }, 404);
      return json(response, { ticket: { ...ticket }, messages: messages.get(id) });
    }
    let raw = ''; request.setEncoding('utf8'); for await (const chunk of request) raw += chunk;
    const body = raw ? JSON.parse(raw) : {};
    if (request.headers['x-csrf-token'] !== csrf || !String(request.headers.cookie || '').includes('Antiforgery=synthetic'))
      return json(response, { message: 'Synthetic CSRF required.' }, 400);
    if (request.method === 'POST' && detail && url.pathname.endsWith('/messages')) {
      const id = detail[1];
      const ticket = records.find(item => item.id === id);
      if (!ticket || ticket.status === 'Closed') return json(response, { message: 'Closed or missing.' }, ticket ? 409 : 404);
      requests.push({ method: 'POST', path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
      const message = { id: `reply-${id}`, body: body.body, isSupportReply: true, author: null, createdAt: new Date().toISOString() };
      messages.get(id).push(message); ticket.messageCount++; ticket.updatedAt = message.createdAt; ticket.status = 'AwaitingCustomer';
      return json(response, { id: message.id, isSupportReply: true, createdAt: message.createdAt }, 201);
    }
    const statusRoute = url.pathname.match(/^\/api\/platform\/support\/tickets\/([0-9a-f-]+)\/status$/i);
    if (request.method === 'PATCH' && statusRoute) {
      const ticket = records.find(item => item.id === statusRoute[1]);
      if (!ticket) return json(response, { message: 'Not found' }, 404);
      requests.push({ method: 'PATCH', path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
      ticket.status = body.status; ticket.updatedAt = new Date().toISOString();
      return json(response, { id: ticket.id, status: ticket.status, updatedAt: ticket.updatedAt });
    }
    return json(response, { message: 'Unsupported support request.' }, 405);
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' }); response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
page.setDefaultTimeout(10_000);
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/platform-admin.html`, { waitUntil: 'domcontentloaded' });
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#nav [data-view="support"]').click();
  await page.locator('#supportTable').getByText('Older support issue').waitFor();
  assert.equal(await page.locator('#supportCount').innerText(), '2 talep');
  assert.equal(await page.locator('#prevSupport').isDisabled(), true);
  assert.equal(await page.locator('#nextSupport').isDisabled(), true);

  const olderSearchRequest = page.waitForRequest(request => new URL(request.url()).pathname === '/api/platform/support/tickets' && new URL(request.url()).searchParams.get('q') === 'older');
  const olderSearchResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/platform/support/tickets' && new URL(response.url()).searchParams.get('q') === 'older');
  await page.locator('#supportSearch').fill('older'); await page.locator('#supportSearch').press('Enter'); await olderSearchRequest;
  const newerSearch = page.waitForResponse(response => new URL(response.url()).pathname === '/api/platform/support/tickets' && new URL(response.url()).searchParams.get('q') === 'newer');
  await page.locator('#supportSearch').fill('newer'); await page.locator('#supportSearch').press('Enter'); await newerSearch; await olderSearchResponse;
  await page.locator('#supportTable').getByText('Newer support issue').waitFor();
  await wait(550);
  assert.equal(await page.locator('#supportTable').getByText('Older support issue').count(), 0,
    'A delayed older search must not replace the current filtered support queue.');

  await page.locator('#supportSearch').fill(''); await page.locator('#supportSearch').press('Enter');
  await page.locator(`#supportTable [data-support-admin-open="${ids[0]}"]`).waitFor();
  const oldDetailRequest = page.waitForRequest(request => new URL(request.url()).pathname === `/api/platform/support/tickets/${ids[0]}`);
  const oldDetailResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/support/tickets/${ids[0]}`);
  await page.locator(`#supportTable [data-support-admin-open="${ids[0]}"]`).click(); await oldDetailRequest;
  const newDetail = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/support/tickets/${ids[1]}`);
  await page.locator(`#supportTable [data-support-admin-open="${ids[1]}"]`).click(); await newDetail; await oldDetailResponse;
  await page.locator('#supportAdminTitle').getByText('Newer support issue').waitFor(); await wait(550);
  assert.equal(await page.locator('#supportAdminTitle').innerText(), 'Newer support issue',
    'A delayed detail response must not replace the currently selected support ticket.');
  assert.equal(await page.locator('#supportAdminMessages img').count(), 0, 'Ticket messages must render as text, not HTML.');
  assert.match(await page.locator('#supportAdminMessages').innerText(), /<img src=x onerror=/);

  await page.locator('#supportAdminReply textarea[name="body"]').fill('Checked by platform support.');
  await page.locator('#supportAdminReplyButton').click();
  await page.locator('#supportAdminMessages').getByText('Checked by platform support.').waitFor();
  await page.locator('#supportAdminStatus select[name="status"]').selectOption('Resolved');
  await page.locator('#supportAdminStatus button[type="submit"]').click();
  await page.locator('#supportAdminStatus select[name="status"]').waitFor();
  assert.equal(await page.locator('#supportAdminStatus select[name="status"]').inputValue(), 'Resolved');
  assert.ok(requests.some(item => item.method === 'POST' && item.path.endsWith('/messages') && item.csrf === csrf));
  assert.ok(requests.some(item => item.method === 'PATCH' && item.path.endsWith('/status') && item.body.status === 'Resolved' && item.csrf === csrf));

  await page.locator('#supportStatus').selectOption('Closed');
  await page.locator('#supportTable').getByText('Destek talebi bulunmuyor.').waitFor();
  await page.locator('#refreshSupport').click();
  await page.locator('#supportTable').getByText('Destek talebi bulunmuyor.').waitFor();
  assert.deepEqual(pageErrors, [], 'Platform support UI must not throw browser errors.');
  console.log('PASS: platform support ticket filtering, stale response guards, safe message rendering, CSRF reply/status updates, and refresh.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
