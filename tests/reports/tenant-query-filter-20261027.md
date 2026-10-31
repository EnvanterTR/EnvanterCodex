# P1 tenant query filter implementation — 27 October 2026

User explicitly authorized proceeding with P1 while real payment/customer steps remain pending, overriding the former phase-order wait. Full objective remains intact. No previously passed test was rerun without a relevant source change.

## Implemented

AppDbContext adds named TenantScope filters to each root mapped entity with Guid/nullableGuid CompanyId, and Company.Id. Query predicates use runtime context properties, not a company constant in the shared model. The existing Asset soft-delete predicate is retained as a separate named SoftDelete filter. Program binds a validated company after existing session/membership/role/host/subscription checks for ordinary company handlers, then clears the scope in finally. No claim/request parameter alone selects a company.

Account operations, platform operations and verified checkout paths retain their existing system access and checks. Migration/design-time/background/provisioning contexts begin as system infrastructure; anonymous login/setup/payment flows still use their existing explicit endpoint conditions. This is an additional query/write defense, not replacement of authorization or a claim that every infrastructure query is tenant-scoped.

Tracked SaveChanges/SaveChangesAsync writes reject a Company or CompanyId-bearing entry outside the bound tenant, including a changed original CompanyId; rejection happens before saving any entries. EF IgnoreQueryFilters and rawSQL remain explicit bypass mechanisms: existing endpoint tenant checks and compoundFKs are retained, not claimed to be automatically guarded by tracked-save validation. No migration or schema/data rewrite.

## Local acceptance

Build exit0. Targeted actual PostgreSQL7/7passed52.2370seconds: two new context/filter/write cases, both existing asset PATCH/PUT cases, shared transactional provisioning, company/platform authorization and representative role matrix. Two simultaneously scoped contexts share one Model while returning different assets/companies. SoftDelete-only bypass retains tenant filtering; cleared scope returns no company/assets; another context is unchanged. A tracked foreign write mixed with an own update throws and neither name persists. HasPendingModelChanges false.

Logs: tenant-filter-build-20261027.log, tenant-filter-acceptance-20261027.log, tenant-filter-cleanup-20261027.log. Disposable tenant-filter-20261027 DB/network removed,cleanup0; image retained. Broad remoteCI and exact-source staging/recovery/deployment have not run for this change. Production remains f073752/schema122. Full P1, rawSQL/worker scope audit, real20.4 and customer/payment acceptance are not declared complete.

TAMAM dediğim1ilk yerel tenant filtre paketinin1'i gerçek PostgreSQL test çıktısıyla doğrulanmıştır; uzakCI ve canlı kapıları açık.

## Exact server staging and initial recovery

Candidate 2ee892bcfbe5d6211d8513e93206cc6723500c31 is on PR25. CI40516313967 PostgreSQL job122231024605 is running; no final success is claimed.

Both exact-source server image builds exited0. Isolated web/public containers envtr-stage-web-2ee892b and envtr-stage-public-2ee892b run on internal-only envtr-stage-ae875b6 with no host ports. Runtime asserts read-only web, dedicated clone envantertr_stage_2ee892b, copied media/keys, SMTP credentials/host disabled, payment/setup values blank, trials/reminders false. Fresh SQL pre-web-v1-20261027T135745Z.dump restored independently at schema122/0.

HTTP/proxy probes completed: three host health endpoints ready122/0; anonymous asset and handover/onboarding endpoints401; Secure CSRF; CSS/reset feedback preservation; tenant platform HTML/scripts/API/admin/reauth404; root platformHTML200; root/www admin302 relativeLocation/noCookie while tenantadmin404. All95 original data-table field-projection hashes remained equal after startup/probes. These are anonymous/runtime probes, not authenticated tenant acceptance substitutes.

Initial recovery bundle /var/backups/envantertr/rollback-20261027T140745Z is143MB and verified files/images recovery exit0. No production restore or candidate production deployment. A fresh release-time recovery and full exact CI still gate any rollout. Production remains f073752/schema122.

Static follow-up reviewed all27 IgnoreQueryFilters call sites. Asset row-lock SQL retains parameterized CompanyId plus recordIds/deletion status; kit history joins both CompanyId and recordId; trash, return-name and export-presence LINQ retain explicit CompanyId. This source review does not prove all raw/bulk SQL, workers, cache/media paths or every authenticated endpoint and does not close full P1.
