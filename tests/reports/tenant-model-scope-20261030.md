# Explicit tenant data model scope — 30 October2026

Status: DOĞRULANMADI; corrected acceptance run is active (exec session6791). No deployment or schema change.

AppDbContext now validates each root entity before generating tenant filters, instead of silently accepting a newly introduced unscoped business entity. A business entity requires a Guid company property and non-null mapping; Company uses its root Id. Existing shared account/Identity/plan/platform types are explicit. Checkout orders, demo requests, platform audit entries and sessions retain their conditional company scope because they can exist before provisioning or for platform-only actions. Owned values inherit the owner's scope.

Initial acceptance:4/7 passed;3 failed because IdentityPasskeyData was treated as an independent company entity. This was a local startup/model failure, not a live incident. Preserved evidence:tenant-model-scope-before-passkey-fix-20261030.log. Corrective change: owned values recursively validate their owner; shared IdentityUserPasskey<Guid> is explicitly classified with the other shared Identity types. Do not bypass validation for arbitrary unrecognized types.

The corrected run covers the real provider model without schema drift, rejection of missing/nullable/text company properties in future business records, acceptance of a required Guid and existing database query-scope/soft-delete/foreign-write atomicity regressions. Its result is pending.

Hedef6.1 audit timestamp/deletion columns remain incomplete. This change does not add those columns or claim full isolation/customer acceptance/P1 completion. Shared account preservation follows the existing multi-company membership architecture; the user was not asked to design database internals after explaining unfamiliar terminology.

## Final result: BAŞARISIZ, isolated proposal

Second run also passed4/7 and failed3/7 in35.6687seconds with the same IdentityPasskeyData model error. No third test attempt was started. AppDbContext call was removed; git diff confirms no product-source changes remain from this proposal. Proposed source/test files were moved to .cs.txt artifacts outside the build/test projects. Existing successful tenant filters remain intact; no source, schema, credentials or production runtime was weakened.

Primary-source review: ASP.NET Core10 IdentityUserContext configures passkeys as owned JSON only in schemaVersion3; schemaVersion1/2 ignore the passkey entity. This explains why treating in-progress OnModelCreating metadata as finalized persisted entities is unsafe (inference from the source plus the observed failure). A future correction must validate finalized mapped entities, with both current Identity schema and passkey schema coverage, rather than adding arbitrary exemptions. Source:https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Identity/EntityFrameworkCore/src/IdentityUserContext.cs.

The timestamp/deletion-field requirement and this startup guard remain incomplete. This report is failure evidence, not completed P1 work.
