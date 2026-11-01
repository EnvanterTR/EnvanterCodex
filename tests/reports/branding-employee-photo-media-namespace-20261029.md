# Company logo and employee photo protection — 29 October 2026

## Result

The six focused PostgreSQL tests passed in1.7630minutes (exit0). Temporary database and network cleanup completed. This change is not deployed yet.

Company logos and employee photos now validate their full company storage paths before reading, replacing or removing files. Unsafe paths fail before metadata or audit changes. Employee uploads/deletions validate all supported profile-photo extensions before moving any files.

## Existing logo failure corrected

The first run passed4/6 tests; both failures were logo uploads returning500. `[FromForm] IFormFile` had created implicit framework antiforgery metadata while the application validates API writes in its group endpoint filter.

Logo upload now reads the multipart form explicitly, matching other upload endpoints. The existing `IAntiforgery.ValidateRequestAsync` filter remains unchanged. Regression assertions prove multipart logo requests without CSRF return400 and valid requests succeed.

## Coverage and evidence

Four attack cases cover company logos and employee photos with foreign directory and final-file links. They verify404 reads,409 replacements,404 employee-photo deletion, unchanged foreign bytes/audit/metadata, restoration of the own-company image and normal replacement. Existing employee photo role/CSRF/size and active-company photo/document scenarios also passed.

- Passing evidence: `branding-employee-photo-media-namespace-20261029.log`.
- Preserved initial failure: `branding-employee-photo-before-form-fix-20261029.log`.
- Source: `OperationalEndpoints.cs`.
- Test: `BrandingEmployeePhotoMediaNamespaceAcceptanceTests.cs`.

The account-level profile photo was not modified. Full Hedef/P1 and the remaining media/customer evidence remain open.
