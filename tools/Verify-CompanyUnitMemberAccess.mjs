import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const ui = readFileSync('src/EnvanterTR.Web/wwwroot/index.html', 'utf8');
const access = readFileSync('src/EnvanterTR.Web/Data/CompanyLocationAccess.cs', 'utf8');
const entities = readFileSync('src/EnvanterTR.Web/Data/IdentityEntities.cs', 'utf8');
const db = readFileSync('src/EnvanterTR.Web/Data/AppDbContext.cs', 'utf8');
const memberApi = readFileSync('src/EnvanterTR.Web/Data/OperationalEndpoints.cs', 'utf8');
const browser = readFileSync('deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs', 'utf8');

const contracts = [
  ['Access dialog has a distinct unit scope control', /id="memberCompanyUnitAccessMode"/],
  ['Access dialog renders company units independently', /id="memberCompanyUnitAccessList"/],
  ['Unit choices carry stable identifiers and active state', /data-company-unit-option="true"/],
  ['Client submits both location and unit scopes in one request', /JSON\.stringify\(\{ mode, locationIds, companyUnitMode, companyUnitIds \}\)/],
  ['Client refuses an empty restricted unit scope', /companyUnitMode === 'restricted' && companyUnitIds\.length === 0/],
  ['Access update persists selected company units', /CompanyMembershipCompanyUnitAccesses\.AddRange/],
  ['Unit scope expands only active descendants', /GetEffectiveCompanyUnitIdsAsync[\s\S]*?Where\(x => x\.CompanyId == companyId && x\.IsActive\)[\s\S]*?pending\.TryDequeue[\s\S]*?allowed\.Add\(id\)/],
  ['Unit scope intersects with location restrictions', /allowed\.IntersectWith\(selectedLocationScope\)[\s\S]*?allowed\.IntersectWith\(matchingLocations\)/],
  ['Unit scope is derived through hierarchical locations', /locationParent\.TryGetValue\(cursor, out var parentId\)/],
  ['Member list exposes current unit-access mode', /x\.UserId, x\.RoleKey, x\.CreatedAt, x\.LocationAccessMode, x\.CompanyUnitAccessMode/],
  ['Company-unit catalog is filtered to the signed-in member scope', /GetEffectiveCompanyUnitIdsAsync\([\s\S]*?allowedCompanyUnits == null \|\| allowedCompanyUnits\.Contains\(x\.Id\)/],
  ['Browser acceptance saves and reloads a restricted unit scope', /Chromium member access dialog saves a CSRF-protected company-unit restriction/]
];

for (const [description, pattern] of contracts) {
  const source = description.includes('Company-unit catalog') || description.includes('Member list')
    ? memberApi
    : description.includes('Access update') || description.includes('Unit scope expands') ||
      description.includes('Unit scope intersects') || description.includes('Unit scope is derived')
      ? access
      : description.includes('Browser') ? browser : ui + access + entities + db;
  assert.match(source, pattern, description);
}

assert.match(entities, /class CompanyMembership[\s\S]*?CompanyUnitAccessMode\s*\{\s*get;\s*set;\s*\}\s*=\s*"all"/);
assert.match(entities, /class CompanyMembershipCompanyUnitAccess[\s\S]*?Guid CompanyUnitId/);
assert.match(db, /Entity<CompanyMembershipCompanyUnitAccess>\(\)\.HasKey\(x => new \{ x\.CompanyId, x\.UserId, x\.CompanyUnitId \}\)/);
assert.match(access, /membership\.RoleKey == "owner" && \(mode == CompanyLocationAccess\.Restricted \|\| requestedUnitMode == CompanyLocationAccess\.Restricted\)/);
console.log(`Company-unit membership scope contracts passed (${contracts.length + 4} assertions).`);
