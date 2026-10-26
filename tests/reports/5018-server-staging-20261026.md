# Exact 5018bc5 server staging — 26 October 2026

Candidate: 5018bc595684fe4410a9412755b39b2ada9cde0b, pushed to PR25. Remote CI run40296954613: Linux security and Windows safety successful; PostgreSQL121485218470 last observed running. No production deployment.

Exact GitHub archive extracted to /var/lib/envantertr-staging/5018bc5-20261026. Docker image envantertr-staging:5018bc5 built exit0. Fresh SQL backup pre-web-v1-20261026T091423Z.dump verified by pg_restore list and SHA256; restored into new envantertr_stage_5018bc5 on separate stage DB container. Verified SQL121→122 applied twice; all original field projections across94 data tables unchanged after each application. Private data/dump/env remained on server.

New stage container envtr-stage-web-5018bc5 uses internal network, no host port, read-only root filesystem and independent media/key copies. SMTP/payment/bootstrap disabled. Three host headers return ready122/0; anonymous asset GET/PUT/PATCH401; CSRF token and Secure cookie present. PDF GET and email GET/POST anonymous401. All probes exit0, screenshot5018-stage-http-pass-20261026.png. Source scripts retained alongside this report.

Fresh rollback bundle /var/backups/envantertr/rollback-20261026T092057Z is141MB; prepare exit0. File/image recovery drill -drill-20261026T092201Z restored archives and loaded the exact previous application images, exit0. Screenshot5018-rollback-drill-pass-20261026.png. This is separate from the SQL restore and original-field comparison above.

Pending: full candidate CI, production migration/deployment and live verification. Production remains4237/schema121. Real Santa signing and Farma MFA are separate user steps and have not been claimed complete.
