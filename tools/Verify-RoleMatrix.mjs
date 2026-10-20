import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const htmlPath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const programPath = resolve(process.argv[3] || 'src/EnvanterTR.Web/Program.cs');
const authPath = resolve(process.argv[4] || 'src/EnvanterTR.Web/Data/CompanyRoleAuthorization.cs');
const html = readFileSync(htmlPath, 'utf8');
const program = readFileSync(programPath, 'utf8');
const authorization = readFileSync(authPath, 'utf8');
const catalogMatch = html.match(/let companyRoleCatalog\s*=\s*(\[[\s\S]*?\n\s{4}\]);/);
if (!catalogMatch) throw new Error(`Could not locate the company role catalog in ${htmlPath}`);
const uiRoles = vm.runInNewContext(`(${catalogMatch[1]})`, {}, { timeout: 1000 });

const constants = Object.fromEntries([...authorization.matchAll(/public const string (\w+) = "([^"]+)";/g)]
  .map(([, name, value]) => [name, value]));
const builtInBlock = authorization.match(/private static readonly CompanyRoleDefinitionDto\[\] BuiltInRoles\s*=\s*\[([\s\S]*?)\n\s*\];/);
if (!builtInBlock) throw new Error(`Could not locate server built-in role definitions in ${authPath}`);
const serverRoles = [...builtInBlock[1].matchAll(/Role\("([^"]+)",\s*"[^"]+"(?:,\s*([^\n)]*))?\)/g)]
  .map(([, key, permissionList]) => ({ key, permissions: (permissionList || '').split(',').map(value => constants[value.trim()]).filter(Boolean) }));
if (serverRoles.length !== 7) throw new Error(`Expected 7 server built-in roles, found ${serverRoles.length}`);

const capabilityPermissions = {
  assetsRead: ['assets.read'], assetsWrite: ['assets.write'], peopleRead: ['people.read'],
  peopleManage: ['people.manage'], employeeDocuments: ['people.documents'], companyAdministration: ['company.settings']
};
const policyPermissions = {
  'asset.read': ['assets.read'], 'asset.write': ['assets.write'], 'settings.manage': ['company.settings'],
  'people.read': ['people.read'], 'people.manage': ['people.manage'],
  'people.files.read': ['people.documents'], 'people.files.manage': ['people.documents']
};
function assertSame(label, expected, actual) {
  const missing = [...expected].filter(value => !actual.has(value));
  const unexpected = [...actual].filter(value => !expected.has(value));
  if (missing.length || unexpected.length)
    throw new Error(`${label} mismatch: missing [${missing.join(', ')}], unexpected [${unexpected.join(', ')}]`);
}

assertSame('UI/server built-in role keys', new Set(serverRoles.map(role => role.key)), new Set(uiRoles.map(role => role.key)));
for (const serverRole of serverRoles) {
  const uiRole = uiRoles.find(role => role.key === serverRole.key);
  for (const [capability, permissions] of Object.entries(capabilityPermissions)) {
    const serverAllows = permissions.every(permission => serverRole.permissions.includes(permission));
    if (uiRole[capability] !== serverAllows)
      throw new Error(`${serverRole.key}.${capability} differs between UI and server authorization.`);
  }
}

for (const [policyName, permissions] of Object.entries(policyPermissions)) {
  const escaped = policyName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const policy = program.match(new RegExp(`options\\.AddPolicy\\("${escaped}",\\s*policy\\s*=>\\s*policy\\.RequireAuthenticatedUser\\(\\)\\.RequireClaim\\("company_permission",\\s*"([^"]+)"\\)\\);`));
  if (!policy) throw new Error(`Policy ${policyName} must require a company_permission claim.`);
  const actualPermission = policy[1];
  if (actualPermission !== policyName)
    throw new Error(`Policy ${policyName} requires ${actualPermission} instead of its matching permission claim.`);
  const expectedPermission = permissions.length === 1 ? permissions[0] : null;
  if (expectedPermission) {
    const permissionConstant = Object.entries(constants).find(([, value]) => value === expectedPermission)?.[0];
    const policyClaimMapping = policyName.startsWith('people.files.')
      ? /case\s+EmployeeDocuments:\s*claims\.Add\("people\.files\.read"\);\s*claims\.Add\("people\.files\.manage"\);/.test(authorization)
      : new RegExp(`case\\s+${permissionConstant}:\\s*claims\\.Add\\("${policyName}"\\);`).test(authorization);
    if (!policyClaimMapping) throw new Error(`Server role resolver does not map ${expectedPermission} to ${policyName}.`);
  } else if (actualPermission !== 'people.files.read' && actualPermission !== 'people.files.manage') {
    throw new Error(`${policyName} has unexpected permission claim ${actualPermission}.`);
  }
  const expectedRoles = new Set(serverRoles.filter(role => permissions.every(permission => role.permissions.includes(permission))).map(role => role.key));
  const actualRoles = new Set(serverRoles.filter(role => role.permissions.some(permission => permissions.includes(permission))).map(role => role.key));
  assertSame(`${policyName} server role coverage`, expectedRoles, actualRoles);
}

if (!program.includes('RequireClaim("company_member", "true")') || !program.includes('identity.AddClaim(new Claim("company_member", "true"))'))
  throw new Error('Company membership policy is not backed by live membership resolution.');
if (!authorization.includes('role.CompanyId == companyId && role.RoleKey == roleKey && role.IsActive'))
  throw new Error('Custom roles must resolve within the active tenant only.');
console.log(`Verified ${serverRoles.length} built-in roles, six UI capabilities, tenant-scoped custom-role resolution, and seven server permission policies.`);
