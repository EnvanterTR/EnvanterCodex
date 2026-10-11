import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const htmlPath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const programPath = resolve(process.argv[3] || 'src/EnvanterTR.Web/Program.cs');
const html = readFileSync(htmlPath, 'utf8');
const program = readFileSync(programPath, 'utf8');
const catalogMatch = html.match(/const companyRoleCatalog\s*=\s*(\[[\s\S]*?\n\s{4}\]);/);

if (!catalogMatch) {
  throw new Error(`Could not locate the company role catalog in ${htmlPath}`);
}

const roles = vm.runInNewContext(`(${catalogMatch[1]})`, {}, { timeout: 1000 });
const roleKeys = new Set(roles.map(role => role.key));
const capabilityPolicies = [
  ['assetsRead', ['asset.read']],
  ['assetsWrite', ['asset.write']],
  ['peopleRead', ['people.read']],
  ['peopleManage', ['people.manage']],
  ['employeeDocuments', ['people.files.read', 'people.files.manage']],
  ['companyAdministration', ['settings.manage']]
];

function policyRoles(policyName) {
  const escapedName = policyName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const policyMatch = program.match(new RegExp(
    `options\\.AddPolicy\\("${escapedName}",\\s*policy\\s*=>\\s*policy\\.RequireAuthenticatedUser\\(\\)\\.RequireClaim\\("company_role",([\\s\\S]*?)\\)\\);`
  ));
  if (!policyMatch) throw new Error(`Could not locate the ${policyName} server policy in ${programPath}`);
  return new Set([...policyMatch[1].matchAll(/"([a-z_]+)"/g)].map(match => match[1]));
}

function assertSameRoles(label, expected, actual) {
  const missing = [...expected].filter(role => !actual.has(role));
  const unexpected = [...actual].filter(role => !expected.has(role));
  if (missing.length || unexpected.length) {
    throw new Error(`${label} mismatch: missing [${missing.join(', ')}], unexpected [${unexpected.join(', ')}]`);
  }
}

const companyMembers = policyRoles('company.member');
assertSameRoles('Company role catalog', companyMembers, roleKeys);

for (const [capability, policyNames] of capabilityPolicies) {
  const expectedSets = policyNames.map(policyRoles);
  const reference = expectedSets[0];
  for (let index = 1; index < expectedSets.length; index++) {
    assertSameRoles(`${policyNames[index]} vs ${policyNames[0]}`, reference, expectedSets[index]);
  }

  const clientRoles = new Set(roles.filter(role => role[capability] === true).map(role => role.key));
  assertSameRoles(`${capability} (${policyNames.join(' & ')})`, reference, clientRoles);
}

console.log(`Verified ${roles.length} built-in company roles against company.member and six server authorization policies.`);
