# Annual existing-company lifecycle acceptance — 24 October 2026

Result: 5/5 passed, exit 0, 1.2723 minutes. Actual application endpoints, Identity reauthentication, PostgreSQL and a synthetic payment provider; no external charge, production change or real inbox test.

Added Existing_company_annual_lifecycle_preserves_paid_time_and_plan_boundaries:

| Initial subscription | Purchase | Expected and observed |
|---|---|---|
| Active, ten days remaining | Different plan | 409, provider never initialized, no order/subscription mutation |
| Cancelled, ten days remaining | Different plan | 409, provider never initialized, existing period preserved |
| Active, already expired | Different eligible annual plan | Activates target plan for 365 days; original end/plan retained in history |
| Cancelled, ten days remaining | Same annual plan | Renews until original end + 365 days |
| Suspended | Same plan | 409 before provider initialization |

Successful cases repeat callback twice, assert exactly one new active subscription/audit, expected payment reference and retained original history. Every case asserts own-company and other-company inventory remains attached to its original company. These assertions are specific lifecycle acceptance, not a replacement for full tenant isolation or live Section 20.

Build and first invocation: annual-lifecycle-20261024.log. The image built, but the first command redundantly added dotnet test to the image's existing entrypoint and failed MSB1008 before running tests. Corrected command used the already built image and did not change product rules:

```powershell
docker compose -p annual-lifecycle-20261024 -f deploy/testing/compose.security-acceptance.yaml run --rm acceptance --filter FullyQualifiedName~Existing_company_annual_lifecycle
```

Successful log: annual-lifecycle-verified-20261024.log. Disposable database/network removed with compose down --volumes --remove-orphans. No product source or migration changed. Real provider, SMTP, authenticated staging and live customer acceptance remain open.
