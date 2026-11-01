# MFA throttling recovery regression — 2026-11-01

## Change

Extended `deploy/testing/platform-mfa-browser/platform-mfa-first-login.mjs` to simulate a same-origin HTTP 429 response after the platform MFA challenge has been issued. The acceptance now checks that the Authenticator field remains visible after the throttled response and after a page reload, then removes the mock before completing login with a fresh synthetic TOTP.

## Safety and current verification

- Uses only the test's disposable synthetic admin and a mocked 429 response; no production account, password, code, API, or database is accessed.
- `node --check deploy/testing/platform-mfa-browser/platform-mfa-first-login.mjs`: PASS.
- `git diff --check`: PASS.
- `dotnet build src/EnvanterTR.Web/EnvanterTR.Web.csproj --no-restore`: PASS, 0 warnings / 0 errors, 2m17s.
- The full disposable PostgreSQL/browser acceptance cannot run in this Windows session: Docker Engine named pipe `//./pipe/dockerDesktopLinuxEngine` is unavailable. CI is required to execute this new scenario.
- This report describes added coverage, not a passed run. It is not deployed.
