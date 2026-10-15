import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const planId = '11111111-1111-4111-8111-111111111111';
const plan = {
  id: planId,
  code: 'browser-acceptance-cloud',
  name: 'Synthetic Cloud plan',
  description: 'Synthetic plan for isolated browser acceptance.',
  deploymentKind: 'cloud',
  maxLoginUsers: null,
  monthlyPrice: 1250,
  annualPrice: 12000,
  oneTimePrice: 0,
  currency: 'TRY'
};
let paymentAvailable = false;
let initializeCalls = 0;
let lastInitialize = null;
const html = await readFile('/checkout-site/checkout.html');
const script = await readFile('/checkout-site/checkout.js');

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/checkout.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    response.end(html);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/checkout.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(script);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/plans') {
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify([plan]));
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/checkout/status') {
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({
      available: paymentAvailable,
      provider: paymentAvailable ? 'iyzico' : null,
      message: paymentAvailable ? null : 'Çevrimiçi ödeme şu anda kullanılamıyor. Bilgileriniz gönderilmeden önce ödeme yapılandırmasının tamamlanması gerekiyor.'
    }));
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/session/csrf') {
    response.writeHead(200, { 'content-type': 'application/json', 'cache-control': 'no-store' });
    response.end(JSON.stringify({ requestToken: 'synthetic-csrf-token' }));
    return;
  }
  if (request.method === 'POST' && url.pathname === '/api/checkout/initialize') {
    initializeCalls++;
    let body = '';
    for await (const chunk of request) body += chunk;
    lastInitialize = { headers: request.headers, body: JSON.parse(body) };
    response.writeHead(200, { 'content-type': 'application/json', 'cache-control': 'no-store' });
    response.end(JSON.stringify({ checkoutUrl: 'https://sandbox-api.iyzipay.com/payment/checkout/synthetic', orderId: planId }));
    return;
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
  response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const baseUrl = `http://127.0.0.1:${address.port}`;
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));
await page.route('https://sandbox-api.iyzipay.com/**', route => route.abort());

try {
  await page.goto(`${baseUrl}/checkout.html?planId=${planId}`, { waitUntil: 'domcontentloaded' });
  await page.locator('#checkoutNotice.error').waitFor();
  assert.equal(await page.locator('#checkoutFields').evaluate(element => element.disabled), true,
    'Personal and billing data must remain disabled while checkout configuration is unavailable.');
  assert.equal(await page.locator('#checkoutSubmit').isDisabled(), true);
  assert.match(await page.locator('#checkoutNotice').innerText(), /Çevrimiçi ödeme şu anda kullanılamıyor/);
  assert.equal(await page.locator('#demoLink').getAttribute('href'), '/demo/');
  await page.locator('#checkoutSubmit').click({ force: true });
  assert.equal(initializeCalls, 0, 'Disabled checkout must not send company or billing data.');

  paymentAvailable = true;
  await page.evaluate(() => localStorage.setItem('envantertr-home-language', 'en'));
  await page.goto(`${baseUrl}/checkout.html?planId=${planId}`, { waitUntil: 'domcontentloaded' });
  await page.locator('#checkoutNotice.success').waitFor();
  assert.equal(await page.locator('#checkoutFields').evaluate(element => element.disabled), false);
  assert.equal(await page.locator('#checkoutSubmit').isDisabled(), false);
  assert.match(await page.locator('#planName').innerText(), /Synthetic Cloud plan/);
  assert.equal(await page.locator('#annualTerm').getAttribute('aria-pressed'), 'true');
  await page.locator('#monthlyTerm').click();
  assert.equal(await page.locator('#monthlyTerm').getAttribute('aria-pressed'), 'true');
  assert.equal(await page.locator('#annualTerm').getAttribute('aria-pressed'), 'false');
  await page.locator('#annualTerm').click();

  await page.locator('[name="companyName"]').fill('Synthetic Browser Company');
  assert.equal(await page.locator('[name="slug"]').inputValue(), 'synthetic-browser-company');
  await page.locator('[name="ownerFirstName"]').fill('Synthetic');
  await page.locator('[name="ownerLastName"]').fill('Owner');
  await page.locator('[name="ownerEmail"]').fill('synthetic-owner@example.invalid');
  await page.locator('[name="phone"]').fill('+905551234567');
  await page.locator('[name="identityNumber"]').fill('11111111111');
  await page.locator('[name="billingAddress"]').fill('1 Synthetic Street');
  await page.locator('[name="city"]').fill('Ankara');
  await page.locator('[name="country"]').fill('Türkiye');
  await page.locator('[name="dataProcessingAcknowledged"]').check();
  const trustedProviderRedirect = page.waitForRequest(request => request.url() === 'https://sandbox-api.iyzipay.com/payment/checkout/synthetic', { timeout: 5000 });
  await page.locator('#checkoutSubmit').click();
  await trustedProviderRedirect;

  assert.equal(initializeCalls, 1);
  assert.equal(lastInitialize.headers['x-csrf-token'], 'synthetic-csrf-token');
  assert.equal(lastInitialize.body.planId, planId);
  assert.equal(lastInitialize.body.term, 'annual');
  assert.equal(lastInitialize.body.dataProcessingAcknowledged, true);
  assert.equal(lastInitialize.body.dataProcessingNoticeVersion, 'cloud-checkout-data-v1');
  assert.equal('amount' in lastInitialize.body, false, 'The browser must not supply payment amount.');
  assert.equal('cardNumber' in lastInitialize.body, false, 'Card data must never enter the company checkout form.');
  assert.equal('checkoutId' in lastInitialize.body, true);
  assert.deepEqual(pageErrors, [], 'Checkout page must not throw browser JavaScript exceptions.');
  console.log('PASS: real Chromium checkout UI fails closed when unavailable, supports term selection, validates company data, posts with CSRF and consent, and redirects only to the trusted iyzico host.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}

await import('./command-palette.mjs');
await import('./asset-detail-actions.mjs');
await import('./platform-admin-members.mjs');
await import('./platform-admin-bootstrap.mjs');
await import('./public-site-pricing.mjs');
