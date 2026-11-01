import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const html = await readFile('/public-site/index.html');
const labelImage = await readFile('/public-site/assets/images/asset-label-roll-transparent.png');
const securityImage = await readFile('/public-site/assets/images/account-security-product-preview.png');
const portalImage = await readFile('/public-site/assets/images/farma-company-portal-preview.png');
assert.equal(labelImage[25], 6, 'The label image must preserve its RGBA transparency.');
const checkoutHtml = await readFile('/checkout-site/checkout.html');
const checkoutJs = await readFile('/checkout-site/checkout.js');
let planResponse = 'slow-empty';
const publishedPlan = {
  id: '22222222-2222-4222-8222-222222222222',
  name: 'Synthetic published Cloud plan',
  description: 'Synthetic description from the public plan API.',
  deploymentKind: 'cloud',
  maxLoginUsers: 10,
  monthlyPrice: 990,
  annualPrice: 9900,
  oneTimePrice: 0,
  currency: 'TRY'
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (url.pathname === '/assets/images/farma-company-portal-preview.png') {
    response.writeHead(200, { 'content-type': 'image/png' }); response.end(portalImage); return;
  }
  if (url.pathname === '/assets/images/account-security-product-preview.png') {
    response.writeHead(200, { 'content-type': 'image/png' }); response.end(securityImage); return;
  }
  if (url.pathname === '/assets/images/asset-label-roll-transparent.png') {
    response.writeHead(200, { 'content-type': 'image/png' }); response.end(labelImage); return;
  }
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
  if (request.method === 'GET' && url.pathname === '/api/checkout/status') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
    response.end('{"available":true}');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/checkout.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    response.end(checkoutHtml);
    return;
  }
  if (request.method === 'GET' && url.pathname === '/checkout.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(checkoutJs);
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
  await page.getByRole('heading', { name: 'Kurumsal SaaS: şirketinize özel portal ve güvenli erişim' }).waitFor();
  await page.waitForFunction(() => document.querySelector('#cloudPortalBody')?.textContent.includes('farma.envantertr.com'));
  assert.match(await page.locator('#cloudPortalBody').innerText(), /farma\.envantertr\.com/);
  assert.match(await page.locator('#cloudMfaBody').innerText(), /Authenticator.*6 haneli kod/s);
  assert.match(await page.locator('#cloudMfaNextBody').innerText(), /yeni bir oturum.*kurtarma kodu/s);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator('#varlik-etiketleri img').scrollIntoViewIfNeeded();
  await page.waitForFunction(() => document.querySelector('#varlik-etiketleri img')?.naturalWidth === 1200);
  assert.equal(await page.locator('#assetLabelDemo').getAttribute('href'), '/demo/');
  await page.locator('#guvenlik-ekranlari img').scrollIntoViewIfNeeded();
  await page.waitForFunction(() => document.querySelector('#guvenlik-ekranlari img')?.naturalWidth === 1536);
  const securityImageBounds = await page.locator('#guvenlik-ekranlari img').boundingBox();
  await page.locator('#sirket-portali img').scrollIntoViewIfNeeded();
  await page.waitForFunction(() => document.querySelector('#sirket-portali img')?.naturalWidth === 1536);
  assert.match(await page.locator('#companyPortalPreviewIntro').innerText(), /farma\.envantertr\.com/);
  assert.match(await page.locator('#companyPortalPreviewIntro').innerText(), /santa\.envantertr\.com/);
  assert.ok(securityImageBounds && securityImageBounds.x >= 0 && securityImageBounds.x + securityImageBounds.width <= 390);
  const labelBounds = await page.locator('#varlik-etiketleri img').boundingBox();
  assert.ok(labelBounds && labelBounds.x >= 0 && labelBounds.x + labelBounds.width <= 390,
    'The transparent label roll must fit the phone viewport.');
  const cloudCards = page.locator('#cloud .feature');
  assert.equal(await cloudCards.count(), 3, 'The SaaS onboarding section must present portal, first MFA setup, and subsequent MFA sign-ins.');
  const cloudCardBounds = await cloudCards.evaluateAll(cards => cards.map(card => { const rect = card.getBoundingClientRect(); return { top: rect.top, bottom: rect.bottom, right: rect.right }; }));
  assert.ok(cloudCardBounds.every((rect, index) => rect.right <= 390 && (index === 0 || rect.top >= cloudCardBounds[index - 1].bottom)),
    'The onboarding cards must stack without horizontal overflow on a narrow phone viewport.');
  for (const [language, title, portal, mfa] of [
    ['tr', 'Kurumsal SaaS:', 'farma.envantertr.com', '6 haneli kod'],
    ['en', 'Business SaaS:', 'farma.envantertr.com', 'six-digit code'],
    ['ru', 'Корпоративный SaaS:', 'farma.envantertr.com', 'шестизначный код'],
    ['uk', 'Корпоративний SaaS:', 'farma.envantertr.com', 'шестизначний код'],
    ['mk', 'Корпоративен SaaS:', 'farma.envantertr.com', 'шестцифрена шифра']
  ]) {
    await page.locator('#homeLanguage').selectOption(language);
    await page.getByRole('heading', { name: new RegExp(title) }).waitFor();
    assert.match(await page.locator('#cloudPortalBody').innerText(), new RegExp(portal));
    assert.ok((await page.locator('#cloudMfaBody').innerText()).includes(mfa), `${language} must explain authenticator enrollment and code verification.`);
    assert.ok((await page.locator('#cloudMfaNextBody').innerText()).length > 70, `${language} must explain MFA on subsequent sign-ins.`);
    const security = page.locator('#hesap-guvenligi');
    assert.equal(await security.locator('article').count(), 3);
    assert.match(await security.innerText(), /QR/);
    assert.match(await security.innerText(), /MFA/);
    assert.equal((await security.locator('h2').innerText()).startsWith('Kurumsal'), language === 'tr',
      'Security marketing must use the selected language and restore Turkish when requested.');
    assert.equal((await page.locator('#assetLabelTitle').innerText()).startsWith('Fiziksel'), language === 'tr');
    assert.match(await page.locator('#assetLabelIntro').innerText(), /QR/);
    const bounds = await security.locator('article').evaluateAll(nodes => nodes.map(node => {
      const rect = node.getBoundingClientRect(); return { left: rect.left, right: rect.right };
    }));
    assert.ok(bounds.every(rect => rect.left >= 0 && rect.right <= 390), 'Security cards must fit the phone viewport.');
  }
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.locator('#homeLanguage').selectOption('tr');
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
  assert.match(await card.locator('.price-user-quota').innerText(), /^10\s/, 'Published account quota must be shown instead of an unlimited claim.');
  assert.match(await card.getByRole('link').getAttribute('href'), new RegExp(`^/checkout\\.html\\?planId=${publishedPlan.id}$`));
  publishedPlan.monthlyPrice=0;
  await page.reload({waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>[...document.querySelectorAll('.price-card a')].some(link=>link.pathname==='/trial.html'));
  const trialLink=new URL(await card.getByRole('link').getAttribute('href'),baseUrl);
  assert.equal(trialLink.pathname,'/trial.html');assert.equal(trialLink.searchParams.get('planId'),publishedPlan.id);
  assert.match(await card.locator('.price-comparison').innerText(),/825/);
  publishedPlan.monthlyPrice=990;
  await page.goto(`${baseUrl}/checkout.html?planId=${publishedPlan.id}`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => !document.querySelector('#checkoutFields').disabled);
  const companyName = page.locator('[name="companyName"]');
  const slug = page.locator('[name="slug"]');
  for (const [company, expected] of [
    ['Farma A.Ş.', 'farma'], ['Firma Farma A.Ş.', 'farma'], ['Santa', 'santa'], ['Celal', 'celal']
  ]) {
    await companyName.fill(company);
    assert.equal(await slug.inputValue(), expected, `${company} should suggest ${expected}.envantertr.com`);
  }
  await slug.fill('celal-ozel');
  await companyName.fill('Celal Belge');
  assert.equal(await slug.inputValue(), 'celal-ozel', 'A user-edited portal slug must remain unchanged when the company name changes.');
  assert.deepEqual(pageErrors, [], `Public homepage and checkout must not throw browser errors: ${pageErrors.join(' | ')}`);
  console.log('PASS: public Cloud pricing shows loading, empty and API-error states without invented tiers, localizes SaaS/MFA copy in all five languages, suggests concise tenant subdomains from company names, and preserves manually edited slugs.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
