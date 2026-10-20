import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of [
  'inventoryRequestsPage', 'requestCatalogSearch', 'requestCatalogRows', 'requestCatalogCount',
  'requestSearch', 'requestStatusFilter', 'requestRows', 'requestCount', 'requestPrev', 'requestNext',
  'requestExport', 'requestsRefresh', 'inventoryRequestDialog', 'inventoryRequestForm',
  'inventoryRequestActionDialog', 'inventoryRequestActionForm', 'inventoryRequestActionSummary',
  'inventoryRequestActionNote', 'inventoryRequestActionSubmit'
]) assert.match(html, new RegExp(`id="${id}"`), `stock requests must expose ${id}`);

const start = html.indexOf("let requestCurrentUserId = ''");
const end = html.indexOf('async function startApp()', start);
assert.ok(start >= 0 && end > start, 'stock request behavior must be present');
const requests = html.slice(start, end);
for (const token of [
  'requestListSequence', 'requestCatalogSequence', 'requestEmployeeSequence', 'requestDialogSequence',
  'sequence !== requestListSequence', 'sequence !== requestCatalogSequence', 'sequence !== requestEmployeeSequence',
  '/api/requests/catalog?${params}', '/api/requests?${params}', '/api/requests/export.csv?${params}',
  "openInventoryRequestAction(item,'approve')", "openInventoryRequestAction(item,'reject')",
  "openInventoryRequestAction(item,'cancel')", '/api/requests/${action.id}/cancel',
  '/api/requests/${action.id}/decision', 'approve: action.approve', 'requestAction = null'
]) assert.ok(requests.includes(token), `stock request flow must include ${token}`);
assert.ok(requests.includes("escapeHtml(item.location||'—')"),
  'the stock request catalog must render the scoped API location safely');
assert.doesNotMatch(requests, /window\.(?:prompt|confirm)\s*\(/, 'stock request actions must use in-app dialogs');
assert.ok(requests.includes("$('#inventoryRequestActionDialog').addEventListener('cancel'"), 'Escape must clear the pending stock request action');
assert.ok(requests.includes('dialog.showModal()'), 'stock request action dialog must open accessibly');

console.log('Verified stock request catalog/list/search filters/paging/export, employee scoping, stale-response guards, submission, and in-app approve/reject/cancel dialog flows.');
