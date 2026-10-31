# Remaining attachment provider namespaces — 2026-10-29

Hedef6.4/6.5: model, contract, accessory purchase-order and stock purchase-order uploads, downloads and deletions now validate the full company media path. Replaced four duplicate directory-only checks with CompanyMediaPathGuard; reject unsafe upload409 and read/delete404 before any metadata/audit deletion. Contract content SHA256/length validation and all existing authorization/limits remain intact.

Local PostgreSQL9/9 passed in4.0087minutes,exit0; temporary database/network removed. Eight cases cover each provider with foreign directory and final-file links, foreign sentinel preservation, no false deletion audit or missing metadata, normal own-file recovery and normal deletion. Existing contract company-scope/audit/attachment scenario also passed.

This is a follow-up to2997062, not part of its in-progress production deployment. No all-media/fullHedef completion is claimed; photos/catalog/logo and further remaining security/customer acceptance are separate.
