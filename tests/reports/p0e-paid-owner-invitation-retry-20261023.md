# P0-E paid company invitation failure and retry — 2026-10-23

- Status: PASS in isolated PostgreSQL with captured synthetic SMTP. Real merchant/inbox delivery remains open.
- Change: Paid-company creation and owner-invitation retry now log a failed send with only company ID and exception type. They preserve the existing user-facing unsuccessful-send response. Raw exception details, email tokens and SMTP credentials are not logged.
- Test: Expanded the existing paid provisioning acceptance to two cases: successful first delivery and failed first delivery. The failure case uses an invalid SMTP port, verifies `201` with `invitationSent=false` and a visible message, then verifies a failed retry returns `503`. Restoring the capture configuration allows the same owner invitation to be sent; the existing account-confirmed resend is also verified. The final database assertions require exactly one company, owner membership and subscription.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0-owner-mail run --build --rm acceptance --filter "FullyQualifiedName~Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation"` exited 0; 2/2 passed, 55.0757 seconds.
- Build: Web project 0 warnings / 0 errors; acceptance project four existing ASP.NET deprecation warnings / 0 errors. Related platform provisioning and invitation source checks passed.
- Evidence: `tests/reports/paid-owner-invitation-failure-20261023.log` contains both safe warning records and both passed cases. Disposable database/network removed after completion. No production data/settings changed.
- Remote checkpoint: Earlier candidate `06cbd5d` completed the full PostgreSQL CI job `120518734937` in run `39984118678`: 281/281 passed, 18.9787 minutes. That candidate's separate Linux source-check job failed the old Cloud-exclusion assertion, fixed in `17b54f1`; this checkpoint is not a green full CI claim for the newer SMTP candidate.

TAMAM dediğim 2 kabul testinin 2'si test çıktısıyla doğrulanmıştır.
