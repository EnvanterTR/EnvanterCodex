import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];
const bodyRule = source.match(/(?:^|})body\{([^}]*)\}/)?.[1] || '';

if (!source.includes('<meta name="viewport" content="width=device-width, initial-scale=1">')) {
  failures.push('The company app must use the device viewport on mobile.');
}
if (/\bmin-width\s*:\s*\d+px/i.test(bodyRule)) {
  failures.push('The document body must not force a minimum width that creates page-level horizontal scrolling.');
}
if (!source.includes('@media(max-width:720px){.app{display:block}') || !source.includes('.main{min-width:0}')) {
  failures.push('The mobile shell must allow its main content to shrink to the viewport.');
}
if (!source.includes('.auth-layer{position:fixed;inset:0;') || !source.includes('.auth-card{width:min(440px,100%)')) {
  failures.push('The sign-in card must fit inside narrow viewports.');
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified the company app shell and sign-in card do not impose a page-level minimum width on mobile.');
}
