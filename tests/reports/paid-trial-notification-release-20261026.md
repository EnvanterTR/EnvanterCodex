# Paid/trial notification release — 26 October 2026

Candidate: `56e40838f702063203178915145770de7010a009`. Changes: new paid-company provisioning rolls back on access-email failure; trial confirmation requires its access email before committing. No schema migration or frontend asset change. Existing payment failure/retry evidence and SMTP external-atomicity limits remain in paid-provisioning-email-20261026.md and trial-welcome-20261026.md.

## Release gates passed

- Exact CI run40331368345: PostgreSQL tenant acceptance successful25m, Linux tenant security successful6m, Windows deployment/backup safety successful4m. Evidence:56e-ci-checks-pass-20261026.log and56e-ci-pass-20261026.png.
- Server build of exact Git archive56e: exit0; image envantertr-staging:56e4083. Independent internal-network/read-only stage envtr-stage-web-56e4083, isolated SQL clone envantertr_stage_56e4083 and copied media/keys; outbound SMTP/payment/bootstrap disabled. Scripts56e-prepare-isolated-stage,56e-stage-runtime-guards,56e-stage-http-probe,56e-stage-preservation preserve the execution recipe.
- Fresh SQL dump pre-web-v1-20261026T134409Z.dump validated by pg_restore listing and SHA256, restored into the clone. Stage ready122/0 for apex/Santa/Farma Host headers; anonymous asset mutation/PDF/email endpoints denied, CSRF secure cookie present. All95 original data-table column projections identical after startup/probes. No new migration. Evidence:56e-stage-acceptance-pass-20261026.png.
- Rollback package /var/backups/envantertr/rollback-20261026T135152Z,143MB, prepared from actual previous5018 images/media/keys and the fresh SQL dump; preparation exit0. Independent file/image recovery drill135452Z exit0, no production data restored. Evidence:56e-backup-and-build-20261026.png and56e-rollback-drill-pass-20261026.png.

## Deployment started, not yet proven complete

Saved exact56e source SHA in Coolify and reloaded to verify persistence. Generic pending-configuration banner is not proof of failed save. One deployment0woxca0tgg33mdcozvfyga2q started14:03:36UTC; log confirms importing full56e SHA. Completion, actual runtime image provenance and external post-release boundaries remain to be recorded below. Saved production callback URL will also apply on this deployment; missing real merchant keys must keep payment closed.

No real payment, new customer registration, signature or customer email was initiated by this release verification. Real merchant acceptance is ENGELLENDİ: user explicitly has no iyzico account. Five-step onboarding and real Santa acceptance remain open; this release is not a sales-readiness claim.

## Completed deployment and verification

Deployment0woxca0tgg33mdcozvfyga2q finished Success, duration4m37s. Actual web/public containers suffix20261026T140404 both run exact56e image tags with zero restarts; matching database is healthy. Web read-only rootfs, relay TLS587/no SMTP AUTH, trials false, missing iyzico keys and saved HTTPS callback applied were verified14:10:51UTC. Public-site rootfs is not configured read-only in Compose; the initial verification incorrectly asserted it and failed, then the check was corrected to the actual web-only restriction. No runtime protection was changed to make that check pass. Final script56e-live-runtime-guards-20261026.py passed; screenshot56e-live-runtime-pass-20261026.png.

External apex/Santa/Farma HTTP200ready122/0 passed. Public8plans remain available; checkout and trial registration unavailable. Anonymous PDFGET/emailGET+POST401. All8existing gallery CSS/JS/image files match source SHA256, and live Santa company HTML equals candidate. Read-only probes only, no new real mail or registration. Evidence56e-live-external-health-20261026.log,56e-live-boundaries-20261026.mjs/.log,56e-coolify-release-finished-20261026.png. These are immediate observations, not continuous uptime or real merchant/inbox acceptance.
