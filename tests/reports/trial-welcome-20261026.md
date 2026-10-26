# Trial-registration welcome email — 26 October 2026

Scope: Hedef12.4 notification/rollback for the public15-day trial registration path. Local implementation and acceptance passed; NOT deployed. Live remains5018bc5/schema122 with trial registration and payment closed. The user's merchant account is not yet created; that does not prevent this isolated correctness work.

## Reproduced missing behavior

The previous confirmation committed the new company/owner/subscription without sending an access notice. With a valid proof and an unreachable local SMTP port, both new-owner and existing-owner confirmations returned200. The new requirement tests expected503 with no company/trial-right consumption. trial-welcome-first-20261026.log:2/2failed, expectedServiceUnavailable,actualOK.

## Change

After staging and saving the company, portal, owner role,15-day subscription and CloudTrialStarted audit inside the existing transaction, confirmation now sends the appropriate access notice or account invitation. SMTP acceptance precedes commit; sends have a25-second linked cancellation bound. Expected send/configuration/timeout failures return503 and dispose the uncommitted transaction. The same protected proof can be retried before its original expiry; a failure does not consume the one-trial entitlement.

The existing advisory lock/idempotent registrationID path serializes parallel confirmation/replay. A replay of a committed registration returns its existing portal and period without sending a second welcome notice. Existing password hash/security stamp and existing memberships are preserved. Stage, SMTP acceptance, commit and error-type logs identify registrationID without including email contents or reset tokens.

## Verification

- trial-welcome-regression-20261026.log:11/11pass,1.6119minutes. Includes new/existing-owner SMTP failure rollback, same-proof parallel retry with exactly one welcome email, body containing the actual tenant portal, one owner/15-day subscription/audit, pre-existing credential preservation, previous trial expiry/plan/proof/CSRF/one-trial/concurrency tests, SMTP readiness privacy, and both paid new-company rollback regressions.
- trial-welcome-build-20261026.log:Web build0errors/0warnings; test project0errors/10existing warnings. Image trial-welcome-20261026-acceptance:latest built from final source.
- trial-welcome-source-20261026.log:Cloud quotas/15-day annual specification and payment/provisioning source contracts pass.
- Failed-first evidence retained. All email/provider/account/DB data was synthetic on an internal isolated Docker test network. No customer message, signature, payment or production data was changed.

No schema migration or frontend layout change. Existing mail templates are reused. As with paid provisioning, this is database rollback on notification failure, not an atomic transaction with an external SMTP server: an already accepted message cannot be unsent after a later DB failure, and no real inbox delivery or exactly-once SMTP guarantee is claimed.

Next:full remote CI for the combined paid/trial correction, exact staging and refreshed backup/recovery before deployment. Real merchant and Santa/Farma/Demo3 acceptance remain open.
