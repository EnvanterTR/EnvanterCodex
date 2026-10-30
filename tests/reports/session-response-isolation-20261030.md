# Session response isolation — 30 October 2026

Local focused behavior acceptance:10/10 passed, exit0. The actual api() function is extracted from product index.html and executed with controlled asynchronous fetch/body completion, rather than replacing it with a test implementation. Pinned d19cfe5 baseline reproduced delivery of an old company's successful response after the authentication generation changed.

The API helper already guarded stale401 responses. It now rejects other responses whose request generation differs from the current authentication generation, including successful JSON after delayed body parsing and old error details after delayed parsing. Current JSON/raw/204 results and current401/error behavior remain unchanged. The stale rejection does not open login over a new session. A concurrent new request keeps its consumer result when an older request completes.

Evidence: session-response-baseline-20261030.log, session-response-isolation-20261030.log, deploy/testing/verify-session-response-isolation.mjs. Added to Linux CI. The first baseline harness invocation hit Node's default child-process output buffer because the HTML exceeds1MiB; the fixture uses a bounded16MiB buffer and pinned baseline, then reproduction and all10 checks passed. No product workaround for the harness issue.

This is focused API-helper evidence, not full browser E2E or proof that every direct fetch/raw stream is isolated. Raw-stream consumers retain their own generation checks. It also does not cancel or roll back server-side writes already accepted before a session change.

Not pushed, staged or deployed. The active CI41004287391 and accepted d19cfe5 staging do not contain this new source change. Keep candidates separate; no cancellation/restart of the active run. FullHedef/P1 remains incomplete.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.

Additional local checks after the API-helper change passed: Generate-UiInventory.mjs --check (2304 controls) and Verify-UiLocales.mjs (2260 entries across five languages). No DOM controls were added. A separate remaining UI lifecycle audit is needed for already-rendered/cached data when accounts change; rejecting late HTTP responses alone does not prove all existing DOM state is cleared. This remains open and must not be counted as complete P1 cache isolation.
