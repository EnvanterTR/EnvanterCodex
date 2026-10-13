import { readFileSync } from 'node:fs';

const endpoint = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const mail = readFileSync('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const app = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const docs = readFileSync('docs/DATA-API.md', 'utf8');
const failures = [];

if (!endpoint.includes('var existingUser = await userManager.FindByEmailAsync(email)') ||
    !endpoint.includes('var invitedUser = existingUser;') ||
    !endpoint.includes('CompanyId = companyId, UserId = invitedUser.Id, RoleKey = roleKey') ||
    endpoint.includes('Bu e-posta için davet açılamadı. E-posta adresini doğrulayın')) {
  failures.push('A target-company manager must be able to add an existing account as a distinct company membership.');
}
if (!endpoint.includes('SendCompanyAccessAddedEmailAsync(email, invitedUser.DisplayName ?? name') ||
    !mail.includes('SendCompanyAccessAddedEmailAsync') ||
    !mail.includes('Mevcut EnvanterTR hesabınızla giriş yapıp bu şirketi seçebilirsiniz.')) {
  failures.push('An existing confirmed account must receive an access notice without a password-reset token.');
}
if (!app.includes('Mevcut EnvanterTR hesabı varsa bu şirkete ayrı rolüyle eklenir; diğer şirketlerdeki üyeliği ve rolü değişmez.') ||
    app.includes('Aynı e-posta başka bir şirkette kayıtlıysa hesaplar güvenlik için birleştirilmez.')) {
  failures.push('The invitation dialog must describe cross-company account reuse and per-company role separation accurately.');
}
if (!acceptance.includes('Existing_account_can_join_an_additional_company_with_its_own_role_and_access_notice') ||
    !acceptance.includes('Assert.Equal("viewer", selectedSession.RootElement.GetProperty("role").GetString())')) {
  failures.push('PostgreSQL acceptance coverage must prove a second membership, independent role, access notice, and company chooser login.');
}
if (!docs.includes('Aynı doğrulanmış hesap başka bir şirkette üye olsa da') ||
    !docs.includes('her istek sadece seçili şirket kapsamıyla çalışır')) {
  failures.push('The API contract must document cross-company membership and tenant isolation.');
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: Existing EnvanterTR accounts can be invited to another company with an isolated role and tested company selection.');
