# Cloud trial browser acceptance — 2026-10-23

- Status: PASS with a local synthetic API and Chromium; real SMTP delivery and live route remain unverified.
- Commands: `docker build --tag envantertr-hosted-checkout-browser-acceptance:local --file deploy/testing/Dockerfile.hosted-checkout-browser .` then `docker run --rm --init --entrypoint node envantertr-hosted-checkout-browser-acceptance:local cloud-trial.mjs`.
- Result: `PASS: trial registration fails closed, preserves custom slugs, sends one CSRF/consent POST, supports 5 languages/390px, requires explicit email confirmation, removes fragment proof from address, and rejects external portal links. SyntheticAPI.`
- Verified: disabled state does not submit, company names generate expected slugs, manual slug survives name edits, repeated submit does not duplicate requests, all five locales render, 390px viewport does not overflow, email proof is removed from the URL and only submitted after explicit confirmation, and an untrusted external portal URL is rejected.
- Limitation: browser calls are intercepted by a local synthetic API. This does not prove live SMTP delivery, production enrollment, or a live tenant host.
- Evidence: this report and the command output from the acceptance run.
