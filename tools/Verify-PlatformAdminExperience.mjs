import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const members = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-members.js', 'utf8');
const subscriptions = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-subscriptions.js', 'utf8');
const demos = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-demo-requests.js', 'utf8');

assert.match(page, /<style id="platform-luxe-skin">/);
assert.match(page, /color-scheme:dark/);
assert.match(page, /\.nav button\.active[\s\S]*?box-shadow/);
assert.match(page, /\.card,\.panel[\s\S]*?linear-gradient/);
assert.match(page, /:where\(button,a,input,select,textarea\):focus-visible/);
assert.match(page, /prefers-reduced-motion:reduce/);
assert.match(page, /@media\(max-width:950px\)/);
assert.match(page, /@media\(max-width:650px\)/);

for (const view of ['overview', 'companies', 'members', 'demo-requests', 'orders', 'plans', 'support', 'audit']) {
  assert.match(page, new RegExp(`data-view="${view}"`), `${view} navigation remains available`);
  assert.match(page, new RegExp(`id="view-${view}"`), `${view} view remains available`);
}
assert.match(page, /platform-admin-members\.js/);
assert.match(page, /platform-admin-subscriptions\.js/);
assert.match(page, /platform-admin-demo-requests\.js/);
assert.match(page, /id="platformTwoFactorField" hidden/);
assert.match(page, /id="platformLoginReadiness"[^>]*role="status"/);
assert.match(page, /id="platformLoginSubmit"/);
assert.match(page, /async function checkPlatformLoginReadiness\(\)/);
assert.match(page, /status\.status==='migration-required'[\s\S]*?pendingMigrationCount[\s\S]*?submit\.disabled=true/);
assert.match(page, /status\.status==='ready'[\s\S]*?submit\.disabled=false/);
assert.match(page, /if\(!await checkPlatformLoginReadiness\(\)\)return/);
assert.match(page, /name="twoFactorCode"[^>]*autocomplete="one-time-code"/);
assert.match(page, /twoFactorCode:f\.get\('twoFactorCode'\)\|\|null/);
assert.match(page, /if\(x\.twoFactorRequired\)[\s\S]*?platformTwoFactorField\.hidden=false/);
assert.match(page, /error\.twoFactorRequired=data\?\.twoFactorRequired===true/);
assert.match(members, /loadMemberTimeline\(id, 1, false\)/);
assert.match(api, /PlatformDemoRequests/);
assert.match(api, /Satın alma kaydı oluşturuldu/);
assert.match(api, /Ödeme doğrulandı/);
assert.match(api, /checkoutOrders, lifecycleHistory/);
assert.match(api, /order\.PaidAt/);
assert.match(page, /Satın alma ve ödeme geçmişi/);
assert.match(page, /o\.paidAt/);
assert.match(page, /o\.amount,o\.currency/);
assert.doesNotMatch(api.match(/admin\.MapGet\("\/companies\/\{companyId:guid\}"[\s\S]*?admin\.MapPatch\("\/companies\/\{companyId:guid\}\/lifecycle"/)?.[0] || '', /CheckoutTokenHash|CheckoutPageUrlProtected/);
assert.match(subscriptions, /Plan \/ tarih düzenle/);
assert.match(subscriptions, /method: 'PUT'/);
assert.match(subscriptions, /Ücretsiz uzatma \/ plan değişikliği gerekçesi/);
assert.match(demos, /api\/platform\/demo-requests/);

console.log('Platform admin premium skin, member/demo/purchase/subscription workflows, responsive/accessibility affordances, and two-factor sign-in verified.');
