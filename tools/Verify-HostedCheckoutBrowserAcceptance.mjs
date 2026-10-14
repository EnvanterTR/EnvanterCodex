import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const script = await readFile('deploy/testing/hosted-checkout-browser/hosted-checkout.mjs', 'utf8');
const palette = await readFile('deploy/testing/hosted-checkout-browser/command-palette.mjs', 'utf8');
const dockerfile = await readFile('deploy/testing/Dockerfile.hosted-checkout-browser', 'utf8');
const runner = await readFile('deploy/testing/run-hosted-checkout-browser-acceptance.ps1', 'utf8');
const workflow = await readFile('.github/workflows/ci.yml', 'utf8');

assert.match(dockerfile, /mcr\.microsoft\.com\/playwright:v1\.64\.0-noble/);
assert.match(dockerfile, /npm ci --omit=dev/);
assert.match(dockerfile, /src\/EnvanterTR\.PublicSite\/checkout\.html src\/EnvanterTR\.PublicSite\/checkout\.js/);
assert.match(dockerfile, /src\/EnvanterTR\.Web\/wwwroot\/index\.html/);
assert.match(dockerfile, /index\.html \/checkout-site\/company-app\.html/);
assert.match(script, /await import\('\.\/command-palette\.mjs'\)/);
assert.match(palette, /role: 'viewer'/);
assert.match(palette, /The page must not expose company member administration to a viewer role/);
assert.match(palette, /apiRequests\.some\(item => item\.method !== 'GET'\)/);
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

console.log('Hosted checkout and command palette browser acceptance harness verification passed (21 assertions).');
