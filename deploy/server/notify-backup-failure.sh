#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

CONFIG_FILE="${ENVT_SERVER_ENV_FILE:-/etc/envantertr/server.env}"
FAILED_UNIT="${1:-unknown}"
log() { logger -p daemon.err -t envantertr-backup-alert -- "$*" 2>/dev/null || printf '%s\n' "$*" >&2; }

[[ -r "$CONFIG_FILE" ]] || { log 'FAIL: sunucu yapılandırması okunamıyor; bildirim gönderilemedi.'; exit 1; }
ALERT_URL="$(awk -F= '$1 == "BACKUP_ALERT_WEBHOOK_URL" {sub(/^[^=]*=/, ""); print; exit}' "$CONFIG_FILE")"
if [[ -z "$ALERT_URL" ]]; then
  log "SKIP: $FAILED_UNIT başarısız oldu; BACKUP_ALERT_WEBHOOK_URL ayarlanmadığı için dış bildirim gönderilmedi."
  exit 0
fi
[[ "$ALERT_URL" == https://* && "$ALERT_URL" != *[[:space:]]* ]] || {
  log "FAIL: $FAILED_UNIT başarısız oldu; bildirim adresi geçerli bir HTTPS URL değil."
  exit 1
}

RESULT="$(systemctl show --property=Result --value "$FAILED_UNIT" 2>/dev/null || printf unknown)"
EXIT_STATUS="$(systemctl show --property=ExecMainStatus --value "$FAILED_UNIT" 2>/dev/null || printf unknown)"
HOST="$(hostname -f 2>/dev/null || hostname)"
OBSERVED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
PAYLOAD="{\"event\":\"envantertr_backup_failure\",\"unit\":\"$FAILED_UNIT\",\"host\":\"$HOST\",\"result\":\"$RESULT\",\"exitStatus\":\"$EXIT_STATUS\",\"observedAtUtc\":\"$OBSERVED_AT\"}"

if curl --fail --silent --show-error --output /dev/null \
  --connect-timeout 5 --max-time 15 --retry 2 \
  --header 'Content-Type: application/json' --data-binary "$PAYLOAD" "$ALERT_URL" 2>/dev/null; then
  log "PASS: $FAILED_UNIT başarısızlık bildirimi HTTPS webhook'una gönderildi."
else
  log "FAIL: $FAILED_UNIT başarısız oldu ve HTTPS webhook bildirimi teslim edilemedi."
  exit 1
fi
