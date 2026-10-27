# Tenant filter checkout regression — 27 October 2026

Authoritative CI40516313967 for2ee892b finished: PostgreSQL366passed/1failed (38m29s); Linux security failed6m14s in real existing-company checkout browser acceptance; Windows safety passed. Both Linux failures throw the tracked company write guard at existing checkout InitializeAsync when reached through POST /subscription/checkout. No production deploy was attempted.

The prior exemption classified API checkout initialization/callback/webhook as trusted payment infrastructure, but omitted the server-rendered existing-company checkout route. Its pending CompanyCheckoutOrder has nullable CompanyId until provisioning and uses TargetCompanyId/RequestedByUserId for the verified existing-company purchase; the ordinary company guard therefore rejected SaveChanges. The form uses the same InitializeAsync as the API and retains CSRF, current owner resolution, current password/MFA verification, server-side plan/price/coupon checks and provider verification.

Fix adds exact normalized /subscription/checkout to the existing verified checkout classification, covering GET and POST without prefix-widening any ordinary company route. No query filter or tracked write assertion was removed. The existing real-HTTP and Chromium tests are the regression evidence; do not weaken their expected redirects, provider call count, amount or owner/MFA assertions.

Local targeted PostgreSQL and Chromium tests are running/pending. Full CI and a new exact staging/recovery remain necessary before production. Previous2ee staging evidence applies only to the previous source, not this fix.

Local targeted realPostgreSQL completed exit0:10/10passed,3.0354minutes. Includes failing form redirect/provider/price assertions, ownerMFA/capacity, deleted/deactivated targets, provider error/mismatch, tenant context/write guard and company/platform boundary. Disposable DB/network cleanup completed. Chromium same-source build/test still running; no broadCI success claimed.
