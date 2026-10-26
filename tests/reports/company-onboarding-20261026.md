# Five-step company onboarding — 26 October 2026

Hedef12.1 local implementation and acceptance passed; full CI/staging/backup/release remain open. This does not close the real merchant or Santa customer acceptance requirements.

## Behavior

The existing company dashboard now contains a collapsible five-step guide: company profile, first active location, first active employee, first non-archived/non-deleted asset and first current assignment to an active employee. Buttons use the existing sidebar routes and validated forms; asset creation includes its category prerequisite, and the assignment action directs to the real asset checkout action. No fake starter records, login accounts, signatures or payments are created.

GET /api/company/onboarding projects the five readiness flags from one tenant-filtered SQL statement and a previously completed timestamp. POST /api/company/onboarding/complete independently rechecks all five flags, rejects incomplete setup409 and writes one CompanyOnboardingCompleted audit event. Company-row locking serializes concurrent completion; replay returns the persisted timestamp. Existing active membership, settings.manage, CSRF, tenant and subscription boundaries remain in place. No new migration/table, no credentials/authentication changes. Completion is a historical event; current flags still reflect later changes. Completed companies start with the guide collapsed, and normal company operations remain available throughout.

## Evidence and limitations

- First PostgreSQL run:1/3 passed,2failed. One test wrongly expected403 for an invalid membership where existing session handling returns401; updated to match the established denial. The other exposed sub-microsecond timestamp drift between the first response and PostgreSQL replay. Fixed by returning the database-persisted timestamp. Original onboarding-api-first-20261026.log retained.
- Final local API/PostgreSQL regression:6/6 passed44.3536s, onboarding-api-fixed-20261026.log. Covers real records, empty/partial/ready state, incomplete denial, parallel completion/persistence/single audit, other-tenant/inactive/archived/deleted exclusion, unchanged asset name/password hash, anonymous/viewer/forged-membership/CSRF denial, no-store, existing company profile/location regressions and API authorization inventory. No production data used.
- Web build0errors/0warnings; tests0errors/10existingwarnings. Build logs onboarding-build and onboarding-build-fixed-20261026.log retained. The final Turkish audit-label addition is included in source and will be compiled by exact CI; the local six-test image precedes that label-only addition.
- Actual HTML/CSS/JavaScript Chromium UI accepted with synthetic API transport. All5step destinations, progress changes, failed completion feedback, CSRF POST, completed reload/collapse, viewer-hidden state, light/dark themes and390px no-horizontal-overflow passed; no JavaScript errors. Final log onboarding-ui-final-20261026.log and PNGs in onboarding-ui-20261026/. This is not a claim of a combined real Kestrel/browser/customer acceptance.
- First UI attempt failed because the synthetic transport returned object-shaped lookup lists. Corrected test fixture to arrays; onboarding-ui-first-20261026.log retained. Product source was not weakened to make the fixture pass.
- Existing navigation verifier passed52destinations/10groups; all3inline scripts parse. CI now runs the onboarding browser scenario and preserves its screenshots; PostgreSQL suite discovers the new tests normally.

Current live remains56e4083/schema122. No onboarding deployment or new real customer email/payment occurred during this package. Full exact CI and fresh deployment gates are next.
