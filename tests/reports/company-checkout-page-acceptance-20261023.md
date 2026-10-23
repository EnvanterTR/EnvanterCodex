# Existing company annual checkout page — 23 October 2026

## Result

Server-rendered checkout page and form integration passed the targeted acceptance suite. No production or staging change was made.

## Verified

- An authenticated company owner can open `/subscription/checkout` while the existing company's trial is expired. The rendered page uses the current company and owner context, displays an eligible annual Cloud plan and its monthly equivalent, and supports the selected language/theme.
- The checkout page is non-cacheable and sends a `no-referrer` policy. Password and Authenticator/recovery inputs are not echoed into the HTML. A non-owner company role receives `403`; no provider initialization occurs.
- The owner form submits under CSRF protection, verifies password and MFA, snapshots a 12-month TRY order against the existing company and requesting owner, then redirects to the synthetic iyzico checkout URL. The test asserts amount `4,200 TRY`, one provider initialization, `PaymentStarted`, and no new company from client-supplied slug data.
- Two parallel payment callbacks for one existing-company order both return the success page while leaving one `Active` subscription and one activation audit row. The existing-company callback transaction uses the order lock and shared company lock at `READ COMMITTED`; the separate new-company provisioning transaction remains `SERIALIZABLE`.
- The hosted Chromium acceptance suite completed locally with exit 0 after restoring the established English, Russian, Ukrainian and Macedonian expiry-notice opening sentences. Its checkout UI checks unavailable/error states, server-quoted annual pricing and coupon behavior, company slug suggestions/manual edits, CSRF/consent submission, and trusted iyzico redirect validation using the synthetic API.
- Existing-company API acceptance remained green for trial, expired-trial, and prepaid conversion, replay, payment-time owner/capacity rechecks, and tenant-session isolation.

## Evidence

- `company-checkout-page-build-20261023.log`: Docker acceptance image built; web build had 0 warnings/0 errors. Test project had four existing ASP.NET deprecation warnings and 0 errors.
- `company-checkout-page-regression-20261023.log`: after the callback serialization fix, `FullyQualifiedName~Existing_company`, 10/10 passed, exit 0, 2.1253 minutes. PostgreSQL ran in the isolated `p0-checkout-page` Compose project.
- `company-checkout-page-concurrent-callback-20261023.log` preserves the first race reproduction, where PostgreSQL reported `40001: could not serialize access due to concurrent update` on a concurrent existing-company replay. `company-checkout-concurrent-callback-20261023.log` is the fixed targeted test: 1/1 passed, exit 0, 1.0272 minutes. The isolated Compose database/network were removed after acceptance.
- `company-checkout-hosted-chromium-20261023.log`: hosted Chromium suite exit 0; synthetic API, no real card or merchant callback. Prior remote run `40004284465` had only the Linux Chromium step fail; the three current uncommitted fixes require a new full remote CI run.
- `node tools/Verify-UiLocales.mjs`: verified 2,246 UI locale entries across Turkish, English, Russian, Ukrainian and Macedonian.
- `git diff --check`: passed.

## Not established

The hosted Chromium run uses the synthetic API and does not exercise the newly added existing-company owner checkout form end to end. Real iyzico card/callback behavior, SMTP delivery, browser payment return/status interaction, staging, a fresh verified backup/rollback rehearsal, and the Santa/Farma/Demo3 release scenario remain open. These results do not authorize deployment or establish sales readiness.
