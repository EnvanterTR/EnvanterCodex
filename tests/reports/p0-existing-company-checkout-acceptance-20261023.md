# P0 existing company annual checkout — 23 October 2026

## Scope

Backend candidate only. The customer purchase form/entry point/browser flow and real provider/staging/production acceptance remain open. No production deployment was performed.

## Implementation

- Separate authenticated context/initialize/order-status routes, DB owner membership and confirmed email, CSRF, checkout request limit, current password and Authenticator/recovery-code verification. Password verification does not reset failed MFA attempts before the second factor passes.
- Existing-company purchase identity uses endpoint metadata, so an equivalent trailing slash cannot turn the handler into anonymous new-company mode. Company/portal/owner email are resolved on the server.
- Immutable target company/requesting owner UUIDs with an additive migration and paired annual-context DB constraint. No target FK can clear that purchase purpose upon deletion.
- Shared provisioning service locks the target company inside the payment transaction. It rechecks owner/company/capacity, preserves history/data and creates one annual Active subscription with the verified payment reference. Existing prepaid same-plan days survive renewal. Active prepaid plan changes are rejected.
- Normal expired-trial writes remain blocked. Only exact normalized checkout initialization and provider callback/webhook routes bypass the subscription write lock; handler authorization/provider validation still apply.

## Evidence

- `existing-company-checkout-first-failure-20261023.log`: first fixture failed at a 41-character plan code against the existing 40-character DB bound. New checkout behavior was not reached. The code was shortened.
- `existing-company-checkout-rate-limit-20261023.log`: 6/7 passed; sixth request correctly received 429 instead of the test's expected 403. The final test independently checks both the exhausted limiter and a fresh-host non-owner rejection.
- `existing-company-checkout-20261023.log`: exit 0, 8/8 passed, 1.7565 minutes. Six new cases cover active/expired trial conversion, prepaid renewal, fresh owner/password/MFA/capacity and rate-limit boundaries, owner revocation and capacity reduction after provider initialization. Two existing cases cover cross-subdomain sessions and anonymous rejection at every protected API route. Each of the three successful purchase cases also checks canonical/trailing-slash retry, server-derived identity, protected order access, anonymous purchase-ID reuse rejection, sequential callback replay, preserved own/other-company asset IDs, preserved previous dates and exactly one active subscription/audit event.
- The successful fixtures assert `HasPendingModelChanges() == false` after applying migrations to the disposable database. Web build: 0 warnings/0 errors; acceptance build: four existing deprecation warnings/0 errors.
- `existing-company-checkout-regression-20261023.log`: exit 0, 3/3 passed, 37.7785 seconds. Existing anonymous checkout still rejects missing CSRF/unconfigured provider; server-priced signed/retrieved new-company payment still provisions exactly once; production-baseline schema upgrades through the new migration without losing company assets.
- `p0-existing-checkout-source-checks-20261023.json`: 78/78 standalone CI source checks passed on the candidate.

The tests use disposable PostgreSQL and synthetic payment responses, not a merchant payment or customer database. Known framework warnings concern existing obsolete TestServer APIs, ephemeral DataProtection keys and unsupported TestServer request-body-limit features.

## Remaining acceptance

Customer purchase UI/browser, concurrent existing-company callbacks, target deletion at runtime, existing-company failed/mismatched provider responses, invalid MFA accumulation/lockout, broader plan-change rules, real iyzico/SMTP, staging and the fresh backup/rollback production gates remain separate. Sequential replay, a no-FK metadata assertion and existing new-company tests do not establish these cases.
