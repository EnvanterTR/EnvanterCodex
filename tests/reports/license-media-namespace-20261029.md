# License attachment namespace follow-up — 29 October 2026

While the asset candidate5d1e CI ran, the same missing filesystem guard was found in software license attachment upload/download/delete. The shared CompanyMediaPathGuard now checks the full destination before creating a directory, rejects linked reads and validates the filename/path before metadata/audit deletion. Existing archive, EULA-current/version and assignment retention rules remain intact. No migration.

New actual-HTTP/PostgreSQL directory and leaf redirect scenarios preserve foreign bytes and original metadata/audit, reject GET/DELETE404, reject redirected directory upload409 and restore original own download after removing the link. Normal license attachment validation/tenant/audit acceptance and employee EULA privacy/versioned acceptance ran alongside these cases:4/4passed1.5080minutes, exit0; disposable DB/network cleanup completed. Log license-media-namespace-20261027.log records the original command date; result was collected29October. No repeated local asset acceptance.

Full exact-source CI and server release pending for this follow-up. Asset candidate5d1e's369/369 result does not prove this additional source. Current production at last verified release is1d49ac1/schema122; asset rollout proceeds separately with its own terminal CI and fresh recovery.
