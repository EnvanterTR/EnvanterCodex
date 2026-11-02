# MFA throttling recovery regression — 2026-11-01

## Change

Extended `deploy/testing/platform-mfa-browser/platform-mfa-first-login.mjs` to simulate a same-origin HTTP 429 response after the platform MFA challenge has been issued. The acceptance now checks that the Authenticator field remains visible after the throttled response and after a page reload, then removes the mock before completing login with a fresh synthetic TOTP.

## Safety and current verification

- Uses only the test's disposable synthetic admin and a mocked 429 response; no production account, password, code, API, or database is accessed.
- `node --check deploy/testing/platform-mfa-browser/platform-mfa-first-login.mjs`: PASS.
- `git diff --check`: PASS.
- `dotnet build src/EnvanterTR.Web/EnvanterTR.Web.csproj --no-restore`: PASS, 0 warnings / 0 errors, 2m17s.
- Local full disposable PostgreSQL/browser acceptance could not run in this Windows session because Docker Engine named pipe `//./pipe/dockerDesktopLinuxEngine` is unavailable.
- GitHub Actions run `41332041275` on commit `1e72c47ef30876c51a765db028b27a9a82213470` completed **success, 5/5**. The Platform MFA browser acceptance job passed in 2m23s and executed the new mocked-429 plus reload assertions; the other four workflow jobs also passed.
- This is source/CI acceptance only. The PR remains unmerged and the change is not deployed.
