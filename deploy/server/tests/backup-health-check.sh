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
(cd -- "$BACKUP_ROOT" && sha256sum -- "$BACKUP_NAME") > "$BACKUP_PATH.sha256"

printf 'BACKUP_SFTP_TARGET=backup@example.invalid\nBACKUP_SFTP_DIRECTORY=/envantertr\nBACKUP_SFTP_KEY_FILE=%s/key\nBACKUP_SFTP_KNOWN_HOSTS_FILE=%s/known_hosts\n' \
  "$WORK_DIR" "$WORK_DIR" >> "$CONFIG_FILE"
printf 'test private key\n' > "$WORK_DIR/key"
printf 'backup.example.invalid ssh-ed25519 AAAATEST\n' > "$WORK_DIR/known_hosts"
chmod 0600 "$WORK_DIR/key" "$WORK_DIR/known_hosts"
mkdir -m 0700 "$WORK_DIR/bin"
printf '%s\n' \
  '#!/usr/bin/env bash' \
  'set -Eeuo pipefail' \
  'batch=""' \
  'while (($#)); do if [[ "$1" == "-b" ]]; then shift; batch="$1"; break; fi; shift; done' \
  '[[ -n "$batch" && -f "$batch" ]] || exit 2' \
  'grep -q "^ls -1 envantertr-server-\\*" "$batch" || exit 3' \
  '[[ "${ENVT_TEST_SFTP_FAIL:-0}" != "1" ]] || exit 4' \
  'local_dir="$(awk '\''$1 == "lcd" {gsub(/"/, "", $2); print $2}'\'' "$batch")"' \
  'remote_name="$(awk '\''$1 == "get" {gsub(/"/, "", $3); print $3}'\'' "$batch")"' \
  '[[ -n "$local_dir" && -n "$remote_name" && -f "${ENVT_TEST_REMOTE_SHA256:-}" ]] || exit 5' \
  'cp -- "$ENVT_TEST_REMOTE_SHA256" "$local_dir/$remote_name"' \
  'printf "%s\\n" "${ENVT_TEST_SFTP_LISTING:-}"' > "$WORK_DIR/bin/sftp"
chmod 0700 "$WORK_DIR/bin/sftp"
export PATH="$WORK_DIR/bin:$PATH"
export ENVT_TEST_SFTP_LISTING="sftp> cd /envantertr
sftp> ls -1 envantertr-server-*
$BACKUP_NAME
$BACKUP_NAME.sha256"
export ENVT_TEST_REMOTE_SHA256="$BACKUP_PATH.sha256"
run_check || { cat "$WORK_DIR/output.log" >&2; exit 1; }
grep -q 'remote=latest-copy-present' "$WORK_DIR/output.log"
echo 'PASS: latest remote backup and checksum are reachable'

export ENVT_TEST_SFTP_LISTING="$BACKUP_NAME.sha256"
expect_failure 'remote checksum without its archive is rejected' run_check
export ENVT_TEST_SFTP_LISTING="$BACKUP_NAME"
expect_failure 'remote archive without its checksum is rejected' run_check
export ENVT_TEST_SFTP_LISTING="$BACKUP_NAME $BACKUP_NAME.sha256"
printf '0%.0s' {1..64} > "$WORK_DIR/remote-mismatch.sha256"
export ENVT_TEST_REMOTE_SHA256="$WORK_DIR/remote-mismatch.sha256"
expect_failure 'remote checksum mismatch is rejected' run_check
export ENVT_TEST_REMOTE_SHA256="$BACKUP_PATH.sha256"
export ENVT_TEST_SFTP_FAIL=1
expect_failure 'unavailable remote SFTP target is rejected' run_check

echo 'Linux backup health acceptance passed.'
