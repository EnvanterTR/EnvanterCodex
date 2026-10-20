import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';

const source = readFileSync('src/EnvanterTR.Web/Data/SoftwareLicenseEndpoints.cs', 'utf8');
const importSource = readFileSync('src/EnvanterTR.Web/Data/SoftwareLicenseImportEndpoints.cs', 'utf8');
const tests = readFileSync('tests/EnvanterTR.SecurityAcceptance/SoftwareLicenseEmployeeScopeAcceptanceTests.cs', 'utf8');

assert.match(source, /GetEmployeeAccessScopeAsync\(db, companyId, id, ct\)/,
  'software license endpoints must resolve the shared location and company-unit employee scope');
assert.match(source, /private static IQueryable<Employee> ApplyEmployeeScope\(IQueryable<Employee> query, LicenseAccessScope scope\)\s*=>\s*scope\.EmployeeAccess\.Apply\(query\)/,
  'license assignment targets must use the shared employee scope rules');
for (const helper of ['ApplyAssignmentLocationScope', 'ApplyEventLocationScope']) {
  const start = source.indexOf(`private static IQueryable<SoftwareLicense${helper === 'ApplyAssignmentLocationScope' ? 'Assignment' : 'Event'}> ${helper}`);
  assert.ok(start >= 0, `${helper} must exist`);
  const next = source.indexOf('\n    private static ', start + 1);
  const body = source.slice(start, next < 0 ? source.length : next);
  assert.match(body, /ApplyEmployeeScope\(db\.Employees\.AsNoTracking\(\)/,
    `${helper} must filter employee-linked rows through the shared employee scope`);
  assert.doesNotMatch(body, /CompanyUnitId\.HasValue/,
    `${helper} must not reject an allowed employee solely because its direct company-unit ID is null`);
}
assert.match(tests, /Software_license_scope_allows_location_inherited_unit_but_rejects_conflicting_employee_unit/,
  'acceptance coverage must prove inherited-unit visibility and conflicting-unit denial');
assert.match(tests, /Assert\.Equal\(HttpStatusCode\.Created, allowed\.StatusCode\)/,
  'acceptance coverage must verify assignment to an in-scope employee');
assert.match(tests, /Assert\.Equal\(HttpStatusCode\.BadRequest, denied\.StatusCode\)/,
  'acceptance coverage must verify assignment to an out-of-scope employee is rejected');
assert.match(importSource, /if\s*\(commit\s*&&\s*\(await CompanyLocationAccess\.GetEmployeeAccessScopeAsync\(db, companyId, actorId, ct\)\)\.IsRestricted\)\s*return Results\.Forbid\(\)/,
  'license imports must reject commits for members with either location or company-unit restrictions');
assert.match(tests, /import\.csv[\s\S]*?Assert\.Equal\(HttpStatusCode\.Forbidden, blockedImport\.StatusCode\)/,
  'acceptance coverage must verify company-unit-restricted license imports are forbidden');

console.log('Software-license visibility, assignment targets, and imports use shared company-unit/location scope; regression coverage is present.');
