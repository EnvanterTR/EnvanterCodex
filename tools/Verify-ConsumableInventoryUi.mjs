import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of [
  'consumablesPage', 'consumableSearch', 'consumableStockFilter', 'consumableSort', 'consumableRows',
  'consumableCount', 'consumablePrev', 'consumableNext', 'consumablePageInfo', 'exportConsumablesButton',
  'importConsumablesButton', 'newConsumableButton', 'consumableDialog', 'consumableFormDialog',
  'consumableStockDialog', 'consumableCheckoutDialog', 'consumableImportDialog', 'consumableMovements'
]) assert.match(html, new RegExp(`id="${id}"`), `consumable inventory must expose ${id}`);

const listStart = html.indexOf('async function loadConsumables(');
const listEnd = html.indexOf('async function openConsumable(', listStart);
assert.ok(listStart >= 0 && listEnd > listStart, 'consumable inventory loader must be present');
const list = html.slice(listStart, listEnd);
for (const token of [
  'const requestId = ++consumableRequestId', '/api/consumables?${params}', "params.set('q', q)",
  "params.set('stock', stock)", 'requestId !== consumableRequestId', 'escapeHtml(item.name)',
  'consumable-checkout', 'consumable-row-edit', 'consumable-row-stock', 'canWriteAssets()',
  "$('#consumablePrev').disabled", "$('#consumableNext').disabled"
]) assert.ok(list.includes(token), `consumable list must implement ${token}`);

const detailStart = html.indexOf('async function openConsumable(');
const detailEnd = html.indexOf('function openNewConsumable(', detailStart);
const detail = html.slice(detailStart, detailEnd);
assert.ok(detail.includes('consumableDetailRequestId') && detail.includes('requestId !== consumableDetailRequestId'), 'detail requests must not overwrite a newer selection');
assert.ok(detail.includes("if (!$('#consumableDialog').open) $('#consumableDialog').showModal()"), 'refreshing an open detail must not throw by opening the dialog twice');
assert.ok(detail.includes('escapeHtml(') && detail.includes('consumableMovements'), 'detail fields and movement history must render safely');

const exportStart = html.indexOf("$('#exportConsumablesButton').addEventListener('click'");
const exportEnd = html.indexOf("$('#consumableRows').addEventListener('click'", exportStart);
const exportHandler = html.slice(exportStart, exportEnd);
for (const token of ['/api/consumables/export.csv?', "stockExportParams('consumables')", 'window.location.assign'])
  assert.ok(exportHandler.includes(token), `consumable CSV must preserve ${token}`);
const stockExportStart = html.indexOf('function stockExportParams(kind)');
const stockExportEnd = html.indexOf('async function downloadWorkbook(', stockExportStart);
const stockExportParams = html.slice(stockExportStart, stockExportEnd);
for (const token of ["params.set('q',q)", "params.set('stock',stock)"])
  assert.ok(stockExportParams.includes(token), `consumable export filter builder must include ${token}`);

const rowStart = html.indexOf("$('#consumableRows').addEventListener('click'");
const rowEnd = html.indexOf("$('#consumableSearch').addEventListener('input'", rowStart);
const rows = html.slice(rowStart, rowEnd);
for (const token of ["'.consumable-checkout'", "'.consumable-row-edit'", "'.consumable-row-stock'", 'openConsumable('])
  assert.ok(rows.includes(token), `consumable row shortcut must support ${token}`);

const mutationsStart = html.indexOf("$('#consumableForm').addEventListener('submit'");
const mutationsEnd = html.indexOf("$('#closeConsumableDetail').addEventListener", mutationsStart);
const mutations = html.slice(mutationsStart, mutationsEnd);
for (const token of [
  "'/api/consumables'", '/api/consumables/${currentConsumable.id}/stock',
  '/api/consumables/${currentConsumable.id}/checkout', 'await openConsumable(currentConsumable.id)',
  'await loadConsumables()'
]) assert.ok(mutations.includes(token), `consumable mutation/import flow must include ${token}`);
const importStart = html.indexOf('const consumableImportState =');
const importEnd = html.indexOf("$('#exportConsumablesButton').addEventListener('click'", importStart);
const importFlow = html.slice(importStart, importEnd);
for (const token of ['consumableImportState.valid', '/api/consumables/import/preview.${upload.format}', '/api/consumables/import.${upload.format}'])
  assert.ok(importFlow.includes(token), `consumable preview/apply import must include ${token}`);

console.log('Verified consumable search, stock filtering, stale-response guards, safe detail/movement rendering, row shortcuts, CSV parity, and create/stock/checkout/import refresh flows.');
