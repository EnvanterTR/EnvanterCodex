# Tenant filter production release — 27 October 2026

Candidate:1d49ac17082c861ec52e67f3214ad117d983fc50. Adds validated company query filters/tracked write guards and preserves verified existing-company checkout form/order status behavior. No migration/schema/data rewrite.

Full CI40528142372 succeeded in all three jobs: PostgreSQL367/367, job26m40s/test24.8060minutes; Linux security8m23s; Windows deployment/backup5m6s. Local actual Chromium acceptance and targeted PostgreSQL are documented in tenant-filter-checkout-regression-20261027.md.

Exact server app/public builds exit0. Isolated1d49 stage used internal network/nohostports/read-onlyweb/copiedmedia+keys/disabledSMTP-payment-setup-trials-reminders. Runtime guards, threehost122/0, anonymous401/CSRF, publicCSS/reset, tenantplatform404/rootadmin302 and all95data-table field-projection preservation passed. No stage migration.

After CI: fresh SQL pre-web-v1-20261027T192154Z.dump SHA/list/independent restore passed schema122/tables95. Current f073 SQL/media/key/images recovery bundle /var/backups/envantertr/rollback-20261027T192200Z (143MB) verified. No production restore. Source SHA saved and reloaded as1d49; generic unsaved/unapplied UI banner did not alter saved value.

Single Coolify deployment dxlkeknxdqygasopecsv5kim started2026-10-27 19:23:38UTC; actual import log confirms1d49 fullSHA. It is currently building. Do not trigger another deployment from the generic configuration banner. Completion/runtime/external verification pending. Production is not yet claimed updated.

Full Hedef/P1 and real customer/merchant acceptance remain open. This release evidence does not substitute for unsigned customer documents, normal customer MFA/role acceptance or real iyzico merchant configuration.
