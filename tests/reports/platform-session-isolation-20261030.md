# Platform administrator session isolation — 30 October 2026

The pinned 3b98183 baseline reproduced a successful private response arriving after logout and an old protected write being sent after logout while awaiting CSRF. These were independent platform API problems; company-page guards did not cover them.

Platform api()/csrf() now capture an authentication generation and reject stale headers, JSON bodies, CSRF tokens and step-up continuations. Logout/login presentation invalidates the generation and closes open dialogs. Native raw Response blob/text/arrayBuffer/formData/json reads are checked before and after awaiting their bodies. Company CSV uses this shared path. Successful platform sign-in invalidates outstanding responses and reloads the document; fresh startup obtains a new CSRF token under the authenticated identity, so prior administrator DOM/cache is discarded.

Local evidence:
- Actual product functions in controlled asynchronous VM: 20/20 passed, exit0. Baseline reproductions are retained separately.
- Actual product HTML in Chromium: 11/11 passed, exit0. Five native body methods each reject after the actual logout button and succeed for an unchanged session; successful sign-in removes an injected old DOM marker/JavaScript cache and obtains fresh authenticated CSRF.
- Existing platform-admin-members Chromium regression: exit0 with final HTML, including filtered formula-safe CSV download, role step-up/retry, invitations, plans, subscriptions, company trash/restore and audit behavior.
- PlatformAdminExperience, PlatformMemberDirectory (93 assertions), PlatformMemberInvites, HostedCheckoutBrowserAcceptance, PlatformMfaBrowserAcceptance and AuthErrorFlows source verifiers passed. Two old literal-login assertions initially failed after document reload replaced in-place showApp; they now require generation invalidation/reload and fresh-document CSRF rather than the obsolete source sequence. MFA/backend gates were preserved.

Browser tests mount actual changed source read-only into the existing Playwright image envantertr-hosted-checkout-browser-acceptance:midnight. Synthetic fixtures only; no real customer login, credential change or security bypass. This is not a clean-image/remote CI/staging/live proof. Direct Response clones/streams and all remaining P1 caches/queues/files are not universally covered by these cases.

New checks are wired into the standard browser Dockerfile/orchestrator and CI. Full local browser orchestration completed in exec session82141, exit0, with the new final11/11 case after all existing fixtures. The same read-only source mounts were used; this is still not a fresh clean-image or remote CI result. This platform candidate is separate from accepted company-session release3b98183 and has not been published.

Evidence: platform-session-baseline-20261030.log; platform-session-isolation-20261030.log; platform-session-browser-20261030.log; platform-session-members-regression-20261030.log; platform-session-full-browser-20261030.log.

TAMAM dediğim 1 yerel düzeltme maddesinin 1'i test çıktısıyla doğrulanmıştır. Tam Hedef/P1 ve yayın kabulü tamamlanmadı.
