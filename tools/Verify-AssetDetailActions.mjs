import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const appPath = path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html');
const apiPath = path.join(root, 'src/EnvanterTR.Web/Data/OperationalEndpoints.cs');
const acceptancePath = path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs');
const app = fs.readFileSync(appPath, 'utf8');
const api = fs.readFileSync(apiPath, 'utf8');
const acceptance = fs.readFileSync(acceptancePath, 'utf8');
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
requireSource(app.includes('async function removeAssetAttachment') && app.includes("method: 'DELETE'"), 'Attachment removal must use the delete API and refresh the detail list.');
requireSource(app.includes("document.querySelectorAll('[data-asset-tab]')"), 'Asset-detail tabs must have a bound navigation handler.');
requireSource(app.includes('id="quickLookupDialog"') && app.includes("quickLookupDialog.showModal(); form.elements.name.focus();") && app.includes("api(`/api/lookups/${kind}`"), 'Category, brand, and model quick-add must use the in-app form and submit to the tenant lookup API.');
requireSource(app.includes('id="returnAssetDialog"') && app.includes("returnAssetDialog.showModal();") && app.includes("api(`/api/assets/${currentAsset.id}/return`"), 'Asset return must use the in-app confirmation/note form and submit to the tenant return API.');
requireSource(app.includes('id="assetTrashConfirmDialog" aria-labelledby="assetTrashConfirmTitle"') &&
  app.includes('id="assetTrashConfirmForm"') && app.includes('id="assetTrashConfirmSummary"') &&
  app.includes('name="note" maxlength="1000" rows="3"') &&
  app.includes("assetTrashConfirmDialog.showModal();") &&
  app.includes("assetTrashConfirmDialog.addEventListener('click', event => { if (event.target === assetTrashConfirmDialog) closeAssetTrashConfirm(); })"),
  'Asset trash must use an accessible in-app confirmation/note dialog that can be cancelled or dismissed without submitting.');
const quickLookupHandler = app.slice(app.indexOf("document.querySelectorAll('[data-quick-add]')"), app.indexOf("$('#newAssetForm').addEventListener('submit'"));
const detailReturnHandler = app.slice(app.indexOf("$('#returnAssetButton').addEventListener('click'"), app.indexOf("$('#trashAssetButton').addEventListener('click'"));
requireSource(quickLookupHandler.length > 0 && !quickLookupHandler.includes('window.prompt('), 'Asset quick-add must not depend on unsupported native prompt dialogs.');
requireSource(detailReturnHandler.length > 0 && !detailReturnHandler.includes('confirm(') && !detailReturnHandler.includes('prompt('), 'Asset detail return must use its in-app confirmation/note form instead of native dialogs.');
const trashButtonHandler = app.slice(app.indexOf("$('#trashAssetButton').addEventListener('click'"), app.indexOf("$('#assetStatusAction').addEventListener('change'"));
const trashSubmitHandler = app.slice(app.indexOf("$('#assetTrashConfirmForm').addEventListener('submit'"), app.indexOf("$('#assetStatusAction').addEventListener('change'"));
requireSource(trashButtonHandler.length > 0 && trashButtonHandler.includes("openAssetTrashAction(currentAsset, 'trash')") && !trashButtonHandler.includes('confirm(') && !trashButtonHandler.includes('prompt('), 'Opening asset trash confirmation must not trigger native browser dialogs or mutate data.');
requireSource(app.includes("openAssetTrashAction(item, 'restore')") && app.includes("`/api/assets/${target.id}/restore-from-trash`") && !app.slice(app.indexOf('async function loadAssetTrash'), app.indexOf("$('#openAssetTrash').addEventListener")).includes('prompt('), 'Restore from trash must use the same accessible in-app note dialog and avoid native prompt dialogs.');
requireSource(trashSubmitHandler.length > 0 && trashSubmitHandler.includes('if (!assetTrashTarget || !canWriteAssets()) return;') && trashSubmitHandler.includes('`/api/assets/${target.id}/trash`') && trashSubmitHandler.includes('method: \'POST\'') && trashSubmitHandler.includes('closeAssetTrashConfirm();') && trashSubmitHandler.includes('await loadDashboard('), 'Only explicit in-app confirmation may post the tenant-scoped trash action, and success must refresh the dashboard.');
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

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${tabs.length} asset-detail tabs and ${actions.length} asset-detail actions; asset trash is role-gated, localized, bound to the tenant API, and covered by the security acceptance suite.`);
}
