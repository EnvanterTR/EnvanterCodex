import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/checkout-site/company-app.html', 'utf8');
const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage();
  let saved = false, saveCount = 0;
  const staleRequests = [];
  await page.route('**/*', async route => {
    const request = route.request(), url = new URL(request.url());
    if (url.hostname === 'farma.envantertr.com')
      return route.fulfill({ contentType: 'text/html', body: '<h1>Renamed synthetic Farma portal</h1>' });
    if (url.hostname !== 'santa.envantertr.com') return route.abort();
    if (!url.pathname.startsWith('/api/')) {
      let body = url.pathname === '/' ? html : '';
      if (['/report-print.js', '/scheduled-reports.js', '/vendor/qrcodegen.js'].includes(url.pathname))
        body = await readFile(`/checkout-site${url.pathname}`, 'utf8');
      return route.fulfill({ contentType: url.pathname === '/' ? 'text/html' : 'text/javascript', body });
    }
    if (saved) staleRequests.push(url.pathname);
    if (url.pathname === '/api/company/profile' && request.method() === 'PUT') {
      assert.equal(request.headers()['x-csrf-token'], 'synthetic-csrf');
      assert.equal(request.postDataJSON().slug, 'farma');
      assert.equal(request.postDataJSON().name, 'Farma');
      saveCount++; saved = true;
      return route.fulfill({ contentType: 'application/json', body: JSON.stringify({ slug: 'farma', tenantUrl: 'https://farma.envantertr.com/' }) });
    }
    const data = {
      '/api/status': { status: 'ready' }, '/api/setup/status': { setupRequired: false },
      '/api/session/csrf': { requestToken: 'synthetic-csrf' },
      '/api/session': { companyId: 'synthetic-company', name: 'Synthetic Owner', companyName: 'Santa', role: 'owner' },
      '/api/company/branding': { name: 'Santa', hasLogo: false },
      '/api/company/label-settings': { isConfigured: false, settings: null },
      '/api/company/profile': { name: 'Santa', slug: 'santa', tenantDomain: 'envantertr.com', hasLogo: false },
      '/api/assets/custom-fields': [], '/api/appearance/preferences': { language: 'tr', theme: 'dark' }
    }[url.pathname] ?? { items: [], total: 0, page: 1, pageSize: 50 };
    return route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });
  });
  await page.goto('https://santa.envantertr.com/');
  await page.locator('[data-group="settings"]').click();
  await page.locator('#sidebar [data-page="Firma profili"]').click();
  await page.waitForFunction(() => document.querySelector('#companyProfileForm')?.dataset.savedSlug === 'santa');
  await page.locator('#companyProfileForm [name="name"]').fill('Farma');
  await page.locator('#companyProfileForm [name="slug"]').fill('farma');
  await page.locator('#companyProfileForm button[type="submit"]').click();
  await page.waitForURL('https://farma.envantertr.com/');
  assert.equal(saveCount, 1);
  assert.deepEqual(staleRequests, [], 'Do not reload the former tenant profile/branding after its slug changed');
  assert.equal(await page.locator('h1').innerText(), 'Renamed synthetic Farma portal');
  console.log('PASS: real profile form persists new slug once with CSRF and moves to new portal without old-host reload');
} finally { await browser.close(); }

