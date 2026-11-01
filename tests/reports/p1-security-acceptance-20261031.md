# P1 security regression acceptance — 31 October 2026

## Result

P1-F cross-tenant and P1-G MFA/session/CSRF automated regression coverage passed on commit `ca59fb5` in GitHub Actions run [41199606417](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41199606417). All seven checks succeeded: PostgreSQL tenant shards 0–3, Linux tenant security, Linux PostgreSQL acceptance, and Windows deployment/backup safety. The sharder discovered 433 test cases and all four PostgreSQL shards completed successfully.

The latest full PR run, [41205017241](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41205017241), also completed 7/7 successfully on source commit `8bd0eb020f5aae94a98677ef5b9d4ab9b97a1c02`, including Linux tenant security and PostgreSQL tenant shards 0–3. Commits `be5b387` and `a9ba5f1` after it changed documentation only. This reconfirms automated P1-F/P1-G coverage on the current application source; it still does not replace live Santa/Farma attack acceptance or production disaster recovery.

## P1-F — cross-tenant isolation

The suite includes database query-filter checks (`TenantQueryFilterAcceptanceTests`), HTTP denial of a foreign asset while impersonating (`PlatformImpersonationAcceptanceTests`), host/session continuity, and endpoint tests for foreign asset, employee, attachment, media, export, update, bulk update/delete, and relation IDs throughout `TenantSessionAcceptanceTests` and its partial test files. The acceptance fixture uses synthetic companies and IDs; it does not claim a live Santa/Farma end-to-end exercise.

## P1-G — MFA, sessions, and CSRF

- `SessionDurationAcceptanceTests`: company and platform MFA enrollment/disable/password-change behavior; fresh-device MFA challenges; remember-device behavior; cookie and server session expiration.
- `PlatformLoginLockoutAcceptanceTests`: lockout after five failed password or second-factor attempts and rejection during the lock period.
- `PlatformLoginFreshnessAcceptanceTests`: password-plus-MFA/recovery freshness, consecutive authorized writes, and rejection of platform write authority after a customer-context login.
- `PlatformImpersonationAcceptanceTests`: MFA and recent reauthentication gates, authority revocation, session restoration, CSRF-protected return, missing/invalid/replayed return-secret rejection, and customer-scope preservation.
- API write paths request and supply antiforgery tokens in the relevant endpoint acceptance tests; the successful CI run includes those cases.

## CI correction during acceptance

Run `41197650679` exposed a flaky test assertion: the migration rollback check counted triggers across every PostgreSQL schema while tenant shards execute tests concurrently. It was scoped to the test's generated schema in commit `ca59fb5`. Fresh local PostgreSQL acceptance passed `CurrentReleaseMigrationAcceptanceTests` 1/1 and `CatalogForeignKeyAcceptanceTests` 3/3; the follow-up full CI then passed all seven checks.

## Limits and next step

This proves automated acceptance on the repository source and isolated CI databases, not production/staging runtime, a live cross-company account attack, or disaster recovery. P1-H backup/restore rehearsal and the remaining Hedef work remain open. No production migration, restore, or deployment was performed for this package.
