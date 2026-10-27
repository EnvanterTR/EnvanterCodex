# P0 coupon period and subscription preservation acceptance — 27 October 2026

Result: localAPI/PostgreSQL5/5passed, exit0,32.3547seconds. Test-only change; production remains verifiedf073752 and no product code/migration/configuration changed.

Three new cases exercise real anonymous POST /api/checkout/quote for expired, future and inactive coupons on a synthetic annualTRY2400 plan. Each gets400/couponCode validation; no-coupon annual quote remains200/2400/zero discount. No checkout order is created; the existing company serialized fields and companyasset count remain equal, and coupon dates/activeflag remain unchanged. This proves quote rejection/read-only behavior, not payment initialization/webhook or realmerchant acceptance. Millisecond-aligned synthetic dates avoid relying on PostgreSQL submicrosecond rounding.

Adjacent existing cases passed in same run: Platform_coupon_management_is_platform_only_validated_and_audited; Expired_cloud_subscription_preserves_read_access_and_blocks_company_writes. The latter separately proves expired subscription access restrictions/data preservation; coupon quote expiry is not claimed to revoke a paid subscription.

First run2passed/3failed39.9608seconds: new fixture omitted CreatedByUserId and hit FK_SubscriptionCoupons_AspNetUsers_CreatedByUserId before reaching quote. Added normal synthetic fixture user; assertion requirements unchanged. Both failed and successful logs retained. Fixedbuild exited0; existing analyzer warnings retained. Docker projectcoupon-period-20261027 DB/network removed, cleanup exit0; image retained.

Evidence: coupon-period-build-20261027.log; coupon-period-acceptance-20261027.log(firstfixturefailure); coupon-period-fixed-build-20261027.log; coupon-period-fixed-acceptance-20261027.log; coupon-period-cleanup-20261027.log. RemoteCI for new test is pending; no new deploy needed for test-only acceptance. FullHedef12.5 real customer/coupon lifecycle and merchant gates remain open.

TAMAM dediğim1yerel kabul paketinin1'i gerçekAPI/PostgreSQLtest çıktısıyla doğrulanmıştır.
