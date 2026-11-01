# Reuse fresh platform login proof — 24 October 2026

Successful platform-only login has already checked the password and enabled second factor. It now issues `platform_reauth_at` at that same time, reusing the existing 600-second critical-operation window. This avoids a second identical challenge immediately after sign-in. No timeout, MFA policy, permission or session duration was widened.

Company-bound logins and password-only platform logins receive no proof. Existing sessions without the claim still require explicit reauthentication. Security-stamp refresh preserves the original timestamp rather than refreshing it.

## Acceptance

Docker PostgreSQL/actual Identity-cookie suite: 15/15 passed, exit 0, 5.6644 minutes. Log: `platform-fresh-login-rerun-20261024.log`.

- Authenticator and one-use recovery-code platform logins authorize consecutive audited catalog writes without a separate reauthentication request.
- Recovery code consumption checked through a fresh database scope.
- A platform administrator signing into a company receives no platform freshness proof and cannot write the catalog.
- An expired signed proof and an MFA-less platform login remain rejected.
- Existing support impersonation scope, actor audit, revocation and return tests pass.

First run: 3/4 passed. The recovery assertion read a tracked identity token from the pre-login test scope; changing that assertion to a fresh scope verified the persisted consumption. No product recovery-code logic was changed. Original log retained.

`Verify-AuthErrorFlows.mjs`, `Verify-PlatformMfaBrowserAcceptance.mjs` and `git diff --check` passed. No migration added. These results are local acceptance, not live deployment or actual provider acceptance.

## Full-suite fixture collision and correction

The first complete CI run 40098994968 failed 1 of 327 tests (326 passed): this freshness fixture left fixed annual catalog codes in the shared disposable database, causing the existing catalog test's seed insert to collide. The production change itself was not modified. Terminal job evidence is `pr25-ci-8db0343-20261024.json`; failure excerpt is `ci-8db-catalog-collision-20261024.log`.

The freshness test now creates two unique, unpublished plans through the real protected API, checking both persisted plans and both actor-scoped `PlanCreated` audit records. It does not populate or alter the fixed annual catalog. The initial unique code exceeded the API's 40-character limit: the intermediate combined run was 14/16; shortening the synthetic prefix fixed that test input without relaxing validation.

Clean disposable database combined run: **16/16 passed**, exit 0, 3.4624 minutes, including the existing annual catalog test, both fresh-login cases, company-login denial and all support impersonation cases. Evidence: `platform-fresh-login-catalog-regression-fixed-20261024.log`; intermediate failed log retained as `platform-fresh-login-catalog-regression-20261024.log`. Full CI must pass before deployment. Product/runtime sources still match the staged 8db0343 candidate.
