import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const html = readFileSync(sourcePath, 'utf8');
for (const id of ['licenseRevokeDialog', 'licenseRevokeForm', 'licenseRevokeTitle', 'licenseRevokeSummary', 'licenseRevokeClose', 'licenseRevokeCancel', 'licenseRevokeSubmit']) {
  assert.match(html, new RegExp(`id="${id}"`), `license revocation dialog must include ${id}`);
}
assert.match(html, /id="licenseRevokeDialog"[^>]*aria-labelledby="licenseRevokeTitle"/, 'license revocation dialog must have an accessible name');
for (const id of ['licenseAssignmentSearch', 'licenseAssignmentState', 'licenseAssignmentRows', 'licenseAssignmentCount', 'licenseAssignmentPrev', 'licenseAssignmentNext', 'licenseAssignmentPageInfo', 'licenseBulkEmployeeMode', 'licenseBulkEmployeeHelp']) {
  assert.match(html, new RegExp(`id="${id}"`), `license assignment list must include ${id}`);
}
assert.match(html, /<option value="true">Aktif atamalar<\/option><option value="false">Geri al\u0131nan atamalar<\/option><option value="">T\u00fcm atamalar<\/option>/,
  'license assignment list must offer active, revoked, and all states');

const revokeStart = html.indexOf('async function revokeLicense(');
const assignmentsStart = html.indexOf('async function loadLicenseAssignments(', revokeStart);
const revokeDialogStart = html.indexOf("$('#licenseRevokeClose')", assignmentsStart);
const contractsStart = html.indexOf('let contractPage', revokeDialogStart);
const assignmentFormStart = html.indexOf("$('#licenseAssignForm').addEventListener('submit'", revokeDialogStart);
assert.ok(revokeStart >= 0 && assignmentsStart > revokeStart && revokeDialogStart > assignmentsStart && assignmentFormStart > revokeDialogStart && contractsStart > assignmentFormStart,
  'license revocation handlers must be present in the expected order');
