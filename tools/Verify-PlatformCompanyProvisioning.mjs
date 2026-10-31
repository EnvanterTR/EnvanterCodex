import { readFileSync } from 'node:fs';

const endpoint = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const provisioning = readFileSync('src/EnvanterTR.Web/Data/TenantProvisioningService.cs', 'utf8');
const ui = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const demoAcceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/PlatformDemoProvisioningAcceptanceTests.cs', 'utf8');
const failures = [];

for (const contract of [
  'admin.MapPost("/companies"',
  'TenantHostResolver.IsValidSlug(slug)',
  'request.TermDays is < 1 or > 1095',
  'request.PaymentVerified',
  'x.IsActive && x.DeploymentKind == "cloud"',
  'BeginTransactionAsync(IsolationLevel.Serializable',
  'provisioning.ProvisionCloudAsync',
  '"/companies/{companyId:guid}/owner-invitation/resend"'
]) {
  if (!endpoint.includes(contract)) failures.push(`Server provisioning contract missing: ${contract}`);
}

for (const contract of ['Status = request.IsTrial || request.IsDemo ? "Trial" : "Active", IsDemo = request.IsDemo',
  'request.IsDemo && !string.IsNullOrWhiteSpace(request.PaymentReference)',
  'RoleKey = "owner"', 'PaymentReference = request.PaymentReference', 'SecurityRequireMfa = true',
  'db.Database.CurrentTransaction is null']) {
  if (!provisioning.includes(contract)) failures.push(`Shared provisioning contract missing: ${contract}`);
}

for (const contract of [
  'id="provisionCompany"',
  "openAction('Ücretli Cloud şirketi oluştur',null,'provision')",
  "p.isActive&&p.deploymentKind==='cloud'",
  "api('/api/platform/companies',{method:'POST'",
  "paymentVerified:f.get('paymentVerified')==='on'",
  'name="paymentVerified" type="checkbox" required',
  'data-resend-owner',
  '/owner-invitation/resend'
]) {
  if (!ui.includes(contract)) failures.push(`Platform UI provisioning contract missing: ${contract}`);
}

for (const contract of ['request.TermDays > 60 || request.PaymentVerified',
  'request.Note.Trim().Length is < 8 or > 1000',
  'request.IsDemo ? "DemoCompanyProvisioned" : "PaidCompanyProvisioned"',
  'IsDemo: request.IsDemo']) {
  if (!endpoint.includes(contract)) failures.push(`Reasoned unpaid demo contract missing: ${contract}`);
}
for (const contract of ['id="provisionDemoCompany"', "isDemo:a.type==='provisionDemo'",
  "type==='provisionDemo'?60:1095", "type==='provisionDemo'?15:365", 'name="note" minlength="8"']) {
  if (!ui.includes(contract)) failures.push(`Demo provisioning UI contract missing: ${contract}`);
}
if (!demoAcceptance.includes('Platform_demo_provisioning_uses_shared_service_preserves_owner_and_records_no_payment') ||
    !demoAcceptance.includes('Platform_demo_provisioning_still_requires_csrf_after_fresh_mfa_login') ||
    !demoAcceptance.includes('Platform_demo_provisioning_still_requires_platform_mfa'))
  failures.push('Demo provisioning must retain owner, payment separation, MFA and CSRF acceptance.');

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
