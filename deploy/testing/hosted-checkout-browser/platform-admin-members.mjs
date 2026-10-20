import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const platformHtml = await readFile('/platform-site/platform-admin.html');
const platformScripts = new Map([
  ['/platform-admin-members.js', await readFile('/platform-site/platform-admin-members.js')],
  ['/platform-admin-subscriptions.js', await readFile('/platform-site/platform-admin-subscriptions.js')],
  ['/platform-admin-demo-requests.js', await readFile('/platform-site/platform-admin-demo-requests.js')],
  ['/platform-admin-payment-follow-ups.js', await readFile('/platform-site/platform-admin-payment-follow-ups.js')]
]);
const csrfToken = 'synthetic-platform-members-csrf';
const accountId = '11111111-1111-4111-8111-111111111111';
const invitedAccountId = '22222222-2222-4222-8222-222222222222';
const companyId = '33333333-3333-4333-8333-333333333333';
const secondCompanyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab';
const demoLeadId = '66666666-6666-4666-8666-666666666666';
const platformAdminId = '77777777-7777-4777-8777-777777777777';
const failedOrderId = '88888888-8888-4888-8888-888888888888';
const member = {
  id: accountId, displayName: 'Synthetic SaaS Owner', email: 'qa-owner@example.invalid', emailConfirmed: true,
  phoneNumber: '+905550000001', phoneNumberConfirmed: true, twoFactorEnabled: true, isLockedOut: false,
  accessFailedCount: 0, accountCreatedAt: '2026-09-01T09:30:00Z', lastSeenAt: '2026-10-15T08:00:00Z',
  firstMembershipAt: '2026-09-02T10:00:00Z', membershipCount: 2,
  memberships: [
    { id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo', roleKey: 'admin', isActive: true },
    { id: secondCompanyId, name: 'Synthetic Second Company', slug: 'synthetic-second', roleKey: 'viewer', isActive: true }
  ]
};
const membership = {
  id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo', isActive: true, roleKey: 'admin',
  createdAt: '2026-09-02T10:00:00Z', subscription: {
    name: 'Cloud Pro', code: 'cloud-pro', status: 'Active', isDemo: false,
    startsAt: '2026-09-02T10:00:00Z', endsAt: '2027-09-02T10:00:00Z', paymentReference: 'synthetic-provider-reference'
  }
};
const secondMembership = {
  id: secondCompanyId, name: 'Synthetic Second Company', slug: 'synthetic-second', isActive: true, roleKey: 'viewer',
  createdAt: '2026-09-05T10:00:00Z', subscription: {
    name: 'Cloud Starter', code: 'cloud-starter', status: 'Trial', isDemo: true,
    startsAt: '2026-09-05T10:00:00Z', endsAt: '2026-10-05T10:00:00Z', paymentReference: null
  }
};
const subscriptionId = '55555555-5555-4555-8555-555555555555';
const directoryCompany = {
  id: companyId, name: 'Synthetic Demo Company', slug: 'synthetic-demo', contactEmail: 'demo@example.invalid', isActive: true,
  createdAt: '2026-09-01T09:30:00Z', hasBrandLogo: true, brandPrimaryColor: '#1769df',
  lifecycleStage: 'AtRisk', lifecycleOwnerName: 'Synthetic Platform Admin', nextFollowUpAt: '2026-10-10T10:00:00Z',
  employeeRecords: 12, loginAccounts: 4, assetRecords: 28,
  subscription: { id: subscriptionId, planName: 'Cloud Pro', status: 'Active', isDemo: false,
    startsAt: '2026-09-02T10:00:00Z', endsAt: '2027-09-02T10:00:00Z' },
  verifiedMonthlyRecurringRevenue: 100, verifiedMrrCurrency: 'TRY'
};
const companies = [directoryCompany];
const accounts = [member];
const planId = '44444444-4444-4444-8444-444444444444';
const alternatePlanId = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';
const alternatePlan = { id: alternatePlanId, code: 'synthetic-alternate', name: 'Synthetic Alternate Plan', deploymentKind: 'cloud', isActive: true };
const plans = [];
let includeAlternatePlanOption = false;
const writes = [];
const auditRequests = [];
const companyExports = [];
const companyListRequests = [];
let auditErrorInjected = false;
let systemHealthReads = 0;
const auditEntries = Array.from({ length: 51 }, (_, index) => ({
  id: `99999999-9999-4999-8999-${String(index + 1).padStart(12, '0')}`,
  source: index === 0 ? 'platform' : 'company', actorUserId: platformAdminId,
  actorName: index === 0 ? 'Synthetic Platform Admin' : 'Synthetic Company Actor',
  actorEmail: index === 0 ? 'operator@example.invalid' : 'qa-owner@example.invalid',
  companyId, companyName: 'Synthetic Demo Company', action: index === 0 ? 'PlanUpdated' : 'AssetUpdated',
  targetType: index === 0 ? 'Plan' : 'Asset', targetId: `synthetic-target-${index + 1}`,
  details: JSON.stringify({ note: index === 0 ? 'Unique audit filter marker' : `Synthetic audit row ${index + 1}` }),
  occurredAt: new Date(Date.UTC(2026, 9, 15, 10, 0, index)).toISOString()
}));
const companySubscription = {
  id: subscriptionId, planId, name: 'Cloud Pro', code: 'cloud-pro', status: 'Active', isDemo: false,
  startsAt: '2026-09-02T10:00:00Z', endsAt: '2027-09-02T10:00:00Z', paymentReference: 'synthetic-provider-reference'
};
const demoLead = {
  id: demoLeadId, companyName: 'Synthetic Demo Lead', contactName: 'Synthetic Contact', email: 'lead@example.invalid',
  status: 'Qualified', source: 'Synthetic acceptance', createdAt: '2026-10-08T10:00:00Z', consentAt: '2026-10-08T10:00:00Z',
  consentNoticeVersion: 'synthetic-v1', followUpAt: '2026-10-11T10:00:00Z', assignedAdminUserId: null, companyId: null,
  phone: '+905550000002', message: 'Synthetic request for a product demo.', teamSize: '25', assetCount: '120', jobTitle: 'IT Manager'
};
const newerDemoLeadId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const newerDemoLead = {
  ...demoLead, id: newerDemoLeadId, companyName: 'Synthetic Newer Demo', contactName: 'Synthetic Newer Contact',
  email: 'newer-lead@example.invalid', status: 'New', followUpAt: null, companyId
};
const slowCompanyId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const fastCompanyId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const demoCompanyOptions = [
  directoryCompany,
  { id: slowCompanyId, name: 'Slow Synthetic Company', slug: 'slow-synthetic', isActive: true },
  { id: fastCompanyId, name: 'Fast Synthetic Company', slug: 'fast-synthetic', isActive: true }
];
const demoLeadHistory = [];
const newerDemoLeadHistory = [];
let delayOlderDemoDetail = false;
let delayOlderDemoCompanyFallback = false;
const paymentFollowUpHistory = [];
const browserErrors = [];
let reauthenticated = false;
let browser;

const json = (response, body, status = 200) => {
  response.writeHead(status, { 'content-type': 'application/json; charset=utf-8', 'cache-control': 'no-store' });
  response.end(JSON.stringify(body));
};
const server = createServer(async (request, response) => {
  const url = new URL(request.url || '/', 'http://127.0.0.1');
  if (request.method === 'GET' && url.pathname === '/platform-admin.html') {
    response.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
    response.end(platformHtml); return;
  }
  if (request.method === 'GET' && platformScripts.has(url.pathname)) {
    response.writeHead(200, { 'content-type': 'text/javascript; charset=utf-8' });
    response.end(platformScripts.get(url.pathname)); return;
  }
  if (request.method === 'GET' && url.pathname === '/assets/fonts/inter.css') {
    response.writeHead(200, { 'content-type': 'text/css' }); response.end(''); return;
  }
  if (request.method === 'GET' && url.pathname.startsWith('/assets/fonts/')) {
    response.writeHead(200, { 'content-type': 'font/woff2' }); response.end(Buffer.alloc(0)); return;
  }
  if (!url.pathname.startsWith('/api/')) { response.writeHead(404); response.end('Not found'); return; }
  const body = request.method === 'GET' ? null : await new Promise(resolve => {
    let raw = ''; request.setEncoding('utf8'); request.on('data', chunk => raw += chunk);
    request.on('end', () => resolve(raw ? JSON.parse(raw) : null));
  });
  const path = url.pathname;
  if (request.method === 'GET') {
    if (path === `/api/platform/companies/${companyId}/logo`) {
      response.writeHead(200, { 'content-type': 'image/png', 'cache-control': 'no-store' });
      response.end(Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p1sAAAAASUVORK5CYII=', 'base64')); return;
    }
    if (path === '/api/session/csrf') return json(response, { requestToken: csrfToken });
    if (path === '/api/session') return json(response, { platformAdmin: true, name: 'Synthetic Platform Admin' });
    if (path === '/api/session/2fa') return json(response, { enabled: true });
    if (path === '/api/platform/system/health') {
      systemHealthReads++;
      return json(response, {
        generatedAt: '2026-10-16T12:00:00Z',
        application: { status: 'healthy', cpuPercent: 7.5, cpuSampleMilliseconds: 300, processorCount: 4,
          workingSetBytes: 536870912, managedHeapBytes: 268435456, memoryLimitBytes: 2147483648,
          memoryUsagePercent: 25, startedAt: '2026-10-16T10:00:00Z', uptimeSeconds: 7200 },
        database: { status: 'healthy', sizeBytes: 10485760 },
        disk: { scope: 'application-container-filesystem', root: '/', totalBytes: 10737418240,
          availableBytes: 8589934592, usedPercent: 20 },
        integrations: { smtpConfigured: true, iyzicoConfigured: false },
        note: 'Synthetic health note: application-container metrics only.'
      });
    }
    if (path === '/api/platform/overview') return json(response, {
      companies: { total: 1, active: 1, suspended: 0 },
      subscriptions: { paidActive: 1, customerTrials: 0, demos: 0, expired: 0, renewalsDue: 0 },
      checkout: { paymentPending: 0, paymentReview: 0, paymentFailed: 0, provisioningFailed: 0 }, recurringRevenue: { currencies: [] },
      recentPayments: [
        { id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1', companyName: 'Synthetic Paid Company', ownerName: 'Synthetic Paid Owner', ownerEmail: 'paid@example.invalid', planName: 'Cloud Pro', termDays: 365, amount: 1500, currency: 'TRY', status: 'Provisioned', createdAt: new Date(Date.now() - 2 * 60 * 60_000).toISOString(), paidAt: new Date(Date.now() - 60 * 60_000).toISOString() },
        { id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2', companyName: 'Synthetic Failed Company', ownerName: 'Synthetic Failed Owner', ownerEmail: 'failed@example.invalid', planName: 'Cloud Starter', termDays: 30, amount: 250, currency: 'USD', status: 'PaymentFailed', createdAt: new Date(Date.now() - 30 * 60_000).toISOString(), paidAt: null }
      ],
      activity: { activeUsers: 3, activeSessions: 4, loginUsers: 12, loginSessions: 17, recentLogins: [{
        displayName: 'Synthetic SaaS Owner', email: 'qa-owner@example.invalid', companyName: 'Synthetic Demo Company',
        platformAdmin: false, ipAddress: '192.0.2.10', userAgent: 'Synthetic Browser',
        createdAt: new Date(Date.now() - 2 * 60_000).toISOString(), lastSeenAt: new Date(Date.now() - 60_000).toISOString(),
        expiresAt: new Date(Date.now() + 60 * 60_000).toISOString(), revokedAt: null
      }] }
    });
    if (path === '/api/platform/companies/export.csv') {
      companyExports.push(Object.fromEntries(['q','active','planId','createdFrom','createdTo','sortBy','sortDirection'].map(key => [key, url.searchParams.get(key)])));
      response.writeHead(200, { 'content-type': 'text/csv; charset=utf-8', 'content-disposition': 'attachment; filename="envantertr-firmalar-20261016.csv"' });
      response.end('\uFEFF"Firma";"Durum"\r\n"\'=2+2;Injected";"Aktif"\r\n'); return;
    }
    if (path === '/api/platform/companies/renewals') return json(response, { items: [], total: 0, page: 1, pageSize: 50 });
    if (path === '/api/platform/companies' && url.searchParams.get('followUp') === 'overdue') return json(response, { items: companies, total: companies.length, page: 1, pageSize: 50 });
    if (path === '/api/platform/companies') {
      companyListRequests.push(Object.fromEntries(['q','active','planId','createdFrom','createdTo','sortBy','sortDirection','page','pageSize'].map(key => [key, url.searchParams.get(key)])));
      const q = (url.searchParams.get('q') || '').trim().toLowerCase();
      if (q === 'slow') await new Promise(resolve => setTimeout(resolve, 450));
      const items = q === 'slow' ? [demoCompanyOptions[1]] : q === 'fast' ? [demoCompanyOptions[2]] : demoCompanyOptions;
      return json(response, { items, total: items.length, page: 1, pageSize: 200 });
    }
    if (path === '/api/platform/demo-requests') {
      const q = (url.searchParams.get('q') || '').trim().toLowerCase();
      if (url.searchParams.get('followUp') === 'overdue') return json(response, {
        page: 1, pageSize: 50, total: 1,
        items: [{ ...demoLead, assignedAdminName: demoLead.assignedAdminUserId ? 'Synthetic Platform Admin' : null,
          linkedCompanyName: demoLead.companyId ? 'Synthetic Demo Company' : null }]
      });
      if (q === 'slow') await new Promise(resolve => setTimeout(resolve, 450));
      const leads = q === 'slow' ? [demoLead] : q === 'fast' ? [newerDemoLead] : [demoLead, newerDemoLead];
      return json(response, {
        page: Number(url.searchParams.get('page') || 1), pageSize: Number(url.searchParams.get('pageSize') || 25), total: leads.length,
        items: leads.map(lead => ({ ...lead, assignedAdminName: lead.assignedAdminUserId ? 'Synthetic Platform Admin' : null,
          linkedCompanyName: lead.companyId ? 'Synthetic Demo Company' : null }))
      });
    }
    if (path === `/api/platform/demo-requests/${demoLeadId}`) {
      if (delayOlderDemoDetail) { delayOlderDemoDetail = false; await new Promise(resolve => setTimeout(resolve, 600)); }
      return json(response, { lead: { ...demoLead,
        assignedAdminName: demoLead.assignedAdminUserId ? 'Synthetic Platform Admin' : null,
        linkedCompanyName: demoLead.companyId ? 'Synthetic Demo Company' : null,
        demoGrantedAt: demoLead.status === 'DemoGranted' ? '2026-10-12T10:00:00Z' : null }, history: [...demoLeadHistory] });
    }
    if (path === `/api/platform/demo-requests/${newerDemoLeadId}`) return json(response, { lead: { ...newerDemoLead,
      assignedAdminName: newerDemoLead.assignedAdminUserId ? 'Synthetic Platform Admin' : null,
      linkedCompanyName: newerDemoLead.companyId ? 'Synthetic Demo Company' : null,
      demoGrantedAt: newerDemoLead.status === 'DemoGranted' ? '2026-10-12T10:00:00Z' : null }, history: [...newerDemoLeadHistory] });
    if (path === '/api/platform/checkout-orders') return json(response, { items: [{
      id: failedOrderId, companyName: 'Synthetic Failed Payment', slug: 'synthetic-failed-payment',
      contactEmail: 'billing@example.invalid', ownerName: 'Synthetic Billing Owner', ownerEmail: 'billing@example.invalid',
      status: 'PaymentFailed', termDays: 30, amount: 1500, currency: 'TRY', paymentReference: 'synthetic-failed-payment-ref',
      companyId: null, createdAt: '2026-10-13T10:00:00Z', expiresAt: '2026-10-20T10:00:00Z', paidAt: null,
      planName: 'Cloud Pro', planCode: 'cloud-pro'
    }], page: 1, pageSize: 25, total: 1 });
    if (path === `/api/platform/checkout-orders/${failedOrderId}/follow-ups`) return json(response, {
      order: { id: failedOrderId, status: 'PaymentFailed', companyName: 'Synthetic Failed Payment', ownerName: 'Synthetic Billing Owner', ownerEmail: 'billing@example.invalid', amount: 1500, currency: 'TRY' },
      history: [...paymentFollowUpHistory]
    });
    if (path === `/api/platform/companies/${companyId}`) {
      if (delayOlderDemoCompanyFallback) {
        delayOlderDemoCompanyFallback = false;
        await new Promise(resolve => setTimeout(resolve, 700));
      }
      return json(response, {
      company: { ...companies[0], isActive: true, createdAt: '2026-09-02T10:00:00Z', contactEmail: 'qa-owner@example.invalid', lifecycleStage: 'AtRisk', lifecycleOwnerId: null, nextFollowUpAt: '2026-10-10T10:00:00Z' },
      members: [{ displayName: member.displayName, email: member.email, roleKey: 'owner', employeeName: null,
        accountCreatedAt: member.accountCreatedAt, createdAt: '2026-09-02T10:00:00Z', emailConfirmed: true, twoFactorEnabled: true }],
      memberCount: 1, membersPage: 1, membersTotalPages: 1, ownerMemberExists: true,
      usage: { generatedAt: '2026-10-16T15:00:00Z', assets: 28, employees: 12, loginAccounts: 4,
        licenses: 3, assignedLicenseSeats: 2, activeApiTokens: 1, planName: 'Cloud Pro', deploymentKind: 'cloud',
        maxAssets: 500, maxLoginUsers: null, recordedAttachmentCount: 4, recordedAttachmentBytes: 1048576,
        storageNote: 'Eklerin boyut toplamı yalnız veritabanında kayıtlı dosya eklerini kapsar; şirket logoları ve medya dizinindeki dosyalar dahil değildir. Planlarda dosya alanı sınırı tanımlı değildir.' },
      subscriptions: [{ ...companySubscription, name: plans[0]?.name || companySubscription.name, code: plans[0]?.code || companySubscription.code }],
      checkoutOrders: [], lifecycleHistory: []
      });
    }
    if (path === '/api/platform/admins') return json(response, [{ id: platformAdminId, name: 'Synthetic Platform Admin', email: 'operator@example.invalid' }]);
    if (path === '/api/platform/members') {
      const q = (url.searchParams.get('q') || '').toLocaleLowerCase();
      const items = accounts.filter(item => !q || `${item.displayName} ${item.email}`.toLocaleLowerCase().includes(q));
      const page = Number(url.searchParams.get('page') || 1), pageSize = Number(url.searchParams.get('pageSize') || 50);
      return json(response, { items, total: items.length, page, pageSize });
    }
    if (path === `/api/platform/members/${accountId}` || path === `/api/platform/members/${invitedAccountId}`) {
      const account = accounts.find(item => item.id === path.split('/').at(-1));
      return account ? json(response, { account, memberships: account.id === accountId ? [membership, secondMembership] : [], membershipsTruncated: false, membershipCount: account.membershipCount })
        : json(response, { message: 'Synthetic account not found.' }, 404);
    }
    if (path.endsWith('/timeline')) return json(response, { items: [
      { id: 'synthetic-membership-event', title: 'Şirkete üye oldu', occurredAt: '2026-09-02T10:00:00Z', description: 'Owner rolüyle üyelik başladı.', companyName: 'Synthetic Demo Company' },
      { id: 'synthetic-demo-event', title: 'Demo talebi alındı', occurredAt: '2026-09-01T10:00:00Z', description: 'Demo talebi değerlendirildi.', companyName: 'Synthetic Demo Company' },
      { id: 'synthetic-payment-event', title: 'Ücretli plan satın alındı', occurredAt: '2026-09-02T10:00:00Z', description: 'Sağlayıcı doğrulamalı sipariş.', companyName: 'Synthetic Demo Company', amount: 1200, currency: 'TRY', paymentReference: 'synthetic-provider-reference' }
    ], page: 1, pageSize: 50, hasMore: false, truncated: false });
  }

  if (request.method !== 'GET' && request.headers['x-csrf-token'] !== csrfToken) return json(response, { message: 'Missing synthetic CSRF token.' }, 400);
  if (path === '/api/platform/companies/bulk-status' && request.method === 'PATCH') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    return json(response, { updated: body.companyIds.length, unchanged: 0 });
  }
  if (path === '/api/platform/companies/bulk-plan' && request.method === 'PATCH') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    companySubscription.planId = body.planId;
    companySubscription.name = alternatePlan.name;
    directoryCompany.subscription.planId = body.planId;
    directoryCompany.subscription.planName = alternatePlan.name;
    return json(response, { updated: body.companyIds.length, unchanged: 0, planId: body.planId, planName: alternatePlan.name, billingChanged: false });
  }
  if (path === '/api/session/platform-reauth' && request.method === 'POST') {
    reauthenticated = true;
    response.writeHead(204); response.end(); return;
  }
  if (path.startsWith('/api/platform/members/') && request.method === 'PATCH' && path.endsWith('/role')) {
    if (!reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Synthetic reauthentication required.' }, 428);
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    const companyMembership = [membership, secondMembership].find(item => path === `/api/platform/members/${accountId}/memberships/${item.id}/role`);
    if (!companyMembership) return json(response, { message: 'Synthetic membership not found.' }, 404);
    companyMembership.roleKey = body.roleKey;
    member.memberships.find(item => item.id === companyMembership.id).roleKey = body.roleKey;
    response.writeHead(204); response.end(); return;
  }
  if (path === `/api/platform/members/${accountId}/security-actions` && request.method === 'POST') {
    if (!reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Synthetic reauthentication required.' }, 428);
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    if (body.action === 'lock') member.isLockedOut = true;
    if (body.action === 'unlock') member.isLockedOut = false;
    const messages = { lock: 'Synthetic account locked.', unlock: 'Synthetic account unlocked.', revokeSessions: 'Synthetic sessions revoked.', sendPasswordReset: 'Synthetic reset email sent.' };
    return json(response, { action: body.action, userId: accountId, message: messages[body.action], revokedSessions: body.action === 'lock' || body.action === 'revokeSessions' ? 2 : 0, isLockedOut: member.isLockedOut, accessFailedCount: 0 });
  }
  if (path === '/api/platform/members/invite' && request.method === 'POST') {
    if (!reauthenticated) return json(response, { code: 'platform_reauth_required', message: 'Synthetic reauthentication required.' }, 428);
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    const invited = { id: invitedAccountId, displayName: body.fullName, email: body.email, emailConfirmed: false,
      twoFactorEnabled: false, isLockedOut: false, accessFailedCount: 0, accountCreatedAt: null,
      lastSeenAt: null, firstMembershipAt: null, membershipCount: 1, memberships: [{ name: 'Synthetic Demo Company', roleKey: body.roleKey, isActive: true }] };
    accounts.push(invited);
    return json(response, { message: 'Sentetik davet kaydedildi.' });
  }
  if (path === '/api/platform/plans' && request.method === 'GET') return json(response, includeAlternatePlanOption ? [...plans, alternatePlan] : plans);
  if (path === '/api/platform/plans' && request.method === 'POST') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    const plan = { ...body, id: planId };
    plans.push(plan);
    return json(response, { id: plan.id, code: plan.code, name: plan.name }, 201);
  }
  if (path === `/api/platform/plans/${planId}` && request.method === 'PUT') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    Object.assign(plans[0], body);
    return json(response, { id: planId, code: plans[0].code, name: plans[0].name, isActive: plans[0].isActive });
  }
  if (path === `/api/platform/companies/${companyId}/subscriptions/${subscriptionId}` && request.method === 'PUT') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    Object.assign(companySubscription, body);
    const plan = plans.find(item => item.id === body.planId);
    if (plan) { companySubscription.name = plan.name; companySubscription.code = plan.code; }
    return json(response, { id: subscriptionId, planId: companySubscription.planId, startsAt: companySubscription.startsAt, endsAt: companySubscription.endsAt });
  }
  if (path === `/api/platform/demo-requests/${demoLeadId}` && request.method === 'PATCH') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    Object.assign(demoLead, body, { updatedAt: new Date().toISOString(), lastContactedAt: new Date().toISOString() });
    demoLeadHistory.unshift({ occurredAt: demoLead.updatedAt, actor: 'Synthetic Platform Admin', action: 'DemoRequestUpdated', details: body.note || '' });
    return json(response, { id: demoLeadId, status: demoLead.status, assignedAdminUserId: demoLead.assignedAdminUserId,
      followUpAt: demoLead.followUpAt, lastContactedAt: demoLead.lastContactedAt, companyId: demoLead.companyId, updatedAt: demoLead.updatedAt });
  }
  if (path === `/api/platform/demo-requests/${newerDemoLeadId}` && request.method === 'PATCH') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    Object.assign(newerDemoLead, body, { updatedAt: new Date().toISOString(), lastContactedAt: new Date().toISOString() });
    newerDemoLeadHistory.unshift({ occurredAt: newerDemoLead.updatedAt, actor: 'Synthetic Platform Admin', action: 'DemoRequestUpdated', details: body.note || '' });
    return json(response, { id: newerDemoLeadId, status: newerDemoLead.status, assignedAdminUserId: newerDemoLead.assignedAdminUserId,
      followUpAt: newerDemoLead.followUpAt, lastContactedAt: newerDemoLead.lastContactedAt, companyId: newerDemoLead.companyId, updatedAt: newerDemoLead.updatedAt });
  }
  if (path === '/api/platform/site-content') return json(response, []);
  if (path === '/api/platform/demo-requests') return json(response, { items: [], total: 0, page: 1, pageSize: 25 });
  if (path === `/api/platform/checkout-orders/${failedOrderId}/follow-ups` && request.method === 'POST') {
    writes.push({ method: request.method, path, body, csrf: request.headers['x-csrf-token'] });
    paymentFollowUpHistory.unshift({ id: `synthetic-follow-up-${paymentFollowUpHistory.length + 1}`,
      occurredAt: new Date().toISOString(), actor: 'Synthetic Platform Admin',
      details: JSON.stringify(body) });
    return json(response, { orderId: failedOrderId, outcome: body.outcome, nextFollowUpAt: body.nextFollowUpAt, recordedAt: new Date().toISOString() });
  }
  if (path === '/api/platform/support/tickets') return json(response, { items: [], total: 0, page: 1, pageSize: 25 });
  if (path === '/api/platform/audit' && request.method === 'GET') {
    const params = Object.fromEntries(url.searchParams.entries());
    auditRequests.push(params);
    if (params.q === 'synthetic audit service failure' && !auditErrorInjected) {
      auditErrorInjected = true;
      return json(response, { message: 'Synthetic audit service unavailable.' }, 503);
    }
    let filtered = [...auditEntries];
    const q = (params.q || '').toLocaleLowerCase();
    if (q) filtered = filtered.filter(x => `${x.action} ${x.targetType} ${x.details} ${x.actorName} ${x.actorEmail} ${x.companyName}`.toLocaleLowerCase().includes(q));
    if (params.source) filtered = filtered.filter(x => x.source === params.source);
    if (params.action) filtered = filtered.filter(x => x.action === params.action);
    if (params.targetType) filtered = filtered.filter(x => x.targetType === params.targetType);
    if (params.from) filtered = filtered.filter(x => Date.parse(x.occurredAt) >= Date.parse(params.from));
    if (params.to) filtered = filtered.filter(x => Date.parse(x.occurredAt) < Date.parse(params.to));
    filtered.sort((a, b) => b.occurredAt.localeCompare(a.occurredAt) || b.id.localeCompare(a.id) || a.source.localeCompare(b.source));
    const page = Number(params.page || 1), pageSize = Number(params.pageSize || 50), total = filtered.length;
    return json(response, { entries: filtered.slice((page - 1) * pageSize, page * pageSize), total, page, pageSize });
  }
  response.writeHead(404); response.end('Unknown synthetic API route');
});

