# Hedef10.3 tenant platform surface boundary

Live read-only reproduction27October02:24:49UTC: apex/platform-admin.html200text/html, Santa/platform-admin.html200text/html, Farma/platform-setup.html200text/html. This contradicts the requested tenant platform404 boundary. It does not by itself prove unauthorized platform API access: the existing API host guard already denies that surface.

Root cause: UseStaticFiles ran before the tenant platform guard. New pre-static middleware returns404/no-store for platform pages, platform-admin JavaScript, /admin subtree and /api/platform subtree on tenant hosts, without touching auth/session state. Existing downstream membership/host checks remain. Apex platform pages and normal company root remain accessible. No migration/authentication relaxation.

The company login's platform link uses an anonymous setup-status response to point to the configured root host on a tenant, while localhost/apex retain the local relative route. It stays hidden until a valid link is received. Client validation rejects credential-bearing/query/fragment/non-HTTPS external destinations. This prevents the former relative link from taking customers into the newly denied tenant path.

Red API acceptance:1/1failed with `north.envantertr.com/platform-admin.html: expected404, actual200`;33.6740s. After fix,7/7passed1.3776min, including the new boundary, five real-Identity shared-session continuity cases and credential-URL scrubbing. Three tenant host forms include trailing dot; pages/assets/API and setup-status root/local links are asserted. Synthetic test accounts only, disposable PostgreSQL.

Standalone390pxChromium with actual product HTML and synthetic transport passed: tenant root link, local relative link, credential-bearing URL hidden, no page errors. Screenshot tenant-platform-ui-20261027/tenant-platform-root-link.png visually inspected. Verify-AuthErrorFlows first failed because its VM did not know the new selector; fixture extended and adds safe/unsafe-link assertions, then passed. UIinventory refreshed/checkpassed2304; locales2260passed. New browser scenario is included in the main hosted entrypoint. Full hosted regression running; completion not yet claimed.

Logs:tenant-platform-surface-red-build/red/fixed-build/fixed-20261027.log,tenant-platform-ui-20261027.log,tenant-platform-browser-build/regression-20261027.log. No customer credentials/MFA/signatures changed. Production remains27e9120/schema122; exact remoteCI/staging/freshbackup/recovery and deployment are still required for this candidate.

## Final local regression

The same full hosted-browser process completed exit0, including the newly imported platform-link scenario. Its earlier running status is historical. Web build0warnings/0errors; test build0errors/10existingwarnings. Routing checks Verify-CoolifyTenantRouting,Verify-TenantWildcardRouting,Verify-TenantSubscriptionAccess passed (the last17assertions). The earlier auth verifier failure was corrected by extending its mock and assertions, not by removing the feature check. Disposable test project cleanup recorded in tenant-platform-cleanup-20261027.log. These results do not change production or close real customer/merchant acceptance.
