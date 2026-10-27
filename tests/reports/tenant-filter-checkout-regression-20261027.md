# Tenant filter checkout regression — 27 October 2026

Authoritative CI40516313967 for2ee892b finished: PostgreSQL366passed/1failed (38m29s); Linux security failed6m14s in real existing-company checkout browser acceptance; Windows safety passed. Both Linux failures throw the tracked company write guard at existing checkout InitializeAsync when reached through POST /subscription/checkout. No production deploy was attempted.

The prior exemption classified API checkout initialization/callback/webhook as trusted payment infrastructure, but omitted the server-rendered existing-company checkout route. Its pending CompanyCheckoutOrder has nullable CompanyId until provisioning and uses TargetCompanyId/RequestedByUserId for the verified existing-company purchase; the ordinary company guard therefore rejected SaveChanges. The form uses the same InitializeAsync as the API and retains CSRF, current owner resolution, current password/MFA verification, server-side plan/price/coupon checks and provider verification.

Fix adds exact normalized /subscription/checkout to the existing verified checkout classification, covering GET and POST without prefix-widening any ordinary company route. No query filter or tracked write assertion was removed. The existing real-HTTP and Chromium tests are the regression evidence; do not weaken their expected redirects, provider call count, amount or owner/MFA assertions.

Local targeted PostgreSQL and Chromium tests are running/pending. Full CI and a new exact staging/recovery remain necessary before production. Previous2ee staging evidence applies only to the previous source, not this fix.

Local targeted realPostgreSQL completed exit0:10/10passed,3.0354minutes. Includes failing form redirect/provider/price assertions, ownerMFA/capacity, deleted/deactivated targets, provider error/mismatch, tenant context/write guard and company/platform boundary. Disposable DB/network cleanup completed. Chromium same-source build/test still running; no broadCI success claimed.

## Chromium follow-up: pending order visibility

The first local Chromium rerun terminated exit139 after the form successfully reached the synthetic provider. It then received a non-JSON response reading /api/subscription/checkout/orders/{id}: the pending order's CompanyId is null, so the ordinary TenantScope filter hid it. This is a distinct follow-up on the same nullable pre-provisioning model, not a passed browser test.

ExistingCompanyCheckoutEndpoints now disables only the named TenantScope filter on this one order projection, after ResolveExistingCheckoutOwnerAsync, with Id + TargetCompanyId + RequestedByUserId required in the same query. It does not disable role/host/session authorization or expose all orders. The HTTP form test adds own pending-order200/PaymentStarted plus foreign-target404 and same-company other-user404. Local PostgreSQL and Chromium tests for this updated source are running; e80370c staging/CI belongs to the prior form-only fix and is not final release evidence.

Updated-source validation completed: PostgreSQL3/3passed1.9053minutes, exit0 and disposable DB/network cleanup; existing form test includes own pending order200 and both foreign target/other requester404 assertions. Actual Kestrel + PostgreSQL + Chromium harness exit0, browser form redirect and callback replay activate exactly one paid period; owner/viewer memberships, trial history and both companies' assets preserved, viewer denied and zero browser errors. Evidence: tenant-filter-order-scope-fix-20261027.log, tenant-filter-checkout-browser-order-fix-20261027.log and checkout-390.png. Synthetic provider only, no external payment/email. Full exact-source CI/staging and production remain pending.
