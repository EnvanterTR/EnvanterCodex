# Rate-limit rejection feedback — 31 October 2026

The reported platform login screen displayed only the generic429 error. ClientRateLimits now returns a localized JSON message, retryAfterSeconds and a Retry-After header from the actual limiter lease. Rejection responses are no-store. The existing platform API helper already displays the server message. When a lease has no retry metadata, the message requests a wait without inventing a duration.

Existing policy thresholds and trusted-IP partitioning are unchanged: login10/5min,step-up5/5min,customer-api120/min,checkout5/10min. No production password, OTP, cookie or account was used by this test; it exercises the production named limiter policies in TestServer with documentation-only addresses.

Validation:8/8passed,exit0,3.1711s. Each named policy was exhausted under verified and untrusted forwarding. Permit counts and forged-header isolation remain intact;429 header/body seconds match and are positive, and no-store is present. Evidence rate-limit-retry-feedback-20261031.log. Existing build deprecation/analyzer/NU1900 warnings remain. This proves the limiter response contract, not an authenticated live login or a production deployment. This feature does not remove account/MFA requirements or resolve an already active rate window by resetting it.
