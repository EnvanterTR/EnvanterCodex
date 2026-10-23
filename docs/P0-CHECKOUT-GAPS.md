# P0 checkout release gaps — 23 October 2026

These are open requirements, not completed features. Local synthetic acceptance and a green PostgreSQL suite do not establish live sales readiness.

## Existing trial company to paid subscription — INCOMPLETE

The approved commercial flow gives the customer a 15-day trial followed by an annual payment. The existing company, portal, memberships and inventory must survive the change.

The original audit found that the anonymous new-company checkout rejected occupied company slugs both before and after payment. The current candidate adds a distinct authenticated `/api/subscription/checkout` API and immutable existing-company/owner context. It activates the existing company through the shared provisioning service after verified payment. A server-rendered `/subscription/checkout` page now lets a company owner choose an eligible annual Cloud plan, enter billing information, reauthenticate with password and MFA, and continue to the existing iyzico checkout. An expired-trial notice links to that page; the handler independently enforces owner authorization. Existing company data, members and portal identity remain tied to the existing-company transaction path. The expired-trial browser acceptance now passes against actual Kestrel/PostgreSQL with a synthetic provider; real-provider acceptance remains open. Platform subscription extension/edit routes remain separate administrator adjustments.

Required before accepting trial-to-paid sales:

- Bind checkout to the authenticated company owner and its immutable company ID; reject other company/user contexts and enforce CSRF/MFA requirements.
- Snapshot the purchased annual plan, price and payment purpose in the order. Allow the owner to reach this purchase action after trial expiry while retaining normal write restrictions.
- After provider verification, activate the paid period for that same company atomically with the payment/order state. Preserve trial history, company ID, portal, memberships and inventory. Enforce package capacity before accepting a downgrade.
- Reject failed/mismatched payments; replay and concurrent callbacks must not duplicate subscriptions or extend the purchased period twice. Deleting the target company must not turn an existing-company purchase into new-company provisioning.
- Complete real-provider purchase/return/status acceptance. On 24 October, the expired-trial owner browser flow and non-owner denial passed against the actual Kestrel/API/PostgreSQL with a synthetic provider. Full paid-plan lifecycle and live tenant-boundary acceptance remain open.

The original source audit is retained in commit `3c63980`. Current candidate acceptance is recorded separately under `tests/reports/existing-company-checkout-*` and `tests/reports/company-checkout-page-acceptance-*`. The latest isolated PostgreSQL candidate covers concurrent callback delivery: the order-row lock plus the existing company-row lock serialize callback processing, and both callback responses report the single activated subscription. On 24 October, targeted cases also covered a verified provider result with the wrong currency, an unavailable provider result, target-company deactivation and deletion. The payment is never activated in those cases; a deleted company is not recreated. The server-rendered expired-trial browser flow now passes; real provider acceptance and the complete plan-change lifecycle still need verification. New-company checkout acceptance alone does not cover those cases.

The separate existing-company browser acceptance is recorded in `tests/reports/existing-company-browser-acceptance-20261024.md` (exit 0). It verifies password/MFA/cookies, server-rendered form submission, failed-password field handling, mobile layout, provider Location, callback replay, paid history and retained tenant data. Real provider acceptance and the complete plan-change lifecycle remain open.

## Email verification before direct paid checkout — LOCAL ACCEPTANCE PASSED

The original audit found that anonymous `/api/checkout/initialize` accepted an unverified owner email. The candidate now sends a 30-minute protected email/plan-bound link, requires explicit CSRF-protected confirmation, and independently validates `emailVerificationToken` before provider initialization. Missing/altered/expired/wrong-purpose or mismatched proof returns 428 with no provider initialization or order for that request. SMTP configuration is required by public readiness and direct initialization; a previously valid proof cannot bypass removed SMTP configuration. Existing-company owner purchase retains its confirmed-account/password/MFA flow.

`tests/reports/checkout-email-verification-acceptance-20261024.md` records 15/15 isolated API/PostgreSQL cases plus Chromium checkout acceptance and the prior quota regression. SMTP is captured locally and iyzico is synthetic. This closes the local pre-payment ownership gap; real inbox/provider acceptance and release gates remain open.

## Real provider and customer gates — OPEN

Real iyzico merchant acceptance, real SMTP/inbox delivery, staging acceptance, fresh verified backup/rollback, and the Santa/Farma/Demo3 acceptance scenario remain required. No production release is authorized by this source audit.

The 24 October read-only public probe is recorded in `tests/reports/p0-live-sales-readiness-probe-20261024.md`: live reports 118 applied migrations, but `/api/plans` is empty, `/api/checkout/status` is unavailable, and the candidate `/api/trials/status` route returns 404. This confirms the candidate sales/onboarding path has not reached production; migration readiness alone does not establish a release.
