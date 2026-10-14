import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const returnAction = source.slice(source.indexOf("$('#bulkReturnSelectedAssets').addEventListener('click'"), source.indexOf("$('#bulkAssetStatus').addEventListener('change'"));
const statusAction = source.slice(source.indexOf("$('#applyBulkAssetStatus').addEventListener('click'"), source.indexOf("document.addEventListener('pointerdown'", source.indexOf("$('#applyBulkAssetStatus').addEventListener('click'")));
const dialog = source.slice(source.indexOf("const bulkAssetActionDialog = document.createElement('dialog')"), source.indexOf("$('#bulkReturnSelectedAssets').addEventListener('click'"));

requireSource(dialog.includes("bulkAssetActionDialog.showModal()") && dialog.includes("event.target === bulkAssetActionDialog") && dialog.includes("event.preventDefault(); closeBulkAssetAction(null)") && dialog.includes("closeBulkAssetAction({ note:"),
  'Bulk actions must use a dismissible in-app dialog with distinct submit and cancel outcomes.');
requireSource(returnAction.includes('if (!canWriteAssets()') && returnAction.includes('await requestBulkAssetAction') && returnAction.includes("'/api/assets/bulk-return'") && !returnAction.includes('confirm(') && !returnAction.includes('prompt('),
  'Bulk check-in must be role gated, explicit-submit only, and free of native browser prompts.');
requireSource(statusAction.includes('if (!canWriteAssets()') && statusAction.includes('await requestBulkAssetAction') && statusAction.includes("'/api/assets/bulk-status'") && !statusAction.includes('confirm(') && !statusAction.includes('prompt('),
  'Bulk status change must be role gated, explicit-submit only, and free of native browser prompts.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified in-app confirmation and note dialog for bulk asset check-in and bulk status change, including cancel-without-write and role gates.');
}
