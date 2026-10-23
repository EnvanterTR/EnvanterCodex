# Pre-payment email ownership — 24 October 2026

## Result and commands

- **15/15 API/PostgreSQL cases passed**, exit **0**, 5.9858 minutes: `deploy/testing/run-checkout-email-acceptance.ps1`. Full output: `checkout-email-final-acceptance-20261024.log`. The runner built the candidate, used a disposable internal-network database, and removed its database/network in `finally`.
- Targeted real Chromium checkout scenario passed, exit **0**: `docker run --rm --init -e HOSTED_CHECKOUT_SCENARIO_ONLY=1 envantertr-checkout-email-browser:local`. Its Dockerfile is `deploy/testing/Dockerfile.hosted-checkout-browser`. Output: `checkout-email-browser-20261024.log`.
- Quota regression during this package: **30/30 combinations** across five languages passed, exit 0; `checkout-email-quota-regression-20261024.log`.
- Web build: **0 warnings / 0 errors**. Test build: **0 errors**, four pre-existing ASP.NET deprecation warnings in `TrustedClientIpTests.cs`. Final build output: `checkout-email-final-build-20261024.log`.
- Hosted checkout, provider-flow and browser-harness source verifiers passed. UI inventory was regenerated and its check passed with **2,272 controls**; locale verifier passed with 2,246 entries. `git diff --check` passed.

## Product behavior

New-company purchases must verify the owner email before initializing a provider session. `/api/checkout/email/request` sends a 30-minute Data Protection proof to the requested mailbox; its HTTP response does not expose the proof. The proof is bound to the email, plan and a separate cryptographic purpose. The link carries it only in a URL fragment. `/api/checkout/email/confirm` requires CSRF and returns the verified email/plan; the browser additionally requires explicit confirmation, removes the fragment, and keeps the proof in page memory only.

`/api/checkout/initialize` independently requires that proof for anonymous new-company purchases. Missing, altered, expired, wrong-purpose, wrong-email or wrong-plan proof is rejected with 428 before payment/order creation. Public readiness and initialization also stop when SMTP configuration is unavailable; even a still-valid proof cannot bypass a removed SMTP configuration. Existing-company owner purchase continues using its existing confirmed account and password/MFA authorization.

Email request/confirmation has its own five-per-ten-minute IP limit, so it does not consume payment initialization attempts. The checkout sends no card data or client-supplied payment amount. Frontend and backend must ship together; the checkout script URL is versioned for this protocol change.

## Covered cases

The API suite uses a real isolated PostgreSQL database, ASP.NET application, SMTP capture server and synthetic iyzico transport. It verifies email delivery/capture, confirmation, no-proof denial, verified initialize/replay, all invalid proof variants, CSRF denial, missing SMTP, actual SMTP connection failure, the separate request limit, a valid proof after SMTP configuration removal, explicit API authentication metadata, provider pricing/provisioning replay, owner-only existing-company form/page and platform/public readiness alignment.

Chromium uses the actual checkout HTML/JS with synthetic APIs. It verifies disabled payment before proof, failed-email retry, duplicate-send blocking, explicit confirmation, invalid proof feedback, same-document fragment handling/removal, changing email, ignoring a late confirmation for a replaced link, expiry during payment, retained billing fields, clearing stale expiry errors, explicit renewal/retry, CSRF on both email calls, and passing only the new proof to checkout initialization. The previous coupon, slug, consent and trusted-provider redirect assertions still pass.

## Earlier failures and limits

The initial API run passed 6/12 and failed six new assertions because unrelated fixture orders were present. Assertions now check the rejected request's own checkout identity; other orders are preserved. The first failure output is retained in `checkout-email-acceptance-first-20261024.log`. An intermediate corrected suite passed 13/13 before the additional SMTP-removal guard; final coverage is the 15/15 suite above.

The first browser run found that a link opened in the same checkout tab did not rerun initialization. `hashchange` now consumes the link, clears its fragment and invalidates older pending confirmation results. The original failure is retained in `checkout-email-browser-fragment-first-20261024.log`.

No real email was sent or payment charged. Real inbox/iyzico merchant acceptance, staging, fresh production backup/rollback and real Santa/Farma/Demo3 acceptance remain open. No production mutation or deployment occurred.

The preceding remote candidate `dfdb36e` passed all three CI jobs in run `40035243704`; `pr25-ci-dfdb36e-20261024.json` records that result. This does not certify the new quota/email commits; their new full remote CI is required after push.
