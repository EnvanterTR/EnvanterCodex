# Account photo and archive namespace acceptance — 29 October2026

## Result

Seven focused PostgreSQL tests passed in1.6026minutes (exit0). Temporary database/network cleanup completed. This is a local follow-up, not yet deployed.

## Change

The existing media component-walk guard now exposes a separate account scope, `accounts/UserId`, while retaining the original CompanyId scope. Account-photo upload/delete validates all supported profile filenames before moving files; reads and the session photo flag reject redirected storage. Company ZIP export also applies the account path guard before including a member's profile photo.

The existing account-based image path and shared identity across memberships are retained. No real profile image was replaced or removed.

## Coverage

Three attack cases cover the accounts directory, the selected user's directory and the final profile file. They prove read404, replacement409, deletion404, false photo-presence flags, no aliased profile in company archives, unchanged foreign bytes and unchanged account-photo audit events. Removing the link restores normal photo reads and normal archive inclusion.

Existing maximum-size account photo behavior, full company JSON/ZIP export and both asset attachment directory/file-link cases also passed. The shared guard change therefore has company and account regression evidence.

Evidence: `account-photo-media-namespace-20261029.log`; test: `AccountPhotoMediaNamespaceAcceptanceTests.cs`.

This does not close Hedef6.4/P1 or the all-table audit-field requirement. Deployment and the remaining lifecycle/restore/customer evidence remain separate.
