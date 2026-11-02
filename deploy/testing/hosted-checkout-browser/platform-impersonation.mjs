import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/checkout-site/company-app.html', 'utf8');
const browser = await chromium.launch({ headless: true });
try {
  for (const exitControl of ['#impersonationReturn', '#logoutButton']) {
    const page = await browser.newPage();
    let returns = 0, logouts = 0;
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/*', async route => {
      const request = route.request(), url = new URL(request.url());
      if (url.href === 'https://envantertr.com/platform-admin.html')
        return route.fulfill({ contentType: 'text/html', body: '<h1>Platform administration restored</h1>' });
      if (url.hostname !== 'farma.envantertr.com') return route.abort();
      if (!url.pathname.startsWith('/api/')) {
        let body = url.pathname === '/' ? html : '';
        if (['/report-print.js', '/scheduled-reports.js', '/vendor/qrcodegen.js'].includes(url.pathname))
          body = await readFile(`/checkout-site${url.pathname}`, 'utf8');
        return route.fulfill({ contentType: url.pathname === '/' ? 'text/html' : 'text/javascript', body });
      }
      if (url.pathname === '/api/session/impersonation/stop') {
        assert.equal(request.method(), 'POST');
        assert.equal(request.headers()['x-csrf-token'], 'synthetic-support-csrf');
        returns++;
        return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ platformUrl: 'https://envantertr.com/platform-admin.html' }) });
      }
      if (url.pathname === '/api/session/logout') logouts++;
      const data = {
        '/api/status': { status: 'ready' }, '/api/setup/status': { setupRequired: false },
        '/api/session/csrf': { requestToken: 'synthetic-support-csrf' },
        '/api/session': { companyId: 'synthetic-company', name: 'Synthetic Customer', companyName: 'Farma', role: 'viewer',
          availableCompanies: [{ companyId: 'synthetic-company', name: 'Farma' }],
          impersonation: { actorName: 'Synthetic Administrator', asUserName: 'Synthetic Customer', expiresAt: new Date(Date.now() + 900000).toISOString() } },
        '/api/company/branding': { name: 'Farma', hasLogo: false },
        '/api/company/label-settings': { isConfigured: false, settings: null },
        '/api/company/profile': { name: 'Farma', slug: 'farma', tenantDomain: 'envantertr.com', hasLogo: false },
        '/api/assets/custom-fields': [], '/api/appearance/preferences': { language: 'tr', theme: 'dark' }
      }[url.pathname] ?? { items: [], total: 0, page: 1, pageSize: 50 };
      return route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });
    });
    await page.goto('https://farma.envantertr.com/');
    await page.locator('#impersonationNotice').waitFor({ state: 'visible' });
    assert.match(await page.locator('#impersonationActor').innerText(), /Synthetic Administrator.*Synthetic Customer/);
    assert.equal(await page.locator('#impersonationNotice').evaluate(element => getComputedStyle(element).backgroundColor), 'rgb(255, 243, 205)');
    await page.locator('#authLayer.show').waitFor({ state: 'hidden' });
    if(process.env.SUPPORT_EVIDENCE_DIR && exitControl==='#impersonationReturn') await page.screenshot({path:process.env.SUPPORT_EVIDENCE_DIR+'/support-banner.png',fullPage:false});
    await page.locator(exitControl).click();
    await page.waitForURL('https://envantertr.com/platform-admin.html');
    assert.equal(returns, 1);
    assert.equal(logouts, 0);
    assert.deepEqual(errors, []);
    await page.close();
  }
  console.log('PASS: support banner identifies actor and customer; banner and logout each return once with CSRF');
} finally { await browser.close(); }
