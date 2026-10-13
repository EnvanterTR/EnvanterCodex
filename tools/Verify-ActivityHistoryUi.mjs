import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const html = readFileSync(sourcePath, 'utf8');
const activityStart = html.indexOf('async function loadActivity(');
const activityEnd = html.indexOf('async function loadAssetCheckouts(', activityStart);
assert.ok(activityStart >= 0 && activityEnd > activityStart, 'activity history renderer must be present');
const activity = html.slice(activityStart, activityEnd);

for (const id of ['activitySearch', 'activityType', 'activityFrom', 'activityTo', 'activityTableRows', 'activityCount', 'activityPrev', 'activityNext', 'activityPageInfo']) {
  assert.match(html, new RegExp(`id="${id}"`), `activity history must expose ${id}`);
}
assert.match(activity, /new URLSearchParams\(\{ page: activityPage, pageSize \}\)/, 'activity history must request the selected page');
assert.match(activity, /if \(q\) params\.set\('q', q\)/, 'activity search must reach the API');
assert.match(activity, /if \(eventType\) params\.set\('eventType', eventType\)/, 'event type filter must reach the API');
assert.match(activity, /localDateIso\(\$\('#activityFrom'\)\.value\)/, 'date filter must use the local start of day');
assert.match(activity, /localDateIso\(\$\('#activityTo'\)\.value, true\)/, 'date filter must include the local end of day');
assert.match(activity, /cell\.textContent = value/, 'activity rows must render API text without interpreting it as HTML');
assert.match(activity, /row\.setAttribute\('role','button'\)/, 'asset-linked history rows must be exposed as buttons');
assert.match(activity, /event\.key==='Enter'\|\|event\.key===' '/, 'asset-linked history rows must support keyboard activation');
assert.match(activity, /openAsset\(entry\.assetId\)/, 'history rows must open their corresponding asset');
assert.match(activity, /activityPage \* pageSize >= activityTotal/, 'activity pagination must disable next on the last page');
assert.match(html, /activityPrev'\)\.addEventListener\('click', \(\) => loadActivity\(activityPage - 1\)/, 'previous-page control must load the prior activity page');
assert.match(html, /activityNext'\)\.addEventListener\('click', \(\) => loadActivity\(activityPage \+ 1\)/, 'next-page control must load the next activity page');
assert.match(html, /activitySearch'\)\.addEventListener\('input'/, 'activity search must reload after input');
assert.match(html, /activityType'\)\.addEventListener\('change'/, 'event type changes must reload the first page');
assert.match(html, /\$\('#activityFrom'\)\.addEventListener\('change', \(\) => loadActivity\(1\)/, 'start-date changes must reload the first page');
assert.match(html, /\$\('#activityTo'\)\.addEventListener\('change', \(\) => loadActivity\(1\)/, 'end-date changes must reload the first page');

const routeStart = html.indexOf("document.querySelectorAll('[data-page]')");
const routeEnd = html.indexOf("document.querySelectorAll('#dataImportPanel", routeStart);
const routes = html.slice(routeStart, routeEnd);
assert.match(routes, /page === 'Transferler'[\s\S]*?\$\('#activityType'\)\.value = 'Transferred'/, 'Transferler must preset its event type');
assert.match(routes, /page === 'Hareket geçmişi'[\s\S]*?\$\('#activityType'\)\.value = ''/, 'Hareket geçmişi must clear the transfer-only filter');

console.log('Verified activity history filters, local date boundaries, pagination, safe rendering, keyboard asset shortcuts, and menu presets.');
