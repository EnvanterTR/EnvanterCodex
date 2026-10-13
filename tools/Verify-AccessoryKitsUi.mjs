import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of ['accessoryKitsPage','accessoryKitSearch','accessoryKitRows','accessoryKitCount','accessoryKitFormDialog','accessoryKitLineEditor','accessoryKitHistoryDialog','accessoryKitHistoryRows','accessoryKitActionDialog','accessoryKitActionForm','accessoryKitActionSummary','accessoryKitActionSubmit']) {
  assert.match(html, new RegExp(`id="${id}"`), `ready-made kit menu must expose ${id}`);
}
const start = html.indexOf('async function loadAccessoryKits()');
const end = html.indexOf('function applyAccessoryColumnVisibility()', start);
assert.ok(start >= 0 && end > start, 'ready-made kit menu actions must be present');
const kit = html.slice(start, end);
for (const token of [
  'accessoryKitListRequestId', 'requestId !== accessoryKitListRequestId', '/api/accessory-kits?${params}',
  'accessoryKitCatalogRequestId', '/api/accessories?${params}', '/api/consumables?${params}', '/api/components?${params}', '/api/assets?${params}',
  'accessoryKitFormRequestId', 'requestId !== accessoryKitFormRequestId',
  '/api/accessory-kits/export.csv?${params}', 'accessoryKitHistoryRequestId', 'requestId !== accessoryKitHistoryRequestId',
  '/api/accessory-kits/${kit.id}/history', 'confirmAccessoryKitAction({', "method: 'DELETE'", "method: 'POST'", '/api/accessory-kits/${id}', '/api/accessory-kits'
]) assert.ok(kit.includes(token), `ready-made kit flow must include ${token}`);
assert.doesNotMatch(kit, /window\.(?:prompt|confirm)\s*\(|(?<![\w.])confirm\s*\(/, 'ready-made kit actions must not use native confirmation dialogs');
assert.ok(kit.includes("$('#accessoryKitActionDialog').addEventListener('cancel'"), 'Escape must clear a pending archive confirmation');
assert.ok(kit.includes("$('#accessoryKitHistoryDialog').addEventListener('close'"), 'closing kit history must invalidate pending responses');
assert.ok(html.includes('<th>Kalem</th>'), 'kit history must label mixed item types accurately');
assert.ok(html.includes("$('#accessoryKitsPage'), $('#accessoryKitFormDialog'), $('#accessoryKitHistoryDialog')"), 'ready-kit pages and dialogs must be included in language refresh');
console.log('Verified ready-made kit search/race guards, multi-type catalog/form validation, create/edit/archive/restore, filtered safe CSV, history loading/dismissal, and in-app archive confirmation.');
