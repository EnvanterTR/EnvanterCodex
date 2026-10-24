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
