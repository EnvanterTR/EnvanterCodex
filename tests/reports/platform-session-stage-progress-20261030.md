# Platform session isolation — exact staging and recovery, 30 October 2026

Candidate5409c35fbb7397d77133eeacf94dee2b437a31df, source-equivalent to local product HEAD under src/deploy/coolify/Dockerfile. Product/browser/async proofs are separately recorded in platform-session-isolation-20261030.md. Corrected CI41062817250 (#807): Linuxsecurity7m36s and Windows4m57s passed. PostgreSQLjob124086540816 remains live at15m25s; do not cancel/restart or push while active.

Old accepted f12 staging web/public apps were stopped reversibly after exact name/image/internal-network/no-published-port preflight. Production identities remained unchanged; no files, containers, volumes or databases deleted. Available memory5804MiB. Private stop manifest /var/lib/envantertr-staging/pre-platform-session-stage-stop-20261030.json.

New /var/lib/envantertr-staging/5409c35-20261030:
- One buildPID82507 completed build.exit0, both exact candidate images.
- PreparationPID83989 completed prepare.exit0: fresh pre-web-v1-20261030T132825Z.dump hash/list/independent restore passed122/95. Cloneenvantertr_stage_5409c35; private95-table original field fingerprints retained.
- Bounded web768MiB/1CPU and public128MiB/.25CPU started once on internal-only envtr-stage-ae875b6. Private separate media/keys, no published ports, readonly application root, SMTP/payment/setup/trial/reminders disabled.
- AcceptancePID94132 completed acceptance-probes.exit0: runtime/HTTP/proxy/CSRF/anonymous/platform boundaries; root platform HTML equals exact staged candidate bytes with new session guards; all95 original table field projections preserved. Schema122/0; no migration applied.
- Fresh recoveryPID100368 completed release-recovery.exit0: pre-web-v1-20261030T133543Z.dump hash/list/independent restore122/95;143MB /var/backups/envantertr/rollback-20261030T133550Z current3b98183 SQL/media/keys/images recovered independently. No production restore or new deploy.

Prepared exact5409c35 external health/boundary/runtime probes have not been run prematurely. Coolify pin and verified production remain3b98183 with suffix20261030T131248. Full corrected remote CI and actual deployment/live checks remain open. No real authenticated customer acceptance or fullHedef/P1 completion is claimed. Historical creation dates still await user-supplied values.

Evidence: pre-platform-session-stage-stop-pass-20261030.png; platform-session-stage-pass-20261030.png; platform-session-fresh-recovery-pass-20261030.png. Authored bootstrap/preparation/start/probe/recovery scripts are saved alongside this report; raw customer dump/keys/private env are excluded from Git.

TAMAM dediğim 2 kabul maddesinin (staging, bağımsız kurtarma) 2'si test çıktısıyla doğrulanmıştır.
