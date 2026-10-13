import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const ids = ['inventoryMovementsPage','inventoryMovementSearch','inventoryMovementKind','inventoryMovementType','inventoryMovementFrom','inventoryMovementTo','inventoryMovementRows','inventoryMovementCount','inventoryMovementPageInfo','inventoryMovementPrev','inventoryMovementNext','exportInventoryMovementsButton'];
for (const id of ids) assert.match(html, new RegExp(`id="${id}"`), `inventory movements must expose ${id}`);

const start = html.indexOf('let componentPage=');
const end = html.indexOf('let licensePage', start);
assert.ok(start >= 0 && end > start, 'inventory movement list actions must be present');
const movements = html.slice(start, end);
for (const token of [
  'inventoryMovementRequestSequence',
  'requestId!==inventoryMovementRequestSequence',
  'pageSize:50',
  '/api/inventory/movements?${p}',
  'inventoryMovementKind',
  'inventoryMovementType',
  'inventoryMovementFrom',
  'inventoryMovementTo',
  'movementLabels[m.movementType]',
  'escapeHtml(m.item)',
  'escapeHtml(m.target||',
  'escapeHtml(m.notes||',
  'inventoryMovementPageInfo',
]) assert.ok(movements.includes(token), `inventory movement list must include ${token}`);

const handlers = html.slice(html.indexOf("$('#inventoryMovementPrev').addEventListener"), html.indexOf("$('#inventoryMovementNext').addEventListener") + 400);
for (const token of ["loadInventoryMovements(inventoryMovementPage - 1)", "p.set('export','true')", "'/api/inventory/movements?'", "loadInventoryMovements(1)"]) assert.ok(handlers.includes(token), `inventory movement controls must include ${token}`);
assert.ok(handlers.includes("['q','inventoryMovementSearch']") && handlers.includes("['kind','inventoryMovementKind']") && handlers.includes("['movementType','inventoryMovementType']") && handlers.includes("['from','inventoryMovementFrom']") && handlers.includes("['to','inventoryMovementTo']"), 'CSV export must preserve active filters');
console.log('Verified movement list/filter/page/export wiring, stale-response protection, and escaped row rendering.');
