import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const app = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const acceptance = fs.readFileSync(path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const openHandler = app.slice(app.indexOf('function openSavedAssetSearchDialog'), app.indexOf('function saveCurrentAssetSearch'));
const saveHandler = app.slice(app.indexOf('function saveCurrentAssetSearch'), app.indexOf('function applySavedAssetSearch'));
const deleteHandler = app.slice(app.indexOf('function deleteSelectedAssetSearch'), app.indexOf("$('#closeSavedAssetSearchDialog').addEventListener"));
const formHandler = app.slice(app.indexOf("$('#savedAssetSearchForm').addEventListener('submit'"), app.indexOf('function saveAssetRowDensity'));

requireSource(app.includes('id="savedAssetSearchDialog" aria-labelledby="savedAssetSearchDialogTitle"') &&
  app.includes('id="savedAssetSearchForm"') && app.includes('name="name" maxlength="60"') &&
  app.includes('id="savedAssetSearchFeedback" class="auth-error" role="alert" hidden'),
  'Saved asset searches need an accessible name form and a visible error region.');
requireSource(openHandler.includes('savedAssetSearchDialog.showModal()') && !openHandler.includes('confirm(') && !openHandler.includes('prompt('),
  'Opening a saved-search dialog must not open native dialogs or mutate server data.');
requireSource(saveHandler.includes("openSavedAssetSearchDialog('save', { filters })") && !saveHandler.includes('confirm(') && !saveHandler.includes('prompt('),
  'Saving current filters must gather them and defer the write to explicit dialog submission.');
requireSource(deleteHandler.includes("openSavedAssetSearchDialog('delete', { id, name: item.name })") && !deleteHandler.includes('confirm('),
  'Deleting a saved search must defer the delete until explicit dialog submission.');
requireSource(formHandler.includes("configureSavedAssetSearchDialog('overwrite'") && formHandler.includes("savedAssetSearchDialogMode !== 'overwrite'") &&
  formHandler.includes("api('/api/assets/saved-searches'") && formHandler.includes("method: 'DELETE'") &&
  formHandler.includes('`/api/assets/saved-searches/${encodeURIComponent(savedAssetSearchDraft.id)}`') &&
  formHandler.includes('feedback.hidden = false;') && formHandler.includes('finally { submit.disabled = false; }'),
  'Saved search create/update/delete must use explicit submission, require a second confirmation for replacement, and keep failures visible.');
requireSource(app.includes("savedAssetSearchDialog.addEventListener('click', event => { if (event.target === savedAssetSearchDialog) closeSavedAssetSearchDialog(); })") &&
  app.includes("$('#cancelSavedAssetSearch').addEventListener('click', closeSavedAssetSearchDialog)"),
  'Cancel and backdrop dismissal must close saved-search dialogs without submitting.');
requireSource(acceptance.includes('Saved_asset_searches_are_private_to_user_and_company_and_require_csrf_for_changes'),
  'The saved-search API lacks isolated ownership, company-scope, and CSRF acceptance coverage.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified saved-search create/update/delete dialogs, cancel/backdrop behavior, explicit API submission, overwrite confirmation, and tenant/user/CSRF acceptance coverage.');
}
