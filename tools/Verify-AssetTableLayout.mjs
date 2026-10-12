import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const source = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const tableRule = source.match(/\.asset-table\{([^}]+)\}/)?.[1] || '';
const tableWrapRule = source.match(/\.table-wrap\{([^}]+)\}/)?.[1] || '';
const headerRow = source.match(/<table class="asset-table">[\s\S]*?<thead><tr>([\s\S]*?)<\/tr><\/thead>/)?.[1] || '';
const headerCount = [...headerRow.matchAll(/<th\b/g)].length;

if (!tableRule.includes('width:max-content') || !tableRule.includes('min-width:100%') || !tableRule.includes('table-layout:auto')) {
  failures.push('The company asset table must size to its visible columns and fill the available width when narrower.');
}
if (!tableWrapRule.includes('overflow:auto')) {
  failures.push('The company asset table wrapper must provide horizontal scrolling.');
}
if (headerCount !== 20) {
  failures.push(`The company asset table must keep one select column, 18 data columns, and one actions column; found ${headerCount} headers.`);
}

for (let column = 1; column <= 19; column++) {
  const rule = new RegExp(`\\.asset-table th:nth-child\\(${column}\\),\\.asset-table td:nth-child\\(${column}\\)\\{([^}]+)\\}`).exec(source)?.[1] || '';
  if (!/\bwidth:\d+px/.test(rule) || !/\bmin-width:\d+px/.test(rule)) {
    failures.push(`Company asset table column ${column} needs an explicit width and minimum width.`);
  }
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified all 19 company asset data columns have explicit widths, table sizing follows visible columns, and the wrapper can scroll horizontally.');
}
