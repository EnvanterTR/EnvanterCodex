import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
import vm from 'node:vm';

const api = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const inlineScript = page.match(/<script>([\s\S]*?)<\/script>/)?.[1];
assert.ok(inlineScript, 'platform admin inline program exists');
new vm.Script(inlineScript, { filename: 'platform-admin.html:inline-script' });

assert.match(api, /MapGet\("\/audit"[\s\S]*?string\? q = null[\s\S]*?string\? source = null[\s\S]*?DateTimeOffset\? from = null[\s\S]*?DateTimeOffset\? to = null/);
assert.match(api, /q\?\.Length > 120 \|\| action\?\.Length > 80 \|\| targetType\?\.Length > 80/);
assert.match(api, /db\.PlatformAuditEntries\.AsNoTracking\(\)\.Select/);
assert.match(api, /db\.AuditEntries\.AsNoTracking\(\)\.Select/);
assert.match(api, /platformQuery\.Concat\(companyQuery\)/);
assert.match(api, /x\.Action\.ToLower\(\)\.Contains\(term\)/);
assert.match(api, /x\.CompanyName \?\? ""/);
assert.match(api, /x\.OccurredAt >= from\.Value/);
assert.match(api, /x\.OccurredAt < to\.Value/);
assert.match(api, /ThenByDescending\(x => x\.Id\)/);
assert.match(api, /actorName = x\.ActorName, actorEmail = x\.ActorEmail/);
assert.match(api, /source = x\.Source, x\.ActorUserId/);
for (const id of ['auditSearch', 'auditSource', 'auditAction', 'auditTargetType', 'auditFrom', 'auditTo']) {
  assert.match(page, new RegExp(`id="${id}"`));
  assert.match(page, new RegExp(`#${id}`));
}
assert.match(page, /id="searchAudit"/);
assert.match(page, /id="clearAudit"/);
assert.match(page, /requestId!==state\.auditRequestId/);
assert.match(page, /actorEmail\|\|''/);
assert.match(page, /x\.source==='company'\?'Şirket':'Platform'/);
assert.match(page, /companyName\|\|x\.companyId/);
assert.match(page, /<details><summary>Detayı aç/);
assert.match(acceptance, /Platform_audit_search_filters_actor_action_target_and_date_with_stable_paging/);
assert.match(acceptance, /HttpStatusCode\.Unauthorized, anonymous\.StatusCode/);
assert.match(acceptance, /filtered.*actorName/s);
assert.match(acceptance, /tenantMarker/);
assert.match(acceptance, /invalidSource/);
console.log('Unified platform and company audit search verification passed (25 assertions).');
