# Paid new-company SMTP failure rollback — 26 October 2026

Scope: Hedef12.4, public verified-payment creation of a NEW company. Implemented and locally accepted; NOT deployed. Live remains5018bc5/schema122, payment/trial readiness closed. No schema migration in this change. Existing-company renewal and administrator-attested manual company creation retain their distinct existing behavior.

## Reproduction and correction

With a synthetic verified provider result and an unreachable local SMTP port, the old callback committed company/owner membership/subscription and returned success despite notification failure. paid-provisioning-email-first-20261026.log:2/2 fail, expected ProvisioningFailed, actual Provisioned, for new and already-existing owner accounts.

The new paid-company branch stages and saves its company, slug, owner role, subscription, order and PaidCompanyProvisioned audit inside the database transaction. Access/invitation SMTP acceptance is required before commit, with a25-second cancellation bound. A send exception disposes/rolls back staged new records, then separately records ProvisioningFailed plus the verified payment reference and paid timestamp. The result is support guidance without an active portal link. A later valid callback can retry that same payment without another initialization/charge. Existing account password hash/security stamp are not modified.

Structured logs identify staged setup, SMTP acceptance and commit using checkoutID; errors record exception type without mail/token/card contents. Successful audit includes checkoutID and planID. SMTP acceptance does not prove recipient delivery.

## Evidence

- paid-provisioning-email-fixed-20261026.log:2/2 pass,36.8343s. New/existing-owner rollback, payment retention, safe same-payment retry, one company/membership/subscription, preserved existing credentials and no second email on terminal replay.
- paid-provisioning-email-regression-20261026.log:32/32 pass,5.0508minutes, using the final source/test including the single successful audit assertion. Covers public checkout, pre-payment email proof, existing-company annual lifecycle/concurrent callbacks/security, and the existing manual invitation retry behavior.
- paid-provisioning-email-build-20261026.log: Web build0errors/0warnings; test project0errors and10existing warnings. Exact local image paid-provisioning-20261026-acceptance:latest.
- paid-provisioning-email-source-20261026.log: checkout and platform provisioning source contracts both pass.
- Failed-first evidence retained. All provider/SMTP/accounts were synthetic within the isolated internal paid-provisioning-20261026 test environment. No real money, customer mail, credentials or production data was changed.

## Limits and next work

This establishes database rollback on notification failure, not a distributed transaction with SMTP. A message already accepted by an SMTP server cannot be unsent if a later database commit fails; no exactly-once external email or real recipient delivery guarantee is claimed. Request cancellation and infrastructure faults retain their existing broader callback behavior; real provider acceptance remains required.

Next inspect/complete the trial-registration welcome notice under the same12.4 requirement, then run complete candidate CI and release gates. The user's missing merchant account and real Santa/Farma authentication/signature steps remain external dependencies; they do not prevent this local correctness work.
