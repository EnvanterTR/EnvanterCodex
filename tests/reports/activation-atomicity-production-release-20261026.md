# Invitation/reset correction production release — 26 October 2026

Candidate `27e9120d6edd1095d7f87e0b7e99449ef69a2169`; no migration. Full CI40390600764 passed all three jobs. PostgreSQL121810276595:356/356, job29m33s; Linux security8m and Windows safety5m succeeded. Exact server staging, fresh195451ZSQL restore,95-table field preservation and143MB200148Z rollback/recovery passed; see activation-atomicity-server-staging-20261026.md.

Coolify full SHA persisted across reload. Deployment `toqnlyqqdou4kgufmblabdfa` started20:23:46UTC and import logs explicitly confirm27e9120. Generic pending-configuration banner is not treated as failed save. Deployment completion/runtime/source checks remain pending; starting a deployment is not completion. Prior live9585/schema122 until actual switch is verified.

Real passwords/MFA/signatures were not changed. Full customer/provider/Hedef acceptance remains open. Local API/Chromium fault/retry evidence does not prove real customer inbox or signature acceptance.

## Completed release and immediate production checks

Coolify `toqnlyqqdou4kgufmblabdfa` finished successfully. Runtime probe20:29:10UTC verified both exact27e images, all services running with zero restarts, database healthy, web read-only rootfs, SMTP relay TLS587/no SMTP AUTH, absent merchant keys, HTTPS callback and closed public trials. Web/public suffix20261026T202413; database20261026T202412. No migration or production restore. Screenshots:27e-coolify-finished-20261026.png,27e-live-runtime-pass-20261026.png.

Three HTTPS hosts returned200ready/schema122. Boundary/source probe started20:29:25UTC and exited0:8plans,closed checkout/trials, anonymous PDF/email401, eight gallery assets hash-equal, companyHTML exact, onboardingCSS exact/200text-css and anonymousGET/POST401 on all3hosts. New resetHTML is200 and exact source on all3hosts. Evidence:27e-live-boundaries-20261026.log/.mjs. This proves static/runtime/anonymous boundaries; real authenticated customer reset was not performed. TAMAM dediğim1yayının1'i test/komut çıktısıyla doğrulanmıştır.
