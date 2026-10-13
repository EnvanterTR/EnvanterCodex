import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const start = html.indexOf('async function loadCompanyTasks(');
const end = html.indexOf('function openCompanyTaskEditor(', start);
assert.ok(start >= 0 && end > start, 'company task list loader must exist');
const loader = html.slice(start, end);
assert.ok(loader.includes('const requestId = ++companyTaskRequestId'), 'task list requests must have an ordering token');
assert.ok(loader.includes('if (requestId !== companyTaskRequestId) return'), 'stale task results must not replace the current list');
assert.ok(loader.includes('catch (error) { if (requestId !== companyTaskRequestId) return; throw error; }'), 'stale task errors must not appear as current errors');
assert.ok(loader.includes('api(`/api/tasks?${params}`)'), 'task list must load from the company API');
for (const filter of ["$('#taskSearch').value.trim()", "$('#taskStatusFilter').value", "$('#taskPriorityFilter').value"]) {
  assert.ok(loader.includes(filter), `task list must apply ${filter}`);
}
assert.ok(loader.includes("$('#taskNewButton').hidden = !companyTaskCanManage"), 'task creation must be hidden from non-managers');

const rowsStart = html.indexOf('function renderCompanyTasks(');
const rowsEnd = html.indexOf('async function loadCompanyTasks(', rowsStart);
const rows = html.slice(rowsStart, rowsEnd);
assert.ok(rows.includes("if (companyTaskCanManage)"), 'manager actions must be role-gated');
assert.ok(rows.includes('api(`/api/tasks/${task.id}/status`, { method: \'PATCH\''), 'status actions must call the task status API');
assert.ok(rows.includes("select.addEventListener('change'"), 'managers must be able to set task status');
assert.ok(rows.includes("nextStatus === 'InProgress' ? 'Başlat' : 'Tamamla'"), 'assigned members must have start/complete actions');
assert.ok(rows.includes('titleText.textContent = task.title'), 'task titles must be rendered as text');
assert.ok(rows.includes('note.textContent = task.description'), 'task descriptions must be rendered as text');

const editorStart = html.indexOf('function openCompanyTaskEditor(');
const editorEnd = html.indexOf("$('#tasksRefresh').addEventListener(", editorStart);
assert.ok(editorStart >= 0 && editorEnd > editorStart, 'task create/edit form handlers must exist');
const editor = html.slice(editorStart, editorEnd);
assert.ok(editor.includes("api(id ? `/api/tasks/${id}` : '/api/tasks', {"), 'task editor must create and update through the API');
assert.ok(editor.includes("method: id ? 'PUT' : 'POST'"), 'task editor must use the correct create/update methods');
assert.ok(editor.includes('assignedEmployeeId: form.elements.assignedEmployeeId.value || null'), 'task editor must support optional employee assignment');
assert.ok(editor.includes('priority: form.elements.priority.value'), 'task editor must save the selected priority');
assert.ok(editor.includes('dueAt: dueDate ? new Date(`${dueDate}T12:00:00`).toISOString() : null'), 'task editor must serialize the optional local due date consistently');
for (const id of ['tasksRefresh', 'taskStatusFilter', 'taskPriorityFilter', 'taskSearch', 'taskPrev', 'taskNext']) {
  assert.ok(html.includes(`$('#${id}').addEventListener(`), `${id} must be wired to task list actions`);
}

console.log('Verified task filtering/paging, stale-request protection, role-gated create/edit/status actions, assigned-member transitions, and safe task rendering.');
