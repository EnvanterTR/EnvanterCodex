import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/platform-site/platform-admin.html');
const scripts = new Map(await Promise.all(['platform-admin-subscriptions.js', 'platform-admin-members.js', 'platform-admin-demo-requests.js', 'platform-admin-payment-follow-ups.js']
  .map(async name => [`/${name}`, await readFile(`/platform-site/${name}`)])));
const csrf = 'synthetic-retention-csrf';
const writes = [];
let retentionDays = null;
let reauthenticated = false;
let requireStepUp = true;
const json = (response, body, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

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
  if (request.method === 'GET' && url.pathname === '/api/status') return json(response, { status: 'ready', migrationCount: 1, pendingMigrationCount: 0 });
  if (request.method === 'GET' && url.pathname === '/api/session') return json(response, { platformAdmin: true, name: 'Synthetic platform admin' });
  if (request.method === 'GET' && url.pathname === '/api/session/2fa') return json(response, { enabled: true });
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'set-cookie': 'Antiforgery=synthetic; Path=/; SameSite=Lax' });
    response.end(JSON.stringify({ requestToken: csrf })); return;
  }
  if (request.method === 'GET' && url.pathname === '/api/platform/audit') return json(response, { entries: [], total: 0, page: 1, pageSize: 50 });
  if (request.method === 'GET' && url.pathname === '/api/platform/audit-retention')
    return json(response, { retentionDays, enabled: retentionDays !== null, updatedAt: null, updatedBy: null });
  if (request.method === 'GET' && url.pathname === '/api/platform/audit-retention/preview') {
    const days = Number(url.searchParams.get('days'));
    return json(response, { days, cutoff: new Date(Date.now() - days * 86400000).toISOString(), companyCount: 3, platformCount: 2, totalCount: 5 });
  }
  if (request.method === 'PUT' || request.method === 'POST') {
    let raw = ''; request.setEncoding('utf8'); for await (const chunk of request) raw += chunk;
    const body = raw ? JSON.parse(raw) : {};
    if (request.headers['x-csrf-token'] !== csrf || !String(request.headers.cookie || '').includes('Antiforgery=synthetic'))
      return json(response, { message: 'Synthetic CSRF required.' }, 400);
    if (url.pathname === '/api/session/platform-reauth' && request.method === 'POST') {
      reauthenticated = body.password === 'Synthetic-password-42!' && body.code === '123456';
      if (!reauthenticated) return json(response, { message: 'Synthetic step-up rejected.' }, 401);
      writes.push({ method: request.method, path: url.pathname, csrf: request.headers['x-csrf-token'] });
      return json(response, { authenticated: true });
    }
    if (url.pathname === '/api/platform/audit-retention' && request.method === 'PUT') {
      writes.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
      retentionDays = body.retentionDays;
      return json(response, { retentionDays, enabled: retentionDays !== null, updatedAt: new Date().toISOString(), message: 'Politika kaydedildi; mevcut kayıtları silmedi.' });
    }
    if (url.pathname === '/api/platform/audit-retention/apply' && request.method === 'POST') {
      if (requireStepUp && !reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Son 10 dakika içinde yeniden doğrulayın.' }, 428);
      if (body.retentionDays !== retentionDays || body.confirmation !== `DENETIM-KAYITLARINI-SIL:${retentionDays}`)
        return json(response, { message: 'Synthetic policy/confirmation mismatch.' }, 400);
      writes.push({ method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] });
      return json(response, { days: retentionDays, deletedCompanyCount: 3, deletedPlatformCount: 2, previewCompanyCount: 3, previewPlatformCount: 2 });
    }
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' }); response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/platform-admin.html`, { waitUntil: 'domcontentloaded' });
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#nav [data-view="audit"]').click();
  await page.locator('#auditRetentionStatus').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#auditRetentionStatus').innerText(), 'Kapalı');

  await page.locator('#auditRetentionDays').fill('30');
  await page.locator('#saveAuditRetention').click();
  await page.waitForFunction(() => document.querySelector('#auditRetentionStatus').textContent.includes('30 gün'));
  assert.equal(writes.filter(x => x.path === '/api/platform/audit-retention').length, 1, 'Saving a policy is the only initial write and does not delete logs.');
  assert.equal(writes.some(x => x.path.endsWith('/apply')), false);

  await page.locator('#previewAuditRetention').click();
  await page.locator('#auditRetentionPreview').waitFor({ state: 'visible' });
  assert.match(await page.locator('#auditRetentionPreview').innerText(), /şirket: 3 kayıt · platform: 2 kayıt · toplam: 5/);
  assert.equal(writes.filter(x => x.path.includes('preview')).length, 0, 'Preview is a read-only request.');
  await page.locator('#auditRetentionConfirmation').fill('yanlış');
  assert.equal(await page.locator('#applyAuditRetention').isEnabled(), false);
  await page.locator('#auditRetentionConfirmation').fill('DENETIM-KAYITLARINI-SIL:30');
  assert.equal(await page.locator('#applyAuditRetention').isEnabled(), true);
  await page.locator('#applyAuditRetention').click();
  await page.locator('#platformReauthDialog').waitFor({ state: 'visible' });
  await page.locator('#platformReauthForm [name="password"]').fill('Synthetic-password-42!');
  await page.locator('#platformReauthForm [name="code"]').fill('123456');
  await page.locator('#platformReauthSubmit').click();
  await page.waitForFunction(() => document.querySelector('#auditRetentionFeedback').textContent.includes('şirket 3, platform 2 eski kayıt silindi'));
  assert.deepEqual(writes.filter(x => x.path === '/api/platform/audit-retention/apply').map(x => x.body), [
    { retentionDays: 30, confirmation: 'DENETIM-KAYITLARINI-SIL:30' }
  ]);
  assert.ok(writes.every(x => x.csrf === csrf), 'All policy writes and step-up requests must carry CSRF.');

  await page.setViewportSize({ width: 390, height: 844 });
  const mobile = await page.evaluate(() => ({ width: document.documentElement.scrollWidth, client: document.documentElement.clientWidth,
    layout: ['body', '#appShell', 'aside', '.nav', '.main', '.top', '.content', '.view.active', '#view-audit .panel', '#auditRetentionHeading']
      .map(selector => { const element = document.querySelector(selector); if (!element) return { selector, missing: true };
        const rect = element.getBoundingClientRect(); return { selector, left: Math.round(rect.left), right: Math.round(rect.right), width: Math.round(rect.width), scrollWidth: element.scrollWidth, clientWidth: element.clientWidth };
      })
  }));
  assert.ok(mobile.width <= mobile.client + 1, `Audit retention view must not overflow at 390px: ${JSON.stringify(mobile)}`);
  assert.deepEqual(pageErrors, []);
  console.log('PASS: platform audit retention is disabled by default, saving never deletes, preview is read-only, exact phrase and fresh password+MFA are required, CSRF is sent, and the audit view fits 390px.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
