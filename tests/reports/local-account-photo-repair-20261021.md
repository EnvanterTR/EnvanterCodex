# Local account-photo storage repair — 2026-10-21

- User-reported localhost upload500 traced to root-owned legacy media volume; current runtime uses non-root UID1654.
- repair-media-permissions.ps1 validates a writable named volume at the exact media path and derives the running application identity.
- Changes ownership only on the media root and existing accounts directory. No recursive file changes, broad permissions, database operations or upload deletion.
- PowerShell parser PASS. Executed against env antertr local web container (actual name supplied by script default): ACCOUNT_PHOTO_STORAGE_WRITE_PASS, exit0. Existing upload data retained.
- Scope: local legacy-volume recovery. This does not configure SMTP or change production photo storage.
