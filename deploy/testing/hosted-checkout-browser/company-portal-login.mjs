import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/checkout-site/company-app.html', 'utf8');
const browser = await chromium.launch({ headless: true });
try {
  for (const scenario of [
    { entryHost: 'envantertr.com', login: false },
    { entryHost: 'www.envantertr.com', login: false },
    { entryHost: 'envantertr.com', login: true }
  ]) {
    const { entryHost } = scenario;
    let authenticated = !scenario.login;
    let loginCount = 0;
    const page = await browser.newPage();
    page.on('pageerror', error => console.error(error.message));
    const requests = [];
    await page.route('**/*', async route => {
      const url = new URL(route.request().url());
      requests.push(url.href);
      if (url.hostname === 'farma.envantertr.com')
        return route.fulfill({ contentType: 'text/html', body: '<h1>Synthetic Farma portal</h1>' });
      if (!['envantertr.com', 'www.envantertr.com'].includes(url.hostname)) return route.abort();
      if (!url.pathname.startsWith('/api/')) {
        let body = url.pathname === '/giris' ? html : '';
        if (['/report-print.js', '/scheduled-reports.js', '/vendor/qrcodegen.js'].includes(url.pathname))
          body = await readFile(`/checkout-site${url.pathname}`, 'utf8');
        return route.fulfill({ contentType: url.pathname === '/giris' ? 'text/html' : 'text/javascript', body });
      }
      if (url.pathname === '/api/session/login') {
        assert.equal(route.request().method(), 'POST');
        assert.equal(route.request().headers()['x-csrf-token'], 'synthetic-csrf');
        assert.equal(route.request().postDataJSON().email, 'synthetic@example.invalid');
        authenticated = true;
        loginCount++;
        return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ companyId: 'synthetic-farma' }) });
      }
      if (url.pathname === '/api/session' && !authenticated)
        return route.fulfill({ status: 401, contentType: 'application/json', body: '{}' });
      const responses = {
        '/api/status': { status: 'ready' },
        '/api/setup/status': { setupRequired: false },
        '/api/session/csrf': { requestToken: 'synthetic-csrf' },
        '/api/session': { companyId: 'synthetic-farma', availableCompanies: [
          { id: 'synthetic-santa', tenantUrl: 'https://santa.envantertr.com/' },
          { id: 'synthetic-farma', tenantUrl: 'https://farma.envantertr.com/' }
        ] }
      };
      return route.fulfill({ contentType: 'application/json', body: JSON.stringify(responses[url.pathname] ?? []) });
    });
    await page.goto(`https://${entryHost}/giris`);
    if (scenario.login) {
      await page.locator('#loginForm [name="email"]').fill('synthetic@example.invalid');
      await page.locator('#loginForm [name="password"]').fill('Synthetic-Only-Password!');
      await page.locator('#loginForm button[type="submit"]').click();
    }
    await page.waitForURL('https://farma.envantertr.com/');
    assert.equal(await page.locator('h1').innerText(), 'Synthetic Farma portal');
    assert.ok(!requests.some(url => url.includes('/api/assets')), 'Do not load tenant data on the entry host');
    assert.equal(loginCount, scenario.login ? 1 : 0, 'No silent credential retry');
    await page.close();
  }
  console.log('PASS: actual HTML apex/www existing sessions and signed-out CSRF login redirect active membership to Farma, without entry-host tenant data or credential retries');
} finally { await browser.close(); }
