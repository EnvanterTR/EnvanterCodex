import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const platformHtml = await readFile('/platform-site/platform-admin.html');
const platformScripts = new Map([
  ['/platform-admin-members.js', await readFile('/platform-site/platform-admin-members.js')],
  ['/platform-admin-subscriptions.js', await readFile('/platform-site/platform-admin-subscriptions.js')],
  ['/platform-admin-demo-requests.js', await readFile('/platform-site/platform-admin-demo-requests.js')]
]);
const csrfToken = 'synthetic-platform-members-csrf';
const accountId = '11111111-1111-4111-8111-111111111111';
const invitedAccountId = '22222222-2222-4222-8222-222222222222';
const companyId = '33333333-3333-4333-8333-333333333333';
const member = {
  id: accountId, displayName: 'Synthetic SaaS Owner', email: 'qa-owner@example.invalid', emailConfirmed: true,
  phoneNumber: '+905550000001', phoneNumberConfirmed: true, twoFactorEnabled: true, isLockedOut: false,
  accessFailedCount: 0, accountCreatedAt: '2026-09-01T09:30:00Z', lastSeenAt: '2026-10-15T08:00:00Z',
  firstMembershipAt: '2026-09-02T10:00:00Z', membershipCount: 1,
  memberships: [{ id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo', roleKey: 'admin', isActive: true }]
};
const membership = {
  id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo', isActive: true, roleKey: 'admin',
  createdAt: '2026-09-02T10:00:00Z', subscription: {
    name: 'Cloud Pro', code: 'cloud-pro', status: 'Active', isDemo: false,
    startsAt: '2026-09-02T10:00:00Z', endsAt: '2027-09-02T10:00:00Z', paymentReference: 'synthetic-provider-reference'
  }
};
const companies = [{ id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo' }];
const accounts = [member];
const writes = [];
const browserErrors = [];
let reauthenticated = false;
let browser;

const json = (response, body, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/platform-admin.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(platformHtml); return;
  }
  if (request.method === 'GET' && platformScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(platformScripts.get(url.pathname)); return;
  }
  if (request.method === 'GET' && url.pathname === '/assets/fonts/inter.css') {
    response.writeHead(200, { 'content-type': 'text/css' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'font/woff2' }); response.end(Buffer.alloc(0)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }
  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    let raw = ''; request.setEncoding('utf8'); request.on('data', chunk => raw += chunk);
    request.on('end', () => resolve(raw ? JSON.parse(raw) : null));
  });
  const path = url.pathname;
  if (request.method === 'GET') {
    if (path === '/api/session/csrf') return json(response, { requestToken: csrfToken });
    if (path === '/api/session') return json(response, { platformAdmin: true, name: 'Synthetic Platform Admin' });
    if (path === '/api/session/2fa') return json(response, { enabled: true });
    if (path === '/api/platform/overview') return json(response, {
      companies: { total: 1, active: 1, suspended: 0 },
      subscriptions: { paidActive: 1, customerTrials: 0, demos: 0, expired: 0, renewalsDue: 0 },
      checkout: { paymentPending: 0, paymentReview: 0, paymentFailed: 0, provisioningFailed: 0 }, recurringRevenue: { currencies: [] }
    });
    if (path === '/api/platform/companies/renewals') return json(response, { items: [], total: 0, page: 1, pageSize: 50 });
    if (path === '/api/platform/companies') return json(response, { items: companies, total: companies.length, page: 1, pageSize: 200 });
    if (path === '/api/platform/members') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase();
      const items = accounts.filter(item => !q || `${item.displayName} ${item.email}`.toLocaleLowerCase().includes(q));
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
      return json(response, { items, total: items.length, page, pageSize });
    }
    if (path === `/api/platform/members/${accountId}` || path === `/api/platform/members/${invitedAccountId}`) {
      const account = accounts.find(item => item.id === path.split('/').at(-1));
      return account ? json(response, { account, memberships: account.id === accountId ? [membership] : [], membershipsTruncated: false, membershipCount: account.membershipCount })
        : json(response, { message: 'Synthetic account not found.' }, 404);
    }
    if (path.endsWith('/timeline')) return json(response, { items: [
      { id: 'synthetic-membership-event', title: 'Şirkete üye oldu', occurredAt: '2026-09-02T10:00:00Z', description: 'Owner rolüyle üyelik başladı.', companyName: 'Synthetic Demo Company' },
      { id: 'synthetic-demo-event', title: 'Demo talebi alındı', occurredAt: '2026-09-01T10:00:00Z', description: 'Demo talebi değerlendirildi.', companyName: 'Synthetic Demo Company' },
      { id: 'synthetic-payment-event', title: 'Ücretli plan satın alındı', occurredAt: '2026-09-02T10:00:00Z', description: 'Sağlayıcı doğrulamalı sipariş.', companyName: 'Synthetic Demo Company', amount: 1200, currency: 'TRY', paymentReference: 'synthetic-provider-reference' }
    ], page: 1, pageSize: 50, hasMore: false, truncated: false });
  }

  if (request.headers['x-csrf-token'] !== csrfToken) return json(response, { message: 'Missing synthetic CSRF token.' }, 400);
  if (path === '/api/session/platform-reauth' && request.method === 'POST') {
    reauthenticated = true;
    response.writeHead(204); response.end(); return;
  }
  if (path.startsWith('/api/platform/members/') && request.method === 'PATCH' && path.endsWith('/role')) {
    if (!reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Synthetic reauthentication required.' }, 428);
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    membership.roleKey = body.roleKey;
    member.memberships[0].roleKey = body.roleKey;
    response.writeHead(204); response.end(); return;
  }
  if (path === '/api/platform/members/invite' && request.method === 'POST') {
    if (!reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Synthetic reauthentication required.' }, 428);
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    const invited = { id: invitedAccountId, displayName: body.fullName, email: body.email, emailConfirmed: false,
      twoFactorEnabled: false, isLockedOut: false, accessFailedCount: 0, accountCreatedAt: null,
      lastSeenAt: null, firstMembershipAt: null, membershipCount: 1, memberships: [{ name: 'Synthetic Demo Company', roleKey: body.roleKey, isActive: true }] };
    accounts.push(invited);
    return json(response, { message: 'Sentetik davet kaydedildi.' });
  }
  if (path === '/api/platform/site-content') return json(response, []);
  if (path === '/api/platform/demo-requests') return json(response, { items: [], total: 0, page: 1, pageSize: 25 });
  if (path === '/api/platform/checkout-orders') return json(response, { items: [], total: 0, page: 1, pageSize: 25 });
  if (path === '/api/platform/support/tickets') return json(response, { items: [], total: 0, page: 1, pageSize: 25 });
  if (path === '/api/platform/audit') return json(response, { entries: [], total: 0, page: 1, pageSize: 50 });
  if (path === '/api/platform/plans') return json(response, { items: [], total: 0, page: 1, pageSize: 50 });
  response.writeHead(404); response.end('Unknown synthetic API route');
});

try {
  browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  page.setDefaultTimeout(10_000);
  page.on('pageerror', error => browserErrors.push(error.message));
  page.on('response', response => {
    const path = new URL(response.url()).pathname;
    if (path.includes('/api/platform/members/') && path.endsWith('/role')) console.log(`SYNTHETIC role response: ${response.status()}`);
    if (path === '/api/session/platform-reauth') console.log(`SYNTHETIC reauthentication response: ${response.status()}`);
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const address = server.address();
  await page.goto(`http://127.0.0.1:${address.port}/platform-admin.html`);
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#nav [data-view="members"]').click();
  const memberRow = page.locator('#membersTable tr').filter({ hasText: 'qa-owner@example.invalid' });
  await memberRow.waitFor({ state: 'visible' });
  assert.match(await memberRow.innerText(), /doğrulanmış[\s\S]*2FA açık[\s\S]*2026[\s\S]*Synthetic Demo Company[\s\S]*Yönetici/);

  const searchResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/platform/members'
    && new URL(response.url()).searchParams.get('q') === 'qa-owner@example.invalid');
  await page.locator('#memberSearch').fill('qa-owner@example.invalid');
  await page.locator('#memberSearch').press('Enter');
  await searchResponse;
  assert.equal(await page.locator('#memberCount').innerText(), '1 hesap');
  await page.locator('#membersTable [data-member-detail]').click();
  await page.locator('#memberDetail').waitFor({ state: 'visible' });
  const detail = page.locator('#memberDetailBody');
  await detail.getByText('synthetic-provider-reference').first().waitFor({ state: 'visible' });
  const detailText = await detail.innerText();
  assert.match(detailText, /Hesap açılış tarihi[\s\S]*İlk şirket üyeliği[\s\S]*Cloud Pro[\s\S]*synthetic-provider-reference/);
  assert.match(await page.locator('#memberTimeline').innerText(), /Demo talebi alındı[\s\S]*Ücretli plan satın alındı[\s\S]*1\.200,00/);

  const writeCountBeforeCancel = writes.length;
  await detail.locator('[data-member-role]').click();
  const roleDialog = page.locator('dialog.dialog').filter({ hasText: 'Üyelik rolünü değiştir' });
  await roleDialog.waitFor({ state: 'visible' });
  await roleDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await roleDialog.waitFor({ state: 'detached' });
  assert.equal(writes.length, writeCountBeforeCancel, 'Canceling the membership-role dialog must not write.');

  await detail.locator('[data-member-role]').click();
  const roleDialogAgain = page.locator('dialog.dialog').filter({ hasText: 'Üyelik rolünü değiştir' });
  await roleDialogAgain.waitFor({ state: 'visible' });
  await roleDialogAgain.locator('[name="roleKey"]').selectOption('asset_manager');
  await roleDialogAgain.locator('[name="note"]').fill('Synthetic role review');
  const roleRejectedUntilReauth = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/memberships/${companyId}/role`);
  await roleDialogAgain.getByRole('button', { name: 'Rolü kaydet' }).click();
  assert.equal((await roleRejectedUntilReauth).status(), 428, 'Role update must stop before recent MFA reauthentication.');
  await page.locator('#platformReauthDialog').waitFor({ state: 'visible' });
  await page.locator('#platformReauthForm [name="password"]').fill('Synthetic-only-password');
  await page.locator('#platformReauthForm [name="code"]').fill('123456');
  const reauthResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/session/platform-reauth');
  const roleSaved = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/memberships/${companyId}/role`
    && response.status() === 204);
  await page.locator('#platformReauthSubmit').click();
  assert.equal((await reauthResponse).status(), 204, 'Synthetic second-factor reauthentication must succeed.');
  await roleSaved;
  await page.locator('#platformReauthDialog').waitFor({ state: 'hidden' });
  await page.locator('#memberDetailBody').getByText('Envanter yöneticisi').waitFor({ state: 'visible' });
  assert.equal(writes.length, writeCountBeforeCancel + 1);
  assert.deepEqual(writes[0], { method: 'PATCH', path: `/api/platform/members/${accountId}/memberships/${companyId}/role`,
    body: { roleKey: 'asset_manager', note: 'Synthetic role review' }, csrf: csrfToken });

  await page.locator('#closeMemberDetail').click();
  await page.locator('#openMemberInvite').click();
  const inviteDialog = page.locator('#memberInviteDialog');
  await inviteDialog.waitFor({ state: 'visible' });
  await page.locator('#memberInviteCompany').selectOption(companyId);
  await inviteDialog.locator('[name="fullName"]').fill('Synthetic Invited Viewer');
  await inviteDialog.locator('[name="email"]').fill('qa-invitee@example.invalid');
  await inviteDialog.locator('[name="roleKey"]').selectOption('viewer');
  const invitationSaved = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/platform/members/invite' && response.status() === 200);
  await page.locator('#submitMemberInvite').click();
  await invitationSaved;
  await page.locator('#membersTable').getByText('qa-invitee@example.invalid').waitFor({ state: 'visible' });
  assert.equal(writes.length, writeCountBeforeCancel + 2);
  assert.deepEqual(writes.at(-1), { method: 'POST', path: '/api/platform/members/invite',
    body: { companyId, fullName: 'Synthetic Invited Viewer', email: 'qa-invitee@example.invalid', roleKey: 'viewer' }, csrf: csrfToken });
  assert.deepEqual(browserErrors, [], `Platform member UI threw: ${browserErrors.join(' | ')}`);
  console.log('PASS: real Chromium account registration/security/membership/subscription/payment timeline; member search, cancel-without-write, reauthenticated role update, and synthetic member invitation.');
} finally {
  await browser?.close();
  await new Promise(resolve => server.close(resolve));
}
