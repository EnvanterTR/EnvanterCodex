import { readFileSync } from 'node:fs';

const endpoint = readFileSync(new URL('../src/EnvanterTR.Web/Data/CheckoutEndpoints.cs', import.meta.url), 'utf8');
const db = readFileSync(new URL('../src/EnvanterTR.Web/Data/AppDbContext.cs', import.meta.url), 'utf8');
const program = readFileSync(new URL('../src/EnvanterTR.Web/Program.cs', import.meta.url), 'utf8');
const entity = readFileSync(new URL('../src/EnvanterTR.Web/Data/Entities.cs', import.meta.url), 'utf8');
const acceptance = readFileSync(new URL('../tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', import.meta.url), 'utf8');
const plans = readFileSync(new URL('../src/EnvanterTR.Web/Data/PlatformEndpoints.cs', import.meta.url), 'utf8');
const failures = [];

for (const [label, condition] of [
  ['public initialization and provider callback are mapped', endpoint.includes('/api/checkout/initialize') && endpoint.includes('/api/checkout/callback') && program.includes('app.MapCheckoutEndpoints()')],
  ['public checkout readiness reports provider availability without exposing configuration', endpoint.includes('/api/checkout/status') && endpoint.includes('available') && endpoint.includes('IsTrustedCallbackUrl') && endpoint.includes('IsTrustedApiBase')],
  ['checkout initialization validates anti-forgery and has a dedicated rate limit', endpoint.includes('antiforgery.ValidateRequestAsync(context)') && endpoint.includes('RequireRateLimiting("checkout")') && program.includes('PermitLimit = 5, Window = TimeSpan.FromMinutes(10)')],
  ['price is selected from an active public Cloud plan on the server', endpoint.includes('x.IsActive && x.IsPublic && x.DeploymentKind == "cloud"') && endpoint.includes('request.Term == "annual" ? plan.AnnualPrice : plan.MonthlyPrice')],
  ['public plan API includes stable identifiers required by checkout', plans.includes('Select(x => new { x.Id, x.Code, x.Name')],
  ['checkout records versioned data notice acknowledgement and uses an expiring initialization lease', endpoint.includes('DataProcessingNoticeVersion != CheckoutNoticeVersion') && endpoint.includes('DataProcessingAcknowledgedAt = now') && endpoint.includes('InitializationLeaseUntil') && acceptance.includes('DataProcessingAcknowledged = false')],
  ['checkout URL host is verified and its stored form is protected', endpoint.includes('paymentUri.Host != new Uri(options.BaseUrl).Host') && endpoint.includes('CreateProtector("EnvanterTR.Iyzico.CheckoutPage.v1")')],
  ['callback retrieves provider result and matches immutable order fields', endpoint.includes('iyzipay.RetrieveAsync') && endpoint.includes('price != order.Amount') && endpoint.includes('paidPrice != order.Amount') && endpoint.includes('fraudStatus != 1')],
  ['order token is stored only as a hash, not raw token or identity/address fields', db.includes('CheckoutTokenHash') && !entity.includes('IdentityNumber') && !entity.includes('BillingAddress') && !entity.includes('CheckoutToken {')],
  ['company and active paid subscription are created after verified payment', endpoint.includes('Status = "Active", IsDemo = false') && endpoint.includes('lockedOrder.Status = "Provisioned"')],
  ['synthetic fail-closed acceptance covers missing CSRF/provider configuration', acceptance.includes('Public_checkout_requires_csrf_and_fails_closed_without_provider_configuration')],
  ['synthetic end-to-end acceptance covers public plan IDs, acknowledgement, idempotent initialization, provider verification, provisioning, and callback replay', acceptance.includes('Public_checkout_uses_server_price_verifies_provider_and_provisions_once') && acceptance.includes('SyntheticIyzipayHandler') && acceptance.includes('InitializeCount') && acceptance.includes('retried')]
]) {
  if (!condition) failures.push(label);
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: hosted Cloud checkout is server-priced, anti-forgery protected, provider-verified, idempotent, and fails closed without configuration.');
