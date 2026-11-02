# Platform MFA challenge persistence — 2026-11-01

**Status:** Targeted browser regression PASS; full isolated first-login acceptance remains unverified.

## Change

The platform login previously hid and cleared the MFA field whenever either the email or password input changed. After a failed attempt, correcting a mistyped password could remove the required Authenticator/recovery-code field. The page now resets the MFA challenge only when the account email changes; editing the password or retrying the factor keeps the challenge visible.

## Evidence

- Actual platform-admin.html loaded in local Microsoft Edge through Playwright.
- Synthetic same-origin API responses only; no real account, password, MFA code, database, or production endpoint was used.
- A synthetic login response requiring a second factor displayed the MFA field.
- Editing the password kept the MFA field visible.
- Changing the account email hid/reset the account-specific MFA field.
- Result: **PASS**.
- node --check deploy/testing/platform-mfa-browser/platform-mfa-first-login.mjs: exit 0.
- git diff --check: clean.
- Full disposable PostgreSQL/browser acceptance could not start because Docker Engine is unavailable (npipe:////./pipe/dockerDesktopLinuxEngine); do not treat this targeted smoke as the full test.

## Release status

This branch is not merged to main and is not deployed. Production authentication was not attempted.
