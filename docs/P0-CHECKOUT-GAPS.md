# P0 checkout release gaps — 23 October 2026

These are open requirements, not completed features. Local synthetic acceptance and a green PostgreSQL suite do not establish live sales readiness.

## Existing trial company to paid subscription — INCOMPLETE

The approved commercial flow gives the customer a 15-day trial followed by an annual payment. The existing company, portal, memberships and inventory must survive the change.

The original audit found that the anonymous new-company checkout rejected occupied company slugs both before and after payment. The current candidate adds a distinct authenticated `/api/subscription/checkout` API and immutable existing-company/owner context. It activates the existing company through the shared provisioning service after verified payment. This backend work does not complete the customer-facing conversion flow: a purchase entry point, form, return/status interaction and browser acceptance remain open. Platform subscription extension/edit routes are still separate administrator adjustments.

Required before accepting trial-to-paid sales:

- Bind checkout to the authenticated company owner and its immutable company ID; reject other company/user contexts and enforce CSRF/MFA requirements.
- Snapshot the purchased annual plan, price and payment purpose in the order. Allow the owner to reach this purchase action after trial expiry while retaining normal write restrictions.
- After provider verification, activate the paid period for that same company atomically with the payment/order state. Preserve trial history, company ID, portal, memberships and inventory. Enforce package capacity before accepting a downgrade.
- Reject failed/mismatched payments; replay and concurrent callbacks must not duplicate subscriptions or extend the purchased period twice. Deleting the target company must not turn an existing-company purchase into new-company provisioning.
- Provide the customer-facing purchase/return/status flow and prove it against the real API/database and browser, including expired trials and tenant-boundary attacks.

The original source audit is retained in commit `3c63980`. Current candidate acceptance is recorded separately under `tests/reports/existing-company-checkout-*`; only a successful final run can establish the covered backend cases. Existing new-company checkout tests alone do not cover this requirement. Concurrent callbacks, target deletion at runtime, provider failure/mismatch in the existing-company context, and full paid-plan change lifecycle still need explicit acceptance; sequential replay and metadata showing no target foreign key are narrower evidence.

## Email verification before direct paid checkout — INCOMPLETE

Trial registration verifies a short-lived protected email proof. The separate anonymous `/api/checkout/initialize` path accepts owner email without an equivalent email-verification proof before payment. The goal's registration → email verification → payment sequence is therefore not proven for direct purchases. Existing-account credentials are preserved during provisioning; that does not establish pre-payment email ownership.

## Real provider and customer gates — OPEN

Real iyzico merchant acceptance, real SMTP/inbox delivery, staging acceptance, fresh verified backup/rollback, and the Santa/Farma/Demo3 acceptance scenario remain required. No production release is authorized by this source audit.
