import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const endpoints = await readFile(new URL('../src/EnvanterTR.Web/Data/CustomAssetReportEndpoints.cs', import.meta.url), 'utf8');

const fieldsStart = html.indexOf('async function loadCustomReportFields(');
const fieldsEnd = html.indexOf('function readCustomReportDraft(', fieldsStart);
const fields = html.slice(fieldsStart, fieldsEnd);
assert.ok(fieldsStart >= 0 && fieldsEnd > fieldsStart, 'custom field loader must exist');
assert.ok(fields.includes('const sessionKey = customReportSettingsKey'), 'custom field load must capture its company/user scope');
assert.ok(fields.includes('++customReportFieldsRequestId'), 'custom field load must have request ordering');
assert.ok(fields.includes('requestId !== customReportFieldsRequestId || sessionKey !== customReportSettingsKey'), 'stale custom field response and errors must be ignored after a session change');
assert.ok(fields.includes("host.querySelectorAll('[data-custom-report-field]')"), 'company changes must remove old dynamic columns');
assert.ok(fields.includes("item.fieldType !== 'sensitive'"), 'sensitive fields must not appear in report column choices');

const presetsStart = html.indexOf('async function loadCustomReportPresets(');
const presetsEnd = html.indexOf('async function saveCustomReportPreset(', presetsStart);
const presets = html.slice(presetsStart, presetsEnd);
assert.ok(presetsStart >= 0 && presetsEnd > presetsStart, 'custom preset loader must exist');
assert.ok(presets.includes('const sessionKey = customReportSettingsKey, requestId = ++customReportPresetsRequestId'), 'preset load must capture company/user scope and ordering');
assert.ok(presets.includes('if (requestId !== customReportPresetsRequestId || sessionKey !== customReportSettingsKey) return'), 'stale preset loads and migration steps must stop');
assert.ok(presets.includes('localStorage.removeItem(sessionKey)'), 'legacy preset removal must target the captured account key');

for (const [name, selector] of [
  ['save', 'customReportSavePreset'], ['load', 'customReportLoadPreset'], ['delete', 'customReportDeletePreset'], ['export', 'customReportExport']
]) {
  assert.ok(html.includes(`$('#${selector}').addEventListener('click'`), `custom report ${name} action must be wired`);
}
const exportStart = html.indexOf('async function exportCustomAssetReport(');
const exportEnd = html.indexOf('function reportPrintMetadata(', exportStart);
assert.ok(exportStart >= 0 && exportEnd > exportStart, 'custom report export handler must exist');
const exportHandler = html.slice(exportStart, exportEnd);
for (const token of ["new URLSearchParams({ columns: draft.columns.join(','), sortBy: draft.sortBy, sortDirection: draft.sortDirection })",
  'if (draft.q) params.set(\'q\', draft.q)', 'if (draft.status) params.set(\'status\', draft.status)',
  'if (draft.warranty) params.set(\'warranty\', draft.warranty)', 'api(`/api/assets/export-all.${format}?${params}`)'])
  assert.ok(exportHandler.includes(token), `custom report export must include ${token}`);
assert.ok(html.includes("const fileStem = (draft.name || 'envanter-varlik-raporu')"), 'custom report download must use a sanitized report filename');
assert.ok(endpoints.includes('.RequireAuthorization("asset.read")'), 'custom report presets must require asset read permission');

console.log('Verified custom report preset actions, safe field columns, company/user session isolation, stale request protection, and CSV export wiring.');
