import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const endpointPath = resolve(process.argv[3] || 'src/EnvanterTR.Web/Data/CompanyRoleEndpoints.cs');
const endpoints = readFileSync(endpointPath, 'utf8');
const requirements = [
  ['Role management page', /id="rolesPage"/],
  ['Create-role control is permission-gated', /createRoleButton\.hidden\s*=\s*!canManageSettings\(\)/],
  ['Role editor dialog', /<dialog[^>]+id="companyRoleDialog"/],
  ['Role deactivation dialog', /<dialog[^>]+id="deactivateCompanyRoleDialog"/],
  ['Create and edit API calls', /api\(roleKey\s*\?\s*`\/api\/company-roles\/\$\{encodeURIComponent\(roleKey\)\}`\s*:\s*'\/api\/company-roles'/],
  ['CSRF-aware app API helper is used', /await api\(roleKey\s*\?/],
  ['Only owner can grant company settings', /checkbox\.disabled\s*=\s*permission\s*===\s*'company\.settings'\s*&&\s*sessionRole\s*!==\s*'owner'/],
  ['Server error is shown in dialog', /companyRoleFeedback'\)\.textContent\s*=\s*translateUiText\(error\.message/],
  ['Assigned role deactivation uses custom dialog', /openDeactivateCompanyRole\(role\)/],
  ['Inactive custom roles can be reactivated', /translateUiText\('Etkinleştir'\)[\s\S]{0,260}isActive:\s*true/],
  ['Role list refreshes after write', /await loadCompanyRoleDefinitions\(\);\s*await renderRoleAccessMatrix\(\);\s*await loadMembers\(\)/],
  ['Role selector uses server-provided assignments', /for\s*\(const role of result\.roles\)/],
  ['Role editor has accessible submit control', /id="saveCompanyRoleButton"\s+type="submit"/]
];
for (const [label, expression] of requirements) {
  if (!expression.test(source))
    throw new Error(`${label} is missing from ${sourcePath}`);
}
if (!/if\s*\(!request\.IsActive\s*&&\s*await db\.CompanyMemberships\.AnyAsync/.test(endpoints))
  throw new Error(`Assigned-role deactivation guard is missing from ${endpointPath}`);
let inlineScripts = 0;
for (const [, attributes, body] of source.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/gi)) {
  if (/\bsrc\s*=/.test(attributes) || /application\/json/i.test(attributes) || !body.trim()) continue;
  new vm.Script(body, { filename: `${sourcePath}#inline-script-${inlineScripts + 1}` });
  inlineScripts++;
}
console.log(`Verified ${requirements.length} custom-role UI and safety hooks plus ${inlineScripts} inline script syntax block(s) in ${sourcePath}.`);
