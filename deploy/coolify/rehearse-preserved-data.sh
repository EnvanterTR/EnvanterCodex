#!/bin/sh
# Operator preflight: a restored clone only; raw projections stay private.
set -eu
umask 077
taskClone=${1:?restored clone required}
taskSql=${2:?verified migration SQL required}
case "$taskClone" in envantertr_restorecheck_[0-9]*_[0-9]*) ;; *) exit 2;; esac
case "$taskClone" in *[!a-z0-9_]*) exit 2;; esac
test "$taskClone" != "${POSTGRES_DB:-envantertr}"
case "$taskSql" in /var/lib/postgresql/data/envantertr-backups/candidate-*.sql) ;; *) exit 2;; esac
test "$(sha256sum "$taskSql" | cut -d ' ' -f 1)" = d9257fd7a652f66f2309b8c755f4d1b3277ee0c5a72095c7628e261e3aba448b
taskUser=${POSTGRES_USER:-envantertr}
taskDir="${PGDATA:-/var/lib/postgresql/data}/envantertr-backups/content-rehearsal-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -m 700 "$taskDir"
test "$(psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')" = 92

# Freeze the original column list. Added columns are intentionally not in the
# byte comparison; every original field and row is compared, not only counts.
psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" > "$taskDir/original-projections.sql" <<'SQL'
SELECT format(
  'SELECT %L; COPY (SELECT row_to_json(t)::text FROM (SELECT %s FROM %I.%I) AS t ORDER BY row_to_json(t)::text COLLATE "C") TO STDOUT;',
  'TABLE|' || c.relname,
  string_agg(format('%I', a.attname), ', ' ORDER BY a.attnum),
  n.nspname, c.relname)
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p') AND c.relname <> '__EFMigrationsHistory'
GROUP BY n.nspname, c.relname ORDER BY c.relname COLLATE "C";
SQL
psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/original-projections.sql" > "$taskDir/before.rows"
psql -q -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskSql" > "$taskDir/first-apply.private.log" 2>&1
test "$(psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')" = 117
psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/original-projections.sql" > "$taskDir/after.rows"
cmp "$taskDir/before.rows" "$taskDir/after.rows"
psql -q -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskSql" > "$taskDir/repeat-apply.private.log" 2>&1
psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/original-projections.sql" > "$taskDir/repeated.rows"
cmp "$taskDir/before.rows" "$taskDir/repeated.rows"
test "$(psql -q -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')" = 117
sha256sum "$taskDir/before.rows" "$taskDir/after.rows" "$taskDir/repeated.rows" > "$taskDir/projection-hashes.private.txt"
printf 'PRESERVED_CONTENT_REHEARSAL_OK\nClone: %s\nOriginal tables: %s\nOriginal rows and fields identical after 92 -> 117 and idempotent repeat.\nPrivate evidence: %s\n' "$taskClone" "$(wc -l < "$taskDir/original-projections.sql" | tr -d ' ')" "$taskDir"
