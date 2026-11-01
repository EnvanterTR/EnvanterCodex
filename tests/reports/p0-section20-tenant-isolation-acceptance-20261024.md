# P0 Section 20.4 tenant isolation acceptance — 24 October 2026

## Result

Three focused acceptance tests passed against isolated PostgreSQL: **3/3**, exit code 0, total 2.1753 minutes.

The two synthetic companies stand in for Santa and Farma. The tests establish that:

- an authenticated user cannot read a foreign company's asset; the API returns `404` and list results omit that foreign asset;
- an authorized tenant admin's attempt to `PATCH` a foreign company's asset returns `404`, while the same test confirms an in-tenant edit succeeds;
- an authenticated company session presented on another tenant's subdomain is rejected with `401` (as is an unknown tenant host).

## Command and log

```powershell
docker compose -p p0-section20 -f deploy/testing/compose.security-acceptance.yaml run --build --rm acceptance --filter 'FullyQualifiedName~Role_downgrade_revocation_and_company_suspension_take_effect_on_the_next_request|FullyQualifiedName~Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history|FullyQualifiedName~Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain'
```

Full output: `tests/reports/p0-section20-tenant-isolation-20261024.log`.

## Limits

This proves the cross-tenant API/session invariant in synthetic companies and isolated PostgreSQL. It does not prove Santa/Farma live-account data, all ten Section 20 steps, third-company signup, or browser-based customer acceptance. The Windows Computer Use bridge stopped before browser inspection because URL-policy enforcement is unsupported for the current browser. No live tenant or production data was changed.
