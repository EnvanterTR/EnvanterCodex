# Demo-company candidate real staging — 24 October 2026

Exact candidate: 5790980f8f8a298b720287260a37b7f972bd1e57. Existing authenticated Coolify server terminal used; no new SSH/API credentials.

## Completed

- Source archive for the exact commit built on the server with Docker, exit 0. Directory `/var/lib/envantertr-staging/5790980-20261024`; image `envantertr-staging:5790980`.
- Actual app container `envtr-stage-web-5790980` started on the existing internal `envtr-stage-ae875b6` staging network, with no published ports. Media/keys are separate copies of the prior staging files; the connection uses the existing staging clone, not production. No production volume was mounted.
- Readiness for apex, Farma-host and Santa-host returned ready, 120 migrations, 0 pending. Host health is not proof that a Santa tenant exists.
- The served platform HTML includes the new `provisionDemoCompany` control.
- Anonymous `/api/assets` and apex `/api/platform/companies` returned 401. CSRF endpoint returned a request-token response; token was not printed.
- Staging SMTP/iyzico credentials remain blank and the network remains internal. No actual email/payment delivery was attempted.

Evidence: `coolify-demo-stage-ready-20261024.png`, `coolify-demo-stage-security-20261024.png`. Local actual Identity/PostgreSQL 11/11 and desktop/mobile Chromium evidence is in `platform-demo-provisioning-acceptance-20261024.md`.

## Open gates

Run 40129797684 is still live: Linux security and Windows deployment/backup successful, PostgreSQL acceptance in progress. No restart was triggered. This candidate has not been deployed; production remains the separately verified bf3ed6d MFA release. Before a later demo production release, take a fresh backup/restore package containing the currently running bf3ed6d app images. The earlier bundle from the MFA release contains the older c328 images and must not be mistaken for the immediate prior-version rollback.

No authenticated customer demo creation was performed on staging or production in this step. Actual Santa/Farma/Demo3 acceptance, SMTP inbox and iyzico remain open. Full Hedef completion is not claimed.
