import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const start = html.indexOf('async function loadMaintenanceAttachments(');
const end = html.indexOf('function refreshMaintenanceAttachmentTitle()', start);
assert.ok(start >= 0 && end > start, 'maintenance attachment list and actions must be present');

const section = html.slice(start, end);
assert.ok(section.includes("api(`/api/maintenances/${maintenanceAttachmentRecordId}/attachments`)"), 'attachments must load for the selected maintenance record');
assert.ok(section.includes("api(`/api/maintenances/${maintenanceAttachmentRecordId}/attachments/${item.id}/file`)"), 'download action must request the selected attachment file');
assert.ok(section.includes("translateUiText('“{name}” dosyası kaldırılsın mı?').replace('{name}', item.originalFileName)"), 'removal confirmation must identify the selected filename in the active locale');
assert.ok(section.includes("await confirmEmployeeAction({ title: 'İşlemi onayla'"), 'removal must use the accessible in-app confirmation dialog');
assert.ok(section.includes("confirmLabel: 'Kaldır'"), 'confirmation button must identify the removal action');
assert.ok(section.includes("method: 'DELETE'"), 'confirmed removal must call the company-scoped delete endpoint');
assert.ok(section.includes('await loadMaintenanceAttachments(); notify(translateUiText(\'Bakım eki kaldırıldı.\'))'), 'successful removal must refresh the list and announce success');
assert.doesNotMatch(section, /\bwindow\.confirm\s*\(|\bconfirm\s*\(/, 'maintenance attachment actions must not open a browser-native confirmation');

console.log('Verified maintenance attachment listing, selected-file download, localized in-app removal confirmation, delete request, and list refresh.');
