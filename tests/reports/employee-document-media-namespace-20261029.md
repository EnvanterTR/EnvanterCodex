# Employee document namespace acceptance — 2026-10-29

Employee document upload, download and deletion now validate the full company media path before filesystem access or metadata deletion. Both directory and leaf-file redirects to another company's storage fail closed. Normal document content validation and audit behavior remain covered.

Local PostgreSQL acceptance: 3/3 passed in 1.2205 minutes; temporary database and network removed. Evidence: employee-document-media-namespace-20261029.log. This change has not yet been deployed.

The preceding license candidate's Linux browser gate failed because reservation dates 2026-10-27/28 had fallen below the live browser date minimum. The browser fixture now derives future dates from that minimum and preserves invalid-range, request payload, CSRF and row-visibility assertions. This is a test-date correction; application reservation rules remain unchanged.

Browser verification: the unchanged fixture reproduced the same reservation timeout locally. The corrected full asset-detail-actions Chromium scenario completed successfully (exit 0), including reservation validation, payload, CSRF, decisions, cancellation, filters and all 52 sidebar destinations. Evidence: reservation-browser-fixed-20261029.log.
