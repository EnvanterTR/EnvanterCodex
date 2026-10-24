# Saved Coolify provider configuration audit — 24 October 2026

Read-only UI audit of the existing production application resource izrckbfan6xop8b6xgigduvt. No save, update, delete, reset, deploy or credential creation was performed.

The Environment Variables list contains 94 production/preview/derived entries. Presence of a variable name is not proof that a value was configured.

The production edit dialogs completed their value loading. Read-only DOM checks restricted to the visible password input returned exactly one field and hasValue:false for each:

- EMAIL_SMTP_PASSWORD
- IYZIPAY_API_KEY
- IYZIPAY_SECRET_KEY

Password-visibility toggles and Copy value were never used; no secret values were returned, logged or saved. SMTP host edit also displayed an empty field, but the first broad boolean input inventory was not sufficiently scoped to establish an independent host assertion, so the three scoped checks above are the authoritative saved-secret result.

This rules out 'saved but not deployed' for these three required production credentials on this specific resource. It does not establish whether the user has a provider account elsewhere, and does not inspect preview secrets or every external account. The UI also reports no connected secret manager tokens on this resource.

User was asked whether the admin@envantertr.com sender and iyzico accounts are accessible, explicitly requesting no secrets in chat. Real SMTP/inbox and iyzico acceptance require valid provider configuration. Existing source and synthetic tests do not replace that evidence.

Farma controlled IAB tab was rechecked and still requests MFA. User-reported login completion is retained as a report, not treated as agent-visible authenticated acceptance. No recovery code or MFA reset was used.

Evidence screenshots:
- coolify-saved-smtp-empty-20261024.png
- coolify-saved-payment-empty-20261024.png
- coolify-saved-payment-secret-empty-20261024.png

CI c328a2a run 40077928353 was confirmed in_progress, not restarted. Prior exact-source ae875b6 CI is 3/3 successful; the new lifecycle test head needs its own completed result.
