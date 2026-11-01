# Section 20.4 asset update methods — 2026-10-25

## Requirement and change

Hedef 20.4 explicitly requires foreign asset GET and PUT requests to return 404/403 in both tenant directions. The existing resource update route accepted PATCH only, so PUT could not exercise its tenant-scoped update handler.

`AssetEndpoints` now maps PATCH and PUT to the same existing `UpdateAssetRequest` handler. Request validation, company scoping, authorization, antiforgery, optimistic concurrency, history and sensitive-field protection remain shared. There is no database migration or UI change.

## Verification

The existing asset update acceptance is parameterized for both methods and additionally checks GET and update rejection in the reverse tenant direction. Each case verifies a successful in-tenant update, history, CSRF denial, viewer denial, invalid category denial and stale version/replay rejection.

Command:

```powershell
docker compose -p asset-put-section20-20261025 -f deploy/testing/compose.security-acceptance.yaml run --build --rm acceptance --filter 'FullyQualifiedName~Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history'
```

Result: **2/2 passed**, exit 0, 27.2758 seconds of test execution. Application build: 0 errors/0 warnings; test build: 0 errors with four existing ASP.NET deprecation warnings.

The initial run failed 2/2 in the test harness because a second antiforgery response correctly reused the existing cookie and did not issue another `Set-Cookie` header. The corrected harness reuses that cookie when the header is absent; product security was not weakened. Initial log: `asset-put-section20-20261025.log`; successful log: `asset-put-section20-fixed-20261025.log`.

The successful rerun used the already-built candidate image and a read-only bind mount of the corrected test source, recompiling tests before execution:

```powershell
$testSource = Join-Path (Get-Location) 'tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs'
docker compose -p asset-put-section20-20261025 -f deploy/testing/compose.security-acceptance.yaml run --no-deps --rm -v "${testSource}:/source/tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs:ro" --entrypoint dotnet acceptance test --no-restore --logger 'console;verbosity=normal' --filter 'FullyQualifiedName~Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history'
```

The harness uses synthetic authenticated principals against actual isolated PostgreSQL and the real endpoint pipeline. It does not prove live Santa/Farma logins, real email delivery, or completion of Section 20. Production remains 6198ef6 until separate CI/staging/backup/release gates pass.
