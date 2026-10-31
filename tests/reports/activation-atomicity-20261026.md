# Reliable invitation/password-reset completion — 26 October 2026

Status: local implementation/API/UI acceptance passed; full hosted-browser result, exact CI/staging and release are not yet established at this report's creation. Production remains9585fae/schema122. This is P0-E invited-account activation work, not a claim that real merchant/customer acceptance is complete.

## Reproduced failures

1. Actual PostgreSQL/Identity fault injection rejects only the disposable fixture user's EmailConfirmed update. On old source, the reset request fails after its password/security-stamp save; the original-password preservation assertion fails. Original log activation-atomicity-first-20261026.log, failed1/1,44.0344s. No real account, email, password or production database was used. This synthetic failure is not asserted as the cause of the earlier real Santa Invalid-token report; its separate investigation remains in invitation-password-retry-20261025.md.
2. Chromium submits a valid synthetic reset after a503 retry. Old HTML hides the whole form containing the success message, so the message is present but invisible. The first UI log explicitly records the hidden message timeout. A bare/invalid link has the same placement problem. Original activation-atomicity-ui-first-20261026.log retained.

## Change

The same scoped AppDbContext already used by Identity now encloses password/stamp changes, membership audit entries, platform audit, session revocation and invitation email confirmation in one transaction. Invalid proof/password still rejects; pre-commit failure disposes the uncommitted transaction. Database errors return503 with a neutral retry message and log only exception type. Successful commit keeps the existing single-use token and login/MFA behavior. No new credential, bypass, external side effect, migration or schema change.

Reset-page status feedback sits outside the form, so both success and missing-link errors remain visible after form hiding. Failure retains the form/fields for an explicit retry. New Chromium case is included in the existing hosted-browser CI entrypoint and image.

## Local acceptance

- First fixed API target2/2 passed47.7227s: injected storage failure/rollback/retry/replay plus existing rejected-password invitation retry.
- Final6/6PostgreSQL regression passed1.0976min: those2, existing recovery/single-use coverage, platform member revoke/reset action, login CSRF refresh and complete API authorization inventory. New test checks unchanged password hash/stamp, unconfirmed email, retained active session and absence of success audits after failure; removing the fault permits the same token to activate once, revoke sessions and persist exactly one company/platform reset audit. Source/log activation-atomicity-regression-20261026.log.
- Final Web build0errors/0warnings; test build0errors/10pre-existingwarnings. DDL fault identifiers derive exclusively from the fixture Guid; the two narrow EF1002 suppressions explain why SQL identifiers cannot be parameterized. No arbitrary input is interpolated. Final build log retained alongside the first/fixed logs.
- Synthetic Chromium case passed: fragment removed from address, CSRF header, same proof/body retry after503, retained error form/button, visible success with hidden form, visible missing-link message,390px/no JavaScript errors. PNGs activation-atomicity-ui-20261026/reset-retry-error.png,reset-retry-success.png,reset-missing-link.png. Test transport is separate from actual API/PostgreSQL evidence.
- Existing UI inventory2304controls matches; authentication-error and2260-entry locale dictionary verifiers passed. No new UI controls were added by moving feedback.

The database transaction guarantees all-or-none persisted state, not delivery of a successful commit acknowledgement over a lost connection. A commit acknowledgement failure can leave the whole transaction committed; neutral error reporting does not assert that it was definitely rolled back. No real user password/reset/MFA action was performed. Full remote CI, fresh source-specific stage/backup/recovery and deployment remain required.

## Full local browser regression completed

Rebuilt the hosted-browser image from the final sources and ran its complete existing entrypoint including the new reset case:exit0,79PASSoutputlines. This is not a claim of79distincttestcases. Evidence activation-atomicity-browser-build-20261026.log and activation-atomicity-browser-regression-20261026.log. The temporary API test database/network was removed; cleanup log retained. Local API/UI/regression acceptance is complete; remote release gates remain open.
