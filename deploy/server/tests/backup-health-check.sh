#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
CHECK_SCRIPT="$SCRIPT_DIR/../check-backup-health.sh"
WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/envantertr-backup-health-test.XXXXXXXX")"
trap 'rm -rf -- "$WORK_DIR"' EXIT

BACKUP_ROOT="$WORK_DIR/backups"
CONFIG_FILE="$WORK_DIR/server.env"
mkdir -m 0700 "$BACKUP_ROOT"
printf 'BACKUP_ROOT=%s\n' "$BACKUP_ROOT" > "$CONFIG_FILE"
BACKUP_NAME="envantertr-server-20261008-120000-a1b2c3d4.etbackup"
BACKUP_PATH="$BACKUP_ROOT/$BACKUP_NAME"
printf 'synthetic encrypted backup fixture\n' > "$BACKUP_PATH"
(cd -- "$BACKUP_ROOT" && sha256sum -- "$BACKUP_NAME") > "$BACKUP_PATH.sha256"

run_check() {
  local max_age="${1:-36}"
  ENVT_SERVER_ENV_FILE="$CONFIG_FILE" ENVT_BACKUP_MAX_AGE_HOURS="$max_age" \
    bash "$CHECK_SCRIPT" > "$WORK_DIR/output.log" 2>&1
}

expect_failure() {
  local label="$1"
  shift
  if "$@"; then
    cat "$WORK_DIR/output.log" >&2
    echo "FAIL: $label unexpectedly passed." >&2
    exit 1
  fi
  printf 'PASS: %s\n' "$label"
}

[[ "$EUID" -eq 0 ]] || { echo 'Run this check as root (for example: sudo bash deploy/server/tests/backup-health-check.sh).' >&2; exit 1; }

run_check || { cat "$WORK_DIR/output.log" >&2; exit 1; }
grep -q '^PASS: .*sha256=verified' "$WORK_DIR/output.log"
echo 'PASS: current backup with a matching SHA-256 sidecar'

touch -d '48 hours ago' "$BACKUP_PATH"
expect_failure 'stale backup is rejected' run_check
touch "$BACKUP_PATH"

printf 'tampered' >> "$BACKUP_PATH"
expect_failure 'modified backup is rejected' run_check

printf 'synthetic encrypted backup fixture\n' > "$BACKUP_PATH"
(cd -- "$BACKUP_ROOT" && sha256sum -- "$BACKUP_NAME") > "$BACKUP_PATH.sha256"
expect_failure 'invalid maximum-age setting is rejected' run_check 0

rm -- "$BACKUP_PATH.sha256"
expect_failure 'missing SHA-256 sidecar is rejected' run_check

echo 'Linux backup health acceptance passed.'
