import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const app = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];

function requireSource(condition, message) {
  if (!condition) failures.push(message);
}

requireSource(app.includes("$('#importPreviewForm').addEventListener('submit'"), 'The import preview form must have a submit handler.');
requireSource(app.includes("api(isAssetImport ? `/api/assets/import/preview.${isXlsx ? 'xlsx' : 'csv'}` : '/api/company/import/preview'"), 'CSV/XLSX validation must call the asset preview API, while JSON uses its own preview route.');
requireSource(app.includes('preview.rowCount ?? preview.counts?.assets ?? 0'), 'Asset preview must display the count returned by the API when rowCount is omitted.');
requireSource(app.includes("$('#applyCsvImport').hidden = !(isAssetImport && preview.isValid)"), 'The apply action must remain hidden until a valid asset preview.');
requireSource(app.includes("$('#applyCompanyImport').hidden = !(lowerName.endsWith('.json') && preview.isValid && preview.restoreTargetEmpty === true)"), 'Company restore must require a valid JSON preview and empty target.');
requireSource(app.includes("$('#importPreviewFile').addEventListener('change', () => { $('#applyCsvImport').hidden = true; $('#applyCompanyImport').hidden = true;"), 'Selecting another file must hide any prior apply action and preview result.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified asset import preview routes, accurate row-count fallback, and explicit valid-preview gating before any apply action.');
}