const detailRevoke = html.slice(revokeStart, assignmentsStart);
const assignmentList = html.slice(assignmentsStart, revokeDialogStart);
const dialogActions = html.slice(revokeDialogStart, contractsStart);
const assignmentSubmission = html.slice(assignmentFormStart, contractsStart);
assert.doesNotMatch(detailRevoke, /\bconfirm\s*\(/, 'license details must use the in-app confirmation dialog');
assert.match(detailRevoke, /openLicenseRevocation\(currentLicense\.id,\s*assignmentId,\s*'detail'/, 'license detail revoke must open the shared dialog');
assert.doesNotMatch(assignmentList, /\bconfirm\s*\(/, 'assignment list revoke must use the in-app confirmation dialog');
assert.match(assignmentList, /openLicenseRevocation\(b\.dataset\.licenseId,b\.dataset\.assignmentId,'list'/,
  'license assignment list revoke must pass its company license and assignment IDs');
assert.match(assignmentList, /new URLSearchParams\(\{page:licenseAssignmentPage,pageSize:25\}\)/,
  'license assignment results must use a fixed page size and explicit page number');
assert.match(assignmentList, /if\(q\)params\.set\('q',q\);if\(active\)params\.set\('active',active\)/,
  'license assignment search and status filter must reach the API');
assert.match(html, /requestSequence=\+\+licenseAssignmentLoadSequence[\s\S]*?if\(requestSequence!==licenseAssignmentLoadSequence\)return/,
  'a late assignment response must not overwrite a newer filter or page result');
assert.match(html, /licenseAssignmentPageInfo'\)\.textContent=[\s\S]*?licenseAssignmentPrev'\)\.disabled=r\.page<=1;\$\('#licenseAssignmentNext'\)\.disabled=r\.page\*r\.pageSize>=r\.total/,
  'license assignment pagination must show the current page and disable unavailable directions');
assert.match(html, /licenseAssignmentState'\)\.addEventListener\('change',\(\)=>\{clearTimeout\(licenseAssignmentTimer\);loadLicenseAssignments\(1\)/,
  'changing assignment status must cancel pending search debounce and return to page one');
assert.match(html, /\$\('#licenseBulkEmployeeMode'\)\.addEventListener\('change',e=>setBulkLicenseEmployees\(e\.currentTarget\.checked\)\)/,
  'the bulk assignment mode control must activate employee multi-select');
assert.match(assignmentSubmission, /employeeIds\.length<1\|\|employeeIds\.length>100/,
  'the bulk assignment form must enforce the 1–100 employee limit before submitting');
assert.match(assignmentSubmission, /\/api\/software-licenses\/\$\{licenseId\}\/assign\/bulk/,
  'bulk employee assignment must use the atomic company license endpoint');
assert.match(assignmentSubmission, /submit\.disabled=true[\s\S]*?finally\{submit\.disabled=false;/,
  'the assignment submit control must prevent duplicate submissions and recover after errors');
assert.match(dialogActions, /const formElement=event\.currentTarget,draft=pendingLicenseRevocation/, 'capture the dialog form and revoke draft before awaiting');
assert.match(dialogActions, /\/api\/software-licenses\/\$\{draft\.licenseId\}\/revoke/, 'submit revocation to the company license endpoint');
assert.match(dialogActions, /draft\.context==='detail'[\s\S]*?loadLicenseAssignments\(\)/, 'refresh the current license details or assignment list after revocation');
assert.match(dialogActions, /loadLicenses\(\)/, 'refresh license seat totals after revocation');
assert.match(dialogActions, /licenseRevokeDialog'\)\.addEventListener\('close'/, 'dismissal must clear pending revocation state');

assert.match(html, /data-page="Lisanslarım"/, 'the software menu must expose the employee self-service license page');
assert.match(html, /id="myLicensesPage"[\s\S]*?id="myLicensesRefresh"[\s\S]*?id="myLicensesRows"/,
  'the employee page must include an accessible refresh action and a dedicated license list');
const employeeLicenseStart = html.indexOf('async function loadMySoftwareLicenses()');
const employeeLicenseEnd = html.indexOf("$('#licenseRevokeClose')", employeeLicenseStart);
assert.ok(employeeLicenseStart >= 0 && employeeLicenseEnd > employeeLicenseStart, 'employee license page logic must be present before adjacent license action handlers');
const employeeLicenseFlow = html.slice(employeeLicenseStart, employeeLicenseEnd);
assert.match(employeeLicenseFlow, /api\('\/api\/session\/software-licenses'\)/, 'employee licenses must come from the signed-in employee-scoped endpoint');
assert.match(employeeLicenseFlow, /item\.eulaUrl[\s\S]*?target\s*=\s*'_blank'[\s\S]*?rel\s*=\s*'noopener'/,
  'the current EULA must open in a separate protected tab without exposing the opener');
assert.match(employeeLicenseFlow, /opened\s*=\s*false[\s\S]*?accept\.disabled\s*=\s*!checkbox\.checked\s*\|\|\s*!opened/,
  'acceptance must require both opening the current document and an explicit checked confirmation');
assert.match(employeeLicenseFlow, /if\s*\(!requestToken\)\s*await\s+refreshCsrf\(\)[\s\S]*?\/eula\/accept[\s\S]*?confirmed:\s*checkbox\.checked/,
  'acceptance must send the current document ID and explicit confirmation through the CSRF-protected POST');
assert.doesNotMatch(employeeLicenseFlow, /innerHTML\s*=/, 'API-provided employee license values must be rendered as text, not HTML');
assert.match(html, /page === 'Lisanslarım'[\s\S]*?loadMySoftwareLicenses\(\)/,
  'navigating to the employee license menu must render and fetch the self-service list');

console.log('Verified license assignment actions and the employee EULA self-service menu, document access, explicit acceptance gating, and CSRF-protected submission.');
