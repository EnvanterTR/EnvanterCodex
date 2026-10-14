import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';

const api = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const contract = readFileSync('src/EnvanterTR.Web/Data/PlatformContracts.cs', 'utf8');
const page = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(api, /admin\.MapGet\("\/plans"/);
assert.match(api, /admin\.MapPost\("\/plans"/);
assert.match(api, /admin\.MapPut\("\/plans\/\{id:guid\}"/);
assert.match(api, /request\.ChangeReason[\s\S]*?changeReason/);
assert.match(api, /PlanCreated[\s\S]*?creationDetails/);
assert.match(api, /PlanUpdated[\s\S]*?before = previous[\s\S]*?after = current/);
assert.match(api, /FeatureFlagsJson[\s\S]*?JsonDocument\.Parse/);
assert.match(api, /request\.DeploymentKind == "cloud" && request\.MaxLoginUsers is not null/);
assert.match(contract, /string\? ChangeReason/);
assert.match(page, /id="view-plans"/);
assert.match(page, /name="changeReason" required minlength="8" maxlength="500"/);
assert.match(page, /api\/platform\/plans/);
assert.match(acceptance, /Platform_plan_catalog_requires_reason_and_audits_pricing_changes_before_publishing/);
assert.match(acceptance, /Maliyet ve yıllık fiyatlandırma güncellendi/);
assert.match(acceptance, /"monthlyPrice"/);
console.log('Platform plan management verification passed (15 assertions).');
