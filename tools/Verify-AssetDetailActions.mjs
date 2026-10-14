import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const appPath = path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html');
const apiPath = path.join(root, 'src/EnvanterTR.Web/Data/OperationalEndpoints.cs');
const acceptancePath = path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs');
const browserAcceptancePath = path.join(root, 'deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs');
const browserDockerPath = path.join(root, 'deploy/testing/Dockerfile.hosted-checkout-browser');
const browserRunnerPath = path.join(root, 'deploy/testing/hosted-checkout-browser/hosted-checkout.mjs');
const app = fs.readFileSync(appPath, 'utf8');
const api = fs.readFileSync(apiPath, 'utf8');
const acceptance = fs.readFileSync(acceptancePath, 'utf8');
const browserAcceptance = fs.readFileSync(browserAcceptancePath, 'utf8');
const browserDocker = fs.readFileSync(browserDockerPath, 'utf8');
const browserRunner = fs.readFileSync(browserRunnerPath, 'utf8');
const failures = [];

function requireSource(condition, message) {
  if (!condition) failures.push(message);
}

const assetDialog = app.match(/<dialog\b[^>]*id="assetDialog"[\s\S]*?<\/dialog>/)?.[0] ?? '';
const tabs = [...assetDialog.matchAll(/data-asset-tab="([^"]+)"/g)].map(match => match[1]);
requireSource(tabs.length === 5, `Expected five asset-detail tabs; found ${tabs.length}.`);
requireSource(new Set(tabs).size === tabs.length, 'Asset-detail tab identifiers must be unique.');
for (const tab of tabs) {
  requireSource(assetDialog.includes(`id="assetTab${tab[0].toUpperCase()}${tab.slice(1)}"`), `Missing panel for asset-detail tab ${tab}.`);
}

const actions = [
  ['editAssetButton', "$('#editAssetButton').addEventListener('click'", "$('#editAssetForm').addEventListener('submit'", "method: 'PATCH'"],
  ['auditAssetButton', "$('#auditAssetButton').addEventListener('click'", "$('#assetAuditForm').addEventListener('submit'", '/audit`'],
  ['scheduleAssetAuditButton', "$('#scheduleAssetAuditButton').addEventListener('click'", "$('#assetAuditScheduleForm').addEventListener('submit'", '/audit-schedule`'],
  ['addMaintenanceButton', "$('#addMaintenanceButton').addEventListener('click'", "$('#maintenanceStartForm').addEventListener('submit'", '/maintenances${scheduled ?'],
  ['transferAssetButton', "$('#transferAssetButton').addEventListener('click'", "$('#assetTransferForm').addEventListener('submit'", '/transfer`'],
  ['assignAssetButton', "$('#assignAssetButton').addEventListener('click'", "$('#assignForm').addEventListener('submit'", '/assign`'],
  ['returnAssetButton', "$('#returnAssetButton').addEventListener('click'", '/return`', 'POST'],
  ['restoreAssetButton', "$('#restoreAssetButton').addEventListener('click'", '/restore`', 'POST'],
  ['trashAssetButton', "$('#trashAssetButton').addEventListener('click'", 'assetTrashConfirmDialog.showModal()', "$('#trashAssetButton').hidden = !canWriteAssets()"],
  ['assetStatusAction', "$('#assetStatusAction').addEventListener('change'", '/status`', 'PATCH'],
  ['uploadAssetAttachment', "$('#assetAttachmentForm').addEventListener('submit'", '/attachments`', '10 * 1024 * 1024'],
  ['copyAssetTag', "$('#copyAssetTag').addEventListener('click'", 'navigator.clipboard.writeText'],
  ['printAssetLabel', "$('#printAssetLabel').addEventListener('click'", 'printSingleAssetLabel(currentAsset)'],
];

for (const [id, ...contracts] of actions) {
  requireSource(assetDialog.includes(`id="${id}"`), `Asset-detail action ${id} is missing from the dialog.`);
  for (const contract of contracts) requireSource(app.includes(contract), `Asset-detail action ${id} is missing source contract: ${contract}.`);
}

