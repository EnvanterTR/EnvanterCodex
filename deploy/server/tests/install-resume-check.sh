#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_SCRIPT="$SCRIPT_DIR/../install.sh"
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
