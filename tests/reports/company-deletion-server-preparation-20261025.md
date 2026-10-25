# Company deletion release preparation — 25 October 2026

Candidate `6198ef6bc424ac44e9e74c9ab39357b2d6c162b9`, PR #25. Production is still `5790980f8f8a298b720287260a37b7f972bd1e57` and schema 120; no deployment/migration to production performed here.

## Completed on the actual server

- Reconnected the existing authenticated Coolify root terminal. No new MFA/SSH/API credentials requested. Actual Docker runtime reconfirmed web/public image 5790980, suffix `20261024T205706`, healthy database.
- Fresh current-production dump: `/var/lib/postgresql/data/envantertr-backups/pre-web-v1-20261024T214754Z.dump`; `pg_restore --list` and SHA-256 passed.
- Root-only rollback bundle `/var/backups/envantertr/rollback-20261024T214755Z`, 140 MB. It contains the **currently running 5790980** app images, SQL, media, protection keys and private environment. Private contents were not printed or uploaded.
- File/image recovery rehearsal succeeded into `/var/backups/envantertr/rollback-20261024T214755Z-drill-20261024T214900Z`.
- Dump restored into the separate network-none restore-check container `envtr-restorecheck-20261024-0723`, new DB `envantertr_restorecheck_20261024_2147`. All **95** public table row counts matched current production. This is row-count verification, not an all-column content fingerprint.
- Local isolated `company-trash-20261025` acceptance DB/network removed after all local runs reached terminal state. Other local projects were preserved.

Screenshots: `coolify-company-deletion-backup-20261025.png`, `coolify-company-deletion-rollback-20261025.png`, `coolify-company-deletion-restore-counts-20261025.png`.

## Live processes / next checks

- GitHub CI **40135536734** for exact candidate is terminal **success, all 3 jobs**: Windows `120996150172`, Linux security `120996150314`, PostgreSQL `120996150390`. Final API snapshot is `pr25-ci-6198ef6-20261025.json`; do not restart or keep polling this completed run.
- Exact source downloaded into `/var/lib/envantertr-staging/6198ef6-20261025`; original build PID **683413** finished with `build.exit=0`. After the browser/tool interruption the candidate image was authoritatively absent (`docker image inspect` and image list), although the original result remained 0. A recovery build from the **same exact sources** was therefore launched, PID **1083550**, with distinct `build-recovery.log` / `build-recovery.exit` results. Recheck this handle/result; do not launch another build based only on silence.
- New staging DB container `envtr-stage-db-6198ef6`, dedicated named volume `envtr-stage-db-6198ef6-data`, internal network `envtr-stage-ae875b6`, no published port. Current-production dump restored into the previously empty `envantertr_stage` database, owned by a separate staging account. No production volume is mounted.
- Candidate SQL checksum matched the committed artifact. Migration 120→121 was applied **twice** only on the new staging clone. The original-column projection set captured before migration produced identical row-count/MD5 content fingerprints before, after and after repeat for **94 data tables** (migration history excluded). Schema history count is 121. Private fingerprint files stayed on the server; no row contents were printed. Screenshot `coolify-company-deletion-staging-migration-20261025.png`.
- Initial quoted empty-database query failed before restore; a corrected `current_schema()` guard then restored successfully. A multiline terminal input was joined by the UI and remained unexecuted in a heredoc prompt; the terminal connection was replaced and the SQL was transferred as a single encoded command. No migration was applied by either failed attempt.
- The app-start UID guard rejected the absent image before creating media/key directories or starting an app. Pending: recovery image success, separate media/key copies and private staging-only environment, app readiness 121/0, protected endpoints and rendered controls. Production is still 5790980/schema 120. Because time passed during the tool interruption, refresh the production backup/restore/rollback gates immediately before a production release.

## Subsequent verified preparation

Recovery build exit 0; app started on the internal staging network with independent media/keys and no published port. Plain HTTP initially failed Secure-only antiforgery-cookie enforcement. Logs confirmed the cause. The staging-only app was recreated with the private deployment's opt-in forwarded-proto configuration; HTTPS-proxy host checks then passed ready/121/0, CSRF response and Secure cookie. No protection was disabled. Provider credentials are empty. Screenshot: `coolify-company-deletion-stage-ready-20261025.png`.

Backup refreshed after the interruption: `pre-web-v1-20261025T023646Z.dump`, 140 MB bundle `/var/backups/envantertr/rollback-20261025T023647Z`, file/image drill suffix `20261025T023832Z`, new network-none restore DB `envantertr_restorecheck_20261025_0236`. All 95 public table counts matched production. Earlier pending app/backup statements above are historical. Production migration/deploy actions are tracked separately in `production-company-deletion-release-20261025.md`.

Actual customer/provider acceptance remains open. The last checked root platform browser was at login; no new OTP was requested. Future real tenant actions should be batched into one legitimate fresh session.
