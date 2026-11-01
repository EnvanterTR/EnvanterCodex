# Live visual and MFA follow-up — 2026-10-25

Production candidate: `6198ef6bc424ac44e9e74c9ab39357b2d6c162b9`.

## Actual live browser observations

- The label gallery advanced automatically to the left: horizontal transforms 0, -390 and -1117.94 pixels in three timed observations. The last observation captured a transition in progress.
- The gallery frame remained 505.890625 × 503.0625 pixels. Heading, paragraph and benefit copy remained identical.
- Original roll and all three supplied device photographs loaded; images use `object-fit: contain`. Native dimensions are recorded in `live-gallery-manifest-20261025.json`.
- The company portal preview opened in a 1280 × 720 dialog matching the viewport. Expanded source remained the original 1536 × 1024 PNG URL.
- Escape closed the dialog and restored opener focus; Enter reopened it, and the close button closed it again.
- Evidence: `live-gallery-timed-20261025.json`, `live-gallery-timed-20261025.png`, `live-portal-fullscreen-20261025.json`, `live-portal-fullscreen-20261025.png`.

These observations cover the live desktop browser, not mobile or every browser. The portal image is a marketing preview, not evidence of an authenticated customer operation.

## Repeated MFA prompts

The deployed root platform password-plus-MFA login grants the existing `platform_reauth_at` proof. Critical operations reuse that proof for 600 seconds. Expiration still requires fresh verification; MFA was not disabled, its validity was not extended, and no shared recovery code was used. This follow-up required no personal OTP. Runtime code and prior Identity/PostgreSQL acceptance support this change; no new personal login was claimed here.

## Remaining P0 gates

Read-only live GET `/api/trials/status` returned HTTP 200 with `available:false`, `trialDays:15`, `billingPeriod:annual`, and `noticeVersion:cloud-trial-data-v1`. Public trial registration therefore remains closed; this is not successful email delivery. Actual SMTP recipient delivery, provider payment acceptance, and complete Section 20 customer and cross-tenant acceptance remain open. No trial request or paid transaction was submitted during this check.
