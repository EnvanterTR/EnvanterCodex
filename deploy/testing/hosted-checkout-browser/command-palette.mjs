import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const syntheticCompanyId = '11111111-1111-4111-8111-111111111111';
const syntheticUserId = '22222222-2222-4222-8222-222222222222';
const appHtml = await readFile('/checkout-site/company-app.html');
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const apiRequests = [];
const externalRequests = [];
const loginAttempts = [];
let subscriptionMode = 'full';
let loginMode = false;
let companyMfaEnrollmentMode = false;
let loginPendingMigrationCount = 0;
let companyRole = 'viewer';
let companySecurityPolicy = { passwordMinimumLength: 12, sessionDurationHours: 720, requireMfa: false };
let notificationPreferences = { enabledKinds: ['maintenance', 'audit', 'license', 'warranty', 'return', 'reservation'], emailEnabledKinds: [] };
let integrationTokens = [];
let sessionDevices = [];
const syntheticIntegrationSecret = 'etr_live_synthetic-one-time-secret';
const syntheticAuthenticatorUri = 'otpauth://totp/EnvanterTR%3Asynthetic%40example.invalid?secret=JBSWY3DPEHPK3PXP&issuer=EnvanterTR&digits=6&period=30';
let invalidAuthenticatorUri = false;
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    response.end(appHtml);
    return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'text/css; charset=utf-8' });
    response.end('');
    return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(qrCodeGenerator);
    return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(appScripts.get(url.pathname));
    return;
  }
  if (url.pathname.startsWith('/api/')) {
    const record = { method: request.method, path: url.pathname, csrf: request.headers['x-csrf-token'] };
    if (request.method !== 'GET') {
      let raw = ''; for await (const chunk of request) raw += chunk;
      record.body = raw ? JSON.parse(raw) : null;
    }
    apiRequests.push(record);
    if (request.method !== 'GET') {
      if (loginMode && request.method === 'POST' && url.pathname === '/api/session/login') {
        loginAttempts.push(record);
        response.writeHead(companyMfaEnrollmentMode ? 409 : 401, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
        response.end(JSON.stringify(companyMfaEnrollmentMode
          ? { companyMfaSetupRequired: true, setupTicket: 'synthetic-one-time-mfa-ticket', companyName: 'Synthetic Viewer Company', message: 'Authenticator kurulumu gerekli.' }
          : { message: 'Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor.' }));
        return;
      }
      if (request.method === 'PUT' && url.pathname === '/api/company-security' && record.csrf === 'synthetic-viewer-csrf') {
        companySecurityPolicy = { ...record.body };
        response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
        response.end(JSON.stringify(companySecurityPolicy));
        return;
      }
      if (request.method === 'POST' && url.pathname === '/api/session/company-mfa/setup' && record.csrf === 'synthetic-viewer-csrf') {
        response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
        response.end(JSON.stringify({ secret: 'JBSWY3DPEHPK3PXP', authenticatorUri: syntheticAuthenticatorUri, companyName: 'Synthetic Viewer Company' }));
        return;
      }
      if (request.method === 'POST' && url.pathname === '/api/session/company-mfa/verify' && record.csrf === 'synthetic-viewer-csrf') {
        response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
        response.end(JSON.stringify({ authenticated: true, companyId: syntheticCompanyId, role: 'admin', platformAdmin: false,
          name: 'Synthetic Admin', recoveryCodes: Array.from({ length: 10 }, (_, index) => `SYNTHETIC-RECOVERY-${index + 1}`) }));
        return;
      }
      if (request.method === 'POST' && url.pathname === '/api/integrations/tokens' && record.csrf === 'synthetic-viewer-csrf') {
        const token = {
          id: '33333333-3333-4333-8333-333333333333', name: record.body.name,
          tokenPrefix: syntheticIntegrationSecret.slice(0, 16), scopes: record.body.scopes,
          createdAt: new Date().toISOString(), expiresAt: record.body.expiresAt, lastUsedAt: null,
          revokedAt: null, isExpired: false
        };
        integrationTokens = [token, ...integrationTokens.filter(item => item.id !== token.id)];
        response.writeHead(201, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
        response.end(JSON.stringify({ ...token, secret: syntheticIntegrationSecret }));
        return;
      }
      if (request.method === 'POST' && url.pathname === '/api/session/2fa/setup' && record.csrf === 'synthetic-viewer-csrf') {
        response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
        response.end(JSON.stringify({ secret: 'JBSWY3DPEHPK3PXP', authenticatorUri: invalidAuthenticatorUri ? 'https://invalid.example/totp' : syntheticAuthenticatorUri }));
        return;
      }
      const revokeSessionMatch = url.pathname.match(/^\/api\/session\/devices\/([0-9a-f-]+)\/revoke$/i);
      if (request.method === 'POST' && revokeSessionMatch && record.csrf === 'synthetic-viewer-csrf') {
        sessionDevices = sessionDevices.filter(session => session.id !== revokeSessionMatch[1]);
        response.writeHead(204);
        response.end();
        return;
      }
      const revokeTokenMatch = url.pathname.match(/^\/api\/integrations\/tokens\/([0-9a-f-]+)\/revoke$/i);
      if (request.method === 'POST' && revokeTokenMatch && record.csrf === 'synthetic-viewer-csrf') {
        integrationTokens = integrationTokens.map(token => token.id === revokeTokenMatch[1]
          ? { ...token, revokedAt: token.revokedAt || new Date().toISOString() } : token);
        response.writeHead(204);
        response.end();
        return;
      }
      if (request.method === 'PUT' && url.pathname === '/api/notifications/preferences' && record.csrf === 'synthetic-viewer-csrf') {
        notificationPreferences = {
          enabledKinds: [...new Set((record.body?.enabledKinds || []).map(value => String(value).toLowerCase()))],
          emailEnabledKinds: [...new Set((record.body?.emailEnabledKinds || []).map(value => String(value).toLowerCase()))]
        };
        response.writeHead(200, { 'content-type': 'application/json' });
        response.end(JSON.stringify(notificationPreferences));
        return;
      }
      if (request.method === 'PUT' && url.pathname === '/api/appearance/preferences' && record.csrf === 'synthetic-viewer-csrf') {
        response.writeHead(200, { 'content-type': 'application/json' });
        response.end(JSON.stringify(record.body));
        return;
      }
      response.writeHead(405, { 'content-type': 'application/json' });
      response.end(JSON.stringify({ message: 'Read-only browser acceptance mock.' }));
      return;
    }
    if (loginMode && url.pathname === '/api/session') {
      response.writeHead(401, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
      response.end(JSON.stringify({ message: 'Synthetic signed-out session.' }));
      return;
    }
    const data = url.pathname === '/api/status'
      ? loginMode && loginPendingMigrationCount > 0
        ? { status: 'migration-required', migrationCount: 91 + loginPendingMigrationCount, pendingMigrationCount: loginPendingMigrationCount }
        : { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 }
      : url.pathname === '/api/setup/status'
        ? { setupRequired: false }
        : url.pathname === '/api/session/csrf'
          ? { requestToken: 'synthetic-viewer-csrf' }
          : url.pathname === '/api/session'
            ? {
              companyId: syntheticCompanyId,
              companyName: 'Synthetic Viewer Company',
              userId: syntheticUserId,
              name: 'Synthetic Viewer',
              role: companyRole,
              hasPhoto: false,
              subscriptionAccess: { mode: subscriptionMode, status: subscriptionMode === 'readOnly' ? 'Expired' : 'Active', planName: 'Synthetic cloud plan' }
            }
            : url.pathname === '/api/company/branding'
              ? { name: 'Synthetic Viewer Company', brandPrimaryColor: '#2563EB', hasLogo: false }
            : url.pathname === '/api/company/label-settings'
                ? { isConfigured: false, settings: null }
              : url.pathname === '/api/notifications/preferences'
                ? notificationPreferences
                : url.pathname === '/api/company-security'
                  ? companySecurityPolicy
              : url.pathname === '/api/integrations/tokens'
                ? integrationTokens
                : url.pathname === '/api/session/2fa'
                  ? { enabled: false }
                  : url.pathname === '/api/session/devices'
                    ? { sessions: sessionDevices, truncated: false }
              : url.pathname === '/api/assets/custom-fields'
                ? []
              : url.pathname === '/api/appearance/preferences'
                ? { theme: 'system', language: 'tr' }
                : { items: [], total: 0, page: 1, pageSize: 50 };
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
    response.end(JSON.stringify(data));
    return;
  }
  response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
  response.end('Not found');
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));
page.on('request', request => { if (new URL(request.url()).origin !== `http://127.0.0.1:${address.port}`) externalRequests.push(request.url()); });
page.on('console', message => { if (message.type() === 'error') console.error(`BROWSER ${message.text()}`); });

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  try {
    await page.waitForFunction(() => {
      const memberLink = document.querySelector('#membersNav');
      return memberLink?.hidden === true && !document.body.classList.contains('auth-required');
    }, null, { timeout: 15000 });
  } catch (error) {
    console.error(JSON.stringify({
      apiRequests,
      bodyClass: await page.locator('body').getAttribute('class'),
      memberHidden: await page.locator('#membersNav').evaluate(element => element.hidden),
      authError: await page.locator('#authError').innerText(),
      pageErrors
    }));
    throw error;
  }
  assert.ok(apiRequests.some(item => item.path === '/api/session' && item.method === 'GET'),
    'The app must initialize from the synthetic viewer session before checking its navigation.');

  await page.setViewportSize({ width: 390, height: 844 });
  const sidebar = page.locator('#sidebar'), mobileMenu = page.locator('#mobileMenu');
  assert.equal(await mobileMenu.isVisible(), true, 'The mobile menu control must be available at phone width.');
  const dispatchTouchSwipe = (target, fromX, fromY, toX, toY) => target.evaluate((element, coords) => {
    const send = (type, x, y) => element.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerType: 'touch', isPrimary: true, clientX: x, clientY: y }));
    send('pointerdown', coords.fromX, coords.fromY);
    send('pointerup', coords.toX, coords.toY);
  }, { fromX, fromY, toX, toY });
  await dispatchTouchSwipe(page.locator('body'), 3, 360, 112, 365);
  await sidebar.waitFor({ state: 'visible' });
  assert.equal(await sidebar.evaluate(element => element.classList.contains('open') && !element.inert), true,
    'A right swipe from the left viewport edge must open the mobile sidebar and make its navigation available.');
  assert.equal(await mobileMenu.getAttribute('aria-expanded'), 'true');
  await dispatchTouchSwipe(sidebar, 190, 360, 92, 364);
  assert.equal(await sidebar.evaluate(element => !element.classList.contains('open') && element.inert), true,
    'A left swipe starting inside the open sidebar must close it and restore inert navigation.');
  assert.equal(await mobileMenu.getAttribute('aria-expanded'), 'false');
  await dispatchTouchSwipe(page.locator('body'), 3, 260, 110, 350);
  assert.equal(await sidebar.evaluate(element => !element.classList.contains('open')), true,
    'A primarily vertical gesture from the screen edge must not open navigation.');
  const swipeBlockedDialog = page.locator('#keyboardShortcutsDialog');
  await swipeBlockedDialog.evaluate(element => element.showModal());
  await dispatchTouchSwipe(page.locator('body'), 3, 360, 112, 365);
  assert.equal(await sidebar.evaluate(element => !element.classList.contains('open')), true,
    'A swipe must not open navigation while a modal dialog is active.');
  await swipeBlockedDialog.evaluate(element => element.close());
  await page.setViewportSize({ width: 1280, height: 900 });
  await dispatchTouchSwipe(page.locator('body'), 3, 360, 112, 365);
  assert.equal(await sidebar.evaluate(element => !element.classList.contains('open')), true,
    'Touch swipes must not change sidebar state in desktop layout.');

  await page.locator('#pageTitle').click();
  await page.keyboard.press('/');
  const globalSearch = page.locator('#globalSearch');
  assert.equal(await globalSearch.evaluate(input => document.activeElement === input), true, 'Slash should focus global search when not typing.');
  assert.equal(await globalSearch.getAttribute('aria-keyshortcuts'), '/');
  await page.keyboard.press('/');
  assert.equal(await globalSearch.evaluate(input => document.activeElement === input), true, 'Slash typed in the search field must not steal focus.');
  assert.equal(await globalSearch.inputValue(), '/', 'Slash should remain ordinary text while a text field has focus.');
  await globalSearch.fill('');

  await page.locator('#pageTitle').click();
  await page.evaluate(() => { if (document.activeElement instanceof HTMLElement) document.activeElement.blur(); });
  await page.keyboard.press('Control+n');
  const deniedShortcutToast = page.locator('#toast');
  await deniedShortcutToast.waitFor({ state: 'visible' });
  assert.match(await deniedShortcutToast.innerText(), /oluşturma yetkiniz bulunmuyor/i);
  assert.equal(await page.locator('#newAssetButton').isHidden(), true, 'Ctrl+N must not expose asset creation to a viewer.');

  await page.keyboard.press('?');
  const shortcutsDialog = page.locator('#keyboardShortcutsDialog');
  await shortcutsDialog.waitFor({ state: 'visible' });
  assert.equal(await page.locator('#helpButton').getAttribute('aria-keyshortcuts'), '?');
  await page.keyboard.press('Escape');
  await shortcutsDialog.waitFor({ state: 'hidden' });

  await page.keyboard.press('Control+k');
  const dialog = page.locator('#commandPaletteDialog');
  await dialog.waitFor({ state: 'visible' });
  await page.keyboard.press('Escape');
  await dialog.waitFor({ state: 'hidden' });

  await page.keyboard.press('Control+k');

  try { await dialog.waitFor({ state: 'visible', timeout: 5000 }); }
  catch (error) {
    console.error('COMMAND_PALETTE_SHORTCUT_STATE', JSON.stringify(await page.evaluate(() => ({
      bodyClass: document.body.className,
      activeElement: document.activeElement?.id || document.activeElement?.tagName,
      dialogOpen: document.querySelector('#commandPaletteDialog')?.open,
      dataStatus: document.querySelector('#dataStatus')?.textContent,
      expiryNotice: document.querySelector('#subscriptionExpiryNotice')?.innerText,
      language: document.querySelector('#languagePreference')?.value
    }))));
    throw error;
  }
  const search = page.locator('#commandPaletteSearch');
  await search.fill('Uygulama kullanıcıları');
  assert.equal(await page.locator('#commandPaletteResults .command-option').count(), 0,
    'The page must not expose company member administration to a viewer role through Ctrl+K.');
  assert.match(await page.locator('#commandPaletteResults [role="status"]').innerText(), /Eşleşen sayfa veya işlem bulunamadı/);

  await search.fill('Varlıklar');
  const assetDestination = page.locator('#commandPaletteResults .command-option').filter({ hasText: 'Varlıklar' }).first();
  await assetDestination.waitFor({ state: 'visible' });
  await assetDestination.click();
  await dialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.querySelector('#pageTitle')?.textContent === 'Varlık kayıtları');

  const notificationButton = page.locator('#notificationButton');
  const notificationPanel = page.locator('#notificationPanel');
  await notificationButton.click();
  await notificationPanel.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#notificationSummary')?.textContent.includes('0 okunmamış'));
  assert.equal(await notificationButton.getAttribute('aria-expanded'), 'true');
  assert.match(await page.locator('#notificationList').innerText(), /bu dönemde yaklaşan iş yok/i);
  assert.ok(apiRequests.some(item => item.path === '/api/calendar' && item.method === 'GET'));
  assert.ok(apiRequests.some(item => item.path === '/api/notifications/preferences' && item.method === 'GET'));
  await page.locator('#pageTitle').click();
  await notificationPanel.waitFor({ state: 'hidden' });
  assert.equal(await notificationButton.getAttribute('aria-expanded'), 'false', 'Outside click must close the notification panel and reset aria-expanded.');

  await page.locator('#sidebar [data-group="settings"]').click();
  const notificationsNav = page.locator('#sidebar [data-page="Bildirimler"]');
  await notificationsNav.click();
  await page.waitForFunction(() => document.querySelector('#pageTitle')?.textContent === 'Bildirimler');
  const preferencesForm = page.locator('#companyNotificationPreferencesForm');
  await preferencesForm.waitFor({ state: 'visible' });
  await page.getByText('Bildirim tercihleri yüklendi.').waitFor();
  const auditPreference = preferencesForm.locator('input[name="kind"][value="audit"]');
  const reservationPreference = preferencesForm.locator('input[name="kind"][value="reservation"]');
  const warrantyEmailPreference = preferencesForm.locator('input[name="emailKind"][value="warranty"]');
  await page.waitForFunction(() => document.querySelector('#companyNotificationPreferencesForm input[name="kind"][value="reservation"]')?.checked === true);
  assert.equal(await auditPreference.isChecked(), true, 'The form must load the current in-app notification preference.');
  const preferenceWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/notifications/preferences' && item.method === 'PUT').length;
  await reservationPreference.uncheck();
  await page.locator('#companyNotificationPreferencesReset').click();
  assert.equal(apiRequests.filter(item => item.path === '/api/notifications/preferences' && item.method === 'PUT').length, preferenceWritesBeforeCancel,
    'Discarding notification edits must not write preferences.');
  await page.waitForFunction(() => document.querySelector('#companyNotificationPreferencesForm input[name="kind"][value="reservation"]')?.checked === true);
  assert.equal(await reservationPreference.isChecked(), true, 'Reset must restore the persisted choices.');
  await auditPreference.uncheck();
  await warrantyEmailPreference.check();
  await page.locator('#companyNotificationPreferencesSave').click();
  await page.getByText('Bildirim tercihleri kaydedildi.').waitFor();
  const notificationWrite = apiRequests.find(item => item.path === '/api/notifications/preferences' && item.method === 'PUT');
  assert.deepEqual(notificationWrite?.body, {
    enabledKinds: ['maintenance', 'license', 'warranty', 'return', 'reservation'],
    emailEnabledKinds: ['warranty']
  });
  assert.equal(notificationWrite?.csrf, 'synthetic-viewer-csrf');
  await page.locator('#companyNotificationPreferencesRefresh').click();
  assert.equal(await auditPreference.isChecked(), false, 'Saved notification choices must reload from the API-backed synthetic preference record.');
  assert.equal(await warrantyEmailPreference.isChecked(), true);
  await page.locator('#sidebar [data-page="Genel Bakış"]').click();
  await notificationPanel.waitFor({ state: 'hidden' });

  await page.locator('#userAvatar').click();
  const accountDialog = page.locator('#accountPhotoDialog');
  await accountDialog.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#accountSessionsList')?.textContent.includes('Etkin oturum bulunamadı'));
  assert.ok(apiRequests.some(item => item.path === '/api/session/devices' && item.method === 'GET'));
  assert.ok(apiRequests.some(item => item.path === '/api/session/2fa' && item.method === 'GET'));
  const writesBeforeAccountBackdrop = apiRequests.filter(item => item.method !== 'GET').length;
  await page.mouse.click(5, 5);
  await accountDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.method !== 'GET').length, writesBeforeAccountBackdrop,
    'Outside-click dismissal of the account/security window must not change security settings.');
  const currentSessionId = '44444444-4444-4444-8444-444444444444';
  const remoteSessionId = '55555555-5555-4555-8555-555555555555';
  sessionDevices = [
    { id: currentSessionId, current: true, userAgent: 'Synthetic current browser', ipAddress: '192.0.2.10', lastSeenAt: '2026-10-15T09:00:00Z' },
    { id: remoteSessionId, current: false, userAgent: 'Synthetic remote browser', ipAddress: '198.51.100.23', lastSeenAt: '2026-10-15T08:45:00Z' }
  ];
  await page.locator('#userAvatar').click();
  await accountDialog.waitFor({ state: 'visible' });
  const sessionsList = page.locator('#accountSessionsList');
  await sessionsList.getByText('Synthetic current browser').waitFor();
  await sessionsList.getByText('Synthetic remote browser').waitFor();
  await sessionsList.getByText('Bu cihaz (şu an)').waitFor();
  await sessionsList.getByText('198.51.100.23').waitFor();
  const remoteSessionCard = sessionsList.locator('article').filter({ hasText: 'Synthetic remote browser' });
  const sessionRevokeDialog = page.locator('#componentActionDialog');
  const sessionRevokesBeforeCancel = apiRequests.filter(item => /^\/api\/session\/devices\/.+\/revoke$/.test(item.path) && item.method === 'POST').length;
  await remoteSessionCard.getByRole('button', { name: 'Oturumu kapat' }).click();
  await sessionRevokeDialog.waitFor({ state: 'visible' });
  assert.match(await sessionRevokeDialog.innerText(), /uzaktan kapatmak/);
  await sessionRevokeDialog.locator('[data-close-component-action]').first().click();
  await sessionRevokeDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => /^\/api\/session\/devices\/.+\/revoke$/.test(item.path) && item.method === 'POST').length, sessionRevokesBeforeCancel,
    'Canceling remote session revocation must not send a write request.');
  assert.equal(sessionDevices.length, 2, 'Canceling must preserve both synthetic sessions.');
  await remoteSessionCard.getByRole('button', { name: 'Oturumu kapat' }).click();
  await sessionRevokeDialog.waitFor({ state: 'visible' });
  await page.locator('#componentActionSubmit').click();
  await sessionRevokeDialog.waitFor({ state: 'hidden' });
  await sessionsList.getByText('Synthetic remote browser').waitFor({ state: 'detached' });
  await sessionsList.getByText('Synthetic current browser').waitFor();
  const remoteSessionRevoke = apiRequests.find(item => item.path === `/api/session/devices/${remoteSessionId}/revoke` && item.method === 'POST');
  assert.equal(remoteSessionRevoke?.csrf, 'synthetic-viewer-csrf', 'Confirmed revocation must include CSRF protection.');
  assert.equal(remoteSessionRevoke?.body && Object.keys(remoteSessionRevoke.body).length, 0);
  assert.deepEqual(sessionDevices.map(session => session.id), [currentSessionId], 'The synthetic API state must retain the current session and revoke only the selected remote one.');
  const sessionListReadsBeforeRefresh = apiRequests.filter(item => item.path === '/api/session/devices' && item.method === 'GET').length;
  await page.locator('#refreshAccountSessions').click();
  await sessionsList.getByText('Synthetic current browser').waitFor();
  assert.equal(apiRequests.filter(item => item.path === '/api/session/devices' && item.method === 'GET').length, sessionListReadsBeforeRefresh + 1,
    'The explicit refresh control must request the current session list again.');
  assert.equal(await sessionsList.getByText('Synthetic remote browser').count(), 0);
  await page.locator('#closeAccountPhoto').click();
  await accountDialog.waitFor({ state: 'hidden' });
  sessionDevices = [];
  console.log('PASS: account session UI lists current and remote devices, preserves canceled revocation, confirms a CSRF-protected remote revoke, and refreshes the remaining session list.');
  await page.locator('#userAvatar').click();
  await accountDialog.waitFor({ state: 'visible' });
  await page.locator('#closeAccountPhoto').click();
  await accountDialog.waitFor({ state: 'hidden' });
  await page.locator('#userAvatar').click();
  await accountDialog.waitFor({ state: 'visible' });
  let releaseAccountMfaStatus;
  const accountMfaStatusGate = new Promise(resolve => { releaseAccountMfaStatus = resolve; });
  await page.route('**/api/session/2fa', async route => {
    await accountMfaStatusGate;
    await route.continue();
  });
  await page.locator('#openAccountTwoFactor').click();
  const accountTwoFactorDialog = page.locator('#accountTwoFactorDialog');
  await accountTwoFactorDialog.waitFor({ state: 'visible' });
  try {
    assert.equal(await page.locator('#twoFactorCurrentPassword').isDisabled(), true,
      'MFA password input must wait for server status instead of clearing a password entered during loading.');
    assert.equal(await page.locator('#startTwoFactorSetup').isDisabled(), true,
      'MFA setup must remain gated while status is loading.');
  } finally { releaseAccountMfaStatus(); }
  assert.match(await accountTwoFactorDialog.innerText(), /Google Authenticator, Microsoft Authenticator veya TOTP uyumlu başka bir uygulama/);
  assert.match(await accountTwoFactorDialog.innerText(), /Google\/Microsoft hesabı, abonelik, SMS ya da ayrıca bir hizmet satın almanız gerekmez/);
  await page.locator('#twoFactorCurrentPassword').fill('Synthetic-only-password');
  assert.equal(await page.locator('#twoFactorQrSetupHint').isVisible(), true);
  assert.match(await page.locator('#twoFactorQrSetupHint').innerText(), /QR/);
  assert.equal(await page.locator('#twoFactorQr').isVisible(), false,
    'The provisioning QR must not appear before current-password verification.');
  await page.locator('#startTwoFactorSetup').click();
  const twoFactorSecret = page.locator('#twoFactorSecret');
  try {
    await page.waitForFunction(() => document.querySelector('#twoFactorSecret')?.textContent === 'JBSWY3DPEHPK3PXP');
  } catch (error) {
    console.error(JSON.stringify({ stage: 'account-mfa-setup', pageErrors,
      feedback: await page.locator('#twoFactorFeedback').innerText(),
      requests: apiRequests.filter(item => item.path.includes('/2fa') || item.path.includes('/csrf'))
        .map(item => ({ method: item.method, path: item.path, hasCsrf: Boolean(item.csrf) })) }));
    throw error;
  }
  const twoFactorQr = page.locator('#twoFactorQr');
  assert.equal(await twoFactorQr.isVisible(), true, 'The account MFA setup must display a locally generated QR code.');
  assert.equal(await twoFactorQr.getAttribute('alt'), 'Authenticator uygulamasıyla taranacak kurulum QR kodu');
  assert.match(await page.locator('#twoFactorFeedback').textContent(), /QR kodunu tarayın veya kurulum anahtarını elle girin/);
  assert.match(decodeURIComponent((await twoFactorQr.getAttribute('src')).split(',')[1]), /<svg[\s\S]*<path d="M/,
    'The QR image must contain a generated SVG matrix, not a remote image or placeholder.');
  const expectedQrDataUrl = await page.evaluate(uri => {
    const qr = qrcodegen.QrCode.encodeText(uri, qrcodegen.QrCode.Ecc.MEDIUM), quietZone = 4, viewSize = qr.size + quietZone * 2;
    let path = '';
    for (let y = 0; y < qr.size; y++) for (let x = 0; x < qr.size; x++) if (qr.getModule(x, y)) path += `M${x + quietZone},${y + quietZone}h1v1h-1z`;
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${viewSize} ${viewSize}" shape-rendering="crispEdges"><rect width="100%" height="100%" fill="#fff"/><path d="${path}" fill="#111"/></svg>`;
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
  }, syntheticAuthenticatorUri);
  assert.equal(await twoFactorQr.getAttribute('src'), expectedQrDataUrl,
    'The QR matrix must encode the exact otpauth URI returned by the authenticated setup API.');
  assert.deepEqual(externalRequests, [], 'The MFA setup key and QR must not be sent to external services.');
  assert.ok(apiRequests.some(item => item.path === '/api/session/2fa/setup' && item.method === 'POST' && item.csrf === 'synthetic-viewer-csrf'));
  await page.locator('#closeAccountTwoFactor').click();
  await accountTwoFactorDialog.waitFor({ state: 'hidden' });
  assert.equal(await twoFactorSecret.textContent(), '', 'The close button must immediately erase the TOTP enrollment secret from the DOM.');
  assert.equal(await twoFactorQr.getAttribute('src'), null, 'The close button must remove the generated QR image containing the TOTP URI.');
  assert.equal(await page.locator('#twoFactorCurrentPassword').inputValue(), '');
  await page.locator('#userAvatar').click();
  await accountDialog.waitFor({ state: 'visible' });
  await page.locator('#openAccountTwoFactor').click();
  await accountTwoFactorDialog.waitFor({ state: 'visible' });
  await page.locator('#twoFactorCurrentPassword').fill('Synthetic-only-password');
  invalidAuthenticatorUri = true;
  await page.locator('#startTwoFactorSetup').click();
  try {
    await page.waitForFunction(() => document.querySelector('#twoFactorSecret')?.textContent === 'JBSWY3DPEHPK3PXP', null, { timeout: 15000 });
  } catch (error) {
    console.error(JSON.stringify({
      recentTwoFactorRequests: apiRequests.filter(item => item.path.startsWith('/api/session/2fa')),
      secretText: await page.locator('#twoFactorSecret').textContent(),
      feedback: await page.locator('#twoFactorFeedback').textContent(),
      setupButtonHidden: await page.locator('#startTwoFactorSetup').evaluate(element => element.hidden),
      setupButtonDisabled: await page.locator('#startTwoFactorSetup').isDisabled(),
      pageErrors
    }));
    throw error;
  }
  assert.equal(await twoFactorQr.isVisible(), false, 'An invalid provisioning URI must not produce a QR image.');
  assert.equal(await twoFactorQr.getAttribute('src'), null);
  assert.match(await page.locator('#twoFactorFeedback').textContent(), /QR kod oluşturulamadı\. Kurulum anahtarını uygulamaya elle girin/,
    'If local QR rendering fails, the UI must preserve and explain the manual-key recovery path.');
  await page.keyboard.press('Escape');
  invalidAuthenticatorUri = false;
  await accountTwoFactorDialog.waitFor({ state: 'hidden' });
  assert.equal(await twoFactorSecret.textContent(), '', 'Escape must immediately erase the TOTP enrollment secret from the DOM.');
  assert.equal(await twoFactorQr.getAttribute('src'), null, 'Escape must remove the generated QR image containing the TOTP URI.');

  const settingsGroup = page.locator('#sidebar [data-group="settings"]');
  const integrationsNav = page.locator('#sidebar [data-page="Entegrasyonlar"]');
  if (!await integrationsNav.isVisible()) await settingsGroup.click();
  await integrationsNav.waitFor({ state: 'visible' });
  await integrationsNav.click();
  await page.waitForFunction(() => document.querySelector('#pageTitle')?.textContent === 'Entegrasyonlar');
  const integrationsForm = page.locator('#integrationTokenForm');
  const apiTokenCreatesBeforeValidation = apiRequests.filter(item => item.path === '/api/integrations/tokens' && item.method === 'POST').length;
  await integrationsForm.locator('[name="name"]').fill('Synthetic reporting integration');
  await integrationsForm.locator('#createIntegrationToken').click();
  await page.waitForFunction(() => document.querySelector('#integrationTokenFeedback')?.textContent.includes('En az bir API yetkisi seçin'));
  assert.equal(await page.locator('#integrationAssetWriteScopeLabel').isVisible(), false,
    'A viewer must not see the asset write permission even if the form is inspected directly.');
  assert.equal(apiRequests.filter(item => item.path === '/api/integrations/tokens' && item.method === 'POST').length, apiTokenCreatesBeforeValidation,
    'The integration form must reject an empty scope selection without an API write.');
  const integrationSecretDialog = page.locator('#integrationSecretDialog');
  const secretField = page.locator('#integrationSecretValue');
  await secretField.evaluate((input, secret) => { input.value = secret; input.closest('dialog').showModal(); }, syntheticIntegrationSecret);
  await page.keyboard.press('Escape');
  await integrationSecretDialog.waitFor({ state: 'hidden' });
  assert.equal(await secretField.inputValue(), '', 'Escape must synchronously clear the one-time secret before the dialog fully closes.');
  await integrationsForm.locator('input[name="scope"][value="assets:read"]').check();
  await integrationsForm.locator('[name="expiresInDays"]').selectOption('30');
  await integrationsForm.locator('#createIntegrationToken').click();
  await integrationSecretDialog.waitFor({ state: 'visible' });
  assert.equal(await secretField.getAttribute('type'), 'password', 'A newly created API secret must be masked by default.');
  assert.equal(await secretField.inputValue(), syntheticIntegrationSecret);
  await page.locator('#toggleIntegrationSecret').click();
  assert.equal(await secretField.getAttribute('type'), 'text', 'The explicit Show action must reveal the one-time secret.');
  await page.locator('#toggleIntegrationSecret').click();
  assert.equal(await secretField.getAttribute('type'), 'password');
  const tokenCreateWrite = apiRequests.find(item => item.path === '/api/integrations/tokens' && item.method === 'POST');
  assert.equal(tokenCreateWrite?.csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(tokenCreateWrite?.body.scopes, ['assets:read']);
  assert.ok(Date.parse(tokenCreateWrite.body.expiresAt) > Date.now() + 29 * 86400000,
    'The selected 30-day lifetime must reach the API as an expiration date.');
  await page.locator('#dismissIntegrationSecret').click();
  await integrationSecretDialog.waitFor({ state: 'hidden' });
  assert.equal(await secretField.inputValue(), '', 'Closing the one-time secret dialog must erase its value from the DOM.');
  assert.equal(await integrationSecretDialog.isVisible(), false, 'A secret is not available again after its one-time dialog closes.');
  await page.locator('#integrationTokenRows').getByText('Synthetic reporting integration').waitFor();
  await page.locator('#integrationsRefresh').click();
  await page.locator('#integrationTokenRows').getByText('Synthetic reporting integration').waitFor();
  const integrationTokenRow = page.locator('#integrationTokenRows tr').filter({ hasText: 'Synthetic reporting integration' });
  const integrationRevokesBeforeCancel = apiRequests.filter(item => item.path.endsWith('/revoke') && item.method === 'POST').length;
  await integrationTokenRow.getByRole('button', { name: 'İptal et' }).click();
  const integrationRevokeDialog = page.locator('#componentActionDialog');
  await integrationRevokeDialog.waitFor({ state: 'visible' });
  await integrationRevokeDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await integrationRevokeDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path.endsWith('/revoke') && item.method === 'POST').length, integrationRevokesBeforeCancel,
    'Canceling API key revocation must not revoke the key.');
  await integrationTokenRow.getByRole('button', { name: 'İptal et' }).click();
  await integrationRevokeDialog.waitFor({ state: 'visible' });
  await page.locator('#componentActionSubmit').click();
  await integrationRevokeDialog.waitFor({ state: 'hidden' });
  await integrationTokenRow.getByText('İptal edildi').waitFor();
  const tokenRevokeWrite = apiRequests.find(item => item.path.startsWith('/api/integrations/tokens/') && item.path.endsWith('/revoke') && item.method === 'POST');
  assert.equal(tokenRevokeWrite?.csrf, 'synthetic-viewer-csrf');
  assert.equal(integrationTokens[0].revokedAt === null, false, 'Confirmed revocation must persist in the synthetic API source.');
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.locator('#membersNav').waitFor({ state: 'hidden' });
  if (!await integrationsNav.isVisible()) await settingsGroup.click();
  await integrationsNav.waitFor({ state: 'visible' });
  await integrationsNav.click();
  await page.locator('#integrationTokenRows').getByText('Synthetic reporting integration').waitFor();
  await page.locator('#integrationTokenRows').getByText('İptal edildi').waitFor();
  console.log('PASS: integration API key menu validates scopes, posts CSRF, masks and clears the one-time secret, preserves cancellation, confirms/reloads revocation, and persists the synthetic state.');

  companyRole = 'admin';
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.locator('#sidebar [data-page="Genel Bakış"]').waitFor({ state: 'visible' });
  const writesBeforeSecurityNavigation = apiRequests.filter(item => item.method !== 'GET').length;
  const securityNav = page.locator('#sidebar [data-page="Güvenlik"]');
  if (!await securityNav.isVisible()) await settingsGroup.click();
  await securityNav.click();
  await page.locator('#companySecurityPolicyPanel').waitFor({ state: 'visible' });
  await page.locator('#securityOverviewPanel').waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#securityOverviewMfa')?.textContent === 'Kapalı'
    && document.querySelector('#securityOverviewSessions')?.textContent === '0');
  assert.equal(await accountDialog.isVisible(), false,
    'Navigating to Security must show its overview without automatically opening account settings.');
  await page.locator('#featureStatusAccountSecurityButton').click();
  await accountDialog.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#accountSessionsList')?.textContent.includes('Etkin oturum bulunamadı'));
  assert.equal(apiRequests.filter(item => item.method !== 'GET').length, writesBeforeSecurityNavigation,
    'The security overview and deliberate account security action must not change security settings.');
  await page.locator('#closeAccountPhoto').click();
  await accountDialog.waitFor({ state: 'hidden' });
  assert.deepEqual(companySecurityPolicy, { passwordMinimumLength: 12, sessionDurationHours: 720, requireMfa: false });
  assert.equal(await page.locator('#companySecurityPolicyForm [name="passwordMinimumLength"]').inputValue(), '12');
  await page.locator('#companySecurityPolicyForm [name="passwordMinimumLength"]').fill('16');
  await page.locator('#companySecurityPolicyForm [name="sessionDurationHours"]').fill('48');
  await page.locator('#companySecurityPolicyForm [name="requireMfa"]').check();
  await page.locator('#companySecurityPolicySave').click();
  await page.waitForTimeout(100);
  assert.equal(await page.locator('#companySecurityPolicyFeedback').innerText(), 'Güvenlik politikası kaydedildi. Zorunlu MFA açıksa MFA kurmayan üyeler bir sonraki girişte kurulum yapar.',
    `Company policy save feedback; requests: ${JSON.stringify(apiRequests.filter(item => item.path === '/api/company-security'))}`);
  const companySecurityWrite = apiRequests.find(item => item.path === '/api/company-security' && item.method === 'PUT');
  assert.deepEqual(companySecurityWrite?.body, { passwordMinimumLength: 16, sessionDurationHours: 48, requireMfa: true });
  assert.equal(companySecurityWrite?.csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(companySecurityPolicy, companySecurityWrite.body, 'The company policy must persist in the synthetic API source.');
  const appearanceDialog = page.locator('#appearanceDialog');
  await page.locator('#appearanceButton').click();
  await appearanceDialog.waitFor({ state: 'visible' });
  const beforeAppearanceCancel = apiRequests.filter(item => item.method !== 'GET').length;
  await page.locator('#appearanceCancel').click();
  await appearanceDialog.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.method !== 'GET').length, beforeAppearanceCancel, 'Canceling appearance settings must not write preferences.');
  await page.locator('#appearanceButton').click();
  await appearanceDialog.waitFor({ state: 'visible' });
  await page.locator('#themePreference').selectOption('dark');
  await page.locator('#languagePreference').selectOption('en');
  await page.locator('#appearanceSave').click();
  await appearanceDialog.waitFor({ state: 'hidden' });
  await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
  await page.waitForFunction(() => document.querySelector('#featureStatusAccountSecurityButton')?.textContent === 'Open your account security'
    && document.querySelector('#securityOverviewMfa')?.textContent === 'Off'
    && document.querySelector('#securityOverviewSessionsNote')?.textContent === 'Active sessions for this account.');
  const appearanceWrite = apiRequests.find(item => item.path === '/api/appearance/preferences' && item.method === 'PUT');
  assert.deepEqual(appearanceWrite?.body, { theme: 'dark', language: 'en' });
  assert.equal(appearanceWrite?.csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(JSON.parse(await page.evaluate(() => localStorage.getItem('envantertr.appearance'))), { theme: 'dark', language: 'en' });
  assert.deepEqual(pageErrors, [], 'The command palette and route navigation must not throw browser errors.');

  subscriptionMode = 'readOnly';
  const expiryTranslations = {
    tr: ['Abonelik süresi doldu', 'Şirket kayıtlarınız korunuyor.', 'Authenticator uygulamasıyla taranacak kurulum QR kodu'],
    en: ['Subscription expired', 'Your company records are safe.', 'Setup QR code to scan with your authenticator app'],
    ru: ['Срок подписки истёк', 'Данные вашей компании сохранены.', 'QR-код настройки для сканирования приложением-аутентификатором'],
    uk: ['Термін підписки минув', 'Дані вашої компанії збережено.', 'QR-код налаштування для сканування застосунком-автентифікатором'],
    mk: ['Претплатата е истечена', 'Податоците на вашата компанија се зачувани.', 'QR-код за поставување за скенирање со апликацијата за автентикација']
  };
  for (const [language, [title, description, qrAlt]] of Object.entries(expiryTranslations)) {
    await page.evaluate(languageCode => localStorage.setItem('envantertr.appearance', JSON.stringify({ language: languageCode })), language);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await page.waitForFunction(expectedTitle => {
      const notice = document.querySelector('#subscriptionExpiryNotice');
      return notice?.hidden === false && notice.textContent.includes(expectedTitle);
    }, title);
    const noticeText = await page.locator('#subscriptionExpiryNotice').innerText();
    assert.ok(noticeText.includes(title) && noticeText.includes(description),
      `The expired-subscription notice must be translated into ${language}.`);
    assert.equal(await page.locator('#twoFactorQr').getAttribute('alt'), qrAlt,
      `The MFA QR image alternative text must be translated into ${language}.`);
  }
  assert.deepEqual(pageErrors, [], 'The app must not throw browser errors while switching languages and showing subscription access state.');
  const writes = apiRequests.filter(item => item.method !== 'GET');
  assert.equal(writes.length, 8, 'Only explicit synthetic notification, company security policy, API key create/revoke, remote session revoke, TOTP setup, and appearance actions may write.');
  assert.deepEqual(writes.map(item => item.path).sort(), [
    '/api/appearance/preferences', '/api/company-security', '/api/integrations/tokens', tokenRevokeWrite.path,
    '/api/notifications/preferences', '/api/session/2fa/setup', '/api/session/2fa/setup',
    `/api/session/devices/${remoteSessionId}/revoke`
  ].sort());
  assert.ok(writes.filter(item => item.path === '/api/session/2fa/setup').every(item => item.csrf === 'synthetic-viewer-csrf'),
    'Each explicit TOTP enrollment request must include CSRF protection.');
  assert.deepEqual(writes.find(item => item.path === '/api/appearance/preferences')?.body, { theme: 'dark', language: 'en' });

  loginMode = true;
  const loginPage = await browser.newPage({ viewport: { width: 1280, height: 900 } });
  loginPage.setDefaultTimeout(10_000);
  const loginPageErrors = [];
  loginPage.on('pageerror', error => loginPageErrors.push(error.message));
  await loginPage.goto(`http://127.0.0.1:${address.port}/?email=qa-url-only%40example.invalid&keep=1#login`, { waitUntil: 'domcontentloaded' });
  await loginPage.locator('#loginQueryNotice').waitFor({ state: 'visible' });
  assert.equal(new URL(loginPage.url()).search, '?keep=1', 'An email-only login URL must be scrubbed before app initialization while unrelated query state remains.');
  assert.equal(new URL(loginPage.url()).hash, '#login', 'Scrubbing login data must preserve the URL fragment.');
  assert.equal(await loginPage.locator('#loginForm [name="email"]').inputValue(), '', 'The email-only query value must not be copied into the login form.');
  await loginPage.locator('#loginConnectionStatus').getByText(/Sunucu ve veritabanı hazır/).waitFor({ state: 'visible' });
  assert.equal(await loginPage.locator('#loginForm button[type="submit"]').isDisabled(), false,
    'A ready database must leave the sign-in action available.');
  await loginPage.locator('#loginForm [name="email"]').fill('qa-login@example.invalid');
  await loginPage.locator('#loginForm [name="password"]').fill('Synthetic-only-password');
  assert.equal(await loginPage.locator('#loginForm [name="rememberMe"]').isChecked(), false,
    'A new login must not opt into a long-lived session by default.');
  await loginPage.locator('#loginForm button[type="submit"]').click();
  await loginPage.locator('#authError').getByText('Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor.').waitFor({ state: 'visible' });
  await loginPage.locator('#loginConnectionStatus').getByText(/bu giriş hatası bağlantıdan kaynaklanmıyor/i).waitFor({ state: 'visible' });
  assert.equal(await loginPage.locator('#loginForm button[type="submit"]').isDisabled(), false,
    'A rejected synthetic login must allow a deliberate retry.');
  assert.equal(loginAttempts.length, 1, 'A rejected login must not recurse or be silently retried.');
  assert.equal(loginAttempts[0].method, 'POST');
  assert.equal(loginAttempts[0].csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(loginAttempts[0].body, { email: 'qa-login@example.invalid', password: 'Synthetic-only-password', companyId: null, rememberMe: false, twoFactorCode: null });
  assert.equal(new URL(loginPage.url()).search, '?keep=1', 'Login details must not be sent in the URL, while unrelated query state stays intact.');
  await loginPage.locator('#retryApiButton').click();
  await loginPage.locator('#loginConnectionStatus').getByText(/bu giriş hatası bağlantıdan kaynaklanmıyor/i).waitFor({ state: 'visible' });
  assert.equal(loginAttempts.length, 1, 'Retrying the server check must not resubmit login credentials.');
  loginPendingMigrationCount = 21;
  await loginPage.locator('#retryApiButton').click();
  await loginPage.locator('#loginConnectionStatus').getByText(/21 veritabanı güncellemesi bekliyor/).waitFor({ state: 'visible' });
  assert.equal(await loginPage.locator('#authError').innerText(), '', 'A migration block must clear the stale wrong-credentials message.');
  assert.equal(await loginPage.locator('#loginForm button[type="submit"]').isDisabled(), true,
    'The sign-in action must stay disabled until the pending production schema is updated.');
  assert.equal(await loginPage.locator('#forgotPasswordButton').isDisabled(), true,
    'Password recovery must not be offered as a credential fix while the schema is pending.');
  assert.equal(loginAttempts.length, 1, 'A migration diagnosis must not resubmit login credentials.');
  loginPendingMigrationCount = 0;
  await loginPage.locator('#retryApiButton').click();
  await loginPage.locator('#loginConnectionStatus').getByText(/bu giriş hatası bağlantıdan kaynaklanmıyor/i).waitFor({ state: 'visible' });
  assert.equal(await loginPage.locator('#loginForm button[type="submit"]').isDisabled(), false,
    'A healthy schema must restore the sign-in action.');
  assert.equal(loginAttempts.length, 1, 'Recovering readiness must not silently retry credentials.');
  await loginPage.locator('#loginForm [name="rememberMe"]').check();
  companyMfaEnrollmentMode = true;
  await loginPage.locator('#loginForm button[type="submit"]').click();
  const companyMfaDialog = loginPage.locator('#companyMfaEnrollmentDialog');
  await companyMfaDialog.waitFor({ state: 'visible' });
  assert.equal(loginAttempts[1].body.rememberMe, true,
    'The explicit device-session choice must be sent with the MFA enrollment challenge.');
  assert.match(await loginPage.locator('#companyMfaEnrollmentQr').getAttribute('src'), /^data:image\/svg\+xml/);
  assert.equal(await loginPage.locator('#companyMfaEnrollmentSecret').innerText(), 'JBSWY3DPEHPK3PXP');
  assert.equal(apiRequests.find(item => item.path === '/api/session/company-mfa/setup')?.body?.ticket, 'synthetic-one-time-mfa-ticket');
  await loginPage.locator('#companyMfaEnrollmentForm [name="code"]').fill('123456');
  await loginPage.locator('#verifyCompanyMfaEnrollment').click();
  await loginPage.locator('#companyMfaRecoveryPanel').waitFor({ state: 'visible' });
  assert.equal((await loginPage.locator('#companyMfaRecoveryCodes').innerText()).split('\n').length, 10);
  assert.equal(await loginPage.locator('#closeCompanyMfaEnrollment').isDisabled(), true,
    'Recovery codes must remain visible until the user explicitly acknowledges saving them.');
  assert.deepEqual(apiRequests.find(item => item.path === '/api/session/company-mfa/verify')?.body,
    { ticket: 'synthetic-one-time-mfa-ticket', code: '123456' });
  assert.equal(apiRequests.find(item => item.path === '/api/session/company-mfa/verify')?.csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(loginPageErrors, [], `The signed-out login UI must not throw browser errors: ${loginPageErrors.join(' | ')}`);
  await loginPage.close();
  console.log('PASS: synthetic viewer/admin visibility; keyboard/sidebar navigation; company security policy load/save with CSRF and persistence; notification cancel/save persistence; profile security and appearance preferences; five-language expiry notice; signed-out login distinguishes credential rejection from schema lockout, clears stale errors, blocks login/recovery until ready, tests first-time company MFA QR/secret/verification/recovery codes, and never silently retries credentials.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
