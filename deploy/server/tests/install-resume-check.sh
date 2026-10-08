#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_SCRIPT="$SCRIPT_DIR/../install.sh"
WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/envantertr-install-resume.XXXXXXXX")"
trap 'rm -rf -- "$WORK_DIR"' EXIT
FIXTURE_ROOT="$WORK_DIR/source"
FIXTURE_SCRIPT="$FIXTURE_ROOT/deploy/server/install.sh"
MOCK_BIN="$WORK_DIR/mock-bin"
mkdir -p "$FIXTURE_ROOT/deploy/server" "$MOCK_BIN" "$WORK_DIR/run/lock" "$WORK_DIR/etc/systemd/system"
cp -- "$INSTALL_SCRIPT" "$FIXTURE_SCRIPT"
cp -- "$SCRIPT_DIR/../compose.yaml" "$FIXTURE_ROOT/deploy/server/compose.yaml"
cp -- "$SCRIPT_DIR/../envantertr-backup.service" "$SCRIPT_DIR/../envantertr-backup.timer" \
  "$SCRIPT_DIR/../envantertr-backup-health.service" "$SCRIPT_DIR/../envantertr-backup-health.timer" \
  "$SCRIPT_DIR/../envantertr-backup-alert@.service" "$FIXTURE_ROOT/deploy/server/"
touch "$FIXTURE_ROOT/Dockerfile"
sed -i \
  -e "s|/opt/envantertr|$WORK_DIR/opt/envantertr|g" \
  -e "s|/etc/envantertr|$WORK_DIR/etc/envantertr|g" \
  -e "s|/var/backups/envantertr|$WORK_DIR/var/backups/envantertr|g" \
  -e "s|/run/lock/envantertr-operations.lock|$WORK_DIR/run/lock/envantertr-operations.lock|g" \
  -e "s|/etc/systemd/system|$WORK_DIR/etc/systemd/system|g" \
  "$FIXTURE_SCRIPT"
