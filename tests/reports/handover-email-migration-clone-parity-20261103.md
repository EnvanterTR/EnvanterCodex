# Handover-email migration parity on restored backup clone

Date: 2026-11-03

## Scope

The `20261026083058_AddHandoverEmailAttempts` model, migration, and target snapshot were added to the isolated merge-preview branch. The migration body preserves delivery-attempt evidence on downgrade by refusing a destructive `Down`; its deployed migration ID and SQL shape were taken from the historical feature commit. The public send/read endpoints and PDF/email implementation are a separate work package and are not claimed as complete here.

## Evidence

- EF migration IDs in the merge-preview source: 122.
- Migration IDs in the restored Coolify backup clone's `__EFMigrationsHistory`: 122.
- Set differences: `onlyBackup=0`, `onlySource=0`.
- `dotnet ef migrations has-pending-model-changes`: “No changes have been made to the model since the last migration.”
- Ran `dotnet ef database update 20261026083058_AddHandoverEmailAttempts` against the already restored, network-isolated clone. EF reported: “No migrations were applied. The database is already up to date.”
- Clone guardrails at the time of inspection: PostgreSQL 17, `NetworkMode=none`, no port bindings, no mounts.
- Separate acceptance PostgreSQL database: recoverable-company-deletion acceptance 4/4 PASS after the 122-migration model update.

## Effect

No migration SQL ran against the restored clone because the backup already records this migration as applied. No production database or live deployment was changed. No customer field values were read or copied into this report. The clone remained unchanged by the EF no-op check.

## Remaining

Implement the application endpoints/PDF/email flow and its send-idempotency acceptance, then check the integrated branch again before any release approval.
