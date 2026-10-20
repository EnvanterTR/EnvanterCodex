import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const kitId = '33333333-3333-4333-8333-333333333333';
const stockOrderId = '99999999-9999-4999-8999-999999999999';
const stockOrderLines = {
  consumable: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  component: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'
};
const ids = {
  accessory: '44444444-4444-4444-8444-444444444444',
  consumable: '55555555-5555-4555-8555-555555555555',
  asset: '66666666-6666-4666-8666-666666666666',
  component: '77777777-7777-4777-8777-777777777777'
};
const csrfToken = 'synthetic-kits-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const requests = [];
const kits = [];
const writes = [];
const catalog = {
  accessories: [{ id: ids.accessory, name: 'Synthetic headset', totalQuantity: 9, checkedOut: 1 }],
  consumables: [{ id: ids.consumable, name: 'Synthetic toner', totalQuantity: 12, issued: 2 }],
  assets: [{ id: ids.asset, name: 'Synthetic laptop', assetTag: 'QA-KIT-001', status: 'Available', assignedEmployeeId: null }],
  components: [{ id: ids.component, name: 'Synthetic dock', totalQuantity: 8, allocated: 0 }]
};
const json = (response, status, body) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};
const summarizeKit = kit => ({ ...kit, itemCount: kit.items.length,
  totalUnits: kit.items.reduce((sum, item) => sum + item.quantity, 0), availableKits: 1,
  items: kit.items.map(item => ({ ...item, remaining: item.itemType === 'Asset' ? 1 : 6,
    accessoryId: item.itemType === 'Accessory' ? item.itemId : null,
    itemName: catalog[({ Accessory: 'accessories', Consumable: 'consumables', Asset: 'assets', Component: 'components' })[item.itemType]]
      .find(entry => entry.id === item.itemId)?.name || 'Synthetic item', modelNumber: null, itemNumber: null, requestable: true })) });

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' }); response.end(appHtml); return;
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
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }
  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    const chunks = []; request.on('data', chunk => chunks.push(Buffer.from(chunk)));
    request.on('end', () => resolve(chunks.length ? JSON.parse(Buffer.concat(chunks).toString('utf8')) : null));
  });
  const record = { method: request.method, path: url.pathname, query: Object.fromEntries(url.searchParams.entries()), body, csrf: request.headers['x-csrf-token'] };
  requests.push(record);

  if (request.method === 'GET' && url.pathname === '/api/session/csrf') return json(response, 200, { requestToken: csrfToken });
  if (request.method === 'GET' && url.pathname === '/api/session') return json(response, 200, {
    companyId, companyName: 'Synthetic Kit Company', userId, name: 'Synthetic Owner', role: 'owner', hasPhoto: false,
    availableCompanies: [{ id: companyId, name: 'Synthetic Kit Company', role: 'owner' }],
    subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic plan' }
  });
  if (request.method === 'GET' && url.pathname === '/api/company/branding') return json(response, 200, { name: 'Synthetic Kit Company', brandPrimaryColor: '#2563EB', hasLogo: false });
  if (request.method === 'PUT' && url.pathname === '/api/appearance/preferences') {
    assert.equal(record.csrf, csrfToken, 'Changing the UI locale must use CSRF.');
    return json(response, 200, { theme: body.theme, language: body.language });
  }

  const family = url.pathname.match(/^\/api\/(accessories|consumables|components|assets)$/)?.[1];
  if (request.method === 'GET' && family) return json(response, 200, { items: catalog[family], total: catalog[family].length, page: 1, pageSize: 200 });
  if (request.method === 'GET' && url.pathname === '/api/accessory-kits') {
    const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
    const matching = kits.filter(kit => !q || `${kit.name} ${kit.description || ''}`.toLocaleLowerCase('tr-TR').includes(q));
    return json(response, 200, { items: matching.map(summarizeKit), total: matching.length });
  }
  if (request.method === 'GET' && url.pathname === '/api/stock-orders') return json(response, 200, {
    items: [{ id: stockOrderId, orderNumber: 'QA-ST-001', supplier: 'Synthetic supplier', status: 'Received', orderDate: '2026-10-16', expectedOn: null,
      currency: 'TRY', quantityReceived: 10, quantity: 12, lineCount: 2, totalCost: 100 }], total: 1, page: 1, pageSize: 25
  });
  if (request.method === 'GET' && url.pathname === `/api/stock-orders/${stockOrderId}`) return json(response, 200, {
    order: { id: stockOrderId, orderNumber: 'QA-ST-001', supplier: 'Synthetic supplier', status: 'Received', orderDate: '2026-10-16', expectedOn: null,
      currency: 'TRY', notes: null, discountAmount: 0, taxRatePercent: 0, freightAmount: 0 },
    lines: [
      { id: stockOrderLines.consumable, itemType: 'Consumable', itemId: ids.consumable, itemName: 'Synthetic toner', itemNumber: 'TONER-1', quantity: 8, quantityReceived: 6, quantityReturned: 1, remaining: 2, unitCost: 10, totalCost: 80, landedUnitCost: 10 },
      { id: stockOrderLines.component, itemType: 'Component', itemId: ids.component, itemName: 'Synthetic dock', itemNumber: 'DOCK-1', quantity: 4, quantityReceived: 4, quantityReturned: 0, remaining: 0, unitCost: 5, totalCost: 20, landedUnitCost: 5 }
    ], attachments: [], quantity: 12, quantityReceived: 10, subtotal: 100, taxAmount: 0, totalCost: 100
  });
  if (request.method === 'GET' && url.pathname === `/api/stock-orders/${stockOrderId}/returns`) return json(response, 200, []);
  if (request.method === 'POST' && url.pathname === `/api/stock-orders/${stockOrderId}/kit-template`) {
    assert.equal(record.csrf, csrfToken, 'Creating a purchase-order kit template must use CSRF.');
    const created = { id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', name: body.name, description: body.description,
      isActive: true, createdAt: '2026-10-16T12:00:00Z', updatedAt: '2026-10-16T12:00:00Z', items: [
        { itemType: 'Consumable', itemId: ids.consumable, quantity: body.items[0].quantityPerKit },
        { itemType: 'Component', itemId: ids.component, quantity: body.items[1].quantityPerKit },
        { itemType: 'Asset', itemId: body.assetId, quantity: 1 }
      ] };
    kits.push(created); writes.push(record);
    return json(response, 201, { id: created.id, name: created.name, sourcePurchaseOrderId: stockOrderId, stockChanged: false });
  }
  if (request.method === 'POST' && url.pathname === '/api/accessory-kits') {
    assert.equal(record.csrf, csrfToken, 'Creating a mixed kit must use CSRF.');
    const created = { id: kitId, ...body, isActive: true, createdAt: '2026-10-16T12:00:00Z', updatedAt: '2026-10-16T12:00:00Z' };
    kits.push(created); writes.push(record); return json(response, 201, { id: kitId, name: created.name });
  }
  const edit = url.pathname.match(/^\/api\/accessory-kits\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && edit) {
    assert.equal(record.csrf, csrfToken, 'Editing a mixed kit must use CSRF.');
    const kit = kits.find(item => item.id === edit[1]); assert.ok(kit, 'The mixed kit must exist before editing.');
    Object.assign(kit, body, { updatedAt: '2026-10-16T12:01:00Z' }); writes.push(record);
    return json(response, 200, { id: kit.id, name: kit.name });
  }
  if (request.method === 'GET' && url.pathname === `/api/accessory-kits/${kitId}/history`) {
    return json(response, 200, { kitId, kitName: kits[0]?.name, items: ['Accessory', 'Consumable', 'Asset', 'Component'].map((itemType, index) => ({
      id: `88888888-8888-4888-8888-88888888888${index}`, itemType, itemName: `Synthetic ${itemType}`,
      targetType: 'Employee', target: 'Synthetic employee', actor: 'Synthetic Owner', quantity: index ? 2 : 1,
      returnedQuantity: index === 0 ? 1 : 0, remaining: index ? 2 : 0, checkedOutAt: `2026-10-${16 - index}T12:00:00Z`, notes: 'Synthetic kit history'
    })), total: 4 });
  }
  if (request.method === 'GET' && url.pathname === '/api/accessory-kits/export.csv') {
    response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename=synthetic-kits.csv', 'cache-control': 'no-store' });
    response.end(`Kit,Açıklama,Durum,İçerik\r\n${kits.map(kit => `${kit.name},${kit.description || ''},Etkin,${summarizeKit(kit).items.map(item => `${item.itemName} x ${item.quantity}`).join(' | ')}`).join('\r\n')}\r\n`); return;
  }
  if (request.method === 'GET') {
    if (url.pathname === '/api/status') return json(response, 200, { status: 'ready', migrationCount: 100, pendingMigrationCount: 0 });
    if (url.pathname === '/api/setup/status') return json(response, 200, { setupRequired: false });
    return json(response, 200, { items: [], total: 0, page: 1, pageSize: 50 });
  }
  response.writeHead(404); response.end();
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address(); assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = []; page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="supplies"]').click();
  await page.locator('#sidebar [data-page="Hazır kitler"]').click();
  await page.locator('#accessoryKitsPage').waitFor({ state: 'visible' });
  await page.locator('#newAccessoryKitButton').click();
  const dialog = page.locator('#accessoryKitFormDialog'); await dialog.waitFor({ state: 'visible' });
  const rows = page.locator('#accessoryKitLineEditor > .kit-line-row');
  const selections = rows.locator('[data-kit-accessory]');
  await selections.nth(0).selectOption(`Accessory:${ids.accessory}`);
  for (const [index, type, id] of [[1, 'Consumable', ids.consumable], [2, 'Asset', ids.asset], [3, 'Component', ids.component]]) {
    await page.locator('#addAccessoryKitLine').click();
    await selections.nth(index).selectOption(`${type}:${id}`);
  }
  assert.equal(await rows.nth(2).locator('[data-kit-quantity]').inputValue(), '1');
  assert.equal(await rows.nth(2).locator('[data-kit-quantity]').evaluate(input => input.readOnly), true, 'A unique asset line must have a fixed quantity of one.');
  await dialog.locator('[name="name"]').fill('Synthetic mixed onboarding kit');
  await dialog.locator('[name="description"]').fill('All four inventory families');
  const createRequest = page.waitForRequest(request => request.method() === 'POST' && new URL(request.url()).pathname === '/api/accessory-kits');
  await dialog.locator('button[type="submit"]').click(); await createRequest;
  await page.locator('#accessoryKitRows').getByText('Synthetic mixed onboarding kit').waitFor();
  assert.equal(writes.length, 1);
  assert.equal(writes[0].csrf, csrfToken);
  assert.deepEqual(writes[0].body.items.map(item => `${item.itemType}:${item.itemId}:${item.quantity}`), [
    `Accessory:${ids.accessory}:1`, `Consumable:${ids.consumable}:1`, `Asset:${ids.asset}:1`, `Component:${ids.component}:1`
  ]);
  assert.match(await page.locator('#accessoryKitRows').innerText(), /Synthetic headset.*Synthetic toner.*Synthetic laptop.*Synthetic dock/s);

  const exportPromise = page.waitForEvent('download');
  await page.locator('#exportAccessoryKitsButton').click();
  const download = await exportPromise; assert.equal(download.suggestedFilename(), `envantertr-hazir-kitler-${new Date().toISOString().slice(0, 10)}.csv`);
  assert.ok(requests.some(item => item.method === 'GET' && item.path === '/api/accessory-kits/export.csv'));

  await page.locator('#accessoryKitRows').getByRole('button', { name: 'Geçmiş' }).click();
  const history = page.locator('#accessoryKitHistoryDialog'); await history.waitFor({ state: 'visible' });
  for (const type of ['Aksesuar', 'Sarf', 'Varlık', 'Bileşen']) await history.getByText(type, { exact: true }).waitFor();
  await page.locator('#closeAccessoryKitHistory').click(); await history.waitFor({ state: 'hidden' });

  await page.locator('#accessoryKitRows').getByRole('button', { name: 'Düzenle' }).click();
  await dialog.waitFor({ state: 'visible' });
  assert.equal(await rows.count(), 4);
  assert.equal(await selections.nth(3).inputValue(), `Component:${ids.component}`);
  await dialog.locator('[name="name"]').fill('Synthetic mixed kit revised');
  const editRequest = page.waitForRequest(request => request.method() === 'PUT' && new URL(request.url()).pathname === `/api/accessory-kits/${kitId}`);
  await dialog.locator('button[type="submit"]').click(); await editRequest;
  await page.locator('#accessoryKitRows').getByText('Synthetic mixed kit revised').waitFor();

  await page.locator('#sidebar [data-page="Siparişler"]').click();
  await page.locator('#openStockOrders').click();
  const stockOrderDetailRequest = page.waitForRequest(request => request.method() === 'GET' && new URL(request.url()).pathname === `/api/stock-orders/${stockOrderId}`);
  await page.locator(`#stockOrderRows .stock-order-open[data-id="${stockOrderId}"]`).first().click(); await stockOrderDetailRequest;
  await page.locator('#stockOrderDetailDialog').waitFor({ state: 'visible' });
  const kitTemplateButton = page.locator('#createKitFromStockOrder'); await kitTemplateButton.waitFor({ state: 'visible' });
  assert.equal(await kitTemplateButton.isVisible(), true);
  await kitTemplateButton.click();
  const templateDialog = page.locator('#stockOrderKitTemplateDialog'); await templateDialog.waitFor({ state: 'visible' });
  assert.match(await templateDialog.innerText(), /sipariş stoğunu değiştirmez/i);
  await templateDialog.locator('[name="name"]').fill('Synthetic PO onboarding kit');
  await templateDialog.locator('[name="assetId"]').selectOption(ids.asset);
  const templateRows = templateDialog.locator('[data-kit-order-line]');
  assert.equal(await templateRows.count(), 2, 'Only received and not-returned lines are eligible.');
  await templateDialog.locator(`[data-kit-order-quantity="${stockOrderLines.consumable}"]`).fill('2');
  await templateDialog.locator(`[data-kit-order-quantity="${stockOrderLines.component}"]`).fill('1');
  const templateRequest = page.waitForRequest(request => request.method() === 'POST' && new URL(request.url()).pathname === `/api/stock-orders/${stockOrderId}/kit-template`);
  await templateDialog.locator('button[type="submit"]').click(); await templateRequest;
  await page.waitForFunction(() => document.querySelector('#toast')?.textContent.includes('Sipariş stoğu değiştirilmedi'));
  const templateWrite = writes.find(item => item.path.endsWith('/kit-template'));
  assert.equal(templateWrite.csrf, csrfToken);
  assert.deepEqual(templateWrite.body, { name: 'Synthetic PO onboarding kit', description: null, assetId: ids.asset,
    items: [{ lineId: stockOrderLines.consumable, quantityPerKit: 2 }, { lineId: stockOrderLines.component, quantityPerKit: 1 }] });
  assert.match(await page.locator('#toast').innerText(), /Sipariş stoğu değiştirilmedi/);

  await page.locator('#appearanceButton').click();
  await page.locator('#languagePreference').selectOption('en');
  const languageSave = page.waitForRequest(request => request.method() === 'PUT' && new URL(request.url()).pathname === '/api/appearance/preferences');
  await page.locator('#appearanceSave').click(); await languageSave;
  await page.locator('#appearanceDialog').waitFor({ state: 'hidden' });
  await page.locator(`#stockOrderRows .stock-order-open[data-id="${stockOrderId}"]`).first().click();
  await page.locator('#stockOrderDetailDialog').waitFor({ state: 'visible' });
  assert.match(await page.locator('#createKitFromStockOrder').innerText(), /Create kit recipe from this order/);
  await page.locator('#createKitFromStockOrder').click();
  await page.locator('#stockOrderKitTemplateDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#stockOrderKitTemplateDialog h2').innerText(), 'Create kit recipe');
  assert.match(await page.locator('#stockOrderKitTemplateLines').innerText(), /Consumable.*received from the order.*Component.*received from the order/s);
  await page.locator('[data-close-stock-order-kit]').last().click();
  assert.deepEqual(pageErrors, [], 'The mixed kit menu and dialogs must not throw browser errors.');
  console.log('PASS: Chromium covers mixed-kit CRUD/history/export plus purchase-order kit-recipe navigation, received/returned line eligibility, per-kit quantities, asset requirement, CSRF write, no-stock-change feedback, and English localization.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
