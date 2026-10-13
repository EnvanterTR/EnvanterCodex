import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const policy = await readFile('src/EnvanterTR.Web/Data/TenantSubscriptionAccess.cs', 'utf8');
const program = await readFile('src/EnvanterTR.Web/Program.cs', 'utf8');
const account = await readFile('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const page = await readFile('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(policy, /deploymentKind, "cloud"/);
assert.match(policy, /status is not \("Active" or "Trial" or "Cancelled"\)/);
assert.match(policy, /now < startsAt \|\| now >= endsAt/);
assert.match(program, /requestPath\.StartsWithSegments\("\/api\/session"\)/);
assert.match(program, /requestPath\.StartsWithSegments\("\/api\/platform"\)/);
assert.match(program, /Status402PaymentRequired[\s\S]*subscription_read_only/);
assert.match(account, /subscriptionAccess = subscription is null \? null/);
assert.match(account, /mode = TenantSubscriptionAccess\.IsReadOnly/);
assert.match(page, /id="subscriptionExpiryNotice"[\s\S]*?şirket kayıtlarınız korunuyor/i);
assert.match(page, /session\.subscriptionAccess\?\.mode !== 'readOnly'/);
assert.match(page, /402: 'Şirket aboneliği yazma işlemlerine kapalı/);
assert.match(acceptance, /Expired_cloud_subscription_preserves_read_access_and_blocks_company_writes/);
console.log('Tenant subscription access verification passed (12 assertions).');
