import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const employeeId = '33333333-3333-4333-8333-333333333333';
const taskId = '44444444-4444-4444-8444-444444444444';
const createdTaskId = '55555555-5555-4555-8555-555555555555';
const otherEmployeeId = '66666666-6666-4666-8666-666666666666';
const csrfToken = 'synthetic-tasks-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const requests = [];
const writes = [];
let actingAsEmployee = false;
const tasks = [{ id: taskId, title: 'Synthetic quarterly review', description: 'Review assigned equipment', status: 'Open', priority: 'High',
  assignedEmployeeId: employeeId, employeeName: 'Synthetic QA Employee', employeeNumber: 'QA-001', createdByUserId: userId,
  createdBy: 'Synthetic Owner', dueAt: '2026-11-01T12:00:00Z', createdAt: '2026-10-15T12:00:00Z', updatedAt: '2026-10-15T12:00:00Z' }];
tasks.push({ id: '77777777-7777-4777-8777-777777777777', title: 'Other employee private task', description: 'Must not leak to the current employee', status: 'Open', priority: 'Normal',
  assignedEmployeeId: otherEmployeeId, employeeName: 'Other QA Employee', employeeNumber: 'QA-002', createdByUserId: userId,
  createdBy: 'Synthetic Owner', dueAt: null, createdAt: '2026-10-15T12:00:00Z', updatedAt: '2026-10-15T12:00:00Z' });
