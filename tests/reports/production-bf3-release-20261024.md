# Platform fresh-login proof release — 24 October 2026

Candidate: bf3ed6de538423ed8744047a08e4e7f3d2762c5d. Coolify operation: is1feyzg0b9enmg0uovqtbtb.

## Verified release gates

- GitHub run 40102471823 completed with all three jobs successful; `pr25-ci-bf3ed6d-20261024.json` records terminal jobs. The previous failed 8db run and its test-fixture correction remain documented.
- `git diff 8db0343 bf3ed6d -- src deploy/coolify Dockerfile` was empty. Runtime/product sources match the actual isolated 8db staging image. Staging was rechecked ready/120/0 before this deployment. The later demo feature is separate and not in this candidate.
- New production custom dump `pre-web-v1-20261024T201739Z.dump`: archive list and SHA verification passed.
- Fresh root-private 140 MB rollback bundle `/var/backups/envantertr/rollback-20261024T201929Z` contains database, media, keys, exact old app images and private settings. File/image restore succeeded in separate `-drill-20261024T202113Z` directory.
- Fresh dump restored with exit-on-error into `envantertr_restorecheck_20261024_2017` on the separate restore container. All 95 public-table counts matched current production. No raw customer content exported to the repository.
- No schema change in this release; source and deployed schema remain 120 migrations. No production migration command executed.
- Saved Coolify SHA reloaded as bf3ed6d before deployment. The deployment log confirmed `commit sha bf3ed6de538423ed8744047a08e4e7f3d2762c5d`, even though a later demo commit has since been pushed for separate CI.

## Current result

Deployment finished successfully (Coolify green Success/Finished). Actual web and public-site container image tags both match bf3ed6de538423ed8744047a08e4e7f3d2762c5d; new container suffix `20261024T202457`, PostgreSQL healthy. External HTTPS apex and Farma health returned 200/ready, schema 120/0 (`bf3-live-readiness-20261024.log`, exit 0). Public catalog retained eight plans. The full sales probe still exits 1 because checkout is unavailable (`bf3-live-sales-boundaries-20261024.log`); no sales readiness claim.

Live customer/admin sign-in with a newly entered second factor was not exercised after deployment. Cookie/proof behavior was tested in the actual Identity/PostgreSQL suite and deployed image identity was verified. No OTP was requested or reused to claim additional live acceptance.

Screenshots: `coolify-bf3-fresh-restore-20261024.png`, `coolify-bf3-rollback-stage-ready-20261024.png`, `coolify-bf3-source-persisted-20261024.png`, `coolify-bf3-deployment-started-20261024.png`.

Completion screenshots: `coolify-bf3-deployment-finished-20261024.png`, `coolify-bf3-live-images-20261024.png`.

## Scope

Successful platform-only password+MFA login reuses the existing 600-second freshness window. Company-bound/password-only sessions get no proof; the timeout is not widened. Existing old sessions do not receive new proof retroactively. Full customer acceptance, actual SMTP/iyzico and sale readiness remain open. The separate 5790980 demo-company feature has local 11/11 API, desktop/mobile browser and 79 source checks; its own full CI is running and it is not deployed here.
