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
echo "PASS: envanter yedeği güncel; ageHours=$AGE_HOURS; sha256=verified; file=$LATEST_NAME"
