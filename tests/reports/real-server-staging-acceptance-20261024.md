# Real server backup, restore and staging — 24 October 2026

Candidate: ae875b61821e07af95c042d323d99b6a885f5d24 (PR #25).
Existing authenticated Coolify terminal provided access; no new SSH/API credentials were created.

## Passed

- Fresh production PostgreSQL custom dump: pre-web-v1-20261024T071848Z.dump.
- Root-private rollback bundle: /var/backups/envantertr/rollback-20261024T072017Z, 142 MB. Seven checksums passed. Includes SQL, media, data-protection keys, exact old images and private configuration.
- verify-rollback-bundle.sh restored files and exact image identities: ROLLBACK_FILES_AND_IMAGES_RESTORED. Drill directory suffix: -drill-20261024T072226Z.
- Actual dump restored into a separate, network-isolated PostgreSQL container. All 95 public table counts matched production (174 rows including migration history).
- Baseline: 118 migrations; ordered identity MD5 c8694e4a153c5f81d5cface1e0c9f366.
- Exact candidate SQL SHA256 cddf35fd830861e61d018fb1885185e921bccfdf04934572391a162e1db77b49. Applied to restored clone and repeated idempotently: PRESERVED_CONTENT_REHEARSAL_OK. All original fields and rows across 94 data tables preserved; target 120.
- Private raw comparison archive retained only on server, SHA256 d17fa43d7305add3941bfcdb16201270d3f2bdd0e6a7319123e3dca92a73f4bb.
- Candidate app and public-site images built successfully on server. Staging directory: /var/lib/envantertr-staging/ae875b6-20261024T0735Z. Data/media/keys are copies; production volumes are not mounted.
- Staging network envtr-stage-ae875b6 is internal. Active database, webproxy and public containers have no published host ports and only that network. SMTP/payment credentials blank; external delivery disabled.
- Eleven basic HTTP acceptance cases passed through staging Nginx: ready/120/0 for apex, Santa and Farma (3); CSRF response (1); marketing home (1); Santa/Farma portal HTML (2); /giris (1); anonymous assets/platform APIs return 401 (2); CSRF cookie Secure/HttpOnly/SameSite=Strict (1).
- GitHub Actions run 40073522201: Linux security, Windows deployment/backup, Linux PostgreSQL all success (3/3).
- Final read-only live status remains ready, 118 migrations, 0 pending. No production migration/deploy performed.

## Operational corrections

Initial docker cp into tmpfs PGDATA failed before SQL execution. Streaming files through docker exec resolved it. Initial staging web lacked forwarded-header configuration; replaced with matching proxy configuration. Private staging is checked internally rather than weakening network isolation to expose a host port.
The preservation rehearsal used a private copy of the existing script with only expected baseline/target/hash updated for 118→120; repository script unchanged. Original script SHA256 f50b277a2ee25f6736a23decb638b06d41b20c7be77b11723a8a2704da16b31f; adapted private script SHA256 0ddc6a42b40ef01e712339a2a045985fb7ca95cfda561fa628e4af9dce254644.

## Scope limits and next gates

This proves real backup recovery, cloned-data migration preservation and basic isolated staging routing/security. It does not prove full authenticated customer workflows, actual SMTP/iyzico delivery or live Section 20 Santa/Farma/Demo3 acceptance. These gates remain open; do not describe the product as sale-ready or complete.

Screenshots: coolify-fresh-backup-20261024.png; coolify-real-restore-20261024.png; coolify-real-118-to-120-rehearsal-20261024.png; coolify-stage-ready-120-20261024.png; coolify-stage-routing-security-20261024.png; coolify-stage-isolation-20261024.png.
