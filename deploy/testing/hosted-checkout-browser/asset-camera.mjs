import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

// Execute the application's actual camera functions in a minimal, deterministic media harness.
const app = await readFile('/checkout-site/company-app.html', 'utf8');
const lifecycle = app.slice(app.indexOf('    let assetCameraRequestId ='), app.indexOf('    async function openScannedAsset('));
const startup = app.slice(app.indexOf('    async function startAssetCamera()'), app.indexOf("    $('#scanAssetAction').addEventListener('change'"));
assert.ok(lifecycle.includes('createAssetCameraDecoder') && startup.includes('getUserMedia'));
const vendors = new Map(await Promise.all(['qrcodegen.js', 'zxing-browser-0.2.1.min.js', 'bwip-js-min.js']
  .map(async name => [`/vendor/${name}`, await readFile(`/checkout-site/vendor/${name}`)])));
const requests = [];
let failDecoderLoad = false;
const html = `<!doctype html><meta name="viewport" content="width=device-width"><style>body{font:16px sans-serif}dialog{max-width:90vw}video{width:100%}button{min-height:44px}p{overflow-wrap:anywhere}</style>
<button id="open">Okuyucuyu aç</button><dialog id="scanAssetDialog"><video id="scanAssetVideo" muted autoplay playsinline hidden></video>
<button id="startAssetCamera">Kamerayı başlat</button><button id="close">Kapat</button><p id="scanAssetFeedback" role="status" aria-live="polite"></p></dialog>
<script src="/vendor/qrcodegen.js"></script><script src="/vendor/bwip-js-min.js"></script><script>
const $=s=>document.querySelector(s);let assetScanStream=null,assetScanFrame=0,assetScanBusy=false,assetScanner=null;
window.scanned=[];window.mediaCalls=0;window.BarcodeDetector=undefined;
async function handleScannedAsset(code){window.scanned.push(code);stopAssetScanner();}
${lifecycle}\n${startup}
$('#open').onclick=()=>{$('#scanAssetFeedback').textContent='';$('#scanAssetDialog').showModal()};
$('#close').onclick=()=>$('#scanAssetDialog').close();$('#scanAssetDialog').onclose=stopAssetScanner;
$('#startAssetCamera').onclick=startAssetCamera;window.addEventListener('pagehide',stopAssetScanner);
</script>`;
const server = createServer((request, response) => {
  requests.push(request.url);
  if (failDecoderLoad && request.url === '/vendor/zxing-browser-0.2.1.min.js') { response.writeHead(503);response.end('Synthetic unavailable decoder');return; }
  const vendor = vendors.get(request.url);
  response.writeHead(vendor || request.url === '/' ? 200 : 404, { 'content-type': vendor ? 'text/javascript; charset=utf-8' : 'text/html; charset=utf-8' });
  response.end(vendor || (request.url === '/' ? html : 'Not found'));
});
let browser;
try {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  page.setDefaultTimeout(10_000);
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(`http://127.0.0.1:${server.address().port}/`);
  for (const [name, message] of [['NotAllowedError', 'Kamera izni verilmedi'], ['NotFoundError', 'kamera bulunamadı'], ['NotReadableError', 'başka bir uygulama']]) {
    await page.evaluate(errorName => {
      Object.defineProperty(navigator.mediaDevices, 'getUserMedia', { configurable: true, value: async () => {
        window.mediaCalls++; throw new DOMException('Synthetic media failure', errorName);
      } });
    }, name);
    await page.locator('#open').click();
    await page.locator('#startAssetCamera').click();
    await page.waitForFunction(text => document.querySelector('#scanAssetFeedback').textContent.includes(text), message).catch(async error => {
      throw new Error(`${error.message}; status=${await page.locator('#scanAssetFeedback').innerText()}; pageErrors=${JSON.stringify(errors)}`);
    });
    assert.equal(await page.locator('#startAssetCamera').isEnabled(), true, `${name} must permit an explicit retry.`);
    await page.locator('#close').click();
  }
  assert.equal(await page.evaluate(() => window.mediaCalls), 3);
  console.log('PASS: camera permission denial, missing device and busy device each show an actionable status and unlock retry.');

  await page.clock.install();
  await page.evaluate(() => {
    Object.defineProperty(navigator.mediaDevices, 'getUserMedia', { configurable: true, value: () => {
      window.mediaCalls++;
      return new Promise(resolve => { window.resolveTimedOutCamera = resolve; });
    } });
  });
  await page.locator('#open').click();
  await page.locator('#startAssetCamera').click();
  await page.waitForFunction(() => !!window.resolveTimedOutCamera);
  await page.clock.runFor(20_001);
  assert.match(await page.locator('#scanAssetFeedback').innerText(), /Kamera izni yanıtlanmadı/);
  assert.equal(await page.locator('#startAssetCamera').isEnabled(), true);
  await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width=320;canvas.height=240;
    const stream = canvas.captureStream(10); window.timedOutTrack = stream.getTracks()[0];
    window.resolveTimedOutCamera(stream);
  });
  await page.waitForFunction(() => window.timedOutTrack.readyState === 'ended');
  assert.equal(await page.evaluate(() => document.querySelector('#scanAssetVideo').srcObject === null), true);
  await page.locator('#close').click();
  await page.clock.resume();
  console.log('PASS: unanswered permission reaches a bounded deadline, unlocks retry and releases a stream granted after timeout.');

  await page.evaluate(() => {
    Object.defineProperty(navigator.mediaDevices, 'getUserMedia', { configurable: true, value: () => {
      window.mediaCalls++;
      return new Promise(resolve => { window.resolvePendingCamera = resolve; });
    } });
  });
  await page.locator('#open').click();
  await page.locator('#startAssetCamera').click();
  await page.waitForFunction(() => !!window.resolvePendingCamera);
  assert.equal(await page.locator('#startAssetCamera').isDisabled(), true);
  await page.locator('#close').click();
  await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width=320;canvas.height=240;
    const stream = canvas.captureStream(10); window.pendingTrack = stream.getTracks()[0];
    window.resolvePendingCamera(stream);
  });
  await page.waitForFunction(() => window.pendingTrack.readyState === 'ended');
  assert.equal(await page.locator('#scanAssetVideo').isVisible(), false);
  assert.equal(await page.evaluate(() => document.querySelector('#scanAssetVideo').srcObject === null), true);
  console.log('PASS: closing during camera permission releases a late stream and cannot resurrect the hidden scanner.');

  for (const [type, value] of [['qr', 'SANTA-CAMERA-QR-001'], ['code128', 'SANTA-CAMERA-128-002']]) {
    await page.evaluate(({ type, value }) => {
      const canvas = document.createElement('canvas');
      if (type === 'qr') {
        const qr = qrcodegen.QrCode.encodeText(value, qrcodegen.QrCode.Ecc.MEDIUM), scale = 8, border = 4;
        canvas.width = canvas.height = (qr.size + border * 2) * scale;
        const ctx = canvas.getContext('2d'); ctx.fillStyle='white';ctx.fillRect(0,0,canvas.width,canvas.height);ctx.fillStyle='black';
        for(let y=0;y<qr.size;y++)for(let x=0;x<qr.size;x++)if(qr.getModule(x,y))ctx.fillRect((x+border)*scale,(y+border)*scale,scale,scale);
      } else bwipjs.toCanvas(canvas,{bcid:'code128',text:value,scale:3,height:25,padding:20});
      Object.defineProperty(navigator.mediaDevices,'getUserMedia',{configurable:true,value:async()=>{
        window.mediaCalls++;const stream=canvas.captureStream(10);const track=stream.getTracks()[0];window.activeTestTrack=track;
        const paint=setInterval(()=>{if(track.readyState==='ended'){clearInterval(paint);return;}canvas.getContext('2d').drawImage(canvas,0,0);track.requestFrame();},80);
        return stream;
      }});
    }, { type, value });
    await page.locator('#open').click();
    await page.locator('#startAssetCamera').click();
    await page.waitForFunction(code => window.scanned.includes(code), value).catch(async error => {
      const state = await page.evaluate(() => ({ feedback: document.querySelector('#scanAssetFeedback').textContent,
        scanned: window.scanned, ready: document.querySelector('video').readyState, width: document.querySelector('video').videoWidth,
        errors: window.activeTestTrack?.readyState }));
      throw new Error(`${type}: ${error.message}; ${JSON.stringify(state)}; errors=${JSON.stringify(errors)}`);
    });
    assert.equal(await page.evaluate(() => window.activeTestTrack.readyState), 'ended');
    assert.equal(await page.locator('#startAssetCamera').isEnabled(), true);
    await page.locator('#close').click();
  }
  assert.deepEqual(await page.evaluate(() => window.scanned), ['SANTA-CAMERA-QR-001', 'SANTA-CAMERA-128-002']);
  assert.equal(await page.evaluate(() => typeof window.BarcodeDetector), 'undefined');
  assert.deepEqual(errors, []);
  assert.equal(requests.filter(path => path === '/vendor/zxing-browser-0.2.1.min.js').length, 1, 'The local fallback decoder must load once.');
  console.log('PASS: without native BarcodeDetector, actual QR and Code128 video frames decode locally, return exact codes and release camera tracks. No physical camera or external decoder service used.');
  const failurePage = await browser.newPage();
  await failurePage.goto(`http://127.0.0.1:${server.address().port}/`);
  failDecoderLoad = true;
  await failurePage.locator('#open').click();
  await failurePage.locator('#startAssetCamera').click();
  await failurePage.waitForFunction(() => document.querySelector('#scanAssetFeedback').textContent.includes('Barkod okuyucu yüklenemedi'));
  assert.equal(await failurePage.locator('#startAssetCamera').isEnabled(), true);
  assert.equal(await failurePage.evaluate(() => window.mediaCalls), 0);
  console.log('PASS: a failed local decoder download shows its error and permits an explicit retry before requesting camera access.');
} finally {
  await browser?.close();
  await new Promise(resolve => server.close(resolve));
}
