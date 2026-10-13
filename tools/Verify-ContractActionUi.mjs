import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of ['contractActionDialog', 'contractActionForm', 'contractActionTitle', 'contractActionSummary', 'contractActionMessage', 'contractActionClose', 'contractActionCancel', 'contractActionSubmit']) {
  assert.match(html, new RegExp(`id="${id}"`), `contract action dialog must include ${id}`);
}
assert.match(html, /id="contractActionDialog"[^>]*aria-labelledby="contractActionTitle"/, 'contract action dialog must have an accessible name');
assert.match(html, /const roots = \[[^\]]*\$\('#contractActionDialog'\)/, 'contract action dialog must be refreshed when the selected language changes');

const rowStart = html.indexOf("$('#contractRows').addEventListener('click'");
const formStart = html.indexOf("$('#contractForm').addEventListener('submit'", rowStart);
const actionHandlersStart = html.indexOf("$('#contractActionClose').addEventListener('click'", formStart);
const submitStart = html.indexOf("$('#contractActionForm').addEventListener('submit'", actionHandlersStart);
const attachmentStart = html.indexOf("$('#contractAttachmentRows').addEventListener('click'", submitStart);
const nextSection = html.indexOf('let consumablePage', attachmentStart);
assert.ok(rowStart >= 0 && formStart > rowStart && actionHandlersStart > formStart && submitStart > actionHandlersStart && attachmentStart > submitStart && nextSection > attachmentStart,
  'contract archive and attachment actions must be wired in the expected handlers');
const rowActions = html.slice(rowStart, formStart);
const dialogActions = html.slice(actionHandlersStart, nextSection);
assert.doesNotMatch(rowActions, /\bconfirm\s*\(/, 'contract archive must not depend on a browser confirmation dialog');
assert.ok(rowActions.includes("openContractAction('archive'") && rowActions.includes('/api/contracts/${row.dataset.id}/restore'),
  'archiving must open the in-app confirmation and restore must remain connected');
assert.ok(dialogActions.includes('/api/contracts/${draft.contractId}/archive'), 'archive confirmation must call the contract archive endpoint');
assert.ok(dialogActions.includes('/api/contracts/${draft.contractId}/attachments/${draft.attachmentId}'), 'attachment confirmation must delete the selected contract attachment');
assert.match(dialogActions, /await openContract\(draft\.contractId\)[\s\S]*?await loadContracts\(contractPage\)/, 'attachment deletion must refresh the open contract and list');
assert.ok(dialogActions.includes("$('#contractActionDialog').addEventListener('close'") && dialogActions.includes('pendingContractAction = null'), 'dismissal must clear the pending contract action');
assert.doesNotMatch(dialogActions, /\bconfirm\s*\(/, 'contract attachment deletion must not depend on a browser confirmation dialog');

console.log('Verified localized contract archive and attachment deletion dialogs, endpoint wiring, list/detail refresh, and dismissal cleanup.');
