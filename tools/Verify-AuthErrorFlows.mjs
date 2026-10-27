import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const accountApi = readFileSync('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const program = readFileSync('src/EnvanterTR.Web/Program.cs', 'utf8');
const securityTests = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const apiStart = source.indexOf('async function api(path, options = {})');
const apiEnd = source.indexOf('async function refreshCsrf()', apiStart);
const loginStart = source.indexOf('function showLogin(message = \'\')');
const loginEnd = source.indexOf('\n', loginStart);
assert.ok(apiStart >= 0 && apiEnd > apiStart, 'Could not find the application API helper.');
assert.ok(loginStart >= 0 && loginEnd > loginStart, 'Could not find the login-layer helper.');
const loginEndpointStart = accountApi.indexOf('endpoints.MapPost("/api/session/login"');
const credentialLookup = accountApi.indexOf('userManager.FindByEmailAsync(request.Email.Trim())', loginEndpointStart);
assert.ok(loginEndpointStart >= 0 && credentialLookup > loginEndpointStart, 'Could not find the server-side login endpoint.');
assert.match(accountApi.slice(loginEndpointStart, credentialLookup), /GetPendingMigrationsAsync\(cancellationToken\)[\s\S]*?schema_migration_required[\s\S]*?ServiceUnavailable/,
  'Login must report the pending schema update before querying or incrementing credentials.');
assert.match(source, /if \(readiness\.status === 'migration-required'\) \{\s*\$\('#authError'\)\.textContent = '';/,
  'A pending migration diagnosis must clear any stale bad-credentials message.');
assert.match(program, /options\.Lockout\.MaxFailedAccessAttempts\s*=\s*5\s*;/,
  'Identity lockout must activate after exactly five failed attempts.');
assert.match(program, /options\.Lockout\.DefaultLockoutTimeSpan\s*=\s*TimeSpan\.FromMinutes\(5\)\s*;/,
  'Identity lockout duration must remain five minutes.');
assert.match(securityTests, /Account_lockout_matches_five_failed_attempts_and_five_minutes[\s\S]*?Assert\.Equal\(5, lockout\.MaxFailedAccessAttempts\)[\s\S]*?Assert\.Equal\(TimeSpan\.FromMinutes\(5\), lockout\.DefaultLockoutTimeSpan\)/,
  'The security acceptance suite must exercise both configured lockout thresholds.');

const dialogs = [{ closed: 0, close() { this.closed++; } }, { closed: 0, close() { this.closed++; } }];
const elements = new Map([
  ['#authLayer', { classList: { added: [], add(value) { this.added.push(value); } } }],
  ['#retryApiButton', { hidden: true }],
  ['#authError', { textContent: '' }],
  ['#impersonationNotice', { hidden: false }],
  ['#loginQueryNotice', { hidden: true, textContent: '' }],
]);
let response;
let loginLayerShown = 0;
let connectionChecks = 0;
const requests = [];
const context = {
  AbortController,
  Headers,
  FormData,
  clearTimeout,
  document: {
    body: { classList: { add() { loginLayerShown++; } } },
    querySelectorAll(selector) { return selector === 'dialog[open]' ? dialogs : []; },
  },
  fetch: async (path, options) => { requests.push({ path, options }); return response; },
  requestToken: '',
  authSessionGeneration: 0,
  impersonationSession: { actorName: 'Synthetic administrator' },
  checkLoginConnection: async () => { connectionChecks++; },
  setTimeout,
  translateUiText: value => value,
  window: { __envanterCredentialsInUrl: false },
  $: selector => {
    assert.ok(elements.has(selector), `Unexpected login-layer selector: ${selector}`);
    return elements.get(selector);
  },
};
vm.runInNewContext(`${source.slice(loginStart, loginEnd)}\n${source.slice(apiStart, apiEnd)}`, context);

response = { status: 401, ok: false, headers: { get: () => null } };
await assert.rejects(context.api('/api/assets/asset-id/audit', { method: 'POST', body: JSON.stringify({ auditDate: '2026-10-12' }) }), error => {
  assert.equal(error.code, 'AUTH_REQUIRED');
  assert.equal(error.status, 401);
  return true;
});
assert.equal(loginLayerShown, 1, 'An authenticated API 401 must show the login layer.');
assert.equal(context.impersonationSession, null, 'An expired session must clear support identity state.');
assert.equal(elements.get('#impersonationNotice').hidden, true, 'An expired session must hide the support banner.');
assert.equal(connectionChecks, 1, 'Showing the login screen must automatically check API readiness.');
assert.deepEqual(dialogs.map(dialog => dialog.closed), [1, 1], 'An authenticated API 401 must close open dialogs.');
assert.equal(elements.get('#retryApiButton').hidden, false, 'The server retry action must remain available.');
assert.equal(requests.length, 1, 'A failed write must not be silently retried after its session expires.');
assert.equal(requests[0].path, '/api/assets/asset-id/audit');
assert.equal(requests[0].options.method, 'POST');

response = {
  status: 403,
  ok: false,
  headers: { get: name => name === 'content-type' ? 'application/json' : null },
  json: async () => ({ message: 'Bu işlem için yetkiniz yok.' }),
};
await assert.rejects(context.api('/api/assets'), error => {
  assert.equal(error.status, 403);
  assert.equal(error.message, 'Bu işlem için yetkiniz yok.');
  return true;
});
assert.equal(loginLayerShown, 1, 'A 403 permission failure must not show the login layer.');
assert.deepEqual(dialogs.map(dialog => dialog.closed), [1, 1], 'A 403 permission failure must not close unrelated dialogs.');
assert.equal(requests.length, 2, 'A permission failure must not cause a second write attempt.');

response = { status: 401, ok: false, headers: { get: () => null } };
await assert.rejects(context.api('/api/session/login', { method: 'POST', body: '{}' }), error => {
  assert.equal(error.status, 401);
  return true;
});
assert.equal(loginLayerShown, 1, 'A rejected login attempt must keep the current login form in control.');
assert.deepEqual(requests.map(request => request.path), ['/api/assets/asset-id/audit', '/api/assets', '/api/session/login'], 'The client must not replay the expired-session write while handling a rejected login.');

const credentialScrubScript = source.match(/<script>([\s\S]*?)<\/script>/)?.[1] || '';
assert.match(credentialScrubScript, /authParams=\['email','password','twoFactorCode','companyId'\]/, 'The early page script must detect every auth/account-scope parameter in the URL.');
const historyCalls = [];
const credentialUrlContext = {
  URL,
  location: { href: 'http://localhost:8080/?email=private%40example.invalid&password=not-a-real-secret&twoFactorCode=123456&companyId=secret-company&keep=1#dashboard' },
  history: { state: null, replaceState(...args) { historyCalls.push(args); } },
  window: {},
};
vm.runInNewContext(credentialScrubScript, credentialUrlContext);
assert.equal(credentialUrlContext.window.__envanterCredentialsInUrl, true, 'The login screen must disclose that URL credentials were discarded.');
assert.equal(historyCalls.length, 1, 'Credential URLs must be scrubbed from browser history immediately.');
assert.equal(historyCalls[0][2], '/?keep=1#dashboard', 'URL cleanup must preserve unrelated query parameters and the current fragment.');
assert.doesNotMatch(historyCalls[0][2], /private|not-a-real-secret|123456|secret-company/, 'Email, password, 2FA code, and company ID must not remain in browser history.');
const emailOnlyHistoryCalls = [];
const emailOnlyUrlContext = {
  URL,
  location: { href: 'http://localhost:8080/?email=private%40example.invalid&keep=1#dashboard' },
  history: { state: null, replaceState(...args) { emailOnlyHistoryCalls.push(args); } },
  window: {},
};
vm.runInNewContext(credentialScrubScript, emailOnlyUrlContext);
assert.equal(emailOnlyUrlContext.window.__envanterCredentialsInUrl, true, 'An email-only login URL must also be identified as sensitive.');
assert.equal(emailOnlyHistoryCalls.length, 1, 'An email-only login URL must be scrubbed immediately.');
assert.equal(emailOnlyHistoryCalls[0][2], '/?keep=1#dashboard', 'Email-only cleanup must preserve unrelated URL state.');

const diagnosticStart = source.indexOf('function showFirstRunSetupNotice(setup)');
const diagnosticEnd = source.indexOf("$('#loginForm').addEventListener('submit'", diagnosticStart);
assert.ok(diagnosticStart >= 0 && diagnosticEnd > diagnosticStart, 'Could not find the login connection diagnostic.');
assert.match(source, /<button class="btn small" id="retryApiButton" type="button"[^>]*>Sunucu bağlantısını test et<\/button>/,
  'The login screen must keep a visible server-test action.');
assert.match(source, /\$\('#retryApiButton'\)\.addEventListener\('click', \(\) => \{ void checkLoginConnection\(\); \}\);/,
  'The server-test action must run the live readiness and setup diagnostic when clicked.');
const successfulLoginStart = source.indexOf("hideLogin(); await startApp();", loginStart);
assert.ok(successfulLoginStart >= 0, 'Could not find the successful login transition.');
assert.match(source.slice(successfulLoginStart, successfulLoginStart + 420), /document\.querySelector\('#sidebar \[data-page="Genel Bakış"\]'\)\?\.click\(\)/,
  'Successful login must use the normal overview navigation handler to clear a stale menu/page overlay.');
const diagnosticElements = new Map([
  ['#retryApiButton', { hidden: false, disabled: false, textContent: 'Sunucu bağlantısını test et' }],
  ['#loginConnectionStatus', { hidden: true, textContent: '' }],
  ['#authError', { textContent: 'Önceki parola reddi' }],
  ['#firstRunSetupNotice', { hidden: true }],
  ['#firstRunSetupMessage', { textContent: '' }],
  ['#loginForm button[type="submit"]', { disabled: false }],
  ['#forgotPasswordButton', { disabled: false }],
  ['#platformLoginLink', { hidden: true, href: '/platform-admin.html' }],
]);
let diagnosticMode = 'ready';
const diagnosticContext = {
  URL,
  window: { location: { origin: 'https://north.example.invalid' } },
  $: selector => {
    assert.ok(diagnosticElements.has(selector), `Unexpected connection diagnostic selector: ${selector}`);
    return diagnosticElements.get(selector);
  },
  api: async path => {
    if (diagnosticMode === 'unavailable') throw new Error('API bağlantısı kurulamadı.');
    if (path === '/api/status' && diagnosticMode === 'migration') return { status: 'migration-required', pendingMigrationCount: 19 };
    if (path === '/api/status') return { status: 'ready' };
    if (path === '/api/setup/status') return { setupRequired: diagnosticMode === 'setup', setupTokenConfigured: true,
      platformLoginUrl: diagnosticMode === 'unsafe-platform-link' ? 'https://user:secret@example.invalid/platform-admin.html' : 'https://example.invalid/platform-admin.html' };
    throw new Error(`Unexpected diagnostic request: ${path}`);
  },
  refreshCsrf: async () => {},
  translateUiText: value => value,
};
vm.runInNewContext(source.slice(diagnosticStart, diagnosticEnd), diagnosticContext);
await diagnosticContext.checkLoginConnection();
assert.equal(diagnosticElements.get('#loginConnectionStatus').hidden, false, 'A successful server check must reveal its result.');
assert.match(diagnosticElements.get('#loginConnectionStatus').textContent, /giriş hatası bağlantıdan kaynaklanmıyor/, 'A ready server must distinguish credential failure from connectivity failure.');
assert.equal(diagnosticElements.get('#retryApiButton').disabled, false, 'The retry control must be restored after a successful check.');
assert.equal(diagnosticElements.get('#platformLoginLink').href, 'https://example.invalid/platform-admin.html');
assert.equal(diagnosticElements.get('#platformLoginLink').hidden, false);
diagnosticMode = 'unsafe-platform-link';
await diagnosticContext.checkLoginConnection();
assert.equal(diagnosticElements.get('#platformLoginLink').hidden, true, 'Credential-bearing platform links must stay hidden.');

diagnosticMode = 'migration';
await diagnosticContext.checkLoginConnection();
assert.match(diagnosticElements.get('#loginConnectionStatus').textContent, /19 veritabanı güncellemesi bekliyor/, 'Pending migrations must be identified separately from bad credentials.');
assert.equal(diagnosticElements.get('#authError').textContent, '', 'A migration diagnosis must clear stale credential rejection.');
assert.equal(diagnosticElements.get('#loginForm button[type="submit"]').disabled, true, 'Sign-in must be disabled while production schema migrations are pending.');
assert.equal(diagnosticElements.get('#forgotPasswordButton').disabled, true, 'Password reset must not imply a credential problem when the schema is not updated.');

diagnosticMode = 'ready';
await diagnosticContext.checkLoginConnection();
assert.equal(diagnosticElements.get('#loginForm button[type="submit"]').disabled, false, 'A recovered ready server must re-enable sign-in.');
assert.equal(diagnosticElements.get('#forgotPasswordButton').disabled, false, 'A recovered ready server must re-enable password reset.');

diagnosticMode = 'setup';
await diagnosticContext.checkLoginConnection();
assert.equal(diagnosticElements.get('#firstRunSetupNotice').hidden, false, 'A first-run database must direct the user to company setup.');
assert.match(diagnosticElements.get('#firstRunSetupMessage').textContent, /tek kullanımlık kurulum anahtarını alın/, 'First-run guidance must explain the required server-provided setup token.');
assert.equal(diagnosticElements.get('#loginForm button[type="submit"]').disabled, true, 'Login must be disabled until first-run setup is complete.');

diagnosticMode = 'unavailable';
await diagnosticContext.checkLoginConnection();
assert.equal(diagnosticElements.get('#loginConnectionStatus').textContent, 'API bağlantısı kurulamadı.', 'Connection failures must be shown separately from invalid credentials.');
assert.equal(diagnosticElements.get('#retryApiButton').disabled, false, 'The retry control must be restored after a failed check.');

console.log('Verified authenticated 401 closes open dialogs and shows login; 403 stays a permission error; rejected login does not recurse; URL credentials are scrubbed; login exposes a wired server-test button; readiness distinguishes ready, first-run, pending-migration, and unavailable states.');
