# Account security product preview — 21 October 2026

User requested homepage visuals based on their profile, MFA and session screenshots, explicitly retaining the portrait. The built-in image_gen tool produced a three-panel navy product illustration. The final edit replaced only the placeholder avatar with the supplied portrait reference. Original source screenshots and generated originals are preserved.

Asset: src/EnvanterTR.PublicSite/assets/images/account-security-product-preview.png
Placement: after account-security benefit cards and before pricing. Responsive width, intrinsic1536x1024, lazy loading. Heading/caption/supporting copy in five languages. Session rows are illustrative; no provisioning key, live MFA code or real IP is included. Portrait retained at user's explicit request.

Final edit prompt: Edit image1 only by replacing its top-left circular E avatar with the exact man's profile photograph from the circular avatar in image2. Preserve his identity, facial features, hairstyle, clothing and expression as closely as possible, crop naturally within the existing circle. Keep all other layout, navy colors, panels, Turkish labels, buttons, phone illustration and sample session rows unchanged. Keep the full rectangular product preview and its example label.

Current HTML/image/test mounted in Docker Chromium public-site-pricing.mjs: PASS. Image-load1536px and390px phone-bound checks included. Static inventory refreshed; PublicDemoLayout and diff-check PASS. Full application source still uses existing118schema; no new DBmigration.
