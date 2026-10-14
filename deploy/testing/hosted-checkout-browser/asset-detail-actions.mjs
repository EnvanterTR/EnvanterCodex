import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const assetId = '33333333-3333-4333-8333-333333333333';
const asset = {
  id: assetId, companyId, companyName: 'Synthetic Asset Company', assetTag: 'QA-ASSET-001',
  name: 'Synthetic audit target', status: 'Available', serialNumber: 'QA-SERIAL-001',
  categoryId: '55555555-5555-4555-8555-555555555555', location: 'Head Office', updatedAt: '2026-10-14T12:00:00Z', nextAuditOn: null,
  createdAt: '2026-10-01T12:00:00Z', purchaseCost: null, currency: 'TRY'
};
const appHtml = await readFile('/checkout-site/company-app.html');
const requests = [];
const apiRequests = [];
const assetHistory = [];
const json = (response, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(response.body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(appHtml);
    return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end('window.qrcodegen = window.qrcodegen || {};'); return;
  }
  if (!url.pathname.startsWith('/api/')) {
    response.writeHead(404); response.end('Not found'); return;
  }

  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    let raw = ''; request.setEncoding('utf8'); request.on('data', chunk => raw += chunk);
    request.on('end', () => resolve(raw ? JSON.parse(raw) : null));
  });
  const record = { method: request.method, path: url.pathname, body, csrf: request.headers['x-csrf-token'] };
  requests.push(record);
  if (request.method !== 'GET') apiRequests.push(record);

  if (request.method === 'GET') {
    let result;
    if (url.pathname === '/api/status') result = { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 };
    else if (url.pathname === '/api/setup/status') result = { setupRequired: false };
    else if (url.pathname === '/api/session/csrf') result = { requestToken: 'synthetic-asset-csrf' };
    else if (url.pathname === '/api/session') result = {
      companyId, companyName: 'Synthetic Asset Company', userId, name: 'Synthetic Owner', role: 'owner',
      hasPhoto: false, subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic cloud plan' }
    };
    else if (url.pathname === '/api/company/branding') result = { name: 'Synthetic Asset Company', brandPrimaryColor: '#2563EB', hasLogo: false };
    else if (url.pathname === '/api/company/label-settings') result = { isConfigured: false, settings: null };
    else if (url.pathname === '/api/dashboard/summary') result = {
      totalAssets: 1, assignedAssets: 0, availableAssets: 1, maintenanceAssets: 0,
      softwareLicenses: 0, accessories: 0, consumables: 0, components: 0, activeEmployees: 0
    };
    else if (url.pathname === '/api/assets') result = { items: [asset], total: 1, page: 1, pageSize: 50 };
    else if (url.pathname === `/api/assets/${assetId}`) result = {
      asset: { ...asset }, references: { model: null, modelNumber: null, employeeNumber: null, defaultLocation: 'Head Office' },
      history: [...assetHistory], attachments: [], accessories: [], components: [], softwareLicenses: [], relatedAssets: [],
      valuation: {}, purchaseOrder: null
    };
    else if (url.pathname === `/api/assets/${assetId}/maintenances`) result = { items: [], total: 0 };
    else if (url.pathname === '/api/assets/custom-fields') result = [];
    else if (url.pathname === '/api/lookups/categories') result = [{ id: asset.categoryId, name: 'Synthetic equipment', assetCount: 1 }];
    else if (url.pathname === '/api/lookups/brands' || url.pathname === '/api/lookups/models') result = [];
    else if (url.pathname === '/api/locations') result = { items: [] };
    else if (url.pathname.startsWith('/api/assets/audit-schedule')) result = { items: [], total: 0, overdue: 0, dueSoon: 0 };
    else if (url.pathname === '/api/activity') result = { entries: [], total: 0 };
    else if (url.pathname === '/api/calendar') result = { items: [], total: 0 };
    else result = { items: [], total: 0, page: 1, pageSize: 50 };
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(JSON.stringify(result));
    return;
  }

  if (request.headers['x-csrf-token'] !== 'synthetic-asset-csrf') {
    response.writeHead(400, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ message: 'Missing synthetic CSRF token.' })); return;
  }
  if (request.method === 'PUT' && url.pathname === `/api/assets/${assetId}/audit-schedule`) {
    asset.nextAuditOn = body?.nextAuditOn || null;
    response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify({ nextAuditOn: asset.nextAuditOn }));
    return;
  }
  if (request.method === 'POST' && url.pathname === `/api/assets/${assetId}/audit`) {
    asset.nextAuditOn = body?.nextAuditOn || null;
    asset.updatedAt = new Date().toISOString();
    assetHistory.unshift({
      id: '44444444-4444-4444-8444-444444444444', eventType: 'Audited', occurredAt: asset.updatedAt,
      details: `Fiziksel denetim tamamlandı · ${body.auditDate}${body.notes ? ` · ${body.notes}` : ''}`,
      actorName: 'Synthetic Owner', attachmentCount: 0
    });
    response.writeHead(204); response.end();
    return;
  }
  if (request.method === 'PATCH' && url.pathname === `/api/assets/${assetId}`) {
    if (body?.expectedUpdatedAt !== asset.updatedAt) {
      response.writeHead(409, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ message: 'Asset changed since the form was opened.' })); return;
    }
    asset.assetTag = body.assetTag;
    asset.name = body.name;
    asset.categoryId = body.categoryId;
    asset.serialNumber = body.serialNumber;
    asset.location = body.location;
    asset.updatedAt = '2026-10-14T13:00:00Z';
    assetHistory.unshift({ id: '66666666-6666-4666-8666-666666666666', eventType: 'Updated', occurredAt: asset.updatedAt,
      details: `Varlık bilgileri güncellendi · ${asset.name}`, actorName: 'Synthetic Owner', attachmentCount: 0 });
    response.writeHead(200, { 'content-type': 'application/json' }); response.end(JSON.stringify({ changed: true }));
    return;
  }
  response.writeHead(405, { 'content-type': 'application/json' });
  response.end(JSON.stringify({ message: 'Unexpected write in asset-detail browser acceptance.' }));
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('[data-group="inventory"]').click();
  await page.locator('[data-page="Varlıklar"]').first().click();
  try {
    await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).waitFor({ state: 'visible', timeout: 15000 });
  } catch (error) {
    console.error('ASSET_LIST_STATE', JSON.stringify({
      bodyClass: await page.locator('body').getAttribute('class'),
      authError: await page.locator('#authError').innerText().catch(() => ''),
      assetRows: await page.locator('#assetRows').innerText().catch(() => ''),
      pageTitle: await page.locator('#pageTitle').innerText().catch(() => ''),
      requests: requests.map(({ method, path }) => ({ method, path })), pageErrors
    }));
    throw error;
  }
  await page.locator('#assetRows .asset-name').filter({ hasText: asset.name }).click();
  const assetDialog = page.locator('#assetDialog');
  await assetDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#dialogTitle').innerText(), asset.name);

  const editDialog = page.locator('#editAssetDialog');
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  const editForm = page.locator('#editAssetForm');
  assert.equal(await editForm.locator('[name="assetName"]').inputValue(), 'Synthetic audit target');
  assert.equal(await editForm.locator('[name="categoryId"]').inputValue(), asset.categoryId);
  await page.locator('[data-close-edit-asset]').first().click();
  await editDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, 0, 'Canceling asset edit must not write.');

  const editedName = 'Synthetic asset edit persisted';
  await page.locator('#editAssetButton').click();
  await editDialog.waitFor({ state: 'visible' });
  await editForm.locator('[name="assetName"]').fill(editedName);
  assert.equal(await editForm.locator('[name="assetName"]').evaluate(input => input.checkValidity()), true);
  await editForm.locator('button[type="submit"]').click();
  await editDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(apiRequests.length, 1, 'Only the explicit asset edit should write before the audit flows.');
  assert.equal(apiRequests[0].method, 'PATCH');
  assert.equal(apiRequests[0].path, `/api/assets/${assetId}`);
  assert.equal(apiRequests[0].body.name, editedName);
  assert.equal(apiRequests[0].body.expectedUpdatedAt, '2026-10-14T12:00:00Z');
  assert.equal(apiRequests[0].csrf, 'synthetic-asset-csrf');
  await page.locator('#assetDialog').locator('#closeDialog').click();
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).waitFor({ state: 'visible' });
  await page.locator('#assetRows .asset-name').filter({ hasText: editedName }).click();
  await page.waitForFunction(name => document.querySelector('#assetDialog')?.open && document.querySelector('#dialogTitle')?.textContent === name, editedName);
  assert.equal(asset.name, editedName, 'Reloaded list and detail must reflect the persisted server result.');

  await page.locator('#scheduleAssetAuditButton').click();
  const scheduleDialog = page.locator('#assetAuditScheduleDialog');
  await scheduleDialog.waitFor({ state: 'visible' });
  const beforeScheduleCancel = apiRequests.length;
  await page.locator('[data-close-audit-schedule]').first().click();
  await scheduleDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeScheduleCancel, 'Canceling the schedule must not send a write request.');

  await page.locator('#scheduleAssetAuditButton').click();
  const date = new Date(); date.setDate(date.getDate() + 7);
  const nextAuditOn = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').fill(nextAuditOn);
  assert.equal(await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').inputValue(), nextAuditOn);
  assert.equal(await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').evaluate(input => input.checkValidity()), true);
  await page.locator('#assetAuditScheduleForm button[type="submit"]').click();
  try { await scheduleDialog.waitFor({ state: 'hidden', timeout: 10000 }); }
  catch (error) {
    console.error('AUDIT_SCHEDULE_STATE', JSON.stringify({
      apiRequests, requests: requests.map(({ method, path, body }) => ({ method, path, body })),
      dateValue: await page.locator('#assetAuditScheduleForm [name="nextAuditOn"]').inputValue(),
      feedback: await page.locator('[role="status"]').allTextContents(), pageErrors
    }));
    throw error;
  }
  await page.waitForFunction(expected => document.querySelector('#detailNextAuditOn')?.textContent !== 'Planlanmadı' && document.querySelector('#assetDialog')?.open, nextAuditOn);

  assert.equal(apiRequests.length, beforeScheduleCancel + 1, 'Only the explicit asset edit and schedule should write.');
  assert.equal(apiRequests.at(-1).method, 'PUT');
  assert.equal(apiRequests.at(-1).path, `/api/assets/${assetId}/audit-schedule`);
  assert.deepEqual(apiRequests.at(-1).body, { nextAuditOn });
  assert.equal(apiRequests.at(-1).csrf, 'synthetic-asset-csrf');
  assert.equal(asset.nextAuditOn, nextAuditOn, 'Reopened detail must reflect the persisted server result.');

  const beforeAuditCancel = apiRequests.length;
  await page.locator('#auditAssetButton').click();
  const auditDialog = page.locator('#assetAuditDialog');
  await auditDialog.waitFor({ state: 'visible' });
  await page.locator('[data-close-asset-audit]').first().click();
  await auditDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.length, beforeAuditCancel, 'Canceling a physical audit must not send a write request.');

  await page.locator('#auditAssetButton').click();
  const auditDate = await page.evaluate(() => {
    const date = new Date(); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); return date.toISOString().slice(0, 10);
  });
  const auditNotes = 'Synthetic audit saved from Chromium';
  await page.locator('#assetAuditForm [name="auditDate"]').fill(auditDate);
  await page.locator('#assetAuditForm [name="nextAuditOn"]').fill(nextAuditOn);
  await page.locator('#assetAuditForm [name="notes"]').fill(auditNotes);
  await page.locator('#assetAuditForm button[type="submit"]').click();
  await auditDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#assetDialog')?.open && document.querySelector('#assetHistory .asset-history-item')?.textContent.includes('Synthetic audit saved from Chromium'));
  assert.equal(apiRequests.length, beforeAuditCancel + 1, 'Only the explicitly saved physical audit should write.');
  const auditRequest = apiRequests.at(-1);
  assert.equal(auditRequest.method, 'POST');
  assert.equal(auditRequest.path, `/api/assets/${assetId}/audit`);
  assert.deepEqual(auditRequest.body, { auditDate, nextAuditOn, notes: auditNotes });
  assert.equal(auditRequest.csrf, 'synthetic-asset-csrf');
  assert.equal(asset.nextAuditOn, nextAuditOn);
  assert.equal(assetHistory[0].eventType, 'Audited');
  assert.match(await page.locator('#assetHistory').innerText(), /Synthetic audit saved from Chromium/);
  assert.deepEqual(pageErrors, [], `The asset detail UI must not throw: ${pageErrors.join(' | ')}`);
  console.log('PASS: asset edit, audit schedule, and physical audit cancel/save flows; one tenant-scoped PATCH/PUT/POST per explicit save with CSRF; refreshed list/detail preserve edited name, schedule, and audit history; no browser exceptions.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
