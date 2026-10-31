# Exact admin entry server preparation — 27 October 2026

Candidate f073752a40072bdc0a515f3156bfc785dbaffe0d. CI40458121020 / PostgreSQL122036098775 started; no success claim yet.

Separate image build PID1453548 was confirmed live; most recent output reached EnvanterTR.Web.dll during dotnet publish, no build.exit yet. The existing process was not restarted.

Fresh production dump pre-web-v1-20261027T060041Z.dump was checksum/list validated and restored into separate envantertr_stage_f073752. Schema122 and fingerprints for95 original data tables were recorded privately. Separate media/key/env copies prepared; credentials not copied into repository. Screenshot terminal output confirmed FRESH_BACKUP_AND_ISOLATED_CLONE_READY. No production restore, migration or deployment.

Next: inspect same build, start isolated app/proxy, run prepared HTTP/admin redirect/tenant denial/runtime/preservation checks, then fresh rollback and exact CI gates before release. Prepared recipes are not proof of passed checks.

## Exact candidate isolated acceptance completed

Both f073752 app and public-site image builds exited0. Dedicated envtr-stage-web-f073752/public-f073752 containers started without host ports on the internal staging network. Runtime guard assertions passed: read-only application, isolated clone/media/key mounts, SMTP/payment/bootstrap/trials/reminders disabled.

Actual app+Nginx probes passed: apex/www /admin, /admin/ and query forms302 with relative /platform-admin.html Location and no Set-Cookie; Santa/Farma equivalents404/noLocation. All existing three-host schema122/0 readiness, anonymous assets/PDF/email/onboarding401, Secure CSRF, reset feedback, onboardingCSS, tenant platformHTML/JS/API/reauth404 and root platform login checks passed. These are real isolated source builds, not the earlier local proxy stub.

After startup/probes, all95 original table field fingerprints equaled the pre-start restored baseline, schema122 unchanged. Screenshots: f073-stage-http-pass-20261027.png; f073-stage-preservation-pass-20261027.png. Raw customer fingerprints remain private on server.

CI40458121020 PostgreSQL122036098775 was visibly running at9m4s. FullCI, fresh pre-release independent SQL/file/image recovery and production release remain open. Recovery recipe prepared with current90c image guard but not executed before CI.

TAMAM dediğim1 izole sunucu kabul paketinin1'i gerçek HTTP/runtime/veri karşılaştırması çıktısıyla doğrulanmıştır. Genel Hedef tamamlanmadı.

## Initial exact-candidate recovery drill passed

Current90c SQL/media/data-protection keys and web/public images were bundled under /var/backups/envantertr/rollback-20261027T062301Z (143MB). The source SQL dump is the already validated/restored060041Z snapshot. prepare-rollback-bundle and verify-rollback-bundle both exited0; file/image recovery marker verified. Screenshot f073-stage-recovery-pass-20261027.png. Production was not restored or deployed. This is the initial staging recovery gate, not the final fresh pre-deploy snapshot. CI40458121020 PostgreSQL still running at28m18s; two other jobs passed.
