import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const app = await readFile('/checkout-site/company-app.html', 'utf8');
const code = app.slice(app.indexOf('    async function loadCompanyStorageUsage()'), app.indexOf('    async function startAssetCamera()'));
assert.ok(code.includes('/api/company/storage-usage'));
const html = `<!doctype html><div id="companyStoragePanel" hidden><span id="companyStorage"></span></div><script>
const $=s=>document.querySelector(s);let permitted=true,activeCompanyContextId='A',selectedLocale='tr-TR';
let mode='success',calls=0;const canManageSettings=()=>permitted,translateUiText=x=>x;
async function api(){calls++;if(mode==='error')throw Error('Unavailable');if(mode==='pending')return new Promise(r=>window.resolveUsage=r);return {bytes:1234,fileCount:2};}
${code}
</script>`;
const server = createServer((request,response)=>{response.writeHead(200,{'content-type':'text/html; charset=utf-8'});response.end(html);});
let browser;
try {
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  browser=await chromium.launch({headless:true});
  const page=await browser.newPage();
  const errors=[];page.on('pageerror',error=>errors.push(error.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  await page.evaluate(()=>loadCompanyStorageUsage());
  assert.equal(await page.locator('#companyStoragePanel').isVisible(),true);
  assert.equal(await page.locator('#companyStorage').innerText(),'1.234 B');
  assert.match(await page.locator('#companyStorage').getAttribute('title'),/kotası tanımlı değil/);
  await page.evaluate(async()=>{permitted=false;await loadCompanyStorageUsage();});
  assert.equal(await page.locator('#companyStoragePanel').isVisible(),false);
  assert.equal(await page.evaluate(()=>calls),1,'Unauthorized UI must not request company-wide storage.');
  await page.evaluate(async()=>{permitted=true;mode='error';await loadCompanyStorageUsage();});
  assert.match(await page.locator('#companyStorage').innerText(),/alınamadı/);
  await page.evaluate(()=>{mode='pending';void loadCompanyStorageUsage();});
  await page.waitForFunction(()=>!!window.resolveUsage);
  await page.evaluate(()=>{activeCompanyContextId='B';document.querySelector('#companyStorage').textContent='New company';window.resolveUsage({bytes:99999});});
  assert.equal(await page.locator('#companyStorage').innerText(),'New company','Late company A usage must not overwrite company B.');
  assert.deepEqual(errors,[]);
  console.log('PASS: actual storage UI renders measured bytes without an invented quota, hides unauthorized data, reports failure and rejects late cross-company responses.');
} finally {await browser?.close();await new Promise(resolve=>server.close(resolve));}
