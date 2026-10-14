import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
import vm from 'node:vm';

const api = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const inlineScript = page.match(/<script>([\s\S]*?)<\/script>/)?.[1];
assert.ok(inlineScript, 'platform admin inline program exists');
new vm.Script(inlineScript, { filename: 'platform-admin.html:inline-script' });

assert.match(api, /MapGet\("\/audit"[\s\S]*?string\? q = null[\s\S]*?DateTimeOffset\? from = null[\s\S]*?DateTimeOffset\? to = null/);
assert.match(api, /q\?\.Length > 120 \|\| action\?\.Length > 80 \|\| targetType\?\.Length > 80/);
assert.match(api, /x\.audit\.Action\.ToLower\(\)\.Contains\(term\)/);
assert.match(api, /x\.DisplayName\.ToLower\(\)\.Contains\(term\)/);
assert.match(api, /x\.audit\.OccurredAt >= from\.Value/);
assert.match(api, /x\.audit\.OccurredAt < to\.Value/);
assert.match(api, /ThenByDescending\(x => x\.audit\.Id\)/);
assert.match(api, /actorName = x\.DisplayName, actorEmail = x\.Email/);
for (const id of ['auditSearch', 'auditAction', 'auditTargetType', 'auditFrom', 'auditTo']) {
  assert.match(page, new RegExp(`id="${id}"`));
  assert.match(page, new RegExp(`#${id}`));
}
assert.match(page, /id="searchAudit"/);
assert.match(page, /id="clearAudit"/);
assert.match(page, /requestId!==state\.auditRequestId/);
assert.match(page, /actorEmail\|\|''/);
assert.match(page, /<details><summary>Detayı aç/);
assert.match(acceptance, /Platform_audit_search_filters_actor_action_target_and_date_with_stable_paging/);
assert.match(acceptance, /HttpStatusCode\.Unauthorized, anonymous\.StatusCode/);
assert.match(acceptance, /filtered.*actorName/s);
console.log('Platform audit search verification passed (19 assertions).');
