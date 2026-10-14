import assert from 'node:assert/strict';
import { readdirSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('../src/EnvanterTR.Web/wwwroot/', import.meta.url));
const files = [];

function visit(directory) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) visit(fullPath);
    else if (/\.(?:html|js)$/i.test(entry.name)) files.push(fullPath);
  }
}

visit(root);
const nativeDialogCalls = [];
const pattern = /(?:\bwindow\s*\.\s*|\bglobalThis\s*\.\s*)?(?:alert|confirm|prompt)\s*\(/g;
for (const file of files) {
  const source = readFileSync(file, 'utf8');
  for (const match of source.matchAll(pattern)) {
    const line = source.slice(0, match.index).split('\n').length;
    nativeDialogCalls.push(`${path.relative(root, file)}:${line}`);
  }
}

assert.deepEqual(nativeDialogCalls, [], `Browser-native alert/confirm/prompt calls block reliable app flows:\n${nativeDialogCalls.join('\n')}`);
assert.ok(files.length > 0, 'No application HTML/JavaScript files were scanned.');
console.log(`Verified ${files.length} application HTML/JavaScript files contain no native alert(), confirm(), or prompt() calls.`);
