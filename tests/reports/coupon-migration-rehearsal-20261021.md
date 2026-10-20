# Coupon migration rehearsal — 21 Oct 2026

## Evidence

- Fresh production PostgreSQL backup `daily-20261020T210608Z.dump`: checksum sidecar verified `OK`; `pg_restore --list` succeeded.
- Restoring that backup to a temporary isolated clone succeeded: 14 MB and 117 migration-history rows through `20261019201743_AddCompanySecurityPolicies`. The temporary production clone was dropped after verification.
- EF Core 10.0.0 generated an idempotent candidate for migration 117 → `20261020174959_AddSubscriptionCoupons`.
- The generated SQL exposed a missing statement terminator in the migration's `SubtotalAmount = Amount` data backfill. The C# migration was corrected and rebuilt; the regenerated SQL now terminates the statement inside the PostgreSQL `DO` block.
- The committed candidate SQL was applied to an isolated clone of the local 117-migration PostgreSQL database, seeded with one historical checkout order (amount 125.50). Result: one row preserved, subtotal backfilled to 125.50, discount 0, pricing mismatches 0, history 118. Applying the same idempotent script a second time succeeded and history stayed at 118.
- Both temporary clones and the candidate file in the database container were removed. The original local database remains at 117 migrations.
- Coolify's interactive DB terminal timed out before the candidate could be run against the restored production-backup clone; that specific combination remains unverified. Production database/application were not changed.

## Candidate

- Source commit: `05e4510` (fix branch; this report will be updated with the rehearsal result commit).
- SQL: `coupon-migration-117-to-118-20261021.sql`.
- No live migration or deployment has been performed.
