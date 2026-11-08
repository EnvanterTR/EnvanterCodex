// Tüm menü ve alt menüleri gezen tarama testi: bozuk metin, JS hatası, başarısız API çağrısı,
// butonların açtığı pencereler ve Escape ile kapanma kontrolü.
// Çalıştırma: BASE_URL=http://127.0.0.1:5099 TEST_EMAIL=... TEST_PASSWORD=... TEST_COMPANY=Santa node tests/browser/menu-sweep.acceptance.mjs
// Yalnız yerel/izole ortamda çalışır. Silme, arşivleme, çıkış ve gönderme butonlarına tıklanmaz.
import { chromium } from 'playwright';

const base = process.env.BASE_URL || 'http://127.0.0.1:5099';
const email = process.env.TEST_EMAIL, password = process.env.TEST_PASSWORD, company = process.env.TEST_COMPANY || 'Santa';
if (!email || !password) { console.error('TEST_EMAIL ve TEST_PASSWORD gerekli.'); process.exit(2); }
if (/envantertr\.com/i.test(base)) { console.error('Bu test canlı ortamda çalıştırılmaz.'); process.exit(2); }

const DANGEROUS = /(sil|arşiv|çıkış|kaldır|iptal et|devre dışı|sıfırla|kalıcı|delete|archive|logout|remove|revoke|gönder|davet|yayınla|uygula|dışa aktar|indir)/i;
const BAD_TEXT = /\b(NaN|undefined|null|\[object Object\]|Invalid Date)\b/;
const jsErrors = [], badResponses = [];
const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1600, height: 1000 }, acceptDownloads: false });
const page = await context.newPage();
page.on('pageerror', error => jsErrors.push(error.message));
page.on('dialog', dialog => dialog.dismiss().catch(() => {}));
page.on('response', response => {
  const url = response.url();
  if (url.startsWith(base) && response.status() >= 400 && !/\/api\/session$|csrf|\/photo(\?|$)|\/attachments\/[^/]+\/file/.test(url)) badResponses.push(`${response.status()} ${response.request().method()} ${url.replace(base, '')}`);
});
context.on('page', popup => popup.close().catch(() => {}));

async function login() {
  await page.goto(base + '/'); await page.waitForTimeout(1000);
  await page.fill('#authLayer input[type=email]', email); await page.fill('#authLayer input[type=password]', password);
  await page.click('#authLayer button[type=submit]'); await page.waitForTimeout(1800);
  const select = page.locator('#authLayer select').first();
  if (await select.count() && await select.isVisible()) {
    const value = await select.evaluate((element, name) => [...element.options].find(option => option.text.toLocaleLowerCase('tr-TR').includes(name.toLocaleLowerCase('tr-TR')))?.value, company);
    await select.selectOption(value); await page.click('#authLayer button[type=submit]'); await page.waitForTimeout(2500);
  }
  if (await page.evaluate(() => document.body.classList.contains('auth-required'))) throw new Error('Oturum açılamadı');
}
await login();

const pages = await page.evaluate(() => [...document.querySelectorAll('.sub-item[data-page], .nav-item[data-page]')].map(item => item.dataset.page).filter((name, index, all) => all.indexOf(name) === index));
const report = [];
for (const name of pages) {
  const entry = { page: name, issues: [], buttons: 0, dialogs: 0 };
  const errorsBefore = jsErrors.length, responsesBefore = badResponses.length;
  try {
    await page.evaluate(n => { const item = document.querySelector(`.sub-item[data-page="${n}"], .nav-item[data-page="${n}"]`); item?.click(); }, name);
    await page.waitForTimeout(1600);
    const visible = await page.evaluate(() => { const section = [...document.querySelectorAll('.workspace-page')].find(element => !element.hidden) || document.querySelector('main'); if (section && !section.id) section.id = 'sweepRoot'; return section ? { id: section.id, text: section.innerText, heading: section.querySelector('h1')?.textContent?.trim() || '' } : null; });
    if (!visible) entry.issues.push('Sayfa bölümü görünür değil');
    else {
      if (!visible.heading) entry.issues.push('Başlık (h1) yok');
      const bad = visible.text.match(BAD_TEXT); if (bad) entry.issues.push(`Bozuk metin: ${bad[0]}`);
      const buttons = await page.evaluate(id => [...document.querySelectorAll(`#${id} button:not([disabled]), #${id} [role=button]:not([disabled])`)].filter(b => b.offsetParent !== null).map((b, i) => ({ i, label: (b.getAttribute('aria-label') || b.title || b.textContent || '').trim().slice(0, 60) })), visible.id);
      entry.buttons = buttons.length;
      for (const button of buttons.slice(0, 25)) {
        if (!button.label || DANGEROUS.test(button.label)) continue;
        const clicked = await page.evaluate(({ id, index }) => { const b = [...document.querySelectorAll(`#${id} button:not([disabled]), #${id} [role=button]:not([disabled])`)].filter(x => x.offsetParent !== null)[index]; if (!b) return false; b.click(); return true; }, { id: visible.id, index: button.i });
        if (!clicked) continue;
        await page.waitForTimeout(450);
        const open = await page.evaluate(() => [...document.querySelectorAll('dialog[open]')].map(d => ({ id: d.id, text: d.innerText })));
        if (open.length) {
          entry.dialogs += 1;
          for (const dialog of open) { const dialogBad = dialog.text.match(BAD_TEXT); if (dialogBad) entry.issues.push(`"${button.label}" penceresi (#${dialog.id}) bozuk metin: ${dialogBad[0]}`); }
          // İç içe pencerelerde her Escape yalnız en üsttekini kapatır; en çok 4 basış beklenir.
          for (let press = 0; press < 4; press += 1) { await page.keyboard.press('Escape'); await page.waitForTimeout(350); if (!await page.evaluate(() => document.querySelectorAll('dialog[open]').length)) break; }
          if (await page.evaluate(() => document.querySelectorAll('dialog[open]').length)) { entry.issues.push(`"${button.label}" penceresi Escape ile kapanmadı`); await page.evaluate(() => document.querySelectorAll('dialog[open]').forEach(d => d.close())); }
        }
        const stillHere = await page.evaluate(id => !document.getElementById(id)?.hidden, visible.id);
        if (!stillHere) await page.evaluate(n => document.querySelector(`.sub-item[data-page="${n}"], .nav-item[data-page="${n}"]`)?.click(), name), await page.waitForTimeout(900);
      }
    }
  } catch (error) { entry.issues.push(`İstisna: ${error.message.split('\n')[0]}`); }
  for (const message of jsErrors.slice(errorsBefore)) entry.issues.push(`JS hatası: ${message.slice(0, 140)}`);
  for (const response of badResponses.slice(responsesBefore)) entry.issues.push(`API: ${response}`);
  report.push(entry);
}
await browser.close();

let failed = 0;
for (const entry of report) {
  const status = entry.issues.length ? 'FAIL' : 'PASS'; if (entry.issues.length) failed += 1;
  console.log(`${status} ${entry.page} · ${entry.buttons} buton · ${entry.dialogs} pencere`);
  for (const issue of [...new Set(entry.issues)]) console.log(`   - ${issue}`);
}
console.log(`\nSayfa: ${report.length}, hatalı sayfa: ${failed}`);
process.exit(failed ? 1 : 0);
