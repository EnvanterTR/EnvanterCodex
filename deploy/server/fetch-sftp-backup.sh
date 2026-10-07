#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
CONFIG_FILE="${ENVT_SERVER_ENV_FILE:-/etc/envantertr/server.env}"
fail() { printf 'HATA: %s\n' "$*" >&2; exit 1; }

[[ "$EUID" -eq 0 ]] || fail 'Kurtarma yedeğini sudo ile alın.'
[[ $# -eq 1 && "$1" =~ ^envantertr-server-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}\.etbackup$ ]] || \
  fail 'Kullanım: sudo bash deploy/server/fetch-sftp-backup.sh envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup'
[[ -r "$CONFIG_FILE" ]] || fail "Yapılandırma bulunamadı: $CONFIG_FILE"
command -v flock >/dev/null || fail 'Kurtarma indirmesi için flock gereklidir.'
command -v sftp >/dev/null || fail 'Kurtarma indirmesi için OpenSSH sftp istemcisi gereklidir.'
command -v docker >/dev/null || fail 'Şifreli yedeği doğrulamak için Docker gereklidir.'
command -v sha256sum >/dev/null || fail 'SHA-256 doğrulaması için sha256sum gereklidir.'
command -v realpath >/dev/null || fail 'Geçici dizin güvenliği için realpath gereklidir.'
command -v stat >/dev/null || fail 'Dosya izinlerini denetlemek için stat gereklidir.'
[[ -f "$PROJECT_ROOT/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj" && -f "$PROJECT_ROOT/deploy/local/restore-preview.sh" ]] || \
  fail 'Yedek aracı veya izole kurtarma provası bu sürüm paketinde bulunamadı.'

read_config() { awk -F= -v key="$1" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE"; }
BACKUP_ROOT="$(read_config BACKUP_ROOT)"
PASSWORD_FILE="$(read_config BACKUP_PASSPHRASE_FILE)"
SFTP_TARGET="$(read_config BACKUP_SFTP_TARGET)"
SFTP_DIRECTORY="$(read_config BACKUP_SFTP_DIRECTORY)"
SFTP_KEY_FILE="$(read_config BACKUP_SFTP_KEY_FILE)"
SFTP_KNOWN_HOSTS_FILE="$(read_config BACKUP_SFTP_KNOWN_HOSTS_FILE)"
[[ "$BACKUP_ROOT" = /* && "$BACKUP_ROOT" != / ]] || fail 'BACKUP_ROOT güvenli bir mutlak yol olmalıdır.'
[[ -n "$SFTP_TARGET" && -n "$SFTP_DIRECTORY" && -n "$SFTP_KEY_FILE" && -n "$SFTP_KNOWN_HOSTS_FILE" ]] || \
  fail 'Uzak yedek indirme için dört BACKUP_SFTP_* ayarının tümü gereklidir.'
[[ "$SFTP_TARGET" =~ ^[A-Za-z0-9._-]+@[A-Za-z0-9.-]+$ ]] || fail 'BACKUP_SFTP_TARGET user@host biçiminde olmalıdır.'
[[ "$SFTP_DIRECTORY" =~ ^/[A-Za-z0-9._/-]+$ && "$SFTP_DIRECTORY" != *..* && "$SFTP_DIRECTORY" != *//* ]] || \
  fail 'BACKUP_SFTP_DIRECTORY boşluksuz, mutlak ve güvenli bir uzak dizin olmalıdır.'
[[ "$BACKUP_ROOT" != *'"'* && "$BACKUP_ROOT" != *$'\n'* && "$BACKUP_ROOT" != *$'\r'* ]] || \
  fail 'BACKUP_ROOT SFTP batch dosyası için çift tırnak veya satır sonu içeremez.'
[[ -f "$PASSWORD_FILE" && -r "$PASSWORD_FILE" && "$(stat -c '%u:%a' "$PASSWORD_FILE")" == 0:600 ]] || \
  fail 'Yedek parolası root sahipliğinde ve chmod 600 ile korunmalıdır.'
[[ -f "$SFTP_KEY_FILE" && -r "$SFTP_KEY_FILE" && "$(stat -c '%u:%a' "$SFTP_KEY_FILE")" == 0:600 ]] || \
  fail 'SFTP özel anahtarı root sahipliğinde ve chmod 600 ile korunmalıdır.'
KNOWN_HOSTS_MODE="$(stat -c '%u:%a' "$SFTP_KNOWN_HOSTS_FILE" 2>/dev/null || true)"
[[ -f "$SFTP_KNOWN_HOSTS_FILE" && -r "$SFTP_KNOWN_HOSTS_FILE" && "$KNOWN_HOSTS_MODE" =~ ^0:[0-7]{3,4}$ && \
  "${KNOWN_HOSTS_MODE: -2:1}" != [2367] && "${KNOWN_HOSTS_MODE: -1}" != [2367] ]] || \
  fail 'Sabitlenmiş known_hosts root sahipliğinde olmalı, grup/diğer kullanıcılarca yazılamamalıdır.'

exec 9>/run/lock/envantertr-operations.lock
flock -n 9 || fail 'Başka bir EnvanterTR sunucu işlemi sürüyor; uzak yedek indirilmedi.'
install -d -m 0700 "$BACKUP_ROOT"
BACKUP_ROOT="$(cd -- "$BACKUP_ROOT" && pwd -P)"
FINAL_PATH="$BACKUP_ROOT/$1"
[[ ! -e "$FINAL_PATH" && ! -e "$FINAL_PATH.sha256" ]] || fail 'Aynı adlı yerel yedek var; üzerine yazılmadı.'
WORK_DIR="$(mktemp -d "$BACKUP_ROOT/.remote-download-XXXXXXXX")"
chmod 0700 "$WORK_DIR"
cleanup() {
  local resolved_work
  resolved_work="$(realpath -m -- "$WORK_DIR")"
  if [[ "$resolved_work" == "$BACKUP_ROOT"/.remote-download-* && -d "$resolved_work" ]]; then
    rm -rf -- "$resolved_work"
  fi
}
trap cleanup EXIT

BATCH_FILE="$WORK_DIR/sftp.batch"
printf 'cd "%s"\nlcd "%s"\nget "%s"\nget "%s.sha256"\n' \
  "$SFTP_DIRECTORY" "$WORK_DIR" "$1" "$1" > "$BATCH_FILE"
sftp -i "$SFTP_KEY_FILE" -oBatchMode=yes -oIdentitiesOnly=yes \
  -oStrictHostKeyChecking=yes -oUserKnownHostsFile="$SFTP_KNOWN_HOSTS_FILE" \
  -b "$BATCH_FILE" "$SFTP_TARGET"
(cd -- "$WORK_DIR" && sha256sum --check -- "$1.sha256")

cp -a "$PROJECT_ROOT/tools/EnvanterTR.Backup" "$WORK_DIR/tool"
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/backup" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /backup/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- verify "/backup/$1" /run/secrets/backup-password
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/backup" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /backup/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- decrypt "/backup/$1" /backup/verified /run/secrets/backup-password
bash "$PROJECT_ROOT/deploy/local/restore-preview.sh" "$WORK_DIR/verified"

mv -- "$WORK_DIR/$1" "$FINAL_PATH"
mv -- "$WORK_DIR/$1.sha256" "$FINAL_PATH.sha256"
chmod 0600 "$FINAL_PATH" "$FINAL_PATH.sha256"
printf 'Uzak yedek indirildi, SHA-256/şifre doğrulaması ve izole kurtarma provası geçti: %s\n' "$FINAL_PATH"
printf 'Canlı geri yükleme için ayrıca restore.sh komutunu kullanın; bu işlem canlı veritabanına dokunmadı.\n'
