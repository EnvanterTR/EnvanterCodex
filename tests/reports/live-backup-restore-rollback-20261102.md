# Live database backup restore and rollback rehearsal — 2 November 2026

## Scope

WP-007 / F1 evidence for the current live-derived release candidate. Source candidate: `1a676b2ae2dce9416fdd918ba8c26472f49ec316`; observed live deployment: `4789b509c0b719c1bf71a0b8032f7224bae9fd43`. No production deployment or production database write was performed.

## Backup verification and isolated restore

- Archive: `daily-20261102T122121Z.dump`, 464,439 bytes (453.55 KB as displayed in Coolify).
- SHA-256 from the Coolify manifest and recomputed locally: `331eb93292dfb97fbb780007a6a39c86e7c25537f109df97d63344dc8911dd55` (match).
- Local `pg_restore --list`: PASS; isolated restore helper: `ISOLATED_COOLIFY_BACKUP_RESTORE_OK`.
- Target: `envantertr-f1-restore-20261102`, PostgreSQL 17, Docker `NetworkMode=none`, zero mounts, zero published ports.
- Restored aggregate baseline: 96 public tables, 122 migration rows, latest `20261026083058_AddHandoverEmailAttempts`, 3 companies, 1 asset.
- The archive stayed outside the repository. Only aggregate counts are recorded here.

## Current-candidate migration and rollback rehearsal

The clean candidate has the same 122 migration IDs as the live database, so no migration is pending. EF generated a 3-byte no-op script from the latest migration to that same migration (SHA-256 `F1945CD6C19E56B3C1C78943EF5EC18116907A4CA1EFC40A57D48AB1DB7ADFC5`). `rehearse-migrations.sh` applied it to a disposable clone of the restored baseline, reapplied it, then recreated a rollback clone from the untouched baseline.

Result: `CLONE_MIGRATION_AND_ROLLBACK_REHEARSAL_OK`. The 122-row migration history and all existing public-table row counts matched across baseline, candidate and rollback clones. The restored source database remained intact; temporary candidate/rollback databases were cleaned by the harness.

## Finding and boundary

Generating a full idempotent SQL script from migration history failed at line 4729 on already-applied migration `20261010215527_AddAssetModelNumbersAndDefaultLocation`: raw `MigrationBuilder.Sql` statements lack terminating semicolons inside generated `DO` blocks. The applied migration was not edited. This did not affect the current candidate's no-pending rehearsal, but full-history idempotent SQL generation must be resolved or explicitly excluded from the deployment path before it is used for a future migration.

The dirty feature branch still has migration/model drift recorded in `docs/HANDOFF.md`; this rehearsal does not authorize applying its migrations. The current production database was not changed.