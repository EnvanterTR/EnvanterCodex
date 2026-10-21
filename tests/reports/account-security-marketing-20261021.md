# Account security marketing and QR setup — 21 October 2026

## Change
- Public SaaS page: three security cards before pricing, using existing theme/card styles. Five-language title and body copy; no real user photos, session metadata or provisioning secrets.
- Company MFA dialog: explain that current-password verification reveals the provisioning QR and manual key. Full-width styled password field. Hide setup hint on already enabled accounts. Localized hint.
- QR provisioning behavior already existed; this change clarifies its discovery, without bypassing verification or re-enrolling a real user.

## Evidence
- Docker Chromium public-site-pricing.mjs with current HTML and current test mounted: PASS. Three marketing cards, five-language switching, phone viewport overflow assertions included.
- Docker Chromium command-palette.mjs with current company HTML and test mounted: PASS. Explicit pre-verification QR-hidden assertion, setup hint, existing locally generated QR comparison, fallback, secret cleanup, enrollment/recovery and session flows.
- Verify-PublicDemoLayout.mjs and git diff --check: PASS.
- Local preview screenshot: primary checkout tests/reports/security-marketing-preview-20261021.png.

## Limits
Synthetic browser/API acceptance does not prove real-mail delivery or all customer workflows. Production remains on e4a8d28 until current PR checks and controlled release complete. SMTP configuration and Santa/Farma account matching remain open.
