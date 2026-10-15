import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const nginx = readFileSync('deploy/coolify/nginx-public.conf', 'utf8');
const readme = readFileSync('deploy/coolify/README.md', 'utf8');
const marketing = nginx.match(/server\s*\{[\s\S]*?server_name envantertr\.com www\.envantertr\.com;[\s\S]*?\n\}/)?.[0];
const wildcardStart = nginx.indexOf('server_name *.envantertr.com;');
assert.ok(marketing, 'The default public marketing host must remain configured.');
assert.ok(wildcardStart >= 0, 'Coolify Nginx must declare a wildcard tenant host.');
assert.ok(nginx.indexOf('server_name envantertr.com www.envantertr.com;') < wildcardStart, 'Apex and www marketing hosts must take precedence over wildcard tenant hosts.');
const wildcard = nginx.slice(nginx.lastIndexOf('server {', wildcardStart), nginx.indexOf('\n}', wildcardStart) + 2);
assert.match(wildcard, /server_name\s+\*\.envantertr\.com;/, 'Tenant traffic must be limited to the configured wildcard domain.');
assert.match(wildcard, /location\s+\/\s*\{[\s\S]*?proxy_pass\s+http:\/\/web:8080;/, 'Every tenant path must enter the company app.');
assert.match(wildcard, /proxy_set_header\s+Host\s+\$host;/, 'The requested tenant host must reach the app.');
assert.match(wildcard, /proxy_set_header\s+X-Forwarded-For\s+\$proxy_add_x_forwarded_for;/, 'Client forwarding metadata must be preserved.');
assert.match(wildcard, /proxy_set_header\s+X-Forwarded-Proto\s+\$http_x_forwarded_proto;/, 'The TLS scheme from the Coolify proxy must be preserved.');
assert.match(wildcard, /client_max_body_size\s+32m;/, 'SaaS upload requests must retain the documented size budget.');
assert.match(readme, /`\*\.envantertr\.com`[\s\S]*?wildcard DNS[\s\S]*?TLS certificate/i, 'Coolify setup must document wildcard DNS and TLS requirements.');
assert.match(readme, /wildcard host's TLS certificate, login page, `\/api\/status`, and a tenant-scoped authenticated request/i, 'Production acceptance must check the tenant app and authenticated scope.');
assert.match(readme, /`https:\/\/\*\.envantertr\.com`[\s\S]*?`public-site` service[\s\S]*?container port `80`[\s\S]*?redeploy/i, 'The wildcard host must be explicitly routed to the public-site Compose service on its internal port and redeployed.');
assert.match(readme, /HTTP 503 with `no available server`[\s\S]*?public proxy\/edge[\s\S]*?does not establish[\s\S]*?Cloudflare[\s\S]*?Coolify/i, 'The runbook must distinguish a public proxy/edge 503 from an application login failure without assuming which upstream layer is misconfigured.');
assert.match(marketing, /try_files\s+\$uri\s+\$uri\/\s+\/index\.html;/, 'The marketing host must keep serving its public-site fallback.');

console.log('Verified apex/www and tenant proxy rules, internal forwarding, wildcard Coolify service/port/redeploy setup, 503 proxy troubleshooting, TLS/DNS guidance, production acceptance checklist, and marketing fallback.');
