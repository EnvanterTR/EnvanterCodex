# Catalog image release — 29 October 2026

Candidate: `68f4d883aba934e71e927ee33b504542c44d40cd`.

## Passed before deployment

- Catalog/accessory image focused acceptance: 12/12, 2.1442 minutes.
- CI40921546389: all three jobs succeeded. Linux security6m22s, PostgreSQL43m30s, Windows5m10s. No inferred final PostgreSQL case count.
- Exact private stage `/var/lib/envantertr-staging/68f4d88-20261029`: build.exit0 and acceptance-probes.exit0. Runtime isolation, three-host HTTP boundaries and95 original table field projections preserved; schema122/0.
- Fresh release SQL `pre-web-v1-20261029T181643Z.dump`: hash/list/independent restoration passed, schema122/95tables.
- Recovery bundle `/var/backups/envantertr/rollback-20261029T181653Z` (143MB): previous c386113 SQL/media/keys/images independently recovered. No production restore.

## Deployment incident — completion unverified

Coolify source was saved and reloaded as68f4d88. Redeploy was clicked once. Deployment `a2deghcp2kp3g4o6bqgpxwtw` imported68f4d88; observed build log18:23:59UTC was in dotnet publish. No second deployment was requested.

Subsequent observation: real-time connection lost, terminal returned502 and deployment page504 Gateway Time-out. External Node health probe returned522 at apex (assertion failed; not a successful health report). A separate bounded curl received no bytes within10seconds. SSH timed out during banner exchange after8seconds; no server command executed. Root cause, final deployment status and actual running revision are not established. Last fully verified production wasc386113; do not claim68f4d88 is live or currently healthy.

Evidence: `catalog-media-deployment-unavailable-20261029.png`. Preserve the same deployment handle; do not restart based on UI timeout. Priority is recover server access and inspect resources/deployment/container state before any rollback or new deployment.

## Separate candidate

Remotea946a7e includes company-logo/employee-photo and account-photo/ZIP guards (focused6/6 and7/7). CI40929215538: Linux security8m03s and Windows4m18s succeeded; PostgreSQL job123642847553 still running at19minutes. No restart/push while this run is active.

Hedef/P1 and real merchant/customer acceptance remain open. Tenant audit field counts are recorded separately; no schema mutation was made.

## Final live verification — 30 October2026 (Europe/Istanbul)

The same deployment completed Success/Finished in40m58s, started29October18:23:03UTC. No replacement deployment or production restore was launched. Production web/public/database suffix20261029T182336.

Runtime verification29October23:00:57UTC passed: exact68f4d883 image tags for web/public, all three running with zero restarts, database healthy, read-only web root, Google TLS relay587 without SMTP authentication, correct HTTPS callback, no real merchant keys and public trials disabled. Evidence:catalog-media-live-runtime-pass-20261030.png.

External health/source/access checks29October22:59UTC passed with exit0: apex/Santa/Farma200ready122/0; eightplans; checkout/trials unavailable; gallery CSS/JS/siximages exactSHA256; company/resetHTML/onboardingCSS exactsource; anonymous protected endpoints401; tenant platform pages/API/reauth404; root/wwwadmin relative302/no session cookie. Logs:catalog-media-live-health-20261029.log andcatalog-media-live-boundaries-20261029.log. Finished deployment screenshot:catalog-media-deployment-finished-20261030.png.

Current verified production is68f4d883. Earlier outage observations above are retained, not erased. Kernel logs show OOM-killed dotnet processes; the exact causal chain for the deployment outage has not been proved. Old private stage applications were inventoried for reversible cleanup separately.

Separatea946a7e CI40929215538 finished all three jobs successfully: Linuxsecurity8m03s, PostgreSQL45m39s, Windows4m18s. No final test count inferred. This separate account/branding candidate is not live. Platform-logo8a07aec subsequently passed2/2 local tests and is not part ofa946CI or live68. Full Hedef remains open.
