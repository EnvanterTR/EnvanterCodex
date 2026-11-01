# Recoverable company deletion — production release record

Candidate: `6198ef6bc424ac44e9e74c9ab39357b2d6c162b9`, PR #25, source branch `feat/verified-cloud-trials-20261021`.

## Gates passed before production changes

- Full CI **40135536734** terminal success, all three jobs. Exact final source compiled, including the non-destructive migration downgrade refusal. Final snapshot: `pr25-ci-6198ef6-20261025.json`.
- Server source build and recovery build exit 0. Recovery followed confirmed absence of the first successfully built image, not observation silence.
- Independent staging DB restored from production backup; internal network, dedicated DB volume and separate media/key copies; no host ports or production mounts.
- Migration 120→121 applied twice on staging. Original-column content fingerprints for **94 data tables** remained identical; history count 121. Apex/Farma/Santa host-header readiness 121/0. This does not prove tenant existence or public DNS.
- Protected anonymous APIs 401 and company trash control served. CSRF response and Secure cookie pass with the private HTTPS proxy configuration. The initial plain-HTTP CSRF failure was diagnosed as Secure-only-cookie enforcement; no protection was disabled.
- Fresh dump `pre-web-v1-20261025T023646Z.dump`; bundle `/var/backups/envantertr/rollback-20261025T023647Z`, 140 MB, includes current **5790980** images and private SQL/media/keys/environment. File/image drill suffix `20261025T023832Z`; separate restore DB `envantertr_restorecheck_20261025_0236`; all **95** table counts equal production.

Evidence: `company-deletion-acceptance-20261025.md`, `company-deletion-server-preparation-20261025.md`, screenshots and logs. Private backup/environment, raw rows, OTP and cookie values are not published.

## Production actions

Exact 6198ef6 source pin persisted after a real page reload. The Coolify unsaved-state banner still appeared, but the reloaded repository, branch and SHA fields retained the intended values. The subsequent deployment row also links the exact candidate commit; the banner was not used as evidence of a failed save.

Before migration, the old live image tag, fresh bundle hashes, SQL artifact checksum and production history count 120 were checked. Only the additive checked SQL was applied; SQL history is now **121**, confirmed in the actual production PostgreSQL container. Screenshot: `coolify-company-deletion-production-migration-20261025.png`.

Deployment **q7up7i4mglpe1w1ismqpnmzn** started with exact candidate SHA and is **in progress**. Do not restart based on observation delay. Immediately after SQL migration, external apex/Farma remained ready; their old app reported compiled `migrationCount=120`, pending 0. `Program.cs` returns the binary's known migration count, not the DB's applied count; this is expected until the new image runs and does not contradict SQL history 121.

## Verified completed release

Coolify **q7up7i4mglpe1w1ismqpnmzn** finished **Success / Finished**. Actual running web and public-site tags are the full **6198ef6bc424ac44e9e74c9ab39357b2d6c162b9**; container suffix `20261025T025226`; PostgreSQL is healthy. Screenshots: `coolify-company-deletion-finished-20261025.png` and `coolify-company-deletion-live-images-20261025.png`.

External apex and Farma HTTPS readiness pass HTTP 200/ready, compiled 121/pending 0; actual SQL history 121 was independently verified. `company-deletion-live-readiness-20261025.log`, exit 0.

Public catalog still has 8 plans. Checkout remains unavailable due missing provider configuration; the full commercial probe intentionally returns exit 1. This is an open sales gate, not a failed deployment. `company-deletion-live-sales-boundaries-20261025.log`.

All 8 requested gallery/script/style/product-image resources return HTTP 200 and match local source SHA-256 byte-for-byte, including all 3 device images, portrait-bearing account-security preview, company portal and transparent roll. Homepage gallery/fullscreen hooks and live company trash/restore controls are present. `company-deletion-live-assets-20261025.log`, exit 0. This proves delivery integrity; no new live interactive carousel or authenticated trash action is claimed.

## Open scope

Real Demo3/Santa/Farma creation/deletion/cross-tenant/customer acceptance is still required. No new personal MFA code was requested. SMTP inbox and real provider acceptance remain unavailable; checkout stays closed. This release does not complete Hedef, prove a sale, or establish a merge to main.
