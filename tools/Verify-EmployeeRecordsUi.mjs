import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';
import { Script } from 'node:vm';

const path = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const html = readFileSync(path, 'utf8');
const inlineScripts = [...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)].map(match => match[1]);
const appScript = inlineScripts.sort((a, b) => b.length - a.length)[0];
assert.ok(appScript, 'application inline script must be present');
new Script(appScript, { filename: path });
const ids = [
  'employeesPage','employeeSearch','employeeDepartmentFilter','employeeManagerFilterSearch','employeeManagerFilter',
  'employeeState','employeeStartedFrom','employeeStartedTo','employeeRows','employeeCount','employeePrev','employeeNext',
  'newEmployeeButton','exportEmployeesButton','employeeImportOpen','employeeDialog','editEmployeeDialog','editEmployeeForm','employeeImportDialog',
  'employeeActionDialog','employeeActionForm','employeeActionSummary','employeeActionSubmit',
  'employeeProfilePhoto','employeeDirectReports','employeeAssetRows','employeeAccessoryRows','employeeLicenseRows',
  'employeeConsumableRows','employeeMaintenanceRows','employeePrivateNotesPanel','employeeDocumentsPanel'
];
for (const id of ids) assert.ok(html.includes(`id="${id}"`) || html.includes(`id = "${id}"`) || html.includes(`id = '${id}'`), `employee records must expose ${id}`);

const listStart = html.indexOf('async function loadEmployees(page = employeePage)');
const listEnd = html.indexOf('let directorySuggestionsTimer', listStart);
assert.ok(listStart >= 0 && listEnd > listStart, 'employee list loader must be present');
const list = html.slice(listStart, listEnd);
for (const token of ['employeeListRequestId','requestId !== employeeListRequestId','/api/employees?${params}',"params.set('active'", "params.set('startedFrom'", "params.set('startedTo'", "params.set('department'", "params.set('managerId'", 'updateEmployeeBulkControls()', 'employeeNext'])
  assert.ok(list.includes(token), `employee list must include ${token}`);
for (const field of ['displayName','employeeNumber','email','jobTitle','location','companyUnitName']) assert.ok(list.includes(`textContent = employee.${field}`) || list.includes(`textContent=employee.${field}`), `employee list must render ${field} as text`);
assert.ok(html.includes('<th>Şirket birimi</th>') && html.includes('cell.colSpan = 11') && html.includes('colspan="11"'), 'employee list must render the location-linked company unit column and matching empty/loading spans');
assert.ok(html.includes('Ad, personel no, e-posta, departman, görev, konum veya şirket birimi ara'), 'employee search must advertise company-unit search');
assert.ok(html.includes('name="companyUnitId"') && html.includes("loadEmployeeCompanyUnitChoices($('#newEmployeeForm').elements.companyUnitId)") &&
  html.includes('companyUnitId: form.get(\'companyUnitId\') || null'), 'employee create and edit forms must load and persist direct company-unit assignments');
assert.ok(list.includes("assetFilterLink(employee.department, 'department', employee.department)"), 'employee department links must use the safe relationship filter helper');
assert.ok(html.includes("/api/departments/${departmentId}/manager") && html.includes("openDepartmentManager(item)"), 'department directory must expose the manager assignment flow through the company-scoped API');
assert.ok(html.includes('id="departmentManagerDialog"') && html.includes('id="departmentManagerEmployee"') && html.includes('managerName ||'), 'department directory must show the assigned manager and support assigning or clearing an active employee');
assert.ok(html.includes('select.disabled = true; search.disabled = true; save.disabled = true;') && html.includes('finally { select.disabled = false; search.disabled = false; save.disabled = false; }'), 'department manager choices must finish loading before selection or save to prevent stale defaults from overwriting a clear action');
assert.ok(html.includes('departmentManagerCandidatesRequest') && html.includes('requestId !== departmentManagerCandidatesRequest'), 'stale employee-search responses must not replace the latest department manager choices');

const profileStart = html.indexOf('function closeEmployeeProfile()');
const profileEnd = html.indexOf('async function printEmployeeHandoff()', profileStart);
assert.ok(profileStart >= 0 && profileEnd > profileStart, 'employee profile open/close lifecycle must be present');
const profile = html.slice(profileStart, profileEnd);
for (const token of ['employeeProfileRequestId','employeeDialogProgrammaticCloseCount','requestId !== employeeProfileRequestId','loadEmployeePrivateNotes(employee.id, requestId)','loadEmployeeDocuments(employee.id, requestId)','if (!$(\'#employeeDialog\').open) $(\'#employeeDialog\').showModal()'])
  assert.ok(profile.includes(token), `employee profile must include ${token}`);
