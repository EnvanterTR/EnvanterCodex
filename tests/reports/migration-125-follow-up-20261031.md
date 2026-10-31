# Migration 125 follow-up acceptance — 31 Oct 2026

- `FinalizedTenantModelScopeTests.Every_persisted_application_table_has_creation_and_update_timestamps`: 1/1 passed, within suite 12/12 (12.9663 seconds). The assertion visits EF relational table mappings and requires both audit columns on every persisted table; the companion real-model test reports no pending migration changes.
- The first migration-125 CI run, `41196018439`, completed with PostgreSQL shard 3 failing in `CatalogForeignKeyAcceptanceTests` because the test expected no pending migration immediately after validating the 123→124 exact SQL artifact. Migration 125 was correctly pending; no migration data or FK assertion failed.
- Preserved the exact 124 script hash/equality, failure-on-corruption, idempotent double-apply, and FK behavior assertions. Updated the test to assert migration 124 is applied, migrate the isolated schema to latest, then assert no pending migrations/model drift.
- Updated `CatalogForeignKeyAcceptanceTests`: 3/3 passed locally against isolated PostgreSQL 17 (1.2698 minutes). The old full-shard local reproduction was run against the pre-fix image and is not evidence for the fix.
- The correction is local and awaiting a new remote CI run. No staging or production schema changes.
