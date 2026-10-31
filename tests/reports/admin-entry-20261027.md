# Hedef9 root /admin entry — isolated preparation

Prepared in separate checkout admin-entry-20261027 based on exact90cc105 while that candidate's CI was running. Main release worktree and activeCI were not modified/restarted by this package.

Read-only production reproduction: `/admin` and `/admin/` each returned200marketingHTML, noLocation and no platformLoginSubmit. This contradicts the specified root administration entry. The existing actual administration page remains `/platform-admin.html`.

ASP.NET root `/admin` now redirects302 to existing `/platform-admin.html`; the route never creates or authenticates a session. Production/localNginx forward exact `/admin` and `/admin/` requests to the app instead of marketing fallback, preservingHost. The existing pre-static tenant boundary still returns404 on tenant hosts.

LocalAPI/PostgreSQL2/2passed40.1117s: root/www/localhost, trailing slash and irrelevant query all302relativeLocation, noSet-Cookie; tenant404, plus full existing platform-surface boundary test. Actual twoNginx proxy acceptance exited0 for apex/www and both pathforms/query: appupstream reached, originalHost preserved and relative redirect avoids HTTPSoffload downgrade. The upstream is a controlled Nginxstub; actual application redirect semantics are covered separately by the API test, not inferred from the stub.

First proxy run failed aHost assertion because its fetch transport did not preserve the requestedHost; the strict assertion was retained and admin requests use node:http with explicitHost. Same proxy containers reused for the corrected test, then cleaned. Existing app/vendor/module/camera-policy/security-header checks also passed. Routing source verifiers and Node syntax/diff checks passed. DisposableAPI DB/network removed; no product/productionDB or auth policy changed.

Logs:admin-entry-build-20261027.log,admin-entry-api-20261027.log,admin-entry-proxy-20261027.log(firstfailed),admin-entry-proxy-fixed-20261027.log,admin-entry-api-cleanup-20261027.log,admin-entry-proxy-cleanup-20261027.log. No finalremoteCI/staging/deployment for this separate package yet. No UIredesign or newclientcontrols, so no duplicate broad browser run was needed locally. Later releaseCI remains required.

TAMAM dediğim1yerel girişrotası paketinin1'i API/proxytest çıktısıyla doğrulanmıştır; live/overallHedef acceptance remains open.
