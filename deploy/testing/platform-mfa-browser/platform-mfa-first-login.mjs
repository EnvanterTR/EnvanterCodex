import assert from 'node:assert/strict';
import { createHmac, randomUUID } from 'node:crypto';
import { chromium } from 'playwright';

const baseUrl = process.env.ACCEPTANCE_BASE_URL;
const setupToken = process.env.ACCEPTANCE_PLATFORM_SETUP_TOKEN;
assert.ok(baseUrl, 'ACCEPTANCE_BASE_URL is required.');
assert.ok(setupToken, 'ACCEPTANCE_PLATFORM_SETUP_TOKEN is required.');
assert.match(new URL(baseUrl).hostname, /^(web|localhost|127\.0\.0\.1)$/,
  'The browser test may only target the isolated local acceptance service.');

const email = `platform-mfa-${randomUUID()}@example.invalid`;
const password = 'Synthetic-Platform-Password-46!';
const browserErrors = [];
const serverErrors = [];
let browser;

function base32Decode(value) {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  let bits = '';
  for (const character of value.replace(/=+$/, '').toUpperCase()) {
    const index = alphabet.indexOf(character);
    assert.notEqual(index, -1, 'The setup API returned an invalid TOTP secret.');
    bits += index.toString(2).padStart(5, '0');
  }
  const bytes = [];
  for (let index = 0; index + 8 <= bits.length; index += 8)
    bytes.push(Number.parseInt(bits.slice(index, index + 8), 2));
  return Buffer.from(bytes);
}

function totp(secret, timestamp = Date.now()) {
  const counter = Math.floor(timestamp / 30_000);
  const message = Buffer.alloc(8);
  message.writeBigUInt64BE(BigInt(counter));
  const digest = createHmac('sha1', base32Decode(secret)).update(message).digest();
  const offset = digest[digest.length - 1] & 0x0f;
  const code = (digest.readUInt32BE(offset) & 0x7fffffff) % 1_000_000;
  return code.toString().padStart(6, '0');
}

async function submitLogin(page, secondFactor) {
  await page.locator('#loginForm [name="email"]').fill(email);
  await page.locator('#loginForm [name="password"]').fill(password);
  await page.getByRole('button', { name: 'Güvenli giriş yap' }).click();
  if (secondFactor) {
    const factor = page.locator('#loginForm [name="twoFactorCode"]');
    await factor.waitFor({ state: 'visible' });
    await factor.fill(secondFactor);
    await page.getByRole('button', { name: 'Güvenli giriş yap' }).click();
  }
}

async function logout(page) {
  await page.locator('#logout').click();
  await page.waitForFunction(() => !document.querySelector('#loginWrap').classList.contains('login-hidden'));
}

