# Company audit timestamp completion — 31 October 2026

Scope: Hedef6.1 creation/update timestamp gaps for company-scoped data. This does not complete Hedef6.1, deletion semantics, shared platform tables, or P1 as a whole.

Migration `20261030210807_AddMissingCompanyAuditTimestamps` adds 75 nullable columns across 54 existing tables: 24 CreatedAt and 51 UpdatedAt. No old value is fabricated or backfilled; existing columns, rows, keys and endpoint concurrency timestamps are retained. Company root and Guid/nullable-Guid company-scoped entity mappings receive the missing fields. Shared identity/catalog types without a company scope retain their architecture.

Tracked synchronous/asynchronous writes stamp newly mapped fields after tenant-write validation. PostgreSQL row triggers additionally cover direct SQL inserts and bulk updates. Triggers populate newly introduced CreatedAt only on insertion, and newly introduced UpdatedAt on insert/update. Editing an old record leaves its unknown CreatedAt null. A later explicit historical CreatedAt correction remains possible; the supplied CSV has not been imported.

Validation in progress: final PostgreSQL acceptance covers upgrade from schema122, repeated migration, original-field preservation, model/snapshot parity, old unknown dates, sync/async persisted dates, direct SQL insertion, bulk update, and tenant write rejection. The first tracked-write version passed4/4 before bulk/direct-SQL trigger support was added; that result is not final-version acceptance.

Publication: local only, not deployed. A fresh production backup and separate restore/staging check are required before migration. Down removes the new audit columns and their captured history; do not apply Down to production as a routine rollback. Restore the reviewed backup in isolation first and preserve new data for reconciliation.

Known open work: DeletedAt/soft-delete coverage, shared table audit fields, historical inputs, complete P1/customer acceptance, exact deployment/runtime confirmation, remote CI and server staging for this candidate. Existing NU1900 vulnerability-index connectivity and test deprecation/analyzer warnings are not reported as zero-warning builds.

Final local acceptance: exit0,4/4 passed in39.2998s on PostgreSQL17. The final migration includes trigger coverage; the direct SQL/bulk case passed. The release regression preserved original118-era projections through the complete123-migration chain; tenant query/soft-delete and rejected tracked foreign writes passed. `HasPendingModelChanges` was false. Evidence: company-audit-final-acceptance-20261031.log. Test startup emitted existing deprecation/analyzer/connectivity and disposable key-storage warnings; no zero-warning claim. Remote CI/staging/production are not yet validated for this change.
