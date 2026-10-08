#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
CONFIG_FILE="${ENVT_SERVER_ENV_FILE:-/etc/envantertr/server.env}"
[[ "$EUID" -eq 0 ]] || { echo 'Yedeği sudo ile root olarak alın.' >&2; exit 1; }
[[ -r "$CONFIG_FILE" ]] || { echo "Yapılandırma bulunamadı: $CONFIG_FILE" >&2; exit 1; }
command -v flock >/dev/null || { echo 'Yedek işlemi için flock gereklidir.' >&2; exit 1; }
if [[ "${ENVT_SERVER_LOCK_HELD:-0}" != 1 ]]; then
  exec 9>/run/lock/envantertr-operations.lock
  flock -n 9 || { echo 'Başka bir EnvanterTR sunucu işlemi sürüyor; yedek başlatılmadı.' >&2; exit 1; }
fi
read_config() { awk -F= -v key="$1" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE"; }
BACKUP_ROOT="$(read_config BACKUP_ROOT)"
PASSWORD_FILE="$(read_config BACKUP_PASSPHRASE_FILE)"
RETENTION_DAYS="$(read_config BACKUP_RETENTION_DAYS)"
SFTP_TARGET="$(read_config BACKUP_SFTP_TARGET || true)"
SFTP_DIRECTORY="$(read_config BACKUP_SFTP_DIRECTORY || true)"
SFTP_KEY_FILE="$(read_config BACKUP_SFTP_KEY_FILE || true)"
SFTP_KNOWN_HOSTS_FILE="$(read_config BACKUP_SFTP_KNOWN_HOSTS_FILE || true)"
SFTP_RETENTION_DAYS="$(read_config BACKUP_SFTP_RETENTION_DAYS || true)"
SFTP_RETENTION_DAYS="${SFTP_RETENTION_DAYS:-90}"
[[ "$BACKUP_ROOT" = /* && "$BACKUP_ROOT" != / ]] || { echo 'BACKUP_ROOT güvenli bir mutlak yol olmalıdır.' >&2; exit 1; }
[[ "$RETENTION_DAYS" =~ ^[1-9][0-9]{0,3}$ ]] || { echo 'BACKUP_RETENTION_DAYS 1-999 arasında olmalıdır.' >&2; exit 1; }
[[ "$SFTP_RETENTION_DAYS" =~ ^([1-9][0-9]{0,2}|0)$ ]] || { echo 'BACKUP_SFTP_RETENTION_DAYS 0-999 arasında olmalıdır.' >&2; exit 1; }
[[ -f "$PASSWORD_FILE" && -r "$PASSWORD_FILE" ]] || { echo 'Şifreleme parola dosyası eksik veya okunamıyor.' >&2; exit 1; }
[[ "$(stat -c '%u:%a' "$PASSWORD_FILE")" == 0:600 ]] || { echo 'Şifreleme parola dosyası root sahipliğinde ve chmod 600 ile korunmalıdır.' >&2; exit 1; }
install -d -m 0700 "$BACKUP_ROOT"
BACKUP_ROOT="$(cd -- "$BACKUP_ROOT" && pwd -P)"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
RUN_SUFFIX="$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
WORK_DIR="$(mktemp -d "$BACKUP_ROOT/.staging-$STAMP-XXXXXXXX")"
chmod 0700 "$WORK_DIR"
RAW_DIR="$WORK_DIR/raw"
mkdir -m 0700 "$RAW_DIR"
ENCRYPTED="$WORK_DIR/envantertr-server-$STAMP-$RUN_SUFFIX.etbackup"
DB_SUFFIX="$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
DUMP_NAME="database-$DB_SUFFIX.dump"
DATABASE_TEMP="/tmp/$DUMP_NAME"
COMPOSE=(docker compose --project-name envantertr-server --env-file "$CONFIG_FILE" -f "$SCRIPT_DIR/compose.yaml")

cleanup() {
  local container="${1:-}"
  if [[ -n "$container" && -n "${DATABASE_TEMP:-}" ]]; then docker exec "$container" rm -f "$DATABASE_TEMP" >/dev/null 2>&1 || true; fi
  local resolved_work
  resolved_work="$(realpath -m -- "$WORK_DIR")"
  if [[ "$resolved_work" == "$BACKUP_ROOT"/.staging-* && -d "$resolved_work" ]]; then rm -rf -- "$resolved_work"; fi
}
DB_CONTAINER="$("${COMPOSE[@]}" ps -q database)"
WEB_CONTAINER="$("${COMPOSE[@]}" ps -q --all web)"
[[ -n "$DB_CONTAINER" && -n "$WEB_CONTAINER" ]] || { cleanup; echo 'Veritabanı veya uygulama servisi bulunamadı.' >&2; exit 1; }
[[ "$(docker inspect --format '{{.State.Health.Status}}' "$DB_CONTAINER" 2>/dev/null || true)" == healthy ]] || { cleanup "$DB_CONTAINER"; echo 'Veritabanı hazır değil.' >&2; exit 1; }
MEDIA_VOLUME="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/company-media"}}{{.Name}}{{end}}{{end}}' "$WEB_CONTAINER")"
[[ -n "$MEDIA_VOLUME" ]] || { cleanup "$DB_CONTAINER"; echo "Şirket medya volume'u bulunamadı; eksik yedek oluşturulmadı." >&2; exit 1; }
KEYS_VOLUME="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/data-protection-keys"}}{{.Name}}{{end}}{{end}}' "$WEB_CONTAINER")"
[[ -n "$KEYS_VOLUME" ]] || { cleanup "$DB_CONTAINER"; echo "Data Protection anahtar volume'u bulunamadı; eksik yedek oluşturulmadı." >&2; exit 1; }

trap 'cleanup "$DB_CONTAINER"' EXIT
"${COMPOSE[@]}" exec -T database sh -ec "pg_dump --no-owner --no-acl --format=custom --username=\"\$POSTGRES_USER\" \"\$POSTGRES_DB\" --file='$DATABASE_TEMP'"
docker cp "$DB_CONTAINER:$DATABASE_TEMP" "$RAW_DIR/$DUMP_NAME" >/dev/null
docker exec "$DB_CONTAINER" pg_restore --list "$DATABASE_TEMP" >/dev/null
docker run --rm --network none \
  --mount "source=$MEDIA_VOLUME,target=/source,readonly" \
  --mount "type=bind,source=$RAW_DIR,target=/backup" \
  postgres:17-alpine sh -euc 'tar -czf /backup/company-media.tar.gz -C /source .; tar -tzf /backup/company-media.tar.gz >/dev/null'
docker run --rm --network none \
  --mount "source=$KEYS_VOLUME,target=/source,readonly" \
  --mount "type=bind,source=$RAW_DIR,target=/backup" \
  postgres:17-alpine sh -euc 'tar -czf /backup/data-protection-keys.tar.gz -C /source .; tar -tzf /backup/data-protection-keys.tar.gz >/dev/null'

DB_HASH="$(sha256sum "$RAW_DIR/$DUMP_NAME" | awk '{print $1}')"
MEDIA_HASH="$(sha256sum "$RAW_DIR/company-media.tar.gz" | awk '{print $1}')"
KEYS_HASH="$(sha256sum "$RAW_DIR/data-protection-keys.tar.gz" | awk '{print $1}')"
cat > "$RAW_DIR/manifest.json" <<EOF
{
  "format": "envantertr-local-backup",
  "schemaVersion": 1,
  "createdAtUtc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "scope": "linux-server-compose-instance",
  "databaseDump": "$DUMP_NAME",
  "companyMediaArchive": "company-media.tar.gz",
  "dataProtectionKeysArchive": "data-protection-keys.tar.gz",
  "sha256": {"databaseDump": "$DB_HASH", "companyMediaArchive": "$MEDIA_HASH", "dataProtectionKeysArchive": "$KEYS_HASH"},
  "verification": {"databaseDumpReadable": true, "companyMediaArchiveReadable": true},
  "encryption": "envantertr-backup-container",
  "restoreSupportedByApplication": false
}
EOF
cp -a "$PROJECT_ROOT/tools/EnvanterTR.Backup" "$WORK_DIR/tool"
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/backup" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /backup/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- encrypt /backup/raw "/backup/$(basename "$ENCRYPTED")" /run/secrets/backup-password
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/backup" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /backup/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- verify "/backup/$(basename "$ENCRYPTED")" /run/secrets/backup-password
VERIFY_DIR="$WORK_DIR/verified"
docker run --rm --network none \
  --mount "type=bind,source=$WORK_DIR,target=/backup" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /backup/tool/EnvanterTR.Backup.csproj \
  --configuration Release --no-launch-profile -- decrypt "/backup/$(basename "$ENCRYPTED")" /backup/verified /run/secrets/backup-password
bash "$PROJECT_ROOT/deploy/local/restore-preview.sh" "$VERIFY_DIR"

BACKUP_NAME="$(basename "$ENCRYPTED")"
FINAL_PATH="$BACKUP_ROOT/$BACKUP_NAME"
[[ ! -e "$FINAL_PATH" ]] || { echo 'Hedef yedek dosyası zaten var; üzerine yazılmadı.' >&2; exit 1; }
mv -- "$ENCRYPTED" "$FINAL_PATH"
(cd -- "$BACKUP_ROOT" && sha256sum -- "$BACKUP_NAME") > "$FINAL_PATH.sha256"
chmod 0600 "$FINAL_PATH" "$FINAL_PATH.sha256"
if [[ -n "$SFTP_TARGET$SFTP_DIRECTORY$SFTP_KEY_FILE$SFTP_KNOWN_HOSTS_FILE" ]]; then
  [[ -n "$SFTP_TARGET" && -n "$SFTP_DIRECTORY" && -n "$SFTP_KEY_FILE" && -n "$SFTP_KNOWN_HOSTS_FILE" ]] || {
    echo 'Uzak yedekleme için BACKUP_SFTP_TARGET, BACKUP_SFTP_DIRECTORY, BACKUP_SFTP_KEY_FILE ve BACKUP_SFTP_KNOWN_HOSTS_FILE birlikte ayarlanmalıdır.' >&2
    exit 1
  }
  [[ "$SFTP_TARGET" =~ ^[A-Za-z0-9._-]+@[A-Za-z0-9.-]+$ ]] || { echo 'BACKUP_SFTP_TARGET user@host biçiminde olmalıdır.' >&2; exit 1; }
  [[ "$SFTP_DIRECTORY" =~ ^/[A-Za-z0-9._/-]+$ && "$SFTP_DIRECTORY" != *..* && "$SFTP_DIRECTORY" != *//* ]] || {
    echo 'BACKUP_SFTP_DIRECTORY boşluksuz, mutlak ve güvenli bir uzak dizin olmalıdır.' >&2; exit 1;
  }
  [[ "$BACKUP_ROOT" != *'"'* && "$BACKUP_ROOT" != *$'\n'* && "$BACKUP_ROOT" != *$'\r'* ]] || {
    echo 'BACKUP_ROOT SFTP batch dosyası için çift tırnak veya satır sonu içeremez.' >&2; exit 1;
  }
  [[ -f "$SFTP_KEY_FILE" && -r "$SFTP_KEY_FILE" && "$(stat -c '%u:%a' "$SFTP_KEY_FILE")" == 0:600 ]] || {
    echo 'BACKUP_SFTP_KEY_FILE root sahipliğinde, chmod 600 ile korunan özel anahtar olmalıdır.' >&2; exit 1;
  }
  KNOWN_HOSTS_MODE="$(stat -c '%u:%a' "$SFTP_KNOWN_HOSTS_FILE" 2>/dev/null || true)"
  [[ -f "$SFTP_KNOWN_HOSTS_FILE" && -r "$SFTP_KNOWN_HOSTS_FILE" && "$KNOWN_HOSTS_MODE" =~ ^0:[0-7]{3,4}$ && \
    "${KNOWN_HOSTS_MODE: -2:1}" != [2367] && "${KNOWN_HOSTS_MODE: -1}" != [2367] ]] || {
    echo 'BACKUP_SFTP_KNOWN_HOSTS_FILE root sahipliğinde olmalı, sabitlenmiş anahtar içermeli ve grup/diğer kullanıcılarca yazılamamalıdır.' >&2; exit 1;
  }
  command -v sftp >/dev/null || { echo 'Uzak yedekleme için OpenSSH sftp istemcisi kurulu olmalıdır.' >&2; exit 1; }
  VERIFY_REMOTE_DIR="$WORK_DIR/remote-verify"
  mkdir -m 0700 "$VERIFY_REMOTE_DIR"
  BATCH_FILE="$WORK_DIR/sftp.batch"
  printf 'cd "%s"\nlcd "%s"\n' "$SFTP_DIRECTORY" "$BACKUP_ROOT" > "$BATCH_FILE"
  printf 'put "%s" ".%s.uploading"\nrename ".%s.uploading" "%s"\n' \
    "$BACKUP_NAME" "$BACKUP_NAME" "$BACKUP_NAME" "$BACKUP_NAME" >> "$BATCH_FILE"
  printf 'put "%s.sha256" ".%s.sha256.uploading"\nrename ".%s.sha256.uploading" "%s.sha256"\n' \
    "$BACKUP_NAME" "$BACKUP_NAME" "$BACKUP_NAME" "$BACKUP_NAME" >> "$BATCH_FILE"
  printf 'get "%s" "%s/%s"\n' "$BACKUP_NAME" "$VERIFY_REMOTE_DIR" "$BACKUP_NAME" >> "$BATCH_FILE"
  sftp -i "$SFTP_KEY_FILE" -oBatchMode=yes -oIdentitiesOnly=yes \
    -oStrictHostKeyChecking=yes -oUserKnownHostsFile="$SFTP_KNOWN_HOSTS_FILE" \
    -b "$BATCH_FILE" "$SFTP_TARGET"
  cmp -- "$FINAL_PATH" "$VERIFY_REMOTE_DIR/$BACKUP_NAME" || {
    echo 'Uzak yedek kopyası indirildi ancak yerel dosyayla eşleşmedi; uzak kopya doğrulanamadı.' >&2
    exit 1
  }
  echo "Uzak SFTP yedek kopyası SHA-256 ile doğrulandı: $SFTP_TARGET:$SFTP_DIRECTORY/$BACKUP_NAME"
  if (( SFTP_RETENTION_DAYS > 0 )); then
    REMOTE_LISTING="$WORK_DIR/remote-listing.txt"
    LIST_BATCH="$WORK_DIR/sftp-list.batch"
    CLEAN_BATCH="$WORK_DIR/sftp-clean.batch"
    printf 'cd "%s"\nls -1 envantertr-server-*\n' "$SFTP_DIRECTORY" > "$LIST_BATCH"
    sftp -q -i "$SFTP_KEY_FILE" -oBatchMode=yes -oIdentitiesOnly=yes \
      -oStrictHostKeyChecking=yes -oUserKnownHostsFile="$SFTP_KNOWN_HOSTS_FILE" \
      -b "$LIST_BATCH" "$SFTP_TARGET" > "$REMOTE_LISTING"
    CUTOFF_EPOCH="$(date -u -d "$SFTP_RETENTION_DAYS days ago" +%s)"
    declare -A REMOTE_NAMES=()
    while IFS= read -r remote_name; do
      remote_name="${remote_name#sftp> }"
      [[ "$remote_name" =~ ^envantertr-server-[0-9]{8}-[0-9]{6}-[0-9a-f]{8}\.etbackup(\.sha256)?$ ]] || continue
      REMOTE_NAMES["$remote_name"]=1
    done < "$REMOTE_LISTING"
    [[ -n "${REMOTE_NAMES[$BACKUP_NAME]:-}" && -n "${REMOTE_NAMES[$BACKUP_NAME.sha256]:-}" ]] || {
      echo 'Uzak SFTP listelemesi yeni arşiv ve SHA-256 dosyasını doğrulayamadı; uzak saklama temizliği yapılmadı.' >&2
      exit 1
    }
    printf 'cd "%s"\n' "$SFTP_DIRECTORY" > "$CLEAN_BATCH"
    REMOVED_COUNT=0
    for remote_name in "${!REMOTE_NAMES[@]}"; do
      [[ "$remote_name" =~ ^envantertr-server-([0-9]{8}-[0-9]{6})-[0-9a-f]{8}\.etbackup$ ]] || continue
      remote_timestamp="${BASH_REMATCH[1]}"
      remote_epoch="$(date -u -d "${remote_timestamp:0:8} ${remote_timestamp:9:2}:${remote_timestamp:11:2}:${remote_timestamp:13:2}" +%s 2>/dev/null || true)"
      [[ "$remote_epoch" =~ ^[0-9]+$ && "$remote_epoch" -lt "$CUTOFF_EPOCH" ]] || continue
      printf 'rm "%s"\n' "$remote_name" >> "$CLEAN_BATCH"
      if [[ -n "${REMOTE_NAMES[$remote_name.sha256]:-}" ]]; then printf 'rm "%s.sha256"\n' "$remote_name" >> "$CLEAN_BATCH"; fi
      ((REMOVED_COUNT+=1))
    done
    if (( REMOVED_COUNT > 0 )); then
      sftp -q -i "$SFTP_KEY_FILE" -oBatchMode=yes -oIdentitiesOnly=yes \
        -oStrictHostKeyChecking=yes -oUserKnownHostsFile="$SFTP_KNOWN_HOSTS_FILE" \
        -b "$CLEAN_BATCH" "$SFTP_TARGET"
      echo "Uzak SFTP saklama temizliği: $REMOVED_COUNT adet $SFTP_RETENTION_DAYS günden eski EnvanterTR arşivi kaldırıldı."
    fi
  else
    echo 'Uzak SFTP saklama temizliği BACKUP_SFTP_RETENTION_DAYS=0 ile kapalı.'
  fi
fi
find "$BACKUP_ROOT" -maxdepth 1 -type f -name 'envantertr-server-*.etbackup*' -mtime "+$RETENTION_DAYS" -delete
printf 'Şifreli yedek doğrulandı ve kaydedildi: %s\n' "$FINAL_PATH"
printf 'SHA-256: %s\n' "$(sha256sum -- "$FINAL_PATH" | awk '{print $1}')"
