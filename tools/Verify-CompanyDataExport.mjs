import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const endpoint = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/Data/DataExportEndpoints.cs'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const apiStart = source.indexOf('async function api(path, options = {})');
const apiEnd = source.indexOf('async function refreshCsrf()', apiStart);
const api = apiStart >= 0 && apiEnd > apiStart ? source.slice(apiStart, apiEnd) : '';
const handlerStart = source.indexOf('$("#exportBackupButton").addEventListener("click"');
const handlerEnd = source.indexOf("const importPreviewDialog", handlerStart);
const handler = handlerStart >= 0 && handlerEnd > handlerStart ? source.slice(handlerStart, handlerEnd) : '';
const archiveHandlerStart = source.indexOf("$('#dataDownloadCompanyArchive').addEventListener('click'");
const archiveHandlerEnd = source.indexOf("$('#dataDownloadAssets').addEventListener", archiveHandlerStart);
const archiveHandler = archiveHandlerStart >= 0 && archiveHandlerEnd > archiveHandlerStart ? source.slice(archiveHandlerStart, archiveHandlerEnd) : '';
const endpointStart = endpoint.indexOf('endpoints.MapGet("/api/company/export.json", ExportCompanyAsync)');
const endpointEnd = endpoint.indexOf('endpoints.MapGet("/api/company/export.zip"', endpointStart);
const route = endpointStart >= 0 && endpointEnd > endpointStart ? endpoint.slice(endpointStart, endpointEnd) : '';
const archiveRouteStart = endpoint.indexOf('endpoints.MapGet("/api/company/export.zip", ExportCompanyArchiveAsync)');
const archiveRouteEnd = endpoint.indexOf('endpoints.MapPost("/api/company/import/preview"', archiveRouteStart);
const archiveRoute = archiveRouteStart >= 0 && archiveRouteEnd > archiveRouteStart ? endpoint.slice(archiveRouteStart, archiveRouteEnd) : '';
const exportStart = endpoint.indexOf('private static async Task<IResult> ExportCompanyAsync(');
const exportEnd = endpoint.indexOf('private static async Task<IResult> ExportCompanyArchiveAsync(', exportStart);
const implementation = exportStart >= 0 && exportEnd > exportStart ? endpoint.slice(exportStart, exportEnd) : '';
const archiveStart = exportEnd;
const archiveEnd = endpoint.indexOf('private static async Task WriteCompanyJsonAsync(', archiveStart);
const archive = archiveStart >= 0 && archiveEnd > archiveStart ? endpoint.slice(archiveStart, archiveEnd) : '';

const apiErrorBranch = api.indexOf('if (!response.ok)');
const rawResponseBranch = api.indexOf("if (options.responseType === 'raw') return scopedResponse();");
requireSource(api.includes('responseType: _responseType') && apiErrorBranch >= 0 && rawResponseBranch > apiErrorBranch, 'The API helper must return raw responses only after its shared HTTP/auth error handling and must not forward the custom option to fetch.');
const bodyScope = api.slice(api.indexOf('const scopedResponse ='), rawResponseBranch);
requireSource(bodyScope.includes("['blob', 'text', 'arrayBuffer', 'formData', 'json']") &&
  /ensureCurrentSession\(\);\s*const body = await read\.apply\(response, args\);\s*ensureCurrentSession\(\);/.test(bodyScope),
  'Raw export body readers must retain their native response receiver and check the session before and after asynchronous body consumption.');
requireSource(handler.includes('api("/api/company/export.json", { responseType: "raw" })') && handler.includes('response.blob()'), 'Company JSON export must request the raw response before creating its download blob.');
requireSource(handler.includes('link.download = `envantertr-sirket-verisi-') && handler.includes('URL.revokeObjectURL(url)'), 'Company JSON export must download a named file and release its temporary object URL.');
requireSource(route.includes('.RequireAuthorization("settings.manage")'), 'Company data export must stay restricted to settings managers.');
requireSource(implementation.includes('ContentDisposition') && implementation.includes('application/json'), 'Company data export must return an attachment with the JSON content type.');
requireSource(implementation.includes('RecordCompanyExportAsync(db, companyId, userId, "company.export.json"') && implementation.includes('transaction.CommitAsync(cancellationToken)'), 'A completed JSON export must be recorded in the tenant audit log and committed only after streaming succeeds.');
requireSource(archiveRoute.includes('.RequireAuthorization("settings.manage")'), 'Company media archive must stay restricted to settings managers.');
requireSource(archiveHandler.includes("api('/api/company/export.zip')") && archiveHandler.includes('response.blob()'), 'Company archive export must fetch the ZIP response and create a download blob.');
requireSource(archiveHandler.includes('link.download = `envantertr-sirket-arsivi-') && archiveHandler.includes('URL.revokeObjectURL(url)'), 'Company archive export must download a named file and release its temporary object URL.');
requireSource(archive.includes('application/zip') && archive.includes('manifest.json') && archive.includes('README.txt'), 'Company archive must include a manifest and scope README in the ZIP.');
requireSource(archive.includes('RepeatableRead') && archive.includes('DeleteOnClose') && archive.includes('finally'), 'Company archive must use a consistent read transaction and clean up its temporary ZIP.');
requireSource(archive.includes('RecordCompanyExportAsync(db, companyId, userId, "company.export.zip"') && archive.includes('transaction.CommitAsync(cancellationToken)'), 'A completed ZIP export must be recorded in the tenant audit log and committed only after streaming succeeds.');
requireSource(archive.includes('files.Count > 100_000') && archive.includes('4L * 1024 * 1024 * 1024'), 'Company archive must enforce its file count and total size limits.');
requireSource(source.includes("const dataPages = ['Tablodan içe aktar','Verileri dışa aktar','Dışa aktarımlar','Yedekleme geçmişi','Geri yükle']") && source.includes("!canManageSettings()"), 'Exports menu must only open for settings managers.');
requireSource(source.includes("$('#dataDownloadCompany').addEventListener('click', () => $('#exportBackupButton').click())"), 'Data tools JSON download shortcut must invoke the shared export action.');
requireSource(endpoint.includes('TargetType = "CompanyDataExport"') && endpoint.includes('Action = action'), 'Company exports must record the responsible user, company scope, and export format in audit entries.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified company JSON export raw-response handling, attachment download contract, temporary URL cleanup, and settings authorization.');
}
