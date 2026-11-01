# Recoverable asset catalog retention — 31 October 2026

Reproduced against pre-fix product source63fa33f: deleting a brand/model referenced only by a trashed asset returned204 rather than409 (two failing cases); category retention already returned409 through its restrictive foreign key. This can leave recoverable asset brand/model identifiers dangling. Evidence trash-catalog-retention-baseline-20261031.log,1/3pass2/3expectedfail.

The three catalog delete guards now include trashed assets by disabling only the named SoftDelete filter. TenantScope and explicit CompanyId predicates remain. The user message explains that trash entries count as references. No migration or removal of existing data is involved.

Final PostgreSQL/real HTTP acceptance4/4passed,exit0,41.6310s: all three referenced catalog deletes return409; a foreign catalog ID returns404; the actual asset restore-from-trash endpoint succeeds and restored catalog references still resolve. Existing brand/model/category/image tenant acceptance also passed. Evidence trash-catalog-retention-fixed-20261031.log.

Scope: API catalog deletion/asset restoration, not complete DeletedAt coverage, universal relational constraints, concurrent direct SQL writers, all endpoint authorization, full P1 or production acceptance. In particular, this change does not add the missing Asset brand/model database foreign keys; current live legacy references must be audited before a separate constraint migration. Local only; remote combined CI/staging/live remain open.
