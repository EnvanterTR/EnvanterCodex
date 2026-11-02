# Platform password recovery UI — 2026-10-21

Platform login lacked a recovery entry despite the existing shared protected recovery endpoint. Added accessible email-prefilled dialog, explicit cancel/close, fresh CSRF request, duplicate-submit guard, generic server acceptance, visible errors and stale-response isolation after closing. Existing MFA is not disabled or changed.

Actual HTML Chromium acceptance in isolated offline Docker passed: cancel no write; one email-only POST with CSRF; duplicate submit blocked; generic acceptance;503 surfaced; input retained;390px fits. Synthetic endpoint, no real email or password change. Added test to full hosted suite and Dockerfile. UI inventory2246matches, platform source verifier and diffcheckPASS. Real SMTP acceptance remains open.

Extended acceptance adds invalid-email no-write and close/reopen while response is pending. Initial test insertion placed the stale-response scenario before a zero-write assertion and failed1!=0; scenario ordering corrected once. RerunPASS, no product behavior change. No unbounded polling/retry.
