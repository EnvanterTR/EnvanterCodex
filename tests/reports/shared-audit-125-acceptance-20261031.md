# Shared audit timestamp migration 125 acceptance — 31 Oct 2026

- Migration: `20261030225548_AddSharedAuditTimestamps` adds 22 nullable columns across shared Identity/platform tables (12 `CreatedAt`, 10 `UpdatedAt`). Existing rows are not backfilled because their historical dates are unknown.
- EF writes are stamped by `AppDbContext`; migration triggers reuse the v123 PostgreSQL function for direct SQL and bulk writes. Inserts fill missing `CreatedAt` and `UpdatedAt`; updates advance `UpdatedAt` only. Down removes all 12 triggers and columns.
- Command: Docker SDK acceptance image; `dotnet test tests/EnvanterTR.SecurityAcceptance/EnvanterTR.SecurityAcceptance.csproj --filter FullyQualifiedName~CurrentReleaseMigrationAcceptanceTests --no-restore --logger 'console;verbosity=normal'` against isolated schema on `envtr-catalog-fk-clean-db-20261031`.
- Result: `Current_release_preserves_all_original_fields_from_118_and_repeats_idempotently` passed (1/1, 34.3064 seconds). It verified original 118-era row/field projection preservation; unknown legacy dates remain null; tracked EF and raw SQL insert/update timestamp behavior; no pending model changes; Down removes triggers/columns; re-upgrade retains null legacy dates.
- Remote CI for parent SHA `50c808a192ecbe3052126e8d400dc06557522882`, run `41140642006`, completed with all 7 jobs successful, including four PostgreSQL tenant shards and Linux/Windows acceptance jobs. This run predates migration 125 and does not validate it.
- Scope: local acceptance only. Migration 125 is not yet pushed, staged, or applied to production; server backup/restore and live acceptance remain open. General `DeletedAt` coverage and P1 F/G/H remain incomplete.

- Follow-up model-wide timestamp coverage: FinalizedTenantModelScopeTests passed 12/12 (12.9663 seconds); every mapped table exposes both audit columns. See 	ests/reports/migration-125-follow-up-20261031.md.
