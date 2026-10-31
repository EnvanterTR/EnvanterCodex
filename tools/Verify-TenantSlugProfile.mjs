import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const profileUi = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const profileApi = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const tenantResolver = readFileSync('src/EnvanterTR.Web/Data/TenantHostResolver.cs', 'utf8');
const accountApi = readFileSync('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const requestMiddleware = readFileSync('src/EnvanterTR.Web/Program.cs', 'utf8');
const coolifyCompose = readFileSync('deploy/coolify/compose.yaml', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const continuityAcceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantHostSessionContinuityAcceptanceTests.cs', 'utf8');

assert.match(profileUi, /name="slug" maxlength="63"[^>]*required[^>]*aria-describedby="tenantSlugHelp"/, 'Company profile must require and explain a DNS-sized tenant slug.');
assert.match(profileUi, /JSON\.stringify\(\{[^}]*slug:\s*data\.get\('slug'\)/, 'The profile form must persist the slug through the profile API.');
assert.match(profileUi, /location\.replace\(nextPortal\)/, 'Changing a tenant slug from its current hostname must move the browser to the validated new tenant URL.');
assert.match(profileUi, /previousSlug = form\.dataset\.savedSlug/, 'Compare the saved old slug, not the edited new input.');
assert.match(profileUi, /id="tenantUrlLink"/, 'Company profile must expose the tenant address as a direct link.');
assert.match(profileUi, /form\.dataset\.tenantDomain = profile\.tenantDomain/, 'Tenant URL preview must use the configured root domain.');
assert.match(profileApi, /Select\(x => new \{ x\.Name, x\.Slug, x\.ContactEmail/, 'The company profile API must return the current tenant slug.');
assert.match(profileApi, /TenantHostResolver\.IsValidSlug\(slug\)/, 'The company profile API must validate tenant slugs.');
assert.match(profileApi, /company\.Slug = slug/, 'A valid company slug must be persisted.');
assert.match(profileApi, /PostgresErrorCodes\.UniqueViolation/, 'Concurrent duplicate slug updates must return a controlled conflict.');
assert.match(tenantResolver, /ReservedSlugs/, 'Routing-reserved subdomains must not be claimable.');
assert.match(accountApi, /company\.Slug == tenantSlug/, 'Tenant-host login must resolve the company from the hostname slug.');
const hostGuardStart = requestMiddleware.indexOf('if (tenantSlug is not null && currentMembership.Slug != tenantSlug)');
assert.ok(hostGuardStart >= 0, 'Authenticated sessions must explicitly reject a different hostname company.');
const hostGuard = requestMiddleware.slice(hostGuardStart).match(/^if[^]*?\{([^]*?)\n        \}/)?.[1];
assert.ok(hostGuard, 'The hostname guard must have an explicit rejection body.');
assert.match(hostGuard, /StatusCodes\.Status401Unauthorized;\s*return;/, 'A mismatched company host must reject the request before endpoint execution.');
assert.doesNotMatch(hostGuard, /SignOutAsync/, 'A wrong hostname must not erase a valid shared company cookie.');
const membershipGuard = requestMiddleware.indexOf('if (currentMembership is null)');
const roleGuard = requestMiddleware.indexOf('if (!roleAuthorization.Valid)');
assert.ok(membershipGuard >= 0 && membershipGuard < hostGuardStart && roleGuard >= 0 && roleGuard < hostGuardStart,
  'Invalid membership/company and role checks must run before preserving a wrong-host session.');
assert.match(continuityAcceptance, /Wrong_tenant_host_preserves_only_still_valid_shared_sessions/, 'Real shared-cookie continuity acceptance must remain present.');
for (const invalidation of ['revoked-session', 'suspended-company', 'removed-membership'])
  assert.ok(continuityAcceptance.includes(invalidation), `Missing invalid-session continuity control: ${invalidation}`);
assert.match(requestMiddleware, /StartsWithSegments\("\/api\/platform"\)/, 'Platform administration routes must not be exposed on customer tenant hosts.');
assert.match(requestMiddleware, /tenantSlug is not null && companyClaim is null/, 'Platform-only sessions must not be reused on customer tenant hosts.');
assert.match(requestMiddleware, /Cookie\.Domain = builder\.Configuration\["TenantRouting:CookieDomain"\]/, 'The SaaS session cookie domain must be explicitly configured.');
assert.match(coolifyCompose, /TenantRouting__RootDomain:\s*envantertr\.com[\s\S]*?TenantRouting__CookieDomain:\s*\.envantertr\.com/, 'Coolify must configure tenant host resolution and shared session cookies.');
for (const testName of [
  'Login_company_choices_are_limited_to_the_accounts_active_memberships',
  'Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain',
  'Company_profile_rejects_reserved_and_duplicate_tenant_slugs_and_persists_a_valid_slug'
]) assert.ok(acceptance.includes(testName), `Missing tenant slug acceptance scenario: ${testName}`);

console.log('Verified SaaS slug/profile binding, explicit wrong-host rejection without valid-cookie erasure, invalid-session controls, shared-cookie configuration and acceptance coverage.');
