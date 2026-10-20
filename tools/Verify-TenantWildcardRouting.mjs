import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const compose = readFileSync('deploy/coolify/compose.yaml', 'utf8').replace(/\r\n/g, '\n');
const publicSite = compose.split('\n  public-site:\n')[1]?.split('\nnetworks:')[0];
assert.ok(publicSite, 'Public Nginx service must exist');
const entries = [...publicSite.matchAll(/^\s*- '([^']+)'\s*$/gm)].map((match) => {
  const separator = match[1].indexOf('=');
  assert.ok(separator > 0);
  return [match[1].slice(0, separator), match[1].slice(separator + 1)];
});
const labels = new Map(entries);
assert.equal(labels.size, entries.length, 'Duplicate labels must not override routing');
const rule = 'HostRegexp(`^[a-z0-9-]+[.]envantertr[.]com\\z`)';
for (const protocol of ['http', 'https']) {
  const prefix = `traefik.http.routers.envantertr-tenants-${protocol}`;
  assert.equal(labels.get(`${prefix}.rule`), rule);
  assert.equal(labels.get(`${prefix}.entrypoints`), protocol);
  assert.equal(labels.get(`${prefix}.priority`), '20');
  assert.equal(labels.get(`${prefix}.service`), 'envantertr-tenants');
}
assert.equal(labels.get('traefik.enable'), 'true');
assert.equal(labels.get('traefik.http.routers.envantertr-tenants-http.middlewares'), 'envantertr-tenant-https');
assert.equal(labels.get('traefik.http.middlewares.envantertr-tenant-https.redirectscheme.scheme'), 'https');
assert.equal(labels.get('traefik.http.middlewares.envantertr-tenant-https.redirectscheme.permanent'), 'true');
const tls = 'traefik.http.routers.envantertr-tenants-https.tls';
assert.equal(labels.get(tls), 'true');
assert.equal(labels.get(`${tls}.certresolver`), 'envantertr-dns');
assert.equal(labels.get(`${tls}.domains[0].main`), 'envantertr.com');
assert.equal(labels.get(`${tls}.domains[0].sans`), '*.envantertr.com');
assert.equal(labels.get('traefik.http.services.envantertr-tenants.loadbalancer.server.port'), '80');
assert.ok(entries.every(([key, value]) => !`${key}=${value}`.includes('$')), 'Avoid Compose/Coolify dollar interpolation');
console.log('Tenant wildcard routing source contract passed: TLS, redirect, host boundary, upstream and priority.');
