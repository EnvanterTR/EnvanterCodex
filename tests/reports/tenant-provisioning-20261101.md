# Tenant provisioning consolidation — 2026-11-01

## Scope

Hedef v30 §6.9 requires a single tenant creation service shared by first-owner setup, the platform company API, and the verified checkout callback/webhook.

## Implementation evidence

- Branch: `codex/tenant-provisioning-service`
- Head: `1e2b51d`
- `TenantProvisioningService` owns `AppDbContext.Companies.Add` and creates the owner membership; paid cloud company creation also creates the active subscription.
- Call sites: `AccountEndpoints` `/api/setup/initial-owner`, `PlatformEndpoints` `/api/platform/companies`, and `CheckoutEndpoints` checkout callback/webhook processing.
- Transaction, payment verification/idempotency, platform audit, first-owner credentials, MFA requirement, and invitation email remain caller-specific and are preserved.
- Source search `rg -n 'Companies\.Add|new Company \{' src/EnvanterTR.Web/Data --glob '*.cs'` returned `Companies.Add` only in `TenantProvisioningService.cs`.

## Verification

- `dotnet build src/EnvanterTR.Web/EnvanterTR.Web.csproj --no-restore`: passed, 0 warnings, 0 errors.
- `git diff --check`: passed.
- Targeted acceptance selection:
  - `Public_checkout_uses_server_price_verifies_provider_and_provisions_once`
  - `Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation`
- Result: both tests were blocked in fixture initialization before executing because `ENVANTERTR_SECURITY_TEST_CONNECTION` is not configured in this environment. This does not indicate a product assertion failure. A disposable PostgreSQL-backed CI run is still required.
- No PR exists for the branch; no CI run is associated with this branch. Not merged or deployed.

## Current state

Implementation is present and builds. Acceptance is **DOĞRULANMADI** pending PostgreSQL CI and review of the complete requirement against the merged/live source.
