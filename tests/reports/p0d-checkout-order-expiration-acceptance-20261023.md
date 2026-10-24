# P0-D checkout order expiry acceptance — 2026-10-23

- Status: PASS (isolated PostgreSQL 17 acceptance; production behavior not deployed).
- Change: New unpaid Cloud checkout orders expire after 24 hours. A hosted worker runs every 15 minutes and changes overdue `Pending` / `PaymentStarted` orders to `Expired`, releasing any initialization lease. Review/provisioning statuses are not changed.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --build --rm acceptance --filter "FullyQualifiedName~Checkout_expiration_marks_only_overdue_unpaid_orders_and_releases_initialization_leases"`
- Result: exit 0; 1/1 passed twice, including after the worker startup-order adjustment. Assertions covered overdue unpaid statuses, lease release, unexpired orders, `PaymentReview`, `ProvisioningFailed`, and idempotent repeat execution.
- Build: Web project succeeded with 0 warnings / 0 errors. Acceptance project succeeded with 4 existing ASP.NET deprecation warnings / 0 errors.
- Environment: Isolated test database/network only; Compose resources removed after the run. No production data or settings were changed.
- Startup behavior: The expiration worker waits for its first 15-minute tick before querying, so it does not race the test host's initial schema migration. The rerun completed without the earlier schema-not-ready database error.
