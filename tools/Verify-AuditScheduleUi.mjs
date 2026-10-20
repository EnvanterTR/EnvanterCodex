import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const scheduleStart = html.indexOf('async function loadAuditSchedule(');
const scheduleEnd = html.indexOf('async function loadAssignableEmployees(', scheduleStart);
assert.ok(scheduleStart >= 0 && scheduleEnd > scheduleStart, 'physical audit queue loader must exist');
const schedule = html.slice(scheduleStart, scheduleEnd);
assert.ok(schedule.includes('const requestId = ++auditScheduleRequestId'), 'queue loads must have a request ordering token');
assert.ok(schedule.includes('if (requestId !== auditScheduleRequestId) return'), 'stale queue results must not replace current filters');
assert.match(schedule, /catch \(error\) \{\s*if \(requestId !== auditScheduleRequestId\) return;[\s\S]*?throw error;\s*\}/, 'stale queue errors must be discarded before rendering or throwing current errors');
assert.ok(schedule.includes("showAuditScheduleStatus(translateUiText('Denetim kuyruğu yükleniyor…'))"), 'new queue requests must clear previous rows and expose loading');
assert.ok(schedule.includes("showAuditScheduleStatus(`${translateUiText('Denetim kuyruğu yüklenemedi. Yenile ile tekrar deneyin.')} ${error.message}`, true)"), 'current queue errors must expose an inline retry explanation');
assert.ok(schedule.includes('api(`/api/assets/audit-schedule?${params}`)'), 'queue must use the company-scoped audit schedule API');
assert.ok(schedule.includes("$('#auditScheduleState').value"), 'queue must submit the selected state');
assert.ok(schedule.includes("$('#auditScheduleSearch').value.trim()"), 'queue must submit the search text');
assert.ok(schedule.includes('page: String(requestedPage)'), 'queue must request its current page');
assert.ok(schedule.includes('pageSize: String(auditSchedulePageSize)'), 'queue page size must be explicit');
assert.ok(schedule.includes("$('#auditScheduleNext').disabled = auditSchedulePage >= pageCount"), 'next-page control must disable at the final page');
assert.ok(schedule.includes("$('#auditSchedulePrev').disabled = auditSchedulePage <= 1"), 'previous-page control must disable at the first page');
assert.ok(schedule.includes('Math.min(Math.max(Number($(\'#auditScheduleHorizon\').value) || 30, 1), 366)'), 'queue horizon must stay in the supported range');
assert.ok(schedule.includes('cell.textContent = value'), 'queue values must be rendered as text');

const auditStart = html.indexOf("$('#assetAuditForm').addEventListener('submit'");
const auditEnd = html.indexOf("$('#assetAuditForm').addEventListener('submit'", auditStart + 1);
assert.ok(auditStart >= 0 && auditEnd > auditStart, 'single and bulk physical audit forms must exist');
const singleAudit = html.slice(auditStart, auditEnd);
assert.ok(singleAudit.includes('if (!currentAsset) return'), 'single audit must require the current asset');
assert.ok(singleAudit.includes('`/api/assets/${assetId}/audit`'), 'single audit must save to the selected asset API');
assert.ok(singleAudit.includes("await loadAuditSchedule().catch(() => {})"), 'single audit must refresh the queue');
assert.ok(singleAudit.includes('finally { submit.disabled = false; }'), 'single audit submit must recover its button after errors');

const scheduleFormStart = html.indexOf("$('#assetAuditScheduleForm').addEventListener('submit'");
const scheduleFormEnd = html.indexOf('function setAssetTransferMode(', scheduleFormStart);
assert.ok(scheduleFormStart >= 0 && scheduleFormEnd > scheduleFormStart, 'asset audit planning form must exist');
const scheduleForm = html.slice(scheduleFormStart, scheduleFormEnd);
assert.ok(scheduleForm.includes('`/api/assets/${assetId}/audit-schedule`'), 'schedule form must update the current asset plan');
assert.ok(scheduleForm.includes('nextAuditOn: form.elements.nextAuditOn.value || null'), 'schedule form must support clearing the planned date');
assert.ok(scheduleForm.includes("await loadAuditSchedule().catch(() => {})"), 'schedule changes must refresh the queue');

for (const id of ['auditScheduleRefresh', 'auditSchedulePrev', 'auditScheduleNext', 'auditScheduleHorizon', 'auditScheduleState', 'auditScheduleSearch']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired to the audit queue`);
}

console.log('Verified audit queue request ordering, horizon/state/search filters, safe row output, and single/scheduled audit form wiring.');
