# P0-B live-to-candidate migration comparison — 31 October 2026

## Current live observation

Read-only HTTPS `GET /api/status` at 2026-10-31 12:34 UTC returned `ready`, `migrationCount: 122`, and `pendingMigrationCount: 0` for each of `envantertr.com`, `firma.envantertr.com`, `santa.envantertr.com`, and `farma.envantertr.com`. The current verified live source recorded in `docs/AUDIT-REALITY.md` is `d19cfe549dfab76c2088ed1425d9733456db360e`.

## Exact source-tree comparison

Compared EF migration IDs in the live-source commit tree with the current release candidate tree `ca59fb5869a08d69ab57658e4cca4a406bb95a74`:

| Source tree | Migration IDs |
|---|---:|
| Current live source `d19cfe5` | 122 |
| Candidate `ca59fb5` | 125 |
| Candidate-only IDs | 3 |

Candidate-only migrations:

- `20261030210807_AddMissingCompanyAuditTimestamps`
- `20261030220901_AddCompanyScopedCatalogForeignKeys`
- `20261030225548_AddSharedAuditTimestamps`

Reviewed each migration's `Up()` body in the candidate source. No `DropTable`, `DropColumn`, `DropIndex`, `DropForeignKey`, `DeleteData`, `TRUNCATE`, `DELETE FROM`, or direct `UPDATE` statement was found. The timestamp columns are nullable; existing dates are not backfilled. Migration 124 adds company-scoped restrictive foreign keys and indexes, so it can legitimately reject pre-existing dangling or cross-company catalog references.

## Gate status

P0-B source/count comparison is **partially verified**. The four public health endpoints expose counts, not the full live `__EFMigrationsHistory` ID list. Exact database history comparison and a live read-only catalog-reference preflight are still required through authorized server-side database access. No production database, schema, settings, or release was changed. Do not apply migrations 123–125 until the full live preflight and the separate staging + fresh backup/restore/rollback gates pass.

