import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/checkout-site/company-app.html', 'utf8');
const browser = await chromium.launch({ headless: true });
try {
  for (const entryHost of ['envantertr.com', 'www.envantertr.com']) {
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
    await page.waitForURL('https://farma.envantertr.com/');
    assert.equal(await page.locator('h1').innerText(), 'Synthetic Farma portal');
    assert.ok(!requests.some(url => url.includes('/api/assets')), 'Do not load tenant data on the entry host');
    await page.close();
  }
  console.log('PASS: actual company HTML redirects apex/www active membership to Farma before loading tenant data');
} finally { await browser.close(); }
