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

if "flock -n 9" not in source:
    raise SystemExit("FAIL: maintenance operations are serialized")
print("PASS: maintenance operations are serialized")

if any(fragment in source for fragment in ("docker volume rm", "docker volume prune", "down --volumes")):
    raise SystemExit("FAIL: restore must never delete existing or newly staged Docker volumes")
print("PASS: live and restored Docker volumes are never deleted")

if not all(name in source for name in ("OLD_DB_VOLUME", "OLD_MEDIA_VOLUME", "OLD_KEYS_VOLUME")):
    raise SystemExit("FAIL: existing database, media, and key volume names must all be preserved")
print("PASS: existing database, media, and key volume names are preserved")

verify_source = Path(sys.argv[1]).resolve().parents[1] / "local" / "verify-backup.sh"
verify = verify_source.read_text(encoding="utf-8")
media_check = 'python3 "$SCRIPT_DIR/validate-tar-archive.py" "$BACKUP_DIR/$MEDIA_NAME"'
keys_check = 'python3 "$SCRIPT_DIR/validate-tar-archive.py" "$BACKUP_DIR/${METADATA[4]}"'
database_inspection = 'docker run --rm --network none --mount "type=bind,source=$BACKUP_DIR,target=/backup,readonly"'
positions = [verify.find(part) for part in (keys_check, media_check, database_inspection)]
if any(position < 0 for position in positions) or positions != sorted(positions):
    raise SystemExit("FAIL: media/key path, type, duplicate and expanded-size validation must happen before database restore preview")
print("PASS: Linux backup verification bounds media/key tar paths, types, duplicates, entries and expanded bytes before restore preview")
PY
