#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

CONFIG_FILE="${ENVT_SERVER_ENV_FILE:-/etc/envantertr/server.env}"
MAX_AGE_HOURS="${ENVT_BACKUP_MAX_AGE_HOURS:-36}"
fail() { echo "FAIL: $*" >&2; exit 1; }
[[ "$EUID" -eq 0 ]] || fail 'Yedek sağlık kontrolü root olarak çalışmalıdır.'
[[ -r "$CONFIG_FILE" ]] || fail "Yapılandırma bulunamadı: $CONFIG_FILE"
[[ "$MAX_AGE_HOURS" =~ ^[1-9][0-9]{0,3}$ ]] || fail 'ENVT_BACKUP_MAX_AGE_HOURS 1-9999 arasında olmalıdır.'
read_config() { awk -F= -v key="$1" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE"; }
BACKUP_ROOT="$(read_config BACKUP_ROOT)"
[[ "$BACKUP_ROOT" = /* && "$BACKUP_ROOT" != / ]] || fail 'BACKUP_ROOT güvenli bir mutlak yol olmalıdır.'
[[ -d "$BACKUP_ROOT" && ! -L "$BACKUP_ROOT" ]] || fail "Yedek dizini bulunamadı veya sembolik bağ: $BACKUP_ROOT"
BACKUP_ROOT="$(realpath -e -- "$BACKUP_ROOT")"

LATEST_LINE="$(find "$BACKUP_ROOT" -maxdepth 1 -type f -name 'envantertr-server-*.etbackup' -printf '%T@ %f\n' | sort -nr | head -n 1)"
[[ -n "$LATEST_LINE" ]] || fail 'Tamamlanmış şifreli yedek bulunamadı.'
LATEST_NAME="${LATEST_LINE#* }"
[[ "$LATEST_NAME" =~ ^envantertr-server-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}\.etbackup$ ]] || fail "En yeni dosya adı yedek biçimiyle eşleşmiyor: $LATEST_NAME"
LATEST_PATH="$BACKUP_ROOT/$LATEST_NAME"
CHECKSUM_PATH="$LATEST_PATH.sha256"
[[ -f "$CHECKSUM_PATH" && ! -L "$CHECKSUM_PATH" ]] || fail "En yeni yedeğin SHA-256 yan dosyası eksik: $LATEST_NAME"

CHECKSUM_LINE="$(cat -- "$CHECKSUM_PATH")"
[[ "$CHECKSUM_LINE" =~ ^([[:xdigit:]]{64})[[:space:]]{2}(.+)$ ]] || fail "SHA-256 yan dosyası biçimi geçersiz: $LATEST_NAME"
EXPECTED_HASH="${BASH_REMATCH[1],,}"
DECLARED_NAME="${BASH_REMATCH[2]}"
[[ "$DECLARED_NAME" == "$LATEST_NAME" ]] || fail "SHA-256 yan dosyası başka bir dosya adı veriyor: $LATEST_NAME"
ACTUAL_HASH="$(sha256sum -- "$LATEST_PATH" | awk '{print tolower($1)}')"
[[ "$ACTUAL_HASH" == "$EXPECTED_HASH" ]] || fail "En yeni yedeğin SHA-256 değeri uyuşmuyor: $LATEST_NAME"

NOW="$(date +%s)"
MODIFIED="$(stat -c %Y -- "$LATEST_PATH")"
AGE_SECONDS=$((NOW - MODIFIED))
(( AGE_SECONDS >= -300 )) || fail "En yeni yedek sunucu saatinden ileri tarihli: $LATEST_NAME"
(( AGE_SECONDS <= MAX_AGE_HOURS * 3600 )) || fail "En yeni yedek eskimiş (${AGE_SECONDS}s; sınır ${MAX_AGE_HOURS} saat): $LATEST_NAME"
AGE_HOURS=$((AGE_SECONDS / 3600))
REMOTE_STATUS='disabled'
SFTP_TARGET="$(read_config BACKUP_SFTP_TARGET)"
SFTP_DIRECTORY="$(read_config BACKUP_SFTP_DIRECTORY)"
SFTP_KEY_FILE="$(read_config BACKUP_SFTP_KEY_FILE)"
SFTP_KNOWN_HOSTS_FILE="$(read_config BACKUP_SFTP_KNOWN_HOSTS_FILE)"
if [[ -n "$SFTP_TARGET$SFTP_DIRECTORY$SFTP_KEY_FILE$SFTP_KNOWN_HOSTS_FILE" ]]; then
  [[ -n "$SFTP_TARGET" && -n "$SFTP_DIRECTORY" && -n "$SFTP_KEY_FILE" && -n "$SFTP_KNOWN_HOSTS_FILE" ]] || \
    fail 'Uzak yedek sağlık denetimi için dört BACKUP_SFTP_* ayarının tümü gereklidir.'
  [[ "$SFTP_TARGET" =~ ^[A-Za-z0-9._-]+@[A-Za-z0-9.-]+$ ]] || fail 'BACKUP_SFTP_TARGET user@host biçiminde olmalıdır.'
  [[ "$SFTP_DIRECTORY" =~ ^/[A-Za-z0-9._/-]+$ && "$SFTP_DIRECTORY" != *..* && "$SFTP_DIRECTORY" != *//* ]] || \
    fail 'BACKUP_SFTP_DIRECTORY boşluksuz, mutlak ve güvenli bir uzak dizin olmalıdır.'
  [[ -f "$SFTP_KEY_FILE" && ! -L "$SFTP_KEY_FILE" && -r "$SFTP_KEY_FILE" && "$(stat -c '%u:%a' "$SFTP_KEY_FILE")" == 0:600 ]] || \
    fail 'BACKUP_SFTP_KEY_FILE root sahipliğinde ve chmod 600 ile korunmalıdır.'
  KNOWN_HOSTS_MODE="$(stat -c '%u:%a' "$SFTP_KNOWN_HOSTS_FILE" 2>/dev/null || true)"
  [[ -f "$SFTP_KNOWN_HOSTS_FILE" && ! -L "$SFTP_KNOWN_HOSTS_FILE" && -r "$SFTP_KNOWN_HOSTS_FILE" && "$KNOWN_HOSTS_MODE" =~ ^0:[0-7]{3,4}$ && \
    "${KNOWN_HOSTS_MODE: -2:1}" != [2367] && "${KNOWN_HOSTS_MODE: -1}" != [2367] ]] || \
    fail 'BACKUP_SFTP_KNOWN_HOSTS_FILE root sahipliğinde olmalı, sabitlenmiş anahtar içermeli ve grup/diğer kullanıcılarca yazılamamalıdır.'
  command -v sftp >/dev/null || fail 'Uzak yedek sağlık denetimi için OpenSSH sftp istemcisi kurulu olmalıdır.'
  [[ "$SFTP_KNOWN_HOSTS_FILE" != *'"'* && "$SFTP_KNOWN_HOSTS_FILE" != *$'\n'* && "$SFTP_KNOWN_HOSTS_FILE" != *$'\r'* ]] || \
    fail 'BACKUP_SFTP_KNOWN_HOSTS_FILE SFTP batch yolu güvenli karakterler içermelidir.'

  REMOTE_WORK="$(mktemp -d /tmp/envantertr-backup-health.XXXXXXXX)"
  chmod 0700 "$REMOTE_WORK"
  trap 'rm -rf -- "$REMOTE_WORK"' EXIT
  REMOTE_LISTING="$REMOTE_WORK/listing.txt"
  SFTP_BATCH="$REMOTE_WORK/sftp.batch"
  printf 'cd "%s"\nls -1 envantertr-server-*\nlcd "%s"\nget "%s.sha256" remote.sha256\n' \
    "$SFTP_DIRECTORY" "$REMOTE_WORK" "$LATEST_NAME" > "$SFTP_BATCH"
  if ! sftp -q -i "$SFTP_KEY_FILE" -oBatchMode=yes -oIdentitiesOnly=yes \
    -oStrictHostKeyChecking=yes -oUserKnownHostsFile="$SFTP_KNOWN_HOSTS_FILE" \
    -b "$SFTP_BATCH" "$SFTP_TARGET" > "$REMOTE_LISTING"; then
    fail 'Uzak SFTP yedek dizinine erişilemedi; uzaktaki son kopya denetlenemedi.'
  fi
  declare -A REMOTE_BACKUP_NAMES=()
  while IFS= read -r remote_name; do
    remote_name="${remote_name#sftp> }"
    remote_name="${remote_name%$'\r'}"
    [[ "$remote_name" =~ ^envantertr-server-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}\.etbackup(\.sha256)?$ ]] || continue
    REMOTE_BACKUP_NAMES["$remote_name"]=1
  done < "$REMOTE_LISTING"
  [[ -n "${REMOTE_BACKUP_NAMES[$LATEST_NAME]:-}" ]] || fail "En yeni yerel yedek uzak SFTP hedefinde yok: $LATEST_NAME"
  [[ -n "${REMOTE_BACKUP_NAMES[$LATEST_NAME.sha256]:-}" ]] || fail "En yeni uzak yedeğin SHA-256 yan dosyası yok: $LATEST_NAME"
  [[ -f "$REMOTE_WORK/remote.sha256" && ! -L "$REMOTE_WORK/remote.sha256" ]] || fail 'Uzak SFTP yan dosyası güvenli biçimde indirilemedi.'
  cmp -s -- "$CHECKSUM_PATH" "$REMOTE_WORK/remote.sha256" || fail "Uzak yedeğin SHA-256 yan dosyası yerel doğrulanmış özetle eşleşmiyor: $LATEST_NAME"
  REMOTE_STATUS='latest-copy-present'
fi
echo "PASS: envanter yedeği güncel; ageHours=$AGE_HOURS; sha256=verified; remote=$REMOTE_STATUS; file=$LATEST_NAME"