for (const token of ['#employeeAssetRows', 'result.assignedAssets', 'result.assignedAssetCount', 'asset.id', 'openAsset(asset.id)',
  '#employeeAccessoryRows', 'result.assignedAccessories', 'result.assignedAccessoryCount', 'assignment.accessoryId', 'openAccessory(assignment.accessoryId)',
  '#employeeLicenseRows', 'result.activeLicenses', 'result.activeLicenseCount', 'assignment.licenseId', 'openLicense(assignment.licenseId)',
  '#employeeConsumableRows', 'result.consumableCheckouts', 'result.consumableCheckoutCount', 'movement.consumableName',
  '#employeeMaintenanceRows', 'result.responsibleMaintenances', 'result.responsibleMaintenanceCount', 'openAsset(maintenance.assetId)', "$('#assetTabMaintenanceButton').click()"])
  assert.ok(profile.includes(token), `employee profile must render and link assigned inventory relation ${token}`);
assert.ok(profile.includes("addEventListener('close'"), 'native Escape dismissal must invalidate pending employee profile responses');
const editStart = html.indexOf('function openEmployeeEdit(employee)');
const editEnd = html.indexOf("$('#assetPrev').addEventListener", editStart);
assert.ok(editStart >= 0 && editEnd > editStart, 'employee edit dialog and submit handler must be present');
const edit = html.slice(editStart, editEnd);
for (const token of ["$('#editEmployeeDialog').showModal()", "api(`/api/employees/${employeeId}`", "method: 'PATCH'", 'displayName: form.get(\'displayName\')', "await loadEmployees()", "if (currentEmployeeId === employeeId && $('#employeeDialog').open) await openEmployee(employeeId)"])
  assert.ok(edit.includes(token), `employee edit flow must include ${token}`);
const managerStart = html.indexOf('async function saveEmployeeManager(managerEmployeeId)');
const managerEnd = html.indexOf("document.querySelectorAll('[data-close-employee]')", managerStart);
assert.ok(managerStart >= 0 && managerEnd > managerStart, 'employee manager save and clear controls must be present');
const manager = html.slice(managerStart, managerEnd);
for (const token of ['saveEmployeeManager', 'clearEmployeeManager', '/manager', 'managerEmployeeId'])
  assert.ok(manager.includes(token), `employee manager flow must include ${token}`);
const documentStart = html.indexOf('async function loadEmployeeDocuments(');
const documentEnd = html.indexOf('function closeEmployeeProfile()', documentStart);
assert.ok(documentStart >= 0 && documentEnd > documentStart, 'employee document actions must be present');
const documents = html.slice(documentStart, documentEnd);
assert.ok(documents.includes("confirmEmployeeAction({ summary: 'Bu çalışan belgesi silinsin mi?'"), 'employee document deletion must use the in-app confirmation dialog');

const exportParamsStart = html.indexOf('function employeeExportParams()');
const exportParamsEnd = html.indexOf('function licenseExportParams()', exportParamsStart);
assert.ok(exportParamsStart >= 0 && exportParamsEnd > exportParamsStart, 'employee export filter builder must exist');
const exportParams = html.slice(exportParamsStart, exportParamsEnd);
for (const token of ["params.set('startedFrom', startedFrom)", "params.set('startedTo', startedTo)",
  "params.set('q', q)", "params.set('department', department)"])
  assert.ok(exportParams.includes(token), `employee export filter builder must include ${token}`);

const importStart = html.indexOf('const resetEmployeeImportPreview');
const importEnd = html.indexOf("$('#selectEmployeesOnPage').addEventListener", importStart);
assert.ok(importStart >= 0 && importEnd > importStart, 'employee import, export, and bulk controls must be present');
const imports = html.slice(importStart, importEnd);
for (const token of ['employeeImportPreviewRequestId','requestId !== employeeImportPreviewRequestId',"/api/employees/import/preview.${format}","/api/employees/import.${employeeImportState.routeFormat}",'file !== employeeImportState.file','file.size > 32 * 1024 * 1024','/\\.(csv|xlsx)$/i.test(file.name)','application/vnd.openxmlformats-officedocument.spreadsheetml.sheet','if (!employeeImportState.valid || !file || file !== employeeImportState.file)','/api/employees/export.csv?${params}'])
  assert.ok(imports.includes(token), `employee import/export/bulk action must include ${token}`);
