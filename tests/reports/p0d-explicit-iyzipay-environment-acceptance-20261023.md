# P0-D explicit iyzico environment selection — 2026-10-23

- Status: PASS (isolated PostgreSQL acceptance; production configuration was not changed).
- Change: Production Coolify/server Compose and the application options no longer default to the iyzico sandbox. The provider URL must be explicitly selected; the sample server environment file documents the exact sandbox and production endpoints. Local development Compose retains its sandbox default.
- Build: Docker acceptance image built the web project successfully with 0 warnings / 0 errors. The acceptance project built with 4 existing ASP.NET deprecation warnings / 0 errors.
- Test: `Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets` passed 1/1. Added a case with configured credentials and a blank provider URL; readiness reports `ApiEndpointValid=false`, `Environment=unknown`, checkout remains unavailable, and secrets are not exposed. Existing sandbox and production endpoint cases also passed.
- Compose: `docker compose -f deploy/server/compose.yaml config --quiet` and the Coolify equivalent passed with non-secret acceptance-only values for required interpolation variables.
- Environment: Test used isolated PostgreSQL/network. No production database, payment credentials, Coolify settings, or live checkout state were accessed or changed.
- Risk: This is fail-closed configuration behavior. Production checkout remains unavailable until an operator explicitly configures and accepts the correct iyzico environment and required merchant settings.
- Evidence: Command `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0-explicit-provider run --build --rm acceptance --filter "FullyQualifiedName~Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets"` exited 0; 1/1 passed.
