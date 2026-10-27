# P1 asset attachment filesystem namespace — 27 October 2026

Hedef6.4 follow-up after tenant filter production1d49. Asset attachment routes constructed CompanyId/assets/AssetId paths but did not reject an existing symlink/reparse point in a directory or final file. Other media providers have separate guards and remain a wider follow-up; this report does not claim the entire file/cache/queue requirement complete.

CompanyMediaPathGuard canonicalizes the requested path beneath the validated company's configured media root and rejects existing reparse components below the trusted root, including leaf files. Invalid/inaccessible filesystem paths fail closed. Asset attachment upload returns409 before disk/metadata writes for an unsafe destination. Download returns404. Delete validates storage filename/path before metadata/audit mutation and returns404 for an unsafe path.

New real-HTTP/PostgreSQL theory starts with a normal own attachment, redirects its directory or leaf file into another company's media namespace, then checks GET404 and DELETE404 without losing the original metadata/history or changing foreign bytes. Directory-routed upload409 must leave no extra foreign files; a normal new filename in a safe own directory still uploads successfully despite a different leaf being a link. Removing the test link must restore normal own download with original bytes. Existing normal attachment authorization/CSRF/limits/audit acceptance runs alongside these two new cases.

Scope limits: configured root is trusted; checks do not claim protection against a privileged host actor racing path replacement or creating cross-directory hard links. Shared guard is currently wired to asset attachment routes only. Account photos are intentionally account scoped; all other media/queue/cache paths still need explicit mapping or implementation evidence.

Local build/tests are running in disposable Docker PostgreSQL. No production deploy, migration or production file manipulation. Production remains1d49ac1/schema122.

Local actual PostgreSQL acceptance completed exit0:3/3passed1.2033minutes, including directory and leaf symlink denials plus existing normal upload/download/delete/authorization/CSRF/limits/audit behavior. Foreign bytes and original own metadata/history were preserved; restored own file downloaded correctly. Disposable DB/network cleanup completed. Full CI and exact-source server release gates pending.
