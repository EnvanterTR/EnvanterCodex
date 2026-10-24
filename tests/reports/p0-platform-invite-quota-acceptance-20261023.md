# P0 platform invitation quotas — 2026-10-23

- Status: PASS for focused quota/invitation acceptance and the CI source check sweep.
- CI reproduction: Run `39984118678`, Linux security job `120518734620`, stopped at `Verify-PlatformMemberInvites.mjs:17`: its assertion required `DeploymentKind != "cloud"`, contrary to the approved finite Cloud seat quotas.
- Change: Require the actual seat-limit comparison and explicitly reject a Cloud exclusion. The real API quota test now runs for both tenant administration and platform administration. It verifies rejection at capacity without creating an account, admission after increasing the limit, rejection when full again, and preservation of existing memberships. The existing invitation/retry/email behavior test is also retained.
- Runtime command: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0-invite-quota run --build --rm acceptance --filter "FullyQualifiedName~Cloud_plan_user_quota_is_public_and_enforced_without_removing_existing_members|FullyQualifiedName~Platform_admin_can_invite_new_and_existing_members_with_company_scope_and_retryable_email"` exited 0; 3/3 passed, 1.1135 minutes. Web build reused its successful cache; test build had four existing ASP.NET deprecation warnings and zero errors.
- Source sweep: Extracted and ran all 78 distinct `node tools/Verify-*.mjs` checks referenced as standalone commands in CI; 78/78 passed before the separate SMTP-readiness change. This is source-contract coverage, not 78 end-to-end product acceptances.
- Evidence: `tests/reports/platform-invite-seat-quota-20261023.log`, `tests/reports/p0-ci-source-checks-20261023.json`. Isolated database/network removed after the run. No production data/settings changed.

TAMAM dediğim 3 kabul testinin 3'ü test çıktısıyla doğrulanmıştır.