requireSource(app.includes('`/api/assets/${target.id}/trash`'), 'Trash action must call the company-scoped asset trash API after explicit in-app confirmation.');
requireSource(api.includes('api.MapPost("/assets/{id:guid}/trash"'), 'The asset trash API route is missing.');
requireSource(acceptance.includes('Asset_trash_is_tenant_scoped_blocks_live_relations_and_restores_with_audit_history'), 'No tenant/relationship/restore acceptance scenario covers asset trash.');
requireSource(browserAcceptance.includes("page.locator('[data-group=\"inventory\"]').click()") && browserAcceptance.includes("page.locator('#assetRows .asset-name')") && browserAcceptance.includes("page.locator('#editAssetButton').click()") && browserAcceptance.includes("page.locator('#scheduleAssetAuditButton').click()") && browserAcceptance.includes("page.locator('#auditAssetButton').click()") && browserAcceptance.includes("page.locator('#addMaintenanceButton').click()"), 'A real Chromium browser scenario must open the asset detail and its edit, audit, and maintenance forms from the visible UI.');
requireSource(browserAcceptance.includes('assert.equal(apiRequests.length, beforeScheduleCancel') && browserAcceptance.includes("apiRequests[0].method, 'PATCH'") && browserAcceptance.includes('apiRequests[0].body.expectedUpdatedAt') && browserAcceptance.includes("apiRequests.at(-1).method, 'PUT'") && browserAcceptance.includes('assert.deepEqual(apiRequests.at(-1).body, { nextAuditOn })') && browserAcceptance.includes("auditRequest.method, 'POST'") && browserAcceptance.includes('assert.deepEqual(auditRequest.body, { auditDate, nextAuditOn, notes: auditNotes })') && browserAcceptance.includes('scheduleWrite.path, `/api/assets/${assetId}/maintenances/schedule`') && browserAcceptance.includes('startWrite.path, `/api/assets/${assetId}/maintenances/${maintenanceId}/start`') && browserAcceptance.includes('completeWrite.path, `/api/assets/${assetId}/maintenances/${maintenanceId}/complete`') && browserAcceptance.includes("completeWrite.csrf, 'synthetic-asset-csrf'"), 'The browser scenario must prove cancel sends no writes and edit/audit/maintenance saves use the expected CSRF-protected tenant API routes.');
requireSource(browserAcceptance.includes('assert.equal(asset.name, editedName') && browserAcceptance.includes('asset.nextAuditOn, nextAuditOn') && browserAcceptance.includes("#assetHistory .asset-history-item") && browserAcceptance.includes("#assetMaintenanceRows').innerText(), /Synthetic completion note/") && browserAcceptance.includes('assert.deepEqual(pageErrors, [],'), 'The browser scenario must verify refreshed edit/schedule/history/maintenance outcomes and zero JavaScript exceptions.');
requireSource(browserDocker.includes('COPY deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs ./') && browserRunner.includes("await import('./asset-detail-actions.mjs')"), 'The asset-detail browser acceptance must remain wired into the repeatable browser acceptance runner.');
requireSource(api.includes('api.MapPost("/assets/{id:guid}/restore"') && acceptance.includes('Asset_archive_restore_is_authorized_tenant_scoped_and_audited'), 'Archived asset restoration must have a tenant-scoped, role and audit acceptance scenario.');
requireSource(app.includes('function removeAssetAttachment(assetId, attachment)') && app.includes("remove.addEventListener('click', () => removeAssetAttachment(assetId, attachment))") && app.includes("method: 'DELETE'") && app.includes('renderAssetAttachments(target.assetId, result.attachments)'), 'Attachment removal must require a separate explicit confirmation, call the delete API, and refresh its detail list.');
requireSource(app.includes("document.querySelectorAll('[data-asset-tab]')"), 'Asset-detail tabs must have a bound navigation handler.');
requireSource(app.includes('id="quickLookupDialog"') && app.includes("quickLookupDialog.showModal(); form.elements.name.focus();") && app.includes("api(`/api/lookups/${kind}`"), 'Category, brand, and model quick-add must use the in-app form and submit to the tenant lookup API.');
requireSource(app.includes('id="returnAssetDialog" aria-labelledby="returnAssetTitle"') && app.includes("openAssetReturnAction(currentAsset, 'detail')") && app.includes("api(`/api/assets/${target.id}/return`"), 'Asset return must use the in-app confirmation/note form and submit to the tenant return API.');
requireSource(app.includes('id="assetTrashConfirmDialog" aria-labelledby="assetTrashConfirmTitle"') &&
  app.includes('id="assetTrashConfirmForm"') && app.includes('id="assetTrashConfirmSummary"') &&
  app.includes('name="note" maxlength="1000" rows="3"') &&
  app.includes("assetTrashConfirmDialog.showModal();") &&
  app.includes("assetTrashConfirmDialog.addEventListener('click', event => { if (event.target === assetTrashConfirmDialog) closeAssetTrashConfirm(); })"),
  'Asset trash must use an accessible in-app confirmation/note dialog that can be cancelled or dismissed without submitting.');
