import { readFileSync } from 'node:fs';

const endpoint = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const plans = readFileSync('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const admin = readFileSync('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const app = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const publicSite = readFileSync('src/EnvanterTR.PublicSite/index.html', 'utf8');
const acceptance = readFileSync('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');
const failures = [];

if (!endpoint.includes('select new { item.Id, plan.DeploymentKind, plan.MaxLoginUsers }') ||
    !endpoint.includes('subscription.DeploymentKind != "cloud" && subscription.MaxLoginUsers is int seatLimit')) {
  failures.push('Company invitations must ignore a login-user cap for Cloud plans while preserving caps for local plans.');
}
if ((plans.match(/MaxLoginUsers = x\.DeploymentKind == "cloud" \? \(int\?\)null : x\.MaxLoginUsers/g) ?? []).length !== 2) {
  failures.push('Public and platform plan APIs must expose Cloud login-user limits as unlimited, including legacy plan rows.');
}
if (!plans.includes('request.DeploymentKind == "cloud" && request.MaxLoginUsers is not null') ||
    !plans.includes('request.DeploymentKind == "cloud" ? null : request.MaxLoginUsers')) {
  failures.push('Plan writes must reject finite Cloud seat limits and persist Cloud plans as unlimited.');
}
if (!admin.includes('id="planMaxLoginUsers"') || !admin.includes('id="planUserLimitHint"') ||
    !admin.includes('planUserLimit.disabled=cloud') || !admin.includes("if(d.deploymentKind==='cloud')d.maxLoginUsers=null") ||
    !admin.includes("p.deploymentKind==='cloud'?'Sınırsız kullanıcı'")) {
  failures.push('Platform plan UI must communicate, enforce, and display unlimited Cloud users.');
}
if (!app.includes('Bulut şirketlerinde kullanıcı sayısı sınırsızdır; yerel lisanslarda tanımlı sınır uygulanabilir.') ||
    app.includes('Planın giriş hesabı sınırı uygulanır.')) {
  failures.push('Company user management must not imply a login-user cap on Cloud subscriptions.');
}
if (!publicSite.includes("const cloudPlans=publishedPlans.filter(item=>item.deploymentKind==='cloud');") ||
    !publicSite.includes('const tiers=cloudPlans.length?cloudPlans:') ||
    !publicSite.includes('tiers.forEach((plan,index)=>') ||
    !publicSite.includes('const name=plan?.name||copy.tierNames[index]') ||
    !publicSite.includes('Sınırsız kullanıcı ve envanter kaydı') ||
    /(?:50–200|201–500|501–2[., ]?000)\s*(?:users|kullanıcı|пользователей|користувачів|корисници)/i.test(publicSite) ||
    /Cloud user packages|Cloud kullanıcı paketleri|Пакеты Cloud по числу пользователей|Пакети Cloud за кількістю користувачів/i.test(publicSite)) {
  failures.push('Public pricing must select published Cloud plans without user-cap matching and must not advertise finite Cloud user bands.');
}
if (!acceptance.includes('Cloud_company_invites_are_unlimited_even_for_legacy_capped_plans_while_local_caps_remain_enforced') ||
    !acceptance.includes('Assert.Equal(JsonValueKind.Null, cloudPlan.GetProperty("maxLoginUsers").ValueKind)')) {
  failures.push('PostgreSQL acceptance coverage must exercise legacy Cloud plan invites, public quota output, and local cap enforcement.');
}

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: Cloud user limits are unlimited in APIs, plan administration, and company invitations; local limits remain enforced.');
