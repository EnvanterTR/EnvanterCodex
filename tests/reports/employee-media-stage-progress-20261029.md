# Employee/license candidate staging — 2026-10-29

Candidate source: 2997062290267e857ef489b14a8191ba404dd62a (remote branch HEAD at build start).
CI: https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40795019484
Observed: Linux security succeeded 8m00s, Windows deployment/backup safety succeeded 3m17s; PostgreSQL job123189271005 still running at 11m30s. Do not restart from observation timeout.

Server stage: /var/lib/envantertr-staging/2997062-20261029
Build PID1081004 was observed live at elapsed2m29s, dotnet publish running; no build.exit yet. Never rerun bootstrap for this existing stage.
Fresh backup: /var/lib/postgresql/data/envantertr-backups/pre-web-v1-20261029T004736Z.dump
Archive hash/list and independent clone restore completed; schema122 and fingerprints of95 original tables recorded privately.
Clone: envantertr_stage_2997062 in existing isolated stage database. Media and key copies plus private environment prepared, no public port, SMTP/payment/setup/trial/reminder settings disabled for stage.

Remaining: observe same build to terminal result, start exact stage containers, run runtime/HTTP/95-table-preservation probes, wait same CI to terminal result, fresh release recovery drill before any production deployment. Current production remains5d1e261.

Separate local follow-up1822c2a: maintenance/purchase-order media protection,5/5local tests passed, not pushed while this CI is running; not part of2997062 stage.
