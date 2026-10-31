import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const id = '11111111-1111-4111-8111-111111111111';
const html = await readFile('/checkout-site/checkout.html');
const script = await readFile('/checkout-site/checkout.js');
let limit = 4;
let paymentWrites = 0;
const server = createServer((request, response) => {
  const path = new URL(request.url, 'http://localhost').pathname;
  if (request.method === 'POST') paymentWrites++;
  const routes = {
    '/checkout.html': ['text/html; charset=utf-8', html],
    '/checkout.js': ['text/javascript; charset=utf-8', script],
    '/api/plans': ['application/json', JSON.stringify([{ id, name: 'Quota acceptance', deploymentKind: 'cloud',
      maxLoginUsers: limit, annualPrice: 4200, monthlyPrice: 0, currency: 'TRY' }])],
    '/api/checkout/status': ['application/json', '{"available":true}']
  };
  const route = routes[path];
  response.writeHead(route ? 200 : 404, { 'content-type': route?.[0] ?? 'text/plain' });
  response.end(route?.[1] ?? 'Not found');
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const base = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
try {
  for (const [language, unlimited] of [['tr','Sınırsız'], ['en','Unlimited'], ['mk','Неограничено'], ['ru','Без ограничений'], ['uk','Без обмежень']]) {
    const context = await browser.newContext();
    await context.addInitScript(lang => localStorage.setItem('envantertr-home-language', lang), language);
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    for (const value of [4, 1, 10, 2000, null, undefined]) {
      limit = value;
      await page.goto(`${base}/checkout.html?planId=${id}`);
      await page.locator('#checkoutNotice.success').waitFor();
      const shown = (await page.locator('.summary-line').last().locator('strong').innerText()).trim();
      if (value === null || value === undefined)
        assert.equal(shown, value === null ? unlimited : '—', `${language}: unlimited and missing limits must remain distinct.`);
      else {
        // Node and Chromium may ship different ICU locale sets; compare the displayed numeric entitlement.
        assert.match(shown, /^[0-9.,\s]+$/, `${language}: the user quota must be a number, not an unlimited claim.`);
        assert.equal(shown.replace(/\D/g, ''), String(value), `${language}: the displayed quota must match the published plan.`);
      }
      assert.equal(await page.locator('#planDescription').innerText(), '', 'A missing description must not invent an unlimited entitlement.');
    }
    assert.deepEqual(errors, []);
    await context.close();
  }
  assert.equal(paymentWrites, 0, 'Viewing a plan quota must not initiate payment.');
  console.log('PASS: Chromium shows published 1/4/10/2000 user limits, explicitly unlimited plans and unknown-limit placeholders in all five checkout languages, without payment writes.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
