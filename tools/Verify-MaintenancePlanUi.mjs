import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
for (const [name, startMarker, endMarker] of [
  ['asset detail', 'async function updateMaintenancePlan(', 'async function loadAssetMaintenances('],
  ['company maintenance list and calendar', 'async function updateCompanyMaintenancePlan(', 'async function loadAssetMaintenances('],
]) {
  const start = html.indexOf(startMarker);
  const end = html.indexOf(endMarker, start);
  assert.ok(start >= 0 && end > start, `${name} plan action must exist`);
  const section = html.slice(start, end);
  assert.ok(section.includes("await confirmEmployeeAction({ title: 'İşlemi onayla'"), `${name} cancellation must use the accessible app dialog`);
  assert.ok(section.includes("summary: translateUiText('Bu bakım planını iptal etmek istiyor musunuz?')"), `${name} confirmation must explain the cancellation`);
  assert.ok(section.includes("confirmLabel: 'Planı iptal et'"), `${name} confirmation must name the action`);
  assert.ok(section.includes('`/api/assets/${assetId}/maintenances/${maintenanceId}/${action}`'), `${name} must call the maintenance plan endpoint`);
  assert.doesNotMatch(section, /\bwindow\.confirm\s*\(|\bconfirm\s*\(/, `${name} must not show browser-native confirmation`);
}

const companyActionStart = html.indexOf('async function updateCompanyMaintenancePlan(');
const companyActionEnd = html.indexOf('async function loadAssetMaintenances(', companyActionStart);
const companyAction = html.slice(companyActionStart, companyActionEnd);
assert.ok(companyAction.includes('await loadCompanyMaintenances(maintenancePageNumber)'), 'company list must refresh after a plan action');
assert.ok(companyAction.includes('await loadOperationalCalendar()'), 'calendar must refresh after a plan action');
assert.ok(companyAction.includes("notify(translateUiText(action === 'start' ? 'Planlı bakım başladı; varlık bakım durumuna alındı.' : 'Bakım planı iptal edildi.'))"), 'successful actions must announce their result');

const listStart = html.indexOf('async function loadCompanyMaintenances(');
const listEnd = html.indexOf('function exportCompanyMaintenances(', listStart);
assert.ok(listStart >= 0 && listEnd > listStart, 'company maintenance list loader must exist');
const listLoader = html.slice(listStart, listEnd);
assert.ok(listLoader.includes('const requestId = ++maintenanceRequestSequence'), 'maintenance list requests must have an ordering token');
assert.ok(listLoader.includes('if (requestId !== maintenanceRequestSequence) return'), 'outdated list successes must not replace newer results');
assert.ok(listLoader.includes('catch (error) { if (requestId !== maintenanceRequestSequence) return; throw error; }'), 'outdated list failures must not surface as current errors');
assert.ok(listLoader.includes('maintenancePageNumber = result.page'), 'the active page must follow the accepted server response');

console.log('Verified accessible maintenance-plan cancellation across detail/list/calendar and race-safe company maintenance filtering and paging.');
