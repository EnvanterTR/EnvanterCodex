import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
function between(start, end) {
  const from = html.indexOf(start);
  assert.notEqual(from, -1, `Missing source marker: ${start}`);
  const to = html.indexOf(end, from + start.length);
  assert.notEqual(to, -1, `Missing end marker: ${start}`);
  return html.slice(from, to);
}

const modelAttachments = between("$('#modelAttachmentRows').addEventListener('click'", "$('#catalogCreateForm').addEventListener('submit'");
assert.match(modelAttachments, /const modelId = activeModelAttachmentId, attachmentId = button\?\.dataset\.modelAttachmentDelete/, 'Capture both selected model and attachment before awaiting confirmation');
assert.match(modelAttachments, /await confirmEmployeeAction\([\s\S]*?Bu model dosyası kaldırılsın mı\?/, 'Model attachment removal requires the in-app confirmation');
assert.match(modelAttachments, /\/api\/models\/\$\{modelId\}\/attachments\/\$\{attachmentId\}[\s\S]*?method: 'DELETE'/, 'Delete must use the captured model and attachment identity');
assert.doesNotMatch(modelAttachments, /\bconfirm\s*\(/, 'Model attachment removal must not use browser confirm');

const catalogActions = between("$('#catalogRows').addEventListener('click'", "$('#catalogSearch').addEventListener('input'");
assert.match(catalogActions, /item\.assetCount \|\| item\.modelCount[\s\S]*?await confirmEmployeeAction/, 'Only unused catalog values may proceed to a confirmed delete');
assert.match(catalogActions, /translateUiText\('“\{name\}” tanımı silinsin mi\?'\)[\s\S]*?method: 'DELETE'/, 'Catalog deletion must identify the record and call its scoped API');
assert.doesNotMatch(catalogActions, /\bconfirm\s*\(/, 'Catalog deletion must not use browser confirm');

const customFieldEdit = between("if (button.hasAttribute('data-custom-field-edit'))", "} else {");
assert.match(html, /id="editAssetCustomFieldForm"[\s\S]*?<input name="name" required maxlength="100"/, 'Custom field rename dialog must enforce the API name length');
assert.match(customFieldEdit, /editFieldForm\.elements\.name\.value = field\.name/, 'Custom field rename dialog must preload the selected field name');
assert.match(html, /data-close-custom-field-edit[\s\S]*?\$\('#editAssetCustomFieldDialog'\)\.close\(\)/, 'Cancel and close controls must dismiss the edit dialog without submitting');
assert.match(html, /\/api\/assets\/custom-fields\/\$\{field\.id\}[\s\S]*?method: 'PUT'/, 'Custom field edit form must update the selected field');
assert.doesNotMatch(customFieldEdit, /\bprompt\s*\(/, 'Custom field rename must not use browser prompt');

const customFieldEditor = between("function ensureAssetCustomFieldSettings()", "function renderAssetCustomFieldTable()");
assert.match(customFieldEditor, /name="categoryId"[\s\S]*?name="modelId"/, 'Create and edit field forms must expose category and model scope selectors');
assert.match(customFieldEditor, /categoryId: data\.get\('categoryId'\) \|\| null, modelId: data\.get\('modelId'\) \|\| null/, 'New scoped fields must persist both selector values');
assert.match(customFieldEditor, /categoryId: editFieldForm\.elements\.categoryId\.value \|\| null,[\s\S]*?modelId: editFieldForm\.elements\.modelId\.value \|\| null/, 'Editing a field must submit its chosen category and model scope');

const customFieldInputs = between('function renderAssetCustomFieldInputs(', 'const customFieldFormValues =');
assert.match(customFieldInputs, /field\.categoryId === categoryId\) && \(!field\.modelId \|\| field\.modelId === modelId\)/, 'Asset forms must show only fields that match both category and model');
assert.match(customFieldInputs, /field\.groupName[\s\S]*?asset-custom-field-group/, 'Asset forms must visibly group fields by the configured group name');
const scopeOptions = between('function populateAssetCustomFieldScopeOptions(', 'function renderAssetCustomFieldTable()');
assert.match(scopeOptions, /lookupCache\.models\.filter\(model => !categoryId \|\| !model\.categoryId \|\| model\.categoryId === categoryId\)/, 'Model choices must be constrained to the selected category when catalog data declares one');

const importProfile = between("$('#deleteAssetImportProfile').addEventListener('click'", 'function encodeAssetImportMapping(');
assert.match(importProfile, /await confirmEmployeeAction\([\s\S]*?Seçili eşleme profili silinsin mi\?/, 'Profile deletion requires the in-app confirmation');
assert.match(importProfile, /\/api\/assets\/import-mappings\/\$\{encodeURIComponent\(id\)\}[\s\S]*?method: 'DELETE'/, 'Profile deletion must stay scoped to the selected profile');
assert.doesNotMatch(importProfile, /\bconfirm\s*\(/, 'Profile deletion must not use browser confirm');

console.log('Catalog attachment, unused lookup, custom-field edit/group/category-model scope, and asset import-profile dialogs passed.');
