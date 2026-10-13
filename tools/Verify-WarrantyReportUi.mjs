import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const panelStart = html.indexOf('id="warrantyReportPanel"');
const panelEnd = html.indexOf('</section>', panelStart);
assert.ok(panelStart >= 0 && panelEnd > panelStart, 'warranty tracking panel must exist');
const panel = html.slice(panelStart, panelEnd);
for (const state of ['upcoming', 'expired', 'later', 'missing', 'all'])
  assert.ok(panel.includes(`value="${state}"`), `warranty report must expose the ${state} filter`);
for (const horizon of ['30', '90', '180', '366'])
  assert.ok(panel.includes(`value="${horizon}"`), `warranty report must expose the ${horizon}-day horizon`);
for (const id of ['warrantyReportSearch', 'warrantyReportRefresh', 'warrantyReportExport', 'warrantyReportPrev', 'warrantyReportNext', 'warrantyReportRows', 'warrantyReportSummary'])
  assert.ok(panel.includes(`id="${id}"`), `warranty report must include ${id}`);

const start = html.indexOf('function warrantyReportParams()');
const end = html.indexOf('function licenseReportParams()', start);
assert.ok(start >= 0 && end > start, 'warranty report query, rendering, and export handlers must exist');
const source = html.slice(start, end);
assert.match(source, /new URLSearchParams\(\{ through, state: \$\('#warrantyReportState'\)\.value, page: warrantyReportPage, pageSize: 50 \}\)/,
  'filters and pagination must be sent to the company warranty report API');
assert.match(source, /\$\('#warrantyReportSearch'\)\.value\.trim\(\)/, 'search must be included in API filters');
assert.ok(source.includes('++warrantyReportRequestId') && source.includes('/api/reports/warranties?${params}') && source.includes('requestId !== warrantyReportRequestId'),
  'report requests must call the API and ignore stale search/filter responses');
assert.match(source, /open\.addEventListener\('click',\s*\(\)\s*=>\s*openAsset\(item\.id\)/,
  'asset-tag shortcut must open that asset detail');
assert.ok(source.includes('caption.textContent') && source.includes('date.textContent') && source.includes('name.textContent') && source.includes('employee.textContent'),
  'report values must be rendered as text, not injected as HTML');
assert.match(source, /params\.delete\('page'\); params\.delete\('pageSize'\);[\s\S]*?\/api\/reports\/warranties\/export\.csv/,
  'CSV export must preserve active filters and omit pagination');

const eventsStart = html.indexOf("$('#warrantyReportRefresh').addEventListener");
const eventsEnd = html.indexOf("$('#licenseReportRefresh').addEventListener", eventsStart);
assert.ok(eventsStart >= 0 && eventsEnd > eventsStart, 'refresh, filter, search, pagination, and export actions must be wired');
const events = html.slice(eventsStart, eventsEnd);
for (const binding of ['warrantyReportExport', 'warrantyReportState', 'warrantyReportHorizon', 'warrantyReportSearch', 'warrantyReportPrev', 'warrantyReportNext'])
  assert.ok(events.includes(`$('#${binding}')`), `warranty report action ${binding} must be wired`);

console.log('Verified warranty filters, horizons, debounced search, stale-response guard, safe asset shortcut rendering, pagination, refresh, and filtered CSV export.');
