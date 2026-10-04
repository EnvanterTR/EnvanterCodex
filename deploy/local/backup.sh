#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
COMPOSE=(docker compose --env-file "$SCRIPT_DIR/.env" -f "$SCRIPT_DIR/compose.yaml")

if [[ ! -f "$SCRIPT_DIR/.env" ]]; then
  echo "Gizli ayar dosyası bulunamadı: $SCRIPT_DIR/.env. Önce yerel kurulumu tamamlayın." >&2
  exit 1
fi

OUTPUT_ROOT="${1:-$PROJECT_ROOT/backups}"
mkdir -p -- "$OUTPUT_ROOT"
OUTPUT_ROOT="$(cd -- "$OUTPUT_ROOT" && pwd -P)"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
SUFFIX="$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
BACKUP_DIR="$OUTPUT_ROOT/envantertr-$STAMP-$SUFFIX"
mkdir -- "$BACKUP_DIR"
DUMP_NAME="database-$SUFFIX.dump"
HELPER_NAME="envantertr-backup-$SUFFIX"
DB_CONTAINER=""
WEB_CONTAINER=""
DUMP_IN_CONTAINER=0
HELPER_CREATED=0
BACKUP_COMPLETE=0

cleanup() {
  if (( HELPER_CREATED )); then docker rm "$HELPER_NAME" >/dev/null 2>&1 || true; fi
  if (( DUMP_IN_CONTAINER )) && [[ -n "$DB_CONTAINER" ]]; then
    docker exec "$DB_CONTAINER" rm -f "/tmp/$DUMP_NAME" >/dev/null 2>&1 || true
  fi
  if (( ! BACKUP_COMPLETE )) && [[ -d "$BACKUP_DIR" ]] && [[ "$(dirname -- "$BACKUP_DIR")" == "$OUTPUT_ROOT" ]]; then
    rm -rf -- "$BACKUP_DIR" || true
  fi
}
trap cleanup EXIT

DB_CONTAINER="$("${COMPOSE[@]}" ps -q database | head -n 1)"
if [[ -z "$DB_CONTAINER" ]]; then
  echo 'Veritabanı konteyneri çalışmıyor. Önce deploy/local yönergeleriyle geliştirme ortamını başlatın.' >&2
  exit 1
fi
WEB_CONTAINER="$("${COMPOSE[@]}" ps -q web | head -n 1)"
if [[ -z "$WEB_CONTAINER" ]]; then
  echo 'Uygulama konteyneri çalışmıyor; şirket medya volume adı güvenle belirlenemedi.' >&2
  exit 1
fi
"${COMPOSE[@]}" exec -T database sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null
MEDIA_VOLUME="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/company-media"}}{{.Name}}{{end}}{{end}}' "$WEB_CONTAINER")"
if [[ -z "$MEDIA_VOLUME" ]]; then
  echo 'Şirket medya volume adı belirlenemedi; yedek eksik kalmaması için işlem durduruldu.' >&2
  exit 1
fi

DUMP_CONTAINER_PATH="/tmp/$DUMP_NAME"
DUMP_IN_CONTAINER=1
"${COMPOSE[@]}" exec -T database sh -c "pg_dump --no-owner --no-acl --format=custom --username=\"\$POSTGRES_USER\" \"\$POSTGRES_DB\" --file='$DUMP_CONTAINER_PATH'"
if ! docker exec "$DB_CONTAINER" pg_restore --list "$DUMP_CONTAINER_PATH" >/dev/null; then
  echo 'PostgreSQL yedeği okunamadı; kopya tamamlanmış sayılmadı.' >&2
  exit 1
fi
docker cp "$DB_CONTAINER:$DUMP_CONTAINER_PATH" "$BACKUP_DIR/$DUMP_NAME" >/dev/null

docker create --name "$HELPER_NAME" --volume "$MEDIA_VOLUME:/source:ro" postgres:17-alpine \
  sh -c 'tar -czf /tmp/company-media.tar.gz -C /source . && tar -tzf /tmp/company-media.tar.gz >/dev/null'
HELPER_CREATED=1
docker start -a "$HELPER_NAME"
docker cp "$HELPER_NAME:/tmp/company-media.tar.gz" "$BACKUP_DIR/company-media.tar.gz" >/dev/null

DB_SHA="$(sha256sum "$BACKUP_DIR/$DUMP_NAME" | cut -d ' ' -f 1)"
MEDIA_SHA="$(sha256sum "$BACKUP_DIR/company-media.tar.gz" | cut -d ' ' -f 1)"
CREATED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
cat > "$BACKUP_DIR/manifest.json" <<EOF
{
  "format": "envantertr-local-backup",
  "schemaVersion": 1,
  "createdAtUtc": "$CREATED_AT",
  "scope": "local-compose-instance",
  "databaseDump": "$DUMP_NAME",
  "companyMediaArchive": "company-media.tar.gz",
  "sha256": {
    "databaseDump": "$DB_SHA",
    "companyMediaArchive": "$MEDIA_SHA"
  },
  "verification": {
    "databaseDumpReadable": true,
    "companyMediaArchiveReadable": true
  },
  "encryption": "none",
  "restoreSupportedByApplication": false
}
EOF
BACKUP_COMPLETE=1

echo "Yedek oluşturuldu: $BACKUP_DIR"
echo 'Kapsam: yerel Compose veritabanı ve şirket medya volume’u. Dosyaları yetkisiz erişime kapalı, ayrı bir depoda saklayın.'
echo 'Bu yedek şifrelenmemiştir; uygulama içinden geri yükleme henüz desteklenmez.'
