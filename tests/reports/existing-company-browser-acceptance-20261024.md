# Existing company checkout — Kestrel/PostgreSQL/Chromium acceptance

Date: 24 October 2026. Result: **PASS**, exit code **0**, **1/1 end-to-end scenario**.

## Environment and command

`deploy/testing/run-existing-checkout-browser-acceptance.ps1`

The runner creates a random Compose project with disposable PostgreSQL 17 on an internal network. A test-only .NET 10 WebApplicationFactory hosts the real application on Kestrel. Chromium uses real Identity password/MFA login and application cookies. Only the iyzico HTTP client is replaced with a synthetic provider, exclusively inside the acceptance executable.

The C# build completed with **0 warnings / 0 errors**, recorded in `existing-company-browser-redirect-first-20261024.log`. The final successful run reused that build after a Node-only interception correction; its full output is `existing-company-browser-20261024.log`.

## Verified behavior

- Two synthetic companies, their assets, an expired trial, an MFA-enabled owner and a viewer are seeded in the disposable database.
- Real owner password/TOTP login creates an Identity session cookie.
- The real `/subscription/checkout` page works after trial expiry and shows 4,200 TRY annually / 350 TRY monthly equivalent, English text and dark theme.
- Browser form submission with an incorrect password returns HTTP 400, retains billing fields and clears password/TOTP. Secrets are absent from the returned HTML.
- The layout fits 390px. `existing-company-browser-20261024/checkout-390.png` was captured and visually inspected.
- Correct password/TOTP, consent and the hidden CSRF token submit through the real form. The application's actual HTTP 302 Location is verified against the trusted synthetic iyzico URL.
- After checking that original Location, browser interception substitutes a local provider page to keep all browser traffic inside the isolated environment.
- The real order API returns `PaymentStarted`. Two requests to the real payment callback yield `Provisioned`; the real page then displays `Subscription active`.
- A separately logged-in viewer receives 403 from both the checkout page and owner order endpoint.
- Zero browser JavaScript errors.

PostgreSQL checks confirm exactly one provider initialization, one active paid subscription, the order's original company/owner and 4,200 TRY amount, unchanged company count, both original memberships, retained trial history, and both companies' assets retaining their original CompanyId.

## Initial failures and boundaries

The first run correctly disabled the form because the test callback URL was not on the application's approved list. The harness configuration was corrected without weakening application validation; output is retained in `existing-company-browser-first-20261024.log`.

The second run reached the trusted redirect but Chromium attempted to leave the isolated network. The harness now verifies the real Location and navigates to a local provider stand-in. That failure is retained in `existing-company-browser-redirect-first-20261024.log`.

No production/staging settings or data changed. No payment was charged or email sent. Real iyzico/SMTP, the complete paid plan-change lifecycle, staging, fresh backup/rollback and real Santa/Farma/Demo3 acceptance remain open. The runner removed its disposable database and network. CI runs the scenario and uploads the log and screenshot on each PR.
