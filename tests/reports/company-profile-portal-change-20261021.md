# Company profile portal change — 2026-10-21

Observed live when correcting Santa→Farma: save persisted but browser stayed on former tenant host, which then required login.

Root cause: previousSlug came from edited FormData, equalling saved.slug. A hostname regex also relied on the URL having no trailing slash.

Fix: retain persisted slug in form.dataset.savedSlug on profile load; compare it after save; parse and validate returned portal against saved slug, configured root domain and current old tenant origin; immediately replace location before requests to the old host. Local/apex origins remain unchanged.

PASS: Verify-CompanyPortalLogin.mjs changed-host cases (both slash forms), unchanged slug, local/apex origins, credentials/external domains/port/query/protocol rejection. Verify-TenantSlugProfile.mjs PASS after replacing its obsolete location.assign pattern assertion with validated navigation and persisted-slug assertions. git diff --check PASS.

Actual HTML Chromium profile-save regression PASS: initial persisted Santa slug, edit name/slug to Farma, one CSRF-protected save, redirect to Farma, no old-host profile/branding reload. Network is synthetic; production company data is unchanged by this test. Initial fixture used an incorrect menu selector and timed out; corrected to the actual data-group selector, then exit0. Test added to hosted browser image and suite.

Not yet deployed. PR14 release remains independent and must not be restarted for this separate branch.
