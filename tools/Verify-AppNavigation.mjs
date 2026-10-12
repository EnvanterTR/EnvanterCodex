import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const navStart = source.indexOf('<nav class="menu"');
const navEnd = navStart < 0 ? -1 : source.indexOf('</nav>', navStart);
const routeStart = source.indexOf("document.querySelectorAll('[data-page]')");
const routeEnd = routeStart < 0 ? -1 : source.indexOf("document.querySelectorAll('#dataImportPanel", routeStart);

if (navStart < 0 || navEnd < 0 || routeStart < 0 || routeEnd < 0) {
  throw new Error(`Could not locate the sidebar or its page routing handler in ${sourcePath}`);
}

const nav = source.slice(navStart, navEnd);
const routeHandler = source.slice(routeStart, routeEnd);
const pageLabels = [...nav.matchAll(/data-page="([^"]+)"/g)].map(match => match[1]);
const groupKeys = [...nav.matchAll(/data-group="([^"]+)"/g)].map(match => match[1]);
const submenuKeys = [...nav.matchAll(/id="sub-([^"]+)"/g)].map(match => match[1]);
const failures = [];

const loginForm = source.match(/<form[^>]*id=\"loginForm\"[^>]*>/)?.[0] || '';
if (!/\bmethod=\"post\"/i.test(loginForm)) {
  failures.push('Login forms must use POST so a native browser fallback cannot place credentials in the URL.');
}

if (!source.includes('id="retryApiButton"') || !/function showLogin\([^)]*\)\s*\{[^}]*\$\('#retryApiButton'\)\.hidden\s*=\s*false/.test(source)) {
  failures.push('The login screen must keep the server connection check visible whenever login is shown.');
}
const locationLookupStart = source.indexOf("const locationResult = await api('/api/locations');");
const locationLookupEnd = source.indexOf("fillSelect('categoryId'", locationLookupStart);
const locationLookup = locationLookupStart < 0 || locationLookupEnd < 0 ? '' : source.slice(locationLookupStart, locationLookupEnd);
if (!locationLookup.includes('Array.isArray(locationResult.items)') ||
    !locationLookup.includes('locationResult.items.filter(')) {
  failures.push('Asset lookups must read and validate the items array returned by the locations API envelope.');
}
const assetEndpoints = readFileSync(resolve('src/EnvanterTR.Web/Data/AssetEndpoints.cs'), 'utf8');
const locationsEndpointStart = assetEndpoints.indexOf('api.MapGet("/locations"');
const locationsEndpointEnd = assetEndpoints.indexOf('api.MapPost("/locations/catalog"', locationsEndpointStart);
const locationsEndpoint = locationsEndpointStart < 0 || locationsEndpointEnd < 0 ? '' : assetEndpoints.slice(locationsEndpointStart, locationsEndpointEnd);
if (!locationsEndpoint.includes('Results.Ok(new { items, total = items.Count, departmentCounts })')) {
  failures.push('The locations API must keep its documented items/total/departmentCounts response envelope.');
}
const loginSubmitStart = source.indexOf("$('#loginForm').addEventListener('submit'");
const loginSubmitEnd = source.indexOf("$('#retryApiButton').addEventListener('click'", loginSubmitStart);
const loginSubmitHandler = loginSubmitStart < 0 || loginSubmitEnd < 0 ? '' : source.slice(loginSubmitStart, loginSubmitEnd);
if (!/catch\s*\(error\)\s*\{[\s\S]*?\$\('#retryApiButton'\)\.hidden\s*=\s*false/.test(loginSubmitHandler)) {
  failures.push('The server connection check must remain visible after rejected login credentials.');
}
if (!/else\s*\{\s*\$\('#authError'\)\.textContent\s*=\s*error\.message\s*;\s*void checkLoginConnection\(\)\s*;/.test(loginSubmitHandler) ||
    !/async function checkLoginConnection\(\)[\s\S]*?api\('\/api\/status'[\s\S]*?api\('\/api\/setup\/status'[\s\S]*?status\.hidden\s*=\s*false/.test(source)) {
  failures.push('Rejected login responses must automatically show a separate, live server/setup diagnostic without replacing the credential error.');
}

if (pageLabels.length === 0) failures.push('The sidebar has no page destinations.');
for (const label of new Set(pageLabels)) {
  const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  if (!new RegExp(`['"]${escaped}['"]`).test(routeHandler)) {
    failures.push(`Sidebar destination "${label}" has no explicit route or status copy.`);
  }
}

for (const key of groupKeys) {
  if (!submenuKeys.includes(key)) failures.push(`Menu group "${key}" has no matching sub-${key} menu.`);
}
for (const key of submenuKeys) {
  if (!groupKeys.includes(key)) failures.push(`Submenu "sub-${key}" has no matching data-group control.`);
}

const roleRendererStart = source.indexOf('function renderRoleAccessMatrix()');
const roleRendererEnd = source.indexOf('async function loadTenantBranding()', roleRendererStart);
const roleRenderer = roleRendererStart < 0 || roleRendererEnd < 0 ? '' : source.slice(roleRendererStart, roleRendererEnd);
if (!routeHandler.includes("page === 'Yetki rolleri'") ||
    !source.includes('id="rolesManageMembersButton"') ||
    !roleRenderer.includes('memberManagementButton.hidden = !canManageSettings()') ||
    !roleRenderer.includes("document.querySelector('[data-page=\"Uygulama kullanıcıları\"]')?.click()") ||
    !roleRenderer.includes('for (const role of companyRoleCatalog)')) {
  failures.push('Yetki rolleri must show the built-in access matrix and offer settings managers a direct link to member-role assignment.');
}

const companyUnitsApi = readFileSync(resolve('src/EnvanterTR.Web/Data/OperationalEndpoints.cs'), 'utf8');
const companyUnitsEndpointsStart = companyUnitsApi.indexOf('api.MapGet("/company-units"');
const companyUnitsEndpointsEnd = companyUnitsApi.indexOf('api.MapGet("/departments"', companyUnitsEndpointsStart);
const companyUnitsEndpoints = companyUnitsEndpointsStart < 0 || companyUnitsEndpointsEnd < 0
  ? '' : companyUnitsApi.slice(companyUnitsEndpointsStart, companyUnitsEndpointsEnd);
if (!source.includes('id="companyUnitsPage"') ||
    !/page === 'Şirket birimleri'\) \{[^}]*loadCompanyUnits\(/.test(routeHandler) ||
    !source.includes("api(`/api/company-units${query}`)") ||
    !source.includes("api(id ? `/api/company-units/${id}` : '/api/company-units'") ||
    !source.includes("api(`/api/company-units/${item.id}/active`") ||
    !companyUnitsEndpoints.includes('[FromQuery] bool includeInactive = false') ||
    !companyUnitsEndpoints.includes('RequireAuthorization("people.read")') ||
    !companyUnitsEndpoints.includes('RequireAuthorization("settings.manage")') ||
    !companyUnitsEndpoints.includes('ParentCompanyUnitId')) {
  failures.push('Şirket birimleri must provide tenant-scoped hierarchical list/create/update/archive workflows with read/manage permissions.');
}

for (const selector of [
  ':root[data-theme="dark"] .asset-tabs',
  ':root[data-theme="dark"] .asset-actions',
  ':root[data-theme="dark"] .asset-attachment-upload',
  ':root[data-theme="dark"] .asset-attachment-card',
  ':root[data-theme="dark"] .asset-empty',
  ':root[data-theme="dark"] .asset-attachment-upload input[type="file"]::file-selector-button'
]) {
  if (!source.includes(selector)) failures.push(`Dark theme is missing an asset detail surface style: ${selector}`);
}
if (!source.includes('.asset-attachment-upload{display:flex;align-items:end;gap:10px;flex-wrap:wrap;')) {
  failures.push('Asset detail attachment controls must wrap on narrow dialogs instead of forcing horizontal overflow.');
}
if (!source.includes('@media(max-width:720px){.asset-attachment-upload{align-items:stretch;flex-direction:column}')) {
  failures.push('Asset detail attachment controls must stack on mobile-width dialogs.');
}

if (!source.includes('id="supportPage"') || !/page === 'Destek'\) \{[^}]*loadSupportTickets\(/.test(routeHandler) ||
    !source.includes("api('/api/support/tickets'") || !source.includes("/api/support/tickets/${encodeURIComponent(currentSupportTicketId)}/messages")) {
  failures.push('Destek must expose a company-scoped ticket list, request form, and reply workflow.');
}
const platformAdmin = readFileSync(resolve('src/EnvanterTR.Web/wwwroot/platform-admin.html'), 'utf8');
if (!platformAdmin.includes('data-view="support"') || !platformAdmin.includes("/api/platform/support/tickets") ||
    !platformAdmin.includes('id="supportAdminReply"')) {
  failures.push('Platform administration must expose a support queue and reply/status workflow.');
}

if (!source.includes('id="integrationsPage"') || !/page === 'Entegrasyonlar'[\s\S]*?loadIntegrationTokens\(/.test(routeHandler)) {
  failures.push('Entegrasyonlar must have its dedicated API-key screen and load company-scoped tokens.');
}

if (!source.includes('id="tasksPage"') || !/page === 'Görevler'\) \{[^}]*loadCompanyTasks\(/.test(routeHandler) || !source.includes("api(`/api/tasks?${params}`)")) {
  failures.push('Görevler must have a dedicated screen and load company-scoped task data.');
}

if (!source.includes("bulkEditButton.id = 'bulkEditSelectedAssets'") || !source.includes("bulkEditAssetDialog.id = 'bulkEditAssetDialog'") || !source.includes("api('/api/assets/bulk-update'")) {
  failures.push('Selected assets must expose the bulk-edit dialog and submit to the company-scoped bulk-update API.');
}
for (const field of ['serialNumber', 'notes', 'customFields']) {
  if (!source.includes(`data-bulk-field-enable="${field}"`) &&
      !(field === 'customFields' && source.includes('data-bulk-custom-field-enable'))) {
    failures.push(`Bulk asset editing is missing its ${field} selection control.`);
  }
  if (!source.includes(`fields.includes('${field}')`)) {
    failures.push(`Bulk asset editing does not include ${field} in the submitted payload.`);
  }
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${new Set(pageLabels).size} sidebar destinations and ${groupKeys.length} expandable groups, login diagnostics, asset location and detail theme contracts, and company-unit hierarchy workflows; unfinished modules have explicit status copy.`);
}
