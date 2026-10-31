# P0 Section 20.5 platform administrator acceptance — 24 October 2026

## Verified subset

Three focused acceptance tests passed against isolated PostgreSQL: **3/3**, exit code 0, total 2.9147 minutes.

- A company tenant administrator is forbidden from the platform overview; a platform administrator can open it and the synthetic catalog contains at least two companies.
- Initial platform administrator access requires Authenticator enrollment before the panel is available.
- TOTP is required for second-step sign-in and single-use recovery; after MFA is disabled, platform overview access returns HTTP `428 Precondition Required`.

Full output: `tests/reports/p0-section20-platform-admin-20261024.log`.

## Required subset still missing

The target also calls for platform “Panele gir” impersonation, a visible yellow banner, audit evidence and a return-to-platform action. A source search found no impersonation endpoint or UI action in the current candidate, so these steps are **not implemented or accepted**. Real platform-admin and Santa/Farma live sessions are also unverified.

## Command

```powershell
docker compose -p p0-section205 -f deploy/testing/compose.security-acceptance.yaml run --build --rm acceptance --filter 'FullyQualifiedName~Company_member_and_platform_admin_boundaries_are_enforced_over_http|FullyQualifiedName~Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use|FullyQualifiedName~Platform_admin_first_login_accepts_password_then_requires_mfa_setup_before_panel'
```

All data and accounts used by the tests were synthetic and isolated. No production state changed.
