import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.PublicSite/demo/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];

if (!/<style\s+id="wide-asset-list-layout">[\s\S]*?\.content\{[^}]*width:100%;[^}]*max-width:none;[^}]*margin:0;[^}]*\}/.test(source)) {
  failures.push('The public demo content must use the full workspace width without a max-width cap.');
}

if (!/<style\s+id="wide-asset-list-layout">[\s\S]*?\.table-wrap\{[^}]*max-width:100%;[^}]*overflow:auto;[^}]*overscroll-behavior-x:contain/.test(source)) {
  failures.push('The public demo tables must stay within the workspace and support horizontal scrolling.');
}

if (!source.includes('class="content"') || !source.includes('class="table-wrap"')) {
  failures.push('The public demo must keep its content and responsive table containers.');
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified full-width public demo content and responsive table overflow.');
}
