import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const setupPage = await readFile('src/EnvanterTR.Web/wwwroot/platform-setup.html', 'utf8');
const program = await readFile('src/EnvanterTR.Web/Program.cs', 'utf8');
const account = await readFile('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const members = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-members.js', 'utf8');
const subscriptions = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-subscriptions.js', 'utf8');
const demos = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-demo-requests.js', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const browserAcceptance = await readFile('deploy/testing/hosted-checkout-browser/platform-admin-members.mjs', 'utf8');
const sessionAcceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/SessionDurationAcceptanceTests.cs', 'utf8');

assert.match(page, /name="rememberMe" type="checkbox"/);
assert.doesNotMatch(page, /name="rememberMe"[^>]*checked/);
assert.match(page, /rememberMe:f\.get\('rememberMe'\)==='on'/);
assert.match(account, /request\.RememberMe \? 720 : membership is null \? 8 : 24/);
assert.match(account, /ExpiresUtc = sessionRecord\.ExpiresAt/);
assert.match(sessionAcceptance, /Enabled_mfa_requires_a_code_on_each_fresh_device_even_when_remembered/);
assert.match(sessionAcceptance, /Login_cookie_and_server_expiry_match_platform_and_company_policy/);

assert.match(page, /<style id="platform-luxe-skin">/);
assert.match(page, /color-scheme:dark/);
assert.match(page, /\.nav button\.active[\s\S]*?box-shadow/);
assert.match(page, /\.card,\.panel[\s\S]*?linear-gradient/);
assert.match(page, /:where\(button,a,input,select,textarea\):focus-visible/);
assert.match(page, /id="globalSearch"[^>]*aria-keyshortcuts="Control\+K Meta\+K"/,
  'The platform administrator search must announce its Ctrl+K / Command+K shortcut.');
assert.match(page, /document\.addEventListener\('keydown',event=>\{if\(!\(event\.ctrlKey\|\|event\.metaKey\)\|\|event\.altKey\|\|event\.key\.toLowerCase\(\)!=='k'\|\|\$\('#appShell'\)\.hidden\)return;event\.preventDefault\(\);const input=\$\('#globalSearch'\);input\.focus\(\);input\.select\(\)\}\)/,
  'Ctrl+K / Command+K must focus and select the platform search without hijacking the login screen.');
assert.match(page, /async function searchPlatformGlobal\(query\)[\s\S]*?Promise\.all\(\[api\('\/api\/platform\/members\?'\+params\),api\('\/api\/platform\/companies\?'\+params\)\]\)[\s\S]*?exactMember[\s\S]*?\$\('#nav \[data-view="members"\]'\)\.click\(\)[\s\S]*?loadCompanies\(\)/,
  'Platform Ctrl+K search must resolve matching member accounts and companies to their actual result screens.');
assert.match(page, /let platformGlobalSearchSequence=0/);
assert.match(page, /if\(sequence!==platformGlobalSearchSequence\)return/,
  'Out-of-order global-search responses must not change the destination chosen by the latest query.');
assert.match(page, /if\(sequence===platformGlobalSearchSequence\)toast/,
  'Only the latest platform-search failure may be shown to the user.');
assert.match(page, /prefers-reduced-motion:reduce/);
assert.match(page, /@media\(max-width:950px\)/);
assert.match(page, /@media\(max-width:650px\)/);
assert.match(page, /\.login-wrap\{[^}]*overflow-y:auto;place-items:safe center/,
  'Platform login and MFA setup panels must remain scrollable on short viewports.');

for (const view of ['overview', 'companies', 'members', 'demo-requests', 'orders', 'plans', 'support', 'audit']) {
  assert.match(page, new RegExp(`data-view="${view}"`), `${view} navigation remains available`);
  assert.match(page, new RegExp(`id="view-${view}"`), `${view} view remains available`);
}
assert.match(page, /platform-admin-members\.js/);
assert.match(page, /platform-admin-subscriptions\.js/);
assert.match(page, /platform-admin-demo-requests\.js/);
assert.match(page, /id="exportCompanies"[^>]*aria-label="Filtrelenmiş şirket listesini CSV olarak indir"/,
  'The company directory must provide an accessible CSV export action.');