requireSource(app.includes('id="assetStatusDialog" aria-labelledby="assetStatusDialogTitle"') &&
  app.includes('id="assetStatusForm"') && app.includes('id="assetStatusSummary"') &&
  app.includes('id="assetStatusFeedback" class="auth-error" role="alert" hidden') &&
  app.includes('name="note" maxlength="1000" rows="3"') &&
  app.includes("assetStatusDialog.addEventListener('click', event => { if (event.target === assetStatusDialog) closeAssetStatusDialog(); })"),
  'Asset status changes must use an accessible, dismissible in-app confirmation and optional-note form with an error region.');
requireSource(app.includes('id="assetAttachmentRemoveDialog" aria-labelledby="assetAttachmentRemoveTitle"') &&
  app.includes('id="assetAttachmentRemoveForm"') && app.includes('id="assetAttachmentRemoveSummary"') &&
  app.includes('id="assetAttachmentRemoveFeedback" class="auth-error" role="alert" hidden') &&
  app.includes("assetAttachmentRemoveDialog.addEventListener('click', event => { if (event.target === assetAttachmentRemoveDialog) closeAssetAttachmentRemove(); })"),
  'Asset attachment removal must use an accessible, dismissible in-app confirmation with an error region.');
requireSource(acceptance.includes('Asset_attachment_upload_download_and_delete_are_authorized_tenant_scoped_and_audited'), 'Asset attachment deletion lacks an isolated authorization, tenant, and audit acceptance scenario.');
const quickLookupHandler = app.slice(app.indexOf("document.querySelectorAll('[data-quick-add]')"), app.indexOf("$('#newAssetForm').addEventListener('submit'"));
const detailReturnHandler = app.slice(app.indexOf("$('#returnAssetButton').addEventListener('click'"), app.indexOf("$('#trashAssetButton').addEventListener('click'"));
requireSource(quickLookupHandler.length > 0 && !quickLookupHandler.includes('window.prompt('), 'Asset quick-add must not depend on unsupported native prompt dialogs.');
requireSource(detailReturnHandler.length > 0 && !detailReturnHandler.includes('confirm(') && !detailReturnHandler.includes('prompt('), 'Asset detail return must use its in-app confirmation/note form instead of native dialogs.');
const trashButtonHandler = app.slice(app.indexOf("$('#trashAssetButton').addEventListener('click'"), app.indexOf("$('#assetStatusAction').addEventListener('change'"));
const trashSubmitHandler = app.slice(app.indexOf("$('#assetTrashConfirmForm').addEventListener('submit'"), app.indexOf("$('#assetStatusAction').addEventListener('change'"));
requireSource(trashButtonHandler.length > 0 && trashButtonHandler.includes("openAssetTrashAction(currentAsset, 'trash')") && !trashButtonHandler.includes('confirm(') && !trashButtonHandler.includes('prompt('), 'Opening asset trash confirmation must not trigger native browser dialogs or mutate data.');
requireSource(app.includes("openAssetTrashAction(item, 'restore')") && app.includes("`/api/assets/${target.id}/restore-from-trash`") && !app.slice(app.indexOf('async function loadAssetTrash'), app.indexOf("$('#openAssetTrash').addEventListener")).includes('prompt('), 'Restore from trash must use the same accessible in-app note dialog and avoid native prompt dialogs.');
requireSource(trashSubmitHandler.length > 0 && trashSubmitHandler.includes('if (!assetTrashTarget || !canWriteAssets()) return;') && trashSubmitHandler.includes('`/api/assets/${target.id}/trash`') && trashSubmitHandler.includes('method: \'POST\'') && trashSubmitHandler.includes('closeAssetTrashConfirm();') && trashSubmitHandler.includes('await loadDashboard('), 'Only explicit in-app confirmation may post the tenant-scoped trash action, and success must refresh the dashboard.');
const statusActionHandler = app.slice(app.indexOf("$('#assetStatusAction').addEventListener('change'"), app.indexOf("$('#newEmployeeButton').addEventListener('click'"));
const statusSubmitHandler = app.slice(app.indexOf("$('#assetStatusForm').addEventListener('submit'"), app.indexOf("$('#restoreAssetButton').addEventListener('click'"));
const openStatusActionHandler = app.slice(app.indexOf('function openAssetStatusAction'), app.indexOf("$('#assetStatusAction').addEventListener('change'"));
requireSource(statusActionHandler.length > 0 && statusActionHandler.includes("openAssetStatusAction(currentAsset, 'change', status)") && openStatusActionHandler.includes('canWriteAssets()') && openStatusActionHandler.includes('assetStatusDialog.showModal()') && !openStatusActionHandler.includes('confirm(') && !openStatusActionHandler.includes('prompt('), 'Opening an asset status action must only show the in-app form and must not use native browser dialogs or mutate data.');
requireSource(statusSubmitHandler.length > 0 && statusSubmitHandler.includes('if (!assetStatusTarget || !canWriteAssets()) return;') && statusSubmitHandler.includes('`/api/assets/${target.id}/status`') && statusSubmitHandler.includes("restore ? 'POST' : 'PATCH'") && statusSubmitHandler.includes('closeAssetStatusDialog();') && statusSubmitHandler.includes('await loadDashboard();') && statusSubmitHandler.includes('feedback.hidden = false;'), 'Asset status changes may call the correct tenant API only after explicit in-app submission, refresh on success, and keep the dialog open with an error on failure.');
const restoreAssetHandler = app.slice(app.indexOf("$('#restoreAssetButton').addEventListener('click'"), app.indexOf("$('#newEmployeeButton').addEventListener('click'"));
requireSource(restoreAssetHandler.length > 0 && restoreAssetHandler.includes("openAssetStatusAction(currentAsset, 'restore')") && !restoreAssetHandler.includes('confirm(') && !restoreAssetHandler.includes('prompt(') && statusSubmitHandler.includes('`/api/assets/${target.id}/restore`') && statusSubmitHandler.includes("method: restore ? 'POST' : 'PATCH'"), 'Restoring an archived asset must use the same in-app note form and submit its tenant-scoped restore action only after explicit confirmation.');
const attachmentRemoveHandler = app.slice(app.indexOf("$('#assetAttachmentRemoveForm').addEventListener('submit'"), app.indexOf('function maintenanceDate('));
const attachmentOpenHandler = app.slice(app.indexOf('function removeAssetAttachment'), app.indexOf("$('#closeAssetAttachmentRemove').addEventListener('click'"));
requireSource(attachmentOpenHandler.length > 0 && attachmentOpenHandler.includes("$('#assetAttachmentRemoveDialog').showModal()") && !attachmentOpenHandler.includes('confirm(') && !attachmentOpenHandler.includes('prompt('), 'Opening asset attachment removal must not use native dialogs or mutate data.');
requireSource(attachmentRemoveHandler.length > 0 && attachmentRemoveHandler.includes('if (!assetAttachmentRemoveTarget || !canWriteAssets()) return;') && attachmentRemoveHandler.includes('`/api/assets/${target.assetId}/attachments/${target.attachmentId}`') && attachmentRemoveHandler.includes("method: 'DELETE'") && attachmentRemoveHandler.includes('closeAssetAttachmentRemove();') && attachmentRemoveHandler.includes('feedback.hidden = false;'), 'Asset attachment deletion may run only after in-app submission, and errors must stay visible without closing the form.');
requireSource(app.includes("panel.style.display = active ? 'block' : 'none'"), 'The selected asset-detail panel must be explicitly visible, with inactive panels hidden.');
requireSource(app.includes('panel.setAttribute(\'aria-hidden\', String(!active))'), 'Asset-detail panel accessibility state must follow the active tab.');
requireSource(/button\.addEventListener\('click',\s*\(\)\s*=>\s*\{\s*\$\('#assetDialog'\)\.close\(\);\s*applyAssetRelationshipFilter\(type, value, label\);\s*\}\);/.test(app), 'Overview reference shortcuts must close the detail dialog and apply the matching company-scoped asset filter.');
requireSource(app.includes("await navigator.clipboard.writeText(currentAsset.assetTag); notify('Varlık etiketi panoya kopyalandı.');") && app.includes("catch { notify('Etiket kopyalanamadı. Tarayıcı pano iznini kontrol edin.'); }"), 'Copy-tag shortcut must report both clipboard success and failure to the user.');
requireSource(app.includes('.asset-detail-dialog[open]{display:flex;flex-direction:column;') && !app.includes('.asset-detail-dialog{display:flex;') && app.includes('.asset-dialog-body{min-width:0;min-height:0;width:100%;flex:1;max-height:none;'), 'Only an open asset-detail dialog may use the flex layout, with a scrollable panel between its header and actions.');
requireSource(app.includes('@media(max-width:430px){.asset-detail-dialog[open]{width:calc(100% - 16px)}') && app.includes('.asset-detail-grid{grid-template-columns:minmax(0,1fr);gap:7px}') && app.includes('.asset-dialog-body{overflow-x:hidden}'), 'Narrow asset-detail dialogs must use a single readable detail column without a second horizontal scrollbar.');
for (const copy of [
  '“{assetTag}” varlığını çöp kutusuna taşımak istiyor musunuz?',
  'Çöp kutusu notu (isteğe bağlı):',
  'Varlık çöp kutusuna taşındı.',
]) requireSource(app.includes(copy), `Missing localized asset-trash UI copy: ${copy}`);
for (const copy of [
  'Durum değişikliğini onayla', 'Durum değişikliği notu (isteğe bağlı):', 'Durumu güncelle',
  'Arşivden geri almayı onayla', 'Geri alma notu (isteğe bağlı):', 'Arşivden geri al',
  '“{assetTag}” varlığının durumu “{status}” olarak değiştirilsin mi?',
  '“{assetTag}” varlığı arşivden geri alınarak kullanılabilir duruma getirilsin mi?',
  'Varlık durumu güncellendi ve denetim geçmişine yazıldı.', 'Durum güncellenemedi. Lütfen yeniden deneyin.'
]) requireSource(app.includes(copy), `Missing localized asset-status UI copy: ${copy}`);
for (const copy of [
  'Varlık dosyasını kaldır', 'Dosyayı kaldır', '“{fileName}” dosyası bu varlıktan kaldırılsın mı?',
  'Dosya kaldırılamadı. Lütfen yeniden deneyin.', 'Dosya kaldırıldı, ancak ekler yenilenemedi. Varlık ayrıntısını yeniden açın.'
]) requireSource(app.includes(copy), `Missing localized asset-attachment removal copy: ${copy}`);

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${tabs.length} asset-detail tabs and ${actions.length} asset-detail actions; asset trash is role-gated, localized, bound to the tenant API, and covered by the security acceptance suite.`);
}
