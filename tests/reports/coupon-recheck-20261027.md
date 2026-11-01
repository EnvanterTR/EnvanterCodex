# Coupon validity recheck before provider initialization — 27 October 2026

Local API/PostgreSQL5/5passed,50.8805seconds,exit0. Two new cases start with a valid annual4200TRY/10percent coupon quote3780, then expire or deactivate that same coupon before a fresh existing-company checkout initialization. Normal synthetic owner password/MFA and CSRF paths are used, with an injected synthetic payment transport. Initialization returns400/couponCode validation; provider.Initializations remains0; checkout order is absent; original subscription status, plan and start/end dates remain equal.

Three previous unavailable-period quote cases also passed in the same run. This is five overlapping cases, not five additional product features. ExistingCheckoutFactory/SeedExistingCheckout synthetic fixtures are reused; no real user code/credential, provider, SMTP recipient or production data was used. This validates rechecking before first initialization, not cancellation of an already started protected payment session, real merchant acceptance or every transactional race.

Build exit0; disposable coupon-recheck-20261027 database/network cleanup exit0, image retained. Test-only source change; livef073752 unchanged. New cases are local until next appropriate CI push; activeCI40464013357 on85c4f8c is not restarted.

Evidence: coupon-recheck-build-20261027.log; coupon-recheck-acceptance-20261027.log; coupon-recheck-cleanup-20261027.log.

TAMAM dediğim1yerel ödeme öncesi kupon kontrol paketinin1'i gerçekAPI/PostgreSQLtest çıktısıyla doğrulanmıştır. TamHedef açık.
