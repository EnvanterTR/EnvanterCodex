import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/platform-site/platform-setup.html');
const adminHtml = await readFile('/platform-site/platform-admin.html');
const adminScripts = new Map(await Promise.all(['platform-admin-subscriptions.js', 'platform-admin-members.js', 'platform-admin-demo-requests.js', 'platform-admin-payment-follow-ups.js']
  .map(async (name) => [`/${name}`, await readFile(`/platform-site/${name}`)])));
const writes = [];
const loginAttempts = [];
const companySearchRequests = [];
const memberSearchRequests = [];
const csrfToken = 'synthetic-platform-bootstrap-csrf';
const setupToken = 'synthetic-platform-bootstrap-token';
let state = { setupRequired: false, setupTokenConfigured: true };
let appReadiness = { status: 'ready', migrationCount: 92, pendingMigrationCount: 0 };
let statusFailure = false;
let csrfFailure = false;
const json = (response, body, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/platform-setup.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' }); response.end(html); return;
  }
  if (request.method === 'GET' && url.pathname === '/platform-admin.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' }); response.end(adminHtml); return;
  }
  if (request.method === 'GET' && adminScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8', 'cache-control': 'no-store' }); response.end(adminScripts.get(url.pathname)); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/platform/bootstrap/status') {
    return statusFailure ? json(response, { message: 'Veritabanı şeması hazır değil.' }, 503) : json(response, state);
  }
  if (request.method === 'GET' && url.pathname === '/api/status') return json(response, appReadiness, statusFailure ? 503 : 200);
  if (request.method === 'GET' && url.pathname === '/api/platform/members') {
    const query = url.searchParams.get('q') || '';
    const page = Number(url.searchParams.get('page') || 1);
    const pageSize = Number(url.searchParams.get('pageSize') || 50);
    memberSearchRequests.push({ query, page: String(page), pageSize: String(pageSize) });
    const matches = query.toLocaleLowerCase() === 'synthetic.owner@example.invalid';
    const items = matches ? [{ id: 'synthetic-platform-owner', displayName: 'Synthetic Platform Owner', email: query,
      emailConfirmed: true, accountCreatedAt: '2026-10-01T12:00:00Z', phoneNumber: null, twoFactorEnabled: true,
      isLockedOut: false, firstMembershipAt: '2026-10-01T12:00:00Z', lastSeenAt: null, membershipCount: 1,
      memberships: [{ name: 'Santa Demo', slug: 'santa-demo', roleKey: 'owner', isActive: true }] }] : [];
    return json(response, { items, page, pageSize, total: items.length });
  }
  if (request.method === 'GET' && url.pathname === '/api/platform/companies') {
    companySearchRequests.push({ query: url.searchParams.get('q'), page: url.searchParams.get('page'), pageSize: url.searchParams.get('pageSize') });
    return json(response, { items: [], total: 0, page: Number(url.searchParams.get('page') || 1), pageSize: Number(url.searchParams.get('pageSize') || 20) });
  }
  if (request.method === 'GET' && url.pathname === '/api/session') return json(response, { message: 'Synthetic unauthenticated.' }, 401);
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') {
    return csrfFailure ? json(response, { message: 'Sentetik CSRF servisi kullanılamıyor.' }, 503) : json(response, { requestToken: csrfToken });
  }
  if (request.method === 'POST' && url.pathname === '/api/session/login') {
    let raw = ''; request.setEncoding('utf8'); for await (const chunk of request) raw += chunk;
    assert.equal(request.headers['x-csrf-token'], csrfToken);
    loginAttempts.push(JSON.parse(raw));
    return json(response, { twoFactorRequired: true, message: 'Synthetic MFA challenge.' }, 409);
  }
  if (request.method === 'POST' && url.pathname === '/api/platform/bootstrap') {
    let raw = ''; request.setEncoding('utf8'); for await (const chunk of request) raw += chunk;
    const body = JSON.parse(raw);
    if (request.headers['x-csrf-token'] !== csrfToken) return json(response, { message: 'Synthetic CSRF required.' }, 400);
    if (request.headers['x-platform-setup-token'] !== setupToken) return json(response, { message: 'Synthetic setup token rejected.' }, 404);
    if (!state.setupRequired || !state.setupTokenConfigured) return json(response, { message: 'Setup is unavailable.' }, 409);
    writes.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'], token: request.headers['x-platform-setup-token'] });
    state = { setupRequired: false, setupTokenConfigured: false };
    return json(response, { message: 'Synthetic platform admin created.' }, 201);
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' }); response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 900, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/platform-setup.html`, { waitUntil: 'domcontentloaded' });
  const form = page.locator('#setupForm');
  await form.waitFor({ state: 'hidden' });
  assert.match(await page.locator('#message').innerText(), /Platform yöneticisi daha önce oluşturulmuş/);
  assert.equal(writes.length, 0, 'Existing platform admin status must not permit a bootstrap write.');

  state = { setupRequired: true, setupTokenConfigured: false };
  await page.reload({ waitUntil: 'domcontentloaded' });
  await form.waitFor({ state: 'hidden' });
  assert.match(await page.locator('#message').innerText(), /kurulum anahtarı ayarlanmamış/);
  assert.equal(writes.length, 0, 'Missing server bootstrap token must keep the form unavailable.');

  statusFailure = true;
  await page.reload({ waitUntil: 'domcontentloaded' });
  await form.waitFor({ state: 'hidden' });
  assert.match(await page.locator('#message').innerText(), /Veritabanı şeması hazır değil/);
  assert.equal(writes.length, 0, 'Unavailable application readiness must fail closed.');
  statusFailure = false;
  state = { setupRequired: true, setupTokenConfigured: true };

  csrfFailure = true;
  await page.reload({ waitUntil: 'domcontentloaded' });
  await form.waitFor({ state: 'hidden' });
  assert.match(await page.locator('#message').innerText(), /Sentetik CSRF servisi kullanılamıyor/);
  assert.equal(writes.length, 0, 'Unavailable CSRF must fail closed.');
  csrfFailure = false;

  state = { setupRequired: true, setupTokenConfigured: true };
  await page.reload({ waitUntil: 'domcontentloaded' });
  await form.waitFor({ state: 'visible' });
  assert.equal(await form.locator('button[type="submit"]').isEnabled(), true, 'Bootstrap form enables only after status and CSRF load.');
  const passwordField = form.locator('[name="password"]');
  assert.equal(await passwordField.getAttribute('autocomplete'), 'current-password');
  assert.match(await form.innerText(), /mevcut parolasını girin; yeni hesapta burada girdiğiniz parola oluşturulur/);
  await form.locator('[name="fullName"]').fill('Synthetic Platform Owner');
  await form.locator('[name="email"]').fill('synthetic-platform-owner@example.invalid');
  await passwordField.fill('Synthetic-only-password-42!');
  await form.locator('[name="setupToken"]').fill(setupToken);
  const beforeSubmit = writes.length;
  await form.locator('button[type="submit"]').click();
  await form.waitFor({ state: 'hidden' });
  assert.match(await page.locator('#message').innerText(), /Platform hesabı oluşturuldu\. Kurulum anahtarını sunucu ayarlarından kaldırın/);
  assert.equal(writes.length, beforeSubmit + 1, 'Only explicit valid bootstrap submission should create the first platform admin.');
  assert.deepEqual(writes[0], {
    method: 'POST', path: '/api/platform/bootstrap',
    body: { fullName: 'Synthetic Platform Owner', email: 'synthetic-platform-owner@example.invalid', password: 'Synthetic-only-password-42!' },
    csrf: csrfToken, token: setupToken
  });
  assert.equal(new URL(page.url()).search, '', 'Bootstrap must not put credentials in the URL.');

  await page.goto(`http://127.0.0.1:${address.port}/platform-admin.html`, { waitUntil: 'domcontentloaded' });
  const bootstrapLink = page.locator('#platformBootstrapLink');
  await bootstrapLink.waitFor({ state: 'hidden' });
  const connectionButton = page.locator('#platformCheckConnection');
  await connectionButton.click();
  await page.locator('#platformLoginReadiness').waitFor({ state: 'visible' });
  assert.match(await page.locator('#platformLoginReadiness').innerText(), /Sunucu ve veritabanı hazır\. 92 veritabanı güncellemesi uygulanmış; bekleyen yok\./);
  assert.equal(await page.locator('#platformLoginSubmit').isEnabled(), true, 'A ready manual check leaves login enabled.');
  appReadiness = { status: 'migration-required', migrationCount: 92, pendingMigrationCount: 3 };
  await connectionButton.click();
  await page.waitForFunction(() => document.querySelector('#platformLoginReadiness').innerText.includes('3 veritabanı güncellemesi bekliyor'));
  assert.match(await page.locator('#platformLoginReadiness').innerText(), /3 veritabanı güncellemesi bekliyor/);
  assert.equal(await page.locator('#platformLoginSubmit').isEnabled(), false, 'Pending migrations keep login disabled after the manual check.');
  appReadiness = { status: 'ready', migrationCount: 92, pendingMigrationCount: 0 };
  await connectionButton.click();
  await page.waitForFunction(() => document.querySelector('#platformLoginReadiness').innerText.includes('Sunucu ve veritabanı hazır'));
  assert.match(await page.locator('#platformLoginReadiness').innerText(), /Sunucu ve veritabanı hazır/);
  assert.equal(await page.locator('#platformLoginSubmit').isEnabled(), true, 'A later healthy check restores login availability.');
  const remember = page.getByRole('checkbox', { name: 'Beni hatırla (30 gün)' });
  assert.equal(await remember.isChecked(), false, 'Platform persistence must be opt-in.');
  await page.locator('#loginForm [name="email"]').fill('synthetic-platform-owner@example.invalid');
  await page.locator('#loginForm [name="password"]').fill('Synthetic-only-password-42!');
  await page.locator('#platformLoginSubmit').click();
  await page.locator('#platformTwoFactorField').waitFor({ state: 'visible' });
  assert.equal(loginAttempts.at(-1).rememberMe, false);
  assert.equal(loginAttempts.at(-1).platformAdminOnly, true);
  await page.locator('#loginForm [name="twoFactorCode"]').fill('123456');
  await remember.check();
  const nextLogin = page.waitForResponse(response => new URL(response.url()).pathname === '/api/session/login');
  await page.locator('#platformLoginSubmit').click();
  await nextLogin;
  assert.equal(loginAttempts.at(-1).rememberMe, true);
  assert.equal(loginAttempts.at(-1).twoFactorCode, '123456');
  assert.equal(new URL(page.url()).search, '', 'Login credentials must stay out of URL parameters.');
  console.log('PASS: platform login defaults to a session cookie; explicit remember choice and MFA code survive the second login step.');
  await page.evaluate(() => { document.querySelector('#loginWrap').classList.add('login-hidden'); document.querySelector('#appShell').hidden = false; });
  const globalSearch = page.locator('#globalSearch');
  assert.equal(await globalSearch.getAttribute('aria-keyshortcuts'), 'Control+K Meta+K');
  await page.keyboard.press('Control+k');
  assert.equal(await globalSearch.evaluate(input => document.activeElement === input), true, 'Ctrl+K focuses the platform-wide search.');
  assert.equal(await globalSearch.evaluate(input => input.selectionStart === 0 && input.selectionEnd === input.value.length), true, 'Ctrl+K selects an existing query for quick replacement.');
  await globalSearch.fill('Santa Demo');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => document.querySelector('#view-companies').classList.contains('active'));
  await page.waitForFunction(() => document.querySelector('#companiesTable').textContent.includes('Aramanızla eşleşen firma bulunamadı.'));
  assert.deepEqual(companySearchRequests.at(-1), { query: 'Santa Demo', page: '1', pageSize: '50' }, 'Submitting the global shortcut search routes to the companies view and carries the query to its server-backed filter.');
  await globalSearch.fill('synthetic.owner@example.invalid');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => document.querySelector('#view-members').classList.contains('active'));
  await page.waitForFunction(() => document.querySelector('#memberCount').textContent === '1 hesap').catch(async (error) => {
    const details = await page.evaluate(() => ({ memberCount: document.querySelector('#memberCount').textContent,
      memberSearch: document.querySelector('#memberSearch').value, memberTable: document.querySelector('#membersTable').innerText,
      currentView: [...document.querySelectorAll('.view')].find((view) => view.classList.contains('active'))?.id }));
    throw new Error(`${error.message}; member state=${JSON.stringify(details)}; member requests=${JSON.stringify(memberSearchRequests)}; company requests=${JSON.stringify(companySearchRequests)}`);
  });
  assert.equal(await page.locator('#memberSearch').inputValue(), 'synthetic.owner@example.invalid', 'An exact platform account email must open the account directory with the query preserved.');
  assert.equal(await page.locator('#membersTable').getByText('Synthetic Platform Owner').count(), 1, 'The global search must render the matching platform account.');
  assert.deepEqual(memberSearchRequests.at(-1), { query: 'synthetic.owner@example.invalid', page: '1', pageSize: '50' }, 'The selected member result must use the account directory filter.');
  console.log('PASS: platform Ctrl+K resolves company searches to the company list and exact account emails to the member directory, preserving query and server filters.');
  await page.evaluate(() => { document.querySelector('#appShell').hidden = true; document.querySelector('#loginWrap').classList.remove('login-hidden'); });
  statusFailure = true;
  await connectionButton.click();
  await page.waitForFunction(() => document.querySelector('#platformLoginReadiness').innerText.includes('Sunucu bağlantısı doğrulanamadı'));
  assert.match(await page.locator('#platformLoginReadiness').innerText(), /Sunucu bağlantısı doğrulanamadı/);
  assert.equal(await page.locator('#platformLoginSubmit').isEnabled(), false, 'An unavailable status endpoint fails closed.');
  statusFailure = false;
  state = { setupRequired: true, setupTokenConfigured: false };
  await page.reload({ waitUntil: 'domcontentloaded' });
  await bootstrapLink.waitFor({ state: 'hidden' });
  state = { setupRequired: true, setupTokenConfigured: true };
  await page.reload({ waitUntil: 'domcontentloaded' });
  await bootstrapLink.waitFor({ state: 'visible' });
  statusFailure = true;
  await page.reload({ waitUntil: 'domcontentloaded' });
  await bootstrapLink.waitFor({ state: 'hidden' });
  statusFailure = false;
  assert.deepEqual(pageErrors, [], 'Bootstrap status, disabled, and success states must not throw browser errors.');
  console.log('PASS: platform bootstrap distinguishes existing-admin, missing-token, and first-setup states; hides the first-admin link unless setup is genuinely available; binds an existing account with its current password; submits once with CSRF and setup token without URL secrets.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
