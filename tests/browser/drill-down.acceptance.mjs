// Tıklanabilir ilişki filtreleri ("tıkla → filtrelenmiş liste") için tarayıcı kabul testi.
// Çalıştırma: BASE_URL=http://127.0.0.1:5099 TEST_EMAIL=... TEST_PASSWORD=... TEST_COMPANY=Santa node tests/browser/drill-down.acceptance.mjs
// Santa demo verisi (tools/seed-santa-*.sql) yüklenmiş yerel/izole bir ortam bekler. Canlı ortamda çalıştırmayın.
import { chromium } from 'playwright';
import assert from 'node:assert/strict';

const base = process.env.BASE_URL || 'http://127.0.0.1:5099';
const email = process.env.TEST_EMAIL, password = process.env.TEST_PASSWORD, company = process.env.TEST_COMPANY || 'Santa';
if (!email || !password) { console.error('TEST_EMAIL ve TEST_PASSWORD gerekli.'); process.exit(2); }
if (/envantertr\.com/i.test(base)) { console.error('Bu test canlı ortamda çalıştırılmaz.'); process.exit(2); }

const results = []; const jsErrors = [];
const browser = await chromium.launch();
const page = await (await browser.newContext({ viewport: { width: 1600, height: 1000 } })).newPage();
page.on('pageerror', error => jsErrors.push(error.message));

async function login() {
  await page.goto(base + '/'); await page.waitForTimeout(1000);
  await page.fill('#authLayer input[type=email]', email); await page.fill('#authLayer input[type=password]', password);
  await page.click('#authLayer button[type=submit]'); await page.waitForTimeout(1800);
  const select = page.locator('#authLayer select').first();
  if (await select.count() && await select.isVisible()) {
    const value = await select.evaluate((element, name) => [...element.options].find(option => option.text.toLocaleLowerCase('tr-TR').includes(name.toLocaleLowerCase('tr-TR')))?.value, company);
    await select.selectOption(value); await page.click('#authLayer button[type=submit]'); await page.waitForTimeout(2500);
  }
  assert.equal(await page.evaluate(() => document.body.classList.contains('auth-required')), false, 'Oturum açılamadı');
}
const go = async name => { await page.evaluate(n => document.querySelector(`.sub-item[data-page="${n}"]`).click(), name); await page.waitForTimeout(1800); };
const assetColumn = async index => page.$$eval(`#assetRows tr:not(.asset-empty-row) td:nth-child(${index})`, cells => cells.map(cell => cell.textContent.trim()));
const assetTotal = async () => Number((await page.textContent('#assetCount')).replace(/\D/g, '') || 0);
const relationChips = async () => page.$$eval('#assetRelationFilter button, #assetRelationFilter .filter-chip', chips => chips.map(chip => chip.textContent.trim()));
// Varlık tablosu sütunları (1 tabanlı): 1 seçim, 8 model, 10 kategori, 11 durum, 12 çalışan, 14 konum, 15 varsayılan konum, 16 üretici
async function check(name, fn) {
  try { await fn(); results.push(['PASS', name]); }
  catch (error) { results.push(['FAIL', name, error.message]); await page.screenshot({ path: `drill-fail-${results.length}.png` }).catch(() => {}); }
}

await login();

