import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const root = new URL('../', import.meta.url);
const html = await readFile(new URL('src/EnvanterTR.Web/wwwroot/index.html', root), 'utf8');
const endpoints = await readFile(new URL('src/EnvanterTR.Web/Data/DataExportEndpoints.cs', root), 'utf8');

assert.match(html, /\$\('#dataValidateFile'\)\.addEventListener\('click', \(\) => openImportPreview\(\{ companyRestoreOnly: true \}\)\)/,
  'The restore page must open a JSON-only preview directly, without clicking the asset-import action.');
assert.match(html, /function openImportPreview\(\{ companyRestoreOnly = false \} = \{\}\)/,
  'The shared import dialog opener must support a dedicated company-restore mode.');
assert.match(html, /const canImportAssets = canWriteAssets\(\) && !companyRestoreOnly;/,
  'Asset import controls must be unavailable in restore-only mode and for users without asset-write permission.');
assert.match(html, /if \(canImportAssets\) loadAssetImportProfiles\(\);/,
  'Asset import profiles must not be fetched for a company-admin-only restore session.');
assert.match(html, /file\.accept = companyRestoreOnly \? '\.json,application\/json'/,
  'Restore-only mode must constrain the file picker to company JSON archives.');
assert.match(html, /\$\('#applyCompanyImport'\)\.hidden = !\(lowerName\.endsWith\('\.json'\) && preview\.isValid && preview\.restoreTargetEmpty === true\)/,
  'Applying a restore must require a valid JSON preview and an empty target company.');
assert.match(endpoints, /MapPost\("\/api\/company\/import\/preview", PreviewImportAsync\)\s*\.RequireAuthorization\("settings\.manage"\)/,
  'Company archive preview must be protected by company-management authorization.');
assert.match(endpoints, /MapPost\("\/api\/company\/import\/apply", ApplyCompanyImportAsync\)\s*\.RequireAuthorization\("settings\.manage"\)/,
  'Company archive apply must be protected by company-management authorization.');

console.log('Verified company restore shortcut, JSON-only mode, role-scoped asset controls, valid-preview gating, and server authorization.');
