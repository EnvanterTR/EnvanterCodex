import assert from 'node:assert/strict';
import { createHmac } from 'node:crypto';
import { chromium } from 'playwright';

let input = '';
for await (const chunk of process.stdin) input += chunk;
const seed = JSON.parse(input);
assert.match(new URL(seed.baseUrl).hostname, /^(localhost|127\.0\.0\.1)$/);
function totp() {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const bits = [...seed.secret.replace(/=+$/, '')].map(char => alphabet.indexOf(char).toString(2).padStart(5, '0')).join('');
  const bytes = [];
  for (let i = 0; i + 8 <= bits.length; i += 8) bytes.push(parseInt(bits.slice(i, i + 8), 2));
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000)));
  const digest = createHmac('sha1', Buffer.from(bytes)).update(counter).digest();
  return ((digest.readUInt32BE(digest.at(-1) & 15) & 0x7fffffff) % 1000000).toString().padStart(6, '0');
}
async function login(context, email, code) {
  const csrfResponse = await context.request.get(`${seed.baseUrl}/api/session/csrf`);
  assert.equal(csrfResponse.status(), 200);
  const csrf = await csrfResponse.json();
  const loginResponse = await context.request.post(`${seed.baseUrl}/api/session/login`, {
    headers: { 'X-CSRF-TOKEN': csrf.requestToken },
    data: { email, password: seed.password, companyId: seed.companyId, twoFactorCode: code }
  });
  assert.equal(loginResponse.status(), 200, 'Real cookie login must succeed.');
}
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
try {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  page.setDefaultTimeout(10000);
  const errors = [];
  let trustedRedirects = 0;
  page.on('pageerror', error => errors.push(error.message));
  await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    assert.equal(url.origin, seed.baseUrl, 'The isolated browser must not make external network requests.');
    if (url.pathname === '/__synthetic-provider')
      return route.fulfill({ status: 200, contentType: 'text/html', body: '<h1>Synthetic payment provider</h1>' });
    if (url.pathname === '/subscription/checkout' && route.request().method() === 'POST') {
      const response = await route.fetch({ maxRedirects: 0 });
      if (response.status() === 302) {
        assert.equal(response.headers().location, 'https://sandbox-api.iyzipay.com/checkoutform/isolated-browser');
        trustedRedirects++;
        // Verify the application's real Location, then keep the browser on the isolated local provider stand-in.
        return route.fulfill({ response, headers: { ...response.headers(), location: `${seed.baseUrl}/__synthetic-provider` } });
      }
      return route.fulfill({ response });
    }
    return route.continue();
  });
  await login(context, seed.ownerEmail, totp());
  await page.goto(`${seed.baseUrl}/subscription/checkout?lang=en&theme=dark`);
  assert.equal(await page.locator('html').getAttribute('lang'), 'en');
  assert.equal(await page.locator('html').getAttribute('data-theme'), 'dark');
  assert.match(await page.locator('main').innerText(), /4,200\.00 TRY/);
  assert.match(await page.locator('main').innerText(), /350\.00 TRY/);
  assert.equal(await page.locator('fieldset').getAttribute('disabled'), null, 'Expired trial owner can purchase.');
  const fields = { firstName: 'Synthetic', lastName: 'Owner', phone: '+905550000002', identityNumber: '10000000146',
    city: 'Istanbul', country: 'Turkey', billingAddress: 'Synthetic Street 2' };
  for (const [name, value] of Object.entries(fields)) await page.locator(`[name="${name}"]`).fill(value);
  await page.locator('[name="currentPassword"]').fill('Synthetic-Wrong-Password!');
  await page.locator('[name="twoFactorCode"]').fill('000000');
  await page.locator('[name="acknowledged"]').check();
  const rejected = page.waitForResponse(response => response.url().startsWith(`${seed.baseUrl}/subscription/checkout`) && response.request().method() === 'POST');
  await page.locator('#annualCheckoutSubmit').click();
  assert.equal((await rejected).status(), 400);
  await page.locator('[role="alert"]').waitFor();
  assert.equal(await page.locator('[name="firstName"]').inputValue(), 'Synthetic');
  assert.equal(await page.locator('[name="billingAddress"]').inputValue(), 'Synthetic Street 2');
  assert.equal(await page.locator('[name="currentPassword"]').inputValue(), '');
  assert.equal(await page.locator('[name="twoFactorCode"]').inputValue(), '');
  assert.equal((await page.content()).includes('Synthetic-Wrong-Password!'), false);
  await page.setViewportSize({ width: 390, height: 844 });
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, '390px layout must fit.');
  await page.screenshot({ path: '/evidence/checkout-390.png', fullPage: true });
  const checkoutId = await page.locator('[name="checkoutId"]').inputValue();
  await page.locator('[name="currentPassword"]').fill(seed.password);
  await page.locator('[name="twoFactorCode"]').fill(totp());
  await page.locator('[name="acknowledged"]').check();
  await page.locator('#annualCheckoutSubmit').click();
  await page.waitForURL(`${seed.baseUrl}/__synthetic-provider`);
  assert.equal(trustedRedirects, 1);
  const orderBefore = await context.request.get(`${seed.baseUrl}/api/subscription/checkout/orders/${checkoutId}`);
  assert.equal((await orderBefore.json()).status, 'PaymentStarted');
  for (let replay = 0; replay < 2; replay++) {
    const callback = await context.request.post(`${seed.baseUrl}/api/checkout/callback`, { form: { token: seed.token } });
    assert.equal(callback.status(), 200);
  }
  const orderAfter = await context.request.get(`${seed.baseUrl}/api/subscription/checkout/orders/${checkoutId}`);
  assert.equal((await orderAfter.json()).status, 'Provisioned');
  await page.goto(`${seed.baseUrl}/subscription/checkout?lang=en&theme=dark`);
  assert.match(await page.locator('table').innerText(), /Subscription active/);
  assert.deepEqual(errors, []);
  const viewer = await browser.newContext();
  await login(viewer, seed.viewerEmail);
  const denied = await viewer.request.get(`${seed.baseUrl}/subscription/checkout`);
  assert.equal(denied.status(), 403);
  const crossOrder = await viewer.request.get(`${seed.baseUrl}/api/subscription/checkout/orders/${checkoutId}`);
  assert.equal(crossOrder.status(), 403);
  console.log('PASS: actual Kestrel + PostgreSQL + Chromium: real owner password/MFA cookie login, expired-trial annual page, monthly equivalent, English/dark/390px layout, failed-password error with billing retention and blank secrets, browser form redirect, callback/replay, active history, viewer denial and zero browser errors. Provider is synthetic; no external payment or email.');
} finally { await browser.close(); }
