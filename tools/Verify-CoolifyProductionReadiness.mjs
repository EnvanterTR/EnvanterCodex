import assert from 'node:assert/strict';
import { checkProduction, inspectEndpoint } from './Check-CoolifyProduction.mjs';

function response(status, contentType, body) {
  return { status, ok: status >= 200 && status < 300, headers: { get: name => name === 'content-type' ? contentType : null }, text: async () => body };
}

const liveLikeFetch = async url => url.startsWith('https://apex.invalid')
  ? response(200, 'application/json', JSON.stringify({ status: 'migration-required', migrationCount: 91, pendingMigrationCount: 21 }))
  : response(503, 'text/plain', 'no available server');

const report = await checkProduction(['https://apex.invalid/api/status', 'https://tenant.invalid/api/status'], liveLikeFetch);
assert.equal(report.ready, false, 'Any unhealthy apex or tenant host must fail the production readiness check.');
assert.equal(report.results[0].appStatus, 'migration-required');
assert.equal(report.results[0].pendingMigrationCount, 21);
assert.match(report.results[0].diagnosis, /Back up and verify/i);
assert.equal(report.results[1].httpStatus, 503);
assert.match(report.results[1].diagnosis, /no healthy upstream/i);

const ready = await inspectEndpoint('https://ready.invalid/api/status', async () => response(200, 'application/json', JSON.stringify({ status: 'ready', migrationCount: 91, pendingMigrationCount: 0 })));
assert.equal(ready.ready, true);

const malformed = await inspectEndpoint('https://malformed.invalid/api/status', async () => response(200, 'application/json', '{invalid'));
assert.equal(malformed.ready, false);
assert.match(malformed.diagnosis, /expected JSON health payload/i);

const failed = await inspectEndpoint('https://failed.invalid/api/status', async () => { throw new Error('contains no credential'); });
assert.equal(failed.ready, false);
assert.match(failed.diagnosis, /DNS, TLS/i);
assert.doesNotMatch(JSON.stringify(failed), /contains no credential/);

let requestedUrl = null;
const credentialBearingUrl = 'https://operator:password@tenant.invalid/api/status?password=secret&token=private#fragment';
const sanitized = await inspectEndpoint(credentialBearingUrl, async url => {
  requestedUrl = url;
  return response(200, 'application/json', JSON.stringify({ status: 'ready', migrationCount: 91, pendingMigrationCount: 0 }));
});
assert.equal(requestedUrl, 'https://tenant.invalid/api/status', 'Credentials and query data must never be sent to the readiness endpoint.');
assert.equal(sanitized.url, 'https://tenant.invalid/api/status');
assert.doesNotMatch(JSON.stringify(sanitized), /operator|password|secret|private|fragment/i);

let insecureRequestMade = false;
const insecure = await inspectEndpoint('http://operator:password@tenant.invalid/api/status?token=private', async () => {
  insecureRequestMade = true;
  return response(200, 'application/json', '{}');
});
assert.equal(insecureRequestMade, false, 'The production readiness checker must not make plain HTTP requests.');
assert.equal(insecure.ready, false);
assert.doesNotMatch(JSON.stringify(insecure), /operator|password|private/i);

console.log('Verified healthy readiness, pending migrations, Coolify wildcard 503, malformed health payloads, credential-safe diagnostics, and HTTPS-only requests.');
