# P0-D annual Cloud catalog and checkout readiness acceptance — 2026-10-23

- Status: PASS in isolated PostgreSQL 17 acceptance; live paid checkout remains unavailable.
- Command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Annual_catalog_drafts_are_authorized_csrf_protected_idempotent_and_preserve_existing_plans|FullyQualifiedName~Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets"`
- Result: exit 0; 2/2 passed in 31.6012 seconds.
- Verified: annual Cloud catalog preview carries the 15-day trial term and tax-exclusive label; draft creation is platform-admin-only, CSRF-protected, idempotent, and preserves manually existing plan details. Checkout readiness requires a public, active, positive-price Cloud plan plus valid provider credentials and callback/API configuration; health output does not expose credential values.
- Environment: disposable PostgreSQL 17 and synthetic configuration only. Compose database and network removed after execution. No production plan was created/published and no payment provider was contacted.
- Limit: The acceptance proves local catalog/readiness rules only. Live `/api/plans` currently returns an empty array and live checkout reports unavailable; real sandbox credentials and live SMTP remain outstanding.
