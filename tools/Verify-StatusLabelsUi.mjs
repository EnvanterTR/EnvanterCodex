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

if (defaultsStart < 0 || helperStart < 0 || loadStart < 0 || formStart < 0 || formEnd < 0) {
  throw new Error(`Could not locate the status label UI state and handlers in ${sourcePath}`);
}

const defaults = source.slice(defaultsStart, source.indexOf(';', defaultsStart));
const helper = source.slice(helperStart, source.indexOf('\n    }', helperStart));
const load = source.slice(loadStart, source.indexOf('\n    }', loadStart));
const form = source.slice(formStart, formEnd);
const statusCount = [...defaults.matchAll(/\b(?:Available|Deployed|Pending|Maintenance|Unusable|Archived):/g)].length;

if (statusCount !== 6) failures.push(`Expected six canonical asset status labels; found ${statusCount}.`);
if (!helper.includes("document.querySelectorAll('option')") || !helper.includes('statusLabel(option.value)'))
  failures.push('The shared option refresh must apply the current label map to status options.');
if (!load.includes('applyAssetStatusLabelsToOptions()'))
  failures.push('Loading saved labels must refresh existing status options.');
if (!form.includes('applyAssetStatusLabelsToOptions()'))
  failures.push('Saving labels must refresh existing status options without a page reload.');
if (!source.includes('input.maxLength = 40; input.required = true;'))
  failures.push('Status label inputs must remain required and capped at 40 characters.');
if (!source.includes("$('#assetStatusLabelsSave').hidden = !canManageSettings();"))
  failures.push('Saving controls must remain restricted to company settings managers.');

if (failures.length) throw new Error(failures.join('\n'));
console.log('Verified six company asset status labels, required length-limited inputs, role-gated saving, and immediate plus reload option refresh.');
