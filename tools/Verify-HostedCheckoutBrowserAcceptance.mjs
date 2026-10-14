import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const script = await readFile('deploy/testing/hosted-checkout-browser/hosted-checkout.mjs', 'utf8');
const dockerfile = await readFile('deploy/testing/Dockerfile.hosted-checkout-browser', 'utf8');
const runner = await readFile('deploy/testing/run-hosted-checkout-browser-acceptance.ps1', 'utf8');
const workflow = await readFile('.github/workflows/ci.yml', 'utf8');

assert.match(dockerfile, /mcr\.microsoft\.com\/playwright:v1\.64\.0-noble/);
assert.match(dockerfile, /npm ci --omit=dev/);
assert.match(dockerfile, /src\/EnvanterTR\.PublicSite\/checkout\.html src\/EnvanterTR\.PublicSite\/checkout\.js/);
assert.match(runner, /docker build[\s\S]*?docker run --rm --init/);
assert.match(script, /paymentAvailable = false/);
assert.match(script, /Personal and billing data must remain disabled/);
assert.match(script, /Disabled checkout must not send company or billing data/);
assert.match(script, /localStorage\.setItem\('envantertr-home-language', 'en'\)/);
assert.match(script, /monthlyTerm/);
assert.match(script, /dataProcessingNoticeVersion/);
assert.match(script, /'amount' in lastInitialize\.body/);
assert.match(script, /sandbox-api\.iyzipay\.com/);
assert.match(script, /pageErrors/);
assert.match(workflow, /Verify-HostedCheckoutBrowserAcceptance\.mjs/);
assert.match(workflow, /Run hosted Cloud checkout in Chromium[\s\S]*?docker run --rm --init envantertr-hosted-checkout-browser-ci/);

console.log('Hosted checkout browser acceptance harness verification passed (15 assertions).');
