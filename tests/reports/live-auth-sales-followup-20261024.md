# Live authentication and sales configuration follow-up — 24 October 2026

## Verified observations

- Reused the existing Coolify terminal. A temporary WebSocket disconnect recovered by selecting the existing localhost server; no new credentials or access grants.
- Production web container environment has EMPTY_OR_ABSENT for Email__Smtp__Host, Email__Smtp__Username, Email__Smtp__Password, Email__FromAddress, Iyzipay__ApiKey and Iyzipay__SecretKey. Only presence flags were printed, never values. This is a runtime environment observation, not an audit of every saved Coolify setting or external secret store.
- Production /app has no appsettings*.json within two levels; only media and data-protection key volumes are mounted. A first attempted JSON inspection failed because appsettings.json does not exist and its inline Python was malformed; it produced no configuration result. The subsequent metadata/file inventory established the source limitation.
- Existing staging database, proxy-aware web and public-site containers are still running.
- Compared the real backup media/key archives with current production volume contents: PRODUCTION_MEDIA_MATCH_BACKUP and PRODUCTION_KEYS_MATCH_BACKUP. Private rehearsal archive passes gzip validation.
- Through the actual isolated staging Nginx, /api/checkout/status returns available:false/provider:null; /api/trials/status returns available:false, trialDays:15, billingPeriod:annual; /api/plans returns []. No real email/payment was attempted and no plan was published.
- Exact candidate ae875b6 Actions run 40073522201 completed all three jobs successfully. Compact terminal job evidence: pr25-ci-ae875b6-20261024.json.
- Farma live password was accepted and the page requested Authenticator/recovery proof. User reported completing login, but the agent-controlled IAB Farma tab still showed the MFA field. Full authenticated customer acceptance is therefore NOT VERIFIED. No pasted recovery code was used, no MFA bypass/reset, no customer data written.

## Next work

Complete authenticated Santa/Farma and platform flows after the accessible browser session is authenticated. Configure and verify real SMTP/iyzico through secure provider settings; never put provider secrets into chat, source or reports. Publish approved annual plans through the audited management path only after relevant release acceptance. Section 20 Demo3 provisioning/isolation and actual inbox/payment evidence remain open.

Evidence: coolify-backup-current-files-20261024.png and coolify-stage-sales-gates-20261024.png. Basic staging/restore evidence remains in real-server-staging-acceptance-20261024.md. Production application/schema remain unchanged.
