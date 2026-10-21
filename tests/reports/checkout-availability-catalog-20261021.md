# Public checkout availability — 2026-10-21

Public status previously treated provider configuration alone as purchase availability, even when the published catalog had no payable Cloud plan. Availability now also requires an active public Cloud plan with a positive monthly or annual price and a supported currency. No credentials, prices or plan publication settings were changed.

Acceptance: isolated PostgreSQL, process48541 exit0,3/3PASS: Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets; Public_checkout_requires_csrf_and_fails_closed_without_provider_configuration; Public_checkout_uses_server_price_verifies_provider_and_provisions_once. Price scenarios cover positive monthly/annual, lowercase supported currency, unsupported currency and zero price. Web and test builds0warnings/0errors; normal disposable-host runtime warnings occurred. Disposable database/network removed. No production migration/deployment or real payment performed.
