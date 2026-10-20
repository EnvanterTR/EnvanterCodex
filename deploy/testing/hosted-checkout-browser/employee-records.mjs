import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const companyId = '11111111-1111-4111-8111-111111111111';
const userId = '22222222-2222-4222-8222-222222222222';
const memberUserId = '22222222-2222-4222-8222-222222222223';
const employeeId = '33333333-3333-4333-8333-333333333333';
const companyUnitId = '99999999-9999-4999-8999-999999999991';
const managerId = '44444444-4444-4444-8444-444444444444';
const alternateManagerId = '55555555-5555-4555-8555-555555555555';
const assetId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const accessoryId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const licenseId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const csrfToken = 'synthetic-employee-records-csrf';
const appHtml = await readFile('/checkout-site/company-app.html');
const appScripts = new Map(await Promise.all(['report-print.js', 'scheduled-reports.js'].map(async name => [
  `/${name}`,
  await readFile(`/checkout-site/${name}`, 'utf8')
])));
const qrCodeGenerator = await readFile('/checkout-site/vendor/qrcodegen.js', 'utf8');
const requests = [];
const employees = [
  { id: managerId, employeeNumber: 'QA-MGR-001', displayName: 'QA Supervisor', email: null, department: 'Operations', jobTitle: 'Team lead', phone: null, location: 'Istanbul Office / Floor 1', companyUnitId: '99999999-9999-4999-8999-999999999991', companyUnitName: 'QA Main Unit', startedOn: '2020-03-10', isActive: true, managerEmployeeId: null, managerName: null },
  { id: alternateManagerId, employeeNumber: 'QA-MGR-002', displayName: 'QA Supervisor II', email: null, department: 'IT', jobTitle: 'Engineering lead', phone: null, location: null, startedOn: '2021-05-11', isActive: true, managerEmployeeId: null, managerName: null },
  { id: '77777777-7777-4777-8777-777777777777', employeeNumber: 'QA-EMP-INACTIVE', displayName: 'QA Inactive Employee', email: null, department: 'Operations', jobTitle: 'Former technician', phone: null, location: null, startedOn: '2020-12-31', isActive: false, managerEmployeeId: null, managerName: null },
  { id: '88888888-8888-4888-8888-888888888888', employeeNumber: 'QA-EMP-UNDATED', displayName: 'QA Undated Employee', email: null, department: 'Operations', jobTitle: 'New technician', phone: null, location: null, startedOn: null, isActive: true, managerEmployeeId: null, managerName: null }
];
let syntheticRole = 'admin';
let privateNotes = '';
let employeeDocuments = [];
const handoverDocuments = [];
let employeeImportPreview = null;
const employeePhotos = new Map();
const floorOneId = '66666666-6666-4666-8666-666666666661';
const floorTwoId = '66666666-6666-4666-8666-666666666662';
const departmentOneId = '77777777-7777-4777-8777-777777777771';
const departmentTwoId = '77777777-7777-4777-8777-777777777772';
let departments = [
  { name: 'QA Main Department', employeeCount: 205, activeEmployeeCount: 205, inactiveEmployeeCount: 0, isCatalogEntry: true, isActive: true, departmentId: departmentOneId, parentDepartmentId: null, parentDepartmentName: null, departmentPath: 'QA Main Department', locationCatalogId: null, locationName: null, managerEmployeeId: null, managerName: null, managerIsActive: null },
  { name: 'QA Second Department', employeeCount: 0, activeEmployeeCount: 0, inactiveEmployeeCount: 0, isCatalogEntry: true, isActive: true, departmentId: departmentTwoId, parentDepartmentId: null, parentDepartmentName: null, departmentPath: 'QA Second Department', locationCatalogId: floorTwoId, locationName: 'Istanbul Office / Floor 2', managerEmployeeId: null, managerName: null, managerIsActive: null }
];
let companyMembers = [{ userId: memberUserId, roleKey: 'asset_operator', createdAt: '2026-10-01T09:00:00Z', employeeId: null,
  locationAccessMode: 'all', companyUnitAccessMode: 'all', companyUnitIds: [], isCurrentUser: false, name: 'QA Linked Member', email: 'linked.member@example.test', isInvitationPending: false, employeeName: null, employeeNumber: null }];
