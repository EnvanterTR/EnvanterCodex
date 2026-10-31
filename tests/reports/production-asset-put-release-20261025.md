# Tenant-scoped asset PUT — production release

Candidate `124b2d1e93af2a13c393d68353c271671c856de8`, PR #25. Local isolated endpoint acceptance 2/2; full CI **40158737966 completed success, 3/3 jobs**. Terminal snapshot: `pr25-ci-124b2d1-20261025.json`.

## Preconditions

Exact candidate built and ran on the server in isolated staging with an independently restored DB, separate media/keys, no published ports and an internal network. Three Host-header health checks ready/121/0, anonymous asset GET/PUT/PATCH 401, Secure CSRF check passed. No new migration. Details: `asset-put-server-preparation-20261025.md`.

The source pin saved earlier was reloaded and retained exact 124b2d1. After the observation interruption, actual running production images were still 6198ef6 and staging remained 124b2d1; no new deployment had started. The pending-configuration banner was not mistaken for a failed persistence check.

Immediate pre-release recovery gates were refreshed:

- SQL dump `pre-web-v1-20261025T080433Z.dump`, archive listing/SHA passed.
- 140 MB bundle `/var/backups/envantertr/rollback-20261025T080518Z`, current 6198ef6 web/public images and private SQL/media/keys/environment, all hashes passed.
- File/image drill suffix `20261025T080618Z`, extraction/comparison/image identity passed.
- Separate restore DB `envantertr_restorecheck_20261025_0804` in the isolated staging DB container, restored with exit-on-error. All **95** public table counts equal current production.
- External apex/Farma health ready/121/0, exit 0; `put-immediate-predeploy-health-20261025.log`.

Evidence: `coolify-put-immediate-backup-20261025.png`, `coolify-put-immediate-bundle-20261025.png`, `coolify-put-immediate-drill-20261025.png`, `coolify-put-immediate-counts-20261025.png`.

## Deployment

Coolify **8eyikaefpne2tcfuetcloj3n**, started **08:08:22 UTC**, row links exact 124b2d1 and its asset PUT commit. **In progress**, not yet accepted as live. Follow the same operation; do not restart based on observation delay. No personal OTP requested.

After terminal success, verify actual image tags, DB health, external HTTPS/readiness, catalog/checkout boundaries and retained public assets. T0 for the 24-hour observation plan is the verified completed deployment, not this start time. Real Section 20 customer workflows and SMTP/payment acceptance remain open.

## Verified completed deployment

Coolify **8eyikaefpne2tcfuetcloj3n** completed **Success / Finished**. Actual production web/public image tags are full **124b2d1e93af2a13c393d68353c271671c856de8**, both container suffixes `20261025T080842`; database suffix `20261025T080841`, healthy. Completion was observed around 08:13 UTC. Evidence: `coolify-put-release-finished-20261025.png`, `coolify-put-live-images-20261025.png`.

Initial external health check passed apex but Farma timed out at 10 seconds. This was preserved in `put-live-readiness-20261025.log`, exit 1. An independent normal-TLS HTTP request at 08:14:38 UTC returned Farma 200/ready/121/0; the subsequent two-host Node probe also passed both hosts, exit 0 (`put-live-readiness-recheck-20261025.log`). No restart or rollback was triggered for that single observation timeout.

Public catalog remains 8 plans. The separate commercial probe returned expected exit 1 because checkout remains unavailable without provider credentials (`put-live-commercial-boundaries-20261025.log`); this is not successful payment acceptance.

All eight gallery/style/script/product image assets match local source SHA256 byte-for-byte (`put-live-assets-20261025.log`, exit 0). This preserves the user's three device photos, portrait-bearing security preview, portal and transparent roll; this check is delivery integrity, not a new interactive browser run.

Production release is complete. Full Hedef is not: actual Santa/Demo3 normal provisioning, customer workflows, live cross-tenant acceptance, SMTP recipient delivery and payment provider acceptance remain open. The 24-hour observation plan is defined, but 24 hours of executed observation are not claimed. No new personal OTP was requested and no MFA protection was disabled.
