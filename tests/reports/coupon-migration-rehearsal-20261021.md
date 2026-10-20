# Coupon migration rehearsal — 21 Oct 2026

- Base: live PostgreSQL backup daily-20261020T210608Z.dump; SHA-256 sidecar verified OK.
- Isolated restore drill: temporary clone restored successfully, size 14 MB, migration history 117 entries through 20261019201743_AddCompanySecurityPolicies; temporary clone was dropped after validation.
- Candidate: EF Core 10.0.0 idempotent script from migration 117 to 20261020174959_AddSubscriptionCoupons.
- Source correction: terminate the SubtotalAmount = Amount SQL statement so PostgreSQL accepts the generated DO block.
- Script SHA-256: $hash.
- State: script generation verified after application build; execution against restored clone is the next step. No live database or application was changed.
