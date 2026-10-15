import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const syntheticCompanyId = '11111111-1111-4111-8111-111111111111';
const syntheticUserId = '22222222-2222-4222-8222-222222222222';
const appHtml = await readFile('/checkout-site/company-app.html');
const apiRequests = [];
const loginAttempts = [];
let subscriptionMode = 'full';
let loginMode = false;
let notificationPreferences = { enabledKinds: ['maintenance', 'audit', 'license', 'warranty', 'return', 'reservation'], emailEnabledKinds: [] };
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
    response.end('window.qrcodegen = window.qrcodegen || {};');
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
        response.writeHead(401, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
        response.end(JSON.stringify({ message: 'Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor.' }));
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
      ? { status: 'ready', migrationCount: 91, pendingMigrationCount: 0 }
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
              role: 'viewer',
              hasPhoto: false,
              subscriptionAccess: { mode: subscriptionMode, status: subscriptionMode === 'readOnly' ? 'Expired' : 'Active', planName: 'Synthetic cloud plan' }
            }
            : url.pathname === '/api/company/branding'
              ? { name: 'Synthetic Viewer Company', brandPrimaryColor: '#2563EB', hasLogo: false }
            : url.pathname === '/api/company/label-settings'
                ? { isConfigured: false, settings: null }
              : url.pathname === '/api/notifications/preferences'
                ? notificationPreferences
                : url.pathname === '/api/session/2fa'
                  ? { enabled: false }
                  : url.pathname === '/api/session/devices'
                    ? { sessions: [], truncated: false }
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
  await page.keyboard.press('Control+n');
  await page.waitForFunction(() => document.querySelector('#toast')?.textContent.includes('oluşturma yetkiniz bulunmuyor'));
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
  const notificationShortcut = page.locator('#featureStatusNotificationButton');
  assert.equal(await notificationShortcut.isVisible(), true, 'The Notifications menu must expose its working preferences shortcut.');
  await notificationShortcut.click();
  await notificationPanel.waitFor({ state: 'visible' });
  const preferencesForm = page.locator('#notificationPreferencesForm');
  await preferencesForm.waitFor({ state: 'visible' });
  const auditPreference = preferencesForm.locator('input[name="kind"][value="audit"]');
  const reservationPreference = preferencesForm.locator('input[name="kind"][value="reservation"]');
  const warrantyEmailPreference = preferencesForm.locator('input[name="emailKind"][value="warranty"]');
  await page.waitForFunction(() => document.querySelector('#notificationPreferencesForm input[name="kind"][value="reservation"]')?.checked === true);
  assert.equal(await auditPreference.isChecked(), true, 'The form must load the current in-app notification preference.');
  const preferenceWritesBeforeCancel = apiRequests.filter(item => item.path === '/api/notifications/preferences' && item.method === 'PUT').length;
  await reservationPreference.uncheck();
  await preferencesForm.getByRole('button', { name: 'Vazgeç' }).click();
  await preferencesForm.waitFor({ state: 'hidden' });
  assert.equal(apiRequests.filter(item => item.path === '/api/notifications/preferences' && item.method === 'PUT').length, preferenceWritesBeforeCancel,
    'Canceling notification edits must not write preferences.');
  await page.locator('#notificationSettingsToggle').click();
  await preferencesForm.waitFor({ state: 'visible' });
  await page.waitForFunction(() => document.querySelector('#notificationPreferencesForm input[name="kind"][value="reservation"]')?.checked === true);
  assert.equal(await reservationPreference.isChecked(), true, 'Cancel must restore the persisted choices when the form is reopened.');
  await auditPreference.uncheck();
  await warrantyEmailPreference.check();
  await preferencesForm.getByRole('button', { name: 'Tercihleri kaydet' }).click();
  await preferencesForm.waitFor({ state: 'hidden' });
  const notificationWrite = apiRequests.find(item => item.path === '/api/notifications/preferences' && item.method === 'PUT');
  assert.deepEqual(notificationWrite?.body, {
    enabledKinds: ['maintenance', 'license', 'warranty', 'return', 'reservation'],
    emailEnabledKinds: ['warranty']
  });
  assert.equal(notificationWrite?.csrf, 'synthetic-viewer-csrf');
  await page.locator('#notificationSettingsToggle').click();
  await preferencesForm.waitFor({ state: 'visible' });
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
  await page.locator('#closeAccountPhoto').click();
  await accountDialog.waitFor({ state: 'hidden' });

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
  const appearanceWrite = apiRequests.find(item => item.path === '/api/appearance/preferences' && item.method === 'PUT');
  assert.deepEqual(appearanceWrite?.body, { theme: 'dark', language: 'en' });
  assert.equal(appearanceWrite?.csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(JSON.parse(await page.evaluate(() => localStorage.getItem('envantertr.appearance'))), { theme: 'dark', language: 'en' });
  assert.deepEqual(pageErrors, [], 'The command palette and route navigation must not throw browser errors.');

  subscriptionMode = 'readOnly';
  const expiryTranslations = {
    tr: ['Abonelik süresi doldu', 'Şirket kayıtlarınız korunuyor.'],
    en: ['Subscription expired', 'Your company records are safe.'],
    ru: ['Срок подписки истёк', 'Данные вашей компании сохранены.'],
    uk: ['Термін підписки минув', 'Дані вашої компанії збережено.'],
    mk: ['Претплатата е истечена', 'Податоците на вашата компанија се зачувани.']
  };
  for (const [language, [title, description]] of Object.entries(expiryTranslations)) {
    await page.evaluate(languageCode => localStorage.setItem('envantertr.appearance', JSON.stringify({ language: languageCode })), language);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await page.waitForFunction(expectedTitle => {
      const notice = document.querySelector('#subscriptionExpiryNotice');
      return notice?.hidden === false && notice.textContent.includes(expectedTitle);
    }, title);
    const noticeText = await page.locator('#subscriptionExpiryNotice').innerText();
    assert.ok(noticeText.includes(title) && noticeText.includes(description),
      `The expired-subscription notice must be translated into ${language}.`);
  }
  assert.deepEqual(pageErrors, [], 'The app must not throw browser errors while switching languages and showing subscription access state.');
  const writes = apiRequests.filter(item => item.method !== 'GET');
  assert.equal(writes.length, 2, 'Only the explicitly saved synthetic user notification and appearance preferences may write.');
  assert.deepEqual(writes.map(item => item.path).sort(), ['/api/appearance/preferences', '/api/notifications/preferences']);
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
  await loginPage.locator('#loginForm button[type="submit"]').click();
  await loginPage.locator('#authError').getByText('Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor.').waitFor({ state: 'visible' });
  await loginPage.locator('#loginConnectionStatus').getByText(/bu giriş hatası bağlantıdan kaynaklanmıyor/i).waitFor({ state: 'visible' });
  assert.equal(await loginPage.locator('#loginForm button[type="submit"]').isDisabled(), false,
    'A rejected synthetic login must allow a deliberate retry.');
  assert.equal(loginAttempts.length, 1, 'A rejected login must not recurse or be silently retried.');
  assert.equal(loginAttempts[0].method, 'POST');
  assert.equal(loginAttempts[0].csrf, 'synthetic-viewer-csrf');
  assert.deepEqual(loginAttempts[0].body, { email: 'qa-login@example.invalid', password: 'Synthetic-only-password', companyId: null, rememberMe: true, twoFactorCode: null });
  assert.equal(new URL(loginPage.url()).search, '?keep=1', 'Login details must not be sent in the URL, while unrelated query state stays intact.');
  await loginPage.locator('#retryApiButton').click();
  await loginPage.locator('#loginConnectionStatus').getByText(/bu giriş hatası bağlantıdan kaynaklanmıyor/i).waitFor({ state: 'visible' });
  assert.equal(loginAttempts.length, 1, 'Retrying the server check must not resubmit login credentials.');
  assert.deepEqual(loginPageErrors, [], `The signed-out login UI must not throw browser errors: ${loginPageErrors.join(' | ')}`);
  await loginPage.close();
  console.log('PASS: synthetic viewer role visibility; keyboard/sidebar navigation; notification cancel/save persistence; profile security and appearance preferences; five-language expiry notice; signed-out login shows credential rejection beside ready-server diagnosis, posts once with CSRF, keeps credentials out of the URL, and retries health without resubmitting.');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
