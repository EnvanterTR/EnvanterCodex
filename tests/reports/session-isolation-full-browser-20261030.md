# Session isolation complete hosted browser regression — 30 October 2026

Local hosted-checkout.mjs orchestration completed exit0 with the current company HTML, orchestration script and new session-workspace-reset script mounted read-only into the existing Playwright1.64 acceptance image envantertr-hosted-checkout-browser-acceptance:midnight. It is not a new clean image build or remote CI result. Other fixture/vendor assets came from the previously built image; no production/real account state was used.

The run reached the final WORKSPACE_RESET_BROWSER4/4 pass after checkout/quota/login/portal, scanner/storage, asset-detail/relations/import/print, purchase/license, people/accessory/stock, report, platform administration, trial/pricing and support fixture flows. The new company/account/role reset and same-context control passed in that run. Existing explicit unavailable-feature states in the harness are not full feature-parity acceptance. Backend actions are synthetic HTTP fixtures, not proof of live Identity/MFA/customer completion.

The12 static checks after the previously failed auth-flow verifier also all exited0: session/token dialogs, company data export, dashboard drafts, import preview, restoreUI, mobile layout, subscription access, public-demo layout, demo requests, plan catalog, checkoutUI and supportUI. Auth verifier local fix separately passed; the earlier error was its obsolete early-hideLogin source locator, now replaced with an explicit check that login does not expose the workspace before startApp identity validation.

Evidence:session-isolation-full-browser-20261030.log and session-candidate-Verify-*.log. Existing async API10/10 and focused Chromium4/4 evidence remain separate. No unchanged passing suite was blindly restarted; the full browser run was justified by the new shared api/startApp/login source changes and the initial CI stopping before those steps.

CI41009579015 remains the original in-progress run: Linuxsecurity failed30s; Windows5m06s passed; PostgreSQLjob123912172427 continues. No cancel/restart/new push while that job remains active. A corrected full remote CI is still required.

Product source src/,deploy/coolify/,Dockerfile is identical between remote8cdb77c69392f97915b8f64f406b240fcc760d5e and current local951747c; later changes are verifier/evidence only. This explicit source-equivalence permits preparing an8cdb77c stage while the corrected commit is not yet pushed. It does not transform the failed original Linux CI into a passing result. Current verified production remainsd19cfe5.
