import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of [
  'licenseImportProfileDeleteDialog', 'licenseImportProfileDeleteForm', 'licenseImportProfileDeleteTitle',
  'licenseImportProfileDeleteSummary', 'licenseImportProfileDeleteClose', 'licenseImportProfileDeleteCancel',
  'licenseImportProfileDeleteSubmit',
]) {
  assert.match(html, new RegExp(`id="${id}"`), `profile deletion dialog must include ${id}`);
}
assert.match(html, /id="licenseImportProfileDeleteDialog"[^>]*aria-labelledby="licenseImportProfileDeleteTitle"/,
  'profile deletion dialog must have an accessible name');
assert.match(html, /const roots = \[[^\]]*\$\('#licenseImportProfileDeleteDialog'\)/,
  'profile deletion dialog must be included in the application language refresh');

const start = html.indexOf("$('#deleteLicenseImportProfile').addEventListener('click'");
const end = html.indexOf("$('#applyLicenseImportMapping').addEventListener('click'", start);
assert.ok(start >= 0 && end > start, 'license import profile deletion handlers must exist');
const handlers = html.slice(start, end);
assert.doesNotMatch(handlers, /\bconfirm\s*\(/, 'profile deletion must not rely on a native confirmation dialog');
assert.match(handlers, /pendingLicenseImportProfileDeletion=id[\s\S]*?showModal\(\)/,
  'capture the selected profile and open the in-app confirmation dialog');
assert.match(handlers, /applyShellLanguage\(appearanceLanguage\.value\)[\s\S]*?showModal\(\)/,
  'apply the selected locale before showing the confirmation dialog');
assert.match(handlers, /\/api\/software-licenses\/import-mappings\/\$\{encodeURIComponent\(id\)\}/,
  'delete the selected profile through the company-scoped API route');
assert.match(handlers, /await loadLicenseImportProfiles\(\)[\s\S]*?resetLicenseImport\(\)/,
  'refresh profile options and reset the import preview after deletion');
assert.match(handlers, /licenseImportProfileDeleteDialog'\)\.addEventListener\('close'[\s\S]*?pendingLicenseImportProfileDeletion=null/,
  'dismissal must clear the pending profile ID');

console.log('Verified accessible license import profile deletion, selected ID scoping, refresh/reset behavior, and dismissal cleanup.');
