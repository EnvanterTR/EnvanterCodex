# `DeletedAt` coverage migration 126 — 31 Oct 2026

- Migration `20261031000100_AddDeletedAtToAllTables` adds nullable PostgreSQL `timestamp with time zone` columns to the 92 mapped tables that lacked `DeletedAt` in the 95-table schema audit. The existing `Companies`, `Assets`, and `EmployeeHandoverEmailAttempts` columns are retained.
- Existing rows receive no fabricated deletion timestamps; there is no default, backfill, row deletion, or other data rewrite. The migration does not change soft-delete behavior. Shared Identity, subscription, and platform records remain shared under the previously accepted tenant model; `CompanyId` is not added to those shared records.
- The finalized-model regression asserts every mapped relational table has a nullable `DeletedAt`. The isolated PostgreSQL acceptance applies the full migration chain, compares all physical `DeletedAt` table names against the EF model, verifies preexisting row/column projections remain byte-equivalent, rolls migration 126 back to migration 125 and confirms only the original three columns remain, then reapplies migration 126 and repeats the preservation check.
- Catalog-FK legacy-corruption tests were updated to seed pre-126 schemas through schema-qualified raw SQL, preserving their actual historical-migration test while current EF mappings include `DeletedAt`.
- Command: `docker compose --project-name p1-deletedat-model --file deploy/testing/compose.security-acceptance.yaml run --rm acceptance --filter 'FullyQualifiedName~Every_persisted_application_table_has_a_nullable_deletion_timestamp|FullyQualifiedName~Finalized_application_model_preserves_shared_accounts_and_has_no_schema_drift|FullyQualifiedName~Current_release_preserves_all_original_fields_from_118_and_repeats_idempotently|FullyQualifiedName~Catalog_constraints_reject_legacy_corruption_atomically_and_protect_new_links'`.
- Result: 6/6 passed on the isolated PostgreSQL 17 Compose database; web and acceptance projects built with 0 errors. Model coverage, migration preservation/Down/re-upgrade, and all three legacy FK corruption kinds passed.
- Release image backup/restore runner was also rerun locally from commit `2069f9f`: Release smoke accepted exactly 126 migrations; the synthetic PostgreSQL dump restored into a separate PostgreSQL 17 container with exact row counts matching across 96 tables; media and Data Protection key archives passed traversal and byte checks. This is synthetic local evidence only.
- Commit `2069f9fbcfb5ea6ef29b4d2850bd9f2e60a096fd` is pushed to `feat/verified-cloud-trials-20261021` and is the current head of draft PR #25. GitHub run `41214984204` completed: Windows deployment/backup safety, Linux PostgreSQL, and all four tenant PostgreSQL shards passed; Linux tenant security failed only at “Back up and restore the isolated Release database, media, and keys” with generic exit code 1. The connected CI diagnostic endpoint was unavailable and GitHub's unauthenticated API exposes only that generic annotation, so the CI host root cause is unknown. No other job failed.
- Scope: local schema/migration and synthetic backup acceptance are green; remote CI is not fully green. Neither result establishes production schema state or authorizes deployment. Production preflight, current live backup/restore rehearsal, staging, and green remote CI remain required. Historical `CreatedAt` values remain unknown where source records do not contain trustworthy dates.

### İş paketi: Hedef 6.1 — eksik `DeletedAt` şema alanları — TAMAM

Değişen dosyalar:
- `src/EnvanterTR.Web/Data/AppDbContext.cs`
- `src/EnvanterTR.Web/Data/Migrations/20261031000100_AddDeletedAtToAllTables.cs`
- `src/EnvanterTR.Web/Data/Migrations/20261031000100_AddDeletedAtToAllTables.Designer.cs`
- `src/EnvanterTR.Web/Data/Migrations/AppDbContextModelSnapshot.cs`
- `tests/EnvanterTR.SecurityAcceptance/FinalizedTenantModelScopeTests.cs`
- `tests/EnvanterTR.SecurityAcceptance/CurrentReleaseMigrationAcceptanceTests.cs`
- `tests/EnvanterTR.SecurityAcceptance/CatalogForeignKeyAcceptanceTests.cs`

Komut ve çıktı:
- İzole Docker Compose/PostgreSQL 17 kabul komutu yukarıda; exit 0.
- Web + kabul testi derlemesi başarılı; web 0 uyarı/0 hata, kabul projesinde mevcut 10 uyarı/0 hata.

Test:
- Model, migration preservation/rollback/re-upgrade ve catalog FK senaryoları: 6/6 geçti.

Kanıt:
- `tests/reports/deletedat-126-acceptance-20261031.md`

Risk ve kapsam:
- Üretim, staging, canlı yedek/geri yükleme veya canlı müşteri kabulü yapılmadı. Bu nedenle yayın için kanıt değildir.
- Geçmiş kayıtlarda kaynağı olmayan tarihler `NULL` kalır; kullanıcının belirttiği geçmiş tarih listesi olmadan değer uydurulmayacaktır.
- `CompanyId` tenant iş verilerinde zorunlu tutulur; ortak kullanıcı/plan/platform tabloları ortak kalır.

Sıradaki:
- Aynı Release backup/restore CI adımını yeniden doğrulatmak; eğer yine başarısız olursa log erişimiyle runner-kök nedenini ayırmak. Canlı/staging'e taşıma yok.

TAMAM dediğim 1 izole şema paketinin 1'i test çıktısıyla doğrulanmıştır.
