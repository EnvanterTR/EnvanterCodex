import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const rowBuilder = source.match(/function addAssetRow\(asset\)\s*\{[\s\S]*?(?=\n\s*function |\n\s*async function )/)?.[0] ?? '';
requireSource(rowBuilder.length > 0, 'Asset row builder was not found.');
requireSource(rowBuilder.includes("event.stopPropagation(); callback();"), 'Row action clicks must not also trigger the row detail click.');
requireSource(rowBuilder.includes("row.addEventListener('click', () => openAsset(asset.id)"), 'Clicking a row must open the matching asset.');
requireSource(rowBuilder.includes('selectedAssetIds.size >= 100'), 'Bulk selection must retain the 100-asset safety limit.');

const actions = [
  ['detail', "action(translateUiText('Ayrıntıyı aç'), 'view'", 'openAsset(asset.id)'],
  ['assign-return', "asset.assignedEmployeeId ? 'return' : 'assign'", "$('#returnAssetButton') : $('#assignAssetButton')"],
  ['physical-audit', "action(translateUiText('Fiziksel denetim kaydı aç'), 'audit'", "$('#auditAssetButton').click()"],
  ['print-label', "action(translateUiText('Envanter etiketini yazdır'), 'label'", 'printSingleAssetLabel('],
  ['clone', "action(translateUiText('Varlığı kopyala'), 'clone'", 'openAssetClone(asset)'],
  ['edit', "action(translateUiText('Varlığı düzenle'), 'edit'", "$('#editAssetButton').click()"],
  ['trash', "action(translateUiText('Varlığı arşive taşı'), 'archive'", "const button = $('#trashAssetButton'); if (!button.hidden) button.click()"],
];
for (const [name, ...contracts] of actions) {
  for (const contract of contracts) requireSource(rowBuilder.includes(contract), `Asset row shortcut ${name} is missing source contract: ${contract}.`);
}
requireSource(rowBuilder.includes('if (canWriteAssets()) {'), 'Write-only asset row shortcuts must be gated by the current role.');
requireSource(rowBuilder.includes("checkoutButton.disabled = !canWriteAssets() || asset.status === 'Archived'"), 'Checkout shortcut must respect write permission and archived status.');

const singleLabelPrinter = source.match(/async function printSingleAssetLabel\(asset\)\s*\{[\s\S]*?(?=\n\s*function |\n\s*async function )/)?.[0] ?? '';
const labelPreview = source.match(/function openAssetLabelPreview\(cards, settings, sheetSettings = defaultLabelSheetSettings\)\s*\{[\s\S]*?(?=\n\s*function |\n\s*async function )/)?.[0] ?? '';
const bulkLabelPrinter = source.match(/\$\('#printSelectedAssetLabels'\)\.addEventListener\('click',[\s\S]*?(?=\n\s*\$\('#printAssetLabel'\))/)?.[0] ?? '';
requireSource(singleLabelPrinter.includes('openAssetLabelPreview(') && !singleLabelPrinter.includes('window.open('), 'Single asset labels must preview in-app without relying on popup permissions.');
requireSource(labelPreview.includes("dialog.showModal()") && labelPreview.includes('composeSelectedLabelPrint('), 'Single and sheet labels must use the shared in-app print preview.');
requireSource(bulkLabelPrinter.includes('openAssetLabelPreview(') && !bulkLabelPrinter.includes('window.open('), 'Bulk asset labels must preview in-app without relying on popup permissions.');
requireSource(source.includes("$('#printAssetLabel').addEventListener('click', () => { if (currentAsset) void printSingleAssetLabel(currentAsset); });"), 'Asset-detail label printing must use the same in-app preview as the row shortcut.');
requireSource(source.includes("$('#assetLabelPreviewPrint').addEventListener('click', () => { const frame = $('#assetLabelPreviewFrame'); if (!$('#assetLabelPreviewPrint').disabled) frame.contentWindow?.print(); });"), 'The in-app label preview must print from its explicit print button.');
const mainScriptStart = source.indexOf("const toast = document.getElementById('toast')");
for (const id of ['assetLabelPreviewDialog', 'assetLabelPreviewTitle', 'assetLabelPreviewFrame', 'assetLabelPreviewPrint', 'assetLabelPreviewClose']) {
  const markupPosition = source.indexOf(`id="${id}"`);
  requireSource(markupPosition >= 0 && mainScriptStart >= 0 && markupPosition < mainScriptStart, `Label preview element ${id} must exist in the DOM before handlers are registered.`);
}

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${actions.length} asset-row shortcuts: each has a destination, write-only controls are role-gated, and row clicks remain separate from action clicks.`);
}
