# Tenant query-filter preparation — priority decision pending

Hedef6.2 explicitly calls for a global tenant filter in addition to endpoint checks. Current AppDbContext constructor only receives options; its sole model filter is Asset.DeletedAt==null. Program registers five hosted workers and startup migrations; AppDbContextFactory and migration tests also construct the context directly. A filter added only from an unvalidated claim or ambient last-request state would break this boundary. No implementation has been applied in this preparation.

## Required implementation boundaries once P1 is authorized by phase order

1. Explicit per-context access mode: validated tenant scope, authenticated authorized platform scope, or narrowly authorized background/provisioning scope. Unknown request scope must not silently grant cross-company reads. Existing host/membership/session checks remain in front of tenant operations.
2. Startup/migrations/design-time factories, public plan/email-proof/payment callbacks, identity lookup and shared provisioning need deliberate global/system paths. Platform account is distinct from tenantowner. Global identity/platform tables are not assigned a fictitious CompanyId.
3. Inventory all mapped company-owned entities before building filters; preserve Asset soft-delete filtering. The query model must read runtime context properties, not cache the first company's constant value. Two contexts in one process and concurrent requests must never exchange scope.
4. Keep endpointCompanyId/compoundFK/CSRF/role/location restrictions. Query filters do not independently protect rawSQL or writes; rawSQL/ExecuteUpdate/ExecuteDelete and tracked cross-company changes require their own scope checks.
5. Hosted workers must enumerate intended companies through an explicit trusted path, then use one fresh tenant scope per operation. Request-to-worker ambient leakage is forbidden. Cancellation/disposal must not leave a global override behind.
6. Model/schema compatibility must be tested including HasPendingModelChanges; no destructive migration or production data rewrite is authorized by merely adding filters.
7. Acceptance must include missing/forged/revoked claims, invalid/otherhost, inactivecompany, platformboundary, backgroundprovisioning, two-context/modelcache and softdelete behavior. Existing GET/PUT/stock/report/import/export/PDF and provisioning regression must remain; real20.4 acceptance is still separate.

Priority constraint reread from Hedef17.3: P0 must finish before P1. Real merchant and normal customer/signature actions are open. A concise user question asks whether to separate these external pending steps and proceed with P1; no answer has arrived. Work dependent on changing this order remains unstarted. This preparation identifies concrete next implementation and risk boundaries, not a substitute feature or completion claim.
