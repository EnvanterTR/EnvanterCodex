# EnvanterTR F1 canlı provası — Coolify > EnvanterTR > Terminal > "database" konteynerinde çalıştırın.
# Canlı veritabanına YAZMAZ: yalnız pg_dump alır, yedeği ayrı adlı geçici veritabanlarına açar,
# 5 migration'ı kopyada dener, satır sayılarını karşılaştırır ve kopyaları siler. Parola/veri ekrana yazılmaz.
set -eu; umask 077
U=${POSTGRES_USER:-envantertr}; D=${POSTGRES_DB:-envantertr}; T=$(date -u +%Y%m%d_%H%M%S)
DIR=${PGDATA:-/var/lib/postgresql/data}/envantertr-backups; mkdir -p "$DIR"
F="$DIR/f1-$T.dump"; R=envantertr_restorecheck_$T; C=etrmig_$T
pg_dump -U "$U" -d "$D" --format=custom --file="$F"; pg_restore --list "$F" >/dev/null; sha256sum "$F" | tee "$F.sha256"
counts() { psql -U "$U" -d "$1" -tA -c "select table_name from information_schema.tables where table_schema='public' and table_type='BASE TABLE' order by 1" | while read t; do echo "$t $(psql -U "$U" -d "$1" -tAc "select count(*) from \"$t\"")"; done; }
counts "$D" > /tmp/f1-live.txt
createdb -U "$U" "$R"; pg_restore -U "$U" -d "$R" --no-owner "$F"; counts "$R" > /tmp/f1-restore.txt
diff -q /tmp/f1-live.txt /tmp/f1-restore.txt && echo "GERI_YUKLEME_OK: $(wc -l < /tmp/f1-live.txt) tablo birebir"
echo "CANLI_MIGRATION: $(psql -U "$U" -d "$D" -tAc 'select count(*) from "__EFMigrationsHistory"') son: $(psql -U "$U" -d "$D" -tAc 'select max("MigrationId") from "__EFMigrationsHistory"')"
wget -qO /tmp/f1-candidate.sql "https://raw.githubusercontent.com/EnvanterTR/EnvanterCodex/codex/f2-cloud-seat-limits-20261104/deploy/coolify/candidate-122-127.sql"
echo "00c45d637f9b135dd6573c3a1fae62425b12600a4273289aa08dcefc509ce6ce  /tmp/f1-candidate.sql" | sha256sum -c - >/dev/null || { echo "ADAY_SQL_HASH_HATALI"; exit 1; }; echo "ADAY_SQL_SHA256_OK"
createdb -U "$U" -T "$R" "$C"; psql -U "$U" -d "$C" -v ON_ERROR_STOP=1 -q -f /tmp/f1-candidate.sql
echo "PROVA_MIGRATION: $(psql -U "$U" -d "$C" -tAc 'select count(*) from "__EFMigrationsHistory"')"
counts "$C" > /tmp/f1-after.txt; diff /tmp/f1-restore.txt /tmp/f1-after.txt | grep -v "__EFMigrationsHistory" | grep "^[<>]" && echo "SATIR_FARKI_VAR" || echo "SATIRLAR_KORUNDU"
for t in Companies Assets Employees AspNetUsers; do echo "$t: $(grep "^$t " /tmp/f1-live.txt | cut -d' ' -f2)"; done
dropdb -U "$U" "$C"; dropdb -U "$U" "$R"; rm -f /tmp/f1-*.txt /tmp/f1-candidate.sql
echo "F1_PROVA_BITTI yedek: $F"
