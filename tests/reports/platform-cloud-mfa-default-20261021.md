# Platform-created Cloud MFA default — 2026-10-21

Observed gap: CheckoutEndpoints creates new paid Cloud companies with SecurityRequireMfa=true, while PlatformEndpoints' verified-payment provisioning path omitted it. This allowed different first-login security behavior depending on provisioning route.

Change: set SecurityRequireMfa=true only for newly platform-provisioned paid Cloud companies. Existing companies and user credentials are not modified. No schema change.

Added a persisted DB assertion to Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation. Running this test together with Required_company_mfa_enrollment_is_single_use_and_authenticates_after_totp_verification in isolated PostgreSQL. Result: 2/2 PASS, process exit0. Web and test builds0 warnings/0 errors. Test-only PostgreSQL container and network removed by script cleanup; production/local product data unchanged.