try {
  browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  page.setDefaultTimeout(10_000);
  page.on('pageerror', error => browserErrors.push(error.message));
  page.on('response', response => {
    const path = new URL(response.url()).pathname;
    if (path.includes('/api/platform/members/') && path.endsWith('/role')) console.log(`SYNTHETIC role response: ${response.status()}`);
    if (path === '/api/session/platform-reauth') console.log(`SYNTHETIC reauthentication response: ${response.status()}`);
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const address = server.address();
  await page.goto(`http://127.0.0.1:${address.port}/platform-admin.html`);
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#systemHealthCards').getByText('7,5%', { exact: true }).waitFor({ state: 'visible' });
  assert.equal(systemHealthReads, 1, 'Opening the platform overview must load system health.');
  await page.locator('#systemHealthCards').getByText('PostgreSQL', { exact: true }).waitFor({ state: 'visible' });
  await page.locator('#systemHealthCards').getByText('SMTP ayarı', { exact: true }).waitFor({ state: 'visible' });
  assert.match(await page.locator('#systemHealthNote').innerText(), /application-container metrics only/);
  await page.locator('#overviewCards').getByText('Aktif kullanıcı · 5 dk', { exact: true }).waitFor({ state: 'visible' });
  assert.equal(await page.locator('#overviewCards .card').filter({ hasText: 'Aktif kullanıcı · 5 dk' }).locator('strong').innerText(), '3');
  assert.equal(await page.locator('#overviewCards .card').filter({ hasText: 'Etkin oturum · 5 dk' }).locator('strong').innerText(), '4');
  assert.equal(await page.locator('#overviewCards .card').filter({ hasText: 'Giriş yapan hesap · 24 sa' }).locator('strong').innerText(), '12');
  const recentPaymentsTable = page.locator('#recentPaymentsTable');
  await recentPaymentsTable.getByText('Synthetic Paid Company').waitFor({ state: 'visible' });
  await recentPaymentsTable.getByText('Synthetic Failed Company').waitFor({ state: 'visible' });
  await recentPaymentsTable.getByText('Şirket açıldı', { exact: true }).waitFor({ state: 'visible' });
  await recentPaymentsTable.getByText('Ödeme başarısız', { exact: true }).waitFor({ state: 'visible' });
  await page.locator('#recentPaymentsPanel [data-go="orders"]').click();
  await page.waitForFunction(() => document.querySelector('#view-orders')?.classList.contains('active'));
  await page.locator('#nav [data-view="overview"]').click();
  await page.locator('#recentPaymentsTable').getByText('Synthetic Paid Company').waitFor({ state: 'visible' });
  const recentLoginTable = page.locator('#recentLoginTable');
  await recentLoginTable.getByText('qa-owner@example.invalid').waitFor({ state: 'visible' });
  await recentLoginTable.getByText('192.0.2.10').waitFor({ state: 'visible' });
  await recentLoginTable.getByText('Etkin', { exact: true }).waitFor({ state: 'visible' });
  await recentLoginTable.getByRole('button', { name: 'Hesabı aç' }).click();
  await page.locator('#view-members').waitFor({ state: 'visible' });
  await page.locator('#memberSearch').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#memberSearch').inputValue(), 'qa-owner@example.invalid');
  await page.locator('#nav [data-view="overview"]').click();
  await page.locator('#recentLoginTable').getByText('qa-owner@example.invalid').waitFor({ state: 'visible' });
  const healthReadsBeforeManualRefresh = systemHealthReads;
  const manualHealthRefresh = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/platform/system/health');
  await page.locator('#refreshSystemHealth').click();
  await manualHealthRefresh;
  await page.waitForFunction(() => document.querySelector('#systemHealthCards')?.innerText.includes('PostgreSQL'));
  assert.ok(systemHealthReads > healthReadsBeforeManualRefresh, 'The health refresh button must make a new read-only API request.');
  const followUpTable = page.locator('#followUpTable');
  await followUpTable.getByText('Synthetic Demo Company').waitFor({ state: 'visible' });
  await followUpTable.getByText('Synthetic Demo Lead').waitFor({ state: 'visible' });
  assert.match(await followUpTable.innerText(), /Müşteri takip kuyruğu|Synthetic Demo Company[\s\S]*Synthetic Demo Lead/);
  await followUpTable.locator('[data-follow-up-kind="company"]').click();
  await page.locator('#companyDetailDialog').waitFor({ state: 'visible' });
  await page.locator('#companyDetailDialog [data-detail-close]').first().click();
  await page.locator('#nav [data-view="overview"]').click();
  await followUpTable.getByText('Synthetic Demo Lead').waitFor({ state: 'visible' });
  await followUpTable.locator('[data-follow-up-kind="lead"]').click();
  await page.locator('#demoLeadDetail').waitFor({ state: 'visible' });
  await page.locator('#demoLeadDetailTitle').getByText('Synthetic Demo Lead').waitFor({ state: 'visible' });
  await page.locator('#demoLeadDetailBody [name="status"]').selectOption('Contacted');
  await page.locator('#demoLeadDetailBody [name="assignedAdminUserId"]').selectOption(platformAdminId);
  await page.locator('#demoLeadDetailBody [name="followUpAt"]').fill('2026-10-20T13:45');
  await page.locator('#demoLeadDetailBody [name="companyId"]').selectOption(companyId);
  await page.locator('#demoLeadDetailBody [name="note"]').fill('Synthetic browser QA follow-up');
  const leadWriteStart = writes.length;
  const leadSaveResponse = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/demo-requests/${demoLeadId}`);
  await page.locator('#demoLeadDetailBody button[type="submit"]').click();
  assert.equal((await leadSaveResponse).status(), 200, 'Saving a demo lead must succeed before the panel reloads.');
  const leadWrite = writes.slice(leadWriteStart).find(item => item.path === `/api/platform/demo-requests/${demoLeadId}`);
  assert.ok(leadWrite, 'Saving a demo lead must issue an API write.');
  assert.equal(leadWrite.method, 'PATCH');
  assert.equal(leadWrite.csrf, csrfToken);
  assert.equal(leadWrite.body.status, 'Contacted');
  assert.equal(leadWrite.body.assignedAdminUserId, platformAdminId);
  assert.equal(leadWrite.body.followUpAt, new Date('2026-10-20T13:45').toISOString());
  assert.equal(leadWrite.body.companyId, companyId);
  assert.equal(leadWrite.body.note, 'Synthetic browser QA follow-up');
  await page.locator('#demoLeadDetailBody').getByText('Synthetic browser QA follow-up').waitFor({ state: 'visible' });
  await page.locator('#demoLeadDetailBody [name="status"]').selectOption('Qualified');
  const leadSecondSaveResponse = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/demo-requests/${demoLeadId}`);
  await page.locator('#demoLeadDetailBody button[type="submit"]').click();
  assert.equal((await leadSecondSaveResponse).status(), 200);
  await page.locator('#demoLeadDetailBody [name="status"]').waitFor({ state: 'visible' });
  assert.equal(demoLead.status, 'Qualified', 'A subsequent edit must persist after the detail view reloads.');
  assert.equal(demoLead.assignedAdminUserId, platformAdminId);
  assert.equal(demoLead.companyId, companyId);
  assert.equal(demoLeadHistory.length, 2, 'Each successful lead update must append visible history.');
  await page.locator('#nav [data-view="demo-requests"]').click();
  const demoLeadsTable = page.locator('#demoLeadsTable');
  await demoLeadsTable.getByText('Synthetic Demo Lead').waitFor({ state: 'visible' });
  await demoLeadsTable.getByText('Synthetic Newer Demo').waitFor({ state: 'visible' });
  const demoSearch = page.locator('#demoLeadSearch');
  const slowLeadRequest = page.waitForRequest(request => {
    const requestUrl = new URL(request.url());
    return requestUrl.pathname === '/api/platform/demo-requests' && requestUrl.searchParams.get('q') === 'slow';
  });
  const slowLeadResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/demo-requests' && requestUrl.searchParams.get('q') === 'slow';
  });
  await demoSearch.fill('slow'); await demoSearch.press('Enter'); await slowLeadRequest;
  const fastLeadResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/demo-requests' && requestUrl.searchParams.get('q') === 'fast';
  });
  await demoSearch.fill('fast'); await demoSearch.press('Enter');
  assert.equal((await fastLeadResponse).status(), 200);
  await demoLeadsTable.getByText('Synthetic Newer Demo').waitFor({ state: 'visible' });
  await slowLeadResponse;
  assert.match(await demoLeadsTable.innerText(), /Synthetic Newer Demo/);
  assert.doesNotMatch(await demoLeadsTable.innerText(), /Synthetic Demo Lead/,
    'An older filtered list response must not replace the latest search result.');

  const allLeadResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/demo-requests' && !requestUrl.searchParams.has('q');
  });
  await demoSearch.fill(''); await page.locator('#searchDemoLeads').click(); await allLeadResponse;
  await demoLeadsTable.getByText('Synthetic Demo Lead').waitFor({ state: 'visible' });
  delayOlderDemoDetail = true;
  const olderDetailRequest = page.waitForRequest(request => new URL(request.url()).pathname === `/api/platform/demo-requests/${demoLeadId}`);
  const olderDetailResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/demo-requests/${demoLeadId}`);
  await demoLeadsTable.locator('tr').filter({ hasText: 'Synthetic Demo Lead' }).locator('[data-demo-lead-detail]').click();
  await olderDetailRequest;
  const newerDetailResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/demo-requests/${newerDemoLeadId}`);
  await demoLeadsTable.locator('tr').filter({ hasText: 'Synthetic Newer Demo' }).locator('[data-demo-lead-detail]').click();
  assert.equal((await newerDetailResponse).status(), 200);
  await page.locator('#demoLeadDetailTitle').getByText('Synthetic Newer Demo').waitFor({ state: 'visible' });

  const companySearch = page.locator('#demoLeadCompanySearch');
  const slowCompanyRequest = page.waitForRequest(request => {
    const requestUrl = new URL(request.url());
    return requestUrl.pathname === '/api/platform/companies' && requestUrl.searchParams.get('q') === 'slow';
  });
  const slowCompanyResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/companies' && requestUrl.searchParams.get('q') === 'slow';
  });
  delayOlderDemoCompanyFallback = true;
  const slowCompanyFallbackRequest = page.waitForRequest(request => new URL(request.url()).pathname === `/api/platform/companies/${companyId}`);
  const slowCompanyFallbackResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/companies/${companyId}`);
  await companySearch.fill('slow');
  await slowCompanyRequest;
  await slowCompanyFallbackRequest;
  const fastCompanyResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/companies' && requestUrl.searchParams.get('q') === 'fast';
  });
  const fastCompanyFallbackResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/companies/${companyId}`);
  await companySearch.fill('fast');
  assert.equal((await fastCompanyResponse).status(), 200);
  await slowCompanyResponse;
  assert.equal((await fastCompanyFallbackResponse).status(), 200);
  const companyOptions = await page.locator('#demoLeadCompany option').allTextContents();
  assert.ok(companyOptions.some(option => option.includes('Fast Synthetic Company')));
  assert.ok(!companyOptions.some(option => option.includes('Slow Synthetic Company')),
    'An older company search response must not replace the latest company options.');
  await page.locator('#demoLeadCompany').selectOption(fastCompanyId);
  await slowCompanyFallbackResponse;
  assert.equal(await page.locator('#demoLeadCompany').inputValue(), fastCompanyId,
    'A delayed selected-company fallback must not revert a newer company selection.');

  await olderDetailResponse;
  assert.equal(await page.locator('#demoLeadDetailTitle').innerText(), 'Synthetic Newer Demo',
    'A delayed older detail response must not replace the currently selected request.');
  assert.equal(await page.locator('#demoLeadDetailBody [name="status"]').inputValue(), 'New',
    'The form fields must belong to the newer selected request after the older response finishes.');
  await page.locator('#demoLeadDetailBody [name="status"]').selectOption('Contacted');
  await page.locator('#demoLeadDetailBody [name="companyId"]').selectOption(fastCompanyId);
  await page.locator('#demoLeadDetailBody [name="note"]').fill('Synthetic newer request update');
  const newerLeadWriteResponse = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/demo-requests/${newerDemoLeadId}`);
  await page.locator('#demoLeadDetailBody button[type="submit"]').click();
  assert.equal((await newerLeadWriteResponse).status(), 200);
  const newerLeadWrite = writes.find(item => item.path === `/api/platform/demo-requests/${newerDemoLeadId}`);
  assert.equal(newerLeadWrite.body.status, 'Contacted');
  assert.equal(newerLeadWrite.body.companyId, fastCompanyId);
  assert.equal(newerLeadWrite.body.note, 'Synthetic newer request update');
  assert.equal(newerDemoLead.status, 'Contacted');
  assert.equal(newerDemoLead.companyId, fastCompanyId);
  assert.equal(newerDemoLeadHistory.length, 1, 'Saving the current details after out-of-order responses must audit the selected lead only.');

  await page.locator('#nav [data-view="orders"]').click();
  const paymentRow = page.locator('#ordersTable tr').filter({ hasText: 'Synthetic Failed Payment' });
  await paymentRow.waitFor({ state: 'visible' });
  const paymentWritesBeforeCancel = writes.length;
  await paymentRow.locator('[data-payment-follow-up]').click();
  const paymentDialog = page.locator('#paymentFollowUpDialog');
  await paymentDialog.waitFor({ state: 'visible' });
  await paymentDialog.locator('#paymentFollowUpHistory').getByText('Bu ödeme için henüz takip kaydı yok.').waitFor({ state: 'visible' });
  await paymentDialog.locator('#cancelPaymentFollowUp').click();
  await paymentDialog.waitFor({ state: 'hidden' });
  assert.equal(writes.length, paymentWritesBeforeCancel, 'Canceling a payment follow-up must not write.');
  await page.locator('#ordersTable [data-payment-follow-up]').click();
  await paymentDialog.locator('[name="outcome"]').selectOption('ReminderScheduled');
  await paymentDialog.locator('[name="nextFollowUpAt"]').fill('2026-10-20T14:15');
  await paymentDialog.locator('[name="note"]').fill('Synthetic billing contact attempt.');
  const paymentFollowUpSaved = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/platform/checkout-orders/${failedOrderId}/follow-ups`);
  await paymentDialog.locator('button[type="submit"]').click();
  assert.equal((await paymentFollowUpSaved).status(), 200, 'The payment follow-up should save to the platform API.');
  await paymentDialog.getByText('Synthetic billing contact attempt.').waitFor({ state: 'visible' });
  const paymentFollowUpWrite = writes.at(-1);
  assert.equal(paymentFollowUpWrite.csrf, csrfToken);
  assert.equal(paymentFollowUpWrite.body.outcome, 'ReminderScheduled');
  assert.equal(paymentFollowUpWrite.body.nextFollowUpAt, new Date('2026-10-20T14:15').toISOString());
  assert.equal(paymentFollowUpWrite.body.note, 'Synthetic billing contact attempt.');
  await paymentDialog.locator('#closePaymentFollowUp').click();
  await paymentDialog.waitFor({ state: 'hidden' });
  await page.locator('#ordersTable [data-payment-follow-up]').click();
  await paymentDialog.getByText('Synthetic billing contact attempt.').waitFor({ state: 'visible' });
  assert.equal(paymentFollowUpHistory.length, 1, 'Payment follow-up history must persist after reopening the detail.');
  await paymentDialog.locator('#cancelPaymentFollowUp').click();
  await page.locator('#nav [data-view="members"]').click();
  const memberRow = page.locator('#membersTable tr').filter({ hasText: 'qa-owner@example.invalid' });
  await memberRow.waitFor({ state: 'visible' });
  assert.match(await memberRow.innerText(), /doğrulanmış[\s\S]*2FA açık[\s\S]*2026[\s\S]*Synthetic Demo Company[\s\S]*Yönetici/);

  const searchResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/platform/members'
    && new URL(response.url()).searchParams.get('q') === 'qa-owner@example.invalid');
  await page.locator('#memberSearch').fill('qa-owner@example.invalid');
  await page.locator('#memberSearch').press('Enter');
  await searchResponse;
  assert.equal(await page.locator('#memberCount').innerText(), '1 hesap');
  await page.locator('#membersTable [data-member-detail]').click();
  await page.locator('#memberDetail').waitFor({ state: 'visible' });
  const detail = page.locator('#memberDetailBody');
  await detail.getByText('synthetic-provider-reference').first().waitFor({ state: 'visible' });
  const detailText = await detail.innerText();
  assert.match(detailText, /Hesap açılış tarihi[\s\S]*İlk şirket üyeliği[\s\S]*Cloud Pro[\s\S]*synthetic-provider-reference/);
  assert.match(detailText, /Synthetic Demo Company[\s\S]*Yönetici[\s\S]*Cloud Pro[\s\S]*Synthetic Second Company[\s\S]*Salt okunur[\s\S]*Cloud Starter[\s\S]*Demo/,
    'The account detail must show each company membership, its separate role, and its plan state.');
  assert.equal(await detail.locator('[data-member-role]').count(), 2, 'Each non-owner company membership must have its own role control.');
  assert.match(await page.locator('#memberTimeline').innerText(), /Demo talebi alındı[\s\S]*Ücretli plan satın alındı[\s\S]*1\.200,00/);

  const writeCountBeforeCancel = writes.length;
  await detail.locator('[data-member-role]').nth(1).click();
  const roleDialog = page.locator('dialog.dialog').filter({ hasText: 'Üyelik rolünü değiştir' });
  await roleDialog.waitFor({ state: 'visible' });
  await roleDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await roleDialog.waitFor({ state: 'detached' });
  assert.equal(writes.length, writeCountBeforeCancel, 'Canceling the membership-role dialog must not write.');

  await detail.locator('[data-member-role]').nth(1).click();
  const roleDialogAgain = page.locator('dialog.dialog').filter({ hasText: 'Üyelik rolünü değiştir' });
  await roleDialogAgain.waitFor({ state: 'visible' });
  await roleDialogAgain.locator('[name="roleKey"]').selectOption('asset_manager');
  await roleDialogAgain.locator('[name="note"]').fill('Synthetic role review');
  const roleRejectedUntilReauth = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/memberships/${secondCompanyId}/role`);
  await roleDialogAgain.getByRole('button', { name: 'Rolü kaydet' }).click();
  assert.equal((await roleRejectedUntilReauth).status(), 428, 'Role update must stop before recent MFA reauthentication.');
  await page.locator('#platformReauthDialog').waitFor({ state: 'visible' });
  const writesBeforeStepUpCancel = writes.length;
  await page.locator('#platformReauthCancel').click();
  await page.locator('#platformReauthDialog').waitFor({ state: 'hidden' });
  await roleDialogAgain.locator('.notice.error').filter({ hasText: 'Kritik işlem iptal edildi' }).waitFor({ state: 'visible' });
  assert.equal(writes.length, writesBeforeStepUpCancel, 'Canceling step-up reauthentication must not replay the pending role change.');
  const roleRejectedAfterStepUpCancel = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/memberships/${secondCompanyId}/role`);
  await roleDialogAgain.getByRole('button', { name: 'Rolü kaydet' }).click();
  assert.equal((await roleRejectedAfterStepUpCancel).status(), 428, 'A fresh explicit submit is required after step-up cancellation.');
  await page.locator('#platformReauthDialog').waitFor({ state: 'visible' });
  await page.locator('#platformReauthForm [name="password"]').fill('Synthetic-only-password');
  await page.locator('#platformReauthForm [name="code"]').fill('123456');
  const reauthResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/session/platform-reauth');
  const roleSaved = page.waitForResponse(response => response.request().method() === 'PATCH'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/memberships/${secondCompanyId}/role`
    && response.status() === 204);
  await page.locator('#platformReauthSubmit').click();
  assert.equal((await reauthResponse).status(), 204, 'Synthetic second-factor reauthentication must succeed.');
  await roleSaved;
  await page.locator('#platformReauthDialog').waitFor({ state: 'hidden' });
  await page.locator('#memberDetailBody').getByText('Envanter yöneticisi').waitFor({ state: 'visible' });
  assert.equal(membership.roleKey, 'admin', 'Changing a second-company role must preserve the first company role.');
  assert.equal(secondMembership.roleKey, 'asset_manager', 'The selected second-company membership must receive the role change.');
  assert.equal(writes.length, writeCountBeforeCancel + 1);
  assert.deepEqual(writes[writeCountBeforeCancel], { method: 'PATCH', path: `/api/platform/members/${accountId}/memberships/${secondCompanyId}/role`,
    body: { roleKey: 'asset_manager', note: 'Synthetic role review' }, csrf: csrfToken });

  const securityWritesBeforeCancel = writes.length;
  await detail.locator('[data-member-security="lock"]').click();
  const securityDialog = page.locator('dialog.dialog').filter({ hasText: 'Hesabı geçici kilitle' });
  await securityDialog.waitFor({ state: 'visible' });
  assert.match(await securityDialog.innerText(), /mevcut oturumları da sonlandırılır/i);
  await securityDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await securityDialog.waitFor({ state: 'detached' });
  assert.equal(writes.length, securityWritesBeforeCancel, 'Canceling a security action must not write.');

  await detail.locator('[data-member-security="lock"]').click();
  const lockDialog = page.locator('dialog.dialog').filter({ hasText: 'Hesabı geçici kilitle' });
  await lockDialog.locator('[name="lockoutDays"]').selectOption('7');
  await lockDialog.locator('[name="note"]').fill('Synthetic suspicious sign-in response.');
  const locked = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/security-actions`);
  await lockDialog.getByRole('button', { name: 'Hesabı geçici kilitle' }).click();
  assert.equal((await locked).status(), 200);
  await page.locator('[data-member-security="unlock"]').waitFor({ state: 'visible' });
  assert.equal(member.isLockedOut, true);
  assert.deepEqual(writes.at(-1), { method: 'POST', path: `/api/platform/members/${accountId}/security-actions`,
    body: { action: 'lock', note: 'Synthetic suspicious sign-in response.', lockoutDays: 7 }, csrf: csrfToken });

  await page.locator('[data-member-security="unlock"]').click();
  const unlockDialog = page.locator('dialog.dialog').filter({ hasText: 'Hesap kilidini kaldır' });
  await unlockDialog.locator('[name="note"]').fill('Synthetic identity verified.');
  const unlocked = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/security-actions`);
  await unlockDialog.getByRole('button', { name: 'Hesap kilidini kaldır' }).click();
  assert.equal((await unlocked).status(), 200);
  await page.locator('[data-member-security="lock"]').waitFor({ state: 'visible' });
  assert.equal(member.isLockedOut, false);

  await page.locator('[data-member-security="revokeSessions"]').click();
  const sessionsDialog = page.locator('dialog.dialog').filter({ hasText: 'Tüm oturumları sonlandır' });
  await sessionsDialog.locator('[name="note"]').fill('Synthetic lost device report.');
  const sessionsRevoked = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/security-actions`);
  await sessionsDialog.getByRole('button', { name: 'Tüm oturumları sonlandır' }).click();
  assert.equal((await sessionsRevoked).status(), 200);

  await page.locator('[data-member-security="sendPasswordReset"]').click();
  const resetDialog = page.locator('dialog.dialog').filter({ hasText: 'Parola yenileme e-postası gönder' });
  assert.match(await resetDialog.innerText(), /Parola değiştirilmez/);
  await resetDialog.locator('[name="note"]').fill('Synthetic user-requested recovery.');
  const resetSent = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/platform/members/${accountId}/security-actions`);
  await resetDialog.getByRole('button', { name: 'Parola yenileme e-postası gönder' }).click();
  assert.equal((await resetSent).status(), 200);
  assert.deepEqual(writes.slice(-3).map(write => write.body.action), ['unlock', 'revokeSessions', 'sendPasswordReset']);
  assert.ok(writes.slice(-4).every(write => write.csrf === csrfToken), 'Each account-security action must carry the CSRF token.');

  await page.locator('#closeMemberDetail').click();
  await page.locator('#openMemberInvite').click();
  const inviteDialog = page.locator('#memberInviteDialog');
  await inviteDialog.waitFor({ state: 'visible' });
  await page.locator('#memberInviteCompany').selectOption(companyId);
  await inviteDialog.locator('[name="fullName"]').fill('Synthetic Invited Viewer');
  await inviteDialog.locator('[name="email"]').fill('qa-invitee@example.invalid');
  await inviteDialog.locator('[name="roleKey"]').selectOption('viewer');
  const invitationSaved = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/platform/members/invite' && response.status() === 200);
  await page.locator('#submitMemberInvite').click();
  await invitationSaved;
  await page.locator('#membersTable').getByText('qa-invitee@example.invalid').waitFor({ state: 'visible' });
  assert.equal(writes.length, securityWritesBeforeCancel + 5,
    'Exactly four confirmed account-security actions plus the invitation must write after the canceled action.');
  assert.deepEqual(writes.at(-1), { method: 'POST', path: '/api/platform/members/invite',
    body: { companyId, fullName: 'Synthetic Invited Viewer', email: 'qa-invitee@example.invalid', roleKey: 'viewer' }, csrf: csrfToken });
  const planListLoaded = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/platform/plans');
  await page.locator('#nav [data-view="plans"]').click();
  const planListResponse = await planListLoaded;
  assert.equal(planListResponse.status(), 200, `Plan list GET returned ${planListResponse.status()}: ${await planListResponse.text()}`);
  assert.deepEqual(await planListResponse.json(), []);
  await page.locator('#plansTable').getByText('Henüz plan oluşturulmamış.').waitFor({ state: 'visible' });
  await page.locator('#newPlan').click();
  const planForm = page.locator('#planForm');
  await planForm.locator('[name="changeReason"]').fill('Synthetic pricing approval');
  await planForm.locator('[name="code"]').fill('synthetic-cloud-pro');
  await planForm.locator('[name="name"]').fill('Synthetic Cloud Pro');
  await planForm.locator('[name="deploymentKind"]').selectOption('cloud');
  assert.equal(await planForm.locator('#planMaintenanceEnabled').isChecked(), true,
    'New Cloud plans must include maintenance unless the platform admin disables it.');
  assert.equal(await planForm.locator('[name="maxLoginUsers"]').isDisabled(), true,
    'Cloud plans must not receive a local login-user cap.');
  await planForm.locator('[name="featureFlagsJson"]').fill('[]');
  const writesBeforeInvalidPlan = writes.length;
  await planForm.getByRole('button', { name: 'Planı Kaydet' }).click();
  await page.getByText('Özellik bayrakları JSON nesnesi olmalı ve değerleri true/false olmalıdır.').waitFor({ state: 'visible' });
  assert.equal(writes.length, writesBeforeInvalidPlan, 'An invalid feature-flags document must not write a plan.');
  await page.locator('#clearPlan').click();
  assert.equal(writes.length, writesBeforeInvalidPlan, 'Clearing a draft plan must not write.');
  await planForm.locator('[name="changeReason"]').fill('Synthetic pricing approval');
  await planForm.locator('[name="code"]').fill('synthetic-cloud-pro');
  await planForm.locator('[name="name"]').fill('Synthetic Cloud Pro');
  await planForm.locator('[name="deploymentKind"]').selectOption('cloud');
  await planForm.locator('[name="currency"]').selectOption('TRY');
  await planForm.locator('[name="monthlyPrice"]').fill('990');
  await planForm.locator('[name="annualPrice"]').fill('9900');
  await planForm.locator('[name="maxAssets"]').fill('500');
  await planForm.locator('[name="featureFlagsJson"]').fill('{"reports":true}');
  await planForm.locator('#planReportsEnabled').uncheck();
  await planForm.locator('#planApiEnabled').uncheck();
  await planForm.locator('#planMaintenanceEnabled').uncheck();
  const planCreated = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/api/platform/plans' && response.status() === 201);
  await planForm.getByRole('button', { name: 'Planı Kaydet' }).click();
  await planCreated;
  await page.locator('#plansTable').getByText('Synthetic Cloud Pro').waitFor({ state: 'visible' });
  assert.equal(writes.length, writesBeforeInvalidPlan + 1);
  assert.deepEqual(writes.at(-1), { method: 'POST', path: '/api/platform/plans', csrf: csrfToken,
    body: { changeReason: 'Synthetic pricing approval', code: 'synthetic-cloud-pro', name: 'Synthetic Cloud Pro', deploymentKind: 'cloud', currency: 'TRY', maxLoginUsers: null, maxAssets: 500, monthlyPrice: 990, annualPrice: 9900, oneTimePrice: 0, offlineGraceDays: 30, description: '', featureFlagsJson: '{"reports":false,"api":false,"maintenance":false}', supportsOfflineUse: false, isPublic: true, isActive: true, sortOrder: 0 } });

  await page.locator(`#plansTable [data-edit-plan="${planId}"]`).click();
  assert.equal(await planForm.locator('#planReportsEnabled').isChecked(), false,
    'Editing a plan must restore its saved reports entitlement.');
  assert.equal(await planForm.locator('#planApiEnabled').isChecked(), false,
    'Editing a plan must restore its saved customer API entitlement.');
  assert.equal(await planForm.locator('#planMaintenanceEnabled').isChecked(), false,
    'Editing a plan must restore its saved maintenance entitlement.');
  await planForm.locator('[name="changeReason"]').fill('Synthetic annual price correction');
  await planForm.locator('[name="annualPrice"]').fill('8900');
  await planForm.locator('[name="isPublic"]').uncheck();
  const planUpdated = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/platform/plans/${planId}` && response.status() === 200);
  await planForm.getByRole('button', { name: 'Planı Kaydet' }).click();
  await planUpdated;
  assert.equal(writes.length, writesBeforeInvalidPlan + 2);
  assert.equal(writes.at(-1).body.annualPrice, 8900);
  assert.equal(writes.at(-1).body.isPublic, false);
  assert.equal(writes.at(-1).body.changeReason, 'Synthetic annual price correction');
  assert.equal(writes.at(-1).body.featureFlagsJson, '{"reports":false,"api":false,"maintenance":false}',
    'Updating another plan field must preserve saved feature entitlements.');
  const hiddenPlanRow = page.locator('#plansTable tr').filter({ hasText: 'Synthetic Cloud Pro' });
  await hiddenPlanRow.getByText('Gizli').waitFor({ state: 'visible' });
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#nav [data-view="plans"]').click();
  const reloadedPlanRow = page.locator('#plansTable tr').filter({ hasText: 'Synthetic Cloud Pro' });
  await reloadedPlanRow.getByText('Gizli').waitFor({ state: 'visible' });
  assert.match(await reloadedPlanRow.innerText(), /8\.900,00/,
    'The edited yearly price and publication state must be reloaded from the synthetic API after a page refresh.');
  await reloadedPlanRow.locator(`[data-edit-plan="${planId}"]`).click();
  assert.equal(await planForm.locator('#planMaintenanceEnabled').isChecked(), false,
    'Maintenance entitlement must persist after a full page reload.');

  await page.locator('#nav [data-view="companies"]').click();
  const companyRow = page.locator('#companiesTable tr').filter({ hasText: 'Synthetic Demo Company' });
  let releaseOlderCompanyDetail;
  const olderCompanyDetailGate = new Promise(resolve => { releaseOlderCompanyDetail = resolve; });
  await page.route(`**/api/platform/companies/${companyId}?membersPage=*`, async route => {
    const response = await route.fetch();
    const data = await response.json();
    data.company.name = 'STALE COMPANY RESPONSE';
    await olderCompanyDetailGate;
    await route.fulfill({ response, json: data });
  }, { times: 1 });
  const delayedCompanyRequest = page.waitForRequest(request => new URL(request.url()).pathname === `/api/platform/companies/${companyId}`);
  await companyRow.locator('[data-action="details"]').click();
  await delayedCompanyRequest;
  await page.locator('#companyDetailDialog [data-detail-close]').first().click();
  await companyRow.locator('[data-action="details"]').click();
  const companyDialog = page.locator('#companyDetailDialog');
  await companyDialog.locator('#companyDetailTitle').getByText('Synthetic Demo Company', { exact: true }).waitFor({ state: 'visible' });
  const olderCompanyResponse = page.waitForResponse(response => new URL(response.url()).pathname === `/api/platform/companies/${companyId}`);
  releaseOlderCompanyDetail();
  await olderCompanyResponse;
  await page.waitForLoadState('networkidle');
  assert.equal(await companyDialog.locator('#companyDetailTitle').innerText(), 'Synthetic Demo Company',
    'Closing and reopening company details must invalidate the older response.');
  await companyDialog.waitFor({ state: 'visible' });
  const usagePanel = page.locator('#companyUsagePanel');
  await usagePanel.getByText('Etkin varlık').waitFor({ state: 'visible' });
  assert.match(await usagePanel.innerText(), /28\s*\/\s*500/, 'The company usage view must show asset usage against its active plan limit.');
  assert.match(await usagePanel.innerText(), /4\s*\/\s*Sınırsız/, 'Cloud login-user quota must be reported as unlimited, not as an invented numeric cap.');
  assert.match(await usagePanel.innerText(), /1\s*MB/, 'The usage view must show bytes recorded for attachments.');
  assert.match(await usagePanel.innerText(), /şirket logoları ve medya dizinindeki dosyalar dahil değildir/i, 'The usage view must explain what recorded attachment storage excludes.');
  const listedSubscription = page.locator('#companyDetailBody .panel').filter({ hasText: 'Abonelik geçmişi' }).locator('tbody tr');
  await listedSubscription.getByText('Synthetic Cloud Pro').waitFor({ state: 'visible' });
  assert.equal(await listedSubscription.locator('[data-admin-edit-subscription]').count(), 1,
    'A subscription must have exactly one working plan/date action.');
  const writesBeforeSubscriptionCancel = writes.length;
  await listedSubscription.locator('[data-admin-edit-subscription]').click();
  const subscriptionDialog = page.locator('dialog.dialog').filter({ hasText: 'Abonelik planını ve tarihlerini düzenle' });
  await subscriptionDialog.waitFor({ state: 'visible' });
  await subscriptionDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await subscriptionDialog.waitFor({ state: 'detached' });
  assert.equal(writes.length, writesBeforeSubscriptionCancel, 'Canceling subscription editing must not write.');

  await listedSubscription.locator('[data-admin-edit-subscription]').click();
  const subscriptionDialogAgain = page.locator('dialog.dialog').filter({ hasText: 'Abonelik planını ve tarihlerini düzenle' });
  await subscriptionDialogAgain.locator('[name="planId"]').selectOption(planId);
  await subscriptionDialogAgain.locator('[name="startsAt"]').fill('2026-11-15T10:00');
  await subscriptionDialogAgain.locator('[name="endsAt"]').fill('2026-10-15T10:00');
  await subscriptionDialogAgain.locator('[name="note"]').fill('Synthetic invalid date-range rejection');
  await subscriptionDialogAgain.getByRole('button', { name: 'Değişiklikleri kaydet' }).click();
  await subscriptionDialogAgain.locator('[name="endsAt"]').waitFor({ state: 'visible' });
  assert.equal(writes.length, writesBeforeSubscriptionCancel, 'An invalid subscription date range must not write.');
  await subscriptionDialogAgain.locator('[name="startsAt"]').fill('2026-10-15T10:00');
  await subscriptionDialogAgain.locator('[name="endsAt"]').fill('2028-10-15T10:00');
  await subscriptionDialogAgain.locator('[name="note"]').fill('Synthetic complimentary extension and plan review');
  const subscriptionSaved = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/platform/companies/${companyId}/subscriptions/${subscriptionId}`);
  await subscriptionDialogAgain.getByRole('button', { name: 'Değişiklikleri kaydet' }).click();
  assert.equal((await subscriptionSaved).status(), 200, 'Synthetic company subscription update must succeed.');
  await page.getByText('Abonelik planı ve tarihleri güncellendi.').waitFor({ state: 'visible' });
  assert.equal(writes.length, writesBeforeSubscriptionCancel + 1);
  assert.deepEqual(writes.at(-1), { method: 'PUT', path: `/api/platform/companies/${companyId}/subscriptions/${subscriptionId}`,
    csrf: csrfToken, body: { planId, startsAt: '2026-10-15T10:00:00.000Z', endsAt: '2028-10-15T10:00:00.000Z', note: 'Synthetic complimentary extension and plan review' } });
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.locator('#appShell').waitFor({ state: 'visible' });
  await page.locator('#nav [data-view="companies"]').click();
  await page.locator('#companiesTable tr').filter({ hasText: 'Synthetic Demo Company' }).locator('[data-action="details"]').click();
  const persistedSubscription = page.locator('#companyDetailBody .panel').filter({ hasText: 'Abonelik geçmişi' }).locator('tbody tr');
  await persistedSubscription.getByText('Synthetic Cloud Pro').waitFor({ state: 'visible' });
  await persistedSubscription.getByText(/2028/).waitFor({ state: 'visible' });
  await page.locator('#companyDetailDialog [data-detail-close]').first().click();
  await page.locator('#companyDetailDialog').waitFor({ state: 'hidden' });

  const writesBeforeAuditRead = writes.length;
  await page.locator('#nav [data-view="audit"]').click();
  await page.locator('#auditPage').getByText('Sayfa 1 · 51 kayıt').waitFor({ state: 'visible' });
  await page.locator('#auditSearch').fill('Unique audit filter marker');
  await page.locator('#auditSource').selectOption('platform');
  await page.locator('#auditAction').fill('PlanUpdated');
  await page.locator('#auditTargetType').fill('Plan');
  await page.locator('#auditFrom').fill('2026-10-15');
  await page.locator('#auditTo').fill('2026-10-15');
  const filteredAuditResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/audit' && requestUrl.searchParams.get('q') === 'Unique audit filter marker';
  });
  await page.locator('#searchAudit').click();
  assert.equal((await filteredAuditResponse).status(), 200, 'Applying platform audit filters must return successfully.');
  const filteredAuditRequest = auditRequests.at(-1);
  assert.deepEqual(filteredAuditRequest, {
    page: '1', pageSize: '50', q: 'Unique audit filter marker', source: 'platform', action: 'PlanUpdated', targetType: 'Plan',
    from: '2026-10-15T00:00:00.000Z', to: '2026-10-16T00:00:00.000Z'
  }, 'The filter panel must send every selected filter and an inclusive end date as an exclusive next-day boundary.');
  const auditTable = page.locator('#auditTable');
  await auditTable.getByText('Synthetic Platform Admin').waitFor({ state: 'visible' });
  await auditTable.getByText('PlanUpdated').waitFor({ state: 'visible' });
  assert.equal(await auditTable.locator('tbody tr').count(), 1, 'The combined audit filters must narrow the result to one matching event.');
  await auditTable.getByText('Detayı aç').click();
  await auditTable.getByText('Unique audit filter marker').waitFor({ state: 'visible' });

  await page.locator('#clearAudit').click();
  await page.locator('#auditPage').getByText('Sayfa 1 · 51 kayıt').waitFor({ state: 'visible' });
  assert.deepEqual(auditRequests.at(-1), { page: '1', pageSize: '50' }, 'Clearing audit filters must remove every filter and reset to page one.');
  const nextAuditResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/audit' && requestUrl.searchParams.get('page') === '2';
  });
  await page.locator('#nextAudit').click();
  assert.equal((await nextAuditResponse).status(), 200);
  await page.locator('#auditPage').getByText('Sayfa 2 · 51 kayıt').waitFor({ state: 'visible' });
  assert.equal(auditRequests.at(-1).page, '2', 'Next-page navigation must request the next audit page.');
  assert.equal(await page.locator('#prevAudit').isEnabled(), true, 'Previous-page navigation must become available on page two.');
  const emptyAuditResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/audit' && requestUrl.searchParams.get('q') === 'no matching synthetic event';
  });
  await page.locator('#auditSearch').fill('no matching synthetic event');
  await page.locator('#searchAudit').click();
  assert.equal((await emptyAuditResponse).status(), 200);
  await page.locator('#auditTable').getByText('Filtrelerle eşleşen yönetici işlemi bulunamadı.').waitFor({ state: 'visible' });
  const auditErrorResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/audit' && requestUrl.searchParams.get('q') === 'synthetic audit service failure';
  });
  await page.locator('#auditSearch').fill('synthetic audit service failure');
  await page.locator('#searchAudit').click();
  assert.equal((await auditErrorResponse).status(), 503, 'Audit service failures must be surfaced to the admin.');
  await page.locator('#auditTable').getByText('Synthetic audit service unavailable.').waitFor({ state: 'visible' });
  const auditRetryResponse = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/audit' && requestUrl.searchParams.get('q') === 'synthetic audit service failure' && response.status() === 200;
  });
  await page.locator('#refreshAudit').click();
  assert.equal((await auditRetryResponse).status(), 200, 'Refreshing after an audit error must retry the current filter.');
  await page.locator('#auditTable').getByText('Filtrelerle eşleşen yönetici işlemi bulunamadı.').waitFor({ state: 'visible' });
  assert.equal(await page.locator('#auditSearch').inputValue(), 'synthetic audit service failure', 'Retrying must preserve the selected filter.');
  assert.equal(writes.length, writesBeforeAuditRead, 'Audit filtering, paging, expansion and empty results must be read-only.');

  await page.locator('#nav [data-view="companies"]').click();
  await page.locator('#companiesTable').getByText('Synthetic Demo Company').waitFor({ state: 'visible' });
  const profileRow = page.locator('#companiesTable tr').filter({ hasText: 'Synthetic Demo Company' });
  await profileRow.getByText('100,00').waitFor({ state: 'visible' });
  assert.match(await profileRow.innerText(), /2026[\s\S]*Cloud Pro[\s\S]*100,00/,
    'The company directory must show registration date, current plan, and only the supplied verified MRR.');
  await profileRow.locator('[data-company-logo]').evaluate(image => new Promise((resolve, reject) => {
    if (image.complete && image.naturalWidth > 0) return resolve();
    image.addEventListener('load', resolve, { once: true }); image.addEventListener('error', reject, { once: true });
  }));
  assert.equal(await profileRow.locator('[data-company-logo]').evaluate(image => image.naturalWidth), 1,
    'The company logo must load through the protected company logo endpoint.');
  await page.locator('#companyPlan').locator('option').nth(1).waitFor({ state: 'attached', timeout: 1000 }).catch(() => {});
  const planFilterOptions = await page.locator('#companyPlan option').count();
  const selectedPlanFilter = planFilterOptions > 1 ? await page.locator('#companyPlan option').nth(1).getAttribute('value') : null;
  if (selectedPlanFilter) await page.locator('#companyPlan').selectOption(selectedPlanFilter);
  await page.locator('#companySearch').fill('=2+2');
  await page.locator('#companyActive').selectOption('true');
  await page.locator('#companyCreatedFrom').fill('2026-10-01');
  await page.locator('#companyCreatedTo').fill('2026-10-16');
  await page.locator('#companySort').selectOption('createdAt:desc');
  await page.locator('#companyPageSize').selectOption('100');
  const filteredCompanyList = page.waitForResponse(response => {
    const requestUrl = new URL(response.url());
    return requestUrl.pathname === '/api/platform/companies' && requestUrl.searchParams.get('q') === '=2+2' && requestUrl.searchParams.get('active') === 'true';
  });
  await page.locator('#searchCompanies').click();
  assert.equal((await filteredCompanyList).status(), 200);
  assert.deepEqual(companyListRequests.at(-1), { q: '=2+2', active: 'true', planId: selectedPlanFilter, createdFrom: '2026-10-01', createdTo: '2026-10-16', sortBy: 'createdAt', sortDirection: 'desc', page: '1', pageSize: '100' }, 'The directory request must carry date, sort, and page-size filters.');
  const exportResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/platform/companies/export.csv');
  const exportDownload = page.waitForEvent('download');
  await page.locator('#exportCompanies').click();
  const [csvResponse, download] = await Promise.all([exportResponse, exportDownload]);
  assert.equal(csvResponse.status(), 200);
  assert.equal(download.suggestedFilename(), 'envantertr-firmalar-20261016.csv');
  assert.deepEqual(companyExports.at(-1), { q: '=2+2', active: 'true', planId: selectedPlanFilter, createdFrom: '2026-10-01', createdTo: '2026-10-16', sortBy: 'createdAt', sortDirection: 'desc' }, 'CSV export must preserve the complete selected company-directory filter and sort state.');
  const downloadedCsvPath = await download.path();
  assert.ok(downloadedCsvPath, 'The company export must produce a downloadable file.');
  const downloadedCsv = await readFile(downloadedCsvPath, 'utf8');
  assert.match(downloadedCsv, /"'=2\+2;Injected"/,
    'The delivered spreadsheet row must neutralize formula execution and preserve a semicolon inside the quoted company cell.');

  await page.locator('#companiesTable [data-company-select]').first().check();
  await page.locator('#bulkSuspendCompanies').waitFor({ state: 'visible' });
  await page.locator('#bulkSuspendCompanies').click();
  await page.locator('#actionFields textarea[name=note]').fill('Synthetic bulk suspension reason');
  const bulkStatusResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/platform/companies/bulk-status' && response.request().method() === 'PATCH');
  await page.locator('#actionForm button[type=submit]').click();
  assert.equal((await bulkStatusResponse).status(), 200);
  assert.deepEqual(writes.at(-1), { method: 'PATCH', path: '/api/platform/companies/bulk-status', body: { companyIds: [companyId], isActive: false, note: 'Synthetic bulk suspension reason' }, csrf: csrfToken }, 'Bulk suspension must use the explicitly selected company, required reason, and CSRF token.');

  includeAlternatePlanOption = true;
  await page.locator('#companiesTable [data-company-select]').first().check();
  await page.locator('#bulkChangeCompanyPlan').waitFor({ state: 'visible' });
  await page.locator('#bulkChangeCompanyPlan').click();
  const bulkPlanDialog = page.locator('#actionDialog');
  await bulkPlanDialog.getByText('Bu işlem ödeme almaz; abonelik başlangıç/bitiş tarihini ve ödeme kaydını değiştirmez.').waitFor({ state: 'visible' });
  const writesBeforeBulkPlanCancel = writes.length;
  await bulkPlanDialog.getByRole('button', { name: 'Vazgeç' }).click();
  await bulkPlanDialog.waitFor({ state: 'hidden' });
  assert.equal(writes.length, writesBeforeBulkPlanCancel, 'Canceling a bulk plan change must not write.');
  await page.locator('#bulkChangeCompanyPlan').click();
  const confirmedBulkPlanDialog = page.locator('#actionDialog');
  await confirmedBulkPlanDialog.locator('select[name="planId"]').selectOption(alternatePlanId);
  await confirmedBulkPlanDialog.locator('textarea[name="note"]').fill('Synthetic approved bulk plan change');
  const bulkPlanResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/platform/companies/bulk-plan' && response.request().method() === 'PATCH');
  await confirmedBulkPlanDialog.getByRole('button', { name: 'Seçili planları değiştir' }).click();
  assert.equal((await bulkPlanResponse).status(), 200);
  assert.deepEqual(writes.at(-1), { method: 'PATCH', path: '/api/platform/companies/bulk-plan', body: { companyIds: [companyId], planId: alternatePlanId, note: 'Synthetic approved bulk plan change' }, csrf: csrfToken }, 'The bulk plan action must preserve the exact selected company, plan, reason and CSRF token.');
  await page.locator('#companiesTable tr').filter({ hasText: 'Synthetic Demo Company' }).getByText('Synthetic Alternate Plan').waitFor({ state: 'visible' });

  directoryCompany.subscription.status = 'Canceled';
  await page.locator('#searchCompanies').click();
  await page.locator('#companiesTable [data-company-select]').first().check();
  await page.locator('#bulkChangeCompanyPlan').waitFor({ state: 'hidden' });
  assert.equal(writes.filter(write => write.path === '/api/platform/companies/bulk-plan').length, 1,
    'The bulk plan control must stay unavailable when a selected company has no current Active/Trial subscription.');

  assert.deepEqual(browserErrors, [], `Platform member UI threw: ${browserErrors.join(' | ')}`);
  console.log('PASS: real Chromium account/security/membership/payment timeline; company CSV download with active search/status filters and formula-safe delimited values; demo CRM list/company-search/detail and selected-company fallback stale-response protection plus status, assignment, follow-up, company link, CSRF write and history persistence; member search, cancel-without-write, reauthenticated role update, synthetic invitation, plan create/update/validation/publication reload and maintenance entitlement persistence; audited company subscription plan/date editing with cancel and persistence; platform audit text/source/action/target/date filters, exclusive end-date boundary, clear, paging, event detail, no-match, surfaced 503 and retry behavior without writes.');
} finally {
  await browser?.close();
  await new Promise(resolve => server.close(resolve));
}
