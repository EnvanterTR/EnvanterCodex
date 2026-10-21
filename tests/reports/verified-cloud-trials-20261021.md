# Verified Cloud trial registration — 2026-10-21

Added feature-gated request/confirm endpoints. Default disabled; readiness also requires SMTP/public HTTPS URL and eligible public annual-only finite-seat TRY plan. Request requires CSRF, valid company/slug/name/email and versioned processing acknowledgment. No company/user created before email verification. Time-limited30minute encrypted/protected token stays in email fragment; raw token/address never logged. Confirm is explicit POST, no-store, rate limited, per-email PostgreSQL transaction lock, with reserved-slug checks.

Creates real non-demo Trial for exactly15days, owner membership, MFArequired and audit. Replaying same registration returns same company without extending time; another registration for the same trial owner is rejected through stable audit attribution. Existing owner credentials are not reset; new unconfirmed owner receives a same-site password-setup fragment URL. No automatic charge.

Isolated PostgreSQL2/2PASS, process1748exit0: emailcapture, no pre-verificationcompany, exact15dayperiod, owner/MFA, replay, repeat-trial rejection, defaultclosed, CSRF and invalidtoken. Web0warnings/errors,4testhostdeprecationwarnings; disposableDB/networkcleaned. No schema changes or production trial activation.

Incomplete: trial.html/UX and all5languages; expiry/tamper/concurrency/existing-owner tests; deployconfiguration; annual existing-company payment conversion; tax-inclusive final amount; real SMTP/provider acceptance. No full commercial rollout claim.
