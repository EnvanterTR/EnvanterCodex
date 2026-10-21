import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/platform-site/platform-admin.html');
const writes = [];
let release;
let requestReady;
const prepareRequest = () => new Promise(resolve => { requestReady = resolve; });
let fail = false;
const message = 'Bu e-posta bir hesaba bağlıysa parola yenileme bağlantısı gönderilecektir.';
const server = createServer(async (request, response) => {
  const path = new URL(request.url, 'http://localhost').pathname;
  const json = (body, status = 200) => { response.writeHead(status, { 'content-type': 'application/json' }); response.end(JSON.stringify(body)); };
  if (path === '/') { response.writeHead(200, { 'content-type': 'text/html' }); response.end(html); return; }
  if (path.endsWith('.js')) { response.writeHead(200, { 'content-type': 'application/javascript' }); response.end(''); return; }
  if (path === '/api/session') { json({}, 401); return; }
  if (path === '/api/status') { json({ status: 'ready', pendingMigrationCount: 0 }); return; }
  if (path === '/api/setup/platform/status') { json({ setupRequired: false }); return; }
  if (path === '/api/session/csrf') { json({ requestToken: 'recovery-csrf' }); return; }
  if (path === '/api/session/password/forgot') {
    let body = ''; for await (const chunk of request) body += chunk;
    writes.push({ body: JSON.parse(body), token: request.headers['x-csrf-token'] });
    await new Promise(resolve => { release = resolve; requestReady(); });
    json({ message: fail ? 'Gönderim hizmeti geçici olarak kullanılamıyor.' : message }, fail ? 503 : 202);
    return;
  }
  json({});
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
try {
  const page = await browser.newPage();
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.locator('#loginForm [name="email"]').fill('admin@example.invalid');
  await page.locator('#platformForgotPassword').click();
  assert.equal(await page.locator('#platformRecoveryForm [name="email"]').inputValue(), 'admin@example.invalid');
  await page.locator('#platformRecoveryCancel').click();
  assert.equal(writes.length, 0);
  await page.locator('#platformForgotPassword').click();
  const requested = page.waitForRequest(request => request.url().endsWith('/api/session/password/forgot'));
  const ready = prepareRequest();
  await page.locator('#platformRecoverySubmit').click();
  await requested;
  await page.locator('#platformRecoveryForm').dispatchEvent('submit');
  assert.equal(await page.locator('#platformRecoverySubmit').isDisabled(), true);
  await ready;
  assert.equal(writes.length, 1);
  assert.deepEqual(writes[0], { body: { email: 'admin@example.invalid' }, token: 'recovery-csrf' });
  release(); release = null;
  await page.getByText(message, { exact: true }).waitFor();
  assert.equal(await page.locator('#platformRecoverySubmit').isEnabled(), true);
  fail = true;
  const failedReady = prepareRequest();
  const failedRequested = page.waitForRequest(request => request.url().endsWith('/api/session/password/forgot'));
  await page.locator('#platformRecoverySubmit').click();
  await failedRequested;
  await failedReady;
  release(); release = null;
  await page.getByText('Gönderim hizmeti geçici olarak kullanılamıyor.', { exact: true }).waitFor();
  assert.equal(await page.locator('#platformRecoveryForm [name="email"]').inputValue(), 'admin@example.invalid');
  await page.setViewportSize({ width: 390, height: 844 });
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  await page.locator('#platformRecoveryClose').click();
  console.log('PASS: platform password recovery prefills email, cancels without writes, sends one CSRF-protected request, blocks duplicate submission, displays generic acceptance and actionable errors, retains failed input, and fits390px. Synthetic API; no email sent or MFA bypassed.');
} finally { await browser.close(); await new Promise(resolve => server.close(resolve)); }
