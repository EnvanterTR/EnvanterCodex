import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const ids = ['componentsPage','componentSearch','componentStockFilter','componentRows','componentCount','componentPrev','componentNext','componentDialog','componentActionDialog','componentActionForm','componentActionSummary','componentActionSubmit','componentImportDialog','componentImportFile','componentImportProfile','componentForm','componentStockForm','componentAllocateForm','componentRemoveForm','exportComponentsButton','componentXlsxExport'];
for (const id of ids) assert.match(html, new RegExp(`id="${id}"`), `component inventory must expose ${id}`);
const start = html.indexOf('let componentPage=');
const end = html.indexOf('let licensePage', start);
assert.ok(start >= 0 && end > start, 'component inventory actions must be present');
const component = html.slice(start, end);
for (const token of ['componentRequestSequence','componentDetailSequence','requestId!==componentRequestSequence','requestId!==componentDetailSequence','/api/components?${p}',"/api/components/export.csv?'+stockExportParams('components')",'/api/components/bulk-stock-adjustment','/api/components/bulk-allocate','/api/components/import/preview.${upload.format}','/api/components/import.${upload.format}','confirmComponentAction({title:']) assert.ok(component.includes(token), `component inventory flow must include ${token}`);
assert.ok(html.includes("$('#componentXlsxExport').addEventListener('click', () => downloadStockWorkbook('components'"), 'component Excel export must use the shared workbook downloader and current filters');
assert.doesNotMatch(component, /window\.(?:prompt|confirm)\s*\(|(?<![\w.])confirm\s*\(/, 'component actions must use in-app confirmation dialogs');
assert.ok(component.includes("$('#componentActionDialog').addEventListener('cancel'"), 'Escape must settle component action confirmation');
assert.ok(html.includes("$('#componentDialog').open)$('#componentDialog').showModal()"), 'opening an already-open component detail must not throw');
assert.ok(html.includes("componentDetailSequence++;$('#componentDialog').close()"), 'closing component detail must invalidate pending responses');
console.log('Verified component list/search/stock filters/paging/export, stale-response guards, detail actions, bulk stock/allocation, import preview/profile/commit, and in-app confirmations.');
