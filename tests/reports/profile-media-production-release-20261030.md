# Profile and logo media release — 30 October2026

Exact candidate8a07aec93969c42abb0fc0ac272be358ebd11e40. Protects account profile/ZIP namespaces, company/employee photos and the separate platform logo reader; fixes the existing company-logo multipart500 while retaining CSRF validation. Focused tests7/7,6/6 and2/2 passed. No real user photo or authentication settings changed.

## Passed release gates

- CI40971020704 all three jobs succeeded: Linuxsecurity7m54s, PostgreSQL32m27s, Windows5m21s; total32m30s. Final PostgreSQL case count was not extracted or inferred. Tested remoteheada8e5159 has the same product source as exact8a07.
- Exact isolated stage/var/lib/envantertr-staging/8a07aec-20261030: build.exit0,prepare.exit0,acceptance-probes.exit0. Web768MiB/1CPU/public128MiB/0.25CPU. No published ports, internal network, separate DB/media/keys; SMTP/payment/setup/trial/reminder effects disabled. Runtime/HTTP/access and all95 original-table field projections preserved; schema122/0.
- Stage dump pre-web-v1-20261029T230919Z.dump independently restored and hashed.
- Fresh release dump pre-web-v1-20261029T234551Z.dump: hash/list/independent restore passed, schema122/95tables.
- Recovery bundle/var/backups/envantertr/rollback-20261029T234600Z (143MB) recovered current68f4d883 SQL/media/keys/images independently. No production restore.

Evidence:profile-media-ci-success-20261030.png,profile-media-stage-pass-20261030.png,profile-media-fresh-recovery-pass-20261030.png and corresponding saved scripts.

## Deployment

Coolify source selection was changed to exact8a07aec and save was requested. Persisted selection/reload verification and a single deployment still need completion. Do not deploy newer local work by branchhead.

## Separate work

Support-email recipient reauthorization is a new local change with tests running; not included. The failed model-scope guard proposal is parked outside product/test projects and is not included. All-table audit fields, fullP1 and real merchant/customer acceptance remain open.

## Final live verification

Saved exact8a07aec selection was reloaded and confirmed before a single Redeploy click. Coolifytnxno2lve07vbiehbcu8orak completed Success/Finished4m02s, started29October23:48:39UTC. Actual web/public/database container suffix20261029T234907. Exactweb/public8a07 images, running/zero restarts, healthy database, read-only web root and unchanged relay/payment/trial guards passed at23:53:31UTC.

Both external checks exited0 at23:53:49UTC: apex/Santa/Farma200ready122/0; eightplans; real checkout/publictrials unavailable; gallery CSS/JS/siximageSHA256 and company/resetHTML/onboardingCSS match source; anonymous protected endpoints401; tenant platformpages/scripts/API/reauth404; root/wwwadminrelative302 without session cookies. Screenshots:profile-media-deployment-finished-20261030.png,profile-media-live-runtime-pass-20261030.png; logs:profile-media-live-health-20261030.log,profile-media-live-boundaries-20261030.log.

Current verified production is8a07aec93969c42abb0fc0ac272be358ebd11e40. Supportemail changes remain separate and local; no modelguard proposal is in product code. Full Hedef is not complete.
