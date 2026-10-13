import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };
const rowStart = source.search(/function addAssetRow\(asset(?:,\s*rowNumber)?\)/);
const rowEnd = source.indexOf('function updateAssetRelationFilter()', rowStart);
const row = rowStart < 0 || rowEnd < 0 ? '' : source.slice(rowStart, rowEnd);
const menuStart = source.indexOf('function closeAssetContextMenu(');
const menuEnd = source.search(/function addAssetRow\(asset(?:,\s*rowNumber)?\)/);
const menu = menuStart < 0 || menuEnd < 0 ? '' : source.slice(menuStart, menuEnd);
const trashButtonStart = source.indexOf("$('#trashAssetButton').addEventListener('click'");
const statusActionStart = source.indexOf("$('#assetStatusAction').addEventListener('change'", trashButtonStart);
const trashButtonHandler = trashButtonStart < 0 || statusActionStart < 0 ? '' : source.slice(trashButtonStart, statusActionStart);

requireSource((source.match(/id="assetContextMenu"/g) || []).length === 1 &&
  source.includes('id="assetContextMenu" class="asset-context-menu" role="menu"') &&
  source.includes("assetContextMenu.setAttribute('aria-label', translateUiText('Varlık hızlı işlemleri'))"),
  'The asset context menu must exist once and expose a localized accessible menu label.');
requireSource(row.includes("row.addEventListener('contextmenu', event => openAssetContextMenu(row, event))") &&
  menu.includes('event.preventDefault()') && menu.includes("row.querySelectorAll('.asset-icon-action[data-asset-action]')"),
  'Right-clicking an asset row must open a context menu from that row’s actual available actions.');
requireSource(row.includes('row.tabIndex = 0') && row.includes("row.setAttribute('aria-haspopup', 'menu')") &&
  row.includes("event.key === 'ContextMenu'") && row.includes("event.key === 'F10'"),
  'The asset context menu must also be reachable from a focused row by keyboard.');
requireSource(row.includes('button.dataset.assetAction = icon') &&
  row.includes("'Çalışana zimmetle'") && row.includes("'Zimmeti iade al'") &&
  row.includes("'Varlığı kopyala'") && row.includes("'Varlığı düzenle'") &&
  row.includes("'Varlığı arşive taşı'") && row.includes('if (canWriteAssets())'),
  'Assign/return, clone, edit, and recoverable archive actions must reuse the existing permission-gated row handlers.');
requireSource(source.includes('id="assetTrashConfirmDialog"') &&
  trashButtonHandler.includes("openAssetTrashAction(currentAsset, 'trash')") &&
  !trashButtonHandler.includes('confirm(') && !trashButtonHandler.includes('prompt('),
  'The context-menu archive action must reach the in-app confirmation dialog, without a native browser dialog.');
requireSource(menu.includes('source.click()') && menu.includes('closeAssetContextMenu();') &&
  menu.includes('Math.min(point.x, window.innerWidth - bounds.width - 8)') &&
  menu.includes('Math.min(point.y, window.innerHeight - bounds.height - 8)'),
  'Selecting a context action must invoke the existing handler and keep the popup within the viewport.');
requireSource(menu.includes("assetContextMenu.addEventListener('keydown'") &&
  menu.includes("event.key === 'Escape'") && menu.includes("event.key === 'ArrowDown'") &&
  menu.includes("event.key === 'ArrowUp'") && menu.includes("event.key === 'Home'") && menu.includes("event.key === 'End'"),
  'The context menu must support Escape and standard menu keyboard navigation.');
requireSource(menu.includes("document.addEventListener('pointerdown'") &&
  menu.includes('!assetContextMenu.contains(event.target)') &&
  menu.includes("window.addEventListener('resize'") && menu.includes("window.addEventListener('scroll'"),
  'Outside click, scrolling, and resizing must dismiss the context menu.');
requireSource(source.includes("'Varlık hızlı işlemleri':['Asset quick actions','Быстрые действия с активом','Швидкі дії з активом','Брзи дејства за средството']"),
  'The context menu label must be localized in all currently supported application languages.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified asset row right-click and keyboard context menu, permission-gated existing actions, localized labels, viewport positioning, and dismissal/navigation behavior.');
}
