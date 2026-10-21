import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const html = fs.readFileSync(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const body = html.match(/    function resolveCompanyPortal\(session, currentUrl\) \{[\s\S]*?\n    \}/)?.[0];
assert.ok(body, 'Company portal resolver exists');
const resolve = vm.runInNewContext(`${body}; resolveCompanyPortal`, { URL });
const session = url => ({ companyId: 'farma', availableCompanies: [{ id: 'farma', tenantUrl: url }] });
for (const host of ['envantertr.com', 'www.envantertr.com']) {
  assert.equal(resolve(session('https://farma.envantertr.com/'), `https://${host}/giris`), 'https://farma.envantertr.com/');
}
for (const origin of ['http://localhost:8080/', 'https://localhost/', 'https://santa.envantertr.com/', 'https://other.example/'])
  assert.equal(resolve(session('https://farma.envantertr.com/'), origin), null);
for (const url of ['https://evil.example/', 'https://farma.envantertr.com.evil.example/', 'https://x.y.envantertr.com/',
  'http://farma.envantertr.com/', 'https://user:password@farma.envantertr.com/', 'https://farma.envantertr.com:444/',
  'https://admin.envantertr.com/', 'https://-farma.envantertr.com/', 'https://farma.envantertr.com/path',
  'https://farma.envantertr.com/?token=test', 'https://farma.envantertr.com/#test', 'invalid'])
  assert.equal(resolve(session(url), 'https://envantertr.com/giris'), null, url);
assert.equal(resolve({ companyId: 'santa', availableCompanies: session('https://farma.envantertr.com/').availableCompanies }, 'https://envantertr.com/'), null);
assert.equal(resolve({ companyId: 'farma' }, 'https://envantertr.com/'), null);
assert.match(html, /const companyPortal = resolveCompanyPortal\(session, window.location.href\);\s*if \(companyPortal\) \{ window.location.replace\(companyPortal\); return; \}/);
console.log('PASS: Cloud company portal login routing, membership selection, local origins and unsafe URL rejection');
const changedBody = html.match(/    function resolveChangedCompanyPortal\(saved, previousSlug, tenantDomain, currentUrl\) \{[\s\S]*?\n    \}/)?.[0];
assert.ok(changedBody);
const changed = vm.runInNewContext(`${changedBody}; resolveChangedCompanyPortal`, { URL });
for (const url of ['https://farma.envantertr.com', 'https://farma.envantertr.com/'])
  assert.equal(changed({ slug: 'farma', tenantUrl: url }, 'santa', 'envantertr.com', 'https://santa.envantertr.com/'), 'https://farma.envantertr.com/');
for (const url of ['https://evil.example/', 'https://farma.envantertr.com/?token=test',
  'https://user@farma.envantertr.com/', 'https://farma.envantertr.com:444/', 'http://farma.envantertr.com/', 'invalid'])
  assert.equal(changed({ slug: 'farma', tenantUrl: url }, 'santa', 'envantertr.com', 'https://santa.envantertr.com/'), null);
for (const origin of ['https://envantertr.com/', 'http://localhost:8080/', 'https://other.envantertr.com/'])
  assert.equal(changed({ slug: 'farma', tenantUrl: 'https://farma.envantertr.com/' }, 'santa', 'envantertr.com', origin), null);
assert.equal(changed({ slug: 'santa', tenantUrl: 'https://santa.envantertr.com/' }, 'santa', 'envantertr.com', 'https://santa.envantertr.com/'), null);
assert.match(html, /form\.dataset\.savedSlug = profile\.slug/);
assert.match(html, /previousSlug = form\.dataset\.savedSlug/);
assert.match(html, /location\.replace\(nextPortal\)/);
console.log('PASS: persisted old slug, changed company host, slash variants, local origin and unsafe target rejection');
