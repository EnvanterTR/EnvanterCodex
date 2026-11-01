import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const client = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-members.js', 'utf8');
const contract = await readFile('src/EnvanterTR.Web/Data/PlatformContracts.cs', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(contract, /PlatformInviteMemberRequest\(Guid CompanyId, string FullName, string Email, string RoleKey\)/);
const endpoint = api.match(/admin\.MapPost\("\/members\/invite"[\s\S]*?admin\.MapGet\("\/plans"/);
assert.ok(endpoint, 'platform member invitation endpoint exists');
assert.match(endpoint[0], /RequireRateLimiting\("login"\)/);
assert.match(endpoint[0], /FOR UPDATE/);
assert.match(endpoint[0], /company\.IsActive/);
assert.match(endpoint[0], /TenantSubscriptions/);
assert.match(endpoint[0], /subscription\.MaxLoginUsers is int seatLimit && memberCount >= seatLimit/,
  'Platform invitations must enforce the approved Cloud user quota.');
assert.doesNotMatch(endpoint[0], /DeploymentKind != "cloud"/,
  'Cloud invitations must not bypass the configured user quota.');
assert.match(endpoint[0], /MemberInvitationResent/);
assert.match(endpoint[0], /MemberInvited/);
assert.match(endpoint[0], /MemberInvitationEmailSent/);
assert.match(endpoint[0], /SendAccountInvitationEmailAsync/);
assert.match(endpoint[0], /SendCompanyAccessAddedEmailAsync/);
assert.doesNotMatch(endpoint[0], /PasswordHash\s*=|password\s*:\s*temporaryPassword/i);
assert.match(page, /id="openMemberInvite"/);
assert.match(page, /id="memberInviteDialog"/);
assert.match(page, /id="memberInviteCompanySearch"/);
assert.match(page, /name="roleKey"/);
assert.match(client, /api\('\/api\/session\/csrf'\)/);
assert.match(client, /api\('\/api\/platform\/members\/invite'/);
assert.match(client, /return window\.platformAdminApi\(url, options\)/, 'member invites must share the central API client for CSRF and step-up authentication');
assert.match(page, /window\.platformAdminApi=api/);
assert.match(page, /X-CSRF-TOKEN/);
assert.match(client, /loadInviteCompanies/);
assert.match(client, /memberInviteFeedback|invitationSent/);
assert.match(acceptance, /Platform_admin_can_invite_new_and_existing_members_with_company_scope_and_retryable_email/);
console.log('Platform member invitation source verification passed, including Cloud user quota enforcement.');
