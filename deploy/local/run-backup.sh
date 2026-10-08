#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
if [[ $# -gt 3 ]]; then
  echo 'Kullanım: bash deploy/local/run-backup.sh [parola-dosyası] [çıktı-dizini] [saklama-günü]' >&2
  exit 2
fi

TEMP_PASSWORD_FILE=''
cleanup() {
  if [[ -n "$TEMP_PASSWORD_FILE" && -f "$TEMP_PASSWORD_FILE" ]]; then rm -f -- "$TEMP_PASSWORD_FILE"; fi
}
trap cleanup EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

if [[ -n "${1:-}" ]]; then
  PASSWORD_FILE="$(realpath -- "$1")"
  if [[ ! -f "$PASSWORD_FILE" ]]; then echo 'Parola dosyası bulunamadı.' >&2; exit 1; fi
  MODE="$(stat -c '%a' "$PASSWORD_FILE")"
  if (( (8#$MODE & 077) != 0 )); then
    echo 'Linux parola dosyasının grup/diğer kullanıcı izinleri olmamalı (chmod 600 önerilir).' >&2
    exit 1
  fi
  if ! python3 - "$PASSWORD_FILE" <<'PY'
from pathlib import Path
import sys

try:
    secret = Path(sys.argv[1]).read_text(encoding="utf-8-sig").rstrip("\r\n")
    length = len(secret.encode("utf-16-le")) // 2
    if not 16 <= length <= 4096:
        raise ValueError("password length out of range")
except Exception:
    print("Parola dosyası UTF-8 olmalı ve 16–4096 karakter içermelidir; yedek alınmadı.", file=sys.stderr)
    sys.exit(1)
PY
  then
    exit 1
  fi
else
  printf 'Yedek parolası ekranda gösterilmeden iki kez istenecek. En az 16 karakter kullanın ve parolayı yedekten ayrı bir yerde saklayın.\n' >&2
  IFS= read -r -s -p 'Yeni yedek parolası: ' PASSPHRASE || { printf '\n' >&2; echo 'Parola okunamadı; yedek alınmadı.' >&2; exit 1; }
  printf '\n' >&2
  IFS= read -r -s -p 'Yedek parolasını tekrar girin: ' CONFIRMATION || { printf '\n' >&2; echo 'Parola doğrulaması okunamadı; yedek alınmadı.' >&2; exit 1; }
  printf '\n' >&2
  if [[ "$PASSPHRASE" != "$CONFIRMATION" ]]; then unset PASSPHRASE CONFIRMATION; echo 'Parolalar eşleşmedi; yedek alınmadı.' >&2; exit 1; fi
  if ! printf '%s' "$PASSPHRASE" | python3 -c 'import sys; n=len(sys.stdin.read().encode("utf-16-le"))//2; sys.exit(0 if 16 <= n <= 4096 else 1)'; then
    unset PASSPHRASE CONFIRMATION
    echo 'Parola 16–4096 karakter olmalıdır; yedek alınmadı.' >&2
    exit 1
  fi
  TEMP_ROOT="$(realpath -- "${TMPDIR:-/tmp}")"
  TEMP_PASSWORD_FILE="$(mktemp "$TEMP_ROOT/envantertr-backup-password.XXXXXX")"
  chmod 600 -- "$TEMP_PASSWORD_FILE"
  printf '%s' "$PASSPHRASE" > "$TEMP_PASSWORD_FILE"
  unset PASSPHRASE CONFIRMATION
  PASSWORD_FILE="$TEMP_PASSWORD_FILE"
fi
OUTPUT_ROOT="${2:-$PROJECT_ROOT/backups}"
RETENTION_DAYS="${3:-30}"
if [[ ! "$RETENTION_DAYS" =~ ^[1-9][0-9]{0,3}$ ]] || (( RETENTION_DAYS > 3650 )); then
  echo 'Saklama günü 1 ile 3650 arasında olmalı.' >&2; exit 2
fi
mkdir -p -- "$OUTPUT_ROOT"
OUTPUT_ROOT="$(cd -- "$OUTPUT_ROOT" && pwd -P)"
for path in "$PROJECT_ROOT" "$OUTPUT_ROOT" "$PASSWORD_FILE"; do
  if [[ "$path" == *,* ]]; then echo 'Docker bağlama yollarında virgül desteklenmiyor.' >&2; exit 2; fi
done

BACKUP_OUTPUT="$(bash "$SCRIPT_DIR/backup.sh" "$OUTPUT_ROOT")"
printf '%s\n' "$BACKUP_OUTPUT"
BACKUP_DIR="$(printf '%s\n' "$BACKUP_OUTPUT" | sed -n 's/^Yedek oluşturuldu: //p' | tail -n 1)"
if [[ -z "$BACKUP_DIR" ]]; then echo 'Yedek betiği klasör yolunu bildirmedi.' >&2; exit 1; fi
BACKUP_DIR="$(realpath -- "$BACKUP_DIR")"
if [[ "$(dirname -- "$BACKUP_DIR")" != "$OUTPUT_ROOT" || ! "$(basename -- "$BACKUP_DIR")" =~ ^envantertr-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$ ]]; then
  echo 'Yedek klasörünün yolu beklenen konum/biçimde değil; hiçbir dosya silinmedi.' >&2
  exit 1
fi

bash "$SCRIPT_DIR/verify-backup.sh" "$BACKUP_DIR"
ARCHIVE_PATH="$BACKUP_DIR.etbackup"
BACKUP_NAME="$(basename -- "$BACKUP_DIR")"
ARCHIVE_NAME="$(basename -- "$ARCHIVE_PATH")"
docker run --rm \
  --mount "type=bind,source=$PROJECT_ROOT,target=/source" \
  --mount "type=bind,source=$OUTPUT_ROOT,target=/backups" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project /source/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj \
    --configuration Release --no-launch-profile -- encrypt "/backups/$BACKUP_NAME" "/backups/$ARCHIVE_NAME" /run/secrets/backup-password
if ! docker run --rm \
  --mount "type=bind,source=$PROJECT_ROOT,target=/source" \
  --mount "type=bind,source=$OUTPUT_ROOT,target=/backups" \
  --mount "type=bind,source=$PASSWORD_FILE,target=/run/secrets/backup-password,readonly" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project /source/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj \
    --configuration Release --no-launch-profile -- verify "/backups/$ARCHIVE_NAME" /run/secrets/backup-password; then
  rm -f -- "$ARCHIVE_PATH"
  echo 'Şifreli arşiv doğrulanamadı; hatalı kopya kaldırıldı, düz yedek klasörü korundu.' >&2
  exit 1
fi

chmod 600 -- "$ARCHIVE_PATH"
# Remove the plaintext directory only after both archive creation and verification succeeded.
if [[ "$(dirname -- "$BACKUP_DIR")" == "$OUTPUT_ROOT" && "$(basename -- "$BACKUP_DIR")" =~ ^envantertr-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$ ]]; then
  rm -rf -- "$BACKUP_DIR"
fi

find "$OUTPUT_ROOT" -maxdepth 1 -type f -name 'envantertr-*.etbackup' -mmin "+$((RETENTION_DAYS * 1440))" -print0 |
  while IFS= read -r -d '' old_archive; do
    if [[ "$(basename -- "$old_archive")" =~ ^envantertr-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}\.etbackup$ ]]; then
      rm -f -- "$old_archive"
    fi
  done

echo "Şifreli ve doğrulanmış yedek hazır: $ARCHIVE_PATH"
echo "Saklama süresi: $RETENTION_DAYS gün. Ham yedek klasörü işlem sonunda kaldırıldı."
