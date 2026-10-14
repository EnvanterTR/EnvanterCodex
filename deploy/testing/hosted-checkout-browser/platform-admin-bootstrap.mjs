import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/platform-site/platform-setup.html');
const writes = [];
const csrfToken = 'synthetic-platform-bootstrap-csrf';
const setupToken = 'synthetic-platform-bootstrap-token';
let state = { setupRequired: false, setupTokenConfigured: true };
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
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/platform/bootstrap/status') {
    return statusFailure ? json(response, { message: 'Veritabanı şeması hazır değil.' }, 503) : json(response, state);
  }
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') {
    return csrfFailure ? json(response, { message: 'Sentetik CSRF servisi kullanılamıyor.' }, 503) : json(response, { requestToken: csrfToken });
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
  assert.deepEqual(pageErrors, [], 'Bootstrap status, disabled, and success states must not throw browser errors.');
  console.log('PASS: platform bootstrap distinguishes existing-admin, missing-token, and first-setup states; binds an existing account with its current password; submits once with CSRF and setup token without URL secrets.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
