# Exact five-step onboarding staging — 26 October 2026

Candidate9585fae1d97980f861dbe519252bc1c422230476. Production remains56e4083/schema122; this report is not a production deployment or complete Hedef acceptance.

- Downloaded immutable Git archive for9585 into /var/lib/envantertr-staging/9585fae-20261026. Both application and public Nginx images built successfully:envantertr-staging:9585fae and envantertr-staging-public:9585fae. Build job PID387176 finished exit0. No migration.
- Fresh SQL dump pre-web-v1-20261026T183807Z.dump validated by pg_restore list and SHA256, restored into isolated envantertr_stage_9585fae. All95data-table original-column fingerprints retained privately. Separate copies of production media/keys and isolated runtime env; outbound SMTP/payment/bootstrap/trials/reminders disabled.
- Started envtr-stage-web-9585fae read-only/internal/no-host-port and envtr-stage-public-9585fae on the existing internal stage network. Public proxy resolves web to this stage container's IP explicitly, avoiding other stage aliases. Existing production containers were not replaced.
- Apex/Santa/Farma Host readiness122/0; anonymous asset read/write401, CSRF token/Securecookie, anonymous handover PDF/email401, anonymous onboardingGET/completePOST401. Real staged Nginx returns HTTP200/text-css and onboarding stylesheet content for all3hosts. Original95table field projections unchanged after startup/probes.
- Fresh143MBrollback bundle /var/backups/envantertr/rollback-20261026T184132Z prepared from actual current56e web/public/database and the freshSQLdump. File/image recovery drill exit0; no production restore. Private logs and original backup materials remain server-side. Screenshot9585-stage-and-recovery-pass-20261026.png; scripts9585-stage-bootstrap/start/runtime-guards/http-probe/preservation,9585-prepare-isolated-stage and9585-rollback-drill retain the recipe.

Exact CI40379203209 PostgreSQL job121771140114 was confirmed currently running, with3in-progress checks visible on PR25 at the last observation. No complete CI or release claim. Earlier candidates' CI failures and local corrections are documented in company-onboarding-20261026.md. The script artifacts here were written locally after candidate9585 was pushed; they are evidence recipes, not new product changes to deploy.

Local6/6API/PostgreSQL and Chromium synthetic-transport evidence are separate from these cloned-server basic checks. Full real Santa/Farma/signature/merchant acceptance remains open. TAMAM dediğim1staging/kurtarma paketinin1'i komut/ekran çıktısıyla doğrulanmıştır.
