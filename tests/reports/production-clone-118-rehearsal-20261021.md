# Production-backup clone migration rehearsal — 21 October 2026

- Candidate source: merged main `4a70480d64af903f2818bb040babb63a25a29c6b`.
- SQL artifact: `tests/reports/coupon-migration-117-to-118-20261021.sql`.
- Git blob / downloaded SQL SHA-256: `714b7e9bc8bf36908ceac37df605b590c6d4bf5c161a7180e40510338bb353e8`.
- Source backup: `daily-20261020T210608Z.dump`; checksum verification returned OK.
- Restored database: `envantertr_restorecheck_20261021_0155`, separate from the production database.
- Used existing `rehearse-preserved-data.sh`, with expected migration counts changed from 92/117 to 117/118 and candidate hash updated. Original-column projections exclude migration history and preserve all pre-existing fields and rows.
- Result: `PRESERVED_CONTENT_REHEARSAL_OK`, `REHEARSAL_EXIT=0`.
- All 93 original tables retained identical original fields and rows after migration and after a second idempotent execution. Migration count assertion passed at 118 both times.
- Private row-level evidence remains only on the server at `/var/lib/postgresql/data/envantertr-backups/content-rehearsal-20261020T225335Z`.
- Screenshot: `production-clone-118-rehearsal-20261021.png`.
- The earlier long terminal paste was truncated before its closing quote and was safely terminated with output redirected to `/dev/null`. It did not start the rehearsal. The successful execution downloaded the committed scripts using short commands and checked the SQL against the Git blob hash. The checkout file hash differs because of working-tree line endings.
- Production migration and application deployment have not been performed in this rehearsal.

## CI follow-up
Main run 39510840573 exposed a timing race at platform-audit-retention.mjs:85: the visible status still said Loading. The test now waits for the disabled policy result; a deliberate 150ms synthetic policy delay makes this timing condition observable. Targeted Chromium run passed, including policy save, read-only preview, step-up MFA, CSRF and 390px layout. Product behavior is unchanged.
