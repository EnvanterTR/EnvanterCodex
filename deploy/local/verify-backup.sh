#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

if [[ $# -ne 1 || ! -d "$1" ]]; then
  echo 'Kullanım: bash deploy/local/verify-backup.sh <yedek-klasörü>' >&2
  exit 2
fi

BACKUP_DIR="$(cd -- "$1" && pwd -P)"
MANIFEST="$BACKUP_DIR/manifest.json"
if [[ ! -f "$MANIFEST" ]]; then
  echo 'manifest.json bulunamadı; bu klasör EnvanterTR yedek paketi değil.' >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo 'Manifesti güvenli biçimde okumak için python3 gereklidir.' >&2
  exit 1
fi
mapfile -t METADATA < <(python3 - "$MANIFEST" <<'PY'
import json
import re
import sys
from pathlib import Path

try:
    data = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    if data.get("format") != "envantertr-local-backup" or data.get("schemaVersion") != 1:
        raise ValueError("unsupported manifest")
    dump = data.get("databaseDump", "")
    if not isinstance(dump, str) or not re.fullmatch(r"database-[a-f0-9]{8}\.dump", dump):
        raise ValueError("invalid database file name")
    media = data.get("companyMediaArchive", "")
    if media != "company-media.tar.gz":
        raise ValueError("invalid media file name")
    sha = data.get("sha256", {})
    dump_hash = sha.get("databaseDump", "")
    media_hash = sha.get("companyMediaArchive", "")
    if not isinstance(dump_hash, str) or not re.fullmatch(r"[a-fA-F0-9]{64}", dump_hash):
        raise ValueError("invalid database hash")
    if not isinstance(media_hash, str) or not re.fullmatch(r"[a-fA-F0-9]{64}", media_hash):
        raise ValueError("invalid media hash")
    print(dump)
    print(media)
    print(dump_hash.lower())
    print(media_hash.lower())
except Exception as exc:
    print(f"Yedek manifesti geçersiz: {exc}", file=sys.stderr)
    sys.exit(1)
PY
)
if [[ ${#METADATA[@]} -ne 4 ]]; then
  echo 'Yedek manifesti okunamadı veya desteklenmiyor.' >&2
  exit 1
fi

DUMP_NAME="${METADATA[0]}"
MEDIA_NAME="${METADATA[1]}"
EXPECTED_DUMP_SHA="${METADATA[2]}"
EXPECTED_MEDIA_SHA="${METADATA[3]}"
for file in "$DUMP_NAME" "$MEDIA_NAME"; do
  if [[ ! -f "$BACKUP_DIR/$file" ]]; then
    echo "Yedek dosyası bulunamadı: $file" >&2
    exit 1
  fi
done

ACTUAL_DUMP_SHA="$(sha256sum -- "$BACKUP_DIR/$DUMP_NAME" | cut -d ' ' -f 1)"
ACTUAL_MEDIA_SHA="$(sha256sum -- "$BACKUP_DIR/$MEDIA_NAME" | cut -d ' ' -f 1)"
if [[ "$ACTUAL_DUMP_SHA" != "$EXPECTED_DUMP_SHA" || "$ACTUAL_MEDIA_SHA" != "$EXPECTED_MEDIA_SHA" ]]; then
  echo 'SHA-256 özeti manifest ile eşleşmiyor. Dosya değiştirilmiş veya bozulmuş olabilir.' >&2
  exit 1
fi

tar -tzf "$BACKUP_DIR/$MEDIA_NAME" >/dev/null
docker run --rm --network none --mount "type=bind,source=$BACKUP_DIR,target=/backup,readonly" \
  postgres:17-alpine pg_restore --list "/backup/$DUMP_NAME" >/dev/null

echo 'Yedek doğrulandı: manifest, iki SHA-256 özeti, medya arşivi ve PostgreSQL döküm dizini okunabilir.'
echo 'Bu komut veritabanını değiştirmez ve yedeği geri yüklemez.'
