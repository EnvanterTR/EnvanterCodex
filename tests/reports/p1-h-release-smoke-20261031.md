# P1-H current-release isolated smoke — 31 October 2026

## Result

PASS for the current source image in a disposable, isolated Compose environment. This is supporting release evidence only; it is not the required live PostgreSQL + media + Data Protection key backup, independent restore, row-count comparison, or live rollback rehearsal.

## Artifact and execution

- Source commit: `ca59fb5869a08d69ab57658e4cca4a406bb95a74`.
- Release image tag: `envantertr-web-v1-candidate:ca59fb5-p1h-20261031`.
- Image manifest: `sha256:2a4ccca6e403b49dfc5cf83153e1fc3dde620168d0108ae279477fb1864887bf`.
- Built from the checked-in production `Dockerfile`; build completed successfully.
- Executed `deploy/testing/run-release-smoke.ps1` with synthetic PostgreSQL. The isolated run used a unique Compose project/network and temporary database, media, and key volumes; no host ports or production credentials/data were supplied.
- Smoke verified exact Release image, all 125 source migrations, read-only production runtime, local JS assets, secure cookie flags, first-owner setup, login, scope refresh, tenant/platform separation, CSRF rejection, and logout.
- Runner exited successfully and its `finally` cleanup removed the smoke containers, temporary volumes, and network.
- `pwsh -NoProfile -File deploy/windows/tests/backup-crypto-check.ps1` passed: media and Data Protection key inclusion in the fixture archive, encrypted round trip, wrong-passphrase rejection, and tamper rejection.
- `pwsh -NoProfile -File deploy/windows/tests/restore-safety-check.ps1` passed: restore authentication/integrity checks, bounded extraction, separate database staging, bounded PostgreSQL recovery process, rollback ordering, maintenance lock, and package inclusion.
- `pwsh -NoProfile -File deploy/windows/tests/backup-alert-check.ps1` passed: mocked HTTPS-only notification, no-secret silence/logging, and failed backup/health task propagation.
- These three Windows checks use synthetic fixtures/static safety assertions and mocked delivery. They do not create or restore a live or local application database.
- `pwsh -NoProfile -File deploy/testing/run-release-backup-restore.ps1 -Image envantertr-web-v1-candidate:ca59fb5-p1h-20261031` completed a synthetic full-package drill. The current Release app created a fixture company/owner; a custom-format PostgreSQL 17 dump was restored into a separate disposable PostgreSQL container on an isolated Compose network. Exact per-table row counts matched across 96 tables. Synthetic company-media and Data Protection key archives passed traversal checks and were extracted into separate new volumes; expected fixture bytes matched. SHA-256 values were emitted for all three temporary artifacts. The script then removed only its GUID-named containers, volumes, network, and temporary directory.
- The first drill attempt exposed a test-fixture comparison mistake (different expected media and key marker contents were compared to each other). The fixture was corrected to compare each marker against its own expected bytes; the fresh rerun above passed.
- The synthetic backup/restore runner is now part of `.github/workflows/ci.yml` immediately after the Release smoke. Local execution passed; the workflow step is awaiting its first CI result.

## Scope and remaining P1-H evidence

No production or staging environment was changed. No live snapshot was taken or restored by this run. The local Compose `.env` is absent, so the local backup script could not be used against an application database either. The last documented live recovery bundle and scheduled SQL restore are from 27 October and an earlier source/schema; they do not establish recovery for today's live state or this candidate. P1-H remains open until the current live `pg_dump` + media + key bundle is integrity-checked, restored into a separate database, compared for required row counts, and the rollback path is rehearsed without mutating production data.
