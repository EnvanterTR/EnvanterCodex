import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };

const rowBuilder = source.match(/function addAssetRow\(asset\)\s*\{[\s\S]*?(?=\n\s*function |\n\s*async function )/)?.[0] ?? '';
requireSource(rowBuilder.length > 0, 'Asset row builder was not found.');
requireSource(rowBuilder.includes("event.stopPropagation(); callback();"), 'Row action clicks must not also trigger the row detail click.');
requireSource(rowBuilder.includes("row.addEventListener('click', () => openAsset(asset.id)"), 'Clicking a row must open the matching asset.');
requireSource(rowBuilder.includes('selectedAssetIds.size >= 100'), 'Bulk selection must retain the 100-asset safety limit.');

const actions = [
  ['detail', "action(translateUiText('Ayrıntıyı aç'), 'view'", 'openAsset(asset.id)'],
  ['assign-return', "asset.assignedEmployeeId ? 'return' : 'assign'", "$('#returnAssetButton') : $('#assignAssetButton')"],
  ['physical-audit', "action(translateUiText('Fiziksel denetim kaydı aç'), 'audit'", "$('#auditAssetButton').click()"],
  ['print-label', "action(translateUiText('Envanter etiketini yazdır'), 'label'", 'printSingleAssetLabel('],
  ['clone', "action(translateUiText('Varlığı kopyala'), 'clone'", 'openAssetClone(asset)'],
  ['edit', "action(translateUiText('Varlığı düzenle'), 'edit'", "$('#editAssetButton').click()"],
  ['trash', "action(translateUiText('Varlığı arşive taşı'), 'archive'", "const button = $('#trashAssetButton'); if (!button.hidden) button.click()"],
];
for (const [name, ...contracts] of actions) {
  for (const contract of contracts) requireSource(rowBuilder.includes(contract), `Asset row shortcut ${name} is missing source contract: ${contract}.`);
}
requireSource(rowBuilder.includes('if (canWriteAssets()) {'), 'Write-only asset row shortcuts must be gated by the current role.');
requireSource(rowBuilder.includes("checkoutButton.disabled = !canWriteAssets() || asset.status === 'Archived'"), 'Checkout shortcut must respect write permission and archived status.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log(`Verified ${actions.length} asset-row shortcuts: each has a destination, write-only controls are role-gated, and row clicks remain separate from action clicks.`);
}
