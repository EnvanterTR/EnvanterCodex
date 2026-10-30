# Protected raw response contract verifier — 30 October 2026

CI41013278745 (#804) terminal: Linuxsecurity failed37s in Verify-CompanyDataExport.mjs because the source locator required the old unscoped return response statement. PostgreSQL43m36s and Windows4m55s succeeded. No cancellation/restart. The existing download behavior tests had already passed; this failure is a stale static contract locator, not grounds to remove the new session boundary.

Verifier now requires raw returns through scopedResponse after the shared HTTP/auth error branch, retains the check that responseType is not forwarded to fetch, and additionally requires the native body receiver and generation checks both before/after asynchronous body reading. Existing JSON/ZIP settings authorization, attachment names, audit/transaction and archive-limit checks remain intact.

After the raw body source change and this verifier fix, all12 downstream static checks passed locally, exit0. Earlier checks passed remotely on unchanged product source; full browser combined run also passed locally as recorded in session-isolation-final-full-browser-20261030.md. A new complete remote run remains required.

Product src/,deploy/coolify/,Dockerfile remains identical to f12dd9be672bc520540711b1f96d3c1f17e3ce45; verified with git diff --quiet. The final exact stage uses that remote source while the corrected verifier/evidence commit is prepared. Do not substitute intermediate8cdb stage for this final raw-body source. Production remainsd19cfe5.
