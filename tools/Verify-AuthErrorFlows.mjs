import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const apiStart = source.indexOf('async function api(path, options = {})');
const apiEnd = source.indexOf('async function refreshCsrf()', apiStart);
const loginStart = source.indexOf('function showLogin(message = \'\')');
const loginEnd = source.indexOf('\n', loginStart);
assert.ok(apiStart >= 0 && apiEnd > apiStart, 'Could not find the application API helper.');
assert.ok(loginStart >= 0 && loginEnd > loginStart, 'Could not find the login-layer helper.');

const dialogs = [{ closed: 0, close() { this.closed++; } }, { closed: 0, close() { this.closed++; } }];
const elements = new Map([
  ['#authLayer', { classList: { added: [], add(value) { this.added.push(value); } } }],
  ['#retryApiButton', { hidden: true }],
  ['#authError', { textContent: '' }],
  ['#loginQueryNotice', { hidden: true, textContent: '' }],
]);
let response;
let loginLayerShown = 0;
const context = {
  AbortController,
  Headers,
  FormData,
  clearTimeout,
  document: {
    body: { classList: { add() { loginLayerShown++; } } },
    querySelectorAll(selector) { return selector === 'dialog[open]' ? dialogs : []; },
  },
  fetch: async () => response,
  requestToken: '',
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
await assert.rejects(context.api('/api/assets'), error => {
  assert.equal(error.code, 'AUTH_REQUIRED');
  assert.equal(error.status, 401);
  return true;
});
assert.equal(loginLayerShown, 1, 'An authenticated API 401 must show the login layer.');
assert.deepEqual(dialogs.map(dialog => dialog.closed), [1, 1], 'An authenticated API 401 must close open dialogs.');
assert.equal(elements.get('#retryApiButton').hidden, false, 'The server retry action must remain available.');

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

response = { status: 401, ok: false, headers: { get: () => null } };
await assert.rejects(context.api('/api/session/login', { method: 'POST', body: '{}' }), error => {
  assert.equal(error.status, 401);
  return true;
});
assert.equal(loginLayerShown, 1, 'A rejected login attempt must keep the current login form in control.');

console.log('Verified authenticated 401 closes open dialogs and shows login; 403 stays a permission error; rejected login does not recurse into the login layer.');
