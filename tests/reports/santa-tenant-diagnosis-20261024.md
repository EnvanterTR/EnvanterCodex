# Santa tenant diagnosis — 24 October 2026

Production PostgreSQL was queried in BEGIN READ ONLY / ROLLBACK, using existing server access. No account hashes, secrets, email addresses or customer inventory were selected.

1. Companies left-joined to CompanyMemberships and AspNetUsers for exact slug santa returned zero rows. Therefore this query cannot establish lockout or MFA state for a Santa member.
2. Independent aggregate counts returned: case-insensitive exact company name Santa = 0; case-insensitive exact slug santa = 0; case-insensitive exact slug farma = 1.

This proves the expected Santa company/portal mapping is missing in this production database. The prior generic login error must not be described solely as a bad password. It does not prove whether a matching user account exists elsewhere or which historical operation removed/renamed Santa.

Next: provision Santa and its correct owner through the supported audited management/provisioning path; do not insert identity/tenant/session rows manually. The existing platform administrator password was accepted in the live browser and current MFA was requested. User handoff requested for the platform Authenticator code, explicitly distinguishing it from Farma MFA. No password reset, MFA bypass or membership change performed.

Evidence: coolify-santa-missing-tenant-20261024.png. Live Section 20 remains failed/incomplete pending correct provisioning and full scenarios.

CI run 40077928353 was last confirmed running. A subsequent GitHub poll failed with local DNS resolution, which is not a terminal job result; do not restart or mark that CI failed based on the observation error.
