# Reasoned demo provisioning release — 24 October 2026

Exact candidate: 5790980f8f8a298b720287260a37b7f972bd1e57. Coolify operation: esw998lnesfol9swvgxwq3li.

## Verified gates

- GitHub run 40129797684: all three jobs terminal success (`pr25-ci-5790980-20261024.json`). Local 11/11 API, desktop/mobile Chromium and 79/79 source checks are separately recorded in `platform-demo-provisioning-acceptance-20261024.md`.
- Exact candidate built on the server with exit 0 and ran in isolated staging. Three host health probes ready/120/0, actual demo form bundled, anonymous protected APIs 401, CSRF response present; no host ports, copied staging media/keys and cloned database, provider credentials blank. See `platform-demo-staging-20261024.md`.
- Fresh production custom dump `pre-web-v1-20261024T204650Z.dump`: archive list and SHA verified.
- Fresh private 140 MB bundle `/var/backups/envantertr/rollback-20261024T205007Z` captured the actual immediate previous bf3ed6d web/public images, media, keys, SQL and private environment. File/image restoration passed in separate `-drill-20261024T205224Z` directory. Image references verified as bf3ed6d, not older c328.
- Fresh dump restored with exit-on-error into `envantertr_restorecheck_20261024_2046` on the separate restore container; all 95 public-table counts equal current production. Raw data/count files retained privately on the host.
- No migration change; source schema remains 120. No production migration command executed.
- Coolify saved/reloaded the exact 5790980 SHA before deployment. Deployment log confirms that exact commit. Later local evidence commits are not silently substituted for the tested runtime source.

## Current result

Deployment completed successfully: Coolify green Success/Finished. Actual web and public-site image tags both match 5790980f8f8a298b720287260a37b7f972bd1e57; container suffix `20261024T205706`, PostgreSQL healthy. Post-release checks on 25 October (Europe/Istanbul): apex and Farma HTTPS 200/ready, 120 migrations, 0 pending (`demo-live-readiness-20261025.log`, exit 0). The delivered platform HTML contains the new demo action, explicit demo payload and 15-day default (`demo-live-form-20261025.log`). Public catalog retains eight plans; full sales probe remains exit 1 solely for unavailable checkout (`demo-live-sales-boundaries-20261025.log`).

No authenticated production company creation is claimed by these probes. The existing platform browser session was checked after reload and showed the login form; no new password/MFA attempt, OTP request or secret extraction was made.

Evidence screenshots: `coolify-demo-release-fresh-restore-20261024.png`, `coolify-demo-release-rollback-20261024.png`, `coolify-demo-source-persisted-20261024.png`.

Completion screenshots: `coolify-demo-deployment-finished-20261024.png`, `coolify-demo-live-images-20261025.png`.

## Boundaries

This makes a reasoned, time-limited unpaid company available to a freshly authenticated platform administrator through the shared provisioning service. It does not create an actual Santa/Demo3 company automatically, confirm a new owner's email, set the user's password or simulate a paid sale. Section 20 customer acceptance and actual SMTP/iyzico are still open. A green deploy or static control marker is not authenticated demo provisioning acceptance.

Before this release, the requested three carousel images, portal preview, green account-security preview, transparent label roll and gallery JS/CSS were served with hashes matching the source files. `bf3-live-product-gallery-integrity-20261024.log` records HTTP/byte/hash checks and HTML hooks. This is delivery integrity evidence, not a new interactive carousel/fullscreen browser run.
