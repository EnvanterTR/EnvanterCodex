import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');

function section(startMarker, endMarker, label) {
  const start = html.indexOf(startMarker);
  const end = html.indexOf(endMarker, start);
  assert.ok(start >= 0 && end > start, `${label} source block must exist`);
  return html.slice(start, end);
}

function assertCancellableBeforeApi(block, apiPath, label) {
  const confirm = block.indexOf('confirmEmployeeAction(');
  const request = block.indexOf(apiPath);
  assert.ok(confirm >= 0, `${label} must use an in-app confirmation`);
  assert.ok(request > confirm, `${label} must wait for confirmation before writing`);
  assert.doesNotMatch(block, /\b(?:window\.)?confirm\s*\(/, `${label} must not use a browser-native prompt`);
}

const consumableProfile = section("$('#deleteConsumableImportProfile').addEventListener('click'", "$('#applyConsumableImportMapping').addEventListener", 'consumable mapping profile removal');
assertCancellableBeforeApi(consumableProfile, '/api/consumables/import-mappings/${encodeURIComponent(id)}', 'consumable mapping profile removal');
assert.ok(consumableProfile.includes('resetConsumableImport()'), 'confirmed consumable profile removal must clear the stale preview');

const consumableCommit = section("$('#commitConsumableImport').addEventListener('click'", "$('#exportConsumablesButton').addEventListener", 'consumable import commit');
assertCancellableBeforeApi(consumableCommit, '/api/consumables/import.${upload.format}', 'consumable import commit');
assert.ok(consumableCommit.indexOf('!consumableImportState.valid') < consumableCommit.indexOf('confirmEmployeeAction('), 'consumable commit must revalidate the exact previewed file first');
assert.match(consumableCommit, /confirmEmployeeAction\([\s\S]*?\)\) return;\s*const button/, 'cancelled consumable import must exit before disabling or clearing the prepared preview');
assert.ok(consumableCommit.includes('consumableImportState.upload = null') && consumableCommit.includes('loadConsumables(1)'), 'successful consumable import must clear its preview and reload the list');

const accessoryProfile = section("$('#deleteAccessoryImportProfile').addEventListener('click'", "$('#accessoryImportProfile').addEventListener('change'", 'accessory mapping profile removal');
assertCancellableBeforeApi(accessoryProfile, '/api/accessories/import-mappings/${encodeURIComponent(id)}', 'accessory mapping profile removal');

const accessoryCommit = section("$('#commitAccessoryImport').addEventListener('click'", "$('#accessoryExport').addEventListener", 'accessory import commit');
assertCancellableBeforeApi(accessoryCommit, '/api/accessories/import.${upload.format}', 'accessory import commit');
assert.ok(accessoryCommit.indexOf('!accessoryImportState.valid') < accessoryCommit.indexOf('confirmEmployeeAction('), 'accessory commit must revalidate the exact previewed file first');
assert.match(accessoryCommit, /confirmEmployeeAction\([\s\S]*?\)\) return;\s*const button/, 'cancelled accessory import must exit before disabling or clearing the prepared preview');
assert.ok(accessoryCommit.includes('accessoryImportState.upload = null') && accessoryCommit.includes('loadAccessories(1)'), 'successful accessory import must clear its preview and reload the list');

console.log('Verified consumable/accessory import profile deletion and commit confirmations; cancellation preserves previews and success refreshes imported lists.');
