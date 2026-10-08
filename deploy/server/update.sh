#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
SOURCE_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
INSTALL_ROOT="/opt/envantertr"
CURRENT_RELEASE="$INSTALL_ROOT/current"
OLD_RELEASE=""
CONFIG_FILE="/etc/envantertr/server.env"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
RELEASE_DIR="$INSTALL_ROOT/releases/$STAMP"
SOURCE_ARGUMENT="${1:-$SOURCE_ROOT}"

fail() { printf 'HATA: %s\n' "$*" >&2; exit 1; }
trap 'status=$?; printf "Güncelleme %s koduyla durdu. Veritabanı ve medya volume\x27ları silinmedi.\n" "$status" >&2' ERR

[[ "$EUID" -eq 0 ]] || fail 'Güncellemeyi sudo ile root olarak çalıştırın.'
command -v flock >/dev/null || fail 'Güncelleme için flock gereklidir.'
exec 9>/run/lock/envantertr-operations.lock
flock -n 9 || fail 'Başka bir EnvanterTR sunucu işlemi sürüyor; güncelleme başlatılmadı.'
[[ -d "$CURRENT_RELEASE" && -f "$CONFIG_FILE" ]] || fail 'Kurulu EnvanterTR bulunamadı; önce install.sh çalıştırın.'
OLD_RELEASE="$(readlink -f "$CURRENT_RELEASE")"
[[ -d "$SOURCE_ARGUMENT" ]] || fail 'Kaynak dizini bulunamadı.'
SOURCE_ROOT="$(cd -- "$SOURCE_ARGUMENT" && pwd -P)"
[[ -f "$SOURCE_ROOT/Dockerfile" && -f "$SOURCE_ROOT/deploy/server/compose.yaml" ]] || fail 'Kaynak klasöründe sunucu dağıtımı bulunamadı.'
command -v docker >/dev/null || fail 'Docker Engine bulunamadı.'
command -v curl >/dev/null || fail 'curl bulunamadı.'
docker compose version >/dev/null 2>&1 || fail 'Docker Compose v2 eklentisi bulunamadı.'

install -d -m 0750 "$INSTALL_ROOT/releases"
install -d -m 0750 "$RELEASE_DIR"
tar -C "$SOURCE_ROOT" \
  --exclude=.git --exclude=backups --exclude=data --exclude='*/.env' --exclude=.env \
  --exclude='*/bin' --exclude='*/obj' --exclude='*/node_modules' --exclude='*/.vs' \
  -cf - . | tar -C "$RELEASE_DIR" -xf -

read_config() { awk -F= -v key="$1" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE"; }
DOMAIN="$(read_config APP_DOMAIN)"
OLD_TAG="$(read_config RELEASE_TAG)"
NEW_TAG="release-$STAMP"
[[ -n "$DOMAIN" && -n "$OLD_TAG" ]] || fail 'Sunucu yapılandırmasında etki alanı veya sürüm etiketi bulunamadı.'
TAG_OVERRIDE="$RELEASE_DIR/.release-tag.env"
printf 'RELEASE_TAG=%s\n' "$NEW_TAG" > "$TAG_OVERRIDE"
chmod 0600 "$TAG_OVERRIDE"

compose_old=(docker compose --project-name envantertr-server --env-file "$CONFIG_FILE" -f "$CURRENT_RELEASE/deploy/server/compose.yaml")
compose_new=(docker compose --project-name envantertr-server --env-file "$CONFIG_FILE" --env-file "$TAG_OVERRIDE" -f "$RELEASE_DIR/deploy/server/compose.yaml")
WEB_STOPPED=0
MIGRATION_STARTED=0
restore_old_app_if_safe() {
  local status=$?
  if [[ "$status" -ne 0 && "$WEB_STOPPED" -eq 1 && "$MIGRATION_STARTED" -eq 0 ]]; then
    echo 'Migration başlamadı; önceki uygulama yeniden başlatılıyor.' >&2
    "${compose_old[@]}" up -d web proxy || echo "Önceki uygulama otomatik başlatılamadı; volume'lar korunuyor." >&2
  fi
}
trap restore_old_app_if_safe EXIT
"${compose_old[@]}" stop web
WEB_STOPPED=1
ENVT_SERVER_LOCK_HELD=1 ENVT_SERVER_ENV_FILE="$CONFIG_FILE" bash "$CURRENT_RELEASE/deploy/server/backup.sh"

"${compose_new[@]}" build web
MIGRATION_STARTED=1
"${compose_new[@]}" run --rm web --migrate
"${compose_new[@]}" up -d web proxy

ready=false
for _ in $(seq 1 60); do
  if curl --silent --show-error --fail --insecure --max-time 5 \
      --resolve "$DOMAIN:443:127.0.0.1" "https://$DOMAIN/api/status" 2>/dev/null | grep -q '"status":"ready"'; then
    ready=true
    break
  fi
  sleep 2
done
if [[ "$ready" != true ]]; then
  "${compose_new[@]}" stop web || true
  fail 'Güncellenen uygulama ready olmadı. Yeni uygulama durduruldu; şifreli yedekle geri yükleme ve günlük incelemesi gerekir. Eski uygulama otomatik başlatılmadı; yeni şemayla uyumluluğu doğrulanmamış olabilir.'
fi

CONFIG_TEMP="$CONFIG_FILE.$STAMP.tmp"
awk -F= -v tag="$NEW_TAG" '$1 == "RELEASE_TAG" {$0 = "RELEASE_TAG=" tag} {print}' "$CONFIG_FILE" > "$CONFIG_TEMP"
chmod 0600 "$CONFIG_TEMP"
mv -- "$CONFIG_TEMP" "$CONFIG_FILE"
rm -f -- "$TAG_OVERRIDE"
NEXT_LINK="$INSTALL_ROOT/current.$STAMP"
ln -s "$RELEASE_DIR" "$NEXT_LINK"
mv -Tf -- "$NEXT_LINK" "$CURRENT_RELEASE"
install -m 0644 "$CURRENT_RELEASE/deploy/server/envantertr-backup.service" /etc/systemd/system/envantertr-backup.service
install -m 0644 "$CURRENT_RELEASE/deploy/server/envantertr-backup.timer" /etc/systemd/system/envantertr-backup.timer
install -m 0644 "$CURRENT_RELEASE/deploy/server/envantertr-backup-health.service" /etc/systemd/system/envantertr-backup-health.service
install -m 0644 "$CURRENT_RELEASE/deploy/server/envantertr-backup-health.timer" /etc/systemd/system/envantertr-backup-health.timer
install -m 0644 "$CURRENT_RELEASE/deploy/server/envantertr-backup-alert@.service" /etc/systemd/system/envantertr-backup-alert@.service
systemctl daemon-reload
printf 'Güncelleme tamamlandı: https://%s\n' "$DOMAIN"
printf 'Önceki sürüm korunuyor: %s\n' "$OLD_RELEASE"
printf 'Güncelleme öncesi doğrulanmış şifreli yedek /var/backups/envantertr altında bulunuyor.\n'
