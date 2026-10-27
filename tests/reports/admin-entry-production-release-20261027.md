# Root admin entry production release — in progress

Exact candidate f073752a40072bdc0a515f3156bfc785dbaffe0d. Full CI40458121020 passed all three jobs: PostgreSQL36m21s, Linux security6m19s, Windows safety5m13s. Total test count not inferred. Exact app+proxy staging, initial fresh SQL clone95table preservation and143MB file/image recovery passed (admin-entry-server-staging-20261027.md).

After CI, fresh dump pre-web-v1-20261027T063327Z.dump hash/list/independent restore passed schema122/tables95. Current90c SQL/media/keys/images143MB rollback-20261027T063335Z recovery passed. No production restore. Evidence f073-ci-success-20261027.png and f073-fresh-recovery-pass-20261027.png.

Coolify source SHA saved and reloaded as exactf073752. The generic unsaved/unapplied banner persisted but actual new deployment imported exactf073 SHA. Deployment tg8vi2aspz7yt8qzousehh7s started2026-10-27 06:35:17UTC and was in progress. Import log confirmed f073752. Evidence f073-deployment-started-20261027.png. Completion/runtime/live HTTP are not yet proven; do not claim release complete or restart based on banner.

Next: observe same deployment; upon Finished verify exact images/runtime and external f073-live-boundaries probe, then record final release. Full customer/merchant/Hedef gates remain open.
