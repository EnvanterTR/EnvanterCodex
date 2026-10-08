#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
UPDATE_SCRIPT="$SCRIPT_DIR/../update.sh"
python3 - "$UPDATE_SCRIPT" <<'PY'
import sys
from pathlib import Path

source = Path(sys.argv[1]).read_text(encoding="utf-8")

def require(name: str, *parts: str) -> None:
    positions = [source.find(part) for part in parts]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        raise SystemExit(f"FAIL: {name}")
    print(f"PASS: {name}")

require(
    "maintenance lock is acquired before release preparation",
    'flock -n 9',
    'install -d -m 0750 "$INSTALL_ROOT/releases"',
)
require(
    "old web service stops before backup and verified backup precedes build and migration",
    '"${compose_old[@]}" stop web',
    'bash "$CURRENT_RELEASE/deploy/server/backup.sh"',
    '"${compose_new[@]}" build web',
    '"${compose_new[@]}" run --rm web --migrate',
)
require(
    "backup failure before migration restarts the old application",
    'if [[ "$status" -ne 0 && "$WEB_STOPPED" -eq 1 && "$MIGRATION_STARTED" -eq 0 ]]',
    '"${compose_old[@]}" up -d web proxy',
)
require(
    "failed migration does not advance current release pointer",
    '"${compose_new[@]}" run --rm web --migrate',
    'if [[ "$ready" != true ]]',
    'mv -Tf -- "$NEXT_LINK" "$CURRENT_RELEASE"',
)
require(
    "current release switches only after ready health check",
    'if [[ "$ready" != true ]]',
    "fail 'Güncellenen uygulama ready olmadı.",
    'mv -Tf -- "$NEXT_LINK" "$CURRENT_RELEASE"',
)
if any(fragment in source for fragment in ("docker volume rm", "docker volume prune", "down --volumes")):
    raise SystemExit("FAIL: update must preserve database, media, and key volumes")
print("PASS: update never removes persistent data volumes")
PY
