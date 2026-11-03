#!/bin/sh
# Apply candidate migrations to a disposable clone of an explicitly restored
# baseline. Keep the restored clone untouched as the rollback source.
set -eu
umask 077

taskClone=${1:?restored clone database required}
taskSql=${2:?candidate SQL file required}
expectedBaselineCount=${3:?expected baseline migration count required}
expectedBaselineLast=${4:?expected latest baseline migration ID required}
expectedTargetCount=${5:?expected target migration count required}
expectedTargetLast=${6:?expected latest target migration ID required}

case "$taskClone" in envantertr_restorecheck_[0-9]*_[0-9]*) ;; *) echo 'Refusing non-rehearsal database name.' >&2; exit 2;; esac
case "$taskClone" in *[!a-z0-9_]*) exit 2;; esac
test "$taskClone" != "${POSTGRES_DB:-envantertr}"
case "$expectedBaselineCount:$expectedTargetCount" in *[!0-9:]*) exit 2;; esac
case "$expectedBaselineLast:$expectedTargetLast" in *[!A-Za-z0-9_:]*) exit 2;; esac
case "$taskSql" in /var/lib/postgresql/data/envantertr-backups/candidate-*.sql) ;; *) echo 'Candidate SQL path is not allowed.' >&2; exit 2;; esac
test -f "$taskSql"

taskUser=${POSTGRES_USER:-envantertr}
taskStamp="$(date -u +%Y%m%dT%H%M%SZ)_$$"
taskCandidate="etrmig_${taskStamp}"
taskRollback="etrback_${taskStamp}"
taskDir="${PGDATA:-/var/lib/postgresql/data}/envantertr-backups/rehearsal-${taskStamp}"
mkdir "$taskDir"

cleanup() {
    dropdb -U "$taskUser" --if-exists "$taskCandidate" >/dev/null 2>&1 || true
    dropdb -U "$taskUser" --if-exists "$taskRollback" >/dev/null 2>&1 || true
}
trap cleanup EXIT HUP INT TERM

taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
taskLast=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -c 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')
test "$taskCount" = "$expectedBaselineCount"
test "$taskLast" = "$expectedBaselineLast"

psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" > "$taskDir/count-existing-tables.sql" <<'SQL'
SELECT format('SELECT %L || ''|'' || count(*) FROM %I.%I;', tablename, schemaname, tablename)
FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'
ORDER BY tablename;
SQL
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskClone" -f "$taskDir/count-existing-tables.sql" > "$taskDir/baseline.counts"

# Migration runs only on this disposable copy; taskClone remains the recovery point.
createdb -U "$taskUser" --template="$taskClone" --owner="$taskUser" "$taskCandidate"
psql -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -f "$taskSql" > "$taskDir/first-apply.log" 2>&1
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
taskLast=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -c 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')
test "$taskCount" = "$expectedTargetCount"
test "$taskLast" = "$expectedTargetLast"
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -f "$taskDir/count-existing-tables.sql" > "$taskDir/after.counts"
cmp "$taskDir/baseline.counts" "$taskDir/after.counts"

# Reapplying the idempotent script must not change schema history or existing data.
psql -X -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -f "$taskSql" > "$taskDir/second-apply.log" 2>&1
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
taskLast=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -c 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')
test "$taskCount" = "$expectedTargetCount"
test "$taskLast" = "$expectedTargetLast"
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskCandidate" -f "$taskDir/count-existing-tables.sql" > "$taskDir/repeated.counts"
cmp "$taskDir/after.counts" "$taskDir/repeated.counts"

# Rollback proof: discard the migrated copy and recreate from the untouched baseline.
dropdb -U "$taskUser" "$taskCandidate"
createdb -U "$taskUser" --template="$taskClone" --owner="$taskUser" "$taskRollback"
taskCount=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskRollback" -c 'SELECT count(*) FROM "__EFMigrationsHistory"')
taskLast=$(psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskRollback" -c 'SELECT max("MigrationId") FROM "__EFMigrationsHistory"')
test "$taskCount" = "$expectedBaselineCount"
test "$taskLast" = "$expectedBaselineLast"
psql -X -At -v ON_ERROR_STOP=1 -U "$taskUser" -d "$taskRollback" -f "$taskDir/count-existing-tables.sql" > "$taskDir/rollback.counts"
cmp "$taskDir/baseline.counts" "$taskDir/rollback.counts"

printf 'CLONE_MIGRATION_AND_ROLLBACK_REHEARSAL_OK\nBaseline: %s (%s, %s)\nCandidate: %s (%s, %s)\nExisting table row counts preserved; repeated apply is idempotent; rollback clone matches baseline.\nEvidence: %s\n' \
    "$taskClone" "$expectedBaselineCount" "$expectedBaselineLast" "$taskCandidate" "$expectedTargetCount" "$expectedTargetLast" "$taskDir"
