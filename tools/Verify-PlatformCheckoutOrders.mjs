import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const api = await readFile('src/EnvanterTR.Web/Data/PlatformEndpoints.cs', 'utf8');
const page = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin.html', 'utf8');
const acceptance = await readFile('tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs', 'utf8');

assert.match(api, /admin\.MapGet\("\/checkout-orders"/);
assert.match(api, /validStatuses[\s\S]*?PaymentReview[\s\S]*?ProvisioningFailed/);
assert.match(api, /x\.order\.PaymentReference\.Contains\(term\)/);
assert.match(api, /pageSize = Math\.Clamp\(pageSize == 0 \? 50 : pageSize, 1, 200\)/);
const orderProjection = api.match(/admin\.MapGet\("\/checkout-orders"[\s\S]*?admin\.MapGet\("\/plans"/);
assert.ok(orderProjection, 'checkout order endpoint projection exists');
assert.doesNotMatch(orderProjection[0], /CheckoutTokenHash|CheckoutPageUrlProtected/);
assert.match(page, /data-view="orders"[^>]*>[\s\S]*?Satın almalar/);
assert.match(page, /id="orderSearch"/);
assert.match(page, /id="orderStatus"/);
assert.match(page, /id="refreshOrders"/);
assert.match(page, /async function loadOrders\(/);
assert.match(page, /api\('\/api\/platform\/checkout-orders\?'/);
assert.match(page, /Harici satışlar ve iadeler için sağlayıcı mutabakatı henüz bağlanmadı/);
assert.match(acceptance, /Platform_checkout_orders_are_searchable_paginated_and_hide_provider_secrets/);
console.log('Platform checkout orders UI/source verification passed (14 assertions).');
