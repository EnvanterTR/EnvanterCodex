import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const rowBuilder = source.match(/function addAssetRow\(asset(?:,\s*rowNumber)?\)\s*\{[\s\S]*?(?=\n\s*function |\n\s*async function )/)?.[0] ?? '';
requireSource(rowBuilder.length > 0, 'Asset row builder was not found.');
requireSource(rowBuilder.includes("event.stopPropagation(); callback();"), 'Row action clicks must not also trigger the row detail click.');
requireSource(rowBuilder.includes("row.addEventListener('click', () => openAsset(asset.id)"), 'Clicking a row must open the matching asset.');
requireSource(rowBuilder.includes('selectedAssetIds.size >= 100'), 'Bulk selection must retain the 100-asset safety limit.');
requireSource(rowBuilder.includes("translateUiText('Görseli büyüt')") && rowBuilder.includes("translateUiText('Fotoğraf ve dosyaları aç')"), 'Image-present and image-missing shortcuts must describe their distinct destinations.');
requireSource(rowBuilder.includes("const openAssetFiles = async () => { try { await openAsset(asset.id); selectAssetTab('attachments'); }"), 'An asset without a photo must open the attachments tab for file management.');
requireSource(rowBuilder.includes("imageButton.addEventListener('click', event => { event.stopPropagation(); const visibleImage = imageButton.querySelector('img'); if (visibleImage?.currentSrc) showAssetImagePreview(visibleImage); else openAssetFiles(); });"), 'Keyboard activation must enlarge a present photo and fall back to files when unavailable.');
requireSource(source.includes('function showAssetImagePreview(image)') && source.includes("showAssetImagePreview(image);"), 'Mouse and keyboard image preview must share the in-app viewer.');

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

const cloneFunction = source.match(/async function openAssetClone\(asset\)\s*\{[\s\S]*?(?=\n\s*document\.querySelectorAll\('\[data-close-new\]'\))/)?.[0] ?? '';
requireSource(cloneFunction.includes('asset.defaultLocationCatalogId') && cloneFunction.includes('field.fieldType !== \'sensitive\''), 'Cloning must preserve the catalog location and ordinary custom fields without copying sensitive custom fields.');
if (cloneFunction) {
  const makeCloneContext = canWrite => {
    const names = ['assetName','assetTag','categoryId','brandId','modelId','serialNumber','location','defaultLocationCatalogId','purchaseDate','warrantyExpiresAt','purchaseCost','currency'];
    const elements = Object.fromEntries(names.map(name => [name, { value: `stale-${name}`, focus() {} }]));
    elements.isRequestable = { checked: false };
    elements.isByod = { checked: false };
    const form = { elements, reset() { for (const name of names) elements[name].value = ''; elements.isRequestable.checked = false; elements.isByod.checked = false; } };
    const calls = { lookups: 0, definitions: 0, rendered: null, modelId: null, opened: 0 };
    const dialog = { showModal() { calls.opened++; } };
    const assetCustomFieldDefinitions = [
      { key: 'ownerTeam', fieldType: 'text', isActive: true },
      { key: 'assetSecret', fieldType: 'sensitive', isActive: true },
      { key: 'retiredCode', fieldType: 'text', isActive: false },
    ];
    const context = {
      $: selector => selector === '#newAssetForm' ? form : null,
      canWriteAssets: () => canWrite,
      loadLookups: async () => { calls.lookups++; },
      loadAssetCustomFieldDefinitions: async () => { calls.definitions++; },
      renderAssetCustomFieldInputs: (_form, values) => { calls.rendered = values; },
      refreshModels: (modelId, target) => { calls.modelId = modelId; target.elements.modelId.value = modelId; },
      assetCustomFieldDefinitions,
      newAssetDialog: dialog,
    };
    return { functionValue: vm.runInNewContext(`(${cloneFunction})`, context), form, calls };
  };

  const cloneAsset = {
    name: 'U6 Pro', categoryId: 'category-1', brandId: 'brand-1', modelId: 'model-1',
    serialNumber: 'source-serial', location: 'İstanbul / Kat 2', defaultLocationCatalogId: 'location-2',
    purchaseDate: '2025-03-10', warrantyExpiresAt: '2028-03-10', purchaseCost: 12500, currency: 'TRY',
    isRequestable: true, isByod: false,
    customFields: { ownerTeam: 'Ağ Ekibi', assetSecret: 'must-not-copy', retiredCode: 'old' },
  };
  const allowed = makeCloneContext(true);
  await allowed.functionValue(cloneAsset);
  assert.equal(allowed.calls.lookups, 1);
  assert.equal(allowed.calls.definitions, 1);
  assert.equal(allowed.calls.opened, 1);
  assert.equal(allowed.form.elements.assetName.value, 'U6 Pro');
  assert.equal(allowed.form.elements.categoryId.value, 'category-1');
  assert.equal(allowed.form.elements.brandId.value, 'brand-1');
  assert.equal(allowed.form.elements.modelId.value, 'model-1');
  assert.equal(allowed.calls.modelId, 'model-1');
  assert.equal(allowed.form.elements.defaultLocationCatalogId.value, 'location-2');
  assert.equal(allowed.form.elements.serialNumber.value, '');
  assert.equal(allowed.form.elements.assetTag.value, '');
  assert.equal(JSON.stringify(allowed.calls.rendered), JSON.stringify({ ownerTeam: 'Ağ Ekibi' }));
  assert.equal(allowed.form.elements.isRequestable.checked, true);
  const denied = makeCloneContext(false);
  await denied.functionValue(cloneAsset);
  assert.equal(denied.calls.opened, 0);
  assert.equal(denied.calls.lookups, 0);
}

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${actions.length} asset-row shortcuts; clone preserves catalog data and non-sensitive fields, clears unique identifiers, omits secrets, and stays permission-gated.`);
}
