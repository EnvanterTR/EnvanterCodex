import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const html = await readFile('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const endpoint = await readFile('src/EnvanterTR.Web/Data/AssetModelDetailEndpoints.cs', 'utf8');
const lookupEndpoint = await readFile('src/EnvanterTR.Web/Data/AssetEndpoints.cs', 'utf8');
const acceptance = await readFile('deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs', 'utf8');

for (const id of ['modelDetailsDialog', 'modelDetailsSummary', 'modelDetailsAssets', 'modelDetailsOrders', 'modelDetailsHistory']) {
  assert.ok(html.includes(`id="${id}"`), `Model details dialog must include ${id}.`);
}
assert.match(html, /data-model-details="\$\{item\.id\}"/, 'Each model row must open its detail view.');
assert.match(html, /api\(`\/api\/models\/\$\{modelId\}\/details`\)/, 'The detail dialog must load its model-scoped API resource.');
assert.match(html, /data-model-detail-asset/, 'Related assets must open their asset detail.');
assert.match(endpoint, /Where\(x => x\.CompanyId == companyId && x\.Id == id\)/, 'Model lookup must be company-scoped.');
assert.match(lookupEndpoint, /case "models":[\s\S]{0,500}asset\.DeletedAt == null[\s\S]{0,180}allowedLocations\.Contains/, 'Restricted model lookup must exclude models without active assets in allowed locations.');
assert.match(lookupEndpoint, /"models" => await db\.AssetModels[\s\S]{0,400}allowedLocations\.Contains/, 'Single model lookup must enforce the same location scope.');
assert.match(endpoint, /line\.CompanyId == companyId &&[\s\S]{0,100}line\.ModelId == id/, 'Purchase lines must be company and model scoped.');
assert.match(endpoint, /TargetType == "AssetModel" && entry\.TargetId == id/, 'Model history must be scoped to the model target.');
assert.match(acceptance, /modelDetailsDialog[\s\S]{0,1000}PO-MODEL-001/, 'Chromium acceptance must read the detail summary and linked purchase order.');
assert.match(acceptance, /modelDetailsAssets'\)\.getByRole\('button', \{ name: 'QA-ASSET-001' \}\)/, 'Chromium acceptance must traverse a linked asset into its details.');
console.log('Verified model detail route, company-scoped API, assets/orders/history UI, linked-asset navigation, and Chromium acceptance coverage.');
