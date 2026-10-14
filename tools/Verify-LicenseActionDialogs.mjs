import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of ['licenseActionDialog','licenseActionForm','licenseActionTitle','licenseActionSummary','licenseActionError','licenseActionClose','licenseActionCancel','licenseActionSubmit']) assert.match(html,new RegExp(`id="${id}"`),`confirmation dialog must include ${id}`);
assert.match(html,/id="licenseActionDialog"[^>]*aria-labelledby="licenseActionTitle"/,'dialog must have accessible name');
assert.match(html,/const roots = \[[^\]]*\$\('#licenseActionDialog'\)/,'dialog must participate in language refresh');
const attachmentStart=html.indexOf('async function loadLicenseAttachments('), attachmentEnd=html.indexOf('\\n',attachmentStart);
assert.ok(attachmentStart>=0&&attachmentEnd>attachmentStart,'license attachment loader must exist');
const attachments=html.slice(attachmentStart,attachmentEnd);
assert.doesNotMatch(attachments,/\bconfirm\s*\(/,'attachment deletion must not invoke native confirmation');
assert.match(attachments,/openLicenseAction\(\{kind:'attachment-delete',licenseId:id,attachmentId:b\.dataset\.id\}\)/,'capture the exact license and attachment before confirming');
const actionStart=html.indexOf('let pendingLicenseAction=null;'), actionEnd=html.indexOf('function licenseImportMessage(',actionStart);
assert.ok(actionStart>=0&&actionEnd>actionStart,'shared license action handlers must exist');
const actions=html.slice(actionStart,actionEnd);
assert.doesNotMatch(actions,/\bconfirm\s*\(/,'license actions must not invoke native confirmation');
assert.match(actions,/licenseActionCancel[\s\S]*?licenseActionDialog'\)\.close\(\)/,'cancel must close the in-app dialog');
assert.match(actions,/licenseActionDialog'\)\.addEventListener\('close',[\s\S]*?pendingLicenseAction=null/,'dismissal must clear pending action state');
assert.match(actions,/currentLicense\.id\)!==String\(action\.licenseId\)/,'attachment deletion must revalidate the open license');
assert.match(actions,/licenseImportState\.upload!==action\.upload/,'import must revalidate preview identity before writing');
assert.ok(actions.includes("await api('/api/software-licenses/'+encodeURIComponent(action.licenseId)+'/attachments/'+encodeURIComponent(action.attachmentId)"),'delete the selected attachment through its scoped API route');
assert.ok(actions.includes("api('/api/software-licenses/import.'+action.upload.format"),'commit the validated import to the format-specific API');
assert.ok(actions.includes("error.textContent=actionError.message||String(actionError);error.hidden=false"),'failed actions must remain visible inside the dialog');
assert.match(actions,/licenseImportFeedback'\)\.textContent=licenseImportMessage\(result\.message\)/,'successful import result must remain visible');
assert.match(html,/commitLicenseImport'\)\.addEventListener\('click',\(\)=>\{[\s\S]*?openLicenseAction\(\{kind:'import'/,'import confirmation should open before making the write request');
console.log('Verified accessible, cancellable license confirmation flow, identity revalidation, scoped API requests, and inline error/result reporting.');


