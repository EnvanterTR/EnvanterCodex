#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

UNIT_NAME='envantertr-backup'
UNIT_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user"
SERVICE_FILE="$UNIT_DIR/$UNIT_NAME.service"
TIMER_FILE="$UNIT_DIR/$UNIT_NAME.timer"

if [[ "${1:-}" == '--uninstall' ]]; then
  systemctl --user disable --now "$UNIT_NAME.timer" >/dev/null 2>&1 || true
  rm -f -- "$SERVICE_FILE" "$TIMER_FILE"
  systemctl --user daemon-reload
  systemctl --user reset-failed "$UNIT_NAME.service" "$UNIT_NAME.timer" >/dev/null 2>&1 || true
  echo 'EnvanterTR yedek zamanlayıcısı kaldırıldı.'
  exit 0
fi

if [[ $# -lt 1 || $# -gt 4 ]]; then
  echo 'Kullanım: bash deploy/local/configure-backup-timer.sh <parola-dosyası> [çıktı-dizini] [HH:MM] [saklama-günü]' >&2
  echo 'Kaldırma: bash deploy/local/configure-backup-timer.sh --uninstall' >&2
  exit 2
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
PASSWORD_FILE="$(realpath -- "$1")"
OUTPUT_ROOT="${2:-$PROJECT_ROOT/backups}"
mkdir -p -- "$OUTPUT_ROOT"
OUTPUT_ROOT="$(cd -- "$OUTPUT_ROOT" && pwd -P)"
AT="${3:-02:00}"
RETENTION_DAYS="${4:-30}"
if [[ ! "$AT" =~ ^([01][0-9]|2[0-3]):[0-5][0-9]$ ]]; then echo 'Saat HH:MM biçiminde olmalı.' >&2; exit 2; fi
if [[ ! "$RETENTION_DAYS" =~ ^[1-9][0-9]{0,3}$ ]] || (( RETENTION_DAYS > 3650 )); then echo 'Saklama günü 1 ile 3650 arasında olmalı.' >&2; exit 2; fi
if [[ ! -f "$PASSWORD_FILE" ]]; then echo 'Parola dosyası bulunamadı.' >&2; exit 1; fi
if ! command -v systemctl >/dev/null || ! systemctl --user show-environment >/dev/null 2>&1; then
  echo 'Kullanıcı systemd servisi erişilemiyor.' >&2; exit 1
fi

# Enforce a private plaintext passphrase file before creating a recurring job.
chmod 600 -- "$PASSWORD_FILE"
for path in "$SCRIPT_DIR/run-backup.sh" "$PASSWORD_FILE" "$OUTPUT_ROOT" "$PROJECT_ROOT"; do
  for forbidden in ' ' $'\t' $'\n' '%' '"' "'" ';' '$' '\\'; do
    if [[ "$path" == *"$forbidden"* ]]; then
      echo 'systemd biriminde boşluk veya özel karakter içermeyen yollar kullanın.' >&2; exit 2
    fi
  done
done

mkdir -p -- "$UNIT_DIR"
cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=Create and verify an encrypted EnvanterTR backup

[Service]
Type=oneshot
WorkingDirectory=$PROJECT_ROOT
ExecStart=/usr/bin/bash $SCRIPT_DIR/run-backup.sh $PASSWORD_FILE $OUTPUT_ROOT $RETENTION_DAYS
EOF

cat > "$TIMER_FILE" <<EOF
[Unit]
Description=Daily EnvanterTR encrypted backup

[Timer]
OnCalendar=*-*-* $AT:00
Persistent=true
RandomizedDelaySec=5m
Unit=$UNIT_NAME.service

[Install]
WantedBy=timers.target
EOF

chmod 600 -- "$SERVICE_FILE" "$TIMER_FILE"
systemctl --user daemon-reload
systemctl --user enable --now "$UNIT_NAME.timer"

echo "Günlük $AT kullanıcı zamanlayıcısı kuruldu: $UNIT_NAME.timer"
echo 'systemd kullanıcı yöneticisinin çalışması ve Docker daemon erişiminin zamanlama anında bulunması gerekir.'
echo "Elle başlat: systemctl --user start $UNIT_NAME.service"
echo "Durumu gör: systemctl --user status $UNIT_NAME.timer $UNIT_NAME.service"
echo "Kaldır: bash $SCRIPT_DIR/configure-backup-timer.sh --uninstall"
