import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const entity = await readFile('src/EnvanterTR.Web/Data/Entities.cs', 'utf8');
const context = await readFile('src/EnvanterTR.Web/Data/AppDbContext.cs', 'utf8');
const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(entity, /CustomerLifecycleStage\s*\{\s*get;\s*set;\s*\}\s*=\s*"Unclassified"/);
assert.match(entity, /Guid\? PlatformOwnerUserId/);
assert.match(entity, /DateTimeOffset\? PlatformNextFollowUpAt/);
assert.match(context, /CustomerLifecycleStage.*HasMaxLength\(32\)/);
assert.match(context, /CustomerLifecycleStage, x\.PlatformNextFollowUpAt/);
assert.match(api, /lifecycleStage = x\.CustomerLifecycleStage/);
assert.match(api, /lifecycleOwnerName = db\.Users/);
assert.match(api, /MapPatch\("\/companies\/\{companyId:guid\}\/lifecycle"/);
assert.match(api, /"Unclassified", "Onboarding", "Active", "RenewalDue", "AtRisk", "Churned"/);
assert.match(api, /PlatformAdministrators\.AsNoTracking\(\)[\s\S]*assignedId/);
assert.match(api, /CustomerLifecycleUpdated/);
assert.match(api, /lifecycleHistory/);
assert.match(page, /Müşteri yaşam döngüsü/);
assert.match(page, /Sorumlu platform yöneticisi/);
assert.match(page, /Sonraki takip zamanı/);
assert.match(page, /yaşam döngüsü güncellemesi yok/);
assert.match(page, /method:'PATCH'[\s\S]*nextFollowUpAt/);
assert.match(acceptance, /Platform_customer_lifecycle_is_validated_assigned_audited_and_persisted/);
console.log('Platform customer lifecycle verification passed (18 assertions).');
