# Checkout published user quota — 24 October 2026

Result: **PASS**, targeted Chromium command exit **0**; **30/30 combinations** (five languages × six quota states).

## Problem and change

The checkout summary always advertised unlimited Cloud users, even when the published plan limited login users. A missing plan description also fell back to the word "Unlimited". This contradicted the approved commercial tiers.

The summary now displays the API's positive integer `maxLoginUsers`. Only explicit `null` means unlimited. A missing/invalid quota shows `—`, and a missing description remains empty. No price, entitlement enforcement, payment request or customer data is changed by this display fix.

## Evidence

- Before: `checkout-quota-before-20261024.log` records Chromium rejecting a four-user plan displayed as `Sınırsız`; exit 1.
- After: `checkout-quota-after-20261024.log` records PASS; exit 0. Actual checkout HTML/JS runs in Chromium with synthetic published-plan/status APIs.
- Values `1`, `4`, `10`, `2000`, `null` and an omitted limit are checked in Turkish, English, Macedonian, Russian and Ukrainian. Viewing these plans produces zero payment POSTs and zero JavaScript errors.
- An interim test compared Node's number separators with Chromium's. Their ICU locale support differs for Macedonian (`2.000` / `2,000`). The test now verifies the displayed numeric entitlement independently of separator choice. That test failure is retained in `checkout-quota-icu-20261024.log`; the product requirement was not weakened to permit an incorrect quota.
- `node tools/Verify-HostedCheckoutUi.mjs`, `node tools/Verify-HostedCheckoutBrowserAcceptance.mjs` and `node tools/Generate-UiInventory.mjs --check` passed (2,269 controls).
- `docker build --tag envantertr-checkout-quota-dotnet:local --file deploy/testing/Dockerfile.security-acceptance .` succeeded, exit 0, reusing unchanged C# build layers. Output: `checkout-quota-dotnet-build-20261024.log`.
- `git diff --check` passed.

The targeted browser script is included in the existing hosted Chromium CI suite. The preceding remote run `40035243704` still targets commit `dfdb36e`; its Windows job passed while both Linux jobs were running at the last check. This package is being committed locally before the next batched PR update to avoid canceling that active run.

This is a checkout display correction and synthetic browser acceptance. It does not prove real payment, SMTP, staging or live customer acceptance. The direct-purchase email-verification gap remains open.
