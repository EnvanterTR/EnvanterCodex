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
assert.match(source, /Google Authenticator, Microsoft Authenticator veya TOTP uyumlu başka bir uygulama kullanabilirsiniz\.[\s\S]*?Google\/Microsoft hesabı, abonelik, SMS ya da ayrıca bir hizmet satın almanız gerekmez/,
  'The account MFA UI must explain that TOTP does not require a vendor account, subscription, SMS, or separate service.');
assert.match(source, /function clearTwoFactorDialogSecrets\(\)\s*\{[\s\S]*?\$\('#twoFactorSecret'\)\.textContent = '';[\s\S]*?\$\('#twoFactorRecoveryCodes'\)\.textContent = '';[\s\S]*?\$\('#twoFactorCurrentPassword'\)\.value = '';[\s\S]*?\$\('#twoFactorCode'\)\.value = '';/,
  'Closing the account MFA dialog must clear the setup secret, recovery codes, password, and entered OTP from the DOM.');
assert.match(source, /function renderAccountTotpQr\(authenticatorUri\)[\s\S]*?qrcodegen\.QrCode\.encodeText\(authenticatorUri[\s\S]*?data:image\/svg\+xml;charset=utf-8/,
  'Company-account MFA QR codes must be generated locally from the server-provided TOTP URI.');
assert.match(accountApi, /return Results\.Ok\(new \{ secret = key, authenticatorUri = uri \}\)/,
  'The authenticated MFA setup API must provide its standards-based TOTP provisioning URI for the local QR renderer.');
assert.match(source, /\$\('#accountTwoFactorDialog'\)\.addEventListener\('cancel', clearTwoFactorDialogSecrets\);[\s\S]*?\$\('#accountTwoFactorDialog'\)\.addEventListener\('close', clearTwoFactorDialogSecrets\);/,
  'Escape and all dialog-close paths must synchronously clear sensitive MFA values.');

console.log('Verified session/API-key revocation, MFA provider guidance, locally rendered TOTP QR, and dialog-secret cleanup.');
