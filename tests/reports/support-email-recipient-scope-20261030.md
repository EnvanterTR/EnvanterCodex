# Support email recipient authorization — 30 October2026

TAMAM (local implementation and focused acceptance): seven PostgreSQL/SMTP-capture tests passed in1.1964minutes, exit0; temporary database/network cleanup completed. Not deployed.

SupportTicketEmailService rechecks authorization immediately before reading a queued message's body and transmitting it. Customer delivery requires current membership in the ticket's company, a confirmed current email matching the queued address, and an active company not soft-deleted. Support-mailbox delivery must still match the current configured support mailbox. QueueCustomerAsync also requires company membership when enqueuing.

Invalid destinations remain unsent and use the existing retry/backoff state with SupportEmailRecipientNoLongerAuthorized; no migration/status invention or automatic redirect to a new email. A restored authorized destination can receive the original queued message. This does not claim race-proof authorization against revocation after the final check or transactional SMTP delivery.

Six cases: removed membership, changed email, revoked confirmation, inactive company, soft-deleted company and replaced support mailbox. Each proves zero SMTP messages, Pending/no SentAt/error/backoff, followed by successful single delivery after exact authorization restoration. The existing end-to-end support ticket/tenant/CSRF/platform reply/audit/SMTP test also passed.

Evidence:support-email-recipient-scope-20261030.log; SupportEmailRecipientAuthorizationAcceptanceTests.cs. No real email recipient, company, credentials or production data changed. The failed model guard proposal is not in product/test code. Full Hedef6.1/P1 and real customer/merchant acceptance remain open.

TAMAM dediğim 1 maddenin 1'i test çıktısıyla doğrulanmıştır.

The initial six-case run passed before adding the inactive-company case; retained in commitb95d10a. The added guard matches SupportEndpoints company.member active-company policy. The final seven-case run passed after that source change; it was not a blind retry of unchanged passing checks.

Remote sourceeae37c7c6ac6453bc7972f1eac5a7f3a73085087 was pushed once after the previous CI completed. FullCI40976671612(#800) is confirmed in progress; continue observing the same run. No release of this candidate has started. Current production8a07aec is separately verified. Do not restart or push while this run is active.
