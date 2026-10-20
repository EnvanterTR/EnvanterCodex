import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { setTimeout } from 'node:timers/promises';

const base = 'http://web:8080';
const expectedMigrations = Number(process.env.EXPECTED_MIGRATIONS);
assert.ok(Number.isInteger(expectedMigrations) && expectedMigrations > 0, 'A source migration count is required.');
const cookies = new Map();
async function request(path, options = {}, jar = cookies) {
  const response = await fetch(base + path, {
    ...options,
    redirect: 'manual',
    signal: AbortSignal.timeout(10_000),
    headers: {
      host: 'release.example.invalid',
      'x-forwarded-proto': 'https',
      cookie: [...jar].map(([key, value]) => `${key}=${value}`).join('; '),
      ...options.headers,
    },
  });
  for (const cookie of response.headers.getSetCookie()) {
    const [name, value] = cookie.split(';', 1)[0].split(/=(.*)/s, 2);
    if (/max-age=0(?:;|$)/i.test(cookie) || value === '') jar.delete(name);
    else jar.set(name, value);
  }
  return response;
}
async function post(path, body, headers = {}) {
  const csrf = await request('/api/session/csrf');
  assert.equal(csrf.status, 200);
  const { requestToken } = await csrf.json();
  return request(path, { method: 'POST', headers: {
    'content-type': 'application/json', 'x-csrf-token': requestToken, ...headers,
  }, body: JSON.stringify(body) });
}

let ready = false;
for (let attempt = 0; attempt < 60; attempt++) {
  try {
    const status = await request('/api/status');
    const body = await status.json();
    if (status.status === 200 && body.status === 'ready') {
      assert.equal(body.migrationCount, expectedMigrations);
      assert.equal(body.pendingMigrationCount, 0);
      ready = true;
      break;
    }
  } catch (error) {
    if (error.code === 'ERR_ASSERTION') throw error;
  }
  await setTimeout(500);
}
assert.ok(ready, 'The Release image must start with the migrated disposable database.');

for (const path of ['/vendor/qrcodegen.js', '/vendor/zxing-browser-0.2.1.min.js',
  '/platform-admin-members.js', '/scheduled-reports.js', '/report-print.js']) {
  const script = await request(path);
  assert.equal(script.status, 200, path);
  assert.match(script.headers.get('content-type'), /javascript/, path);
  assert.doesNotMatch((await script.text()).slice(0, 100), /<!doctype html>/i);
}
assert.equal((await request('/api/assets')).status, 401);
assert.equal((await request('/api/platform/overview')).status, 401);
const setup = await request('/api/setup/status');
assert.equal((await setup.json()).setupRequired, true);
const email = `release-${randomUUID()}@example.invalid`;
const password = 'Synthetic-Release-Password-67!';
const owner = await post('/api/setup/initial-owner', {
  companyName: 'Synthetic Release Company', fullName: 'Synthetic Release Owner', email, password,
}, { 'x-setup-token': 'synthetic-release-bootstrap-only' });
assert.equal(owner.status, 201);
const { companyId } = await owner.json();
const login = await post('/api/session/login', { email, password, companyId, rememberMe: false });
assert.equal(login.status, 200);
const sessionCookie = login.headers.getSetCookie().find(value => value.startsWith('EnvanterTR.Session='));
assert.ok(sessionCookie);
assert.match(sessionCookie, /; secure(?:;|$)/i);
assert.match(sessionCookie, /; httponly(?:;|$)/i);
assert.match(sessionCookie, /; samesite=strict(?:;|$)/i);
assert.doesNotMatch(sessionCookie, /; expires=/i);
const session = await request('/api/session');
assert.equal(session.status, 200);
assert.equal((await session.json()).companyId, companyId);
for (const cookie of session.headers.getSetCookie().filter(value => value.startsWith('EnvanterTR.Session=')))
  assert.doesNotMatch(cookie, /; expires=/i, 'Membership refresh must preserve the nonpersistent cookie.');
const assets = await request('/api/assets?page=1&pageSize=5');
assert.equal(assets.status, 200);
assert.ok(Array.isArray((await assets.json()).items));
assert.equal((await request('/api/platform/overview')).status, 403);
const noCsrf = await request('/api/session/logout', { method: 'POST' });
assert.equal(noCsrf.status, 400);
assert.equal((await request('/api/session')).status, 200, 'Rejected logout must not end the session.');
const logout = await post('/api/session/logout', {});
assert.equal(logout.status, 204);
assert.equal((await request('/api/assets')).status, 401);
assert.equal((await request('/api/setup/status').then(response => response.json())).setupRequired, false);
console.log(`PASS: exact Release image, ${expectedMigrations} migrations, Production read-only runtime, local JS assets, secure session-cookie flags, first owner, login, scope refresh, tenant/platform separation, CSRF rejection and logout.`);
