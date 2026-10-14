import { readFileSync } from 'node:fs';

const migration = readFileSync('src/EnvanterTR.Web/Data/Migrations/20261014090000_EnforceAssetPlanQuota.cs', 'utf8');
const quota = readFileSync('src/EnvanterTR.Web/Data/AssetPlanQuota.cs', 'utf8');
const assets = readFileSync('src/EnvanterTR.Web/Data/AssetEndpoints.cs', 'utf8');
const exports = readFileSync('src/EnvanterTR.Web/Data/DataExportEndpoints.cs', 'utf8');
const app = readFileSync('src/EnvanterTR.Web/Program.cs', 'utf8');
const tests = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const failures = [];

if (!migration.includes('UPDATE "Companies" AS company') ||
    !migration.includes('BEFORE INSERT OR DELETE ON "Assets"') ||
    !migration.includes('BEFORE UPDATE OF "CompanyId" ON "Assets"') ||
    !migration.includes('company."AssetCount" < v_max_assets') ||
    !migration.includes("subscription.\"Status\" IN ('Active', 'Trial')") ||
    !migration.includes("ERRCODE = 'P0001'") ||
    !migration.includes('DROP FUNCTION IF EXISTS "MaintainAssetPlanQuota"()')) {
  failures.push('Database migration must backfill counters and atomically enforce active plan limits on inserts and tenant moves, and release usage on deletes.');
}
if (!quota.includes('subscription.StartsAt <= now && subscription.EndsAt > now') ||
    !quota.includes('company.AssetCount') ||
    !quota.includes('asset_quota_exceeded')) {
  failures.push('Quota reads and error mapping must match active subscription windows and the durable company counter.');
}
if (!assets.includes('AssetPlanQuota.IsExceeded(exception)') ||
    !exports.includes('AssetPlanQuota.IsExceeded(exception)') ||
    !exports.includes('AssetPlanQuota.GetLimitAsync(db, companyId, cancellationToken)') ||
    !exports.includes('AssetPlanQuota.GetUsageAsync(db, companyId, cancellationToken)') ||
    !app.includes('catch (Exception exception) when (AssetPlanQuota.IsExceeded(exception)')) {
  failures.push('Manual creation, import preview/commit, and cross-endpoint quota errors must be wired to consistent responses.');
}
if (!tests.includes('Asset_plan_quota_covers_manual_import_and_purchase_receipt_with_atomic_upgrade_behavior') ||
    !tests.includes('Assert.Equal(HttpStatusCode.PaymentRequired, deniedReceipt.StatusCode)') ||
    !tests.includes('Assert.Equal(HttpStatusCode.OK, upgradedImport.StatusCode)')) {
  failures.push('PostgreSQL acceptance coverage must verify manual create, import, purchase receipt rollback, and limit upgrade.');
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: PostgreSQL plan asset quota protects direct writes, imports, purchase receipts, and tenant moves with atomic counters.');
