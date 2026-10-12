import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.PublicSite/demo/index.html');
const source = readFileSync(sourcePath, 'utf8');
const layoutStyle = source.match(/<style\s+id="wide-asset-list-layout">([\s\S]*?)<\/style>/)?.[1] || '';
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

const assetHeaders = source.match(/const head=kind==='assets'\?\[([^\]]+)\]/)?.[1] || '';
const assetHeaderLabels = [...assetHeaders.matchAll(/'([^']*)'/g)].map(match => match[1]);
if (assetHeaderLabels.length !== 19 || new Set(assetHeaderLabels).size !== assetHeaderLabels.length || !assetHeaderLabels.includes('Eylemler')) {
  failures.push(`The public demo asset list must keep its 19 distinct shared columns and action column; found ${assetHeaderLabels.length}.`);
}

if (!/<style[^>]*>[\s\S]*?@media\(min-width:701px\)\{\.table-wrap th:last-child,\.table-wrap td:last-child\{position:sticky;right:0;/.test(source)) {
  failures.push('The public demo action column must stay visible at the right edge while the wide asset table scrolls.');
}

if (!layoutStyle.includes('@media(max-width:700px){')
  || !layoutStyle.includes('body{min-width:0}')
  || !layoutStyle.includes('.topbar{display:grid;grid-template-columns:auto minmax(0,1fr);')
  || !layoutStyle.includes('.crumb{min-width:0;overflow:hidden;')
  || !layoutStyle.includes('.top-actions{grid-column:1/-1;min-width:0;width:100%;')) {
  failures.push('The public demo mobile header must wrap without forcing page-level horizontal overflow.');
}

for (const configPath of ['deploy/local/nginx-public.conf', 'deploy/coolify/nginx-public.conf']) {
  const config = readFileSync(resolve(configPath), 'utf8');
  for (const route of ['/demo/', '/demo/index.html']) {
    const routePattern = new RegExp(`location\\s*=\\s*${route.replaceAll('/', '\\/')}\\s*\\{[^}]*expires\\s+-1\\s*;`, 's');
    if (!routePattern.test(config)) {
      failures.push(`${configPath} must revalidate ${route} so the browser doesn't keep stale demo HTML.`);
    }
  }
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified full-width public demo content, 19 distinct asset columns, sticky actions, responsive table overflow, mobile header wrapping, and demo HTML revalidation.');
}
