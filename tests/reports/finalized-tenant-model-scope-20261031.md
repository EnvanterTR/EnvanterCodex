# Finalized tenant model startup guard — 31 October 2026

Status: local validation running; no publication or schema change for this guard.

The previous OnModelCreating proposal remains archived after its two failed attempts. This implementation does not restore that hook or add an IdentityPasskeyData exemption. It validates the provider's finalized relational table/view mappings after builder.Build, before local recovery or migration execution. Owned JSON follows its owner, inherited entities follow their root. Exact shared Identity/platform/catalog types and four conditional pre-provisioning/platform record types retain the existing shared-account architecture. Other persisted roots require a Guid company identifier (Company.Id for the tenant root), non-null scope unless explicitly conditional, and a named TenantScope filter. This checks configuration presence, not mathematical correctness of arbitrary filter expressions or complete endpoint authorization.

Validation covers the real finalized application/provider model and snapshot parity, actual Identity schema3 passkey JSON, missing/nullable/text company identifiers, missing filters, views, owned JSON and accepted company-scoped mappings, plus existing real PostgreSQL query-scope/foreign-write/soft-delete regressions. No database connection is used by model-only fixtures. Source helper and Program startup are uncommitted pending the result. Full P1/Hedef, migrations/deletion semantics, staging and real customer acceptance remain open.

Primary source review for this approach:
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationalentitytypeextensions.gettablemappings?view=efcore-10.0
- https://raw.githubusercontent.com/dotnet/efcore/v10.0.0/src/EFCore/Metadata/IReadOnlyEntityType.cs (named filter metadata)
- https://raw.githubusercontent.com/dotnet/efcore/v10.0.0/src/EFCore/Metadata/RuntimeEntityType.cs (runtime filter metadata)
- https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Identity/EntityFrameworkCore/src/IdentityUserContext.cs (schema3 owned JSON passkey configuration)

Final local result:13/13 passed, exit0,27.4121s. Real application model/snapshot and Identity schema3 passkeys passed; unscoped table/view/owned-root and missing-filter cases rejected; accepted table/view/JSON ownership cases and two real PostgreSQL tenant query/write/soft-delete regressions passed. First run11/13 failed only because both view fixtures explicitly configured HasKey before HasNoKey. Corrected fixtures select one mapping before model finalization; product guard was not weakened. Evidence finalized-tenant-model-first-20261031.log and finalized-tenant-model-final-20261031.log. No third attempt was needed. Existing build/test warnings remain; not a zero-warning claim. Remote CI/staging/live for this new source have not run.
