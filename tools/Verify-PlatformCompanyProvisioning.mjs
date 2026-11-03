import { readFileSync } from 'node:fs';

const endpoint = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const ui = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const failures = [];

for (const contract of [
  'admin.MapPost("/companies"',
  'TenantHostResolver.IsValidSlug(slug)',
  'request.TermDays is < 1 or > 1095',
  'request.PaymentVerified',
  'x.IsActive && x.DeploymentKind == "cloud"',
  'BeginTransactionAsync(IsolationLevel.Serializable',
  'Status = isTrial ? "Trial" : "Active", IsDemo = isTrial',
  'RoleKey = "owner"',
  'PaymentReference = isTrial ? null : paymentReference',
  '"/companies/{companyId:guid}/owner-invitation/resend"'
]) {
  if (!endpoint.includes(contract)) failures.push(`Server provisioning contract missing: ${contract}`);
}

for (const contract of [
  'id="provisionCompany"',
  "openAction('Cloud şirketi oluştur',null,'provision')",
  "p.isActive&&p.deploymentKind==='cloud'",
  "api('/api/platform/companies',{method:'POST'",
  "paymentVerified:f.get('provisioningMode')==='paid'&&f.get('paymentVerified')==='on'",
  'name="provisioningMode"',
  'name="paymentVerified" type="checkbox" required',
  'data-resend-owner',
  '/owner-invitation/resend'
]) {
  if (!ui.includes(contract)) failures.push(`Platform UI provisioning contract missing: ${contract}`);
}

if (!acceptance.includes('Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation') ||
    !acceptance.includes('Assert.Equal("Active", subscription.Status)') ||
    !acceptance.includes('Assert.Equal("owner", membership.RoleKey)')) {
  failures.push('Isolated PostgreSQL acceptance must cover paid provisioning, owner membership, subscription, email, and retry.');
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}

console.log('PASS: Platform company provisioning validates payment attestation, creates a Cloud tenant and owner, and supports invitation retry.');