try {
  browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  page.setDefaultTimeout(10_000);
  page.on('pageerror', error => browserErrors.push(error.message));
  page.on('response', response => {
    if (response.status() >= 500) serverErrors.push(`${response.status()} ${new URL(response.url()).pathname}`);
  });
  page.on('request', async request => {
    if (new URL(request.url()).pathname === '/api/platform/bootstrap' && request.method() === 'POST') {
      const headers = await request.allHeaders();
      const cookieNames = (headers.cookie || '').split(';').map(value => value.trim().split('=', 1)[0]).filter(Boolean);
      console.log(`Bootstrap request diagnostics: csrf-header=${Boolean(headers['x-csrf-token'])}; cookie-names=${cookieNames.join(',') || 'none'}.`);
    }
  });
  page.on('response', response => {
    const path = new URL(response.url()).pathname;
    if (path === '/api/platform/bootstrap' || path === '/api/session/2fa/setup')
      console.log(`${path} response diagnostics: HTTP ${response.status()}.`);
  });

  let ready = false;
  for (let attempt = 0; attempt < 60; attempt++) {
    try {
      const status = await page.request.get(`${baseUrl}/api/status`, { timeout: 2_000 });
      if (status.status() === 200 && (await status.json()).status === 'ready') {
        ready = true;
        break;
      }
    } catch { /* The disposable app may still be starting. */ }
    await page.waitForTimeout(1_000);
  }
  assert.ok(ready, 'The isolated application and migrated database must become ready.');

  // Bootstrap one synthetic platform admin through the same setup page a first-time owner uses.
  let releaseBootstrapStatus;
  let statusRequestStarted;
  let statusRouteCompleted;
  const bootstrapStatusGate = new Promise(resolve => { releaseBootstrapStatus = resolve; });
  const bootstrapStatusRequested = new Promise(resolve => { statusRequestStarted = resolve; });
  const bootstrapStatusRouted = new Promise(resolve => { statusRouteCompleted = resolve; });
  await page.route('**/api/platform/bootstrap/status', async route => {
    statusRequestStarted();
    await bootstrapStatusGate;
    await route.continue();
    statusRouteCompleted();
  });
  await page.goto(`${baseUrl}/platform-setup.html`);
  await page.locator('#setupForm').waitFor({ state: 'visible' });
  await bootstrapStatusRequested;
  const createAdminButton = page.getByRole('button', { name: 'Platform yöneticisini oluştur' });
  assert.equal(await createAdminButton.isDisabled(), true,
    'The setup form must remain disabled while server readiness is loading.');
  releaseBootstrapStatus();
  await bootstrapStatusRouted;
  await page.unroute('**/api/platform/bootstrap/status');

  let releaseCsrf;
  let csrfRequestStarted;
  let csrfRouteCompleted;
  const csrfGate = new Promise(resolve => { releaseCsrf = resolve; });
  const csrfRequested = new Promise(resolve => { csrfRequestStarted = resolve; });
  const csrfRouted = new Promise(resolve => { csrfRouteCompleted = resolve; });
  await page.route('**/api/session/csrf', async route => {
    csrfRequestStarted();
    await csrfGate;
    await route.continue();
    csrfRouteCompleted();
  });
  await csrfRequested;
  assert.equal(await createAdminButton.isDisabled(), true,
    'The setup form must remain disabled until the CSRF token and cookie are ready.');
  releaseCsrf();
  await csrfRouted;
  await page.unroute('**/api/session/csrf');
  await page.waitForFunction(() => !document.querySelector('#setupForm button[type="submit"]').disabled);

  await page.getByLabel('Ad soyad').fill('Synthetic Platform Admin');
  await page.getByLabel('Yönetici e-posta adresi').fill(email);
  await page.getByLabel('Yeni parola').fill(password);
  await page.getByLabel('Platform kurulum anahtarı').fill(setupToken);
  await page.getByRole('button', { name: 'Platform yöneticisini oluştur' }).click();
  await page.locator('#message').waitFor({ state: 'visible' });
  const bootstrapMessage = (await page.locator('#message').innerText()).trim();
  assert.match(bootstrapMessage, /Platform hesabı oluşturuldu/,
    `Synthetic platform bootstrap was rejected: ${bootstrapMessage}`);

  // First password login must lead to mandatory setup, with no platform panel exposed.
  await page.goto(`${baseUrl}/platform-admin.html`);
  await page.locator('#platformTwoFactorField').waitFor({ state: 'hidden' });
  await submitLogin(page);
  await page.getByRole('heading', { name: 'İlk giriş güvenlik kurulumu' }).waitFor();
  assert.equal(await page.locator('#appShell').isVisible(), false);
  assert.equal(await page.locator('#platformMfaSetupWrap').evaluate(element => !element.classList.contains('login-hidden')), true);
  assert.match(await page.locator('#platformMfaSetupWrap').innerText(), /Google Authenticator/);
  assert.match(await page.locator('#platformMfaSetupWrap').innerText(), /kurtarma kodlarını güvenli bir yere kaydedin/i);

  // Start setup, create a valid TOTP in memory, and enable MFA.
  await page.getByLabel('Mevcut parola').fill(password);
  const startMfaButton = page.getByRole('button', { name: 'Kurulumu başlat' });
  await startMfaButton.click();
  await page.waitForFunction(() => !document.querySelector('#platformMfaStart').disabled);
  assert.equal(await page.locator('#platformMfaFeedback').innerText(),
    'Anahtarı Authenticator uygulamasına girin, ardından uygulamanın ürettiği güncel kodu doğrulayın.',
    `MFA setup UI response: ${(await page.locator('#platformMfaFeedback').innerText()).trim()}`);
  const secretInput = page.locator('#platformMfaSecret');
  await secretInput.waitFor({ state: 'visible' });
  const secret = await secretInput.inputValue();
  assert.match(secret, /^[A-Z2-7]+=*$/);
  await page.locator('#platformMfaCode').fill(totp(secret));
  await page.locator('#platformMfaEnable').click();
  const recoveryPanel = page.locator('#platformMfaRecoveryPanel');
  await recoveryPanel.waitFor({ state: 'visible' });
  const recoveryCodes = (await page.locator('#platformMfaRecoveryCodes').innerText()).trim().split(/\s+/);
  assert.equal(recoveryCodes.length, 10, 'The UI must show ten one-time recovery codes.');
  assert.equal(await page.locator('#platformMfaContinue').isEnabled(), false,
    'The panel must stay gated until recovery codes are acknowledged.');
  await page.getByLabel('Kurtarma kodlarını güvenli yere kaydettim.').check();
  assert.equal(await page.locator('#platformMfaContinue').isEnabled(), true);
  await page.locator('#platformMfaContinue').click();
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.getByRole('heading', { name: 'Genel Bakış' }).waitFor();
  assert.equal(await page.locator('#platformMfaRecoveryCodes').innerText(), '',
    'Recovery codes must be cleared from the page after acknowledgement.');

  // The authenticated platform session must survive a full page reload.
  await page.reload();
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.getByRole('heading', { name: 'Genel Bakış' }).waitFor();

  // A recovery code works once, and replaying the same code is rejected.
  await logout(page);
  await submitLogin(page, recoveryCodes[0]);
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.getByRole('heading', { name: 'Genel Bakış' }).waitFor();
  await logout(page);
  await submitLogin(page, recoveryCodes[0]);
  await page.locator('#loginError').waitFor({ state: 'visible' });
  assert.notEqual((await page.locator('#loginError').innerText()).trim(), '',
    'Reusing a recovery code must show a login error.');
  assert.equal(await page.locator('#appShell').isVisible(), false);

  // A fresh TOTP still works after the failed recovery-code replay.
  await submitLogin(page, totp(secret));
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.getByRole('heading', { name: 'Genel Bakış' }).waitFor();
  assert.equal(browserErrors.length, 0, `JavaScript exceptions: ${browserErrors.join(' | ')}`);
  assert.equal(serverErrors.length, 0, `Server errors: ${serverErrors.join(' | ')}`);

  console.log('PASS: synthetic platform bootstrap, first password login, MFA setup, recovery acknowledgement, session reload, one-time recovery, replay rejection, and TOTP login.');
} finally {
  await browser?.close();
}
