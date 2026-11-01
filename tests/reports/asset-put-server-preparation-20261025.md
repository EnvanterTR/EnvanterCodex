# Asset PUT candidate — server staging and recovery gates

Candidate: `124b2d1e93af2a13c393d68353c271671c856de8`, PR #25. Production remains `6198ef6bc424ac44e9e74c9ab39357b2d6c162b9`.

## Verified server preparation

- Exact GitHub commit archive extracted into `/var/lib/envantertr-staging/124b2d1-20261025`; Docker build `envantertr-staging:124b2d1` exited 0. Original process was observed running (parent 1185440, Docker child 1185507), then its completion and exit file were observed; no restart.
- Fresh production dump `pre-web-v1-20261025T034105Z.dump`, archive listing and SHA check passed.
- Fresh 140 MB bundle `/var/backups/envantertr/rollback-20261025T034258Z` includes current 6198ef6 web/public images, SQL, media, keys and private environment. All checksums passed.
- Recovery drill `/var/backups/envantertr/rollback-20261025T034258Z-drill-20261025T034349Z`: media/key extraction, archive comparison, image load and image identity checks passed.
- Initial bundle invocation used the web timestamp for the public container. Its preflight stopped with `no such object`, before creating a bundle. Live inventory confirmed public suffix `20261025T025227` versus web/database `20261025T025226`; corrected invocation completed. No production service was changed by that failed preflight.
- Fresh dump restored into a new database `envantertr_stage_124b2d1` in the existing isolated `envtr-stage-db-6198ef6` container. All **95** public table counts matched production; individual counts remain private.
- New `envtr-stage-web-124b2d1` runs the exact built image with a private environment file selecting that new database. Media and keys are separate copies under the candidate directory. No production mounts, no published ports; `envtr-stage-ae875b6` is internal. Read-only root filesystem and no-new-privileges enabled.
- Host-header health for apex, Farma and Santa: ready / migrationCount 121 / pending 0. Host-header health does not establish tenant existence or public DNS acceptance.
- Anonymous asset GET, PUT and PATCH each returned 401. CSRF response and Secure cookie passed with the existing HTTPS forwarded-header configuration; token/cookie values were not printed.

Evidence: `coolify-put-fresh-backup-20261025.png`, `coolify-put-rollback-bundle-20261025.png`, `coolify-put-rollback-drill-20261025.png`, `coolify-put-restore-counts-20261025.png`, `coolify-put-staging-ready-20261025.png`.

## Remaining release gate

CI **40158737966** exact candidate is still in progress as of 03:48 UTC: Linux security and Windows deployment/backup jobs completed successfully; PostgreSQL acceptance job 121065583776 remains running. See `pr25-ci-124b2d1-20261025.json`. Do not restart on an observation delay or deploy before terminal success.

No new migration is introduced. No authenticated customer operation, live Santa/Farma cross-tenant acceptance, real SMTP delivery or provider payment acceptance is claimed here. No personal OTP was requested.