const employee = { id: employeeId, displayName: 'Synthetic QA Employee', employeeNumber: 'QA-001' };
const json = (response, status, body) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' }); response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': url.pathname.endsWith('.woff2') ? 'font/woff2' : 'text/css; charset=utf-8' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }

  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    const chunks = []; request.on('data', chunk => chunks.push(Buffer.from(chunk)));
    request.on('end', () => resolve(chunks.length ? JSON.parse(Buffer.concat(chunks).toString('utf8')) : null));
  });
  const record = { method: request.method, path: url.pathname, query: Object.fromEntries(url.searchParams.entries()), body, csrf: request.headers['x-csrf-token'] };
  requests.push(record);

  if (request.method === 'POST' && url.pathname === '/api/tasks') {
    assert.equal(record.csrf, csrfToken, 'Creating a task must use CSRF.');
    const task = { id: createdTaskId, ...body, status: 'Open', createdByUserId: userId, createdBy: 'Synthetic Owner',
      employeeName: body.assignedEmployeeId === employeeId ? employee.displayName : null,
      employeeNumber: body.assignedEmployeeId === employeeId ? employee.employeeNumber : null,
      createdAt: '2026-10-15T16:00:00Z', updatedAt: '2026-10-15T16:00:00Z', completedAt: null };
    tasks.unshift(task); writes.push(record);
    json(response, 201, { id: task.id, title: task.title, status: task.status, priority: task.priority, dueAt: task.dueAt }); return;
  }
  const updateRoute = url.pathname.match(/^\/api\/tasks\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && updateRoute) {
    assert.equal(record.csrf, csrfToken, 'Updating a task must use CSRF.');
    const task = tasks.find(item => item.id === updateRoute[1]); assert.ok(task, 'The edited task must exist.');
    Object.assign(task, body, { employeeName: body.assignedEmployeeId === employeeId ? employee.displayName : null,
      employeeNumber: body.assignedEmployeeId === employeeId ? employee.employeeNumber : null, updatedAt: '2026-10-15T16:01:00Z' });
    writes.push(record); json(response, 200, { id: task.id, title: task.title, status: task.status, priority: task.priority, dueAt: task.dueAt }); return;
  }
  const statusRoute = url.pathname.match(/^\/api\/tasks\/([0-9a-f-]+)\/status$/i);
  if (request.method === 'PATCH' && statusRoute) {
    assert.equal(record.csrf, csrfToken, 'Changing task status must use CSRF.');
    const task = tasks.find(item => item.id === statusRoute[1]); assert.ok(task, 'The status target must exist.');
    task.status = body.status; task.completedAt = body.status === 'Completed' ? '2026-10-15T16:02:00Z' : null;
    task.updatedAt = '2026-10-15T16:02:00Z'; writes.push(record);
    json(response, 200, { id: task.id, status: task.status, completedAt: task.completedAt, updatedAt: task.updatedAt }); return;
  }

  if (request.method === 'GET') {
    if (url.pathname === '/api/status') return json(response, 200, { status: 'ready', migrationCount: 93, pendingMigrationCount: 0 });
    if (url.pathname === '/api/setup/status') return json(response, 200, { setupRequired: false });
    if (url.pathname === '/api/session/csrf') return json(response, 200, { requestToken: csrfToken });
    if (url.pathname === '/api/session') return json(response, 200, { companyId, companyName: 'Synthetic Task Company', userId,
      name: actingAsEmployee ? 'Synthetic QA Employee User' : 'Synthetic Owner', role: actingAsEmployee ? 'viewer' : 'owner', hasPhoto: false, availableCompanies: [{ id: companyId, name: 'Synthetic Task Company', role: actingAsEmployee ? 'viewer' : 'owner' }],
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic plan' } });
    if (url.pathname === '/api/company/branding') return json(response, 200, { name: 'Synthetic Task Company', brandPrimaryColor: '#2563EB', hasLogo: false });
    if (url.pathname === '/api/tasks') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const status = url.searchParams.get('status') || '', priority = url.searchParams.get('priority') || '';
      const scopedTasks = actingAsEmployee ? tasks.filter(task => task.assignedEmployeeId === employeeId) : tasks;
      const filtered = scopedTasks.filter(task => (!q || [task.title, task.description, task.employeeName, task.employeeNumber].some(value => value?.toLocaleLowerCase('tr-TR').includes(q)))
        && (!status || task.status === status) && (!priority || task.priority === priority));
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 25), total = filtered.length;
      const summary = Object.fromEntries(['Open', 'InProgress', 'Completed', 'Cancelled'].map(key => [key[0].toLowerCase() + key.slice(1), scopedTasks.filter(task => task.status === key).length]));
      return json(response, 200, { items: filtered.slice((page - 1) * pageSize, page * pageSize), employees: actingAsEmployee ? [employee] : [employee], page, pageSize, total,
        canManage: !actingAsEmployee, summary });
    }
    return json(response, 200, { items: [], total: 0, page: 1, pageSize: 50 });
  }
  response.writeHead(404); response.end();
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address(); assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const pageErrors = []; page.on('pageerror', error => pageErrors.push(error.message));

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="operations"]').click();
  await page.locator('#sidebar [data-page="Görevler"]').click();
  await page.locator('#tasksPage').waitFor({ state: 'visible' });
  await page.locator('#taskRows').getByText('Synthetic quarterly review').waitFor();
  assert.equal(await page.locator('#taskNewButton').isVisible(), true, 'A company manager must see task creation.');

  const searchRequest = page.waitForRequest(request => {
    const url = new URL(request.url()); return url.pathname === '/api/tasks' && url.searchParams.get('q') === 'Synthetic quarterly';
  });
  await page.locator('#taskSearch').fill('Synthetic quarterly'); await searchRequest;
  const filteredRequest = page.waitForRequest(request => {
    const url = new URL(request.url()); return url.pathname === '/api/tasks' && url.searchParams.get('status') === 'Open' && url.searchParams.get('priority') === 'High';
  });
  await page.locator('#taskStatusFilter').selectOption('Open'); await page.locator('#taskPriorityFilter').selectOption('High'); await filteredRequest;
  assert.equal(await page.locator('#taskRows tr').count(), 1, 'Combined task filters must show the matching row.');
  const resetRequest = page.waitForRequest(request => {
    const url = new URL(request.url()); return url.pathname === '/api/tasks' && !url.searchParams.has('q') && !url.searchParams.has('status') && !url.searchParams.has('priority');
  });
  await page.locator('#taskStatusFilter').selectOption(''); await page.locator('#taskPriorityFilter').selectOption(''); await page.locator('#taskSearch').fill(''); await resetRequest;

  const writesBeforeCancel = writes.length;
  await page.locator('#taskNewButton').click(); await page.locator('#taskEditorPanel').waitFor({ state: 'visible' });
  await page.locator('#taskEditorCancel').click();
  assert.equal(writes.length, writesBeforeCancel, 'Canceling task creation must not write.');

  await page.locator('#taskNewButton').click();
  await page.locator('#taskForm [name="title"]').fill('Synthetic follow-up task');
  await page.locator('#taskForm [name="description"]').fill('Created by hosted browser acceptance');
  await page.locator('#taskForm [name="priority"]').selectOption('Urgent');
  await page.locator('#taskForm [name="assignedEmployeeId"]').selectOption(employeeId);
  await page.locator('#taskForm [name="dueAt"]').fill('2026-11-15');
  await page.locator('#taskSaveButton').click();
  await page.locator('#taskRows').getByText('Synthetic follow-up task').waitFor();
  const create = writes.find(item => item.method === 'POST' && item.path === '/api/tasks');
  assert.equal(create?.csrf, csrfToken); assert.equal(create?.body.title, 'Synthetic follow-up task');
  assert.equal(create?.body.priority, 'Urgent'); assert.equal(create?.body.assignedEmployeeId, employeeId);
  assert.match(create?.body.dueAt || '', /^2026-11-15T/);

  const createdRow = page.locator('#taskRows tr').filter({ hasText: 'Synthetic follow-up task' });
  await createdRow.getByRole('button', { name: 'Düzenle' }).click();
  await page.locator('#taskForm [name="title"]').fill('Synthetic edited task'); await page.locator('#taskSaveButton').click();
  await page.locator('#taskRows').getByText('Synthetic edited task').waitFor();
  assert.ok(writes.some(item => item.method === 'PUT' && item.path === `/api/tasks/${createdTaskId}` && item.csrf === csrfToken),
    'Task edit must persist through CSRF-protected PUT.');
  await page.locator('#taskRows tr').filter({ hasText: 'Synthetic edited task' }).locator('.task-status-select').selectOption('Completed');
  await page.waitForFunction(() => document.querySelector('#taskSummary')?.innerText.includes('Tamamlandı'));
  assert.ok(writes.some(item => item.method === 'PATCH' && item.path === `/api/tasks/${createdTaskId}/status` && item.body.status === 'Completed'),
    'Task status change must persist and refresh the summary.');
  assert.deepEqual(pageErrors, [], `Company task screen must not throw browser errors: ${pageErrors.join(' | ')}`);
  const managerCreatedTask = tasks.find(task => task.id === createdTaskId);
  assert.ok(managerCreatedTask, 'The manager flow must have created its synthetic task.');
  managerCreatedTask.assignedEmployeeId = null;
  managerCreatedTask.employeeName = null;
  managerCreatedTask.employeeNumber = null;
  actingAsEmployee = true;
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="operations"]').click();
  await page.locator('#sidebar [data-page="Görevler"]').click();
  await page.locator('#tasksPage').waitFor({ state: 'visible' });
  await page.locator('#taskRows').getByText('Synthetic quarterly review').waitFor();
  assert.equal(await page.locator('#taskNewButton').isVisible(), false, 'An assigned employee must not see manager-only task creation.');
  assert.equal(await page.locator('#taskRows tr').count(), 1, 'The employee should see only their assigned task.');
  assert.equal(await page.locator('#taskRows').getByText('Other employee private task').count(), 0, 'An employee must not see another employee\'s task.');
  await page.getByRole('button', { name: 'Başlat' }).click();
  await page.getByRole('button', { name: 'Tamamla' }).waitFor();
  await page.getByRole('button', { name: 'Tamamla' }).click();
  await page.locator('#taskRows').getByText('Tamamlandı').waitFor();
  assert.ok(writes.some(item => item.method === 'PATCH' && item.path === `/api/tasks/${taskId}/status` && item.body.status === 'InProgress'),
    'The assigned employee must be able to start only their task.');
  assert.ok(writes.some(item => item.method === 'PATCH' && item.path === `/api/tasks/${taskId}/status` && item.body.status === 'Completed'),
    'The assigned employee must be able to complete their in-progress task.');
  assert.deepEqual(pageErrors, [], `Employee task screen must not throw browser errors: ${pageErrors.join(' | ')}`);
  console.log('PASS: manager task flows and assigned-employee Chromium flow, including role-specific controls, private task visibility, start/complete transitions and summary refresh, with synthetic API.');
} finally {
  await browser.close(); await new Promise(resolve => server.close(resolve));
}