await check('Varlıklar: Üretici tıklaması yalnız o markanın varlıklarını listeler', async () => {
  await go('Varlıklar'); const link = page.locator('#assetRows td:nth-child(16) .relationship-link').first();
  const label = (await link.textContent()).trim(); await link.click(); await page.waitForTimeout(1500);
  const values = await assetColumn(16); assert.ok(values.length > 0); assert.ok(values.every(value => value === label), `Farklı üretici: ${values}`);
  assert.match(page.url(), /brandId=/);
});
await check('Varlıklar: Durum tıklaması yalnız o durumdaki varlıkları listeler ve URL’ye yazılır', async () => {
  await go('Varlıklar'); const link = page.locator('#assetRows td:nth-child(11) .relationship-link').first();
  const label = (await link.textContent()).trim(); await link.click(); await page.waitForTimeout(1500);
  const values = await assetColumn(11); assert.ok(values.length > 0); assert.ok(values.every(value => value === label), `Farklı durum: ${values}`);
  assert.match(page.url(), /status=/);
  await page.reload(); await page.waitForTimeout(3000);
  const afterReload = await assetColumn(11); assert.ok(afterReload.length > 0 && afterReload.every(value => value === label), 'Yenilemeden sonra durum filtresi korunmadı');
});
for (const [pageName, column, param] of [['Varlık modelleri', 8, 'modelId'], ['Markalar', 16, 'brandId'], ['Kategoriler', 10, 'categoryId']]) {
  await check(`${pageName}: "N varlık" tıklaması tam o N kaydı listeler`, async () => {
    await go(pageName); const link = page.locator('#catalogRows a[data-asset-drill]').first();
    const expected = Number((await link.textContent()).replace(/\D/g, '')); const label = await link.evaluate(element => element.closest('tr').querySelector('input[data-catalog-name]')?.value || element.closest('tr').children[0].textContent.trim());
    await link.click(); await page.waitForTimeout(1800);
    assert.equal(await assetTotal(), expected, 'Kayıt sayısı eşleşmiyor'); assert.match(page.url(), new RegExp(param));
    const values = await assetColumn(column); assert.ok(values.every(value => value.includes(label)), `${label} dışında kayıt: ${values.slice(0, 5)}`);
  });
}
await check('Yeni tıklama temiz liste açar (önceki model filtresi taşınmaz)', async () => {
  await go('Varlıklar'); await page.locator('#assetRows td:nth-child(8) .relationship-link').first().click(); await page.waitForTimeout(1200);
  await go('Kategoriler'); const link = page.locator('#catalogRows a[data-asset-drill]').first(); const expected = Number((await link.textContent()).replace(/\D/g, ''));
  await link.click(); await page.waitForTimeout(1800);
  assert.equal(await page.inputValue('#assetModelFilter'), '', 'Model filtresi temizlenmedi'); assert.equal(await page.inputValue('#assetBrandFilter'), '', 'Marka filtresi temizlenmedi');
  assert.equal(await assetTotal(), expected);
});
await check('Lokasyonlar: varlık sayısı tıklaması o konumdaki varlıkları listeler', async () => {
  await go('Lokasyonlar'); const link = page.locator('#directoryRows tr td:nth-child(2) .relationship-link').first();
  const expected = Number((await link.textContent()).replace(/\D/g, '')); const locationName = (await link.evaluate(element => element.closest('tr').children[0].textContent.trim())).replace(/ · Arşivlendi$/, '');
  await link.click(); await page.waitForTimeout(1800);
  assert.equal(await assetTotal(), expected, 'Konum sayısı eşleşmiyor'); assert.ok((await relationChips()).some(chip => chip.includes(locationName)), 'Konum etiketi görünmüyor');
});
await check('Lokasyonlar: "Atanmış" sayısı tıklaması konum + Atanmış durumu ile listeler', async () => {
  await go('Lokasyonlar'); const link = page.locator('#directoryRows tr td:nth-child(3) .relationship-link').first();
  const expected = Number((await link.textContent()).replace(/\D/g, '')); await link.click(); await page.waitForTimeout(1800);
  assert.equal(await page.inputValue('#assetStatusFilter'), 'Deployed'); assert.equal(await assetTotal(), expected);
});
await check('Departmanlar: çalışan sayısı tıklaması o departmanın çalışanlarını açar', async () => {
  await go('Departmanlar'); const link = page.locator('#directoryRows tr td:nth-child(4) .relationship-link').first();
  const department = await link.evaluate(element => element.closest('tr').children[0].textContent.trim()); await link.click(); await page.waitForTimeout(1800);
  assert.ok(await page.locator('#employeeRows').isVisible(), 'Çalışan kayıtları sayfası açılmadı');
  const filter = await page.inputValue('#employeeDepartmentFilter'); assert.ok(filter && department.includes(filter), `Departman filtresi yanlış: ${filter}`);
});
await check('Zimmetler: model, konum ve departman tıklanabilir ve doğru yere gider', async () => {
  await go('Zimmetler'); const model = page.locator('#assignmentRows tr td:nth-child(2) .relationship-link').first();
  const modelName = (await model.textContent()).trim(); await model.click(); await page.waitForTimeout(1800);
  const models = await assetColumn(8); assert.ok(models.length > 0 && models.every(value => value === modelName), `Model listesi yanlış: ${models}`);
  await go('Zimmetler'); const locationLink = page.locator('#assignmentRows .relationship-link[href*="location="]').first();
  const locationName = (await locationLink.textContent()).trim(); await locationLink.click(); await page.waitForTimeout(1800);
  const locations = await assetColumn(14); assert.ok(locations.length > 0 && locations.every(value => value.includes(locationName)), 'Konum listesi yanlış');
  await go('Zimmetler'); const departmentLink = page.locator('#assignmentRows tr td:nth-child(4) .relationship-link').first();
  await departmentLink.click(); await page.waitForTimeout(1800); assert.ok(await page.locator('#employeeRows').isVisible(), 'Departman tıklaması çalışan sayfasını açmadı');
});
await check('Denetimler: etiket varlık detayını, konum filtrelenmiş listeyi açar', async () => {
  await go('Denetimler'); const tag = page.locator('#auditScheduleRows tr td:nth-child(2) .relationship-link').first();
  await tag.click(); await page.waitForTimeout(1500); assert.ok(await page.locator('#assetDialog').evaluate(dialog => dialog.open), 'Varlık detayı açılmadı');
  await page.evaluate(() => document.querySelector('#assetDialog').close());
  const locationLink = page.locator('#auditScheduleRows tr td:nth-child(4) .relationship-link').first(); const locationName = (await locationLink.textContent()).trim();
  await locationLink.click(); await page.waitForTimeout(1800);
  const locations = await assetColumn(14); assert.ok(locations.length > 0 && locations.every(value => value.includes(locationName)));
});
await check('Garanti takibi: konum tıklanabilir', async () => {
  await go('Garanti takibi'); const link = page.locator('#warrantyReportRows .relationship-link[href*="location="]').first();
  if (!await link.count()) throw new Error('Garanti tablosunda konum bağlantısı yok');
  const locationName = (await link.textContent()).trim(); await link.click(); await page.waitForTimeout(1800);
  const locations = await assetColumn(14); assert.ok(locations.length > 0 && locations.every(value => value.includes(locationName)));
});

