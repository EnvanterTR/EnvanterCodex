# P0-E password recovery SMTP-capture acceptance — 2026-10-23

- Status: PASS in isolated acceptance environment; live SMTP delivery remains unverified and production SMTP is not configured.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Password_recovery_does_not_disclose_account_existence_and_reset_tokens_are_single_use"`
- Result: exit 0; 1/1 passed in 25.6045 seconds.
- Verified: existing and missing accounts receive the same accepted response; the isolated SMTP capture receives a reset link for the existing test account; the reset token is single use; the changed password validates afterward.
- Environment: disposable PostgreSQL 17 and local SMTP capture only. Compose database and network removed after execution. No production mail was sent and no production account was changed.
- Limit: This proves the recovery path works when SMTP is configured. It does not prove live SMTP credentials, DNS deliverability, or inbox delivery.
