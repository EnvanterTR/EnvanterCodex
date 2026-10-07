#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

CURRENT_RELEASE=/opt/envantertr/current
CONFIG_FILE=/etc/envantertr/server.env
fail() { printf 'HATA: %s\n' "$*" >&2; exit 1; }
[[ "$EUID" -eq 0 ]] || fail 'Geri yüklemeyi sudo ile root olarak çalıştırın.'
[[ $# -eq 1 ]] || fail 'Kullanım: sudo bash deploy/server/restore.sh /var/backups/envantertr/yedek.etbackup'
[[ -f "$1" ]] || fail 'Şifreli yedek dosyası bulunamadı.'
[[ -d "$CURRENT_RELEASE" && -f "$CONFIG_FILE" ]] || fail 'Kurulu sunucu bulunamadı.'
[[ "$(stat -c '%a' "$CONFIG_FILE")" == 600 ]] || fail 'Sunucu yapılandırma dosyası chmod 600 ile korunmalıdır.'
command -v docker >/dev/null || fail 'Docker Engine bulunamadı.'
command -v curl >/dev/null || fail 'curl bulunamadı.'
command -v python3 >/dev/null || fail 'python3 bulunamadı.'
command -v flock >/dev/null || fail 'Geri yükleme için flock gereklidir.'
exec 9>/run/lock/envantertr-operations.lock
flock -n 9 || fail 'Başka bir EnvanterTR sunucu işlemi sürüyor; geri yükleme başlatılmadı.'

read_config() { awk -F= -v key="$1" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE"; }
DOMAIN="$(read_config APP_DOMAIN)"
DB_PASSWORD="$(read_config POSTGRES_PASSWORD)"
BACKUP_PASSWORD_FILE="$(read_config BACKUP_PASSPHRASE_FILE)"
BACKUP_ROOT="$(read_config BACKUP_ROOT)"
OLD_DB_VOLUME="$(read_config DATABASE_VOLUME_NAME)"
OLD_MEDIA_VOLUME="$(read_config COMPANY_MEDIA_VOLUME_NAME)"
OLD_KEYS_VOLUME="$(read_config DATA_PROTECTION_KEYS_VOLUME_NAME)"
OLD_DB_VOLUME="${OLD_DB_VOLUME:-envantertr_server_database}"
OLD_MEDIA_VOLUME="${OLD_MEDIA_VOLUME:-envantertr_server_company_media}"
OLD_KEYS_VOLUME="${OLD_KEYS_VOLUME:-envantertr_server_data_protection_keys}"
[[ -n "$DOMAIN" && -n "$DB_PASSWORD" && -n "$BACKUP_ROOT" ]] || fail 'Sunucu yapılandırması eksik.'
[[ "$BACKUP_ROOT" == /* && "$BACKUP_ROOT" != / ]] || fail 'Yedek klasörü mutlak ve geçerli bir yol olmalıdır.'
[[ -f "$BACKUP_PASSWORD_FILE" && "$(stat -c '%a' "$BACKUP_PASSWORD_FILE")" == 600 ]] || fail 'Yedek parola dosyası eksik veya chmod 600 değil.'
for volume in "$OLD_DB_VOLUME" "$OLD_MEDIA_VOLUME" "$OLD_KEYS_VOLUME"; do
  [[ "$volume" =~ ^[A-Za-z0-9_.-]+$ ]] || fail 'Mevcut volume adında desteklenmeyen karakter var.'
done

BACKUP_FILE="$(realpath -- "$1")"
printf 'Bu işlem canlı veritabanı, şirket medyası ve oturum anahtarlarını verilen yedekle değiştirecek.\n'
printf 'Önce mevcut durum şifreli yedeklenecek; eski volume\x27lar hata halinde geri bağlanacak ve başarıda korunacak.\n'
read -r -p "Devam etmek için RESTORE $DOMAIN yazın: " CONFIRMATION
[[ "$CONFIRMATION" == "RESTORE $DOMAIN" ]] || fail 'Onay eşleşmedi; mevcut veriler değiştirilmedi.'
STAMP="$(date -u +%Y%m%d-%H%M%S)"
SUFFIX="$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
NEW_DB_VOLUME="envantertr_restore_${STAMP}_${SUFFIX}_database"
NEW_MEDIA_VOLUME="envantertr_restore_${STAMP}_${SUFFIX}_media"
NEW_KEYS_VOLUME="envantertr_restore_${STAMP}_${SUFFIX}_keys"
STAGE_DB="envantertr-restore-load-${STAMP}-${SUFFIX}"
WORK_DIR="$(mktemp -d "/var/tmp/envantertr-live-restore-${STAMP}-XXXXXXXX")"
chmod 0700 "$WORK_DIR"
CONFIG_SNAPSHOT="${CONFIG_FILE}.restore-${STAMP}-${SUFFIX}.bak"
cp -- "$CONFIG_FILE" "$CONFIG_SNAPSHOT"
chmod 0600 "$CONFIG_SNAPSHOT"
WEB_STOPPED=0
CONFIG_SWITCHED=0
COMPOSE=(docker compose --project-name envantertr-server --env-file "$CONFIG_FILE" -f "$CURRENT_RELEASE/deploy/server/compose.yaml")

finish() {
  local status=$?
  trap - EXIT
  docker rm --force "$STAGE_DB" >/dev/null 2>&1 || true
  if [[ "$status" -ne 0 && "$WEB_STOPPED" -eq 1 ]]; then
    if [[ "$CONFIG_SWITCHED" -eq 1 ]]; then
      mv -f -- "$CONFIG_SNAPSHOT" "$CONFIG_FILE"
    else
      rm -f -- "$CONFIG_SNAPSHOT"
    fi
    echo 'Geri yükleme tamamlanmadı; eski yapılandırma ve volume ile servisler açılıyor.' >&2
    "${COMPOSE[@]}" up -d database web proxy || echo 'Eski servisler başlatılamadı; volume verileri korunuyor.' >&2
    echo "Yeni geri yükleme volume'ları inceleme için korundu: $NEW_DB_VOLUME / $NEW_MEDIA_VOLUME / $NEW_KEYS_VOLUME" >&2
  else
    rm -f -- "$CONFIG_SNAPSHOT"
  fi
  rm -rf -- "$WORK_DIR"
  exit "$status"
}
trap finish EXIT

mkdir -m 0700 "$WORK_DIR/tool"
cp -a "$CURRENT_RELEASE/tools/EnvanterTR.Backup/." "$WORK_DIR/tool/"
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/restore" \
  --mount "type=bind,source=$BACKUP_FILE,target=/run/secrets/input.etbackup,readonly" \
  --mount "type=bind,source=$BACKUP_PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /restore/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- decrypt /run/secrets/input.etbackup /restore/restored /run/secrets/backup-password
bash "$CURRENT_RELEASE/deploy/local/restore-preview.sh" "$WORK_DIR/restored"

WEB_STOPPED=1
"${COMPOSE[@]}" stop web
COMPOSE_BACKUP_OUTPUT="$(ENVT_SERVER_LOCK_HELD=1 ENVT_SERVER_ENV_FILE="$CONFIG_FILE" bash "$CURRENT_RELEASE/deploy/server/backup.sh" 2>&1)" || {
  printf '%s\n' "$COMPOSE_BACKUP_OUTPUT" >&2
  fail 'Geri yükleme öncesi güvenlik yedeği alınamadı; eski volume değiştirilmedi.'
}
printf '%s\n' "$COMPOSE_BACKUP_OUTPUT"
SAFETY_BACKUP="$(printf '%s\n' "$COMPOSE_BACKUP_OUTPUT" | sed -n 's/^Şifreli yedek doğrulandı ve kaydedildi: //p' | tail -n 1)"
[[ -n "$SAFETY_BACKUP" && -f "$SAFETY_BACKUP" ]] || fail 'Geri dönüş güvenlik yedeği doğrulanamadı.'

mapfile -t METADATA < <(python3 - "$WORK_DIR/restored/manifest.json" <<'PY'
import json
import sys
from pathlib import Path
data = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
print(data["databaseDump"])
print(data["companyMediaArchive"])
print(data.get("dataProtectionKeysArchive", ""))
PY
)
[[ "${#METADATA[@]}" -eq 3 && -f "$WORK_DIR/restored/${METADATA[0]}" && -f "$WORK_DIR/restored/${METADATA[1]}" ]] || fail 'Açılan yedek manifesti geçerli değil.'
DUMP_PATH="$WORK_DIR/restored/${METADATA[0]}"
MEDIA_PATH="$WORK_DIR/restored/${METADATA[1]}"
KEYS_PATH=""
if [[ -n "${METADATA[2]}" ]]; then KEYS_PATH="$WORK_DIR/restored/${METADATA[2]}"; fi

docker volume create "$NEW_DB_VOLUME" >/dev/null
docker volume create "$NEW_MEDIA_VOLUME" >/dev/null
docker volume create "$NEW_KEYS_VOLUME" >/dev/null
docker run -d --name "$STAGE_DB" --network none \
  --env POSTGRES_DB=envantertr --env POSTGRES_USER=envantertr --env "POSTGRES_PASSWORD=$DB_PASSWORD" \
  --volume "$NEW_DB_VOLUME:/var/lib/postgresql/data" postgres:17-alpine >/dev/null
db_ready=false
for _ in $(seq 1 60); do
  if docker exec "$STAGE_DB" pg_isready -U envantertr -d envantertr >/dev/null 2>&1; then db_ready=true; break; fi
  sleep 1
done
[[ "$db_ready" == true ]] || fail 'Geri yükleme veritabanı hazır olmadı.'
docker cp "$DUMP_PATH" "$STAGE_DB:/tmp/restore.dump" >/dev/null
docker exec "$STAGE_DB" pg_restore --exit-on-error --no-owner --no-acl -U envantertr -d envantertr /tmp/restore.dump
docker exec "$STAGE_DB" psql -X -A -t -U envantertr -d envantertr -c 'SELECT 1' | grep -qx 1 || fail 'Geri yüklenen veritabanı sorgulanamadı.'

docker run --rm --network none \
  --mount "type=volume,source=$NEW_MEDIA_VOLUME,target=/restore" \
  --mount "type=bind,source=$MEDIA_PATH,target=/tmp/archive.tar.gz,readonly" \
  postgres:17-alpine sh -euc 'tar -tzf /tmp/archive.tar.gz >/dev/null; tar -xzf /tmp/archive.tar.gz -C /restore --no-same-owner --no-same-permissions'
if [[ -n "$KEYS_PATH" ]]; then
  docker run --rm --network none \
    --mount "type=volume,source=$NEW_KEYS_VOLUME,target=/restore" \
    --mount "type=bind,source=$KEYS_PATH,target=/tmp/archive.tar.gz,readonly" \
    postgres:17-alpine sh -euc 'tar -tzf /tmp/archive.tar.gz >/dev/null; tar -xzf /tmp/archive.tar.gz -C /restore --no-same-owner --no-same-permissions'
fi

APP_RELEASE_TAG="$(read_config RELEASE_TAG)"
APP_USER="$(docker image inspect --format '{{.Config.User}}' "envantertr-server-web:$APP_RELEASE_TAG")"
[[ "$APP_USER" =~ ^[0-9]+$ ]] || fail 'Uygulama imajının numeric kullanıcı kimliği belirlenemedi.'
for volume in "$NEW_MEDIA_VOLUME" "$NEW_KEYS_VOLUME"; do
  docker run --rm --network none --volume "$volume:/restore" postgres:17-alpine chown -R "$APP_USER:$APP_USER" /restore
done
docker stop "$STAGE_DB" >/dev/null

UPDATED_CONFIG="$WORK_DIR/server.env.updated"
awk -F= -v db="$NEW_DB_VOLUME" -v media="$NEW_MEDIA_VOLUME" -v keys="$NEW_KEYS_VOLUME" '
  BEGIN { OFS="=" }
  $1 == "DATABASE_VOLUME_NAME" { print "DATABASE_VOLUME_NAME", db; seen_db=1; next }
  $1 == "COMPANY_MEDIA_VOLUME_NAME" { print "COMPANY_MEDIA_VOLUME_NAME", media; seen_media=1; next }
  $1 == "DATA_PROTECTION_KEYS_VOLUME_NAME" { print "DATA_PROTECTION_KEYS_VOLUME_NAME", keys; seen_keys=1; next }
  { print }
  END {
    if (!seen_db) print "DATABASE_VOLUME_NAME", db
    if (!seen_media) print "COMPANY_MEDIA_VOLUME_NAME", media
    if (!seen_keys) print "DATA_PROTECTION_KEYS_VOLUME_NAME", keys
  }
' "$CONFIG_FILE" > "$UPDATED_CONFIG"
chmod 0600 "$UPDATED_CONFIG"
CONFIG_SWITCHED=1
mv -f -- "$UPDATED_CONFIG" "$CONFIG_FILE"

"${COMPOSE[@]}" up -d database
"${COMPOSE[@]}" run --rm web --migrate
"${COMPOSE[@]}" up -d web proxy
ready=false
for _ in $(seq 1 60); do
  if curl --silent --show-error --fail --insecure --max-time 5 \
      --resolve "$DOMAIN:443:127.0.0.1" "https://$DOMAIN/api/status" 2>/dev/null \
      | grep -q '"status":"ready".*"pendingMigrationCount":0'; then
    ready=true
    break
  fi
  sleep 2
done
[[ "$ready" == true ]] || fail 'Geri yüklenen sistem hazır olmadı; eski yapılandırma ve volume otomatik geri getiriliyor.'

printf 'Canlı geri yükleme tamamlandı: https://%s\n' "$DOMAIN"
printf 'Güvenlik yedeği: %s\n' "$SAFETY_BACKUP"
printf 'Önceki volume''lar korunuyor: %s / %s / %s\n' "$OLD_DB_VOLUME" "$OLD_MEDIA_VOLUME" "$OLD_KEYS_VOLUME"
printf 'Yeni volume''lar: %s / %s / %s\n' "$NEW_DB_VOLUME" "$NEW_MEDIA_VOLUME" "$NEW_KEYS_VOLUME"
printf 'Geri yüklenen yedek: %s\n' "$BACKUP_FILE"
