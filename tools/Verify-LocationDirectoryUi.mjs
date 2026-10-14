import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const directoryStart = html.indexOf('function openDirectoryLocation(');
const directoryEnd = html.indexOf('function renderDirectoryRows()', directoryStart);
assert.ok(directoryStart >= 0 && directoryEnd > directoryStart, 'location destination shortcut must be present');
const destination = html.slice(directoryStart, directoryEnd);
for (const token of [
  "item.componentCount ? 'components' : 'assets'",
  "$('#assetStatusFilter').value = ''",
  "assetLocationFilter = item.name",
  " $('[data-page=\"Varlıklar\"]').click()",
  "$('#componentSearch').value = item.name"
]) assert.ok(destination.includes(token), `location shortcut destination must include ${token}`);

const renderStart = html.indexOf('function renderDirectoryRows()');
const renderEnd = html.indexOf('async function loadDirectory(', renderStart);
assert.ok(renderStart >= 0 && renderEnd > renderStart, 'location directory renderer must be present');
const render = html.slice(renderStart, renderEnd);
for (const token of [
  "$('#directorySearch').value.trim().toLocaleLowerCase('tr-TR')",
  'openDirectoryLocation(item)',
  "item.assetCount, item.assignedCount, item.availableCount",
  '/api/locations/catalog/${item.locationId}/default',
  'openLocationManager(item)',
  "item.isActive ? 'archive' : 'restore'",
  "confirmEmployeeAction({ title: 'İşlemi onayla', summary: `“${item.name}” konumu arşivlensin mi?",
  "confirmLabel: 'Arşivle'",
  '“${item.name}” yeni kayıtların varsayılan konumu oldu.',
  'Sorumlu ata',
  'Arşivle',
  'Geri al',
  'Adı düzenle'
]) assert.ok(render.includes(token), `location directory actions must include ${token}`);
assert.doesNotMatch(render, /window\.confirm\s*\(/, 'location archive must use the accessible in-app confirmation');

const loaderStart = html.indexOf('async function loadDirectory(');
const loaderEnd = html.indexOf("$('#addDepartmentButton').addEventListener", loaderStart);
assert.ok(loaderStart >= 0 && loaderEnd > loaderStart, 'location and building page loader must be present');
const loader = html.slice(loaderStart, loaderEnd);
for (const token of [
  "titleOverride || (departments ? 'Departmanlar' : 'Lokasyonlar')",
  "'Kat, alan, oda ve kabinleri hiyerarşik konum olarak yönetin.'",
  "api('/api/locations')",
  "$('#addLocationButton').hidden = departments || !canWriteAssets()",
  "$('#directorySearch').addEventListener('input', renderDirectoryRows)"
]) assert.ok(loader.includes(token) || html.includes(token), `location/building page must include ${token}`);

const companyUnitStart = html.indexOf("$('#companyUnitRows').addEventListener('click'");
const companyUnitEnd = html.indexOf("$('#companyUnitForm').addEventListener('submit'", companyUnitStart);
assert.ok(companyUnitStart >= 0 && companyUnitEnd > companyUnitStart, 'company unit archive and restore actions must be present');
const companyUnitActions = html.slice(companyUnitStart, companyUnitEnd);
assert.match(companyUnitActions, /confirmEmployeeAction\(\{ title: nextActive \? 'Şirket birimi geri yüklensin mi\?' : 'Şirket birimi arşivlensin mi\? Etkin alt birimleri varsa işlem reddedilir\.', summary: actionSummary, confirmLabel: nextActive \? 'Geri yükle' : 'Arşivle', danger: !nextActive \}\)/,
  'Company unit archive and restore must use a clear, cancellable in-app confirmation.');
assert.doesNotMatch(companyUnitActions, /\bconfirm\s*\(/,
  'Company unit state changes must not depend on browser-native confirmation.');

for (const token of [
  'id="addLocationButton"',
  'id="locationManagerDialog"',
  'id="locationManagerForm"',
  "api('/api/locations/catalog', { method: 'POST'",
  'parentLocationId',
  "method: 'PATCH', body: JSON.stringify({ name: newName })",
  "method: 'POST', body: JSON.stringify({ employeeId: $('#locationManagerEmployee').value || null })",
  "$('#directorySearch').addEventListener('input', renderDirectoryRows)"
]) assert.ok(html.includes(token), `location catalog flow must include ${token}`);

console.log('Verified location/building routing, hierarchical create and rename, filtering, count links, default location, manager assignment, archive/restore confirmation, and role-gated creation.');
