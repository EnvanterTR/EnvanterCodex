# P0-E new Cloud company provisioning acceptance — 2026-10-23

- Status: PASS in isolated acceptance environment with synthetic SMTP; live customer provisioning and delivery remain unverified.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0e-provision run --build --rm acceptance --filter "FullyQualifiedName~Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation"`
- Result: exit 0; 1 passed, 0 failed, total 48.9926 seconds.
- Verified: platform admin provisions a paid Cloud company with a unique tenant slug; response returns the tenant URL and 365-day term; company is active, subscription is Active/non-demo, owner membership is created, company MFA is required, owner invitation is sent and can be retried, and duplicate slug is rejected.
- Environment: isolated PostgreSQL and SMTP capture. Compose resources removed after the run. No production company, subscription, email, or configuration was changed.
- Evidence: `tests/reports/p0e-new-company-provisioning-acceptance-20261023.log`.
- Limitation: the live candidate lacks this endpoint (live `/api/trials/status` is 404; verified `origin/main` does not contain the trial endpoint mapping). Generic wildcard DNS/TLS does not establish that a tenant exists. Live purchase/owner login/data isolation/deletion acceptance remains open.
