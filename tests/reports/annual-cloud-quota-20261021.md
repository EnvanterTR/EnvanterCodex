# Annual Cloud package quota implementation — 2026-10-21

Latest user delegated tax and upper-tier choice. Decision: prices tax-exclusive;1000/2000 are finite included-account limits; above2000 custom quote.15-day trial then annual one-time payment; monthly figures are display equivalents. Commercial JSON updated; no tax rate invented.

Implemented quota values in public/admin plan responses, plan validation/persistence, company and platform invitations. Existing null/unlimited records stay null and no memberships are deleted. Admin can edit Cloud quota; public card renders actual quota in5languages. Updated stale unlimited source guard to the new user decision. No migration.

Checks: source guard,5language dictionary2244entries, inventory2239controls and inline JS syntaxPASS. Actual public pricing Chromium with finite10account fixturePASS (initial broad selector matched quota and billing-period lines; targeted semantic quota class corrected once).

Initial PostgreSQL3case run89759FAIL3/3: two old fixtures still had finite1 in the Docker snapshot before source correction; new quota test assumed one existing member but shared fixture has several. Corrected fixtures to explicit null and quota upgrade to existing-member-count+1. Rerun6555 uses existing built image and current test files mounted readonly; result pending. No live price publication or quota enablement.

Remaining: actual15day self-service tenant trial, annual-only new catalog persistence, tax-inclusive final payable total, provider/SMTP acceptance, plan downgrade/reactivation and concurrent quota regression, full Chromium/CI and publication. Do not call this complete pricing rollout.

Rerun6555terminal:2/3PASS (newCloudfinitequota and existing-account multi-company acceptance),1FAIL local regression fixture inherited null from uncappedCloud phase. Explicitly reset local phaseMaxLoginUsers=1; not rerun again this turn per bounded retry rule. Disposable DB/networkremoved. No overallPASS claim; full acceptance pending.

Final current-source targetedPG57953PASS3/3: finiteCloudquota, null/unlimitedCloud/localcapregression, existingaccountmulticompany. Testhost2deprecationwarnings; no failures. IsolatedDB/networkcleaned. Does not prove full annual/trial/tax rollout.
