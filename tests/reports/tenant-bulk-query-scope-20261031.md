# Tenant bulk query scope acceptance — 31 October 2026

Status: local acceptance passed,1/1,exit0,24.7209s. This package adds a regression test; it does not claim a new product defect or complete P1.

Uses the actual PostgreSQL-backed application fixture, AppDbContext global query filters, and EF bulk update/delete commands which bypass tracked SaveChanges validation. Synthetic categories belong to two independently seeded companies. CompanyA cannot read, bulk-update or bulk-delete the foreign category; disabling only Asset SoftDelete does not permit updating a foreign asset. The own-category update and delete each affect exactly one row. A cleared company context reads no categories and affects zero rows for both bulk write types. An independently scoped context verifies the foreign category name, the original foreign asset name and the accepted own update; the foreign category remains available to companyB.

Scope limits: this proves these category/asset query paths, not all endpoints, direct SQL bypasses, tenant RLS, every business entity, or real customer acceptance. Existing startup model guard and audit migration are present in the tested local source. No product/schema change was made for this package. Standard existing build/disposable-key warnings remain. Evidence tenant-bulk-query-scope-20261031.log.
