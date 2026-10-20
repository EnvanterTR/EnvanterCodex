#!/bin/sh
# Run inside the existing PostgreSQL container. No password or data is logged.
set -eu
umask 077
taskBackupDir="${PGDATA:-/var/lib/postgresql/data}/envantertr-backups"
mkdir -p "$taskBackupDir"
chmod 700 "$taskBackupDir"
taskBackupFile="$taskBackupDir/daily-$(date -u +%Y%m%dT%H%M%SZ).dump"
test ! -e "$taskBackupFile"
pg_dump -U "${POSTGRES_USER:-envantertr}" -d "${POSTGRES_DB:-envantertr}" \
    --format=custom --file="$taskBackupFile.partial"
pg_restore --list "$taskBackupFile.partial" >/dev/null
mv "$taskBackupFile.partial" "$taskBackupFile"
sha256sum "$taskBackupFile" > "$taskBackupFile.sha256"
printf 'DAILY_SQL_BACKUP_OK\n'

# Append-only on this host. Off-host encrypted copies, retention and media/key
# backups are separate requirements; do not describe this SQL job as full DR.
