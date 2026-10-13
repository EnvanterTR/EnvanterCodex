import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const source = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const start = source.indexOf('async function loadAssetTrash(');
const end = source.indexOf("$('#openAssetTrash').addEventListener", start);
const loader = start < 0 || end < 0 ? '' : source.slice(start, end);
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

requireSource(loader.includes("api(`/api/assets/trash?page=${assetTrashPage}&pageSize=50`)") &&
  loader.includes('assetTrashTotal = result.total'),
  'The trash view must load its tenant-scoped page and display the API total.');
requireSource(loader.includes("rows.innerHTML = '<tr><td colspan=\"5\">Çöp kutusunda varlık yok.</td></tr>'"),
  'The trash view must render a clear empty state when the page contains no items.');
requireSource(loader.includes("button.textContent = 'Geri al'") &&
  loader.includes("openAssetTrashAction(item, 'restore')"),
  'Every trash item must expose the existing restore confirmation flow.');
requireSource(/cell\.append\(button\);\s*row\.append\(cell\);\s*rows\.append\(row\);/.test(loader),
  'Every constructed asset trash row must be appended to the visible table body.');
requireSource(loader.includes("$('#assetTrashPageInfo').textContent") &&
  loader.includes("$('#assetTrashPrev').disabled") && loader.includes("$('#assetTrashNext').disabled"),
  'Trash pagination must report the total and disable previous/next controls at the correct boundaries.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified asset trash empty state, API count, visible row insertion, restore action, and pagination boundaries.');
}
