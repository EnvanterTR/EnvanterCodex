# Hedef12.1 — five-step customer onboarding, 26 October 2026

Status: DOĞRULANMADI / not achieved. This is a P0 requirement, not a reason to stop all code work while real merchant onboarding waits.

Hedef12.1 explicitly ends the public registration/email/payment/provisioning flow with “onboarding (5 adım)”. The current source audit did not locate a five-step customer setup state/API/guide or corresponding acceptance tests. Case-insensitive searches covered onboarding/getting-started/setup-wizard/setup-step and Turkish setup-guide/five-step labels across Web C#,HTML,JS,Razor files and acceptance tests.

Relevant existing sources:
- wwwroot/platform-admin.html:273 and Data/PlatformEndpoints.cs:1867 use Onboarding only as a customer CRM lifecycle label.
- wwwroot/index.html:406 renders the normal company welcome/dashboard. A welcome greeting is not proof of a five-step setup flow.
- Data/CloudTrialEndpoints.cs returns portal/setup-password URL and MFA requirement after registration; account activation/MFA is not a substitute for five setup steps.
- Data/TenantProvisioningService.cs is the shared company/owner/subscription creation path. Its existence alone does not prove the customer setup UI.

No completion claim is made from negative search results alone. Implementation or direct evidence of an existing equivalent is required. Real Santa acceptance and merchant-provider acceptance are separate obligations.

## Implementation direction to validate against existing forms

Use the existing C#/HTML application and real tenant-scoped data/actions. A reasonable five-step product flow is company details, first location, first employee, first asset and first assignment. This is an implementation choice, not text supplied by the user; inspect existing required model/category fields and navigation before finalizing the actions. Reuse normal validated forms. Do not create fake starter records, auto-sign, weaken MFA, mix tenants, or mark completion with client-only checkboxes.

The guide should be usable by authorized company owners/admins, preserve existing accounts/data, and avoid blocking normal existing-company operations. Persist actual completion where required; distinguish readiness from completion/dismissal. Test cross-company and role denial, empty/partial/complete state, idempotent completion, retained records and UI navigation/mobile behavior. Additive schema changes require the same exact CI/staging/backup/recovery gates. Do not silently expand these five steps into a new billing or authentication architecture.

Next after candidate56e4083 release verification: inspect and implement/verify this requirement. No product code has been changed for this gap yet.
