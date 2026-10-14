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
assert.match(program, /orderby item\.Status == "Active" \|\| item\.Status == "Trial" \? 0 : 1, item\.StartsAt descending[\s\S]*?FirstOrDefaultAsync/);
assert.match(account, /subscriptionAccess = subscription is null \? null/);
assert.match(account, /mode = TenantSubscriptionAccess\.IsReadOnly/);
assert.match(page, /id="subscriptionExpiryNotice"[\s\S]*?şirket kayıtlarınız korunuyor/i);
assert.match(page, /'Abonelik süresi doldu':\['Subscription expired','Срок подписки истёк','Термін підписки минув','Претплатата е истечена'\]/);
assert.match(page, /'Şirket kayıtlarınız korunuyor\.[\s\S]*?Platform yöneticinizden süre uzatımı isteyin\.'\s*:\s*\['Your company records are safe\.[\s\S]*?Побарајте од администраторот на платформата да ја продолжи\.'\]/);
assert.match(page, /const roots = \[[\s\S]*?\$\('#subscriptionExpiryNotice'\)/);
assert.match(page, /session\.subscriptionAccess\?\.mode !== 'readOnly'/);
assert.match(page, /402: 'Şirket aboneliği yazma işlemlerine kapalı/);
assert.match(acceptance, /Expired_cloud_subscription_preserves_read_access_and_blocks_company_writes/);
assert.match(acceptance, /Current_cloud_subscription_is_selected_when_company_keeps_expired_subscription_history/);
console.log('Tenant subscription access verification passed (17 assertions).');
