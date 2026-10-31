# Obsolete stage applications stopped — 30 October2026

TAMAM:30 completed private test web/public containers were stopped reversibly. Production container identities were captured before and verified running unchanged afterwards. No container, volume, database, file or backup was deleted. Current68f4d88 stage web/public were retained; stage databases were retained.

Preflight required exact test naming patterns, known stage image prefixes, no published ports and only the internalenvtr-stage-ae875b6 network. Initial preflight rejected an additional historical image prefix before any stop; after inspecting that prefix it was included explicitly. The successful execution saved each exact name/id/image privately in/var/lib/envantertr-staging/obsolete-stage-stop-20261030.json. Recovery is docker start with the recorded names; no recreation required.

Available memory rose from2393MiB to5530MiB; used memory fell3291MiB to1205MiB on a7937MiB server. Kernel logs showed OOM-killed dotnet processes. This establishes memory pressure; it does not alone prove the entire outage causal chain.

Afterwards apex/Santa/Farma returned200ready122/0 (exit0). Evidence:obsolete-stage-stop-pass-20261030.png; after-stage-cleanup-health-20261030.log; stop-obsolete-stage-apps-20261030.py. Production remains68f4d883. FullHedef is incomplete.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.
