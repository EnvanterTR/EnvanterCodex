import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const html = readFileSync(sourcePath, 'utf8');
for (const id of ['licenseRevokeDialog', 'licenseRevokeForm', 'licenseRevokeTitle', 'licenseRevokeSummary', 'licenseRevokeClose', 'licenseRevokeCancel', 'licenseRevokeSubmit']) {
  assert.match(html, new RegExp(`id="${id}"`), `license revocation dialog must include ${id}`);
}
assert.match(html, /id="licenseRevokeDialog"[^>]*aria-labelledby="licenseRevokeTitle"/, 'license revocation dialog must have an accessible name');

const revokeStart = html.indexOf('async function revokeLicense(');
const assignmentsStart = html.indexOf('async function loadLicenseAssignments(', revokeStart);
const revokeDialogStart = html.indexOf("$('#licenseRevokeClose')", assignmentsStart);
const contractsStart = html.indexOf('let contractPage', revokeDialogStart);
assert.ok(revokeStart >= 0 && assignmentsStart > revokeStart && revokeDialogStart > assignmentsStart && contractsStart > revokeDialogStart,
  'license revocation handlers must be present in the expected order');
const detailRevoke = html.slice(revokeStart, assignmentsStart);
const assignmentList = html.slice(assignmentsStart, revokeDialogStart);
const dialogActions = html.slice(revokeDialogStart, contractsStart);
assert.doesNotMatch(detailRevoke, /\bconfirm\s*\(/, 'license details must use the in-app confirmation dialog');
assert.match(detailRevoke, /openLicenseRevocation\(currentLicense\.id,\s*assignmentId,\s*'detail'/, 'license detail revoke must open the shared dialog');
assert.doesNotMatch(assignmentList, /\bconfirm\s*\(/, 'assignment list revoke must use the in-app confirmation dialog');
assert.match(assignmentList, /openLicenseRevocation\(b\.dataset\.licenseId,b\.dataset\.assignmentId,'list'/,
  'license assignment list revoke must pass its company license and assignment IDs');
assert.match(dialogActions, /const formElement=event\.currentTarget,draft=pendingLicenseRevocation/, 'capture the dialog form and revoke draft before awaiting');
assert.match(dialogActions, /\/api\/software-licenses\/\$\{draft\.licenseId\}\/revoke/, 'submit revocation to the company license endpoint');
assert.match(dialogActions, /draft\.context==='detail'[\s\S]*?loadLicenseAssignments\(\)/, 'refresh the current license details or assignment list after revocation');
assert.match(dialogActions, /loadLicenses\(\)/, 'refresh license seat totals after revocation');
assert.match(dialogActions, /licenseRevokeDialog'\)\.addEventListener\('close'/, 'dismissal must clear pending revocation state');

console.log('Verified the accessible license assignment revocation dialog, both entry points, scoped API request, refresh behavior, and dismissal cleanup.');
