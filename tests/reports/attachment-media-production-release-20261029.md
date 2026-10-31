# Attachment provider release — 29 October 2026

Candidate: `c386113671eed23ef564deb4ba904378cbd7130e`.

The change protects maintenance, model, contract, asset purchase-order, accessory purchase-order and stock purchase-order attachments, plus the assigned employee's EULA download. It rejects redirected company storage before reading a file or changing deletion metadata. Focused PostgreSQL acceptance passed: 5/5, 1/1 and 9/9.

## Validation before deployment

- CI [40799655866](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40799655866): all three jobs succeeded. Linux security: 7m25s; PostgreSQL: 41m29s; Windows: 5m00s. The final PostgreSQL case count has not been extracted; no inferred count is claimed.
- Exact stage: `/var/lib/envantertr-staging/c386113-20261029`. Both builds exited 0. Web and public containers ran on the internal network. Runtime, HTTP access boundaries and all 95 original table field projections passed (`acceptance-probes.exit=0`, schema 122/0).
- Fresh stage SQL backup: `pre-web-v1-20261029T172204Z.dump`. Hash, archive listing and independent clone restoration passed.
- Fresh release SQL backup: `pre-web-v1-20261029T172949Z.dump`. Independent restoration confirmed schema 122 and 95 data tables.
- Recovery bundle: `/var/backups/envantertr/rollback-20261029T172956Z` (143 MB). SQL, media, keys and images of the current 2997062 release were recovered independently. Production was not restored.

Evidence: `attachment-media-ci-success-20261029.png`, `attachment-media-stage-pass-20261029.png`, `attachment-media-fresh-recovery-pass-20261029.png` and the corresponding saved scripts.

## Deployment

Coolify's commit was saved and reloaded as c386113. One deployment, `pjm2ptmwauornyistja3brsa`, started at approximately 17:33 UTC. Its actual source fetch/import was c386113, despite the newer branch head. At this observation the build was running. Deployment completion, actual runtime images and external health/source/boundary verification remain pending. Do not restart from an observation timeout or an unapplied-configuration banner.

## Remaining scope

Catalog/accessory image protection (`68f4d88`, 12/12 focused tests) is a separate candidate. Company-logo/employee-photo protection is a later local change with tests still running. Neither is included in this release. Full Hedef/P1, the remaining media evidence and real merchant/customer acceptance remain open.

## Final live verification

The deployment finished successfully in 4m42s (started 17:33:07 UTC). Production web/public/database containers have suffix `20261029T173335`.

Runtime verified at 17:38:39 UTC: exact c386113 images, all running with zero restarts, healthy database, read-only web filesystem, Google SMTP relay with TLS on port587 and no SMTP authentication, correct HTTPS checkout callback, absent real merchant keys and disabled public trials.

External checks at 17:39:02 UTC both exited0. Apex, Santa and Farma returned200/ready with schema122/0. Eight plans remain published; real checkout and public trials remain unavailable. Gallery CSS/JS and six image hashes, company/reset HTML and onboarding CSS match source. Protected anonymous PDF/email/onboarding endpoints return401. Tenant platform pages/scripts/API/reauth remain404; root/www admin redirects remain relative302 without session cookies, tenant admin remains404.

Evidence: `attachment-media-deployment-finished-20261029.png`, `attachment-media-live-health-20261029.log`, `attachment-media-live-boundaries-20261029.log` and `attachment-media-live-runtime-guards-20261029.py`.

Current production is c386113. The separate 68f4d88 catalog/accessory image candidate is not live. Full Hedef remains open.
