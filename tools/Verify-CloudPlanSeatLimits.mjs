import { readFileSync } from 'node:fs';

const invitations = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const plans = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const admin = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const companyUsers = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const pricing = readFileSync('src/EnvanterTR.PublicSite/index.html', 'utf8');
const checkout = readFileSync('src/EnvanterTR.PublicSite/checkout.js', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const failures = [];

if (!invitations.includes('select new { item.Id, plan.DeploymentKind, plan.MaxLoginUsers }') ||
    !invitations.includes('subscription.MaxLoginUsers is int seatLimit && memberCount >= seatLimit')) {
  failures.push('Company invitations must enforce the configured login-user cap for Cloud and self-hosted plans.');
}
if ((plans.match(/x\.MaxLoginUsers,/g) ?? []).length < 2) {
  failures.push('Platform and public plan APIs must return the configured login-user limit.');
}
if (!plans.includes('if (request.MaxLoginUsers is <= 0 || request.MaxAssets is <= 0)') ||
    !plans.includes('plan.MaxLoginUsers = request.MaxLoginUsers;')) {
  failures.push('Plan writes must validate and preserve the configured user limit.');
}
if (!admin.includes('id="planMaxLoginUsers"') ||
    !admin.includes('Cloud ve yerel paketlerde isteğe bağlı giriş hesabı sınırı belirleyin.') ||
    !admin.includes('p.maxLoginUsers??\'∞\'')) {
  failures.push('Platform plan UI must allow operators to set and review the seat cap.');
}
if (companyUsers.includes('Bulut şirketlerinde kullanıcı sayısı sınırsızdır') ||
    !companyUsers.includes('Kullanıcı sayısı, geçerli planınızda tanımlı giriş hesabı sınırına göre yönetilir.')) {
  failures.push('Company user management must accurately explain the configured plan cap.');
}
if (!pricing.includes('plan.maxLoginUsers==null?labels[0]:`${plan.maxLoginUsers} ${labels[1]}`') ||
    !checkout.includes('formatPlanLimits(plan.maxLoginUsers, plan.maxAssets)')) {
  failures.push('Pricing and checkout must display user limits from the published plan data.');
}
if (!acceptance.includes('Cloud_company_invites_obey_plan_seat_caps_while_local_caps_remain_enforced') ||
    !acceptance.includes('Assert.Equal(4, cloudPlan.GetProperty("maxLoginUsers").GetInt32())') ||
    !acceptance.includes('4 giriş hesabıyla sınırlı')) {
  failures.push('PostgreSQL acceptance coverage must exercise Cloud seat caps and preserve local plan behavior.');
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: Cloud and self-hosted plans preserve seat caps across APIs, administration, invitations, pricing, and checkout.');
