import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const navStart = source.indexOf('<nav class="menu"');
const navEnd = navStart < 0 ? -1 : source.indexOf('</nav>', navStart);
const routeStart = source.indexOf("document.querySelectorAll('[data-page]')");
const routeEnd = routeStart < 0 ? -1 : source.indexOf("document.querySelectorAll('#dataImportPanel", routeStart);

if (navStart < 0 || navEnd < 0 || routeStart < 0 || routeEnd < 0) {
  throw new Error(`Could not locate the sidebar or its page routing handler in ${sourcePath}`);
}

const nav = source.slice(navStart, navEnd);
const routeHandler = source.slice(routeStart, routeEnd);
const pageLabels = [...nav.matchAll(/data-page="([^"]+)"/g)].map(match => match[1]);
const groupKeys = [...nav.matchAll(/data-group="([^"]+)"/g)].map(match => match[1]);
const submenuKeys = [...nav.matchAll(/id="sub-([^"]+)"/g)].map(match => match[1]);
const failures = [];

if (pageLabels.length === 0) failures.push('The sidebar has no page destinations.');
for (const label of new Set(pageLabels)) {
  const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  if (!new RegExp(`['"]${escaped}['"]`).test(routeHandler)) {
    failures.push(`Sidebar destination "${label}" has no explicit route or status copy.`);
  }
}

for (const key of groupKeys) {
  if (!submenuKeys.includes(key)) failures.push(`Menu group "${key}" has no matching sub-${key} menu.`);
}
for (const key of submenuKeys) {
  if (!groupKeys.includes(key)) failures.push(`Submenu "sub-${key}" has no matching data-group control.`);
}

const notReadyStart = routeHandler.indexOf('const notReady = {');
const notReadyEnd = notReadyStart < 0 ? -1 : routeHandler.indexOf('\n        };', notReadyStart);
if (notReadyStart < 0 || notReadyEnd < 0) {
  failures.push('The not-ready destination copy map is missing.');
} else {
  const notReady = routeHandler.slice(notReadyStart, notReadyEnd);
  for (const label of ['Destek', 'Entegrasyonlar', 'Şirket birimleri']) {
    if (!new RegExp(`['"]${label}['"]\\s*:\\s*['"]`).test(notReady)) {
      failures.push(`Not-ready menu "${label}" has no explicit explanation.`);
    }
  }
}

if (!source.includes('id="tasksPage"') || !/page === 'Görevler'\) \{[^}]*loadCompanyTasks\(/.test(routeHandler) || !source.includes("api(`/api/tasks?${params}`)")) {
  failures.push('Görevler must have a dedicated screen and load company-scoped task data.');
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${new Set(pageLabels).size} sidebar destinations and ${groupKeys.length} expandable groups; unfinished modules have explicit status copy.`);
}
