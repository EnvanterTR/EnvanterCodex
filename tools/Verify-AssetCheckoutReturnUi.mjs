import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const app = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const acceptance = fs.readFileSync(path.join(root, 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const listReturn = app.slice(app.indexOf('if (isReturns) { const checkin'), app.indexOf('action.append(actions)', app.indexOf('if (isReturns) { const checkin')));
const openReturn = app.slice(app.indexOf('function openAssetReturnAction'), app.indexOf("$('#returnAssetButton').addEventListener('click'"));
const submitReturn = app.slice(app.indexOf("$('#returnAssetForm').addEventListener('submit'"), app.indexOf('const assetTrashConfirmDialog'));

requireSource(app.includes('id="returnAssetDialog" aria-labelledby="returnAssetTitle"') &&
  app.includes('id="returnAssetFeedback" class="auth-error" role="alert" hidden') &&
  app.includes("returnAssetDialog.addEventListener('click', event => { if (event.target === returnAssetDialog) closeAssetReturn(); })"),
  'Asset check-in needs an accessible, dismissible in-app dialog with an error region.');
requireSource(listReturn.includes("checkin.addEventListener('click', () => openAssetReturnAction(item, 'returns'))") &&
  !listReturn.includes('confirm(') && !listReturn.includes('prompt('),
  'The returns-list action must open the in-app form without native browser dialogs or API mutation.');
requireSource(openReturn.includes('if (!asset || !canWriteAssets()') && openReturn.includes('returnAssetDialog.showModal()') &&
  !openReturn.includes('confirm(') && !openReturn.includes('prompt('),
  'Opening an asset return dialog must be permission-gated and free of native browser dialogs.');
requireSource(submitReturn.includes('if (!assetReturnTarget || !canWriteAssets()) return;') &&
  submitReturn.includes('`/api/assets/${target.id}/return`') && submitReturn.includes("method: 'POST'") &&
  submitReturn.includes("target.source === 'returns' ? [loadAssetCheckouts('returns', returnsPage)] : []") &&
  submitReturn.includes('feedback.hidden = false;'),
  'Asset return may write only after explicit submission, refresh its source list on success, and keep failures in the dialog.');
const returnsPage = app.slice(app.indexOf('async function loadAssetCheckouts'), app.indexOf('async function loadAudit('));
requireSource(app.includes('id="returnsState"') && ['overdue', 'due30', 'undated', 'later', 'all'].every(state => app.includes(`value="${state}"`)),
  'The returns queue must offer separate overdue, due, undated, later, and all-assignment filters.');
requireSource(returnsPage.includes('returnsRequestId') && returnsPage.includes('requestId !== (isReturns ? returnsRequestId : assignmentRequestId)') &&
  returnsPage.includes("params.set('q', search)") && returnsPage.includes("pageSize: '50'") &&
  app.includes("$('#returnsPrev').addEventListener('click'") && app.includes("$('#returnsNext').addEventListener('click'"),
  'Returns queue needs search, paging, and protection from stale list responses.');
requireSource(acceptance.includes('Asset_checkout_queue_is_company_scoped_searchable_and_filters_expected_returns') &&
  acceptance.includes('Return due at boundary') && acceptance.includes('state=overdue') && acceptance.includes('state=undated') &&
  acceptance.includes('state=later') && acceptance.includes('page=2&pageSize=2'),
  'Asset check-in lacks isolated API, date-boundary, pagination, and history acceptance coverage.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified asset return actions from both detail and returns list, cancel/backdrop behavior, role-gated POST, refresh, and isolated history acceptance.');
}
