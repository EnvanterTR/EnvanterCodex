#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
SOURCE_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
INSTALL_ROOT="/opt/envantertr"
CONFIG_DIR="/etc/envantertr"
CONFIG_FILE="$CONFIG_DIR/server.env"
BACKUP_PASSWORD_FILE="$CONFIG_DIR/backup.pass"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
RELEASE_DIR="$INSTALL_ROOT/releases/$STAMP"
DOMAIN="${1:-${APP_DOMAIN:-}}"

fail() { printf 'HATA: %s\n' "$*" >&2; exit 1; }
on_error() { local status=$?; printf 'Kurulum %s koduyla durdu. Dosyalar ve volume\x27lar korundu; günlükleri inceleyin.\n' "$status" >&2; }
trap on_error ERR

[[ "$EUID" -eq 0 ]] || fail 'Kurulumu sudo ile root olarak çalıştırın.'
[[ -n "$DOMAIN" && "$DOMAIN" =~ ^[A-Za-z0-9]([A-Za-z0-9.-]{0,251}[A-Za-z0-9])?$ && "$DOMAIN" != *..* ]] || fail 'Geçerli bir DNS alan adı verin: sudo bash deploy/server/install.sh envanter.sirketiniz.com'
[[ "${DOMAIN,,}" != localhost ]] || fail 'HTTPS kurulumu için localhost kullanılamaz.'
command -v docker >/dev/null || fail 'Docker Engine kurulu olmalıdır.'
command -v openssl >/dev/null || fail 'OpenSSL kurulu olmalıdır.'
command -v curl >/dev/null || fail 'curl kurulu olmalıdır.'
command -v systemctl >/dev/null || fail 'systemd bu kurulum profili için gereklidir.'
docker compose version >/dev/null 2>&1 || fail 'Docker Compose v2 eklentisi kurulu olmalıdır.'
[[ -f "$SOURCE_ROOT/Dockerfile" && -f "$SCRIPT_DIR/compose.yaml" ]] || fail 'Komutu EnvanterTR kaynak paketinin içinden çalıştırın.'
[[ ! -e "$INSTALL_ROOT/current" && ! -e "$CONFIG_FILE" ]] || fail 'EnvanterTR kurulumu zaten var; update.sh komutunu kullanın.'

install -d -m 0750 "$INSTALL_ROOT/releases" "$CONFIG_DIR" /var/backups/envantertr
install -d -m 0750 "$RELEASE_DIR"
tar -C "$SOURCE_ROOT" \
  --exclude=.git --exclude=backups --exclude=data --exclude='*/.env' --exclude=.env \
  --exclude='*/bin' --exclude='*/obj' --exclude='*/node_modules' --exclude='*/.vs' \
  -cf - . | tar -C "$RELEASE_DIR" -xf -

POSTGRES_PASSWORD="$(openssl rand -hex 32)"
SETUP_BOOTSTRAP_TOKEN="$(openssl rand -hex 32)"
SETUP_PLATFORM_BOOTSTRAP_TOKEN="$(openssl rand -hex 32)"
BACKUP_PASSPHRASE="$(openssl rand -hex 32)"
printf '%s\n' "$BACKUP_PASSPHRASE" > "$BACKUP_PASSWORD_FILE"
chmod 0600 "$BACKUP_PASSWORD_FILE"
unset BACKUP_PASSPHRASE
cat > "$CONFIG_FILE" <<EOF
APP_DOMAIN=$DOMAIN
POSTGRES_PASSWORD=$POSTGRES_PASSWORD
SETUP_BOOTSTRAP_TOKEN=$SETUP_BOOTSTRAP_TOKEN
SETUP_PLATFORM_BOOTSTRAP_TOKEN=$SETUP_PLATFORM_BOOTSTRAP_TOKEN
RELEASE_TAG=initial-$STAMP
DATABASE_VOLUME_NAME=envantertr_server_database
COMPANY_MEDIA_VOLUME_NAME=envantertr_server_company_media
DATA_PROTECTION_KEYS_VOLUME_NAME=envantertr_server_data_protection_keys
BACKUP_ROOT=/var/backups/envantertr
BACKUP_PASSPHRASE_FILE=$BACKUP_PASSWORD_FILE
BACKUP_RETENTION_DAYS=30
BACKUP_SFTP_TARGET=
BACKUP_SFTP_DIRECTORY=
BACKUP_SFTP_KEY_FILE=
BACKUP_SFTP_KNOWN_HOSTS_FILE=
EOF
chmod 0600 "$CONFIG_FILE"
unset POSTGRES_PASSWORD SETUP_BOOTSTRAP_TOKEN SETUP_PLATFORM_BOOTSTRAP_TOKEN

compose=(docker compose --project-name envantertr-server --env-file "$CONFIG_FILE" -f "$RELEASE_DIR/deploy/server/compose.yaml")
"${compose[@]}" build web
"${compose[@]}" up -d database

db_container=""
for _ in $(seq 1 60); do
  db_container="$("${compose[@]}" ps -q database)"
  if [[ -n "$db_container" ]] && [[ "$(docker inspect --format '{{.State.Health.Status}}' "$db_container" 2>/dev/null || true)" == healthy ]]; then break; fi
  sleep 2
done
[[ -n "$db_container" && "$(docker inspect --format '{{.State.Health.Status}}' "$db_container")" == healthy ]] || fail 'PostgreSQL başlatılamadı; `docker compose logs database` çıktısını inceleyin.'
"${compose[@]}" run --rm web --migrate
"${compose[@]}" up -d web proxy

ready=false
for _ in $(seq 1 60); do
  if curl --silent --show-error --fail --insecure --max-time 5 \
      --resolve "$DOMAIN:443:127.0.0.1" "https://$DOMAIN/api/status" 2>/dev/null | grep -q '"status":"ready"'; then
    ready=true
    break
  fi
  sleep 2
done
[[ "$ready" == true ]] || fail "HTTPS uygulaması ready durumuna geçmedi. `docker compose logs web proxy` çıktısını inceleyin; veri volume'ları korunuyor."

ln -s "$RELEASE_DIR" "$INSTALL_ROOT/current"
install -m 0644 "$RELEASE_DIR/deploy/server/envantertr-backup.service" /etc/systemd/system/envantertr-backup.service
install -m 0644 "$RELEASE_DIR/deploy/server/envantertr-backup.timer" /etc/systemd/system/envantertr-backup.timer
systemctl daemon-reload
systemctl enable --now envantertr-backup.timer
printf '\nEnvanterTR sunucu kurulumu hazır: https://%s\n' "$DOMAIN"
printf 'İlk şirket sahibi kurulumu: https://%s/setup.html\n' "$DOMAIN"
printf 'Şirket kurulum anahtarını görüntüleme: sudo awk -F= '\''$1=="SETUP_BOOTSTRAP_TOKEN" {print $2}'\'' %s\n' "$CONFIG_FILE"
printf 'Backup parola dosyasını bu sunucu dışında güvenli bir yere alın: %s\n' "$BACKUP_PASSWORD_FILE"
printf 'İnternet DNS kaydı bu sunucuyu göstermeli; güvenlik duvarında TCP 80/443 ve UDP 443 erişimine izin verin.\n'
printf 'SETUP_BOOTSTRAP_TOKEN ve SETUP_PLATFORM_BOOTSTRAP_TOKEN ilk hesaplar kurulduktan sonra server.env içinden kaldırılmalıdır.\n'
