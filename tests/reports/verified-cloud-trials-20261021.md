# Verified Cloud trial registration — 2026-10-21

Added feature-gated request/confirm endpoints. Default disabled; readiness also requires SMTP/public HTTPS URL and eligible public annual-only finite-seat TRY plan. Request requires CSRF, valid company/slug/name/email and versioned processing acknowledgment. No company/user created before email verification. Time-limited30minute encrypted/protected token stays in email fragment; raw token/address never logged. Confirm is explicit POST, no-store, rate limited, per-email PostgreSQL transaction lock, with reserved-slug checks.

Creates real non-demo Trial for exactly15days, owner membership, MFArequired and audit. Replaying same registration returns same company without extending time; another registration for the same trial owner is rejected through stable audit attribution. Existing owner credentials are not reset; new unconfirmed owner receives a same-site password-setup fragment URL. No automatic charge.

Isolated PostgreSQL2/2PASS, process1748exit0: emailcapture, no pre-verificationcompany, exact15dayperiod, owner/MFA, replay, repeat-trial rejection, defaultclosed, CSRF and invalidtoken. Web0warnings/errors,4testhostdeprecationwarnings; disposableDB/networkcleaned. No schema changes or production trial activation.

Incomplete: trial.html/UX and all5languages; expiry/tamper/concurrency/existing-owner tests; deployconfiguration; annual existing-company payment conversion; tax-inclusive final amount; real SMTP/provider acceptance. No full commercial rollout claim.

## Trial UI and annual pricing — continuation
Added green-theme trial registration and explicit email confirmation UI in TR/EN/RU/UK/MK. Confirmation proof is removed from the address, never sent in request URLs, and no confirmation POST occurs automatically. Same-document email-link navigation exposed a real hash-navigation bug; fixed with hashchange handling and stale-load protection. Custom slugs are preserved; Farma/Santa/Celal suggestions are supported. External portal links are rejected.

Standalone Chromium synthetic-API test PASS after fixing the product bug and two test expectations (disabled fieldset child and retained language). Tests cover unavailable gate, CSRF/consent, duplicate submit, five languages, 390px layout, explicit confirmation and unsafe links. This is not real SMTP delivery proof.

Public pricing browser acceptance PASS: annual-only plans link to trial registration and show annual/12 comparison; legacy monthly checkout is preserved. Production/local Nginx proxy acceptance rerun exited0 with output in trial-proxy-acceptance-20261021.log; previous process output was unavailable after context truncation. Inventory regenerated at2268 controls. CloudTrials environment switch defaults false.

Remaining: expiry/tamper/concurrency/existing-owner coverage, existing-company annual payment conversion, final tax-inclusive checkout, real SMTP/provider acceptance. Trial UI is now implemented; feature remains disabled in production. No production deployment of this branch in this continuation.

## Existing owner and CI follow-up
PostgreSQL trial acceptance PASS3/3 (final run c6ceb6c8a8, process31623). New case proves tampered proof creates no company, confirmed existing owner receives no password-setup link, and password hash/security stamp/email confirmation/display name/membership remain intact. Initial new test compilation failed because owner ID was declared string instead of Guid; corrected; original failure log retained. Disposable DB/network removed.

GitHub PR22 and PR23 both failed at Verify platform subscription plan catalog workflow: obsolete source assertion required rejecting finite Cloud quotas. Updated verifier to require preserving requested positive quotas and reject nonpositive values; local33assertionsPASS. Backend runtime quota tests remain separate evidence. No full commercial activation or V1 completion claim.
