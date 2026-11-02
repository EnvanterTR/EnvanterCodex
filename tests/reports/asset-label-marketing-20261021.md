# Transparent label-roll website feature — 21 October 2026

User-supplied label-roll photograph was edited with the built-in image_gen tool, background-extraction mode, transparent_background=true. No CLI fallback.

Asset: src/EnvanterTR.PublicSite/assets/images/asset-label-roll-transparent.png
PNG: 1200x1311, RGBA color type6, 1947841 bytes. Source remains preserved.

Final prompt:
Edit this supplied product photograph for an enterprise IT asset management website. Remove only the white background and floor completely, producing true transparent alpha. Preserve the exact metallic silver asset-label roll, cardboard core, front hanging label, barcode, QR pattern and printed text as closely as possible. No added objects, no frame, no glow, no colored background, no new lettering. Clean antialiased edges, professional sharp product cutout, tight comfortable framing around the whole roll with small transparent margin. Keep natural material lighting and shape; do not crop any part of the roll. Intended for placement directly on a dark website.

Placement: after record/assignment feature narrative, before Cloud SaaS. Two columns on desktop and stacked on mobile. Theme background is inherited. Five-language copy and demo link. Image is an illustrative label sample; its printed QR is not advertised as an operational tenant link.

Evidence: current HTML/image/test mounts in Docker Chromium public-site-pricing.mjs PASS, including image load, RGBA format,390px bounds, locale switching and demo href. Verify-PublicDemoLayout and diff-check PASS. Screenshot in primary checkout tests/reports/asset-label-marketing-preview-20261021.png. Not deployed yet.