const locations = [
  { name: 'Istanbul Office / Floor 1', locationId: floorOneId, isCatalogEntry: true, isActive: true },
  { name: 'Istanbul Office / Floor 2', locationId: floorTwoId, isCatalogEntry: true, isActive: true }
];
const employeeRelations = {
  assignedAssets: [{ id: assetId, assetTag: 'QA-ASSET-001', name: 'QA Laptop', status: 'Deployed', location: 'Istanbul Office' }],
  assignedAccessories: [{ id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd', accessoryId, accessoryName: 'QA Dock', modelNumber: 'DOCK-01', quantity: 3, returnedQuantity: 1, remaining: 2, checkedOutAt: '2026-10-01T09:00:00Z', notes: null }],
  activeLicenses: [{ id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee', licenseId, name: 'QA Design Suite', publisher: 'QA Software', productVersion: '2026', expirationDate: '2027-10-01', assignedAt: '2026-10-02T09:00:00Z', notes: null }],
  consumableCheckouts: [{ id: 'ffffffff-ffff-4fff-8fff-ffffffffffff', consumableName: 'QA Toner', quantity: 2, occurredAt: '2026-10-03T09:00:00Z', notes: 'Synthetic checkout' }],
  responsibleMaintenances: [{ id: '99999999-9999-4999-8999-999999999999', assetId, assetTag: 'QA-ASSET-001', assetName: 'QA Laptop', maintenanceType: 'Preventive maintenance', status: 'Scheduled', plannedOn: '2026-10-20', startedOn: null, completedOn: null, supplier: 'QA Service' }]
};
const json = (response, status, body) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(appHtml); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': url.pathname.endsWith('.woff2') ? 'font/woff2' : 'text/css; charset=utf-8' });
    response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname === '/vendor/qrcodegen.js') {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(qrCodeGenerator); return;
  }
  if (request.method === 'GET' && appScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' }); response.end(appScripts.get(url.pathname)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }

  let body = null;
  let uploadedFile = null;
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    const chunks = []; for await (const chunk of request) chunks.push(chunk);
    const rawBody = Buffer.concat(chunks);
    const contentType = request.headers['content-type'] || '';
    if (contentType.includes('multipart/form-data')) {
      const boundary = contentType.match(/boundary=(?:"([^"]+)"|([^;]+))/i)?.slice(1).find(Boolean);
      assert.ok(boundary, 'Multipart upload must include its boundary.');
      const delimiter = Buffer.from(`--${boundary}`);
      const headerEnd = rawBody.indexOf(Buffer.from('\r\n\r\n'));
      const payloadStart = headerEnd + 4;
      const payloadEnd = rawBody.indexOf(Buffer.from(`\r\n--${boundary}`), payloadStart);
      const headersText = rawBody.subarray(0, headerEnd).toString('utf8');
      const filename = headersText.match(/filename="([^"]*)"/i)?.[1];
      assert.ok(filename, 'Multipart upload must contain a named file.');
      uploadedFile = { filename, contentType: headersText.match(/Content-Type:\s*([^\r\n]+)/i)?.[1] || 'application/octet-stream',
        bytes: rawBody.subarray(payloadStart, payloadEnd) };
    } else if (contentType.toLocaleLowerCase('en-US').startsWith('text/csv')) {
      body = rawBody.toString('utf8');
    } else {
      const bodyText = rawBody.toString('utf8');
      body = bodyText ? JSON.parse(bodyText) : null;
    }
  }
  requests.push({ method: request.method, path: url.pathname, query: Object.fromEntries(url.searchParams.entries()), csrf: request.headers['x-csrf-token'], body,
    contentType: request.headers['content-type'], upload: uploadedFile && { filename: uploadedFile.filename, contentType: uploadedFile.contentType, bytes: uploadedFile.bytes } });

  if (request.method === 'GET') {
    if (url.pathname === '/api/status') return json(response, 200, { status: 'ready', migrationCount: 96, pendingMigrationCount: 0 });
    if (url.pathname === '/api/setup/status') return json(response, 200, { setupRequired: false });
    if (url.pathname === '/api/session/csrf') return json(response, 200, { requestToken: csrfToken });
    if (url.pathname === '/api/session') return json(response, 200, { companyId, companyName: 'Synthetic Employee Company', userId,
      name: 'Synthetic User', role: syntheticRole, hasPhoto: false,
      availableCompanies: [{ id: companyId, name: 'Synthetic Employee Company', role: syntheticRole }],
      subscriptionAccess: { mode: 'active', status: 'Active', planName: 'Synthetic plan' } });
    if (url.pathname === '/api/company/branding') return json(response, 200, { name: 'Synthetic Employee Company', brandPrimaryColor: '#2563EB', hasLogo: false });
    if (url.pathname === '/api/company/appearance') return json(response, 200, {});
    if (url.pathname === '/api/company-units') return json(response, 200, { items: [
      { id: companyUnitId, name: 'QA Main Unit', path: 'QA Main Unit', isActive: true }
    ], total: 1 });
    if (url.pathname === '/api/employees') {
      const filtered = employees.filter(item => {
        const active = url.searchParams.get('active');
        if (active === 'true' && !item.isActive) return false;
        if (active === 'false' && item.isActive) return false;
        const startedFrom = url.searchParams.get('startedFrom');
        const startedTo = url.searchParams.get('startedTo');
        if (startedFrom && (!item.startedOn || item.startedOn < startedFrom)) return false;
        if (startedTo && (!item.startedOn || item.startedOn > startedTo)) return false;
        const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
        return !q || `${item.displayName} ${item.employeeNumber} ${item.email || ''} ${item.department || ''} ${item.jobTitle || ''} ${item.location || ''} ${item.companyUnitName || ''}`.toLocaleLowerCase('tr-TR').includes(q);
      });
      const payload = { items: filtered, total: filtered.length, page: Number(url.searchParams.get('page') || 1), pageSize: Number(url.searchParams.get('pageSize') || 50) };
      if (url.searchParams.get('active') === 'false') {
        response.writeHead(200, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
        response.flushHeaders();
        setTimeout(() => response.end(JSON.stringify(payload)), 200);
        return;
      }
      return json(response, 200, payload);
    }
    const profileMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)$/i);
    if (profileMatch) {
      const employee = employees.find(item => item.id === profileMatch[1]);
      if (!employee) return json(response, 404, { message: 'Employee not found' });
      const directReports = employees.filter(item => item.managerEmployeeId === employee.id).map(item => ({
        id: item.id, employeeNumber: item.employeeNumber, displayName: item.displayName, department: item.department, isActive: item.isActive, assignedAssetCount: 0
      }));
      return json(response, 200, { employee, ...employeeRelations,
        assignedAssetCount: employeeRelations.assignedAssets.length, assignedAccessoryCount: employeeRelations.assignedAccessories.length,
        activeLicenseCount: employeeRelations.activeLicenses.length, consumableCheckoutCount: employeeRelations.consumableCheckouts.length,
        responsibleMaintenanceCount: employeeRelations.responsibleMaintenances.length,
        responsibleMaintenances: employeeRelations.responsibleMaintenances, directReports, directReportCount: directReports.length, assetsVisible: true, page: 1, pageSize: 50 });
    }
    const handoverCollectionMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/handover-documents$/i);
    const handoverDetailMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/handover-documents\/([0-9a-f-]+)$/i);
    if (handoverCollectionMatch) return json(response, 200, handoverDocuments.filter(item => item.employeeId === handoverCollectionMatch[1]).map(({ snapshot, ...item }) => item));
    if (handoverDetailMatch) {
      const handover = handoverDocuments.find(item => item.employeeId === handoverDetailMatch[1] && item.id === handoverDetailMatch[2]);
      return handover ? json(response, 200, handover) : json(response, 404, { message: 'Handover document not found' });
    }
    if (/^\/api\/employees\/[0-9a-f-]+\/private-notes$/i.test(url.pathname)) return json(response, 200, { notes: privateNotes, maxLength: 4000 });
    if (/^\/api\/employees\/[0-9a-f-]+\/documents$/i.test(url.pathname)) return json(response, 200, { documents: employeeDocuments, maxFiles: 30, maxFileSizeBytes: 10485760 });
    const documentFileMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/documents\/([0-9a-f-]+)\/file$/i);
    if (documentFileMatch) {
      const documentInfo = employeeDocuments.find(item => item.id === documentFileMatch[2]);
      if (!documentInfo) return json(response, 404, { message: 'Document not found' });
      response.writeHead(200, { 'content-type': documentInfo.contentType, 'content-disposition': `attachment; filename="${documentInfo.originalFileName}"` });
      response.end(documentInfo.bytes); return;
    }
    const employeePhotoMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/photo$/i);
    if (employeePhotoMatch) {
      const photo = employeePhotos.get(employeePhotoMatch[1]);
      if (!photo) { response.writeHead(404); response.end(); return; }
      response.writeHead(200, { 'content-type': photo.contentType, 'cache-control': 'no-store' });
      response.end(photo.bytes); return;
    }
    if (url.pathname === '/api/employees/export.csv') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase('tr-TR');
      const department = url.searchParams.get('department');
      const selectedEmployee = employees.find(item => item.isActive && item.displayName.toLocaleLowerCase('tr-TR').includes(q) && item.department === department);
      if (!selectedEmployee) { response.writeHead(404); response.end(); return; }
      const cell = value => `"${String(value ?? '').replaceAll('"', '""')}"`;
      const exported = '\uFEFF' + [
        ['Personel No', 'Ad Soyad', 'E-posta', 'Departman', 'Görev Unvanı', 'Telefon', 'Konum', 'İşe Başlama Tarihi', 'Yönetici', 'Durum', 'Kayıt Tarihi', 'Yönetici Personel No'],
        [selectedEmployee.employeeNumber, selectedEmployee.displayName, selectedEmployee.email, selectedEmployee.department,
          selectedEmployee.jobTitle, selectedEmployee.phone, selectedEmployee.location, selectedEmployee.startedOn, '', 'Etkin',
          selectedEmployee.createdAt || '2026-10-17T05:00:00.0000000Z', '']
      ].map(row => row.map(cell).join(',')).join('\r\n') + '\r\n';
      response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8',
        'content-disposition': 'attachment; filename="envantertr-calisanlar-20261017.csv"', 'x-total-count': '1', 'cache-control': 'no-store' });
      response.end(exported); return;
    }
    if (url.pathname === '/api/locations') return json(response, 200, { items: locations, departmentCounts: { [floorOneId]: 0, [floorTwoId]: departments.filter(item => item.locationCatalogId === floorTwoId).length } });
    if (url.pathname === '/api/company-roles') return json(response, 200, { roles: [
      { roleKey: 'owner', name: 'Şirket sahibi', builtIn: true, isActive: true, permissionKeys: ['assets.read', 'assets.write', 'people.read', 'people.manage', 'company.settings'] },
      { roleKey: 'admin', name: 'Yönetici', builtIn: true, isActive: true, permissionKeys: ['assets.read', 'assets.write', 'people.read', 'people.manage', 'company.settings'] },
      { roleKey: 'asset_operator', name: 'Envanter operatörü', builtIn: true, isActive: true, permissionKeys: ['assets.read', 'assets.write', 'people.read'] },
      { roleKey: 'viewer', name: 'Görüntüleyici', builtIn: true, isActive: true, permissionKeys: ['assets.read', 'people.read'] }
    ] });
    if (url.pathname === '/api/members') return json(response, 200, { members: companyMembers, total: companyMembers.length, roles: ['admin', 'asset_operator', 'viewer'] });
    const memberLocationAccessMatch = url.pathname.match(/^\/api\/members\/([0-9a-f-]+)\/location-access$/i);
    if (request.method === 'GET' && memberLocationAccessMatch) {
      const member = companyMembers.find(item => item.userId === memberLocationAccessMatch[1]);
      if (!member) return json(response, 404, { message: 'Member not found' });
      return json(response, 200, { userId: member.userId, mode: member.locationAccessMode, roleKey: member.roleKey,
        locationIds: member.locationIds || [], locations: [
          { id: floorOneId, parentLocationId: null, name: 'Istanbul Office / Floor 1', path: 'Istanbul Office / Floor 1', isActive: true },
          { id: floorTwoId, parentLocationId: floorOneId, name: 'Room 2', path: 'Istanbul Office / Floor 1 / Room 2', isActive: true }
        ], companyUnitMode: member.companyUnitAccessMode || 'all', companyUnitIds: member.companyUnitIds || [],
        companyUnits: [{ id: '99999999-9999-4999-8999-999999999991', parentCompanyUnitId: null,
          name: 'QA Main Unit', code: 'QA-MAIN', unitType: 'organizational', isActive: true }] });
    }
    if (url.pathname === '/api/requests/employees') {
      const query = (url.searchParams.get('q') || '').trim().toLocaleLowerCase('tr-TR');
      if (query === 'qa') await new Promise(resolve => setTimeout(resolve, 550));
      const items = employees.filter(item => item.isActive && (!query || `${item.displayName} ${item.employeeNumber}`.toLocaleLowerCase('tr-TR').includes(query)))
        .map(({ id, displayName, employeeNumber }) => ({ id, displayName, employeeNumber }));
      return json(response, 200, { items, selfEmployee: null });
    }
    if (url.pathname === '/api/departments') {
      const locationId = url.searchParams.get('locationId');
      const items = departments.filter(item => !locationId || item.locationCatalogId === locationId);
      return json(response, 200, { items, total: items.length });
    }
    return json(response, 200, { items: [], total: 0, page: 1, pageSize: 50 });
  }

  if (request.headers['x-csrf-token'] !== csrfToken) return json(response, 400, { message: 'CSRF required' });
  const handoverCreateMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/handover-documents$/i);
  if (request.method === 'POST' && handoverCreateMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const employee = employees.find(item => item.id === handoverCreateMatch[1]);
    if (!employee) return json(response, 404, { message: 'Employee not found' });
    const id = '12121212-1212-4121-8121-121212121212';
    const snapshot = JSON.stringify({ version: 1, companyId, employeeId: employee.id, employeeName: employee.displayName,
      employeeNumber: employee.employeeNumber, department: employee.department, jobTitle: employee.jobTitle,
      createdAt: '2026-10-18T04:00:00Z',
      assets: [{ id: assetId, assetTag: 'QA-ASSET-001', name: 'QA Laptop', serialNumber: 'QA-SERIAL-001', status: 'Deployed', location: 'Istanbul Office', locationCatalogId: floorOneId }],
      accessories: [{ assignmentId: employeeRelations.assignedAccessories[0].id, accessoryId, name: 'QA Dock', modelNumber: 'DOCK-01', quantity: 2, checkedOutAt: '2026-10-01T09:00:00Z', locationCatalogId: floorOneId }],
      eulaAcceptances: [{ acceptanceId: 'abababab-abab-4bab-8bab-abababababab', licenseId, licenseName: 'QA Design Suite', productVersion: '2026', assignmentId: 'acacacac-acac-4cac-8cac-acacacacacac', attachmentId: 'adadadad-adad-4dad-8dad-adadadadadad', fileName: 'QA-EULA-v4.pdf', contentType: 'application/pdf', length: 2048, eulaVersion: '4.2', sha256: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef', acceptedAt: '2026-10-02T09:00:00Z', acceptedByEmail: 'qa.employee@example.test', isCurrentEula: true }] });
    const document = { id, companyId, employeeId: employee.id, documentNumber: 'QA-HO-001', snapshot,
      snapshotSha256: createHash('sha256').update(snapshot).digest('hex'), createdAt: '2026-10-18T04:00:00Z',
      recipient: { recipientName: employee.displayName, recipientSignedAt: null, recipientSignatureSha256: null, signaturePngDataUrl: null },
      issuer: { issuerName: null, issuerSignedAt: null, issuerSignatureSha256: null, signaturePngDataUrl: null }, isFullySigned: false };
    handoverDocuments.push(document);
    return json(response, 201, { ...document, snapshot: null });
  }
  const handoverSignMatch = url.pathname.match(/^\/api\/employee-handover-documents\/([0-9a-f-]+)\/sign$/i);
  if (request.method === 'POST' && handoverSignMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const document = handoverDocuments.find(item => item.id === handoverSignMatch[1]);
    if (!document) return json(response, 404, { message: 'Handover document not found' });
    const side = body.signerType;
    if (!['recipient', 'issuer'].includes(side)) return json(response, 400, { message: 'Invalid signer type' });
    const signature = document[side];
    const signedAtKey = side === 'recipient' ? 'recipientSignedAt' : 'issuerSignedAt';
    const hashKey = side === 'recipient' ? 'recipientSignatureSha256' : 'issuerSignatureSha256';
    if (signature[signedAtKey]) return json(response, 409, { message: 'Already signed' });
    if (typeof body.signaturePngDataUrl !== 'string' || !body.signaturePngDataUrl.startsWith('data:image/png;base64,'))
      return json(response, 400, { message: 'PNG signature required' });
    const bytes = Buffer.from(body.signaturePngDataUrl.slice('data:image/png;base64,'.length), 'base64');
    if (bytes.length < 64 || !bytes.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]))) return json(response, 400, { message: 'Invalid PNG signature' });
    if (side === 'recipient') {
      if (body.signerName !== employees.find(item => item.id === document.employeeId)?.displayName) return json(response, 400, { message: 'Recipient mismatch' });
      signature.recipientName = body.signerName;
    } else signature.issuerName = 'Synthetic User';
    signature[signedAtKey] = '2026-10-18T04:01:00Z';
    signature[hashKey] = createHash('sha256').update(bytes).digest('hex');
    signature.signaturePngDataUrl = body.signaturePngDataUrl;
    document.isFullySigned = Boolean(document.recipient.recipientSignedAt && document.issuer.issuerSignedAt);
    return json(response, 200, document);
  }
  const memberEmployeeMatch = url.pathname.match(/^\/api\/members\/([0-9a-f-]+)\/employee$/i);
  const memberLocationAccessMatch = url.pathname.match(/^\/api\/members\/([0-9a-f-]+)\/location-access$/i);
  if (request.method === 'PUT' && memberLocationAccessMatch) {
    if (!['owner', 'admin'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const member = companyMembers.find(item => item.userId === memberLocationAccessMatch[1]);
    if (!member) return json(response, 404, { message: 'Member not found' });
    if (!['all', 'restricted'].includes(body.mode) || (body.mode === 'restricted' && !body.locationIds?.length) ||
      (body.mode === 'all' && body.locationIds?.length) || !['all', 'restricted'].includes(body.companyUnitMode) ||
      (body.companyUnitMode === 'restricted' && !body.companyUnitIds?.length) ||
      (body.companyUnitMode === 'all' && body.companyUnitIds?.length)) return json(response, 400, { message: 'Invalid member access scope' });
    const changed = member.locationAccessMode !== body.mode || JSON.stringify(member.locationIds || []) !== JSON.stringify(body.locationIds || []) ||
      member.companyUnitAccessMode !== body.companyUnitMode || JSON.stringify(member.companyUnitIds || []) !== JSON.stringify(body.companyUnitIds || []);
    member.locationAccessMode = body.mode; member.locationIds = body.locationIds || [];
    member.companyUnitAccessMode = body.companyUnitMode; member.companyUnitIds = body.companyUnitIds || [];
    return json(response, 200, { userId: member.userId, mode: member.locationAccessMode, locationIds: member.locationIds,
      companyUnitMode: member.companyUnitAccessMode, companyUnitIds: member.companyUnitIds, changed });
  }
  if (request.method === 'PATCH' && memberEmployeeMatch) {
    if (!['owner', 'admin'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const member = companyMembers.find(item => item.userId === memberEmployeeMatch[1]);
    if (!member) return json(response, 404, { message: 'Member not found' });
    const employee = employees.find(item => item.id === body.employeeId && item.isActive);
    if (body.employeeId && !employee) return json(response, 400, { message: 'Choose an active employee in this company.' });
    if (employee && companyMembers.some(item => item.userId !== member.userId && item.employeeId === employee.id)) return json(response, 409, { message: 'Employee already linked.' });
    member.employeeId = employee?.id || null; member.employeeName = employee?.displayName || null; member.employeeNumber = employee?.employeeNumber || null;
    return json(response, 200, { userId: member.userId, employeeId: member.employeeId, employeeName: member.employeeName, employeeNumber: member.employeeNumber });
  }
  const departmentManagerMatch = url.pathname.match(/^\/api\/departments\/([0-9a-f-]+)\/manager$/i);
  if (request.method === 'PATCH' && departmentManagerMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const entry = departments.find(item => item.departmentId === departmentManagerMatch[1]);
    if (!entry) return json(response, 404, { message: 'Department not found' });
    if (!entry.isActive) return json(response, 409, { message: 'Arşivlenmiş departmana yetkili atanamaz.' });
    const manager = employees.find(item => item.id === body.employeeId && item.isActive);
    if (body.employeeId && !manager) return json(response, 400, { message: 'Yetkili etkin bir çalışan olmalıdır.' });
    entry.managerEmployeeId = manager?.id || null; entry.managerName = manager?.displayName || null; entry.managerIsActive = manager ? true : null;
    return json(response, 200, { id: entry.departmentId, managerEmployeeId: entry.managerEmployeeId, managerName: entry.managerName });
  }
  if (request.method === 'POST' && url.pathname === '/api/departments') {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const parent = departments.find(item => item.departmentId === body.parentDepartmentId);
    const location = locations.find(item => item.locationId === body.locationCatalogId);
    const entry = { name: body.name, employeeCount: 0, activeEmployeeCount: 0, inactiveEmployeeCount: 0, isCatalogEntry: true, isActive: true,
      departmentId: `88888888-8888-4888-8888-${String(departments.length + 1).padStart(12, '0')}`,
      parentDepartmentId: parent?.departmentId || null, parentDepartmentName: parent?.departmentPath || null,
      departmentPath: parent ? `${parent.departmentPath} / ${body.name}` : body.name,
      locationCatalogId: location?.locationId || null, locationName: location?.name || null };
    departments.push(entry);
    return json(response, 201, entry);
  }
  const departmentActiveMatch = url.pathname.match(/^\/api\/departments\/([0-9a-f-]+)\/active$/i);
  if (request.method === 'PATCH' && departmentActiveMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const entry = departments.find(item => item.departmentId === departmentActiveMatch[1]);
    if (!entry) return json(response, 404, { message: 'Department not found' });
    if (body.isActive && entry.parentDepartmentId && !departments.find(item => item.departmentId === entry.parentDepartmentId)?.isActive) return json(response, 409, { message: 'Önce üst departmanı geri yükleyin.' });
    if (!body.isActive && departments.some(item => item.parentDepartmentId === entry.departmentId && item.isActive)) return json(response, 409, { message: 'Önce etkin alt departmanları arşivleyin.' });
    entry.isActive = body.isActive; return json(response, 200, { id: entry.departmentId, isActive: entry.isActive });
  }
  const departmentMoveMatch = url.pathname.match(/^\/api\/departments\/([0-9a-f-]+)\/move-employees$/i);
  if (request.method === 'POST' && departmentMoveMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const source = departments.find(item => item.departmentId === departmentMoveMatch[1]);
    const target = departments.find(item => item.departmentId === body.targetDepartmentId && item.isActive);
    if (!source) return json(response, 404, { message: 'Department not found' });
    if (!target || target === source) return json(response, 400, { message: 'Geçerli etkin hedef departman seçin.' });
    const moved = Math.min(Math.max(1, Math.min(body.batchSize || 100, 100)), source.employeeCount);
    source.employeeCount -= moved; source.activeEmployeeCount = Math.max(0, source.activeEmployeeCount - moved);
    target.employeeCount += moved; target.activeEmployeeCount += moved;
    return json(response, 200, { sourceDepartmentId: source.departmentId, sourceName: source.name, targetDepartmentId: target.departmentId,
      targetName: target.name, moved, remaining: source.employeeCount, complete: source.employeeCount === 0 });
  }
  const departmentMatch = url.pathname.match(/^\/api\/departments\/([0-9a-f-]+)$/i);
  if (request.method === 'PUT' && departmentMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const entry = departments.find(item => item.departmentId === departmentMatch[1]);
    const parent = departments.find(item => item.departmentId === body.parentDepartmentId);
    const location = locations.find(item => item.locationId === body.locationCatalogId);
    if (!entry || (body.parentDepartmentId && !parent) || (body.locationCatalogId && !location)) return json(response, 404, { message: 'Department reference not found' });
    entry.name = body.name; entry.parentDepartmentId = parent?.departmentId || null;
    entry.parentDepartmentName = parent?.departmentPath || null; entry.departmentPath = parent ? `${parent.departmentPath} / ${body.name}` : body.name;
    entry.locationCatalogId = location?.locationId || null; entry.locationName = location?.name || null;
    for (const child of departments.filter(item => item.parentDepartmentId === entry.departmentId)) child.departmentPath = `${entry.departmentPath} / ${child.name}`;
    return json(response, 200, { id: entry.departmentId, name: entry.name, parentDepartmentId: entry.parentDepartmentId, locationCatalogId: entry.locationCatalogId, employeeCount: entry.employeeCount });
  }
  const employeePhotoMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/photo$/i);
  if (employeePhotoMatch && request.method === 'POST') {
    if (!['owner', 'admin', 'asset_manager', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const bytes = uploadedFile?.bytes;
    const signature = bytes?.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])) ||
      (bytes?.[0] === 0xff && bytes?.[1] === 0xd8 && bytes?.[2] === 0xff) ||
      (bytes?.subarray(0, 4).toString('ascii') === 'RIFF' && bytes?.subarray(8, 12).toString('ascii') === 'WEBP');
    if (!bytes?.length || bytes.length > 2 * 1024 * 1024 || !signature) return json(response, 400, { message: 'Yalnızca doğrulanmış PNG, JPEG veya WebP fotoğraf yüklenebilir; üst sınır 2 MB.' });
    const contentType = bytes[0] === 0x89 ? 'image/png' : bytes[0] === 0xff ? 'image/jpeg' : 'image/webp';
    employeePhotos.set(employeePhotoMatch[1], { bytes, contentType });
    return json(response, 200, { photoUrl: `${url.pathname}?v=synthetic` });
  }
  if (employeePhotoMatch && request.method === 'DELETE') {
    if (!['owner', 'admin', 'asset_manager', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    employeePhotos.delete(employeePhotoMatch[1]);
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'PUT' && /^\/api\/employees\/[0-9a-f-]+\/private-notes$/i.test(url.pathname)) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    if (typeof body?.notes !== 'string' || body.notes.length > 4000) return json(response, 400, { message: 'Notlar 4.000 karakterle sınırlı.' });
    privateNotes = body.notes.trim() || null;
    return json(response, 200, { updated: true, notes: privateNotes, maxLength: 4000 });
  }
  if (request.method === 'POST' && url.pathname === '/api/employees/import/preview.csv') {
    const normalizedCsv = typeof body === 'string' ? body.replace(/^\uFEFF/, '') : '';
    if (!normalizedCsv.startsWith('Personel No,Ad Soyad,') && !normalizedCsv.startsWith('"Personel No","Ad Soyad",'))
      return json(response, 422, { isValid: false, rowCount: 0, errors: ['Geçersiz test CSV dosyası.'] });
    const existingEmployee = employees.find(item => normalizedCsv.includes(item.employeeNumber));
    if (existingEmployee)
      return json(response, 422, { isValid: false, rowCount: 1, errors: [`Şirkette zaten kayıtlı personel numaraları var: ${existingEmployee.employeeNumber}`], message: 'Dosyada düzeltilmesi gereken alanlar bulundu. Hiçbir çalışan eklenmedi.' });
    employeeImportPreview = body;
    return json(response, 200, { isValid: true, rowCount: 1, errors: [], message: '1 yeni çalışan kaydı doğrulandı. Önizleme hiçbir kayıt eklemedi.' });
  }
  if (request.method === 'POST' && url.pathname === '/api/employees/import.csv') {
    if (typeof body !== 'string' || body !== employeeImportPreview) return json(response, 409, { message: 'Önizleme geçersiz; dosyayı yeniden doğrulayın.' });
    const [employeeNumber, displayName, email, department, jobTitle, phone, location, startedOn] = body.trim().split(/\r?\n/).at(-1).split(',');
    const importedEmployee = { id: '99999999-1111-4999-8999-999999999999', employeeNumber, displayName, email, department, jobTitle, phone, location, startedOn, isActive: true, managerEmployeeId: null, managerName: null };
    employees.push(importedEmployee); employeeImportPreview = null;
    return json(response, 200, { imported: 1, message: '1 çalışan kaydı eklendi.' });
  }
  if (request.method === 'POST' && /^\/api\/employees\/[0-9a-f-]+\/documents$/i.test(url.pathname)) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    if (!uploadedFile || !uploadedFile.bytes.length || !uploadedFile.bytes.subarray(0, 5).equals(Buffer.from('%PDF-'))) return json(response, 400, { message: 'Dosya geçerli bir PDF değil.' });
    if (uploadedFile.bytes.length > 10485760 || employeeDocuments.length >= 30) return json(response, 400, { message: 'Dosya sınırı aşıldı.' });
    const created = { id: '66666666-6666-4666-8666-666666666666', originalFileName: uploadedFile.filename,
      contentType: 'application/pdf', length: uploadedFile.bytes.length, createdAt: '2026-10-17T03:00:00Z', uploadedBy: 'Synthetic User', bytes: uploadedFile.bytes };
    employeeDocuments.push(created);
    return json(response, 201, { id: created.id, originalFileName: created.originalFileName, contentType: created.contentType, length: created.length });
  }
  const deleteEmployeeDocumentMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/documents\/([0-9a-f-]+)$/i);
  if (request.method === 'DELETE' && deleteEmployeeDocumentMatch) {
    if (!['owner', 'admin', 'hr_operator'].includes(syntheticRole)) return json(response, 403, { message: 'Forbidden' });
    const index = employeeDocuments.findIndex(item => item.id === deleteEmployeeDocumentMatch[2]);
    if (index < 0) return json(response, 404, { message: 'Document not found' });
    employeeDocuments.splice(index, 1);
    response.writeHead(204); response.end(); return;
  }
  if (request.method === 'POST' && url.pathname === '/api/employees') {
    const manager = employees.find(item => item.id === body.managerEmployeeId);
    const created = { ...body, id: employeeId, isActive: true, companyUnitName: body.companyUnitId ? 'QA Main Unit' : null,
      managerEmployeeId: manager?.id || null, managerName: manager?.displayName || null };
    employees.push(created); return json(response, 201, created);
  }
  const managerMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)\/manager$/i);
  if (request.method === 'PATCH' && managerMatch) {
    const employee = employees.find(item => item.id === managerMatch[1]);
    const proposedManager = employees.find(item => item.id === body.managerEmployeeId);
    if (!employee || (body.managerEmployeeId && !proposedManager)) return json(response, 404, { message: 'Employee not found' });
    let cursor = proposedManager;
    const seen = new Set();
    while (cursor && !seen.has(cursor.id)) {
      if (cursor.id === employee.id) return json(response, 400, { errors: { managerEmployeeId: ['Bu seçim çalışan-yönetici döngüsü oluşturur.'] } });
      seen.add(cursor.id);
      cursor = employees.find(item => item.id === cursor.managerEmployeeId);
    }
    employee.managerEmployeeId = proposedManager?.id || null;
    employee.managerName = proposedManager?.displayName || null;
    return json(response, 200, { employeeId: employee.id, managerEmployeeId: employee.managerEmployeeId, managerName: employee.managerName });
  }
  const editMatch = url.pathname.match(/^\/api\/employees\/([0-9a-f-]+)$/i);
  if (request.method === 'PATCH' && editMatch) {
    const employee = employees.find(item => item.id === editMatch[1]);
    if (!employee) return json(response, 404, { message: 'Employee not found' });
    Object.assign(employee, body); return json(response, 200, employee);
  }
  json(response, 405, { message: 'Unexpected write' });
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address(); assert.ok(address && typeof address === 'object');
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
const page = await context.newPage();
const pageErrors = []; page.on('pageerror', error => pageErrors.push(error.message));
try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await page.locator('#authLayer').waitFor({ state: 'hidden' });
  await page.locator('#sidebar [data-group="people"]').click({ force: true });
  await page.locator('#sidebar [data-page="Çalışan kayıtları"]').click();
  await page.locator('#employeesPage').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#employeesPage').isVisible(), true, 'Employee directory must be open before adding a record.');
  await page.locator('#employeeRows').getByRole('button', { name: 'QA Supervisor, çalışan profilini aç' }).waitFor();

  const employeeListResponse = predicate => page.waitForResponse(response => {
    if (response.request().method() !== 'GET') return false;
    const url = new URL(response.url());
    return url.pathname === '/api/employees' && predicate(url.searchParams);
  }).then(async response => {
    assert.equal(response.status(), 200);
    const payload = await response.json();
    const expectedIds = payload.items.map(item => item.id).sort();
    await page.waitForFunction(ids => {
      const actual = [...document.querySelectorAll('#employeeRows input[data-employee-id]')]
        .map(input => input.dataset.employeeId).sort();
      return JSON.stringify(actual) === JSON.stringify(ids);
    }, expectedIds);
    return response;
  });
  assert.equal(await page.locator('#employeeRows tr').count(), 3, 'Default active filtering must include active employees and omit the passive record.');
  assert.match(await page.locator('#employeeRows').innerText(), /QA Main Unit/, 'The employee table must display the company unit derived from the linked location.');
  const unitSearchResponse = employeeListResponse(params => params.get('q') === 'QA Main Unit');
  await page.locator('#employeeSearch').fill('QA Main Unit');
  await unitSearchResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 1, 'Company-unit name search must find employees assigned to a location linked to that unit.');
  assert.match(await page.locator('#employeeRows').innerText(), /QA Supervisor/);
  const clearUnitSearchResponse = employeeListResponse(params => !params.has('q'));
  await page.locator('#employeeSearch').fill('');
  await clearUnitSearchResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 3);
  assert.equal((await page.locator('#employeeRows').innerText()).includes('QA Inactive Employee'), false);
  // This filters active/passive and inclusive start-date ranges.
  let listResponse = employeeListResponse(params => params.get('active') === 'false');
  await page.locator('#employeeState').selectOption('false');
  await listResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 1, 'Passive filter must show only passive employees.');
  assert.match(await page.locator('#employeeRows').innerText(), /QA Inactive Employee/);

  listResponse = employeeListResponse(params => !params.has('active'));
  await page.locator('#employeeState').selectOption('');
  await listResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 4, 'All status filter must include active and passive employees.');

  listResponse = employeeListResponse(params => params.get('startedFrom') === '2020-03-10');
  await page.locator('#employeeStartedFrom').fill('2020-03-10');
  await listResponse;
  listResponse = employeeListResponse(params => params.get('startedFrom') === '2020-03-10' && params.get('startedTo') === '2020-12-31');
  await page.locator('#employeeStartedTo').fill('2020-12-31');
  await listResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 2, 'Inclusive date-range boundaries must include the start and end date, while excluding out-of-range and undated employees.');
  const dateRangeRows = await page.locator('#employeeRows').innerText();
  assert.match(dateRangeRows, /QA Supervisor/);
  assert.match(dateRangeRows, /QA Inactive Employee/);
  assert.doesNotMatch(dateRangeRows, /QA Supervisor II|QA Undated Employee/);

  listResponse = employeeListResponse(params => !params.has('startedFrom') && !params.has('startedTo'));
  await page.locator('#clearEmployeeStartedDateFilter').click();
  await listResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 4, 'Clearing the date range must restore all status-filtered employees.');

  listResponse = employeeListResponse(params => params.get('startedFrom') === '2020-12-31' && !params.has('startedTo'));
  await page.locator('#employeeStartedFrom').fill('2020-12-31');
  await listResponse;
  listResponse = employeeListResponse(params => params.get('startedFrom') === '2020-03-10' && params.get('startedTo') === '2020-03-10');
  await page.locator('#employeeStartedTo').fill('2020-03-10');
  await listResponse;
  await page.locator('#toast').getByText('Tarih aralığı başlangıç ve bitiş tarihleriyle eşleştirildi.').waitFor();
  assert.equal(await page.locator('#employeeRows tr').count(), 1, 'The UI must normalize a reversed range to a valid same-day range before querying the API.');
  listResponse = employeeListResponse(params => !params.has('startedFrom') && !params.has('startedTo'));
  await page.locator('#clearEmployeeStartedDateFilter').click();
  await listResponse;
  await page.locator('#employeeState').selectOption('true');
  await page.locator('#employeeRows').getByRole('button', { name: 'QA Supervisor, çalışan profilini aç' }).waitFor();
  assert.equal(await page.locator('#employeeRows tr').count(), 3, 'The existing employee editing flow must start with the active status restored.');

  await page.locator('#newEmployeeButton').click();
  const createForm = page.locator('#newEmployeeForm');
  await page.locator('#newEmployeeDialog').waitFor({ state: 'visible' });
  const nameField = createForm.locator('[name="displayName"]');
  await nameField.fill('QA Employee');
  const invalidRequiredSubmit = createForm.locator('button[type="submit"]');
  await invalidRequiredSubmit.click();
  assert.equal(await page.locator('#newEmployeeDialog').evaluate(dialog => dialog.open), true, 'Required employee number must prevent submission.');
  assert.equal(requests.filter(item => item.method === 'POST' && item.path === '/api/employees').length, 0, 'Invalid create must not write to the API.');

  await createForm.locator('[name="employeeNumber"]').fill('QA-EMP-001');
  await createForm.locator('[name="email"]').fill('qa.employee@example.invalid');
  await createForm.locator('[name="department"]').fill('Operations');
  await createForm.locator('[name="jobTitle"]').fill('Field technician');
  await createForm.locator('[name="phone"]').fill('+90 555 111 22 33');
  await createForm.locator('[name="location"]').fill('Istanbul Office');
  await createForm.locator('[name="startedOn"]').fill('2022-04-05');
  await createForm.locator('[name="companyUnitId"]').selectOption(companyUnitId);
  await page.locator('#newEmployeeManagerId').selectOption(managerId);
  await invalidRequiredSubmit.click();
  await page.locator('#newEmployeeDialog').waitFor({ state: 'hidden' });
  await page.locator('#employeeRows').getByText('QA Employee').waitFor();
  const createRequest = requests.find(item => item.method === 'POST' && item.path === '/api/employees');
  assert.ok(createRequest, 'Creating an employee must post to the employee-record endpoint.');
  assert.equal(createRequest.csrf, csrfToken);
  assert.deepEqual(createRequest.body, { displayName: 'QA Employee', employeeNumber: 'QA-EMP-001', email: 'qa.employee@example.invalid',
    department: 'Operations', jobTitle: 'Field technician', phone: '+90 555 111 22 33', location: 'Istanbul Office', startedOn: '2022-04-05', managerEmployeeId: managerId, companyUnitId });
  assert.equal(requests.some(item => /\/api\/(members|register|invitations)(\/|$)/.test(item.path)), false, 'An employee record must not open a login or invitation account.');

  const row = page.locator('#employeeRows tr').filter({ hasText: 'QA Employee' });
  await row.getByRole('button', { name: 'Düzenle' }).click();
  const editForm = page.locator('#editEmployeeForm');
  await page.locator('#editEmployeeDialog').waitFor({ state: 'visible' });
  await editForm.locator('[name="displayName"]').fill('QA Employee Updated');
  await editForm.locator('[name="employeeNumber"]').fill('QA-EMP-002');
  await editForm.locator('[name="department"]').fill('IT');
  await editForm.locator('[name="jobTitle"]').fill('Support technician');
  await editForm.locator('[name="companyUnitId"]').selectOption(companyUnitId);
  await editForm.locator('button[type="submit"]').click();
  await page.locator('#editEmployeeDialog').waitFor({ state: 'hidden' });
  await page.locator('#employeeRows').getByText('QA Employee Updated').waitFor();
  const editRequest = requests.find(item => item.method === 'PATCH' && item.path === `/api/employees/${employeeId}`);
  assert.ok(editRequest, 'Editing must update the selected employee record.');
  assert.equal(editRequest.csrf, csrfToken);
  assert.equal(editRequest.body.employeeNumber, 'QA-EMP-002');
  assert.equal(editRequest.body.email, 'qa.employee@example.invalid');
  assert.equal(editRequest.body.department, 'IT');
  assert.equal(editRequest.body.jobTitle, 'Support technician');
  assert.equal(editRequest.body.phone, '+90 555 111 22 33');
  assert.equal(editRequest.body.location, 'Istanbul Office');
  assert.equal(editRequest.body.startedOn, '2022-04-05');
  assert.equal(editRequest.body.companyUnitId, companyUnitId);
  assert.equal(editRequest.body.clearCompanyUnit, false);
  assert.equal(requests.some(item => item.method === 'POST' && /\/api\/(members|register|invitations)(\/|$)/.test(item.path)), false);

  const employeeRow = page.locator('#employeeRows tr').filter({ hasText: 'QA Employee Updated' });
  await employeeRow.getByRole('button', { name: 'QA Employee Updated, çalışan profilini aç' }).click();
  const profile = page.locator('#employeeDialog');
  await profile.waitFor({ state: 'visible' });
  await page.locator('#employeeAssetRows').getByText('QA Laptop · QA-ASSET-001').waitFor();
  await page.locator('#employeeAccessoryRows').getByRole('button', { name: 'QA Dock' }).waitFor();
  await page.locator('#employeeLicenseRows').getByRole('button', { name: 'QA Design Suite' }).waitFor();
  await page.locator('#employeeConsumableRows').getByText('QA Toner').waitFor();
  assert.match(await page.locator('#employeeAssignedCount').innerText(), /1/);
  assert.match(await page.locator('#employeeAccessoryCount').innerText(), /1/);
  assert.match(await page.locator('#employeeLicenseCount').innerText(), /1/);
  assert.match(await page.locator('#employeeConsumableCount').innerText(), /1/);
  assert.match(await page.locator('#employeeAccessoryRows').innerText(), /3\s+1\s+2/,
    'Accessory issue, returned, and remaining quantities must all be rendered.');
  assert.match(await page.locator('#employeeLicenseRows').innerText(), /QA Software.*2026/,
    'The license list must include its publisher and product version.');
  assert.match(await page.locator('#employeeConsumableRows').innerText(), /Synthetic checkout/,
    'The consumable list must include the movement note.');
  assert.equal(await page.locator('#employeeAssetRows tr').count(), 1);
  assert.equal(await page.locator('#employeeAccessoryRows tr').count(), 1);
  assert.equal(await page.locator('#employeeLicenseRows tr').count(), 1);
  assert.equal(await page.locator('#employeeConsumableRows tr').count(), 1);
  await page.locator('#employeeMaintenanceRows').getByRole('button', { name: 'QA Laptop · QA-ASSET-001' }).waitFor();
  assert.match(await page.locator('#employeeMaintenanceCount').innerText(), /1/);
  assert.match(await page.locator('#employeeMaintenanceRows').innerText(), /Preventive maintenance.*QA Service.*Planlandı/,
    'The employee profile must render the assigned maintenance type, provider, and state.');
  assert.equal(await page.locator('#employeeMaintenanceRows tr').count(), 1);
  const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=', 'base64');
  const photoFile = page.locator('#employeePhotoFile');
  const photoImage = page.locator('#employeeProfilePhoto');
  assert.equal(await page.locator('#employeePhotoPanel .btn.primary').isVisible(), true, 'Authorized profile editing must expose employee photo upload.');
  await photoFile.setInputFiles({ name: 'employee.png', mimeType: 'image/png', buffer: png });
  await page.locator('#toast').getByText('Çalışan profil fotoğrafı kaydedildi.').waitFor();
  await photoImage.waitFor({ state: 'visible' });
  assert.equal(employeePhotos.get(employeeId).bytes.equals(png), true, 'Successful photo upload must persist the selected image in the API-backed state.');
  const photoUpload = requests.find(item => item.method === 'POST' && item.path === `/api/employees/${employeeId}/photo`);
  assert.ok(photoUpload && photoUpload.csrf === csrfToken && photoUpload.contentType?.startsWith('multipart/form-data; boundary='));
  assert.equal(photoUpload.upload.filename, 'employee.png');
  assert.equal(photoUpload.upload.bytes.equals(png), true);

  const writesBeforeOversizedPhoto = requests.filter(item => item.method === 'POST' && item.path === `/api/employees/${employeeId}/photo`).length;
  await photoFile.setInputFiles({ name: 'large.png', mimeType: 'image/png', buffer: Buffer.concat([png, Buffer.alloc(2 * 1024 * 1024)]) });
  await page.locator('#toast').getByText('Bu dosya 2 MB sınırını aşıyor. Lütfen daha küçük bir fotoğraf seçin.').waitFor();
  assert.equal(requests.filter(item => item.method === 'POST' && item.path === `/api/employees/${employeeId}/photo`).length, writesBeforeOversizedPhoto,
    'An oversized file must be rejected before an API write.');

  const removePhoto = page.locator('#removeEmployeePhoto');
  await removePhoto.click();
  const photoConfirm = page.locator('#employeeActionDialog');
  await photoConfirm.waitFor({ state: 'visible' });
  await photoConfirm.getByRole('button', { name: 'Vazgeç' }).click();
  await photoConfirm.waitFor({ state: 'hidden' });
  assert.equal(requests.filter(item => item.method === 'DELETE' && item.path === `/api/employees/${employeeId}/photo`).length, 0,
    'Canceling photo removal must not write to the API.');
  assert.equal(employeePhotos.has(employeeId), true, 'Canceling removal must preserve the photo.');

  await removePhoto.click();
  await photoConfirm.waitFor({ state: 'visible' });
  await photoConfirm.locator('#employeeActionSubmit').click();
  await page.locator('#toast').getByText('Çalışan profil fotoğrafı kaldırıldı.').waitFor();
  assert.equal(await photoImage.isHidden(), true, 'Confirmed photo removal must clear the profile preview.');
  assert.equal(employeePhotos.has(employeeId), false);
  const photoDelete = requests.find(item => item.method === 'DELETE' && item.path === `/api/employees/${employeeId}/photo`);
  assert.ok(photoDelete && photoDelete.csrf === csrfToken, 'Confirmed photo removal must issue one CSRF-protected request.');
  assert.equal(requests.filter(item => item.method === 'DELETE' && item.path === `/api/employees/${employeeId}/photo`).length, 1);

  await page.locator('#employeeDetailManager').getByText('QA Supervisor').waitFor();
  const managerSelect = page.locator('#employeeManagerSelect');
  await managerSelect.selectOption(alternateManagerId);
  await page.locator('#saveEmployeeManager').click();
  await page.locator('#employeeDetailManager').getByText('QA Supervisor II').waitFor();
  const managerUpdate = requests.find(item => item.method === 'PATCH' && item.path === `/api/employees/${employeeId}/manager`);
  assert.ok(managerUpdate, 'Employee profile manager editor must save the selected manager.');
  assert.equal(managerUpdate.csrf, csrfToken);
  assert.deepEqual(managerUpdate.body, { managerEmployeeId: alternateManagerId });

  // This maintains manager/direct-report hierarchy and rejects cycles.
  await page.locator('#closeEmployeeDetails').click();
  const alternateManagerRow = page.locator('#employeeRows tr').filter({ hasText: 'QA Supervisor II' });
  await alternateManagerRow.getByRole('button', { name: 'QA Supervisor II, çalışan profilini aç' }).click();
  await profile.waitFor({ state: 'visible' });
  await page.locator('#employeeDirectReports').getByText('QA Employee Updated').waitFor();
  // It rejects cycles in the manager/direct-report hierarchy.
  await page.locator('#employeeManagerSelect').selectOption(employeeId);
  await page.locator('#saveEmployeeManager').click();
  await page.locator('#toast').getByText('Bu seçim çalışan-yönetici döngüsü oluşturur.').waitFor();
  assert.equal(await page.locator('#employeeDetailManager').innerText(), 'Yönetici tanımlanmamış', 'A rejected cycle must leave the manager profile unchanged.');
  const managerWrites = requests.filter(item => item.method === 'PATCH' && item.path.endsWith('/manager'));
  assert.equal(managerWrites.length, 2, 'Manager reassignment and rejected cycle must be the only manager writes.');
  assert.deepEqual(managerWrites.map(item => item.body.managerEmployeeId), [alternateManagerId, employeeId]);
  assert.ok(managerWrites.every(item => item.csrf === csrfToken), 'Manager writes must include the session CSRF token.');

  await page.locator('#closeEmployeeDetails').click();
  await page.locator('#employeeRows tr').filter({ hasText: 'QA Employee Updated' }).getByRole('button', { name: 'QA Employee Updated, çalışan profilini aç' }).click();
  await profile.waitFor({ state: 'visible' });
  const notesPanel = page.locator('#employeePrivateNotesPanel');
  await notesPanel.waitFor({ state: 'visible' });
  const notesField = page.locator('#employeePrivateNotes');
  await notesField.fill('  HR-PRIVATE-SENTINEL — işe dönüş planı  ');
  await page.locator('#saveEmployeePrivateNotes').click();
  await page.locator('#toast').getByText('Özel İK notları kaydedildi; işlem denetim kaydına işlendi.').waitFor();
  assert.equal(await notesField.inputValue(), 'HR-PRIVATE-SENTINEL — işe dönüş planı', 'Saved notes must display the normalized persisted value.');
  const notesWrite = requests.find(item => item.method === 'PUT' && item.path === `/api/employees/${employeeId}/private-notes`);
  assert.ok(notesWrite, 'Authorized HR notes must save to their dedicated endpoint.');
  assert.equal(notesWrite.csrf, csrfToken);
  assert.deepEqual(notesWrite.body, { notes: '  HR-PRIVATE-SENTINEL — işe dönüş planı  ' });
  assert.equal(privateNotes, 'HR-PRIVATE-SENTINEL — işe dönüş planı');
  await page.locator('#closeEmployeeDetails').click();
  await page.locator('#employeeRows tr').filter({ hasText: 'QA Employee Updated' }).getByRole('button', { name: 'QA Employee Updated, çalışan profilini aç' }).click();
  await profile.waitFor({ state: 'visible' });
  await page.locator('#employeePrivateNotes').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#employeePrivateNotes').inputValue(), 'HR-PRIVATE-SENTINEL — işe dönüş planı', 'Private HR notes must persist after reopening the profile.');

  await page.locator('#printEmployeeHandoff').click();
  const handoverDialog = page.locator('#employeeHandoverDialog');
  await handoverDialog.waitFor({ state: 'visible' });
  const handoverCreateResponse = page.waitForResponse(response => response.request().method() === 'POST' &&
    new URL(response.url()).pathname === `/api/employees/${employeeId}/handover-documents`);
  await page.locator('#createEmployeeHandover').click();
  assert.equal((await handoverCreateResponse).status(), 201, 'Creating an immutable handover snapshot must persist through the API.');
  await page.locator('#employeeHandoverSigning').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#employeeHandoverDocumentNumber').innerText(), 'QA-HO-001 · QA Employee Updated');
  const handoverCreateRequest = requests.find(item => item.method === 'POST' && item.path === `/api/employees/${employeeId}/handover-documents`);
  assert.equal(handoverCreateRequest?.csrf, csrfToken, 'Creating a handover document must send CSRF protection.');
  const drawSignature = async selector => {
    const canvas = page.locator(selector), box = await canvas.boundingBox();
    assert.ok(box && box.width > 100 && box.height > 50, `Signature canvas ${selector} must have a usable visible drawing area.`);
    await page.mouse.move(box.x + 18, box.y + box.height * 0.6);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width * 0.28, box.y + box.height * 0.35, { steps: 5 });
    await page.mouse.move(box.x + box.width * 0.52, box.y + box.height * 0.62, { steps: 5 });
    await page.mouse.move(box.x + box.width * 0.8, box.y + box.height * 0.32, { steps: 5 });
    await page.mouse.up();
    assert.equal(await canvas.evaluate(element => element.dataset.hasInk), 'true', `Drawing on ${selector} must be captured.`);
  };
  await drawSignature('#employeeHandoverRecipientCanvas');
  await page.locator('#clearEmployeeHandoverRecipient').click();
  assert.equal(await page.locator('#employeeHandoverRecipientCanvas').evaluate(element => element.dataset.hasInk), 'false', 'The recipient clear action must erase the current signature.');
  await drawSignature('#employeeHandoverRecipientCanvas');
  await drawSignature('#employeeHandoverIssuerCanvas');
  await page.locator('#clearEmployeeHandoverIssuer').click();
  assert.equal(await page.locator('#employeeHandoverIssuerCanvas').evaluate(element => element.dataset.hasInk), 'false', 'The issuer clear action must erase the current signature.');
  await drawSignature('#employeeHandoverIssuerCanvas');
  const printPopupPromise = page.waitForEvent('popup');
  await page.locator('#saveEmployeeHandoverSignatures').click();
  const printPopup = await printPopupPromise;
  await printPopup.locator('body .head strong').waitFor({ state: 'visible' });
  assert.match(await printPopup.locator('body').innerText(), /QA-HO-001.*QA Employee Updated|QA-HO-001/s, 'Saving both signatures must open the generated printable document.');
  assert.match(await printPopup.locator('body').innerText(), /EULA ve kabul kayıtları/);
  assert.match(await printPopup.locator('body').innerText(), /QA-EULA-v4\.pdf/);
  assert.match(await printPopup.locator('body').innerText(), /qa\.employee@example\.test/);
  assert.match(await printPopup.locator('body').innerText(), /0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef/);
  assert.equal(await printPopup.locator('.sign img').count(), 2, 'The printable PDF source must contain both captured signatures.');
  assert.match(await printPopup.locator('.foot').innerText(), /nitelikli elektronik imza|qualified electronic signature/i,
    'The print form must explain the legal scope of a drawn signature.');
  const pdfBytes = await printPopup.pdf({ format: 'A4', printBackground: true });
  assert.ok(pdfBytes.subarray(0, 5).equals(Buffer.from('%PDF-')) && pdfBytes.length > 1_000,
    'The browser print document must produce a non-empty PDF file.');
  const handoverSignWrites = requests.filter(item => item.method === 'POST' && item.path === `/api/employee-handover-documents/${handoverDocuments[0].id}/sign`);
  assert.equal(handoverSignWrites.length, 2, 'Saving must submit exactly one recipient signature and one issuer signature.');
  assert.ok(handoverSignWrites.every(item => item.csrf === csrfToken && item.body.signaturePngDataUrl.startsWith('data:image/png;base64,')),
    'Both persisted signatures must be PNG data URLs submitted with CSRF protection.');
  assert.deepEqual(handoverSignWrites.map(item => item.body.signerType), ['recipient', 'issuer']);
  assert.equal(handoverDocuments[0].isFullySigned, true, 'Both signatures must persist in the archived API record.');
  assert.ok(handoverDocuments[0].recipient.recipientSignatureSha256 && handoverDocuments[0].issuer.issuerSignatureSha256,
    'The API record must retain content hashes for both signatures.');
  await printPopup.close();
  await page.locator('#closeEmployeeHandover').click();
  await handoverDialog.waitFor({ state: 'hidden' });
  await page.locator('#printEmployeeHandoff').click();
  await page.locator('#employeeHandoverHistory').getByRole('button', { name: 'Görüntüle / yazdır' }).waitFor();
  const archivedPrintPromise = page.waitForEvent('popup');
  await page.locator('#employeeHandoverHistory').getByRole('button', { name: 'Görüntüle / yazdır' }).click();
  const archivedPrint = await archivedPrintPromise;
  await archivedPrint.waitForLoadState('domcontentloaded');
  await archivedPrint.locator('.sign img').first().waitFor({ state: 'visible' });
  assert.equal(await archivedPrint.locator('.sign img').count(), 2, 'An archived signed record must reopen with both original signatures.');
  const archivedPdfBytes = await archivedPrint.pdf({ format: 'A4' });
  assert.ok(archivedPdfBytes.subarray(0, 5).equals(Buffer.from('%PDF-')) && archivedPdfBytes.length > 1_000,
    'An archived signed document must remain exportable as a non-empty PDF.');
  await archivedPrint.close();
  await handoverDialog.getByRole('button', { name: 'Kapat' }).click();
  await handoverDialog.waitFor({ state: 'hidden' });

  const documentsPanel = page.locator('#employeeDocumentsPanel');
  await documentsPanel.waitFor({ state: 'visible' });
  const documentCount = page.locator('#employeeDocumentCount');
  assert.equal(await documentCount.innerText(), '0 / 30', 'The empty state must show the company document limit.');
  const documentInput = page.locator('#employeeDocumentForm input[type="file"]');
  await documentInput.setInputFiles({ name: 'invalid.pdf', mimeType: 'application/pdf', buffer: Buffer.from('not a PDF') });
  await page.locator('#employeeDocumentForm button[type="submit"]').click();
  await page.locator('#toast').getByText('Dosya geçerli bir PDF değil.').waitFor();
  assert.equal(await documentCount.innerText(), '0 / 30', 'Rejected content must not appear in the document list.');
  await documentInput.setInputFiles({ name: 'qa-hr-document.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.7\nSynthetic HR attachment\n%%EOF') });
  await page.locator('#employeeDocumentForm button[type="submit"]').click();
  await page.locator('#employeeDocumentList').getByText('qa-hr-document.pdf').waitFor();
  assert.equal(await documentCount.innerText(), '1 / 30');
  const uploadRequests = requests.filter(item => item.method === 'POST' && item.path === `/api/employees/${employeeId}/documents`);
  assert.equal(uploadRequests.length, 2, 'Invalid and valid PDF attempts must each submit once.');
  assert.ok(uploadRequests.every(item => item.csrf === csrfToken && item.contentType?.startsWith('multipart/form-data; boundary=')), 'Document uploads must preserve multipart and CSRF semantics.');
  assert.equal(uploadRequests[0].upload.filename, 'invalid.pdf');
  assert.equal(uploadRequests[1].upload.filename, 'qa-hr-document.pdf');
  assert.ok(uploadRequests[1].upload.bytes.subarray(0, 5).equals(Buffer.from('%PDF-')));

  const downloadPromise = page.waitForEvent('download');
  await page.locator('#employeeDocumentList').getByRole('link', { name: 'İndir' }).click();
  const download = await downloadPromise;
  assert.equal(download.suggestedFilename(), 'qa-hr-document.pdf', 'The file link must download the original safe filename.');
  const downloadRequest = requests.find(item => item.method === 'GET' && item.path === `/api/employees/${employeeId}/documents/66666666-6666-4666-8666-666666666666/file`);
  assert.ok(downloadRequest, 'Download must request the selected employee document.');

  const documentRow = page.locator('#employeeDocumentList .employee-document-row');
  await documentRow.getByRole('button', { name: 'Sil' }).click();
  const deleteConfirm = page.locator('#employeeActionDialog');
  await deleteConfirm.waitFor({ state: 'visible' });
  await deleteConfirm.getByRole('button', { name: 'Vazgeç' }).click();
  await deleteConfirm.waitFor({ state: 'hidden' });
  assert.equal(requests.filter(item => item.method === 'DELETE' && item.path.startsWith(`/api/employees/${employeeId}/documents/`)).length, 0, 'Canceling document deletion must not write.');
  await documentRow.getByRole('button', { name: 'Sil' }).click();
  await deleteConfirm.waitFor({ state: 'visible' });
  await deleteConfirm.locator('#employeeActionSubmit').click();
  await page.locator('#toast').getByText('Çalışan belgesi silindi.').waitFor();
  assert.equal(await documentCount.innerText(), '0 / 30', 'Confirmed deletion must refresh the count and empty state.');
  assert.equal(employeeDocuments.length, 0, 'Confirmed deletion must remove the stored document.');
  assert.equal(requests.filter(item => item.method === 'DELETE' && item.path === `/api/employees/${employeeId}/documents/66666666-6666-4666-8666-666666666666`).length, 1,
    'Confirmed deletion must issue exactly one CSRF-protected request.');
  const deleteRequest = requests.find(item => item.method === 'DELETE' && item.path === `/api/employees/${employeeId}/documents/66666666-6666-4666-8666-666666666666`);
  assert.equal(deleteRequest.csrf, csrfToken);

  await page.locator('#closeEmployeeDetails').click();
  await page.locator('#employeeImportOpen').click();
  const employeeImportDialog = page.locator('#employeeImportDialog');
  await employeeImportDialog.waitFor({ state: 'visible' });
  const templateDownloadPromise = page.waitForEvent('download');
  await page.locator('#downloadEmployeeImportTemplate').click();
  const templateDownload = await templateDownloadPromise;
  assert.equal(templateDownload.suggestedFilename(), 'envantertr-calisan-ice-aktarma-sablonu.csv');
  const templatePath = await templateDownload.path();
  const templateText = await readFile(templatePath, 'utf8');
  assert.match(templateText, /Personel No.*Ad Soyad.*E-posta.*İşe Başlama Tarihi/,
    'The downloadable template must contain the documented employee columns.');
  const importCsv = 'Personel No,Ad Soyad,E-posta,Departman,Görev Unvanı,Telefon,Konum,İşe Başlama Tarihi\nQA-IMPORT-001,QA Imported Employee,qa.imported@example.invalid,IT,Analyst,+90 555 222 33 44,Istanbul Office,2025-01-02\n';
  await page.locator('#employeeImportFile').setInputFiles({ name: 'qa-employees.csv', mimeType: 'text/csv', buffer: Buffer.from(importCsv) });
  await page.locator('#previewEmployeeImport').click();
  await page.locator('#employeeImportFeedback').getByText('1 yeni çalışan kaydı doğrulandı. Önizleme hiçbir kayıt eklemedi.').waitFor();
  assert.equal(await page.locator('#commitEmployeeImport').isEnabled(), true, 'A valid preview must enable the explicit commit action.');
  const previewRequest = requests.find(item => item.method === 'POST' && item.path === '/api/employees/import/preview.csv');
  assert.ok(previewRequest && previewRequest.csrf === csrfToken && previewRequest.contentType?.startsWith('text/csv'));
  assert.equal(previewRequest.body, importCsv);
  assert.equal(employees.some(item => item.employeeNumber === 'QA-IMPORT-001'), false, 'Preview must not create employee records.');
  assert.equal(requests.filter(item => item.method === 'POST' && item.path === '/api/employees/import.csv').length, 0,
    'Preview must never call the commit endpoint.');
  await page.locator('#commitEmployeeImport').click();
  const importConfirm = page.locator('#employeeActionDialog');
  await importConfirm.waitFor({ state: 'visible' });
  await importConfirm.getByRole('button', { name: 'Vazgeç' }).click();
  await importConfirm.waitFor({ state: 'hidden' });
  assert.equal(requests.filter(item => item.method === 'POST' && item.path === '/api/employees/import.csv').length, 0,
    'Canceling the import confirmation must not write to the API.');
  await page.locator('#commitEmployeeImport').click();
  await importConfirm.waitFor({ state: 'visible' });
  await importConfirm.locator('#employeeActionSubmit').click();
  await page.locator('#toast').getByText('1 çalışan kaydı eklendi.').waitFor();
  await page.locator('#employeeRows').getByText('QA Imported Employee').waitFor();
  const importRequest = requests.find(item => item.method === 'POST' && item.path === '/api/employees/import.csv');
  assert.ok(importRequest && importRequest.csrf === csrfToken && importRequest.contentType?.startsWith('text/csv'));
  assert.equal(importRequest.body, importCsv, 'The confirmed import must send the exact previewed file contents.');
  assert.equal(employees.filter(item => item.employeeNumber === 'QA-IMPORT-001').length, 1,
    'Confirmed import must add the previewed employee once.');
  await employeeImportDialog.waitFor({ state: 'hidden' });

  listResponse = employeeListResponse(params => params.get('q') === 'QA Imported Employee');
  await page.locator('#employeeSearch').fill('QA Imported Employee');
  await listResponse;
  listResponse = employeeListResponse(params => params.get('q') === 'QA Imported Employee' && params.get('department') === 'IT');
  await page.locator('#employeeDepartmentFilter').fill('IT');
  await listResponse;
  listResponse = employeeListResponse(params => params.get('q') === 'QA Imported Employee' && params.get('department') === 'IT' && params.get('startedFrom') === '2025-01-01');
  await page.locator('#employeeStartedFrom').fill('2025-01-01');
  await listResponse;
  listResponse = employeeListResponse(params => params.get('q') === 'QA Imported Employee' && params.get('department') === 'IT' && params.get('startedFrom') === '2025-01-01' && params.get('startedTo') === '2025-12-31');
  await page.locator('#employeeStartedTo').fill('2025-12-31');
  await listResponse;
  assert.equal(await page.locator('#employeeRows tr').count(), 1, 'The active company employee filters must isolate the expected export row.');
  const exportResponsePromise = page.waitForResponse(response => new URL(response.url()).pathname === '/api/employees/export.csv');
  const exportDownloadPromise = page.waitForEvent('download');
  await page.locator('#exportEmployeesButton').click();
  const [exportResponse, exportDownload] = await Promise.all([exportResponsePromise, exportDownloadPromise]);
  assert.equal(exportResponse.status(), 200, 'Employee export must return a downloadable CSV response.');
  assert.equal(exportDownload.suggestedFilename(), 'envantertr-calisanlar-' + new Date().toISOString().slice(0, 10) + '.csv');
  const exportedCsv = await readFile(await exportDownload.path(), 'utf8');
  assert.ok(exportedCsv.startsWith('\uFEFF'), 'The exported CSV must include a UTF-8 BOM for spreadsheet applications.');
  assert.match(exportedCsv, /"Personel No","Ad Soyad".*"Yönetici Personel No"/s,
    'The employee export must use the complete re-importable round-trip header.');
  assert.match(exportedCsv, /QA-IMPORT-001.*QA Imported Employee/s);
  const exportRequest = requests.findLast(item => item.method === 'GET' && item.path === '/api/employees/export.csv');
  assert.deepEqual(exportRequest?.query, { language: 'tr', q: 'QA Imported Employee', department: 'IT', active: 'true', startedFrom: '2025-01-01', startedTo: '2025-12-31' },
    'CSV export must preserve the current search, department, status, and date filters.');

  await page.locator('#employeeImportOpen').click();
  await employeeImportDialog.waitFor({ state: 'visible' });
  await page.locator('#employeeImportFile').setInputFiles({ name: exportDownload.suggestedFilename(), mimeType: 'text/csv', buffer: Buffer.from(exportedCsv, 'utf8') });
  await page.locator('#previewEmployeeImport').click();
  await page.locator('#employeeImportFeedback').getByText('Hiçbir çalışan eklenmedi.').waitFor();
  assert.equal(await page.locator('#commitEmployeeImport').isEnabled(), false,
    'Re-importing the same company export must be recognized and duplicate employee numbers must block commit.');
  assert.equal(employees.filter(item => item.employeeNumber === 'QA-IMPORT-001').length, 1,
    'Previewing a same-company export must not create or mutate employees.');
  assert.equal(requests.filter(item => item.method === 'POST' && item.path === '/api/employees/import.csv').length, 1,
    'A duplicate export preview must never call the import commit endpoint.');
  const exportedFilePreview = requests.findLast(item => item.method === 'POST' && item.path === '/api/employees/import/preview.csv');
  assert.equal(exportedFilePreview?.body?.replace(/^\uFEFF/, ''), exportedCsv.replace(/^\uFEFF/, ''),
    'The CSV export must be accepted as input by the CSV import parser (the browser may strip the optional BOM when reading the file).');
  await page.locator('#closeEmployeeImport').click();
  await employeeImportDialog.waitFor({ state: 'hidden' });

  await page.locator('#sidebar [data-page="Departmanlar"]').click({ force: true });
  await page.locator('#directoryPage').waitFor({ state: 'visible' });
  await page.locator('#directoryRows').getByRole('button', { name: 'QA Main Department', exact: true }).waitFor();
  const createDepartmentWrites = () => requests.filter(item => item.method === 'POST' && item.path === '/api/departments');
  const writesBeforeCancel = createDepartmentWrites().length;
  await page.locator('#addDepartmentButton').click();
  await page.locator('#createDepartmentDialog').waitFor({ state: 'visible' });
  await page.locator('#createDepartmentForm [name="name"]').fill('QA Canceled Department');
  await page.locator('#cancelCreateDepartment').click();
  await page.locator('#createDepartmentDialog').waitFor({ state: 'hidden' });
  assert.equal(createDepartmentWrites().length, writesBeforeCancel, 'Canceling department creation must make no API write.');

  await page.locator('#addDepartmentButton').click();
  await page.locator('#createDepartmentDialog').waitFor({ state: 'visible' });
  await page.locator('#createDepartmentForm [name="name"]').fill('QA New Child');
  await page.locator('#createDepartmentForm [name="parentDepartmentId"]').selectOption(departmentOneId);
  await page.locator('#createDepartmentForm [name="locationCatalogId"]').selectOption(floorOneId);
  const createDepartmentResponse = page.waitForResponse(response => response.request().method() === 'POST' && new URL(response.url()).pathname === '/api/departments');
  await page.locator('#saveDepartment').click();
  const createdDepartmentResponse = await createDepartmentResponse;
  assert.equal(createdDepartmentResponse.status(), 201);
  const createDepartmentRequest = requests.findLast(item => item.method === 'POST' && item.path === '/api/departments');
  assert.equal(createDepartmentRequest?.csrf, csrfToken, 'Creating a department must include CSRF protection.');
  assert.deepEqual(createDepartmentRequest?.body, { name: 'QA New Child', parentDepartmentId: departmentOneId, locationCatalogId: floorOneId });
  await page.locator('#createDepartmentDialog').waitFor({ state: 'hidden' });
  const childRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department / QA New Child' });
  await childRow.waitFor();
  assert.match(await childRow.innerText(), /Istanbul Office \/ Floor 1/, 'Department directory must show the selected floor alongside the complete hierarchy path.');

  await childRow.getByRole('button', { name: 'Adı düzenle' }).click();
  await page.locator('#directoryRenameDialog').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#directoryRenameForm [name="parentDepartmentId"]').inputValue(), departmentOneId,
    'Editing a department must preselect its current parent.');
  assert.equal(await page.locator('#directoryRenameForm [name="locationCatalogId"]').inputValue(), floorOneId,
    'Editing a department must preselect its current floor.');
  await page.locator('#directoryRenameForm [name="newName"]').fill('QA Moved Child');
  await page.locator('#directoryRenameForm [name="parentDepartmentId"]').selectOption(departmentTwoId);
  await page.locator('#directoryRenameForm [name="locationCatalogId"]').selectOption(floorTwoId);
  const updateDepartmentResponse = page.waitForResponse(response => response.request().method() === 'PUT' && new URL(response.url()).pathname.startsWith('/api/departments/'));
  await page.locator('#saveDirectoryRename').click();
  const updatedDepartmentResponse = await updateDepartmentResponse;
  assert.equal(updatedDepartmentResponse.status(), 200);
  const updateDepartmentRequest = requests.findLast(item => item.method === 'PUT' && item.path.startsWith('/api/departments/'));
  assert.equal(updateDepartmentRequest?.csrf, csrfToken);
  assert.deepEqual(updateDepartmentRequest?.body, { name: 'QA Moved Child', parentDepartmentId: departmentTwoId, locationCatalogId: floorTwoId });
  await page.locator('#directoryRenameDialog').waitFor({ state: 'hidden' });
  const movedRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Second Department / QA Moved Child' });
  await movedRow.waitFor();
  assert.match(await movedRow.innerText(), /Istanbul Office \/ Floor 2/);

  listResponse = employeeListResponse(params => params.get('department') === 'QA Moved Child');
  await movedRow.getByRole('button', { name: 'QA Second Department / QA Moved Child' }).click();
  await listResponse;
  assert.equal(await page.locator('#employeeDepartmentFilter').inputValue(), 'QA Moved Child',
    'Selecting the full department path must open the employee list filtered to that department.');

  await page.locator('#sidebar [data-group="places"]').click();
  await page.locator('#sidebar [data-page="Lokasyonlar"]').click();
  const floorLocationRow = page.locator('#directoryRows tr').filter({ hasText: 'Istanbul Office / Floor 2' });
  await floorLocationRow.waitFor();
  assert.match(await floorLocationRow.innerText(), /Departmanlar 2/, 'The selected floor should show both catalog departments.');
  const floorDepartmentResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return response.request().method() === 'GET' && url.pathname === '/api/departments';
  }).catch(error => error);
  const floorShortcut = floorLocationRow.getByRole('button', { name: /^Departmanlar/ });
  assert.equal(await floorShortcut.count(), 1, 'The selected floor should expose one department shortcut.');
  await floorShortcut.click();
  await page.getByRole('heading', { name: 'Departmanlar · Istanbul Office / Floor 2' }).waitFor();
  const floorDepartmentResult = await floorDepartmentResponse;
  if (floorDepartmentResult instanceof Error) throw new Error(`Floor shortcut sent no department request; requests=${JSON.stringify(requests.filter(item => item.path === '/api/departments'))}`);
  assert.equal(new URL(floorDepartmentResult.url()).searchParams.get('locationId'), floorTwoId,
    'The floor shortcut must request departments for the selected floor.');
  const filteredDepartmentRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Second Department / QA Moved Child' });
  await filteredDepartmentRow.waitFor();
  const floorRefreshResponse = page.waitForResponse(response => {
    const url = new URL(response.url());
    return response.request().method() === 'GET' && url.pathname === '/api/departments';
  }).catch(error => error);
  await page.locator('#directoryRefresh').click();
  const floorRefreshResult = await floorRefreshResponse;
  if (floorRefreshResult instanceof Error) throw new Error(`Filtered refresh sent no department request; requests=${JSON.stringify(requests.filter(item => item.path === '/api/departments'))}`);
  assert.equal(new URL(floorRefreshResult.url()).searchParams.get('locationId'), floorTwoId,
    'Refreshing a floor-filtered department list must preserve its floor filter.');
  assert.ok((await filteredDepartmentRow.innerText()).includes('QA Moved Child'), 'Refreshing a floor-filtered department list must preserve its floor filter.');

  await page.locator('#sidebar [data-group="people"]').evaluate(element => element.click());
  await page.locator('#sidebar [data-page="Departmanlar"]').evaluate(element => element.click());
  let sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department' });
  await sourceRow.waitFor();
  assert.match(await sourceRow.innerText(), /Atanmamış/, 'Department directory must show unassigned manager status.');
  await sourceRow.getByRole('button', { name: 'Yetkili ata' }).click();
  await page.locator('#departmentManagerDialog').waitFor({ state: 'visible' });
  await page.locator('#departmentManagerEmployee').selectOption(managerId);
  const managerResponse = page.waitForResponse(response => response.request().method() === 'PATCH' && new URL(response.url()).pathname === `/api/departments/${departmentOneId}/manager`);
  await page.locator('#saveDepartmentManager').click();
  const savedManagerResponse = await managerResponse;
  assert.equal(savedManagerResponse.status(), 200, 'Saving a department manager must succeed.');
  const managerWrite = requests.findLast(item => item.method === 'PATCH' && item.path === `/api/departments/${departmentOneId}/manager`);
  assert.equal(managerWrite?.csrf, csrfToken, 'Department manager assignment must send CSRF protection.');
  assert.deepEqual(managerWrite?.body, { employeeId: managerId });
  await page.locator('#departmentManagerDialog').waitFor({ state: 'hidden' });
  sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department' });
  await sourceRow.getByRole('button', { name: 'Yetkili ata' }).waitFor();
  assert.match(await sourceRow.innerText(), /QA Supervisor/, 'Saved manager must appear in the refreshed directory row.');
  await sourceRow.getByRole('button', { name: 'Yetkili ata' }).click();
  await page.locator('#departmentManagerEmployee').selectOption('');
  const clearManagerResponse = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/departments/${departmentOneId}/manager`);
  await page.locator('#saveDepartmentManager').click();
  assert.equal((await clearManagerResponse).status(), 200, 'Clearing a department manager must complete its API update before the test checks the request.');
  await page.locator('#departmentManagerDialog').waitFor({ state: 'hidden' });
  const clearManagerWrite = requests.findLast(item => item.method === 'PATCH' && item.path === `/api/departments/${departmentOneId}/manager`);
  assert.deepEqual(clearManagerWrite?.body, { employeeId: null }, 'Clearing a department manager must send null explicitly.');
  sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department' });
  await page.waitForFunction(() => [...document.querySelectorAll('#directoryRows tr')]
    .find(row => row.textContent.includes('QA Main Department'))?.textContent.includes('Atanmamış'));
  assert.match(await sourceRow.innerText(), /Atanmamış/, 'Clearing a department manager must refresh the directory row.');
  const archiveWriteCount = () => requests.filter(item => item.method === 'PATCH' && item.path === `/api/departments/${departmentOneId}/active`).length;
  const archiveWritesBeforeCancel = archiveWriteCount();
  await sourceRow.getByRole('button', { name: 'Arşivle' }).click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
  await page.locator('#employeeActionDialog [data-close-employee-action]').first().click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'hidden' });
  assert.equal(archiveWriteCount(), archiveWritesBeforeCancel, 'Canceling department archive must make no API write.');
  sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department' });
  await sourceRow.getByRole('button', { name: 'Arşivle' }).click();
  await page.locator('#employeeActionDialog').waitFor({ state: 'visible' });
  const archiveResponse = page.waitForResponse(response => response.request().method() === 'PATCH' && new URL(response.url()).pathname === `/api/departments/${departmentOneId}/active`);
  await page.locator('#employeeActionSubmit').click();
  const archivedDepartmentResponse = await archiveResponse;
  assert.equal(archivedDepartmentResponse.status(), 200);
  assert.equal(requests.findLast(item => item.method === 'PATCH' && item.path === `/api/departments/${departmentOneId}/active`)?.csrf, csrfToken,
    'Archiving a department must send CSRF protection.');
  sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department · Arşivlendi' });
  await sourceRow.waitFor();
  assert.match(await sourceRow.innerText(), /205/, 'Archiving must preserve employees in the source department.');
  assert.equal(await sourceRow.getByRole('button', { name: /kademeli taşı/ }).count(), 1,
    `Archived departments with existing employees must keep the staged migration action. Row: ${await sourceRow.innerText()}`);
  const suggestionNames = await page.locator('#departmentSuggestions option').evaluateAll(options => options.map(option => option.value));
  assert.ok(!suggestionNames.includes('QA Main Department'), 'Archived departments must not be suggested for new employees.');

  await sourceRow.getByRole('button', { name: /kademeli taşı/ }).click();
  await page.locator('#departmentMoveDialog').waitFor({ state: 'visible' });
  await page.locator('#departmentMoveForm [name="targetDepartmentId"]').selectOption(departmentTwoId);
  for (const [expectedMoved, expectedRemaining] of [[100, 105], [100, 5], [5, 0]]) {
    const moveResponse = page.waitForResponse(response => response.request().method() === 'POST' && new URL(response.url()).pathname === `/api/departments/${departmentOneId}/move-employees`);
    await page.locator('#moveDepartmentEmployees').click();
    const movedResponse = await moveResponse;
    assert.equal(movedResponse.status(), 200);
    const lastMove = requests.findLast(item => item.method === 'POST' && item.path === `/api/departments/${departmentOneId}/move-employees`);
    assert.equal(lastMove?.csrf, csrfToken, 'Moving a department batch must send CSRF protection.');
    assert.deepEqual(lastMove?.body, { targetDepartmentId: departmentTwoId, batchSize: 100 });
    const progressText = await page.locator('#departmentMoveProgress').innerText();
    assert.ok(progressText.includes(`${expectedMoved} çalışan taşındı`), `Expected batch size ${expectedMoved}, got: ${progressText}`);
    assert.ok(progressText.includes(`${expectedRemaining} çalışan`), `Expected ${expectedRemaining} remaining employees, got: ${progressText}`);
  }
  await page.locator('#moveDepartmentEmployees').waitFor({ state: 'hidden' });
  assert.equal(await page.locator('#moveDepartmentEmployees').isVisible(), false, 'Completed employee migration must disable further writes.');
  await page.locator('#cancelDepartmentMove').click();
  await page.locator('#departmentMoveDialog').waitFor({ state: 'hidden' });
  sourceRow = page.locator('#directoryRows tr').filter({ hasText: 'QA Main Department · Arşivlendi' });
  await sourceRow.waitFor();
  assert.match(await sourceRow.innerText(), /0\s+0\s+0/, 'Completed migration must refresh source employee counts to zero.');
  const restoreResponse = page.waitForResponse(response => response.request().method() === 'PATCH' && new URL(response.url()).pathname === `/api/departments/${departmentOneId}/active`);
  await sourceRow.getByRole('button', { name: 'Geri al' }).click();
  await restoreResponse;
  await page.locator('#directoryRows tr').filter({ hasText: /^QA Main Department/ }).getByRole('button', { name: 'Arşivle' }).waitFor();

  await page.locator('#sidebar [data-group="people"]').click();
  await page.locator('#sidebar [data-page="Uygulama kullanıcıları"]').click();
  await page.locator('#membersPage').waitFor({ state: 'visible' });
  const memberRow = page.locator('#membersRows tr').filter({ hasText: 'QA Linked Member' });
  await memberRow.waitFor();
  const employeeLinkSelect = memberRow.locator('select[aria-label="QA Linked Member çalışan profili"]');
  const employeeLinkSave = memberRow.getByRole('button', { name: 'Bağlantıyı kaydet' });
  assert.equal(await employeeLinkSave.isDisabled(), true, 'An unchanged employee link must not send an unnecessary write.');
  const memberEmployeeSearch = memberRow.getByRole('searchbox', { name: 'QA Linked Member için çalışan ara' });
  const slowSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return response.request().method() === 'GET' && url.pathname === '/api/requests/employees' && url.searchParams.get('q') === 'QA';
  });
  await memberEmployeeSearch.fill('QA');
  await slowSearchResponse;
  const latestSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return response.request().method() === 'GET' && url.pathname === '/api/requests/employees' && url.searchParams.get('q') === 'QA Supervisor II';
  });
  await memberEmployeeSearch.fill('QA Supervisor II');
  await latestSearchResponse;
  assert.equal(await employeeLinkSelect.locator('option').filter({ hasText: 'QA Supervisor II' }).count(), 1,
    'The latest employee-search result must appear in the member-link selector.');
  await page.waitForTimeout(600);
  assert.equal(await employeeLinkSelect.locator(`option[value="${managerId}"]`).count(), 0,
    'A stale, slower employee-search response must not overwrite the latest search results.');

  const managerSearchResponse = page.waitForResponse(response => {
    const url = new URL(response.url()); return response.request().method() === 'GET' && url.pathname === '/api/requests/employees' && url.searchParams.get('q') === 'QA Supervisor';
  });
  await memberEmployeeSearch.fill('QA Supervisor');
  await managerSearchResponse;
  await employeeLinkSelect.selectOption(managerId);
  assert.equal(await employeeLinkSave.isEnabled(), true, 'Changing the member link must enable the save action.');
  const linkResponse = page.waitForResponse(response => response.request().method() === 'PATCH' && new URL(response.url()).pathname === `/api/members/${memberUserId}/employee`);
  await employeeLinkSave.click();
  const linkedResponse = await linkResponse;
  assert.equal(linkedResponse.status(), 200, 'Linking a company member to an employee must succeed.');
  const linkWrite = requests.findLast(item => item.method === 'PATCH' && item.path === `/api/members/${memberUserId}/employee`);
  assert.equal(linkWrite?.csrf, csrfToken, 'Member-to-employee link must send CSRF protection.');
  assert.deepEqual(linkWrite?.body, { employeeId: managerId });
  await memberRow.waitFor();
  await memberRow.locator(`select[aria-label="QA Linked Member çalışan profili"]`).waitFor();
  assert.equal(await memberRow.locator(`select[aria-label="QA Linked Member çalışan profili"]`).inputValue(), managerId,
    'Refreshing the member list must preserve the linked employee choice.');
  assert.match(await memberRow.innerText(), /QA Supervisor/, 'The member row must display the linked employee name after refresh.');

  const refreshedLinkSelect = memberRow.locator('select[aria-label="QA Linked Member çalışan profili"]');
  await refreshedLinkSelect.selectOption('');
  const unlinkResponse = page.waitForResponse(response => response.request().method() === 'PATCH' && new URL(response.url()).pathname === `/api/members/${memberUserId}/employee`);
  await memberRow.getByRole('button', { name: 'Bağlantıyı kaydet' }).click();
  const unlinkedResponse = await unlinkResponse;
  assert.equal(unlinkedResponse.status(), 200, 'Removing the member-to-employee link must succeed.');
  const unlinkWrite = requests.filter(item => item.method === 'PATCH' && item.path === `/api/members/${memberUserId}/employee`).at(-1);
  assert.equal(unlinkWrite?.csrf, csrfToken);
  assert.deepEqual(unlinkWrite?.body, { employeeId: null });
  await memberRow.waitFor();
  assert.equal(await memberRow.locator('select[aria-label="QA Linked Member çalışan profili"]').inputValue(), '',
    'Unlinking must refresh the company member row to the unlinked state.');

  const locationAccessButton = memberRow.getByRole('button', { name: 'Şirket birimi erişimi' });
  await locationAccessButton.click();
  await page.locator('#memberLocationAccessList [data-location-option]').first().waitFor();
  assert.equal(await page.locator('#memberLocationAccessMode').inputValue(), 'all', 'An existing member keeps unrestricted access unless changed by an administrator.');
  await page.locator('#memberLocationAccessMode').selectOption('restricted');
  const scopedLocation = page.locator(`#memberLocationAccessList [data-location-option][value="${floorOneId}"]`);
  assert.equal(await scopedLocation.isEnabled(), true, 'Location choices must be enabled in restricted mode.');
  await scopedLocation.check();
  const scopeWriteResponse = page.waitForResponse(response => response.request().method() === 'PUT' &&
    new URL(response.url()).pathname === `/api/members/${memberUserId}/location-access`);
  await page.locator('#memberLocationAccessForm').getByRole('button', { name: 'Erişimi kaydet' }).click();
  const scopeWrite = await scopeWriteResponse;
  assert.equal(scopeWrite.status(), 200, 'Saving a location scope must succeed.');
  const scopeWriteRequest = requests.findLast(item => item.method === 'PUT' && item.path === `/api/members/${memberUserId}/location-access`);
  assert.equal(scopeWriteRequest?.csrf, csrfToken, 'Location scope updates must send CSRF protection.');
  assert.deepEqual(scopeWriteRequest?.body, { mode: 'restricted', locationIds: [floorOneId], companyUnitMode: 'all', companyUnitIds: [] });
  await page.locator('#membersRows tr').filter({ hasText: 'QA Linked Member' }).getByRole('button', { name: 'Birim erişimini düzenle' }).waitFor();
  await page.locator('#membersRows tr').filter({ hasText: 'QA Linked Member' }).getByRole('button', { name: 'Birim erişimini düzenle' }).click();
  await page.locator('#memberLocationAccessList [data-location-option]').first().waitFor();
  assert.equal(await page.locator('#memberLocationAccessMode').inputValue(), 'restricted');
  assert.equal(await page.locator('#memberCompanyUnitAccessMode').inputValue(), 'all');
  assert.equal(await scopedLocation.isChecked(), true, 'Selected access scope must load back from the API.');
  await page.locator('#memberLocationAccessMode').selectOption('all');
  const restoreAllResponse = page.waitForResponse(response => response.request().method() === 'PUT' &&
    new URL(response.url()).pathname === `/api/members/${memberUserId}/location-access`);
  await page.locator('#memberLocationAccessForm').getByRole('button', { name: 'Erişimi kaydet' }).click();
  assert.equal((await restoreAllResponse).status(), 200, 'Restoring access to all locations must succeed.');
  const restoreAllWrite = requests.filter(item => item.method === 'PUT' && item.path === `/api/members/${memberUserId}/location-access`).at(-1);
  assert.deepEqual(restoreAllWrite?.body, { mode: 'all', locationIds: [], companyUnitMode: 'all', companyUnitIds: [] });

  syntheticRole = 'asset_operator';
  // This limits private HR notes to authorized roles.
  const privateNotesReadsBeforeLimitedPage = requests.filter(item => item.method === 'GET' && item.path === `/api/employees/${employeeId}/private-notes`).length;
  const documentReadsBeforeLimitedPage = requests.filter(item => item.method === 'GET' && item.path === `/api/employees/${employeeId}/documents`).length;
  const limitedPage = await context.newPage();
  limitedPage.on('pageerror', error => pageErrors.push(error.message));
  await limitedPage.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: 'domcontentloaded' });
  await limitedPage.locator('#authLayer').waitFor({ state: 'hidden' });
  await limitedPage.locator('#sidebar [data-group="people"]').click();
  await limitedPage.locator('#sidebar [data-page="Çalışan kayıtları"]').click();
  await limitedPage.locator('#employeesPage').waitFor({ state: 'visible' });
  await limitedPage.locator('#employeeRows tr').filter({ hasText: 'QA Employee Updated' }).getByRole('button', { name: 'QA Employee Updated, çalışan profilini aç' }).click();
  await limitedPage.locator('#employeeDialog').waitFor({ state: 'visible' });
  assert.equal(await limitedPage.locator('#employeePhotoPanel .btn.primary').count(), 0, 'Employees without people.manage must not see the upload action.');
  assert.equal(await limitedPage.locator('#removeEmployeePhoto').count(), 0, 'Employees without people.manage must not see photo removal.');
  assert.equal(await limitedPage.locator('#employeePrivateNotesPanel').isVisible(), false, 'A non-HR asset operator must not see private employee notes.');
  assert.equal(requests.filter(item => item.method === 'GET' && item.path === `/api/employees/${employeeId}/private-notes`).length, privateNotesReadsBeforeLimitedPage,
    'A non-HR asset operator must not request private notes.');
  assert.equal(await limitedPage.locator('#employeeDocumentsPanel').isVisible(), false, 'A role without employee document access must not see the documents panel.');
  assert.equal(requests.filter(item => item.method === 'GET' && item.path === `/api/employees/${employeeId}/documents`).length, documentReadsBeforeLimitedPage,
    'A role without employee document access must not request employee documents.');
  assert.equal(requests.filter(item => item.method === 'PUT' && item.path.endsWith('/private-notes')).length, 1, 'Private notes must only have one explicit save write.');
  await limitedPage.close();
  assert.deepEqual(pageErrors, [], `Employee list and editor must not throw browser errors: ${pageErrors.join(' | ')}`);
console.log('PASS: Chromium verifies employee CSV export/import, company member-to-employee link/unlink with CSRF, stale-search protection and refresh, and the department catalog: cancel-safe creation/archive, department manager assign/clear with CSRF and refreshed status, parent/floor create/edit, full-path/floor rendering, employee shortcut, 100-record staged migration with residual counts, archived department suggestions, restore, floor shortcut and refresh preservation, alongside employee profile/import/export/offboarding flows (synthetic API).');
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
