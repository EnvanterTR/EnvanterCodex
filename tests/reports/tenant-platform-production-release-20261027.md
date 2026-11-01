# Tenant platform surface production release — passed

Currentresult: exact90cc105deployed and immediate production runtime/source/boundary checks passed. Earlier pending entries below are retained chronologically; fullHedef and customer/merchant acceptance remain open.

Exactcandidate `90cc10503eb0aa6d929f7e077b376fdd1e001b81`. CI40442423575all3jobssuccess; PostgreSQL121985891826completed42m34s(step42m26s). Finaltestcount was not yet read from the virtualized joblog; do not infer it from an older run. PR explicitly showedAllcheckshavepassed/3successfulchecks. Evidence90c-ci-success-20261027.png.

Local7/7API and80PASS-line full browser evidence, exact90cstaging(realproxyPOST404/normalrootaccess/95tablepreservation), fresh030900Zrestore and031252Zrecovery passed. See tenant-platform-surface-20261027.md andtenant-platform-final-server-staging-20261027.md. No migration.

After CI completion, freshened again: dump `pre-web-v1-20261027T054146Z.dump` passed hash/list and independentclone restore withschema122/95tables; current27eSQL/media/keys/images bundle `/var/backups/envantertr/rollback-20261027T054153Z`,143MB, independentfile/image recovery passed. No productionrestore/deploy in the recoveryrecipe. Screenshot90c-fresh-release-recovery-pass-20261027.png.

Coolify saved full90cSHA and persisted acrossreload. Singledeployment `dwypxqvea2jk2sa1pwfwc9lr` started05:43:04UTC; actual import/check-out confirms90c. Completion/runtime/externalchecks pending; startingdeployment is not a completedrelease. Previousproduction27e/schema122 until verified switch. Separateadminentrycommitfbe10f9 remains outside this candidate and was not published.

## Completed deployment and immediate live acceptance

Coolifydwypxqvea2jk2sa1pwfwc9lrSuccess/Finished. Actual web/public/database suffix20261027T054326; exact90cweb/publictags,healthyDB/zerorestarts,webread-onlyrootfs,SMTPrelayTLS587/noSMTPAUTH,missingmerchantkeys,closedtrials,HTTPS callback verified05:48:45UTC. No migration/productionrestore. Evidence90c-deployment-finished-20261027.png,90c-live-runtime-pass-20261027.png.

ThreeHTTPS hostsready122/0passed. Publicboundaryprobe05:49:08UTCcompletedexit0:8plans/closedcheckout-trials; eightgalleryCSS/JS/images sourceSHA256equal; companyHTML/resetHTML/onboardingCSS exact; anonymousdocument/onboarding denials retained. Santa/Farma platformadmin/setupHTML,/admin,platformJS,/api/platform/overview404; separateplatformreauthPOST404; setupstatusrootplatformURLcorrect; apexplatformlogin200. Logs90c-live-boundaries-20261027.logand90c-live-health-20261027.log.

This is actual anonymous/static/runtimeboundary acceptance plus prior authenticatedlocalregression. It does not close normalcustomerMFA/signature/PDFrecipient,fullFarma/GET-PUTisolation/Demo3ownerorrealmerchant acceptance. Separatefbe10f9root/adminaliasremainslocal,notpublishedinthisrelease. TAMAM dediğim1yayının1'i test/komut çıktısıyla doğrulanmıştır.
