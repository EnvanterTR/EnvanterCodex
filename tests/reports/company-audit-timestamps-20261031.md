# Company audit timestamp completion — 31 October 2026

Scope: Hedef6.1 creation/update timestamp gaps for company-scoped data. This does not complete Hedef6.1, deletion semantics, shared platform tables, or P1 as a whole.

Migration `20261030210807_AddMissingCompanyAuditTimestamps` adds 75 nullable columns across 54 existing tables: 24 CreatedAt and 51 UpdatedAt. No old value is fabricated or backfilled; existing columns, rows, keys and endpoint concurrency timestamps are retained. Company root and Guid/nullable-Guid company-scoped entity mappings receive the missing fields. Shared identity/catalog types without a company scope retain their architecture.

Tracked synchronous/asynchronous writes stamp newly mapped fields after tenant-write validation. PostgreSQL row triggers additionally cover direct SQL inserts and bulk updates. Triggers populate newly introduced CreatedAt only on insertion, and newly introduced UpdatedAt on insert/update. Editing an old record leaves its unknown CreatedAt null. A later explicit historical CreatedAt correction remains possible; the supplied CSV has not been imported.

Validation in progress: final PostgreSQL acceptance covers upgrade from schema122, repeated migration, original-field preservation, model/snapshot parity, old unknown dates, sync/async persisted dates, direct SQL insertion, bulk update, and tenant write rejection. The first tracked-write version passed4/4 before bulk/direct-SQL trigger support was added; that result is not final-version acceptance.

Publication: local only, not deployed. A fresh production backup and separate restore/staging check are required before migration. Down removes the new audit columns and their captured history; do not apply Down to production as a routine rollback. Restore the reviewed backup in isolation first and preserve new data for reconciliation.

Known open work: DeletedAt/soft-delete coverage, shared table audit fields, historical inputs, complete P1/customer acceptance, exact deployment/runtime confirmation, remote CI and server staging for this candidate. Existing NU1900 vulnerability-index connectivity and test deprecation/analyzer warnings are not reported as zero-warning builds.

Final local acceptance: exit0,4/4 passed in39.2998s on PostgreSQL17. The final migration includes trigger coverage; the direct SQL/bulk case passed. The release regression preserved original118-era projections through the complete123-migration chain; tenant query/soft-delete and rejected tracked foreign writes passed. `HasPendingModelChanges` was false. Evidence: company-audit-final-acceptance-20261031.log. Test startup emitted existing deprecation/analyzer/connectivity and disposable key-storage warnings; no zero-warning claim. Remote CI/staging/production are not yet validated for this change.

## SQL publication artifact (local follow-up)

The exact122-to123 idempotent SQL is saved as company-audit-122-to-123-20261031.sql with a SHA-256 sidecar. The artifact acceptance now compares it with EF-generated SQL, verifies the digest, rejects destructive table/column/row statements, executes the same file twice in a newly created isolated schema, and verifies the historical/new/raw/bulk timestamp scenarios. Only the artifact-specific acceptance needs a new run; previously successful unrelated checks are not repeated. This follow-up changes test packaging and artifacts, not the committed product migration. The candidate CI41129884651 continues for b31a97a; no push will cancel it. Application rollback can retain the additive columns/triggers; a production Down would discard captured audit history and must not be treated as a routine rollback.

Final artifact acceptance: exit0,1/1 in32.4469s. Digest c9d687d9d1a5408fddbd651842ede9df4c10fef6cc3de597686bb5e7727df865 matched; SQL exactly matched EF generation; the same SQL file applied twice in the isolated schema. Historical unknown/new timestamps, direct SQL insertion, bulk update, snapshot parity and foreign-write rejection passed. Evidence company-audit-sql-acceptance-20261031.log. This is local synthetic acceptance, not server staging or production backup/restore acceptance.
