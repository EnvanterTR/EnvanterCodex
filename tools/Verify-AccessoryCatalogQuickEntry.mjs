import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');

function section(start, end) {
  const from = html.indexOf(start);
  assert.notEqual(from, -1, `Missing source marker: ${start}`);
  const to = html.indexOf(end, from + start.length);
  assert.notEqual(to, -1, `Missing source section end: ${start}`);
  return html.slice(from, to);
}

const dialog = section('<dialog class="detail-dialog" id="textEntryDialog"', '<dialog class="detail-dialog employee-detail-dialog"');
assert.match(dialog, /<form id="textEntryForm">/, 'Text entry must submit through a form');
assert.match(dialog, /id="textEntryValue"[^>]*required[^>]*maxlength="120"/, 'Text entry must have a required, bounded input');
assert.match(dialog, /data-close-text-entry/, 'Text entry must expose explicit cancel/close controls');

const helper = section('let textEntryResolver = null', "$('#employeeActionForm').addEventListener('submit'");
for (const [pattern, message] of [
  [/dialog\.showModal\(\)/, 'Use an in-app modal'],
  [/input\.value\.trim\(\)/, 'Trim user input'],
  [/input\.setCustomValidity\(/, 'Reject whitespace-only input'],
  [/input\.reportValidity\(\)/, 'Present native accessible field validation'],
  [/Math\.min\(120, maxLength\)/, 'Enforce the backend-compatible maximum'],
  [/settleTextEntry\(null\)/, 'Resolve cancellation instead of leaving a pending promise'],
  [/addEventListener\('cancel'/, 'Support Escape cancellation'],
  [/textEntryProgrammaticCloseCount/, 'Distinguish programmatic close from user cancellation']
]) assert.match(helper, pattern, message);

const flows = [
  ["$('#accessoryQuickBrand').addEventListener('click'", "async function loadAccessoryCategories()", 'accessory quick brand'],
  ["row.querySelector('.accessory-category-rename').addEventListener('click'", "row.querySelector('.accessory-category-archive').addEventListener('click'", 'accessory category rename'],
  ['async function createAccessoryCategory(', "$('#manageAccessoryCategories').addEventListener('click'", 'accessory category creation'],
  ["$('#accessoryQuickModel').addEventListener('click'", "$('#accessorySearch').addEventListener('input'", 'accessory quick model'],
  ["$('#catalogQuickBrand').addEventListener('click'", "$('#catalogRows').addEventListener('click'", 'catalog quick brand']
];

for (const [start, end, label] of flows) {
  const code = section(start, end);
  assert.match(code, /requestTextValue\(/, `${label}: use the in-app text-entry dialog`);
  assert.match(code, /maxLength:\s*120/, `${label}: use the supported name length`);
  assert.match(code, /if \(name === null\) return|if \(name === null \|\| name === category\.name\) return/, `${label}: cancellation must stop before writing`);
  assert.doesNotMatch(code, /\bprompt\s*\(/, `${label}: do not use a browser prompt`);
}

const rename = section("row.querySelector('.accessory-category-rename').addEventListener('click'", "row.querySelector('.accessory-category-archive').addEventListener('click'");
assert.match(rename, /initialValue:\s*category\.name/, 'Category rename should preload the current name');
assert.match(rename, /method:\s*'PATCH'/, 'Category rename should persist via the update endpoint');
const categoryCreate = section('async function createAccessoryCategory(', "$('#manageAccessoryCategories').addEventListener('click'");
assert.match(categoryCreate, /method:\s*'POST'/, 'Category creation should persist via the create endpoint');
assert.match(categoryCreate, /if \(selectAfter\)[\s\S]*?\.value\s*=\s*category\.id/, 'Quick category creation should select the newly created category');
const modelCreate = section("$('#accessoryQuickModel').addEventListener('click'", "$('#accessorySearch').addEventListener('input'");
assert.match(modelCreate, /if \(!brandId\)[\s\S]*?return/, 'Model creation must require a selected brand');
assert.match(modelCreate, /brandId\s*\}\)\s*\}\);/, 'Model creation must remain scoped to the selected brand');

console.log('Accessory and catalog quick-entry dialog contracts passed.');
