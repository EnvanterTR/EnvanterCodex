# Hedef6.1 schema field inventory — 29 October2026

Status: **not achieved**. This is a read-only column-presence inventory of the95 data tables in the schema122 stage clone. `__EFMigrationsHistory` was excluded. No migration or application data change was performed.

| Required column | Present | Missing |
|---|---:|---:|
| CompanyId |81|14|
| CreatedAt |59|36|
| UpdatedAt |34|61|
| DeletedAt |3|92|

Only `Assets` and `EmployeeHandoverEmailAttempts` contain all four columns. These counts describe this single requirement; they are not a project completion percentage. Nullability, defaults, historical backfill, update behavior and deletion semantics have not been proved by this presence query.

## Tables without a CompanyId column

AspNetRoleClaims, AspNetRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens, AspNetUsers, Companies, PlatformAdministrators, PlatformAuditRetentionPolicies, PlatformFollowUpEmailDigests, PublicSiteContents, SubscriptionCoupons and SubscriptionPlans.

The list includes shared identity, platform/catalog and tenant-root tables. Their current scope must be preserved while resolving the literal requirement; this report does not mark them as completed exceptions. Adding an arbitrary company identifier would not prove isolation or correct shared-account behavior.

## Evidence

- Saved read-only query runner: `tests/reports/tenant-schema-field-audit-20261029.py`.
- Clone: `envantertr_stage_68f4d88` in the isolated stage database.
- Full95-row CSV: `/var/lib/envantertr-staging/68f4d88-20261029/schema-fields-audit.csv` on the server.
- Summary screenshot: `tenant-schema-field-audit-summary-20261029.png`.
- Missing CompanyId/all-four table screenshot: `tenant-schema-field-audit-tables-20261029.png`.

Next work must map these rows to entity configuration and existing audit/lifecycle behavior, then use additive migration and preservation tests for the actual gaps. The full Hedef remains open.
