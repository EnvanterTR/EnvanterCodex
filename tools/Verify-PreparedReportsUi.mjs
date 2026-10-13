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

const maintenance = html.slice(html.indexOf('async function loadMaintenanceReport('), html.indexOf('function warrantyReportParams('));
for (const id of ['maintenanceReportFrom', 'maintenanceReportTo']) {
  assert.ok(maintenance.includes(`$('#${id}').value`), `maintenance report must apply ${id}`);
}
for (const id of ['applyMaintenanceReportFilter', 'clearMaintenanceReportFilter', 'maintenanceReportRefresh']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired`);
}

for (const id of ['applyReportDateFilter', 'clearReportDateFilter', 'reportRefresh']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired`);
}
console.log('Verified prepared report request ordering, current-error handling, date/search filters, paging, refresh, and report API wiring.');
