import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');
const start = html.indexOf("$('#directoryRenameForm').addEventListener('submit'");
const end = html.indexOf('function customReportPresets()', start);
assert.ok(start >= 0 && end > start, 'Directory rename form handler must exist');
const flow = html.slice(start, end);
assert.match(flow, /if \(merge && !await confirmEmployeeAction\([\s\S]*?currentName[\s\S]*?newName[\s\S]*?confirmLabel: 'Birleştir'/, 'Known duplicate names require clear in-app approval before the first request');
assert.match(flow, /await submitRename\(merge\)[\s\S]*?catch \(error\)[\s\S]*?await confirmEmployeeAction\([\s\S]*?submitRename\(true\)/, 'Server-reported concurrent duplicates need a second explicit approval');
assert.match(flow, /if \(!await confirmEmployeeAction\([\s\S]*?\)\) \{\s*\$\('#directoryRenameFeedback'\)\.textContent = translateUiText\('Birleştirme iptal edildi; kayıtlar değiştirilmedi\.'\);\s*return;/, 'Cancelling server-conflict approval must preserve the records and stop');
assert.match(flow, /\.\/rename|api\(`\/api\/\$\{kind\}\/rename`[\s\S]*?merge: allowMerge/, 'Confirmed merge must retain the existing endpoint and explicit merge flag');
assert.doesNotMatch(flow, /\bconfirm\s*\(/, 'Directory merge must not open a browser-native confirmation');

console.log('Directory known-conflict and server-conflict merge confirmation flows passed.');
