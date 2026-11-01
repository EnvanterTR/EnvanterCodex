# Workspace identity reset — 30 October 2026

TAMAM local implementation and focused Chromium acceptance:4/4,exit0. The pre-fix actual product HTML retained an old private-row fixture after changing company/account/role and exposing the workspace during pending loading; same-context control also retained it as expected. No real accounts or customer data were used.

startApp() now reloads the document when a previously initialized company, account or role differs from the validated session. The auth cover remains until the old document is replaced; login and MFA-enrollment callbacks no longer uncover the workspace before startApp validates its identity. The same authorized context does not reload. This clears existing module/DOM state instead of relying on individually listing every cached table.

The Chromium test serves the actual product HTML/vendor files with a controlled HTTP fixture. After arranging old workspace state, the session response changes company/account/role and later loading is deliberately left pending. Each changed identity loads exactly one fresh document with no old record; the fresh fixture requires login so old content cannot be hidden merely by a successful replacement table response. This proves document isolation, not real Identity/MFA/production customer acceptance.

Evidence:workspace-reset-baseline-browser-20261030.log,workspace-reset-browser-20261030.log,session-workspace-reset.mjs. Uses the existing Playwright1.64 acceptance image with read-only mounts of the new source/test; no installation, production state or credentials changed. Test added to the standard hosted browser runner and Dockerfile.

After source changed, API-helper asynchronous acceptance passed10/10 again; UI inventory2304, five-language2260-entry coverage and hosted browser harness verifier passed. The complete browser suite/remote CI/staging/live gates for this candidate remain open. Existing d19cfe5 CI/stage/recovery does not cover the new source.

Known boundary: this guards identity/role transitions observed at startApp and late api() replies. It does not prove every direct fetch/raw stream, permission mutation without a role-key change, cross-tab revocation or all P1 cache requirements.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
