import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const loaders = [
  ['loadAssetReports', 'assetReportRequestId', "api(`/api/reports/assets"],
  ['loadComponentReport', 'componentReportRequestId', "api('/api/reports/components')"],
  ['loadAuditReport', 'auditReportRequestId', 'api(`/api/reports/audits?${params}`)'],
  ['loadMaintenanceReport', 'maintenanceReportRequestId', 'api(`/api/reports/maintenances']
];

for (const [name, token, apiCall] of loaders) {
  const start = html.indexOf(`async function ${name}(`);
  assert.ok(start >= 0, `${name} must exist`);
  const next = html.indexOf('\n    async function ', start + 1);
  const body = html.slice(start, next < 0 ? html.length : next);
  assert.ok(body.includes(`++${token}`), `${name} must assign each request an increasing token`);
  assert.ok(body.includes(`if (requestId !== ${token}) return`), `${name} must ignore stale results`);
  assert.ok(body.includes(`if (requestId === ${token}) throw error; return;`), `${name} must suppress stale errors but surface the current error`);
  assert.ok(body.includes(apiCall), `${name} must call its reports API`);
}

const audit = html.slice(html.indexOf('async function loadAuditReport('), html.indexOf('function auditReportQuery('));
for (const id of ['auditReportFrom', 'auditReportTo', 'auditReportSearch']) {
  assert.ok(audit.includes(`$('#${id}').value`), `physical audit report must apply ${id}`);
}
for (const id of ['auditReportApply', 'auditReportPrev', 'auditReportNext']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired`);
}

const assetPurchaseReport = html.slice(html.indexOf('async function loadAssetPurchaseReport('), html.indexOf('function exportAssetPurchaseReport('));
assert.ok(html.includes('assetPurchaseReportRequestId = 0'), 'asset purchase report request state must be initialized');
assert.ok(assetPurchaseReport.includes('++assetPurchaseReportRequestId'), 'asset purchase report must advance its request version');
assert.ok(assetPurchaseReport.includes('if (requestId !== assetPurchaseReportRequestId) return'), 'asset purchase report must ignore stale results');

const maintenance = html.slice(html.indexOf('async function loadMaintenanceReport('), html.indexOf('function warrantyReportParams('));
for (const id of ['maintenanceReportFrom', 'maintenanceReportTo']) {
  assert.ok(maintenance.includes(`$('#${id}').value`), `maintenance report must apply ${id}`);
}
for (const id of ['applyMaintenanceReportFilter', 'clearMaintenanceReportFilter', 'maintenanceReportRefresh']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired`);
}

const reportDateFormatter = html.slice(html.indexOf('function formatReportDate('), html.indexOf('function pendingDeliveryParams('));
assert.ok(reportDateFormatter.includes('new Date(`${String(value).slice(0, 10)}T12:00:00Z`)'), 'shared report date formatter must parse date-only values at UTC noon to avoid timezone day shifts');
assert.ok(reportDateFormatter.includes('timeZone: \'UTC\''), 'shared report date formatter must display date-only values consistently across client timezones');
assert.ok(reportDateFormatter.includes('if (!Number.isFinite(date.getTime())) return \'—\''), 'shared report date formatter must tolerate malformed or missing server values');
for (const [name, body] of [
  ['pending-delivery', html.slice(html.indexOf('async function loadPendingDeliveryReport('), html.indexOf('async function openPendingDeliveryOrder('))],
  ['depreciation', html.slice(html.indexOf('async function loadDepreciationReport('), html.indexOf('function exportDepreciationReport('))]
]) {
  assert.ok(body.includes('formatReportDate('), `${name} report must use the shared date formatter`);
  assert.ok(!/\bformatDate\s*\(/.test(body), `${name} report must not call an undefined local formatDate function`);
}

for (const id of ['applyReportDateFilter', 'clearReportDateFilter', 'reportRefresh']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired`);
}
console.log('Verified prepared report request ordering, shared timezone-safe date formatting, current-error handling, date/search filters, paging, refresh, and report API wiring.');
