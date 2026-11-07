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
assert.match(html, /data-model-eol="\$\{item\.id\}"/, 'Model catalog rows must expose an editable EOL date to authorized managers.');
assert.match(html, /name="endOfLifeOn" type="date"/, 'Model creation must accept an EOL date.');
assert.match(html, /name="depreciationAnnualRatePercent"/, 'Model creation must accept an annual depreciation percentage.');
assert.match(html, /data-dep-annual-rate="\$\{item\.id\}"/, 'Authorized model managers must be able to edit the annual depreciation percentage.');
assert.match(html, /annualRatePercent: annualRate \? Number\(annualRate\) : null/, 'Model policy updates must persist the annual depreciation percentage.');
assert.match(html, /depreciationAnnualRatePercent: catalogKind === 'models' && data\.get\('depreciationAnnualRatePercent'\)/, 'Model creation must send the annual depreciation percentage to the API.');
assert.match(html, /function syncDepreciationMethod\(rateInput, methodSelect\)[\s\S]{0,260}option\.disabled = hasAnnualRate && option\.value !== 'linear'/, 'An annual rate must lock the UI to the supported straight-line method.');
assert.match(html, /syncDepreciationMethod\(tr\.querySelector\('\[data-dep-annual-rate\]'\), tr\.querySelector\('\[data-dep-method\]'\)\)/, 'Existing model rows must disable unsupported half-year methods when an annual rate is configured.');
assert.match(html, /annualDepreciationRatePercent/, 'The depreciation report must display the effective annual depreciation percentage.');
assert.match(html, /<th>Yıllık amortisman \(%\)<\/th>/, 'The depreciation report must label the annual percentage column.');
assert.match(html, /"Yıllık amortisman oranı \(%\)":\["Annual depreciation rate \(%\)"[^\]]+\]/, 'The annual-rate input must be translated in all supported locales.');
assert.match(html, /"Yıllık amortisman \(%\)":\["Annual depreciation \(%\)"[^\]]+\]/, 'The report column must be translated in all supported locales.');
assert.match(html, /"Ay veya yıllık oran yöntemlerinden birini girin\. Oran doğrusal yöntemde uygulanır\.":\["Enter either a useful life in months or an annual rate\.[^\]]+\]/, 'The policy guidance must be translated in all supported locales.');
assert.match(html, /"yıllık":\["per year"[^\]]+\]/, 'The annual-rate suffix must be translated in all supported locales.');
assert.match(html, /"ay":\["months"[^\]]+\]/, 'The derived month suffix must be translated in all supported locales.');
assert.match(html, /Ömür sonu \(EOL\)/, 'Model detail and catalog views must label the EOL date.');
assert.match(html, /data-model-detail-asset/, 'Related assets must open their asset detail.');
assert.match(endpoint, /Where\(x => x\.CompanyId == companyId && x\.Id == id\)/, 'Model lookup must be company-scoped.');
assert.match(endpoint, /x\.EndOfLifeOn/, 'The company-scoped model detail API must return EOL.');
assert.match(lookupEndpoint, /x\.EndOfLifeOn/, 'Company-scoped model catalog APIs must return EOL.');
assert.match(lookupEndpoint, /case "models":[\s\S]{0,500}asset\.DeletedAt == null[\s\S]{0,180}allowedLocations\.Contains/, 'Restricted model lookup must exclude models without active assets in allowed locations.');
assert.match(lookupEndpoint, /"models" => await db\.AssetModels[\s\S]{0,400}allowedLocations\.Contains/, 'Single model lookup must enforce the same location scope.');
assert.match(endpoint, /line\.CompanyId == companyId &&[\s\S]{0,100}line\.ModelId == id/, 'Purchase lines must be company and model scoped.');
assert.match(endpoint, /TargetType == "AssetModel" && entry\.TargetId == id/, 'Model history must be scoped to the model target.');
assert.match(acceptance, /modelDetailsSummary'\)\.innerText\(\), \/MD-TEST\//, 'Chromium acceptance must read the model detail summary.');
assert.match(acceptance, /Ömür sonu \\\(EOL\\\)/, 'Chromium acceptance must read the EOL field.');
assert.match(acceptance, /30\\\.06\\\.2030/, 'Chromium acceptance must display the stored EOL date.');
assert.match(acceptance, /modelDetailsOrders'\)\.innerText\(\), \/PO-MODEL-001\//, 'Chromium acceptance must read the linked purchase order.');
assert.match(acceptance, /modelDetailsAssets'\)\.getByRole\('button', \{ name: 'QA-ASSET-001' \}\)/, 'Chromium acceptance must traverse a linked asset into its details.');
console.log('Verified model detail route, annual depreciation create/edit/report UI, five-locale labels, company-scoped API, and Chromium acceptance coverage.');
