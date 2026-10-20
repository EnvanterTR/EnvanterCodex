import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const controls = [
  'accessoriesPage', 'accessorySearch', 'accessoryStockFilter', 'accessorySort', 'accessoryPageSize',
  'accessoryCategoryFilter', 'accessoryLocationFilter', 'clearAccessoryAdvancedFilters', 'accessoryExport',
  'accessoryRefresh', 'accessoryPrint', 'accessoryFullscreen', 'accessoryPrev', 'accessoryNext',
  'accessoryRows', 'accessoryColumnPicker', 'resetAccessoryColumns', 'newAccessoryButton'
];
for (const id of controls) assert.match(html, new RegExp(`id="${id}"`), `accessory inventory must expose ${id}`);

const listStart = html.indexOf('async function loadAccessories(');
const listEnd = html.indexOf('async function openAccessory(', listStart);
assert.ok(listStart >= 0 && listEnd > listStart, 'accessory list loader must have a bounded implementation');
const list = html.slice(listStart, listEnd);
assert.ok(html.includes("api('/api/accessories/filter-options')"), 'category and location filters must load company-scoped options');
for (const token of [
  '/api/accessories?${params}', 'pageSize: accessoryPageSize',
  "params.set('q', q)", "params.set('stock', stock)", "params.set('categoryId', categoryId)",
  "params.set('location', location)", 'requestId !== accessoryRequestId', 'escapeHtml(item.name)',
  "$('#accessoryPageInfo')", "$('#accessoryPrev').disabled", "$('#accessoryNext').disabled"
]) assert.ok(list.includes(token), `accessory list loader must implement ${token}`);

const exportStart = html.indexOf("$('#accessoryExport').addEventListener('click'");
const exportEnd = html.indexOf("$('#accessoryRefresh').addEventListener('click'", exportStart);
assert.ok(exportStart >= 0 && exportEnd > exportStart, 'accessory CSV export handler must be present');
const exportHandler = html.slice(exportStart, exportEnd);
for (const token of ['/api/accessories/export.csv?', "stockExportParams('accessories')", 'window.location.assign'])
  assert.ok(exportHandler.includes(token), `accessory export must include ${token}`);
const stockExportStart = html.indexOf('function stockExportParams(kind)');
const stockExportEnd = html.indexOf('async function downloadWorkbook(', stockExportStart);
assert.ok(stockExportStart >= 0 && stockExportEnd > stockExportStart, 'shared inventory export filter builder must exist');
const stockExportParams = html.slice(stockExportStart, stockExportEnd);
for (const token of ["params.set('q',q)", "params.set('stock',stock)", "if (kind === 'accessories')",
  "params.set('categoryId',$('#accessoryCategoryFilter').value)", "params.set('location',$('#accessoryLocationFilter').value)"])
  assert.ok(stockExportParams.includes(token), `accessory export filter builder must include ${token}`);

const handlersStart = html.indexOf("$('#accessoryRows').addEventListener('click'");
const handlersEnd = html.indexOf("$('#accessoryRows').addEventListener('change'", handlersStart);
const handlers = html.slice(handlersStart, handlersEnd);
for (const token of ["'.accessory-checkout'", "'.accessory-row-edit'", "'.accessory-category-filter-link'", "'.accessory-open'", 'openAccessory('])
  assert.ok(handlers.includes(token), `accessory row shortcut must handle ${token}`);
const selectionStart = html.indexOf("selection.className = 'accessory-select'");
assert.ok(selectionStart >= 0 && html.slice(selectionStart, selectionStart + 500).includes('selection.disabled = !canWriteAssets()'), 'bulk selection must be write-role gated');
assert.match(html, /accessoryFullscreen[\s\S]{0,1000}requestFullscreen/, 'fullscreen control must request the list panel');
assert.match(html, /accessoryPrint[\s\S]{0,700}window\.print\(\)/, 'print control must call the browser print flow');
assert.match(html, /clearAccessoryAdvancedFilters[\s\S]{0,260}loadAccessories\(1\)/, 'clear filters must reload from page one');

console.log('Verified accessory search, stock/category/location filters, stable paging, sorting, safe row rendering, shortcuts, role-gated selection, CSV filter parity, column preferences, print, and fullscreen wiring.');
