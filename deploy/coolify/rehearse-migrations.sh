#!/bin/sh
# Run inside PostgreSQL, against an explicitly named restored clone only.
# The application database is never a migration target of this script.
set -eu
umask 077
taskClone=${1:?restored clone database required}
taskSql=${2:?candidate SQL file required}
case "$taskClone" in envantertr_restorecheck_[0-9]*_[0-9]*) ;; *) exit 2;; esac
case "$taskClone" in *[!a-z0-9_]*) exit 2;; esac
test "$taskClone" != "${POSTGRES_DB:-envantertr}"
case "$taskSql" in /var/lib/postgresql/data/envantertr-backups/candidate-*.sql) ;; *) exit 2;; esac
test -f "$taskSql"
taskUser=${POSTGRES_USER:-envantertr}
taskDir="${PGDATA:-/var/lib/postgresql/data}/envantertr-backups/rehearsal-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir "$taskDir"
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
test "$taskCount" = 92
taskLast=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')
test "$taskLast" = 20261015082311_AddPlatformFollowUpEmailDigests
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" > "$taskDir/count-existing-tables.sql" <<'SQL'
SELECT format('SELECT %L || ''|'' || count(*) FROM %I.%I;', tablename, schemaname, tablename)
FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'
ORDER BY tablename;
SQL
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/count-existing-tables.sql" > "$taskDir/before.counts"
psql -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskSql" > "$taskDir/first-apply.log" 2>&1
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
test "$taskCount" = 117
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/count-existing-tables.sql" > "$taskDir/after.counts"
cmp "$taskDir/before.counts" "$taskDir/after.counts"
# A second execution must neither repeat schema changes nor add history rows.
psql -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskSql" > "$taskDir/second-apply.log" 2>&1
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
test "$taskCount" = 117
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/count-existing-tables.sql" > "$taskDir/repeated.counts"
cmp "$taskDir/after.counts" "$taskDir/repeated.counts"
printf 'CLONE_MIGRATION_REHEARSAL_OK\nClone: %s\nMigrations: %s\nExisting table row counts unchanged; second execution idempotent.\nEvidence: %s\n' "$taskClone" "$taskCount" "$taskDir"
