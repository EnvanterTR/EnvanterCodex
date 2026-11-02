# Platform MFA retry and lockout follow-up — 2026-11-02

## Finding and fix

- Rebuilt the candidate app image from this branch and reran the isolated MFA browser acceptance.
- A replayed recovery code exposed a server-side `DbUpdateConcurrencyException` (HTTP 500): Identity updated the user's concurrency stamp before the failed-login audit was saved through the same context.
- Failed-login audit is now persisted before `AccessFailedAsync` mutates the Identity user. Password lockout counting remains enabled explicitly after the audit; failed MFA attempts use the same safe ordering.

## Verification

- `deploy/testing/run-platform-mfa-browser-acceptance.ps1` against a freshly built branch image: **PASS**. Covers synthetic platform bootstrap, local QR, MFA setup, session reload, one-time recovery, replay rejection, 429 persistence, fresh TOTP login, and zero external requests. Temporary Compose resources were removed.
- `deploy/testing/run-platform-mfa-acceptance.ps1`: **2/2 PASS** on a disposable PostgreSQL database, including `Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use` and platform first-login MFA setup. Temporary database/network were removed.
- Web and acceptance projects built successfully in Docker. Existing ASP.NET deprecation warnings are in `TrustedClientIpTests.cs`; no build errors.
- `git diff --check`: PASS.

## Scope and release

- No production account, MFA code, database, or service was accessed or changed.
- Committed as `1477a63` and pushed to the existing PR #31 draft branch for review. It is not merged or deployed.
