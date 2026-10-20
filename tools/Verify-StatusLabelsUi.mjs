import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];
const defaultsStart = source.indexOf('const defaultAssetStatusLabels =');
const helperStart = source.indexOf('function applyAssetStatusLabelsToOptions()');
const loadStart = source.indexOf('async function loadAssetStatusLabels()');
const formStart = source.indexOf("$('#assetStatusLabelsForm').addEventListener('submit'");
const formEnd = source.indexOf("Object.assign(localeRows", formStart);
const configurationStart = source.indexOf('function renderAssetStatusConfiguration()');
const configurationEnd = source.indexOf("$('#addAssetCustomStatus').addEventListener", configurationStart);
const configSubmitStart = source.indexOf("$('#assetStatusConfigForm').addEventListener('submit'");
const configSubmitEnd = source.indexOf("$('#assetStatusLabelsForm').addEventListener('submit'", configSubmitStart);

if (defaultsStart < 0 || helperStart < 0 || loadStart < 0 || formStart < 0 || formEnd < 0) {
  throw new Error(`Could not locate the status label UI state and handlers in ${sourcePath}`);
}

const defaults = source.slice(defaultsStart, source.indexOf(';', defaultsStart));
const helper = source.slice(helperStart, source.indexOf('\n    }', helperStart));
const load = source.slice(loadStart, source.indexOf('\n    }', loadStart));
const form = source.slice(formStart, formEnd);
const statusCount = [...defaults.matchAll(/\b(?:Available|Deployed|Pending|Maintenance|Unusable|Archived):/g)].length;

if (statusCount !== 6) failures.push(`Expected six canonical asset status labels; found ${statusCount}.`);
if (!helper.includes("select.querySelectorAll('option')") || !helper.includes('statusLabel(option.value)'))
  failures.push('The shared option refresh must apply the current label map to status options.');
if (!load.includes('applyAssetStatusLabelsToOptions()'))
  failures.push('Loading saved labels must refresh existing status options.');
if (!form.includes('applyAssetStatusLabelsToOptions()'))
  failures.push('Saving labels must refresh existing status options without a page reload.');
if (!load.includes("api('/api/assets/status-config')") || !load.includes('renderAssetStatusConfiguration()'))
  failures.push('Loading status settings must fetch and render the company transition configuration.');
if (configurationStart < 0 || configurationEnd < 0 || configSubmitStart < 0 || configSubmitEnd < 0)
  failures.push('Custom status and transition matrix handlers must be present.');
const configuration = source.slice(configurationStart, configurationEnd);
const configSubmit = source.slice(configSubmitStart, configSubmitEnd);
if (!configuration.includes('dataset.transitionFrom') || !source.includes("['Available','Pending','Maintenance','Unusable'].includes(item.status)"))
  failures.push('The transition matrix must expose explicit rules and keep archive restore protected.');
if (!source.includes('customStatuses.length>=8') || !configuration.includes('dataset.customStatusLabel'))
  failures.push('Custom status editing must enforce the eight-status UI limit and expose editable labels.');
if (!configSubmit.includes("api('/api/assets/status-config'"))
  failures.push('Saving custom statuses must persist through the company configuration endpoint.');
if (!configSubmit.includes("api('/api/assets/status-labels')"))
  failures.push('Saving custom statuses must refresh status options and labels.');
if (!source.includes("option.dataset.customStatus='true'"))
  failures.push('Custom statuses must be available in status filters, bulk actions, reports, and asset detail actions.');
if (!source.includes('input.maxLength = 40; input.required = true;'))
  failures.push('Status label inputs must remain required and capped at 40 characters.');
if (!source.includes("$('#assetStatusLabelsSave').hidden = !canManageSettings();"))
  failures.push('Saving controls must remain restricted to company settings managers.');

if (failures.length) throw new Error(failures.join('\n'));
console.log('Verified built-in labels, company custom statuses, transition matrix, protected archive workflow, role-gated saves, and refreshed status selectors.');
