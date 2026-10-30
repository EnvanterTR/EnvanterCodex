# Legacy audit-date coverage — 30 October2026

Read-only inspection of the fresh122-schema staging cloneenvantertr_stage_eae37c7. No row, timestamp, schema or model was changed. This is not completion ofHedef6.1.

| Missing column | Tables | Populated tables | Existing rows in those tables |
|---|---:|---:|---:|
| CreatedAt |36|11|138|
| UpdatedAt |61|17|178|
| DeletedAt |92|19|178|

Populated tables missingCreatedAt:AspNetUserTokens6,AspNetUsers3,AssetCategories1,AssetEvents11,AuditEntries33,DashboardPreferences1,PlatformAdministrators1,PlatformAuditEntries71,SoftwareLicenseAssignments1,SoftwareLicenseEvents2,SubscriptionPlans8.

Some have other date columns(AccountCreatedAt,OccurredAt,UpdatedAt,GrantedAt,AssignedAt/RevokedAt/EulaAcceptedAt). Column existence alone does not establish that another timestamp is the original row-creation date; no blanket backfill was performed. Some contain no alternative date column. Historical creation times cannot be invented.

A plain-language requirement question is pending: keep unavailable historical timestamps explicitly unknown while automatically requiring timestamps for new records, or have the user supply the missing historical dates. This is a necessary interpretation of the strict timestamp requirement with existing data; it does not silently relaxHedef or change the shared-account architecture.

Source runner:legacy-date-coverage-audit-20261030.py. Private full metadata/count report:/var/lib/envantertr-staging/eae37c7-20261030/legacy-date-coverage.private.json. The report contains counts and column names, not customer values or credentials.