assert.match(page, /\$\('#exportCompanies'\)\.addEventListener\('click',[\s\S]*?companySearch[\s\S]*?companyActive[\s\S]*?\/api\/platform\/companies\/export\.csv/,
  'Company export must carry the active directory search and status filters.');
assert.match(page, /id="companyPlan"[\s\S]*?id="companyCreatedFrom"[\s\S]*?id="companyCreatedTo"[\s\S]*?id="companySort"[\s\S]*?id="companyPageSize"/,
  'Company list must expose plan/date/sort/page-size filters.');
assert.match(page, /async function loadCompanies\(\)[\s\S]*?planId[\s\S]*?createdFrom[\s\S]*?createdTo[\s\S]*?sortBy[\s\S]*?pageSize/,
  'Company filters must be sent to the pageable API.');
assert.match(page, /async function loadCompanyPlans\(\)[\s\S]*?\/api\/platform\/plans/,
  'Company plan filter options must load from the platform plan catalogue.');
assert.match(api, /admin\.MapGet\("\/companies\/export\.csv"[\s\S]*?RequireAuthorization\("platform\.admin"\)|MapGroup\("\/api\/platform"\)\.RequireAuthorization\("platform\.admin"\)[\s\S]*?admin\.MapGet\("\/companies\/export\.csv"/,
  'Company CSV must be restricted to platform administrators.');
assert.match(api, /companies\/export\.csv[\s\S]*?AsAsyncEnumerable\(\)[\s\S]*?context\.Response\.Body[\s\S]*?UTF8Encoding\(encoderShouldEmitUTF8Identifier: true\)[\s\S]*?CsvCell\(row\.Name\)/,
  'Company CSV must stream the filtered company data with spreadsheet-safe CSV cells.');
assert.match(api, /MapPatch\("\/companies\/bulk-status"[\s\S]*?Distinct\(\)\.Count\(\)[\s\S]*?BeginTransactionAsync[\s\S]*?AddAudit[\s\S]*?CompanyReactivated/,
  'Bulk company status must validate the full selection and atomically record each tenant status change.');
assert.match(page, /data-company-select-all[\s\S]*?data-company-select=[\s\S]*?bulkSuspendCompanies[\s\S]*?bulkReactivateCompanies[\s\S]*?\/api\/platform\/companies\/bulk-status/,
  'Company list must select visible companies and send a reasoned bulk status action.');
assert.match(page, /bulkChangeCompanyPlan[\s\S]*?ödeme almaz[\s\S]*?ödem[e]? kaydını değiştirmez[\s\S]*?\/api\/platform\/companies\/bulk-plan/,
  'Company list must provide a reasoned bulk Cloud plan change and explain that it does not process billing.');
assert.match(api, /admin\.MapPatch\("\/companies\/bulk-plan"[\s\S]*?IsolationLevel\.Serializable[\s\S]*?candidate\.DeploymentKind == "cloud"[\s\S]*?StartsAt <= now[\s\S]*?EndsAt > now[\s\S]*?SubscriptionBulkPlanChanged[\s\S]*?billingChanged = false/,
  'Bulk plan changes must be atomic, require a current eligible Cloud subscription, keep billing separate, and write an audit event.');
assert.match(api, /var activeAssetCount = await db\.Assets[\s\S]*?var employeeCount = await db\.Employees[\s\S]*?var licenseCount = await db\.SoftwareLicenses[\s\S]*?var activeApiTokenCount = await db\.CompanyApiTokens[\s\S]*?recordedAttachmentBytes[\s\S]*?maxLoginUsers/,
  'Company detail usage must be derived from tenant-scoped live records and active plan limits.');
assert.match(page, /id="companyUsagePanel"[\s\S]*?Kullanım ve plan sınırları[\s\S]*?Kayıtlı ek dosya[\s\S]*?storageNote/,
  'Company details must show resource usage and disclose the limits of recorded attachment storage.');
assert.ok(browserAcceptance.includes('Cloud login-user usage must be shown against the configured annual package quota.')
  && browserAcceptance.includes('maxLoginUsers: 4')
  && /companyUsagePanel[\s\S]*?şirket logoları ve medya dizinindeki dosyalar dahil değildir/i.test(browserAcceptance),
  'Hosted Chromium must verify the configured Cloud user quota, attachment bytes and storage exclusions in company details.');
assert.match(acceptance, /Platform_company_member_details_are_pageable_beyond_one_hundred_and_include_account_state[\s\S]*?activeApiTokens[\s\S]*?recordedAttachmentBytes/,
  'The isolated PostgreSQL company-detail acceptance must verify usage counts, plan limits and tenant-scoped recorded file sizes.');
assert.match(api, /MapPost\("\/members\/\{userId:guid\}\/security-actions"[\s\S]*?request\.LockoutDays[\s\S]*?otherUnlockedPlatformAdmins[\s\S]*?SetLockoutEndDateAsync[\s\S]*?RevokedAt[\s\S]*?GeneratePasswordResetTokenAsync[\s\S]*?SendPasswordResetEmailAsync[\s\S]*?MemberSecurityAction/,
  'Platform account security must lock/unlock accounts, revoke sessions, send reset links only through verified email and audit each action.');
assert.match(members, /Hesap güvenliği[\s\S]*?data-member-security="lock"[\s\S]*?data-member-security="revokeSessions"[\s\S]*?data-member-security="sendPasswordReset"[\s\S]*?openMemberSecurityAction[\s\S]*?X-CSRF-TOKEN/,
  'Member detail must expose all safe, reasoned and CSRF-protected account security operations.');
assert.match(browserAcceptance, /Canceling a security action must not write[\s\S]*?Synthetic suspicious sign-in response[\s\S]*?Synthetic identity verified[\s\S]*?Synthetic lost device report[\s\S]*?Synthetic user-requested recovery[\s\S]*?Each account-security action must carry the CSRF token/,
  'Hosted browser acceptance must cover cancel safety and lock, unlock, revoke-session and reset-email actions.');
assert.match(page, /Doğrulanmış MRR[\s\S]*?data-company-logo[\s\S]*?verifiedMonthlyRecurringRevenue/,
  'The company list must show logo, registration date, and only verified recurring revenue.');
assert.match(api, /admin\.MapGet\("\/companies\/\{companyId:guid\}\/logo"[\s\S]*?BrandLogoPath[\s\S]*?Regex\.IsMatch[\s\S]*?RequireRateLimiting\("login"\)/,
  'Company logos must be read from the company-scoped media path and protected against unsafe filenames.');
assert.match(api, /LoadVerifiedRecurringRevenueAsync[\s\S]*?order\.Status == "Provisioned"[\s\S]*?payment\.PaidAt == payment\.StartsAt[\s\S]*?payment\.EndsAt == paidAt\.AddDays\(payment\.TermDays\)/,
  'Company MRR must be tied to a verified provisioned payment and the exact original paid term.');
assert.ok(setupPage.includes("request('/api/platform/bootstrap/status')"));
assert.match(setupPage, /setupRequired/);
assert.match(setupPage, /setupTokenConfigured/);
assert.ok(setupPage.includes('autocomplete="current-password"'));
assert.match(setupPage, /mevcut parolasını girin; yeni hesapta burada girdiğiniz parola oluşturulur/);
assert.match(page, /id="platformTwoFactorField" hidden/);
assert.match(page, /\[hidden\]\s*\{\s*display\s*:\s*none\s*!important\s*;?\s*\}/, 'hidden platform admin fields must stay hidden despite display rules on their classes');
assert.match(page, /id="platformLoginReadiness"[^>]*role="status"/);
assert.match(page, /id="platformCheckConnection"[^>]*type="button">Sunucu bağlantısını test et<\/button>/,
  'Platform login must keep a manual, password-free server/DB readiness check.');
assert.match(page, /async function checkPlatformLoginReadiness\(manual=false\)[\s\S]*?cache:'no-store'[\s\S]*?migration-required[\s\S]*?pendingMigrationCount[\s\S]*?manual[\s\S]*?migrationCount[\s\S]*?Sunucu bağlantısı doğrulanamadı/,
  'Readiness diagnostics must distinguish healthy, pending-migration, and unavailable states without submitting credentials.');
assert.match(page, /platformReadinessCheckSequence[\s\S]*?sequence!==platformReadinessCheckSequence[\s\S]*?message\.dataset\.manualResult!=='success'/,
  'An older automatic readiness response must not erase the latest manual result.');
assert.match(page, /\$\('#platformCheckConnection'\)\.addEventListener\('click',[\s\S]*?checkPlatformLoginReadiness\(true\)/,
  'The manual connection check must be wired to the same safe readiness routine.');
assert.match(page, /id="platformLoginSubmit"/);
assert.match(page, /async function checkPlatformLoginReadiness\(manual=false\)/);
assert.match(page, /id="platformBootstrapLink"[^>]*hidden/, 'First-admin bootstrap must not be exposed by default.');
assert.match(page, /id="platformBootstrapReadiness"[^>]*role="status"/, 'Platform login must explain why first-admin setup is unavailable.');
assert.match(page, /async function checkPlatformBootstrapAvailability\(\)[\s\S]*?status\.setupRequired!==true[\s\S]*?status\.setupTokenConfigured===true[\s\S]*?link\.hidden=false[\s\S]*?SETUP_PLATFORM_BOOTSTRAP_TOKEN/, 'Only a configured first setup may expose the bootstrap link; missing setup configuration must be explained.');
assert.match(page, /checkPlatformBootstrapAvailability\(\)/);
assert.match(page, /status\.status==='migration-required'[\s\S]*?pendingMigrationCount[\s\S]*?submit\.disabled=true/);
assert.match(page, /status\.status==='ready'[\s\S]*?submit\.disabled=false/);
assert.match(page, /if\(!await checkPlatformLoginReadiness\(\)\)return/);
assert.match(page, /name="twoFactorCode"[^>]*autocomplete="one-time-code"/);
assert.match(page, /id="platformTwoFactorHint"[^>]*>[^<]*Google Authenticator, Microsoft Authenticator ya da uyumlu uygulamanın[\s\S]*?EnvanterTR için Google\/Microsoft üyeliği ya da ücretli doğrulama hizmeti gerekmez/,
  'The platform login must explain that an already-enrolled authenticator app produces the code and needs no paid Google/Microsoft integration.');
assert.match(page, /<script src="\/vendor\/qrcodegen\.js"><\/script>/,
  'Platform MFA must use the locally hosted QR encoder, not an external image service.');
assert.match(page, /Google Authenticator, Microsoft Authenticator veya uyumlu uygulamada[\s\S]*?yerel olarak üretilir; Google\/Microsoft hesabı veya ücretli doğrulama hizmeti gerekmez/,
  'First-time MFA setup must explain compatible apps, local QR generation, and no third-party account or paid provider requirement.');
assert.match(page, /function renderPlatformTotpQr\(authenticatorUri\)[\s\S]*?startsWith\('otpauth:\/\/totp\/'\)[\s\S]*?qrcodegen\.QrCode\.encodeText\(authenticatorUri/,
  'The authenticator URI must be validated and encoded locally.');
assert.match(page, /renderPlatformTotpQr\(result\.authenticatorUri\)/,
  'The UI must generate the QR from the server-provided authenticator URI.');
assert.match(page, /id="platformMfaQr" alt="Authenticator uygulaması için güvenlik kurulum QR kodu"/);
assert.match(page, /QR kodunu tarayamıyorsanız aşağıdaki kurulum anahtarını uygulamaya elle girebilirsiniz/,
  'Manual setup-key entry must remain available as a fallback.');
assert.match(page, /twoFactorCode:f\.get\('twoFactorCode'\)\|\|null/);
assert.match(page, /platformAdminOnly:true/, 'Platform login must request an explicitly scoped platform session.');
assert.match(page, /if\(x\.twoFactorRequired\)[\s\S]*?platformTwoFactorField\.hidden=false/);
assert.match(page, /error\.twoFactorRequired=data\?\.twoFactorRequired===true/);
assert.match(account, /request\.PlatformAdminOnly && \(tenantSlug is not null \|\| !isPlatformAdmin \|\| request\.CompanyId is not null\)/,
  'Platform-only login must require a platform administrator and reject company-scoped hosts/IDs.');
assert.match(account, /request\.PlatformAdminOnly\s*\?\s*new List<CompanyMembership>\(\)/,
  'Platform-only login must not require choosing among company memberships.');
assert.match(account, /!request\.PlatformAdminOnly && tenantCompanyId is null && memberships\.Count > 1/,
  'Company login must preserve the company chooser for multi-company accounts.');
assert.match(program, /isPlatformAdmin && context\.Request\.Path\.StartsWithSegments\("\/api\/platform"\)[\s\S]*?platformMfaEnabled[\s\S]*?Status428PreconditionRequired[\s\S]*?platform_mfa_required/);
assert.match(page, /id="platformReauthDialog"/);
assert.match(page, /abonelik gibi kritik platform değişiklikleri/);
assert.match(page, /async function api\([\s\S]*?platform_reauth_required[\s\S]*?requestPlatformReauthentication\(\)[\s\S]*?_skipReauth:true/);
assert.ok(page.includes("function requestPlatformReauthentication()") && page.includes("Kritik işlem iptal edildi; hiçbir değişiklik uygulanmadı."), 'step-up prompt supports reauthentication and safe cancellation');
assert.match(program, /isPlatformAdmin && isPlatformEndpoint && isWriteRequest[\s\S]*?platform_reauth_at[\s\S]*?nowUnix - 600[\s\S]*?Status428PreconditionRequired[\s\S]*?platform_reauth_required/);
const rateLimits = await readFile('src/EnvanterTR.Web/Data/ClientRateLimits.cs', 'utf8');
assert.ok(program.includes('AddRateLimiter(ClientRateLimits.Configure)'));
assert.match(rateLimits, /AddPolicy\("step-up"[\s\S]*?PermitLimit = 5[\s\S]*?TimeSpan\.FromMinutes\(5\)/);
assert.ok(!rateLimits.includes('X-Forwarded-For'), 'Named policies must use the middleware-verified connection address');
assert.match(program, /"platform_reauth_at"/);
assert.match(account, /MapPost\("\/api\/session\/platform-reauth"[\s\S]*?CheckPasswordAsync[\s\S]*?VerifyTwoFactorTokenAsync[\s\S]*?PlatformReauthenticated[\s\S]*?RequireRateLimiting\("step-up"\)/);
assert.match(account, /platform_reauth_at/);
assert.match(page, /id="platformMfaSetupWrap"/);
assert.match(acceptance, /Platform_admin_first_login_accepts_password_then_requires_mfa_setup_before_panel/);
assert.match(page, /id="platformMfaTitle">İlk giriş güvenlik kurulumu/);
assert.match(page, /Google Authenticator, Microsoft Authenticator/);
assert.match(page, /Mevcut parolanızı girip kurulumu başlatın/);
assert.match(page, /QR kodunu tarayamıyorsanız aşağıdaki kurulum anahtarını uygulamaya elle girebilirsiniz/);
assert.match(page, /Tek kullanımlık kurtarma kodlarını güvenli bir yere kaydedin/);
assert.match(page, /\/api\/session\/2fa\/setup/);
assert.match(page, /\/api\/session\/2fa\/enable/);
assert.match(page, /platformMfaRecoveryCodes/);
assert.match(page, /platformMfaSavedRecovery/);
assert.match(page, /data\?\.code==='platform_mfa_required'[\s\S]*?showPlatformMfaSetup/);
assert.match(page, /async function showApp\(name\)[\s\S]*?\/api\/session\/2fa[\s\S]*?if\(!security\.enabled\)\{showPlatformMfaSetup\(\);return\}/);
assert.match(page, /if\(!s\.platformAdmin\)throw Error\('Bu hesapta platform yöneticisi yetkisi yok\.'\);await csrf\(\);await showApp\(s\.name\)/,
  'Refresh the antiforgery token after login because the authenticated identity changed.');
assert.match(page, /platformMfaSetupForm'\)\.addEventListener\('submit',async e=>\{e\.preventDefault\(\);const form=e\.currentTarget[\s\S]*?await api\('\/api\/session\/2fa\/setup'[\s\S]*?form\.elements\.currentPassword\.value=''/,
  'Keep a stable form reference across the asynchronous MFA setup request.');
assert.match(members, /loadMemberTimeline\(id, 1, false\)/);
assert.match(api, /PlatformDemoRequests/);
assert.match(api, /Satın alma kaydı oluşturuldu/);
assert.match(api, /Ödeme doğrulandı/);
assert.match(api, /checkoutOrders, lifecycleHistory/);
assert.match(api, /order\.PaidAt/);
assert.match(page, /Satın alma ve ödeme geçmişi/);
assert.match(page, /o\.paidAt/);
assert.match(page, /o\.amount,o\.currency/);
assert.match(api, /\[FromQuery\] int membersPage = 1, \[FromQuery\] int membersPageSize = 50/);
assert.match(api, /Skip\(\(membersPage - 1\) \* membersPageSize\)\.Take\(membersPageSize\)/);
assert.match(api, /account\.AccountCreatedAt[\s\S]*?account\.EmailConfirmed[\s\S]*?account\.TwoFactorEnabled/);
assert.match(api, /membersTotalPages, membersTruncated/);
assert.match(page, /membersPage=\$\{encodeURIComponent\(requestedMembersPage\)\}&membersPageSize=50/);
assert.match(page, /data-members-page=/);
assert.match(page, /Hesap açılışı/);
assert.match(page, /Şirkete katılım/);
assert.match(page, /E-posta/);
assert.match(page, />MFA<|MFA/);
for (const alert of ['paymentPending', 'paymentReview', 'paymentFailed', 'provisioningFailed']) {
  assert.match(api, new RegExp(`var ${alert} = await db\\.CompanyCheckoutOrders[\\s\\S]*?Status ==`), `${alert} is counted from checkout state`);
  assert.match(page, new RegExp(`${alert}`), `${alert} is visible on the platform overview`);
}
assert.match(api, /checkout = new \{ paymentPending, paymentReview, paymentFailed, provisioningFailed \}/);
assert.match(api, /MapGet\("\/system\/health"[\s\S]*?Process\.GetCurrentProcess\(\)[\s\S]*?TotalProcessorTime[\s\S]*?pg_database_size\(current_database\(\)\)[\s\S]*?application-container-filesystem/,
  'Platform health exposes measured application-process and PostgreSQL metrics with explicit container scope.');
assert.match(api, /smtpConfigured = !string\.IsNullOrWhiteSpace\(configuration\["Email:Smtp:Host"\]\)[\s\S]*?iyzicoConfigured/,
  'Health reports integration configuration without exposing secret values.');
assert.match(page, /id="systemHealthPanel"[\s\S]*?id="refreshSystemHealth"[\s\S]*?id="systemHealthCards"/);
assert.match(page, /async function loadSystemHealth\(\)[\s\S]*?api\('\/api\/platform\/system\/health'\)[\s\S]*?systemHealthRequestId/);
assert.match(page, /setInterval\(\(\)=>\{if\(!\$\('#appShell'\)\.hidden&&\$\('#view-overview'\)\.classList\.contains\('active'\)\)void loadSystemHealth\(\)\},30000\)/,
  'System health refreshes every 30 seconds only while the platform overview is visible.');
assert.match(acceptance, /Platform_system_health_is_admin_only_and_reports_scoped_runtime_metrics[\s\S]*?HttpStatusCode\.Forbidden[\s\S]*?cpuSampleMilliseconds[\s\S]*?application-container-filesystem/);
assert.match(browserAcceptance, /systemHealthReads[\s\S]*?\/api\/platform\/system\/health[\s\S]*?7,5%[\s\S]*?refreshSystemHealth[\s\S]*?manualHealthRefresh[\s\S]*?systemHealthReads > healthReadsBeforeManualRefresh/);
assert.match(api, /activeSince = now\.AddMinutes\(-5\)[\s\S]*?activeUserCount = await activeSessionsQuery\.Select\(x => x\.UserId\)\.Distinct\(\)\.CountAsync[\s\S]*?loginSince = now\.AddHours\(-24\)[\s\S]*?recentLoginQuery\.Select\(x => x\.UserId\)\.Distinct\(\)\.CountAsync[\s\S]*?session\.IpAddress[\s\S]*?\.Take\(10\)\.ToListAsync/,
  'Platform overview reports distinct active users, 24-hour logins, and only the ten newest session rows from stored sessions.');
assert.match(page, /id="recentLoginTable"[\s\S]*?function renderRecentLogins\([\s\S]*?IP[\s\S]*?data-recent-login-email[\s\S]*?Aktif kullanıcı · 5 dk[\s\S]*?Giriş yapan hesap · 24 sa/);
assert.match(api, /orderby session\.CreatedAt descending, session\.Id descending/,
  'Recent sessions use a deterministic identifier tie-breaker for equal timestamps.');
assert.match(acceptance, /Platform_overview_reports_active_users_24_hour_logins_and_last_ten_sessions[\s\S]*?Enumerable\.Range\(0, 6\)[\s\S]*?sessionSnapshot[\s\S]*?GetProperty\("generatedAt"\)[\s\S]*?expectedActive[\s\S]*?expectedLogins[\s\S]*?expectedRecent[\s\S]*?Assert\.Equal\(10, recentLogins\.Length\)/,
  'Session metrics acceptance includes newer competing sessions, evaluates windows at the API timestamp, and checks all ten global results.');
assert.match(acceptance, /ThenByDescending\(session => session\.Id\.ToString\("D"\), StringComparer\.Ordinal\)[\s\S]*?loginUsers\[expected\.UserId\]\.Email[\s\S]*?expected\.PlatformAdmin[\s\S]*?expected\.IpAddress[\s\S]*?expected\.CreatedAt[\s\S]*?expected\.RevokedAt/,
  'Every returned recent-session projection is checked against the independent stored-session snapshot.');
assert.match(browserAcceptance, /activeUsers: 3[\s\S]*?qa-owner@example\.invalid[\s\S]*?192\.0\.2\.10[\s\S]*?Hesabı aç[\s\S]*?memberSearch/,
  'Browser acceptance verifies rendered session metrics, IP, and opening an account from the recent-login table.');
assert.match(api, /var recentPayments = await \(from order in db\.CompanyCheckoutOrders\.AsNoTracking\(\)[\s\S]*?join plan in db\.SubscriptionPlans\.AsNoTracking\(\)[\s\S]*?orderby \(order\.PaidAt \?\? order\.CreatedAt\) descending[\s\S]*?order\.Status[\s\S]*?\.Take\(10\)\.ToListAsync/,
  'Platform overview limits payment activity to the latest ten checkout records, ordered by payment time or creation time.');
assert.match(api, /select new\s*\{[\s\S]*?order\.Amount[\s\S]*?order\.Currency[\s\S]*?order\.Status[\s\S]*?activityAt = order\.PaidAt \?\? order\.CreatedAt[\s\S]*?\}\)\.Take\(10\)[\s\S]*?recentPayments,/,
  'Recent payment projection exposes display fields only.');
assert.match(page, /id="recentPaymentsPanel"[\s\S]*?id="refreshRecentPayments"[\s\S]*?data-go="orders"[\s\S]*?id="recentPaymentsTable"/);
assert.match(page, /function renderRecentPayments\([\s\S]*?orderStatusLabels\[x\.status\][\s\S]*?recentPaymentsTable/);
assert.match(acceptance, /recentPayments = payload\.RootElement\.GetProperty\("recentPayments"\)[\s\S]*?Assert\.Equal\(10, recentPayments\.Length\)[\s\S]*?TryGetProperty\("checkoutTokenHash"/);
assert.match(acceptance, /Assert\.DoesNotContain\("dashboard-secret-token", responseBody[\s\S]*?Assert\.DoesNotContain\("dashboard-protected-checkout", responseBody/,
  'PostgreSQL acceptance verifies recent payment ordering, maximum row count and no checkout secret exposure.');
assert.match(browserAcceptance, /recentPayments: \[[\s\S]*?Synthetic Paid Company[\s\S]*?Synthetic Failed Company[\s\S]*?recentPaymentsPanel \[data-go="orders"\][\s\S]*?view-orders/,
  'Chromium acceptance verifies paid and failed payment rows and the orders navigation shortcut.');
assert.match(page, /Ödeme bekliyor[\s\S]*?Ödeme incelemesi[\s\S]*?Başarısız ödeme[\s\S]*?Firma açılışı takıldı/);
assert.match(api, /var verifiedRecurringPayments = await[\s\S]*?order\.Status == "Provisioned"[\s\S]*?order\.PaymentReference == subscription\.PaymentReference/);
assert.match(api, /x\.PaidAt == x\.StartsAt[\s\S]*?x\.EndsAt == paidAt\.AddDays\(x\.TermDays\)/);
assert.match(api, /group\.Sum\(x => x\.TermDays == 30 \? x\.Amount : x\.Amount \/ 12m\)/);
assert.match(api, /basis = "provider-verified-current-cloud-subscriptions"/);
assert.match(page, /MRR · \$\{item\.currency\}/);
assert.match(page, /Doğrulanmış MRR/);
assert.match(acceptance, /var baselineCurrencies = baselinePayload\.RootElement\.GetProperty\("recurringRevenue"\)\.GetProperty\("currencies"\)/,
  'Revenue acceptance must measure increments against the isolated database baseline.');
assert.match(acceptance, /Assert\.Equal\(RevenueMetric\(baselineCurrencies, "TRY", "monthlyRecurringRevenue"\) \+ 1200m,[\s\S]*?RevenueMetric\(currencies, "TRY", "monthlyRecurringRevenue"\)\)/,
  'Verified TRY revenue must increase by the expected amount without assuming a clean shared acceptance database.');
assert.match(acceptance, /Assert\.Equal\(RevenueMetric\(baselineCurrencies, "USD", "monthlyRecurringRevenue"\) \+ 2000m,[\s\S]*?RevenueMetric\(currencies, "USD", "monthlyRecurringRevenue"\)\)/,
  'Verified USD revenue must increase by the expected amount without assuming a clean shared acceptance database.');
assert.doesNotMatch(api.match(/admin\.MapGet\("\/companies\/\{companyId:guid\}"[\s\S]*?admin\.MapPatch\("\/companies\/\{companyId:guid\}\/lifecycle"/)?.[0] || '', /CheckoutTokenHash|CheckoutPageUrlProtected/);
assert.match(subscriptions, /Plan \/ tarih düzenle/);
assert.match(subscriptions, /method: 'PUT'/);
assert.match(subscriptions, /Zorunlu: ücretsiz uzatma \/ plan değişikliği gerekçesi/);
assert.match(demos, /api\/platform\/demo-requests/);
assert.match(page, /action==='toggle'[\s\S]*?openAction\(next\?'Firma erişimini yeniden aç':'Firma askıya al',id,'active'/);
assert.doesNotMatch(page, /if\(!confirm\(next\?'Firma erişimi yeniden açılsın mı\?':'Firma askıya alınsın mı\?'\)\)/);
assert.match(page, /if\(type==='active'\)[\s\S]*?name="note" minlength="8" maxlength="1000" rows="3" required/);
assert.match(page, /if\(a\.type==='active'\)await api\(`\/api\/platform\/companies\/\$\{a\.id\}\/active`/);
assert.match(api, /company\.IsActive == request\.IsActive[\s\S]*?unchanged = true[\s\S]*?request\.Note\.Trim\(\)\.Length < 8/);

console.log('Platform admin premium skin, pageable member/account details, reasoned idempotent company status workflow, demo/purchase/subscription workflows, responsive/accessibility affordances, and two-factor sign-in verified.');
