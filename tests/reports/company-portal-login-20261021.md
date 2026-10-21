# Company portal login routing — 2026-10-21

- Cloud apex/www sessions now resolve the active membership portal and replace the address before loading company data.
- Only HTTPS single-label envantertr.com company hosts are accepted. Credentials, nondefault ports, paths, queries, fragments, reserved slugs and unrelated domains are rejected.
- Local installations and existing company hosts retain their origin. Server tenant authorization remains authoritative; no session token enters the URL.
- PASS: node tools/Verify-CompanyPortalLogin.mjs (active membership, apex/www, local origins and unsafe targets).
- PASS: node tools/Verify-CoolifyTenantRouting.mjs; git diff --check.
- Not yet deployed or proven by an authenticated live browser login. Santa/Farma account mappings and SMTP remain open.
