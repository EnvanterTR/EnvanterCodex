# P0 hosted Chromium acceptance — 24 October 2026

## Result

- Status: PASS for the hosted synthetic Chromium suite; not a live customer or payment acceptance.
- Command: `deploy/testing/run-hosted-checkout-browser-acceptance.ps1`
- Exit code: `0`.
- Result lines: **74/74 PASS** in `p0-hosted-browser-acceptance-20261024.log`.
- Browser image: `mcr.microsoft.com/playwright:v1.64.0-noble`.

## Checkout and onboarding coverage

The suite opens the actual public checkout HTML and JavaScript in Chromium against synthetic APIs. It verifies checkout fields stay disabled and no personal/billing data is sent while payment is unavailable; annual/monthly selection and server-quoted coupon display; company slug suggestions for Farma, Santa, and Celal; preservation of a manually edited slug; required company data validation; CSRF and data-processing consent; omission of client-supplied price/card data; and redirect only to the trusted sandbox provider host.

The hosted package also passed browser checks for trial registration and tenant slug UX, public pricing empty/error states and five locales, company portal/session flows, platform admin UI, assets, reports, licenses, employee, purchasing, support, and scheduled reports. The log contains the per-flow PASS output.

## Limits

- APIs and checkout provider are synthetic. No email was sent and no payment was charged.
- The generic new-company checkout browser scenario does not prove the separate existing-company `/subscription/checkout` purchase/return flow end to end.
- This suite does not log in to real Santa/Farma accounts, prove live tenant isolation, or exercise Coolify/staging, a fresh production backup, or rollback.
- The browser suite does not verify live sales readiness. The read-only live probe still reports no published plans, checkout unavailable, and the trial status route missing.

## Evidence

- Full output: `tests/reports/p0-hosted-browser-acceptance-20261024.log`.
- Source test: `deploy/testing/hosted-checkout-browser/hosted-checkout.mjs` and other `.mjs` files invoked by that package.
- No production data or settings were changed.
