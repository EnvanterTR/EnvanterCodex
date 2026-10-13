import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const editor = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-subscriptions.js', 'utf8');
const contracts = await readFile('src/EnvanterTR.Web/Data/PlatformContracts.cs', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(contracts, /record EditSubscriptionRequest\(Guid PlanId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string\? Note\)/);
assert.match(api, /MapPut\("\/companies\/\{companyId:guid\}\/subscriptions\/\{subscriptionId:guid\}"/);
assert.match(api, /x\.Id == subscriptionId && x\.CompanyId == companyId/);
assert.match(api, /x\.IsActive && x\.DeploymentKind == "cloud"/);
assert.match(api, /request\.EndsAt <= request\.StartsAt/);
assert.match(api, /request\.EndsAt <= request\.StartsAt/);
assert.match(api, /before = previous, after = current, note = Clean\(request\.Note\)/);
assert.match(api, /subscription\.PlanId = plan\.Id;[\s\S]*?subscription\.StartsAt = request\.StartsAt\.ToUniversalTime\(\);[\s\S]*?subscription\.EndsAt = request\.EndsAt\.ToUniversalTime\(\);/);
assert.match(api, /select new \{ subscription\.Id, subscription\.PlanId, plan\.Name/);
assert.match(page, /platform-admin-subscriptions\.js/);
assert.match(editor, /data-action="details"/);
assert.match(editor, /Plan \/ tarih düzenle/);
assert.match(editor, /startsAt.type = 'datetime-local'/);
assert.match(editor, /api\/platform\/companies\/\$\{companyId\}\/subscriptions\/\$\{subscription\.id\}/);
assert.match(editor, /method: 'PUT'/);
assert.match(editor, /refreshAudit/);
assert.match(acceptance, /Platform_admin_can_adjust_subscription_plan_and_dates_with_validation_and_audit/);
console.log('Platform subscription editor verification passed (17 assertions).');
