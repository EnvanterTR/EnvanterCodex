# Existing company annual checkout page — 23 October 2026

## Result

Server-rendered checkout page and form integration passed the targeted acceptance suite. No production or staging change was made.

## Verified

- An authenticated company owner can open `/subscription/checkout` while the existing company's trial is expired. The rendered page uses the current company and owner context, displays an eligible annual Cloud plan and its monthly equivalent, and supports the selected language/theme.
- The checkout page is non-cacheable and sends a `no-referrer` policy. Password and Authenticator/recovery inputs are not echoed into the HTML. A non-owner company role receives `403`; no provider initialization occurs.
- The owner form submits under CSRF protection, verifies password and MFA, snapshots a 12-month TRY order against the existing company and requesting owner, then redirects to the synthetic iyzico checkout URL. The test asserts amount `4,200 TRY`, one provider initialization, `PaymentStarted`, and no new company from client-supplied slug data.
- Existing-company API acceptance remained green for trial, expired-trial, and prepaid conversion, replay, payment-time owner/capacity rechecks, and tenant-session isolation.

## Evidence

- `company-checkout-page-build-20261023.log`: Docker acceptance image built; web build had 0 warnings/0 errors. Test project had four existing ASP.NET deprecation warnings and 0 errors.
- `company-checkout-page-regression-20261023.log`: `FullyQualifiedName~Existing_company`, 9/9 passed, exit 0, 2.1029 minutes. PostgreSQL ran in the isolated `p0-checkout-page` Compose project; its container/network were removed after the run.
- `node tools/Verify-UiLocales.mjs`: verified 2,246 UI locale entries across Turkish, English, Russian, Ukrainian and Macedonian.
- `git diff --check`: passed.

## Not established

This is not browser automation or a real payment. Merchant credentials, iyzico's real card/callback behavior, SMTP delivery, browser return/status interaction, staging, fresh verified backup/rollback rehearsal, and the Santa/Farma/Demo3 release scenario remain open. These results do not authorize deployment or establish sales readiness.