await check('Çalışan kayıtları: Zimmetler sütunu doğru sayıları gösterir ve profili açar', async () => {
  await go('Çalışan kayıtları');
  const header = await page.$$eval('#employeeRows', () => [...document.querySelector('#employeeRows').closest('table').querySelectorAll('thead th')].map(th => th.textContent.trim()));
  const column = header.indexOf('Zimmetler') + 1; assert.ok(column > 0, 'Zimmetler sütunu yok');
  const apiCounts = await page.evaluate(async () => (await (await fetch('/api/employees?page=1&pageSize=50', { credentials: 'same-origin' })).json()));
  const rowWithAssets = apiCounts.items.find(item => apiCounts.counts?.[item.id]?.assets > 0); assert.ok(rowWithAssets, 'API sayıları dönmüyor');
  const expected = apiCounts.counts[rowWithAssets.id].assets;
  const cellText = await page.locator('#employeeRows tr').filter({ hasText: rowWithAssets.displayName }).first().locator(`td:nth-child(${column})`).innerText();
  assert.ok(cellText.includes(`${expected} varlık`), `Hücre: ${cellText}, beklenen ${expected} varlık`);
  await page.locator('#employeeRows tr').filter({ hasText: rowWithAssets.displayName }).first().locator(`td:nth-child(${column}) .relationship-link`).first().click();
  await page.waitForTimeout(1800);
  const dialogText = await page.evaluate(() => [...document.querySelectorAll('dialog[open]')].map(d => d.innerText).join(' '));
  assert.ok(dialogText.includes(rowWithAssets.displayName), 'Çalışan profili açılmadı');
});

await check('Yazılım lisansları: yayıncı filtreler, koltuk sayısı lisans detayını açar', async () => {
  await go('Yazılım lisansları'); const publisher = page.locator('#licenseRows .inventory-filter-link').first();
  if (!await publisher.count()) throw new Error('Lisans demo verisi yok veya yayıncı tıklanamıyor');
  const name = (await publisher.textContent()).trim(); await publisher.click(); await page.waitForTimeout(1500);
  assert.equal(await page.inputValue('#licenseSearch'), name);
  const publishers = await page.$$eval('#licenseRows tr[data-id] td:nth-child(2) .inventory-filter-link', links => links.map(link => link.textContent.trim()));
  assert.ok(publishers.length > 0 && publishers.every(value => value === name), `Farklı yayıncı: ${publishers}`);
  await page.locator('#licenseRows tr[data-id] td:nth-child(4) .license-open').first().click(); await page.waitForTimeout(1500);
  assert.ok(await page.locator('#licenseDialog').evaluate(dialog => dialog.open), 'Lisans detayı açılmadı');
});

await check('Departmanlar: kat/konum tıklaması o konumun varlıklarını listeler', async () => {
  await go('Departmanlar'); const link = page.locator('#directoryRows tr td:nth-child(2) .relationship-link').first();
  if (!await link.count()) throw new Error('Departman konumu tıklanamıyor');
  const locationName = (await link.textContent()).trim(); await link.click(); await page.waitForTimeout(1800);
  assert.ok(await page.locator('#assetRows').isVisible(), 'Varlık listesi açılmadı');
  assert.ok((await relationChips()).some(chip => chip.includes(locationName)), 'Konum filtresi etiketi yok');
});

await browser.close();
for (const [state, name, detail] of results) console.log(`${state}: ${name}${detail ? ` — ${detail}` : ''}`);
const failed = results.filter(result => result[0] === 'FAIL').length;
if (jsErrors.length) console.log('JS hataları:', jsErrors.slice(0, 5));
console.log(`SONUÇ: ${results.length - failed}/${results.length} PASS, JS hatası: ${jsErrors.length}`);
process.exit(failed || jsErrors.length ? 1 : 0);
