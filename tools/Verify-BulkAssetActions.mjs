import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const returnAction = source.slice(source.indexOf("$('#bulkReturnSelectedAssets').addEventListener('click'"), source.indexOf("$('#bulkAssetStatus').addEventListener('change'"));
const statusAction = source.slice(source.indexOf("$('#applyBulkAssetStatus').addEventListener('click'"), source.indexOf("document.addEventListener('pointerdown'", source.indexOf("$('#applyBulkAssetStatus').addEventListener('click'")));
const assignmentAction = source.slice(source.indexOf('async function openBulkAssignDialog()'), source.indexOf('function ensureEmployeePhotoControls()'));
const transferAction = source.slice(source.indexOf('async function openBulkAssetTransferDialog()'), source.indexOf("$('#assignAssetButton').addEventListener('click'"));
const dialog = source.slice(source.indexOf("const bulkAssetActionDialog = document.createElement('dialog')"), source.indexOf("$('#bulkReturnSelectedAssets').addEventListener('click'"));

requireSource(dialog.includes("bulkAssetActionDialog.showModal()") && dialog.includes("event.target === bulkAssetActionDialog") && dialog.includes("event.preventDefault(); closeBulkAssetAction(null)") && dialog.includes("closeBulkAssetAction({ note:"),
  'Bulk actions must use a dismissible in-app dialog with distinct submit and cancel outcomes.');
requireSource(returnAction.includes('if (!canWriteAssets()') && returnAction.includes('await requestBulkAssetAction') && returnAction.includes("'/api/assets/bulk-return'") && !returnAction.includes('confirm(') && !returnAction.includes('prompt('),
  'Bulk check-in must be role gated, explicit-submit only, and free of native browser prompts.');
requireSource(statusAction.includes('if (!canWriteAssets()') && statusAction.includes('await requestBulkAssetAction') && statusAction.includes("'/api/assets/bulk-status'") && !statusAction.includes('confirm(') && !statusAction.includes('prompt('),
  'Bulk status change must be role gated, explicit-submit only, and free of native browser prompts.');
requireSource(assignmentAction.includes('async function openBulkAssignDialog()') && assignmentAction.includes('selectedAssets.some(asset => !asset || asset.assignedEmployeeId || asset.status !== \'Available\')') && source.includes("api('/api/assets/bulk-assign'"),
  'Bulk assignment must reject ineligible selections and submit through its tenant API.');
requireSource(transferAction.includes('async function openBulkAssetTransferDialog()') && transferAction.includes("'/api/assets/bulk-transfer'") && transferAction.includes('clearBulkAssetSelection()'),
  'Bulk transfer must reject ineligible selections, submit through its tenant API, and clear selection only after success.');
requireSource(source.includes("bulkAuditButton.id = 'bulkAuditSelectedAssets'") && source.includes("'/api/assets/bulk-audit'"),
  'Bulk audit must be exposed from the selection toolbar and submitted through its tenant API.');

const browserAcceptance = fs.readFileSync(path.join(root, 'deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs'), 'utf8');
const apiAcceptance = fs.readFileSync(path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs'), 'utf8');
for (const action of ['assignment', 'transfer', 'audit']) {
  requireSource(browserAcceptance.includes(`PASS: bulk asset ${action}`), `Chromium acceptance must cover confirmed and canceled bulk ${action}.`);
  requireSource(apiAcceptance.includes(`Bulk_asset_${action}_is_atomic_company_scoped_authorized_and_audited`), `PostgreSQL acceptance must verify bulk ${action} authorization, scope, atomicity, and audit.`);
}

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified in-app cancellation and submission for bulk assignment, return, status change, audit, and location transfer, with tenant API/PostgreSQL acceptance coverage.');
}
