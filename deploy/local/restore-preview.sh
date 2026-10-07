#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

if [[ $# -ne 1 || ! -d "$1" ]]; then
  echo 'Kullanım: bash deploy/local/restore-preview.sh <yedek-klasörü>' >&2
  exit 2
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_DIR="$(cd -- "$1" && pwd -P)"
bash "$SCRIPT_DIR/verify-backup.sh" "$BACKUP_DIR"

mapfile -t METADATA < <(python3 - "${BACKUP_DIR}/manifest.json" <<'PY'
import json
import sys
from pathlib import Path
data = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
print(data["databaseDump"])
print(data["companyMediaArchive"])
print(data.get("dataProtectionKeysArchive", ""))
PY
)
DUMP_PATH="$BACKUP_DIR/${METADATA[0]}"
MEDIA_PATH="$BACKUP_DIR/${METADATA[1]}"
KEYS_PATH=""
if [[ -n "${METADATA[2]}" ]]; then KEYS_PATH="$BACKUP_DIR/${METADATA[2]}"; fi
CONTAINER="envantertr-restore-preview-$(od -An -N6 -tx1 /dev/urandom | tr -d ' \n')"
PASSWORD="$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')"
CONTAINER_CREATED=0

cleanup() {
  if (( CONTAINER_CREATED )); then docker rm --force "$CONTAINER" >/dev/null 2>&1 || true; fi
}
trap cleanup EXIT

# No published port, host mount, or network: restored data exists only in this disposable container.
docker create --name "$CONTAINER" --network none --env "POSTGRES_PASSWORD=$PASSWORD" postgres:17-alpine
CONTAINER_CREATED=1
docker start "$CONTAINER" >/dev/null

ready=0
for _ in $(seq 1 60); do
  if docker exec "$CONTAINER" pg_isready -U postgres >/dev/null 2>&1; then ready=1; break; fi
  sleep 1
done
if (( ! ready )); then echo 'Yalıtılmış PostgreSQL konteyneri 60 saniyede hazır olmadı.' >&2; exit 1; fi

docker cp "$DUMP_PATH" "$CONTAINER:/tmp/restore.dump" >/dev/null
docker cp "$MEDIA_PATH" "$CONTAINER:/tmp/company-media.tar.gz" >/dev/null
docker exec "$CONTAINER" pg_restore --exit-on-error --no-owner --no-acl -U postgres -d postgres /tmp/restore.dump

migration_exists="$(docker exec "$CONTAINER" psql -X -A -t -U postgres -d postgres -c "SELECT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname='public' AND tablename='__EFMigrationsHistory')" | tr -d '[:space:]')"
migration_count='yok'
if [[ "$migration_exists" == t ]]; then
  migration_count="$(docker exec "$CONTAINER" psql -X -A -t -U postgres -d postgres -c 'SELECT count(*) FROM "__EFMigrationsHistory"' | tr -d '[:space:]')"
fi

python3 - "$MEDIA_PATH" <<'PY'
import sys
import tarfile
from pathlib import PurePosixPath
with tarfile.open(sys.argv[1], "r:gz") as archive:
    for member in archive.getmembers():
        path = PurePosixPath(member.name)
        if path.is_absolute() or ".." in path.parts or not (member.isfile() or member.isdir()):
            raise SystemExit(f"Güvenli olmayan medya arşivi girdisi: {member.name}")
PY

if [[ -n "$KEYS_PATH" ]]; then
  python3 - "$KEYS_PATH" <<'PY'
import sys
import tarfile
from pathlib import PurePosixPath
with tarfile.open(sys.argv[1], "r:gz") as archive:
    for member in archive.getmembers():
        path = PurePosixPath(member.name)
        if path.is_absolute() or ".." in path.parts or not (member.isfile() or member.isdir()):
            raise SystemExit(f"Güvenli olmayan anahtar arşivi girdisi: {member.name}")
PY
fi

docker exec "$CONTAINER" mkdir -p /tmp/media-preview
docker exec "$CONTAINER" tar -xzf /tmp/company-media.tar.gz -C /tmp/media-preview --no-same-owner --no-same-permissions
media_file_count="$(docker exec "$CONTAINER" sh -c 'find /tmp/media-preview -type f | wc -l' | tr -d '[:space:]')"
key_file_count=0
if [[ -n "$KEYS_PATH" ]]; then
  docker cp "$KEYS_PATH" "$CONTAINER:/tmp/data-protection-keys.tar.gz" >/dev/null
  docker exec "$CONTAINER" mkdir -p /tmp/data-protection-preview
  docker exec "$CONTAINER" tar -xzf /tmp/data-protection-keys.tar.gz -C /tmp/data-protection-preview --no-same-owner --no-same-permissions
  key_file_count="$(docker exec "$CONTAINER" sh -c 'find /tmp/data-protection-preview -type f | wc -l' | tr -d '[:space:]')"
fi

echo 'Kurtarma önizlemesi başarılı: PostgreSQL dökümü, medya arşivi ve varsa Data Protection anahtar arşivi izole konteynere açıldı.'
echo "Migration kaydı: $migration_count; medya dosyası: $media_file_count; Data Protection anahtar dosyası: $key_file_count."
echo 'Üretim/yerel uygulama veritabanına veya kalıcı Docker volume-una dokunulmadı.'
