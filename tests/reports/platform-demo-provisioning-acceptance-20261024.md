# Reasoned platform demo-company provisioning — 24 October 2026

## Requirement and change

Hedef 9.5/9.6 and 20.9 require an administrator to create a new demonstration company through the normal product flow. Previously the company-create form required a paid-sale attestation; granting a demo only worked after a company already existed. No fictitious payment was recorded to work around that gap.

The existing protected `/api/platform/companies` route now supports an explicit demo mode. It uses the same serializable transaction, slug reservations and `TenantProvisioningService` as the paid flow. A demo requires a reason (8–1000 characters), 1–60 days (UI default 15), and an active Cloud plan. Payment verification or a nonblank payment reference is rejected in demo mode. The subscription is Trial/IsDemo with no payment reference, not a paid subscription. Existing paid input still requires payment attestation and preserves its previous behavior.

The platform form offers a separate demo action, with no payment inputs. Platform MFA freshness, CSRF and authorization remain required. New owners remain unconfirmed and receive the normal invitation; existing owners keep their password, security stamp and existing memberships. Email failure remains visible and does not pretend that delivery succeeded. The account timeline includes the reasoned demo provisioning audit.

## Evidence

- Actual PostgreSQL/Identity-cookie API acceptance: **11/11 passed**, exit 0, 2.1207 minutes (`platform-demo-provisioning-final-20261024.log`). Includes new/existing owner, exact 15-day period, no checkout/payment record, duplicate slug rejection, reason/payment/duration rejection, company-admin denial, MFA and CSRF denial, timeline entry, both paid provisioning/invitation-retry cases, existing initial provisioning and fresh MFA login writes.
- Earlier 10-case run passed before the final company-admin/timeline coverage: `platform-demo-provisioning-20261024.log`.
- Real Chromium on actual platform HTML with synthetic HTTP fixtures: desktop and 390×844 mobile passed, exit 0 (`platform-demo-browser-final-20261024.log`). Checks paid form proof controls, demo field separation, defaults/limits, missing-reason no-write, precise CSRF request, modal close, mobile dimensions/scroll/cancel and existing management regressions. This browser fixture does not prove actual database persistence; that is covered separately by the API suite.
- Screenshots: `platform-demo-provisioning-form-20261024.png`, `platform-demo-provisioning-mobile-20261024.png`.
- All **79 CI source commands passed**, including inventory check after regenerating `docs/ui-inventory.md` (`platform-demo-all-source-fixed-20261024.log`). The first run reported stale inventory; its log is retained. The source verifier now recognizes an explicit demo mode while verifying that paid checkout still uses the default non-demo request.
- No migration added. No actual customer credential, private cookie, OTP or provider secret used in these tests.

## Release boundary

This demo change is local acceptance only at this point. It is not part of the separate bf3ed6d MFA-only release, whose three CI jobs passed. Demo needs its own full CI, real staging and release gates before deployment. No Santa/Demo3 production record has been created by this work, and Hedef Section 20 remains incomplete. Real SMTP and iyzico acceptance are still required for sales readiness; this administrator demo flow does not replace public email verification or payment verification.
