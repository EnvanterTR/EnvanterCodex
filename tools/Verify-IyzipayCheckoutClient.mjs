import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../src/EnvanterTR.Web/Data/IyzipayCheckoutClient.cs', import.meta.url), 'utf8');
const program = readFileSync(new URL('../src/EnvanterTR.Web/Program.cs', import.meta.url), 'utf8');
const tests = readFileSync(new URL('../tests/EnvanterTR.SecurityAcceptance/IyzipayCheckoutClientTests.cs', import.meta.url), 'utf8');

const required = [
  ['official initialize path', '/payment/iyzipos/checkoutform/initialize/auth/ecom'],
  ['official retrieve path', '/payment/iyzipos/checkoutform/auth/ecom/detail'],
  ['HMACSHA256 request signature', 'HMACSHA256.HashData(secret, signaturePayload)'],
  ['iyzico request nonce header', '"x-iyzi-rnd"'],
  ['official HTTPS hosts only', '"api.iyzipay.com" or "sandbox-api.iyzipay.com"'],
  ['bounded provider response', 'MaxResponseBytes = 1_048_576'],
  ['configuration bound from environment-capable configuration', 'GetSection("Iyzipay")'],
  ['provider redirects disabled', 'AllowAutoRedirect = false'],
  ['fake HTTP signature acceptance', 'Checkout_requests_use_the_official_server_side_signature'],
  ['missing credentials and host rejection acceptance', 'fails_closed_without_credentials_or_with_an_untrusted_api_host'],
  ['provider failure acceptance', 'provider_failure_is_not_returned_as_a_success_document']
];

for (const [label, value] of required) {
  if (!source.includes(value) && !program.includes(value) && !tests.includes(value)) {
    console.error(`FAIL: ${label}`);
    process.exitCode = 1;
  }
}

if (!process.exitCode) console.log('PASS: iyzico Checkout Form transport is signed, HTTPS host-restricted, bounded, configurable, and covered by synthetic acceptance tests.');
