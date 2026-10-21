# Company profile portal change — 2026-10-21

Observed live when correcting Santa→Farma: save persisted but browser stayed on former tenant host, which then required login.

Root cause: previousSlug came from edited FormData, equalling saved.slug. A hostname regex also relied on the URL having no trailing slash.

Fix: retain persisted slug in form.dataset.savedSlug on profile load; compare it after save; parse and validate returned portal against saved slug, configured root domain and current old tenant origin; immediately replace location before requests to the old host. Local/apex origins remain unchanged.

PASS: Verify-CompanyPortalLogin.mjs changed-host cases (both slash forms), unchanged slug, local/apex origins, credentials/external domains/port/query/protocol rejection. Verify-TenantSlugProfile.mjs PASS after replacing its obsolete location.assign pattern assertion with validated navigation and persisted-slug assertions. git diff --check PASS.

Not yet deployed; actual browser profile-save regression remains to verify. PR14 release remains independent and must not be restarted for this separate branch.
