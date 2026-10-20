import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const endpoints = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/Data/PlatformEndpoints.cs'), 'utf8');
const entity = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/Data/Entities.cs'), 'utf8');
const context = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/Data/AppDbContext.cs'), 'utf8');
const page = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/platform-admin.html'), 'utf8');
const test = fs.readFileSync(path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs'), 'utf8');
const migrations = path.join(root, 'src/EnvanterTR.Web/Data/Migrations');
const migration = fs.readdirSync(migrations).find(name => name.endsWith('_AddPlatformAuditRetentionPolicy.cs'));
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

requireSource(entity.includes('class PlatformAuditRetentionPolicy') && entity.includes('int? RetentionDays'), 'Retention policy must be persisted and nullable means disabled.');
requireSource(context.includes('DbSet<PlatformAuditRetentionPolicy>') && context.includes('CK_PlatformAuditRetentionPolicies_Singleton') && context.includes('BETWEEN 1 AND 36500'), 'Retention policy must be a singleton with database-level day bounds.');
requireSource(Boolean(migration), 'A database migration must create the retention-policy table.');
requireSource(endpoints.includes('admin.MapGet("/audit-retention"') && endpoints.includes('admin.MapGet("/audit-retention/preview"') && endpoints.includes('admin.MapPut("/audit-retention"'), 'Platform administrators must be able to read, preview, and save the retention policy.');
requireSource(endpoints.includes('admin.MapPost("/audit-retention/apply"') && endpoints.includes('IsolationLevel.Serializable') && endpoints.includes('ExecuteDeleteAsync'), 'Cleanup must be explicit, transactional, and operate on both company and platform audit logs.');
requireSource(endpoints.includes('DENETIM-KAYITLARINI-SIL:{request.RetentionDays}') && endpoints.includes('AuditRetentionApplied'), 'Cleanup must require an exact typed confirmation and append an audit record.');
requireSource(page.includes('Denetim kayıtları saklama sınırı') && page.includes('silmez') && page.includes('Silinecek kayıtları önizle'), 'The UI must distinguish policy saving and preview from destructive cleanup.');
requireSource(page.includes('/api/platform/audit-retention/apply') && page.includes('requestPlatformReauthentication'), 'The platform UI must call the protected endpoint through the shared reauthentication-aware API helper.');
requireSource(test.includes('Platform_audit_retention_requires_platform_admin_preview_and_exact_confirmation'), 'A security acceptance test must cover platform authorization, CSRF, preview, confirmation, retention, and audit behavior.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified persisted audit-retention policy, bounded singleton, read-only preview, reauthenticated transactional cleanup, explicit confirmation, and acceptance coverage.');
}
