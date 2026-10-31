import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';

const root = new URL('../', import.meta.url);
const read = path => readFileSync(new URL(path, root), 'utf8');
const endpoints = read('src/EnvanterTR.Web/Data/PlatformCompanyDeletionEndpoints.cs');
const platform = read('src/EnvanterTR.Web/Data/PlatformEndpoints.cs');
const ui = read('src/EnvanterTR.Web/wwwroot/platform-admin.html');
const migration = read('src/EnvanterTR.Web/Data/Migrations/20261024212238_AddRecoverableCompanyDeletion.cs');
const sqlPath = 'tests/reports/company-deletion-120-to-121-20261025.sql';
const sqlBytes = readFileSync(new URL(sqlPath, root));
assert.equal(createHash('sha256').update(sqlBytes).digest('hex'), read(sqlPath + '.sha256').trim().split(/\s+/)[0]);
assert.equal(sqlBytes[0], 'S'.charCodeAt(0), 'Release SQL must be UTF-8 without a BOM.');
for (const forbidden of [/DROP\s+(?:TABLE|COLUMN)/i, /DELETE\s+FROM/i]) assert.doesNotMatch(sqlBytes.toString('utf8'), forbidden);
assert.doesNotMatch(migration, /migrationBuilder\.(?:Drop|Delete|Rename)/);
assert.ok(migration.includes('throw new NotSupportedException'), 'Destructive schema downgrade must remain disabled.');
for (const contract of ['MapCompanyDeletionEndpoints(admin)', 'row.DeletedAt', '[FromQuery] bool? deleted', 'company.DeletedAt == null']) assert.ok(platform.includes(contract), contract);
for (const contract of ['admin.MapDelete("/companies/{companyId:guid}"', 'admin.MapPost("/companies/{companyId:guid}/restore"',
  'IsolationLevel.Serializable', 'CompanySoftDeleted', 'CompanyRestored', 'company.DeletedAt = now', 'company.DeletedAt = null',
  'company.IsActive = false', 'item.CompanyId == companyId && item.RevokedAt == null', 'billingChanged = false',
  'request.ConfirmSlug?.Trim()', 'StringComparison.Ordinal']) assert.ok(endpoints.includes(contract), contract);
assert.doesNotMatch(endpoints, /(?:db\.Users\.Remove|UpdateSecurityStamp|ExecuteDelete|Companies\.Remove)/);
for (const contract of ['id="companyDeleted"', 'data-company-delete', 'data-company-restore', 'companyDeletionDialog',
  "confirmation!==target.slug", "method:target.restore?'POST':'DELETE'", 'hasDeleted', 'Geri al', 'Çöp kutusu']) assert.ok(ui.includes(contract), contract);
console.log('Verified recoverable company deletion source wiring, scoped revocation, exact confirmation, trash/restore UI and additive idempotent SQL artifact checksum. Runtime and live acceptance are separate gates.');