cat > "$MOCK_BIN/docker" <<'SH'
#!/usr/bin/env bash
set -Eeuo pipefail
if [[ "$1" == compose && "$2" == version ]]; then exit 0; fi
if [[ "$*" == *" ps -q database"* ]]; then printf 'fixture-database\n'; exit 0; fi
if [[ "$1" == inspect ]]; then printf 'healthy\n'; exit 0; fi
exit 0
SH
cat > "$MOCK_BIN/curl" <<'SH'
#!/usr/bin/env bash
set -Eeuo pipefail
if [[ "${MOCK_CURL_MODE:-fail}" == succeed ]]; then printf '{"status":"ready"}\n'; exit 0; fi
exit 22
SH
cat > "$MOCK_BIN/systemctl" <<'SH'
#!/usr/bin/env bash
exit 0
SH
cat > "$MOCK_BIN/seq" <<'SH'
#!/usr/bin/env bash
printf '1\n'
SH
cat > "$MOCK_BIN/sleep" <<'SH'
#!/usr/bin/env bash
exit 0
SH
chmod 0755 "$MOCK_BIN"/*
DOMAIN="resume-test.example.invalid"
if PATH="$MOCK_BIN:$PATH" MOCK_CURL_MODE=fail bash "$FIXTURE_SCRIPT" "$DOMAIN" > "$WORK_DIR/first-install.log" 2>&1; then
  echo 'FAIL: simulated first-install health failure unexpectedly succeeded.' >&2
  exit 1
fi
CONFIG_FILE="$WORK_DIR/etc/envantertr/server.env"
BACKUP_PASSWORD_FILE="$WORK_DIR/etc/envantertr/backup.pass"
[[ -f "$CONFIG_FILE" && -f "$BACKUP_PASSWORD_FILE" && ! -e "$WORK_DIR/opt/envantertr/current" ]] || {
  cat "$WORK_DIR/first-install.log" >&2
  echo 'FAIL: failed install must retain protected config and secrets without exposing current.' >&2; exit 1;
}
cp -- "$CONFIG_FILE" "$WORK_DIR/server.env.before"
cp -- "$BACKUP_PASSWORD_FILE" "$WORK_DIR/backup.pass.before"
if PATH="$MOCK_BIN:$PATH" MOCK_CURL_MODE=succeed bash "$FIXTURE_SCRIPT" "other.$DOMAIN" > "$WORK_DIR/mismatched-domain.log" 2>&1; then
  echo 'FAIL: resume accepted a different DNS name.' >&2
  exit 1
fi
cmp -s "$CONFIG_FILE" "$WORK_DIR/server.env.before" || { echo 'FAIL: mismatched resume changed secrets.' >&2; exit 1; }
PATH="$MOCK_BIN:$PATH" MOCK_CURL_MODE=succeed bash "$FIXTURE_SCRIPT" "$DOMAIN" > "$WORK_DIR/resume.log" 2>&1 || {
  cat "$WORK_DIR/resume.log" >&2; exit 1;
}
grep -q 'Yarım ilk kurulum bulundu' "$WORK_DIR/resume.log"
grep -q 'sunucu kurulumu hazır' "$WORK_DIR/resume.log"
cmp -s "$CONFIG_FILE" "$WORK_DIR/server.env.before" || { echo 'FAIL: resume rotated existing credentials.' >&2; exit 1; }
cmp -s "$BACKUP_PASSWORD_FILE" "$WORK_DIR/backup.pass.before" || { echo 'FAIL: resume rotated the backup passphrase.' >&2; exit 1; }
[[ "$(readlink -f "$WORK_DIR/opt/envantertr/current")" == "$WORK_DIR/opt/envantertr/releases/$(awk -F= '$1=="RELEASE_TAG" {sub("initial-", "", $2); print $2}' "$CONFIG_FILE")" ]] || {
  echo 'FAIL: resume changed the original release target.' >&2; exit 1;
}
echo 'PASS: failed first install safely resumes with the same config, secrets, release, and volumes.'

python3 - "$INSTALL_SCRIPT" <<'PY'
import sys
from pathlib import Path

source = Path(sys.argv[1]).read_text(encoding="utf-8")

def require(name: str, *parts: str) -> None:
    positions = [source.find(part) for part in parts]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        raise SystemExit(f"FAIL: {name}")
    print(f"PASS: {name}")

require(
    "first install and maintenance operations are mutually exclusive",
    'exec 9>/run/lock/envantertr-operations.lock',
    'flock -n 9',
    'if [[ -e "$CONFIG_FILE" ]]; then',
)
require(
    "resume accepts only a root-owned private first-install config and its original release",
    '"$(stat -c \'%u:%a\' "$CONFIG_FILE")" == \'0:600\'',
  'SAVED_RELEASE_TAG="$(read_config RELEASE_TAG)"',
  '[[ "$SAVED_RELEASE_TAG" =~ ^initial-[0-9]{8}-[0-9]{6}$ ]]',
  'RELEASE_DIR="$INSTALL_ROOT/releases/$STAMP"',
  '[[ -d "$RELEASE_DIR" && -f "$RELEASE_DIR/deploy/server/compose.yaml" ]]',
  '[[ "$(readlink -f "$INSTALL_ROOT/current")" == "$RELEASE_DIR" ]]',
)
require(
    "resume requires the original root-owned backup secret and reuses existing credentials",
    '"$(stat -c \'%u:%a\' "$BACKUP_PASSWORD_FILE")" == \'0:600\'',
    'printf \'Yarım ilk kurulum bulundu;',
    'else\n  [[ ! -e "$INSTALL_ROOT/current"',
    'POSTGRES_PASSWORD="$(openssl rand -hex 32)"',
)
require(
    "current release is exposed only after HTTPS readiness",
    '[[ "$ready" == true ]]',
    'ln -s "$RELEASE_DIR" "$INSTALL_ROOT/current"',
)

if any(fragment in source for fragment in ("docker volume rm", "docker volume prune", "down --volumes")):
    raise SystemExit("FAIL: installer must not remove database or media volumes when resuming")
print("PASS: installer never deletes existing Docker volumes")
PY
