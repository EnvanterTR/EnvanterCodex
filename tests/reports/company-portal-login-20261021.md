# Company portal login routing — 2026-10-21

- Cloud apex/www sessions now resolve the active membership portal and replace the address before loading company data.
- Only HTTPS single-label envantertr.com company hosts are accepted. Credentials, nondefault ports, paths, queries, fragments, reserved slugs and unrelated domains are rejected.
- Local installations and existing company hosts retain their origin. Server tenant authorization remains authoritative; no session token enters the URL.
- PASS: node tools/Verify-CompanyPortalLogin.mjs (active membership, apex/www, local origins and unsafe targets).
- PASS: node tools/Verify-CoolifyTenantRouting.mjs; git diff --check.
- Not yet deployed or proven by an authenticated live browser login. Santa/Farma account mappings and SMTP remain open.
- Chromium actual HTML acceptance PASS for apex and www: selects active Farma membership, navigates to its HTTPS portal, and makes no asset-data request on the entry host. All network traffic is intercepted with synthetic responses; no production login is claimed.
- Initial browser run failed because the test fixture omitted existing report scripts. Fixture corrected to serve them; second run exit0. Test included in hosted browser acceptance image and suite.
- Signed-out browser form acceptance PASS: 401 initial session, synthetic email/password entry, exactly one CSRF-protected login POST, fresh active membership resolution, then Farma navigation. No automatic credential retry or entry-host asset data loading.
