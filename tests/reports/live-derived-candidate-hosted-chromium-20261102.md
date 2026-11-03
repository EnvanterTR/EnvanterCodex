# Live-derived release candidate — hosted Chromium acceptance

- Measured: 2026-11-02; completed at approximately 12:04 UTC.
- Candidate branch: `release/v1-candidate-20261102`.
- Candidate commit tested: `1a676b2ae2dce9416fdd918ba8c26472f49ec316`.
- Command: `deploy/testing/run-hosted-checkout-browser-acceptance.ps1`.
- Result: exit code 0. The full existing Playwright/Chromium acceptance runner completed; checkout, public pricing and trial, login/MFA/session isolation, tenant routing, inventory and bulk actions, camera/QR/barcode, reports, purchasing, licenses, stock/accessories, locations, members, platform administration, support and scheduled reports all reported PASS. Browser console emitted expected synthetic 404 responses for mocked resources; no acceptance scenario failed.
- Scope: test runner uses synthetic browser/API fixtures; no customer credentials, production database writes, or production deployment.
- Combined candidate evidence: release build 0 warnings/errors; 85/85 source verifiers; UI inventory 2,304 controls / 71 routes; isolated PostgreSQL security acceptance 413/413; hosted Chromium exit 0.
- Remaining: current production backup/restore/migration rollback rehearsal on an isolated live database clone; reconcile the dirty feature branch selectively; real-company acceptance; iyzico remains locked pending legal entity and approved merchant account.
