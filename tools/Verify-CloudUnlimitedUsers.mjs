import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
const read=path=>readFileSync(path,'utf8');
const plans=read('src/EnvanterTR.Web/Data/PlatformEndpoints.cs');
const members=read('src/EnvanterTR.Web/Data/OperationalEndpoints.cs');
const admin=read('src/EnvanterTR.Web/wwwroot/platform-admin.html');
const publicSite=read('src/EnvanterTR.PublicSite/index.html');
assert.ok(!plans.includes('MaxLoginUsers = x.DeploymentKind == "cloud"'));
assert.ok(plans.includes('plan.MaxLoginUsers = request.MaxLoginUsers;'));
assert.ok(!plans.includes('request.DeploymentKind == "cloud" && request.MaxLoginUsers is not null'));
for(const source of [plans,members]){
  assert.ok(source.includes('subscription.MaxLoginUsers is int seatLimit && memberCount >= seatLimit'));
  assert.ok(!source.includes('subscription.DeploymentKind != "cloud" && subscription.MaxLoginUsers'));
}
assert.ok(admin.includes('planUserLimit.disabled=false'));
assert.ok(!admin.includes("if(d.deploymentKind==='cloud')d.maxLoginUsers=null"));
assert.ok(publicSite.includes("plan.maxLoginUsers??'∞'"));
assert.ok(read('tests/EnvanterTR.SecurityAcceptance/CloudSeatQuotaAcceptanceTests.cs').includes('Cloud_plan_user_quota_is_public_and_enforced_without_removing_existing_members'));
const catalog=JSON.parse(read('docs/COMMERCIAL-CATALOG-20261021.json'));
assert.equal(catalog.cloud.trialDays,15);
assert.equal(catalog.cloud.monthlyCollection,false);
assert.equal(catalog.cloud.taxTreatment,'exclusive');
assert.equal(catalog.cloud.customQuoteAboveUsers,2000);
assert.deepEqual(catalog.cloud.plans.map(p=>p.users),[1,4,10,50,100,250,1000,2000]);
for(const plan of catalog.cloud.plans)assert.equal(plan.annualPrice,plan.monthlyEquivalent*12);
console.log('PASS: package quotas are preserved by Cloud APIs/admin/invites; annual catalog arithmetic and15-day trial specification match the latest user decision. Source/spec check only.');