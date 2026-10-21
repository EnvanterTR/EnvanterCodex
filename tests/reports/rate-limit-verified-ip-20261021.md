# Verified client IP for production rate limits — 2026-10-21

Inspection found all four named production policies still reparsed the final raw X-Forwarded-For hop, overriding the address validated by TrustedClientIp. The previous generic HTTP limiter tests did not cover this production registration.

Moved unchanged limits/windows to shared ClientRateLimits.Configure, used directly by Program and new HTTP tests. Policies now use only Connection.RemoteIpAddress; only TrustedClientIp may interpret proxy headers. Login10/5min, step-up5/5min, customer API120/min and checkout5/10min preserved.

Acceptance:29/29PASS in offline Docker, process47480exit0. Eight new cases exercise each actual named policy for verified Cloudflare client separation and forged CF headers on direct-origin requests. Existing resolver/HTTP tests retained. Four test-host deprecation warnings, no errors. Two updated source verifiers and diff checkPASS. No DB/network/production configuration change. Production enablement and external acceptance remain open.

CI39593750246: Linuxbrowser/security andWindowsPASS; PostgreSQL269/270, legacy Login_rate_limit_partitions_by_forwarded_client_ip failed because it explicitly expected arbitrary forwarded headers to create new buckets without trusted ingress. Updated that negative integration test to assert429 for a changed untrusted XFF, while keeping actual verified-proxy positive tests. Targeted isolated PostgreSQL testPASS1/1, process35693exit0, DB/networkcleaned. Web0warnings/errors; testhost4deprecationwarnings. No production enablement.
