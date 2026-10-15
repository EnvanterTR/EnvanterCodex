import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/public-site/index.html');
let planResponse = 'slow-empty';
const publishedPlan = {
  id: '22222222-2222-4222-8222-222222222222',
  name: 'Synthetic published Cloud plan',
  description: 'Synthetic description from the public plan API.',
  deploymentKind: 'cloud',
  monthlyPrice: 990,
  annualPrice: 9900,
  oneTimePrice: 0,
  currency: 'TRY'
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/api/plans') {
    if (planResponse === 'slow-empty') await new Promise(resolve => setTimeout(resolve, 5000));
    if (planResponse === 'error') {
      response.writeHead(503, { 'content-type': 'application/json; charset=utf-8' });
      response.end('{"error":"synthetic unavailable"}');
      return;
    }
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
    response.end(JSON.stringify(planResponse === 'published' ? [publishedPlan] : []));
    return;
  }
  if (request.method === 'GET' && url.pathname === '/api/site-content') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
    response.end('[]');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/demo-request.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end('');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    response.end(html);
    return;
  }
  response.writeHead(404);
  response.end();
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const baseUrl = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage();
  const pageErrors = [];
  page.on('pageerror', error => pageErrors.push(error.message));

  planResponse = 'slow-empty';
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  await page.locator('.pricing-state').getByText('Cloud planları yükleniyor').waitFor({ state: 'visible' });
  assert.equal(await page.locator('.pricing-grid .price-card').filter({ has: page.locator('h3') }).count(), 3,
    'Only the three self-hosted quote cards should exist while Cloud plans are loading.');
  for (const [language, phrase] of [
    ['tr', 'Cloud planları yükleniyor'], ['en', 'Loading Cloud plans'], ['ru', 'Загрузка планов Cloud'],
    ['uk', 'Завантаження планів Cloud'], ['mk', 'Се вчитуваат Cloud плановите']
  ]) {
    await page.locator('#homeLanguage').selectOption(language);
    await page.locator('.pricing-state').getByText(new RegExp(phrase, 'i')).waitFor({ state: 'visible' });
  }
  await page.locator('#homeLanguage').selectOption('tr');
  await page.locator('.pricing-state').getByText(/satın almaya açık bir Cloud planı yayımlanmamış/).waitFor({ state: 'visible' });
  for (const [language, phrase] of [
    ['tr', 'satın almaya açık bir Cloud planı yayımlanmamış'],
    ['en', 'no published Cloud plans available for purchase'],
    ['ru', 'нет опубликованных планов Cloud'],
    ['uk', 'немає опублікованих планів Cloud'],
    ['mk', 'нема објавени Cloud планови']
  ]) {
    await page.locator('#homeLanguage').selectOption(language);
    await page.locator('.pricing-state').getByText(new RegExp(phrase, 'i')).waitFor({ state: 'visible' });
  }
  assert.equal(await page.locator('.pricing-grid .price-card h3').filter({ hasText: /^(Başlangıç|Genişleyen ekip|Kurumsal|Small team|Mid-size team|Enterprise)$/ }).count(), 0,
    'Unpublished sample Cloud tiers must never be rendered.');
  assert.match(await page.locator('.pricing-note a').getAttribute('href'), /^mailto:/,
    'The existing quote contact link remains available when no Cloud plan is published.');

  await page.evaluate(() => localStorage.setItem('envantertr-home-language', 'tr'));
  planResponse = 'error';
  await page.reload({ waitUntil: 'domcontentloaded' });
  const errorMessages = [
    ['tr', 'Cloud planları şu anda yüklenemiyor'], ['en', 'Cloud plans are temporarily unavailable'],
    ['ru', 'Планы Cloud временно недоступны'], ['uk', 'Плани Cloud тимчасово недоступні'],
    ['mk', 'Cloud плановите привремено не се достапни']
  ];
  for (const [language, phrase] of errorMessages) {
    await page.locator('#homeLanguage').selectOption(language);
    await page.locator('.pricing-state').getByText(new RegExp(phrase, 'i')).waitFor({ state: 'visible' });
  }
  assert.equal(await page.locator('.pricing-grid .price-card h3').filter({ hasText: /^(Başlangıç|Genişleyen ekip|Kurumsal)$/ }).count(), 0,
    'API failure must not fall back to sample Cloud offers.');

  planResponse = 'published';
  await page.reload({ waitUntil: 'domcontentloaded' });
  const card = page.locator('.price-card').filter({ has: page.getByRole('heading', { name: publishedPlan.name }) });
  await card.waitFor({ state: 'visible' });
  assert.equal(await card.locator('p').innerText(), publishedPlan.description);
  assert.match(await card.getByRole('link').getAttribute('href'), new RegExp(`^/checkout\\.html\\?planId=${publishedPlan.id}$`));
  assert.deepEqual(pageErrors, [], `Public pricing must not throw browser errors: ${pageErrors.join(' | ')}`);
  console.log('PASS: public Cloud pricing shows loading, empty and API-error states without invented tiers, preserves the quote path, localizes all five languages, and renders only published plan data.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
