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
assert.match(customFieldEdit, /requestTextValue\([\s\S]*?initialValue:\s*field\.name[\s\S]*?maxLength:\s*100/, 'Custom field rename must preload the current value and honor API length');
assert.match(customFieldEdit, /if \(name === null \|\| name === field\.name\) return/, 'Cancel or unchanged value must not send an update');
assert.match(customFieldEdit, /\/api\/assets\/custom-fields\/\$\{field\.id\}[\s\S]*?method: 'PUT'/, 'Custom field rename must update the selected field');
assert.doesNotMatch(customFieldEdit, /\bprompt\s*\(/, 'Custom field rename must not use browser prompt');

const importProfile = between("$('#deleteAssetImportProfile').addEventListener('click'", 'function encodeAssetImportMapping(');
assert.match(importProfile, /await confirmEmployeeAction\([\s\S]*?Seçili eşleme profili silinsin mi\?/, 'Profile deletion requires the in-app confirmation');
assert.match(importProfile, /\/api\/assets\/import-mappings\/\$\{encodeURIComponent\(id\)\}[\s\S]*?method: 'DELETE'/, 'Profile deletion must stay scoped to the selected profile');
assert.doesNotMatch(importProfile, /\bconfirm\s*\(/, 'Profile deletion must not use browser confirm');

console.log('Catalog attachment, unused lookup, custom-field rename, and asset import-profile dialogs passed.');
