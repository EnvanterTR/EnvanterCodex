import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const read = (path) => readFile(path, 'utf8');
const [api, model, context, migration, publicPage, publicClient, adminPage, adminClient, acceptance] = await Promise.all([
  read('src/EnvanterTR.Web/Data/PlatformEndpoints.cs'),
  read('src/EnvanterTR.Web/Data/Entities.cs'),
  read('src/EnvanterTR.Web/Data/AppDbContext.cs'),
  read('src/EnvanterTR.Web/Data/Migrations/20261013201450_AddPlatformDemoRequests.cs'),
  read('src/EnvanterTR.PublicSite/index.html'),
  read('src/EnvanterTR.PublicSite/demo-request.js'),
  read('src/EnvanterTR.Web/wwwroot/platform-admin.html'),
  read('src/EnvanterTR.Web/wwwroot/platform-admin-demo-requests.js'),
  read('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs')
]);

const publicEndpoint = api.match(/endpoints\.MapPost\("\/api\/demo-requests"[\s\S]*?\.AllowAnonymous\(\)\.RequireRateLimiting\("login"\);/);
assert.ok(publicEndpoint, 'anonymous demo request endpoint with rate limit exists');
assert.match(publicEndpoint[0], /request\.Consent/);
assert.match(publicEndpoint[0], /MailAddress\.TryCreate/);
assert.match(publicEndpoint[0], /Website/);
assert.match(publicEndpoint[0], /AddDays\(-1\)/);
assert.match(publicEndpoint[0], /ConsentNoticeVersion = "demo-request-v1"/);
assert.doesNotMatch(publicEndpoint[0], /RemoteIpAddress|ClientIp|IPAddress/);

assert.match(model, /class PlatformDemoRequest/);
assert.match(model, /AssignedAdminUserId/);
assert.match(model, /FollowUpAt/);
assert.match(model, /LastContactedAt/);
assert.match(model, /ConsentNoticeVersion/);
assert.match(context, /CK_PlatformDemoRequests_Status/);
assert.match(migration, /CreateTable\(\s*name: "PlatformDemoRequests"/);
assert.match(migration, /ConsentNoticeVersion/);

const adminRoutes = api.match(/admin\.MapGet\("\/demo-requests"[\s\S]*?admin\.MapGet\("\/plans"/);
assert.ok(adminRoutes, 'platform demo request list/detail/update routes exist');
assert.match(adminRoutes[0], /admin\.MapGet\("\/demo-requests\/\{requestId:guid\}"/);
assert.match(adminRoutes[0], /admin\.MapPatch\("\/demo-requests\/\{requestId:guid\}"/);
assert.match(adminRoutes[0], /Math\.Clamp\(pageSize/);
assert.match(adminRoutes[0], /followUp == "overdue"/);
assert.match(adminRoutes[0], /FollowUpAt <= now/);
assert.match(adminRoutes[0], /followUp != "overdue"/);
assert.match(adminRoutes[0], /PlatformAuditEntries/);
assert.match(api, /MapGroup\("\/api\/platform"\)\.RequireAuthorization\("platform\.admin"\)/);
assert.match(adminRoutes[0], /TenantSubscriptions/);
assert.match(adminRoutes[0], /IsDemo/);
assert.match(adminRoutes[0], /DemoGranted/);
assert.match(adminRoutes[0], /Converted/);

assert.match(publicPage, /data-open-demo-request/);
assert.match(publicPage, /id="demoRequestDialog"/);
assert.match(publicPage, /name="consent" required/);
assert.match(publicPage, /name="website"/);
assert.match(publicPage, /\/demo-request\.js/);
assert.match(publicPage, /let publishedPlans\s*=\s*\[\]/, 'pricing starts safely before the plan request completes');
assert.match(publicPage, /cloudPlans\.forEach\(plan=>/, 'Cloud cards are rendered only from plans returned by the public API');
assert.doesNotMatch(publicPage, /cloudPlans\.length\?cloudPlans:Array\.from\(\{length:copy\.tierNames\.length\}/, 'empty catalog must not fabricate Cloud offer cards');
assert.match(publicPage, /if \(!cloudPlans\.length\)[\s\S]*?pricingAvailabilityCopy\[language\]/, 'loading, empty catalog, and API failure have localized explicit states');
assert.match(publicPage, /pricingAvailabilityCopy\s*=\s*\{[\s\S]*?tr:\s*\{ loading:[\s\S]*?en:\s*\{ loading:[\s\S]*?ru:\s*\{ loading:[\s\S]*?uk:\s*\{ loading:[\s\S]*?mk:\s*\{ loading:/, 'Cloud catalog states are translated into every supported language');
assert.match(publicPage, /pricing-grid[^]*?pricing-state[^]*?Cloud planları yükleniyor/, 'no-script markup does not advertise hardcoded Cloud tiers');
assert.match(publicPage, /data-open-demo-request/, 'product tour request remains available while pricing is unpublished');
for (const language of ['tr', 'en', 'ru', 'uk', 'mk']) assert.match(publicClient, new RegExp(`^    ${language}:`, 'm'));
assert.match(publicClient, /fetch\('\/api\/demo-requests'/);
assert.match(publicClient, /form\.elements\.consent\.checked/);

assert.match(adminPage, /data-view="demo-requests"/);
assert.match(adminPage, /id="view-demo-requests"/);
assert.match(adminPage, /platform-admin-demo-requests\.js/);
assert.match(adminClient, /\/api\/platform\/demo-requests/);
assert.match(adminClient, /\/api\/platform\/admins/);
assert.match(adminClient, /X-CSRF-TOKEN/);
assert.match(adminClient, /followUpAt/);
assert.match(adminClient, /demoLeadFollowUp/);
assert.match(adminClient, /leadRequestVersion/);
assert.match(adminClient, /companyRequestVersion/);
assert.match(adminClient, /detailRequestVersion/);
const companyLoader = adminClient.match(/async function loadCompanies\([\s\S]*?\n  }\n\n  async function showLead/)?.[0];
assert.ok(companyLoader, 'demo company search loader exists');
assert.match(companyLoader, /const current = await api\([\s\S]*?if \(requestVersion !== state\.companyRequestVersion\) return;/,
  'the selected-company fallback response must be discarded after a newer search starts');
assert.match(companyLoader, /if \(requestVersion !== state\.companyRequestVersion\) return;\s*select\.value = selectedId/,
  'stale company search must not restore the prior selection');
assert.match(adminClient, /params\.set\('followUp', followUp\)/);
assert.match(adminClient, /companyId/);
assert.match(adminClient, /history/);

assert.match(acceptance, /Public_demo_requests_are_validated_deduplicated_and_visible_only_to_platform_admins/);
assert.match(acceptance, /Platform_demo_request_stages_require_real_demo_or_paid_company_subscription_and_are_audited/);
assert.match(acceptance, /followUp=overdue/);
console.log('Platform demo request and no-plan sales fallback source verification passed (55 assertions).');
