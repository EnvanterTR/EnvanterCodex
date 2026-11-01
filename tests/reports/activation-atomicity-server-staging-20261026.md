# Exact invitation/reset correction staging — 26 October 2026

Candidate `27e9120d6edd1095d7f87e0b7e99449ef69a2169`. Production remains `9585fae/schema122`; this report is not a production release or complete customer acceptance.

Both exact archive builds succeeded: `envantertr-staging:27e9120` and `envantertr-staging-public:27e9120`, under `/var/lib/envantertr-staging/27e9120-20261026`. Background build PID507376 completed exit0. The first start command ran before build.exit existed and stopped at that precondition without creating containers. The same build was inspected, not restarted; after exit0, web and public stage containers started successfully.

Fresh SQL dump `pre-web-v1-20261026T195451Z.dump` passed pg_restore listing/SHA256 and was restored into independent `envantertr_stage_27e9120`. Original column projections for all95data tables were fingerprinted privately. Separate media/key copies, private environment, internal network/no host ports and web read-only rootfs verified. SMTP/payment/bootstrap/trials/reminders disabled. No migration.

Stage checks passed for apex/Santa/Farma: ready122/0, anonymous asset GET/PUT/PATCH401, Secure CSRF cookie, anonymous PDF/email/onboarding denial. Actual staged Nginx returns the exact feature's CSS as200/text-css, and reset-page HTML has status feedback outside the hideable form on all3hosts. Startup/probes preserved all95original data-table field projections. This is basic real-server acceptance; the authenticated fault/retry and rendered UI behavior were verified separately in the local API/Chromium evidence.

143MBrollback bundle `/var/backups/envantertr/rollback-20261026T200148Z` was prepared from actual9585production images/SQL/media/keys. Separate file/image recovery drill exit0; no production restore. Script uses web/database suffix190756 and public-site190757, matching actual current containers rather than assuming equal suffixes. Private backup/env/data remain server-side. Evidence `27e-stage-and-recovery-pass-20261026.png` and `27e-*.py` recipes.

Exact CI `40390600764`, PostgreSQL job `121810276595`, was confirmed running; Windows job passed at last observed grouping. CI completion and publication remain required. Real user passwords, MFA, signatures, inboxes and payment were not changed/tested by this stage run. TAMAM dediğim1staging/kurtarma paketinin1'i komut/ekran çıktısıyla doğrulanmıştır.
