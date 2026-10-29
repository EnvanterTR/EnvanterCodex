# Catalog image candidate staging — 29 October 2026

Candidate: `68f4d883aba934e71e927ee33b504542c44d40cd`.
CI40921546389 is still running: Linux security completed in6m22s and Windows in5m10s; PostgreSQL remains active. Continue observing that same run.

Stage: `/var/lib/envantertr-staging/68f4d88-20261029`. Bootstrap PID2580313; both builds completed with exit0. Exact web/public containers started on the internal network. Fresh backup `pre-web-v1-20261029T175139Z.dump` passed hash/list checks and independent restoration into `envantertr_stage_68f4d88`; schema122 and the95 original data-table field projections were recorded privately.

Runtime, three-host HTTP and original-data preservation probes were launched once. Their terminal result still needs observation. SMTP/payment/setup/trial/reminder settings are disabled in the private stage environment; stage media/keys are separate copies.

Current production remains c386113. Localf04d7a7 logo/employee-photo protection and the tenant media inventory are not part of this candidate and were not pushed while its CI is active. Full Hedef remains open.

Final staging observation: runtime isolation/side-effect guards, three-host HTTP boundaries and all95 original-table field projections passed (acceptance-probes.exit0; schema122/0). Evidence:catalog-media-stage-pass-20261029.png. CI PostgreSQL job123615809398 is the remaining active gate; do not restart it. No production deployment of68f4d88 has occurred.
