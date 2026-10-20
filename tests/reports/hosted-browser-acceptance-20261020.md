# Hosted Chromium Acceptance — 2026-10-20

- Branch: `feat/saas-coupons-web-v1`
- Commit under test: `f4c6e32` (`Fix checkout acceptance fixtures and route boundary`)
- Command: `./deploy/testing/run-hosted-checkout-browser-acceptance.ps1`
- Result: **PASS** (process exit code 0)
- Environment: pinned Playwright Chromium container; browser scenarios use an isolated synthetic API. The separate PostgreSQL acceptance suite also passed 241/241 tests on this commit's source before push.

## Verified flows observed

- Public Cloud checkout: coupon quote and discounted price, company-slug suggestion, editable slug preservation, validation, CSRF and consent, trusted payment-host redirect.
- Account and security: session listing/revocation, API-key lifecycle, security-policy persistence, MFA QR/secret enrollment, verification and recovery codes, and login diagnostics.
- Camera: permission denial, unavailable/busy device, timeout and late permission cleanup, QR and Code 128 decoding, retry after decoder load failure.
- Inventory UI: asset details/history, row and bulk actions, cancel-safe dialogs, CSV/XLSX import/export, filters, pagination, labels and scanner selection.
- Operations: assignments/returns/transfers/reservations, maintenance/audit, purchase orders, stock and component flows, locations and company-unit hierarchy.
- Admin/reporting: roles, platform member and demo flows, plans, audit search/retention, support tickets, prepared/custom/scheduled reports, filtered CSV/PDF.
- Navigation and interaction: all 52 sidebar destinations, keyboard navigation, responsive table behavior, language handling, stale-response guards, and browser-error-free completion.
- Activity history date filtering: both `from` and `to` boundaries were sent and accepted; this passed in the current full run.

This is browser workflow evidence, not production payment-provider, physical-device, live SMTP, production migration, or customer-host deployment acceptance. Hosted PR CI run `39486915502` later completed successfully: all three jobs passed. The separate-device company MFA regression was added after that CI run and passed its targeted local isolated PostgreSQL acceptance test; the updated commit needs its own hosted CI run.
