# Platform cross-tab authentication invalidation — 30 October 2026

Baseline5409c35 platform HTML in two actual Chromium tabs sharing one browser context reproduced an old private body being returned in the second tab after the first tab's actual logout button. The second workspace remained visible. Both normal browser and BroadcastChannel-disabled baseline cases reproduced the missing cross-tab notification (2/2). Synthetic API fixtures only, not real account sessions.

Platform successful login and logout now publish a same-origin authentication invalidation. Other platform documents call their existing showLogin(), incrementing their response generation, clearing CSRF, closing dialogs and covering old workspaces. BroadcastChannel carries only a constant invalidation string; a localStorage change with a timestamp/random nonce is the fallback. No user/company identifier, credential, response body or business record is included. Receiving a notification does not republish it, avoiding loops. Normal server authorization/MFA/CSRF gates remain unchanged.

Final focused actual Chromium15/15 passed: existing11 cases plus two-tab logout and successful login, each under native BroadcastChannel and storage-event fallback. The waiting private body is rejected in the receiving tab and its login form becomes visible. Existing VM actual API/CSRF/step-up/body regression20/20 passed; two platform source verifiers passed with the required publish-before-reload transition.

The full hosted browser orchestration with final platform HTML/test completed in exec24479, exit0, including final15/15 after all existing fixtures. Actual changed files are read-only mounted into the existing Playwright midnight image; this is not a clean-image/remote CI/staging/live proof. Tests are already part of the standard browser orchestrator; no new runner dependency.

This is a separate later candidate. ActiveCI41062817250 (#807), accepted staging5409c35 and its143MB133550Z recovery DO NOT contain cross-tab invalidation. Current verified production remains3b98183. Do not cancel the live PostgreSQL job or push this candidate while it runs. Company-page cross-tab invalidation and arbitrary remote session-revocation timing remain outside these platform-specific tests and require further P1 work; no universal P1 completion claim.

Evidence: platform-cross-tab-baseline-20261030.log; platform-cross-tab-browser-20261030.log; platform-cross-tab-api-regression-20261030.log; platform-cross-tab-full-browser-20261030.log.

TAMAM dediğim 1 yerel düzeltme maddesinin 1'i test çıktısıyla doğrulanmıştır. Yayın kapıları ve tamHedef açıktır.
