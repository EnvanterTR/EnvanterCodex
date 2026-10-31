# Candidate 8db0343 staging and recovery — 24 October 2026

Exact source: 8db0343753e4e728ce7253ec221af6445118f018. Existing authorized Coolify server terminal used; no new credentials.

## Completed

- Server Docker build exited 0. Actual candidate image `envantertr-staging:8db0343` started as `envtr-stage-web-8db0343`.
- Candidate directory `/var/lib/envantertr-staging/8db0343-20261024`; copied staging media/keys, existing cloned staging database, internal `envtr-stage-ae875b6` network, no published ports. No production data volume mounted. SMTP/iyzico credentials blank.
- Apex, Santa-host and Farma-host readiness returned ready/120/0. This is routing/health evidence, not proof that a Santa tenant exists.
- Anonymous assets API returns 401. Platform companies API returns 401 on apex and 404 on tenant host. CSRF endpoint returned a request-token response; token not printed.
- Fresh production dump `pre-web-v1-20261024T134018Z.dump`: custom archive list and SHA verification passed. Includes published catalog changes.
- Root-private rollback bundle `/var/backups/envantertr/rollback-20261024T134137Z`, 140 MB, preparation exit 0. Files and exact image restore passed in separate directory ending `-drill-20261024T134409Z`.
- Fresh dump restored with exit-on-error into `envantertr_restorecheck_20261024_1340` on the separate restore container. All 95 public-table counts equal current production. Raw count files remain private on server.

Screenshots: `coolify-8db-fresh-restore-20261024.png`, `coolify-8db-stage-restore-20261024.png`, `coolify-8db-stage-security-20261024.png`.

## Pending

Update: run 40098994968 is now terminal failure, 326/327 tests passed. The sole failing test is `Annual_catalog_drafts_are_authorized_csrf_protected_idempotent_and_preserve_existing_plans`: the new freshness tests had left fixed annual catalog codes in the shared disposable database. Evidence: `ci-8db-catalog-collision-20261024.log`. The freshness fixture is being changed to unique unpublished plan writes; product code remains unchanged. Production release remains withheld. The in-progress observation below is historical.

Exact-head CI run 40098994968 remains in progress; Windows deployment/backup and Linux security jobs passed, PostgreSQL tenant acceptance still running at last observation. No production release of 8db0343 has occurred. Production still c328a2a. Local 15/15 authentication/support tests are separate evidence; no synthetic credentials or identity changes were applied to the staging copy of customer accounts.

Existing Farma browser tab showed the login form. No further OTP request, repeated login, recovery-code use, MFA reset or impersonation bypass was attempted. Provider and full customer acceptance remain open.
