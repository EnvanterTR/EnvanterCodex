import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const operational = await readFile(new URL('../src/EnvanterTR.Web/Data/OperationalEndpoints.cs', import.meta.url), 'utf8');
const departmentApiStart = operational.indexOf('api.MapGet("/departments"');
const departmentApiEnd = operational.indexOf('}).RequireAuthorization("people.read");', departmentApiStart);
assert.ok(departmentApiStart >= 0 && departmentApiEnd > departmentApiStart, 'department list API must exist');
const departmentApi = operational.slice(departmentApiStart, departmentApiEnd);
for (const token of [
  'GetEffectiveLocationIdsAsync(db, companyId, actorId, cancellationToken)',
  'allowedLocations.Contains(requestedLocation)',
  'allowedLocations.Contains(x.LocationCatalogId.Value)',
  'allowedLocations.Contains(x.Id)',
  'allowedLocations.Contains(x.LocationCatalogId.Value)))'
]) assert.ok(departmentApi.includes(token), `department list must enforce member location scope: ${token}`);
for (const token of [
  'if (!AssetLocationIsInScope(department.LocationCatalogId, allowedLocations)) return Results.NotFound();',
  'if (!AssetLocationIsInScope(entry.LocationCatalogId, allowedLocations) ||',
  'if (!AssetLocationIsInScope(source.LocationCatalogId, allowedLocations) ||',
  'if (!AssetLocationIsInScope(request.LocationCatalogId, allowedLocations)) return Results.NotFound();',
  'if (allowedLocations is not null) return Results.Forbid();'
]) assert.ok(operational.includes(token), `department mutation APIs must enforce location scope: ${token}`);
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
const refreshHandler = html.match(/\$\('#directoryRefresh'\)\.addEventListener\('click', \(\) => ([^;]+);/);
assert.ok(refreshHandler?.[1].includes('directoryFilterLocationId'), 'Refreshing a location-filtered department directory must retain its location scope.');
const departmentCreateStart = html.indexOf('async function openCreateDepartment(');
const departmentCreateEnd = html.indexOf('async function openDirectoryRename(', departmentCreateStart);
assert.ok(departmentCreateStart >= 0 && departmentCreateEnd > departmentCreateStart, 'department create and edit catalog flows must be present');
const departments = html.slice(departmentCreateStart, departmentCreateEnd);
for (const token of [
  "api('/api/departments')", "api('/api/locations')", "parentSelect.add(new Option(item.departmentPath || item.name, item.departmentId))",
  "locationSelect.add(new Option(item.name, item.locationId))", "method: 'POST', body: JSON.stringify({ name,",
  'parentDepartmentId: form.elements.parentDepartmentId.value || null', 'locationCatalogId: form.elements.locationCatalogId.value || null',
  "$('#createDepartmentDialog').close()", "loadDirectory('departments', '', directoryFilterLocationId)"
]) assert.ok(departments.includes(token), `department create flow must include ${token}`);
const departmentRenderer = html.slice(renderStart, renderEnd);
for (const token of ["item.departmentPath || item.name", "addChild.textContent = 'Alt departman ekle'", "canManagePeople()"])
  assert.ok(departmentRenderer.includes(token), `department hierarchy row must include ${token}`);

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
  'x.depth < 5',
  "$('#createLocationPreview').textContent = name ? `Konum yolu: ${parent ? `${parent.name} / ` : ''}${name}` : ''",
  "api('/api/locations/catalog', { method: 'POST'",
  'parentLocationId',
  "api(`/api/locations/catalog/${locationId}`, { method: 'PATCH', body: JSON.stringify({ name: newName,",
  '...(canManageSettings() ? { companyUnitId: form.elements.companyUnitId.value || null,',
  'clearCompanyUnit: !form.elements.companyUnitId.value',
  "method: 'POST', body: JSON.stringify({ employeeId: $('#locationManagerEmployee').value || null })",
  "$('#directorySearch').addEventListener('input', renderDirectoryRows)"
]) assert.ok(html.includes(token), `location catalog flow must include ${token}`);

console.log('Verified department and company-unit location catalog create/update/clear flow plus location/building routing, hierarchy, filtering, defaults, manager actions, archive/restore confirmation, and role-gated creation.');
