# Operational attachment namespaces — 2026-10-29

Hedef 6.4/6.5 follow-up: maintenance and asset purchase-order attachments use the shared company media path guard before upload directory creation, download file opening, or deletion metadata/audit changes. Directory and final-file redirects fail closed (upload 409, read/delete 404). Existing company/location authorization, content checks, limits and successful deletion behavior remain intact. Duplicate directory-only guard implementations were removed.

Local PostgreSQL acceptance: 5/5 passed in 1.9645 minutes, exit 0, temporary database/network cleanup completed. Four cases exercise both endpoint families with directory and final-file links, verify foreign sentinel bytes and file count, preserve attachment metadata and audit history, restore normal own-file downloads and verify normal deletion. Existing maintenance upload/audit/company-archive coverage also passed.

This follow-up is local only while CI40795019484 runs against 2997062. No deployment or all-media/full-goal completion is claimed. Other media providers and customer acceptance remain open.
