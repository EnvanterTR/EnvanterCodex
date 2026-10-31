# P0-B exact migration history preflight — 31 October 2026

## Result

The candidate snapshot contains 125 complete EF migration IDs. `deploy/testing/production-migration-history-preflight.sql` compares that exact set to the live `__EFMigrationsHistory` table inside an explicit repeatable-read, read-only transaction. It reports both missing and unexpected IDs, counts, first/last IDs, and a stable ordered-ID MD5; it ends with `ROLLBACK` and does not read business rows.

## Local acceptance

- PostgreSQL 17 ran in a new Compose project on its internal-only network; the temporary project and volume were removed after the run.
- Synthetic history with the first 122 candidate IDs reported 125 candidate / 122 live / 3 missing / 0 unexpected. The three reported IDs were migrations 123–125.
- A synthetic unexpected live ID was then added; the preflight reported it as `live_not_in_candidate` and the summary changed to 125 / 123 / 3 / 1.
- Both preflight executions completed and rolled back. The overall acceptance command exited 0.

## Limits and next action

This validates the query and its set-difference behavior against an isolated fixture. It does **not** establish the exact IDs in production. P0-B remains open until this SQL and `catalog-reference-preflight.sql` are run read-only against the current production database and the output is reviewed. No production connection, database, schema, settings, or release was changed.

The full migration ID manifest is a snapshot of the 125-migration candidate present on 31 October 2026. Regenerate it from `src/EnvanterTR.Web/Data/Migrations` if the candidate changes before execution.
