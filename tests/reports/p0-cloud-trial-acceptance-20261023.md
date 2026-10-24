# P0 Cloud trial acceptance — 2026-10-23

- Status: PASS (isolated PostgreSQL 17 / synthetic email capture); live email delivery and real customer onboarding remain unverified.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Verified_cloud_trial|FullyQualifiedName~Cloud_trial|FullyQualifiedName~Concurrent_cloud_trial|FullyQualifiedName~Concurrent_distinct_cloud_trials"`
- Result: exit 0; 6 passed, 0 failed, total 1.1533 minutes.
- Coverage: 15-day cloud trial, verified email, mandatory MFA, idempotent confirmation without extending the trial, existing owner credentials preserved, tampered proof rejected, expired proof/withdrawn plan do not create accounts, and concurrent trial requests for one email grant at most one trial.
- Environment: isolated acceptance database and synthetic SMTP capture. Compose database/network removed after run. No production plan, account, setting, or data was changed.
- Evidence: `tests/reports/cloud-trial-acceptance-20261023.log`.
- Next: P0-D real Iyzico sandbox and P0-E live SMTP plus third-company tenant-isolation acceptance. Live plan catalog is empty and checkout is unavailable; no sales publication or production deploy was performed.
