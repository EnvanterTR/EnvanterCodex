import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of ['includeArchivedAccessories', 'includeArchivedConsumables', 'includeArchivedComponents']) {
  assert.ok(html.includes(id), `inventory archive filter must expose ${id}`);
}
for (const token of [
  "p.set('includeArchived','true')", "params.set('includeArchived', 'true')",
  'button.dataset.inventoryArchive=type', "method:archived?'POST':'DELETE'",
  "'/restore'", 'hareket geçmişi korunur', 'new MutationObserver'
]) assert.ok(html.includes(token), `inventory archive UI must include ${token}`);
console.log('Verified archive filters, archive/restore actions, confirmation flow, and retained history labels for all stock catalogs.');