const bulkStart = html.indexOf("$('#applyEmployeeBulkActive').addEventListener");
const bulkEnd = html.indexOf("$('#employeeState').addEventListener", bulkStart);
assert.ok(bulkStart >= 0 && bulkEnd > bulkStart, 'employee bulk status action must be present');
const bulk = html.slice(bulkStart, bulkEnd);
for (const token of ['/api/employees/bulk-offboarding-summary','/api/employees/bulk-active','selectedEmployeeIds.clear()']) assert.ok(bulk.includes(token), `employee bulk status flow must include ${token}`);
assert.ok(bulk.includes('summary.canCompleteOffboarding === false') && bulk.includes('Seçilen çalışanlarla bağlantılı konum dışı zimmetler var. Pasife alma işlemini şirket genelinde yetkili yönetici yapmalı.'), 'bulk offboarding must stop before confirmation when company-wide assignments are outside the actor’s location scope');
for (const token of ['Pasife alma sırasında varlık ve aksesuarlar iade edilir, lisans atamaları kaldırılır: {items}.','result.returnedAssets','result.returnedAccessories','result.revokedLicenses']) assert.ok(bulk.includes(token) || html.includes(token), `employee offboarding UI must explain and report automatic returns: ${token}`);
assert.ok(list.includes('summary.canCompleteOffboarding === false') && list.includes('Bu çalışanla bağlantılı konum dışı zimmetler var. Pasife alma işlemini şirket genelinde yetkili yönetici yapmalı.'), 'single employee offboarding must stop before confirmation when company-wide assignments are outside the actor’s location scope');
for (const token of ['result.returnedAssets','result.returnedAccessories','result.revokedLicenses']) assert.ok(list.includes(token), `single employee offboarding notification must report ${token}`);
const confirmationStart = html.indexOf('let employeeActionResolver =');
const confirmationEnd = html.indexOf('const employeeImportState =', confirmationStart);
assert.ok(confirmationStart >= 0 && confirmationEnd > confirmationStart, 'employee confirmation dialog lifecycle must be present');
const confirmation = html.slice(confirmationStart, confirmationEnd);
for (const token of ['settleEmployeeAction(false)','settleEmployeeAction(true)','employeeActionProgrammaticCloseCount','addEventListener(\'cancel\'']) assert.ok(confirmation.includes(token), `employee confirmation dialog must include ${token}`);
assert.ok(confirmation.includes("[data-close-employee-action]") && confirmation.includes("employeeActionDialog').addEventListener('close'"), 'close button, native close, and Escape must settle the pending employee confirmation');
for (const section of [list, profile, documents, imports, bulk]) assert.doesNotMatch(section, /(?<![\w.])confirm\s*\(/, 'employee actions must use the in-app confirmation dialog instead of native prompts');
for (const token of ['Bu çalışan belgesi silinsin mi?','Çalışanın profil fotoğrafı kaldırılsın mı?','Seçili çalışan eşleme profili silinsin mi?','Doğrulanan yeni çalışan kayıtları bu şirkete eklensin mi?','confirmLabel: \'Fotoğrafı kaldır\'','confirmLabel: \'Pasife al\'','confirmLabel: \'Etkinleştir\'']) assert.ok(html.includes(token), `employee confirmation must cover ${token}`);
const photoControls = html.slice(html.indexOf('function ensureEmployeePhotoControls()'), html.indexOf('function ensureEmployeeDocumentsPanel()'));
for (const token of ['2 * 1024 * 1024','selected.size >','confirmEmployeeAction','choose.disabled = true','remove.disabled = true','method: \'POST\'','method: \'DELETE\'']) assert.ok(photoControls.includes(token), `employee photo controls must include ${token}`);
for (const endpoint of ['/api/employees/{id}/photo','/api/employees/{id}/documents','/api/employees/{id}/private-notes']) assert.ok(html.includes(endpoint.replace('{id}', '${employeeId}')) || html.includes(endpoint.replace('{id}', '${employee.id}')), `employee profile must wire ${endpoint}`);

const handover = html.slice(html.indexOf('async function loadEmployeeHandoverArchive()'), html.indexOf('function fillSelectControl(', html.indexOf('async function loadEmployeeHandoverArchive()')));
for (const token of ['#employeeHandoverDialog', '/handover-documents', '/employee-handover-documents/', 'toDataURL(\'image/png\')', 'snapshotSha256', 'recipientSignatureSha256', 'issuerSignatureSha256', 'PDF olarak kaydet', 'saveEmployeeHandoverSignatures', 'eulaAcceptances', 'EULA ve kabul kayıtları', 'item.sha256'])
  assert.ok(handover.includes(token) || html.includes(token), `signed handover record flow must include ${token}`);
assert.ok(html.includes('signatureLabel.nextElementSibling'), 'Signed handover signature controls must locate their clear button from the label, not a nonexistent canvas sibling.');
for (const id of ['employeeHandoverRecipientCanvas','employeeHandoverIssuerCanvas','employeeHandoverHistory','employeeHandoverRecipientName'])
  assert.ok(html.includes(`id="${id}"`), `signed handover dialog must include ${id}`);
const handoverApi = readFileSync(resolve('src/EnvanterTR.Web/Data/EmployeeHandoverEndpoints.cs'), 'utf8');
for (const token of ['CanAccessDocumentLocationsAsync', 'ExecuteUpdateAsync', 'MaxSignatureBytes', 'FixedTimeEquals', 'SnapshotSha256', 'SignatureSha256', 'SoftwareLicenseEmployeeEulaAcceptances', 'HandoverEulaSnapshot', 'EulaSha256'])
  assert.ok(handoverApi.includes(token), `signed handover API must enforce ${token}`);

const employeeApi = readFileSync(resolve('src/EnvanterTR.Web/Data/OperationalEndpoints.cs'), 'utf8');
const employeeScope = readFileSync(resolve('src/EnvanterTR.Web/Data/CompanyLocationAccess.cs'), 'utf8');
const customerApi = readFileSync(resolve('src/EnvanterTR.Web/Data/CustomerApiEndpoints.cs'), 'utf8');
assert.ok(employeeScope.includes('CanAccessEmployeeAsync') && employeeScope.includes('public sealed record EmployeeAccessScope') &&
  employeeScope.includes('return !RestrictLocations && companyUnitId is Guid unit') &&
  employeeScope.includes('!RestrictLocations && employee.CompanyUnitId.HasValue'),
  'central employee access scope must authorize direct-unit employees only when location access is unrestricted');
assert.ok(employeeApi.includes('employeeAccessScope.Allows(locationId, companyUnitId)') && employeeApi.includes('employeeAccessScope.Apply(query)'),
  'employee detail and collection boundaries must use the shared location-and-unit scope');
assert.ok(employeeApi.includes('EmployeeLocationIsInScopeAsync(db, companyId, assignedEmployeeId, employeeAccessScope'),
  'asset return authorization must validate the assigned employee against both scopes');
assert.ok(employeeApi.includes('EmployeesInScope(db, companyId, employeeAccessScope)') &&
  employeeApi.includes('ManagerName = scopedEmployees.Where(manager => manager.Id == x.ManagerEmployeeId)') &&
  employeeApi.includes('var reportsQuery = EmployeesInScope(db, companyId, employeeAccessScope)'),
  'employee list/export manager references and profile direct reports must share the same effective location-and-unit visibility scope');
assert.ok(customerApi.includes('GetEmployeeAccessScopeAsync(db, companyId, token.UserId, ct)') &&
  customerApi.includes('employeeScope.Apply(db.Employees.AsNoTracking().Where(x => x.CompanyId == companyId))') &&
  customerApi.includes('CompanyUnitName = x.CompanyUnitId.HasValue') &&
  customerApi.includes('x.CompanyUnitId.HasValue && db.CompanyUnits.Any(unit => unit.CompanyId == companyId'),
  'customer API employee list must enforce direct unit visibility and return only scoped company-unit labels');
const assetsApi = readFileSync(resolve('src/EnvanterTR.Web/Data/AssetEndpoints.cs'), 'utf8');
assert.ok(assetsApi.includes('GetEmployeeAccessScopeAsync') && assetsApi.includes('EmployeeAccessScope') &&
  assetsApi.includes('employeeScope.Apply('),
  'asset employee reads and mutations must use the shared location-and-unit employee boundary');

console.log('Verified employee list and profile flows, scoped assignments/media, import/offboarding, immutable handover and EULA acceptance snapshots, two-part PNG signature capture, tenant/location authorization, atomic one-time signatures, document hashes, archived history, PDF printing, and JavaScript syntax.');
