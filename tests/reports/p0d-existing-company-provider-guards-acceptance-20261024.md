# P0-D existing-company checkout provider guards — 24 October 2026

## Result

The targeted existing-company checkout acceptance suite passed: **7/7 tests**, exit code 0, in 1 minute 40 seconds.

The acceptance confirms that:

- a successful-looking provider payload with the wrong currency is rejected and cannot activate a subscription;
- an unavailable/failing provider result returns `503 ServiceUnavailable` and leaves the order pending without activation;
- deactivating the target company before the callback records `ProvisioningFailed` and does not activate a subscription;
- deleting the target company before the callback records `ProvisioningFailed` and does not recreate the tenant or activate a subscription.

The same targeted suite also retained the existing owner-scoped checkout page and purchase-flow cases. The company, order, payment reference, and subscription state were checked after each relevant callback.

## Command

```powershell
docker compose -p p0-checkout-page -f deploy/testing/compose.security-acceptance.yaml run --build --rm acceptance dotnet test tests/EnvanterTR.SecurityAcceptance/EnvanterTR.SecurityAcceptance.csproj --filter 'FullyQualifiedName~Existing_company_checkout'
```

## Boundaries

This is isolated API/database acceptance with a synthetic provider. It does not verify an interactive browser purchase, a real iyzico merchant account/callback, SMTP delivery, staging, or production readiness. Those gates remain open in `docs/P0-CHECKOUT-GAPS.md`.
