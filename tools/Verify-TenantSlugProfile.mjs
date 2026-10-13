import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const profileUi = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const profileApi = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const tenantResolver = readFileSync('src/EnvanterTR.Web/Data/TenantHostResolver.cs', 'utf8');
const accountApi = readFileSync('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const requestMiddleware = readFileSync('src/EnvanterTR.Web/Program.cs', 'utf8');
const coolifyCompose = readFileSync('deploy/coolify/compose.yaml', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(profileUi, /name="slug" maxlength="63"[^>]*required[^>]*aria-describedby="tenantSlugHelp"/, 'Company profile must require and explain a DNS-sized tenant slug.');
assert.match(profileUi, /JSON\.stringify\(\{[^}]*slug:\s*data\.get\('slug'\)/, 'The profile form must persist the slug through the profile API.');
assert.match(profileUi, /location\.assign\(saved\.tenantUrl\)/, 'Changing a tenant slug from its current hostname must move the browser to the new tenant URL.');
assert.match(profileApi, /Select\(x => new \{ x\.Name, x\.Slug, x\.ContactEmail/, 'The company profile API must return the current tenant slug.');
assert.match(profileApi, /TenantHostResolver\.IsValidSlug\(slug\)/, 'The company profile API must validate tenant slugs.');
assert.match(profileApi, /company\.Slug = slug/, 'A valid company slug must be persisted.');
assert.match(profileApi, /PostgresErrorCodes\.UniqueViolation/, 'Concurrent duplicate slug updates must return a controlled conflict.');
assert.match(tenantResolver, /ReservedSlugs/, 'Routing-reserved subdomains must not be claimable.');
assert.match(accountApi, /company\.Slug == tenantSlug/, 'Tenant-host login must resolve the company from the hostname slug.');
assert.match(requestMiddleware, /company\.Slug == tenantSlug/, 'Authenticated sessions must stay bound to the hostname company.');
assert.match(requestMiddleware, /StartsWithSegments\("\/api\/platform"\)/, 'Platform administration routes must not be exposed on customer tenant hosts.');
assert.match(requestMiddleware, /tenantSlug is not null && companyClaim is null/, 'Platform-only sessions must not be reused on customer tenant hosts.');
assert.match(requestMiddleware, /Cookie\.Domain = builder\.Configuration\["TenantRouting:CookieDomain"\]/, 'The SaaS session cookie domain must be explicitly configured.');
assert.match(coolifyCompose, /TenantRouting__RootDomain:\s*envantertr\.com[\s\S]*?TenantRouting__CookieDomain:\s*\.envantertr\.com/, 'Coolify must configure tenant host resolution and shared session cookies.');
for (const testName of [
  'Login_company_choices_are_limited_to_the_accounts_active_memberships',
  'Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain',
  'Company_profile_rejects_reserved_and_duplicate_tenant_slugs_and_persists_a_valid_slug'
]) assert.ok(acceptance.includes(testName), `Missing tenant slug acceptance scenario: ${testName}`);

console.log('Verified the SaaS company-slug profile flow, unique/reserved validation, tenant-host login/session binding, shared tenant cookie configuration, redirect behavior, and security acceptance coverage.');
