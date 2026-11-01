# Company document cross-tab session isolation — 30 October 2026

A Chromium fixture loaded actual company HTML in two tabs sharing a browser context. Before the change, actual bound logout in the first document left the second document visible and allowed its delayed private raw JSON body to complete (2/2 baseline). Startup remained unauthenticated to keep unrelated module loaders out of this focused boundary test; fixture-only hideLogin() made the workspace presentation visible. Actual api(), native body guards and bound logout/login handlers were exercised. This is not a real authenticated backend/customer acceptance.

Company documents now subscribe to the same-origin authentication invalidation protocol used by platform documents. Receiving a signal increments authSessionGeneration, clears the CSRF token, stops operational polling, clears alerts/report export state, closes dialogs and shows the existing login cover. Successful company sign-in/MFA completion, logout/company-switch and validated support-session return publish a constant invalidation; storage-event nonce fallback includes no company/account/credential/business data. Different origins remain separate; the signal is authentication coordination metadata rather than cached company records.

Focused actual Chromium6/6 passed: same-origin logout/login under native BroadcastChannel and storage-event fallback reject a waiting old body with STALE_SESSION_RESPONSE and restore authentication cover. Two distinct loopback origins (different ports) remain independent under both mechanisms; these cases model origin isolation, not real DNS/tenant login. Root-platform-to-company interoperability is not separately demonstrated by this fixture.

Actual API helper20/20 passed, AuthErrorFlows passed, UIinventory2304 unchanged, five-language2260 entries passed, browser harness verifier passed. No new product copy or dependency. New fixture added to standard browser Dockerfile/orchestrator. Full final combined browser exec11348 completed exit0, including final platform15/15 and company6/6 after all existing fixtures. Mounted actual product files were read-only in the existing Playwright image; no clean-image/remoteCI/staging/live claim.

This follows local platform cross-tab040619c and is separate from accepted staging/activeCI5409c35. ActiveCI41062817250 PostgreSQLjob124086540816 remains live; Linux7m36s/Windows4m57s passed. No cancelling/restarting/pushing while it runs. Production pin remains3b98183. Historical dates/customer signatures/merchant payment and remaining fullHedef requirements remain open.

Evidence: company-cross-tab-baseline-20261030.log; company-cross-tab-browser-20261030.log; company-cross-tab-api-regression-20261030.log; company-cross-tab-full-browser-20261030.log.

TAMAM dediğim 1 yerel düzeltme maddesinin 1'i test çıktısıyla doğrulanmıştır. Yayın ve tamHedef kabulü açık.
