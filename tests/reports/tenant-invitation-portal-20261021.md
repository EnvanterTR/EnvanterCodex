# Tenant portal links in invitations — 21 October 2026

Platform member invitations, manually provisioned company owners and owner-invitation retries previously omitted the company slug when composing email. Company-member invitation and retry paths had the same omission. The shared mail helper therefore used the general entry point and omitted the tenant-specific onboarding context.

All these call sites now pass the company slug already resolved within their authorized company scope. The existing helper builds the HTTPS portal URL. Existing accounts receive an access notice without a password reset token; new accounts keep their activation link and receive their company portal information. The owner role is rendered as `Şirket sahibi` instead of falling through to `Görüntüleyici`.

## Verification

- Web and acceptance projects built with zero errors and warnings.
- Isolated PostgreSQL/API/SMTP acceptance covers platform invitation, pending resend, existing-account additional-company access, manually provisioned owner and owner-invitation retry. Assertions inspect decoded captured mail for the expected tenant portal, and the confirmed-owner notice for the correct role.
- Existing company-scope, invitation retry, CSRF, unlimited Cloud membership and local seat-limit behavior remain covered by the selected tests.
- Platform company provisioning, platform member invitation, multi-company invite and Cloud unlimited-user source verifiers passed; diff whitespace check passed.
- Test process session `44430`; final result recorded below when terminal.

This change does not configure production SMTP or establish real mailbox delivery. No live company/account data or credentials are changed by these synthetic tests.

Final result: session44430 exited0; 4/4 selected acceptance tests passed (2.5056 minutes test time). Temporary PostgreSQL container/network cleaned up. No test retry was needed.
