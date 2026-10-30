# Support and operational recipient authorization — production release candidate, 30 October 2026

Exact candidate d19cfe549dfab76c2088ed1425d9733456db360e. Support outbox current membership/address/confirmed-address/company-active checks passed7/7 local PostgreSQL/SMTP cases. Operational reminder current assets.read permission checks passed5/5 including unit/location regression cases. No migration change.

Full CI41004287391 (#802): all3 jobs successful; Linux security8m18s, Windows3m14s, PostgreSQL51m55s. This CI includes the corrected browser scenario clock and both recipient checks. It does not include later API-response/workspace reset source changes.

Exact staging /var/lib/envantertr-staging/d19cfe5-20261030 passed build.exit0,prepare.exit0,acceptance-probes.exit0. Runtime/HTTP/proxy/CSRF/anonymous/platform boundaries passed. All95 original table field projections preserved; schema122/0. Bounded private web/public, separate SQL/media/keys and no SMTP/payment/trial effects.

Fresh release SQL pre-web-v1-20261030T050211Z.dump passed hash/list/independent restore122/95. Current8a07 recovery bundle /var/backups/envantertr/rollback-20261030T050216Z,143MB, recovered SQL/media/keys/images independently. No production restore.

Coolify commit selection saved and reloaded as exactd19cfe5 before one Redeploy click. Deployment handle/result/runtime/external source checks still need confirmation. Do not click Redeploy again from generic unapplied-config banners or observation delay.

Later local API/workspace reset changes are separate, unpushed and not claimed live. FullHedef/P1, all-table audit fields and real merchant/customer acceptance remain open. User date CSV was safely delivered locally, not placed in Git/public site.

Evidence: operational-role-ci-success-20261030.png; operational-role-stage-pass-20261030.png; operational-role-fresh-recovery-pass-20261030.png; operational-role-stage-progress-20261030.md.

Single deployment qfut0dslqw1dlnybzxcp8llu confirmed in progress, started2026-10-30 05:37:04UTC. Coolify log explicitly imported exactd19cfe549dfab76c2088ed1425d9733456db360e and began image builds. Generic unapplied-config banner is not a failed deployment; no second Redeploy was issued. Live runtime/result checks remain open.
