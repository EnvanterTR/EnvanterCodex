import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const accountApi = readFileSync('src/EnvanterTR.Web/Data/AccountEndpoints.cs', 'utf8');
const integrationApi = readFileSync('src/EnvanterTR.Web/Data/CustomerApiEndpoints.cs', 'utf8');

assert.match(source, /confirmComponentAction\(\{ eyebrow: 'OTURUM GÜVENLİĞİ', title: session\.current \? 'Bu cihazdaki oturumu kapatmak istiyor musunuz\?' : 'Bu cihazdaki oturumu uzaktan kapatmak istiyor musunuz\?', summary: 'Bu işlem seçili oturumu hemen sonlandırır\.'/,
  'Current and remote session revocation must use the cancellable in-app dialog with clear consequences.');
assert.match(source, /\/api\/session\/devices\/\$\{encodeURIComponent\(session\.id\)\}\/revoke/,
  'Session confirmation must remain bound to the selected session ID.');
assert.doesNotMatch(source, /window\.confirm\(translateUiText\(session\.current \? 'Bu cihazdaki oturumu/,
  'Session revocation must not rely on browser-native confirmation.');

assert.match(source, /const summary = translateUiText\('“\{name\}” API anahtarı iptal edilsin mi\?'\)\.replace\('\{name\}', token\.name\)[\s\S]*?confirmComponentAction\(\{ eyebrow: 'API ANAHTARI GÜVENLİĞİ', title: 'API anahtarı iptal edilsin mi\?', summary, confirmLabel: 'API anahtarını iptal et', danger: true \}\)/,
  'API key revocation must identify the selected key, explain the consequence, and require explicit in-app confirmation.');
assert.match(source, /\/api\/integrations\/tokens\/\$\{encodeURIComponent\(token\.id\)\}\/revoke/,
  'API key confirmation must remain bound to the selected token ID.');
assert.doesNotMatch(source, /window\.confirm\(translateUiText\('“\{name\}” API anahtarı/,
  'API key revocation must not rely on browser-native confirmation.');
assert.match(accountApi, /MapPost\("\/api\/session\/devices\/\{id:guid\}\/revoke"/,
  'Session revoke must remain implemented by the authenticated account API.');
assert.match(integrationApi, /MapPost\("\/api\/integrations\/tokens\/\{id:guid\}\/revoke"/,
  'API key revoke must remain implemented by the authenticated integration API.');

console.log('Verified cancellable session and API-key revocation dialogs, selected-record scoping, and authenticated API routes.');
