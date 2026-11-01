# Production candidate release c328a2a — 24 October 2026

## Result

Production web and public-site now run c328a2a0568d6659add87062a2d708cf1fd38653. Coolify deployment we99jqgmmvgojjbf4yj1qrjp finished green (4m45s). Host docker inspection/listing shows both running image tags at that exact SHA, PostgreSQL healthy. External HTTPS apex and Farma /api/status return HTTP 200, ready, 120 migrations, 0 pending. This is a deployed acceptance candidate, not completed V1 or sales readiness.

## Release gates actually exercised

- Exact candidate GitHub Actions run 40077928353: all three jobs completed success. pr25-ci-c328a2a-20261024.json records terminal jobs.
- git diff c328a2a ae875b6 -- src deploy/coolify Dockerfile was empty: deployed product/runtime sources match the actual isolated staging sources. Later local commits contain documentation/evidence only.
- Fresh production custom dump pre-web-v1-20261024T103444Z.dump: pg_restore --list and SHA verification passed.
- New root-private 142 MB bundle /var/backups/envantertr/rollback-20261024T103525Z includes dump, media, keys, exact old application images and private settings. Seven checksums passed. Files/images restored to separate private drill directory ending -drill-20261024T103631Z; old image identities verified.
- Fresh dump restored into separate isolated PostgreSQL DB envantertr_restorecheck_20261024_1035. All 95 original public table counts matched current production before migration.
- Exact SQL SHA256 cddf35fd830861e61d018fb1885185e921bccfdf04934572391a162e1db77b49 applied/repeated on that fresh clone: all original fields and rows across 94 data tables preserved, 118→120. Raw comparison archived only inside the private bundle.
- Same hash-verified SQL applied to production with ON_ERROR_STOP, explicit 118 precondition and 120 postcondition. Original 94 data-table counts matched before/after (migration history excluded because its two added rows are expected). Production row-count evidence does not claim a full field-by-field production comparison; that stronger comparison was performed on the restored clone.
- Coolify branch changed from main to feat/verified-cloud-trials-20261021, exact SHA pinned to c328a2a. Reload confirmed persistence before redeploy. The Changes not applied badge meant deployment was pending, not that Save failed. Badge cleared after successful deployment.

## Actual live resources

- web-izrckbfan6xop8b6xgigduvt-20261024T104145
- public-site-izrckbfan6xop8b6xgigduvt-20261024T104145
- database-izrckbfan6xop8b6xgigduvt-20261024T104145

The deployment pulled postgres:17-alpine before replacing containers; it remained within PostgreSQL 17 and the persistent database is healthy. Exact old database/app image identities remain in the rollback bundle. PR #25 remains a draft stacked on PR #23; no main-branch merge is claimed. Production uses the explicit candidate ref/SHA.

## Post-release acceptance and open work

- Existing platform administrator session survived the additive migration/redeploy; authenticated dashboard loaded.
- New plan UI now allows finite Cloud user quotas, and the annual-catalog preview matches all eight approved price/user tiers and KDV-exclusive annual-only policy.
- Attempt to save annual drafts prompted the expected critical-operation reauthentication. Password was prepared; current MFA was handed to the user. No draft/plan creation success is claimed yet.
- Explicit apex/Farma health probe passed (c328-live-readiness-20261024.log). Full default sales probe exits 1: zero public plans and checkout unavailable (c328-live-sales-gates-20261024.log). The default representative firma host health does not prove any tenant exists there.
- SMTP/iyzico secrets and real inbox/payment acceptance remain missing. Cloud trial/sales was not enabled. Santa still needs proper provisioning; full Santa/Farma/Demo3 Section 20 remains open.
- No credentials, private environment, backup contents, raw customer rows, cookies, codes or tokens were added to the repository. Private MFA handoff screenshot is intentionally excluded.

Evidence screenshots: coolify-release-fresh-backup-20261024.png; coolify-release-restore-counts-20261024.png; coolify-release-migration-rehearsal-20261024.png; coolify-c328-source-persisted-20261024.png; coolify-production-118-to-120-20261024.png; coolify-production-data-counts-20261024.png; coolify-c328-live-ready-120-20261024.png; coolify-c328-deployment-finished-20261024.png.
