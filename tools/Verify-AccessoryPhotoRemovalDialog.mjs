import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const html = (await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8')).replace(/\r\n/g, '\n');
const start = html.indexOf("if (item.photoUrl) { const remove = document.createElement('button')");
assert.notEqual(start, -1, 'Accessory photo removal action must exist');
const end = html.indexOf('\n      }\n      panel.append(preview, content);', start);
assert.notEqual(end, -1, 'Accessory photo section must be bounded');
const flow = html.slice(start, end);
const confirmAt = flow.indexOf('await confirmEmployeeAction(');
const deleteAt = flow.indexOf("method: 'DELETE'");
assert.ok(confirmAt >= 0 && deleteAt > confirmAt, 'Delete request must follow explicit in-app confirmation');
assert.match(flow, /summary:\s*translateUiText\('Aksesuar fotoğrafı kaldırılsın mı\?'\)/, 'Confirmation must identify the effect');
assert.match(flow, /confirmLabel:\s*'Fotoğrafı kaldır'/, 'Confirmation must label the destructive action');
assert.match(flow, /\/api\/accessories\/\$\{item\.id\}\/photo/, 'Delete must target the currently opened accessory photo');
assert.match(flow, /if \(!await confirmEmployeeAction\([\s\S]*?\)\) return;/, 'Cancellation must stop before the API request');
assert.doesNotMatch(flow, /\bconfirm\s*\(/, 'Do not use a browser-native confirmation');
assert.match(flow, /await openAccessory\(item\.id\); await loadAccessories\(accessoryPage\)/, 'Success must refresh detail and list');

console.log('Accessory photo removal confirmation, cancellation, selected-record scope, and refresh passed.');
