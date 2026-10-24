# P0 catalog and browser contract corrections — 2026-10-23

- Status: PASS for the scoped local browser and source checks. This is not production acceptance or a green full CI run.
- Changes: The platform browser fixture now retains the approved Cloud user quota, configures a four-user package, verifies the POST body and the displayed `4 / 4` usage. Source verifiers follow those quota assertions. Model-detail coverage checks now verify the open dialog, matching summary and purchase order independently, avoiding a brittle maximum character distance.
- Source commands: `node tools/Verify-AssetModelDetailsUi.mjs`, `node tools/Verify-HostedCheckoutBrowserAcceptance.mjs`, `node tools/Verify-PlatformAdminExperience.mjs` passed.
- Browser image: `docker build -f deploy/testing/Dockerfile.hosted-checkout-browser -t envantertr-hosted-checkout-browser-acceptance:local .` exited 0.
- Browser commands: `docker run --rm --init --entrypoint node envantertr-hosted-checkout-browser-acceptance:local platform-admin-members.mjs` and the same command with `asset-detail-actions.mjs` both exited 0. The model scenario opened the detail dialog and verified scoped summary, assets, orders, history, attachments and linked-asset navigation. The latter script also completed its existing sidebar, asset and purchase scenarios.
- Isolation: Chromium used the scripts' local synthetic API fixtures. No live company records, secrets or production configuration were modified.
- CI diagnostic: PR #25 run `39980282634` failed the proxy-fixture startup because Docker Hub authentication timed out while pulling `nginx:alpine`, before security assertions ran. Local `Verify-ProxyIpBoundary.ps1` initially failed with `ResponseEnded`; its single rerun passed (two forwarded hops, forged prefix removed), and both disposable Compose fixtures were cleaned up. Neither failure is recorded as a pass; remote CI remains separately required.

TAMAM dediğim 2 tarayıcı akışının 2'si test çıktısıyla doğrulanmıştır.
