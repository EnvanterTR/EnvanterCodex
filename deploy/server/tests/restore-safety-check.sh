#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RESTORE_SCRIPT="$SCRIPT_DIR/../restore.sh"
python3 - "$RESTORE_SCRIPT" <<'PY'
import sys
from pathlib import Path

source = Path(sys.argv[1]).read_text(encoding="utf-8")

def require(name: str, *parts: str) -> None:
    positions = [source.find(part) for part in parts]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        raise SystemExit(f"FAIL: {name}")
    print(f"PASS: {name}")

require(
    "authenticated backup is decrypted and isolated restore preview completes before live services stop",
    "-- decrypt /run/secrets/input.etbackup",
    'bash "$CURRENT_RELEASE/deploy/local/restore-preview.sh" "$WORK_DIR/restored"',
    "WEB_STOPPED=1",
)
require(
    "verified live safety backup is required before replacement volumes are created",
    'SAFETY_BACKUP="$(printf',
    '[[ -n "$SAFETY_BACKUP" && -f "$SAFETY_BACKUP" ]]',
    'docker volume create "$NEW_DB_VOLUME"',
)
require(
    "database restore and SQL verification use the new staging database before config switch",
    'docker exec "$STAGE_DB" pg_restore',
    'docker exec "$STAGE_DB" psql -X -A -t -U envantertr -d envantertr -c \'SELECT 1\'',
    "CONFIG_SWITCHED=1",
)
require(
    "new config is switched only after the isolated restore and safety backup",
    'bash "$CURRENT_RELEASE/deploy/local/restore-preview.sh" "$WORK_DIR/restored"',
    "SAFETY_BACKUP=",
    "CONFIG_SWITCHED=1",
)
require(
    "health check gates successful completion",
    'grep -q \'"status":"ready".*"pendingMigrationCount":0\'',
    "[[ \"$ready\" == true ]]",
    "Canlı geri yükleme tamamlandı:",
)

finish_start = source.index("finish() {")
finish_end = source.index("\n}\ntrap finish EXIT", finish_start) + 2
finish = source[finish_start:finish_end]
rollback = [
    'if [[ "$status" -ne 0 && "$WEB_STOPPED" -eq 1 ]]',
    'if [[ "$CONFIG_SWITCHED" -eq 1 ]]',
    'mv -f -- "$CONFIG_SNAPSHOT" "$CONFIG_FILE"',
    '"${COMPOSE[@]}" up -d database web proxy',
    'echo "Yeni geri yükleme volume',
]
positions = [finish.find(part) for part in rollback]
if any(position < 0 for position in positions) or positions != sorted(positions):
    raise SystemExit("FAIL: restore failure must restore the old configuration before restarting old services and preserve new volumes")
print("PASS: failed live restore reattaches the old configuration before restarting services and keeps new volumes for inspection")

for invariant, fragment in (
    ("maintenance operations are serialized", "flock -n 9"),
    ("live and restored Docker volumes are never deleted", "docker volume rm"),
    ("the existing database, media, and key volume names are preserved", "OLD_KEYS_VOLUME"),
):
    if invariant.startswith("live and"):
        if fragment in source:
            raise SystemExit(f"FAIL: {invariant}")
    elif fragment not in source:
        raise SystemExit(f"FAIL: {invariant}")
    print(f"PASS: {invariant}")
PY
