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
for (const field of ['displayName','employeeNumber','email','jobTitle','location']) assert.ok(list.includes(`textContent = employee.${field}`) || list.includes(`textContent=employee.${field}`), `employee list must render ${field} as text`);
assert.ok(list.includes("assetFilterLink(employee.department, 'department', employee.department)"), 'employee department links must use the safe relationship filter helper');

const profileStart = html.indexOf('function closeEmployeeProfile()');
const profileEnd = html.indexOf('async function printEmployeeHandoff()', profileStart);
assert.ok(profileStart >= 0 && profileEnd > profileStart, 'employee profile open/close lifecycle must be present');
const profile = html.slice(profileStart, profileEnd);
for (const token of ['employeeProfileRequestId','employeeDialogProgrammaticCloseCount','requestId !== employeeProfileRequestId','loadEmployeePrivateNotes(employee.id, requestId)','loadEmployeeDocuments(employee.id, requestId)','if (!$(\'#employeeDialog\').open) $(\'#employeeDialog\').showModal()'])
  assert.ok(profile.includes(token), `employee profile must include ${token}`);
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

const importStart = html.indexOf('const resetEmployeeImportPreview');
const importEnd = html.indexOf("$('#selectEmployeesOnPage').addEventListener", importStart);
assert.ok(importStart >= 0 && importEnd > importStart, 'employee import, export, and bulk controls must be present');
const imports = html.slice(importStart, importEnd);
for (const token of ['employeeImportPreviewRequestId','requestId !== employeeImportPreviewRequestId',"/api/employees/import/preview.${format}","/api/employees/import.${employeeImportState.routeFormat}",'file !== employeeImportState.file','file.size > 32 * 1024 * 1024','/\\.(csv|xlsx)$/i.test(file.name)','application/vnd.openxmlformats-officedocument.spreadsheetml.sheet','if (!employeeImportState.valid || !file || file !== employeeImportState.file)','/api/employees/export.csv?${params}',"params.set('startedFrom'", "params.set('startedTo'"])
  assert.ok(imports.includes(token), `employee import/export/bulk action must include ${token}`);
const bulkStart = html.indexOf("$('#applyEmployeeBulkActive').addEventListener");
const bulkEnd = html.indexOf("$('#employeeState').addEventListener", bulkStart);
assert.ok(bulkStart >= 0 && bulkEnd > bulkStart, 'employee bulk status action must be present');
const bulk = html.slice(bulkStart, bulkEnd);
for (const token of ['/api/employees/bulk-offboarding-summary','/api/employees/bulk-active','selectedEmployeeIds.clear()']) assert.ok(bulk.includes(token), `employee bulk status flow must include ${token}`);
const confirmationStart = html.indexOf('let employeeActionResolver =');
const confirmationEnd = html.indexOf('const employeeImportState =', confirmationStart);
assert.ok(confirmationStart >= 0 && confirmationEnd > confirmationStart, 'employee confirmation dialog lifecycle must be present');
const confirmation = html.slice(confirmationStart, confirmationEnd);
for (const token of ['settleEmployeeAction(false)','settleEmployeeAction(true)','employeeActionProgrammaticCloseCount','addEventListener(\'cancel\'']) assert.ok(confirmation.includes(token), `employee confirmation dialog must include ${token}`);
assert.ok(confirmation.includes("[data-close-employee-action]") && confirmation.includes("employeeActionDialog').addEventListener('close'"), 'close button, native close, and Escape must settle the pending employee confirmation');
for (const section of [list, profile, documents, imports, bulk]) assert.doesNotMatch(section, /(?<![\w.])confirm\s*\(/, 'employee actions must use the in-app confirmation dialog instead of native prompts');
for (const token of ['Bu çalışan belgesi silinsin mi?','Seçili çalışan eşleme profili silinsin mi?','Doğrulanan yeni çalışan kayıtları bu şirkete eklensin mi?','confirmLabel: \'Pasife al\'','confirmLabel: \'Etkinleştir\'']) assert.ok(html.includes(token), `employee confirmation must cover ${token}`);
for (const endpoint of ['/api/employees/{id}/photo','/api/employees/{id}/documents','/api/employees/{id}/private-notes']) assert.ok(html.includes(endpoint.replace('{id}', '${employeeId}')) || html.includes(endpoint.replace('{id}', '${employee.id}')), `employee profile must wire ${endpoint}`);

console.log('Verified employee list filters/page/race guard, profile close lifecycle, editable employee fields, manager assignment/removal, private notes, document deletion, import preview/confirmation, filtered CSV parity, bulk status confirmation, and JavaScript syntax.');
