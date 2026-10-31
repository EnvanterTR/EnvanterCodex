# Raw export body session boundary — 30 October 2026

Pinned d19cfe5 baseline reproduced delivery of a raw export body after the authentication generation changed. JSON/header checks alone did not cover the interval between accepting headers and consuming a non-JSON body.

Product api() now scopes native Response blob/text/arrayBuffer/formData/json body methods when returning a raw/non-JSON response. Each method checks the original authentication generation before reading and after awaiting the body. Stale body results are rejected instead of returned to download consumers; normal current-session results remain usable. The actual Response object/headers are retained. This does not cancel server writes or claim all direct ReadableStream/clone consumers are protected; report streaming retains its existing explicit generation callback.

Local20/20 asynchronous behavior cases passed, exit0. Real Chromium10/10 passed with actual native Response methods: controlled HTTP headers/partial body arrive first, generation changes while the remaining body is delayed, and the consumer receives STALE_SESSION_RESPONSE; each corresponding unchanged-session case completes normally. Actual product HTML is mounted read-only in the existing Playwright image. Synthetic data only; no real export/customer account/credentials used.

Evidence:session-raw-body-baseline-20261030.log,session-response-with-raw-body-20261030.log,session-export-body-browser-20261030.log. New case added to standard hosted browser orchestration/Dockerfile. Auth error verifier, UI inventory2304, five-language2260-entry coverage and browser harness verifier passed after this source change.

The previously passed full hosted browser run preceded this additional body-method source change and is not claimed as the final full-suite result. The next corrected remote CI must cover the combined candidate. Active original CI41009579015 has Linux's old-verifier failure, Windows5m06s success, PostgreSQLjob123912172427 still running at29m44s; no cancellation/restart/push.

Accepted8cdb77c staging does NOT include this new body guard and is no longer source-equivalent to current product HTML. Its95table preservation and122/0 result proves only that intermediate revision. The combined candidate requires final source/build/staging/release verification before publication. Current verified production remainsd19cfe5. FullHedef/P1 remains open.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
