#!/usr/bin/env bash
set -Eeuo pipefail
SOURCE_SCRIPT="${1:?pass install-guided.sh}"
[[ "$EUID" -eq 0 ]] || { echo 'Run only as root in a disposable container.' >&2; exit 2; }
source /etc/os-release
[[ "${ID:-}" == ubuntu && "${VERSION_ID:-}" == 24.04 && "$(dpkg --print-architecture)" == amd64 ]] || { echo 'Expected Ubuntu 24.04 amd64.' >&2; exit 2; }
[[ ! -e /etc/envantertr/server.env && ! -e /opt/envantertr/current && ! -L /opt/envantertr/current ]] || { echo 'Container already contains an EnvanterTR installation.' >&2; exit 2; }
[[ ! -e /etc/apt/sources.list.d/docker.sources && ! -e /etc/apt/keyrings/docker.asc ]] || { echo 'Container already has Docker apt configuration.' >&2; exit 2; }
ROOT="$(mktemp -d /tmp/envantertr-guided-acceptance.XXXXXXXX)"
trap 'rm -rf -- "$ROOT"; rm -f /etc/envantertr/server.env /etc/apt/sources.list.d/docker.sources /etc/apt/keyrings/docker.asc; rmdir /etc/envantertr /etc/apt/sources.list.d /etc/apt/keyrings 2>/dev/null || true' EXIT
PASS_COUNT=0
fail() { printf 'FAIL: %s\n' "$*" >&2; exit 1; }
pass() { PASS_COUNT=$((PASS_COUNT + 1)); printf 'PASS: %s\n' "$*"; }
new_case() {
  CASE="$ROOT/$1"; BIN="$CASE/bin"; FIXTURE="$CASE/source"
  mkdir -p "$BIN" "$FIXTURE"
  cp "$SOURCE_SCRIPT" "$FIXTURE/install-guided.sh"
  : > "$FIXTURE/compose.yaml"
  cat > "$FIXTURE/install.sh" <<'STUB'
#!/usr/bin/env bash
printf '%s\n' "$1" > "$INSTALL_CAPTURE"
STUB
  chmod 0755 "$FIXTURE/install.sh"
  export INSTALL_CAPTURE="$CASE/domain.log" APT_LOG="$CASE/apt.log" SYSTEMCTL_LOG="$CASE/systemctl.log" DOCKER_STARTED="$CASE/docker-started" BIN
  cat > "$BIN/getent" <<'STUB'
#!/usr/bin/env bash
[[ "${DNS_FAIL:-0}" != 1 ]]
STUB
  cat > "$BIN/systemctl" <<'STUB'
#!/usr/bin/env bash
printf '%s\n' "$*" >> "$SYSTEMCTL_LOG"
[[ "$*" == 'enable --now docker' ]] || exit 2
touch "$DOCKER_STARTED"
STUB
  cat > "$BIN/dpkg-query" <<'STUB'
#!/usr/bin/env bash
[[ "${PACKAGE_CONFLICT:-}" == "${!#}" ]] && { echo installed; exit 0; }
exit 1
STUB
  cat > "$BIN/apt-get" <<'STUB'
#!/usr/bin/env bash
printf '%s\n' "$*" >> "$APT_LOG"
if [[ "$*" == *'install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin'* ]]; then
  cat > "$BIN/docker" <<'DOCKER'
#!/usr/bin/env bash
case "$1" in 'compose') [[ "${2:-}" == version ]] && echo 'Docker Compose version v2.30.0' ;; 'info') exit 0 ;; *) exit 2 ;; esac
DOCKER
  chmod 0755 "$BIN/docker"
fi
STUB
  cat > "$BIN/curl" <<'STUB'
#!/usr/bin/env bash
while (($#)); do
  if [[ "$1" == -o ]]; then shift; printf '%s\n' '-----BEGIN PGP PUBLIC KEY BLOCK-----' fixture '-----END PGP PUBLIC KEY BLOCK-----' > "$1"; exit 0; fi
  shift
done
exit 2
STUB
  chmod 0755 "$BIN"/*
  export PATH="$BIN:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
}
no_apt() { [[ ! -s "$APT_LOG" ]] || fail 'apt-get ran although setup should have stopped.'; }

new_case ready
cat > "$BIN/docker" <<'STUB'
#!/usr/bin/env bash
case "$1" in 'compose') [[ "${2:-}" == version ]] && echo 'Docker Compose version v2.30.0' ;; 'info') exit 0 ;; *) exit 2 ;; esac
STUB
chmod 0755 "$BIN/docker"
printf 'e\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 || { cat "$CASE/out" >&2; fail 'ready Docker flow failed.'; }
[[ "$(cat "$INSTALL_CAPTURE")" == inventory.example.com ]] || fail 'wrong domain passed to install.sh.'
no_apt
pass 'ready Docker continues without apt changes'

new_case domain-prompt
cat > "$BIN/docker" <<'STUB'
#!/usr/bin/env bash
case "$1" in 'compose') [[ "${2:-}" == version ]] && echo 'Docker Compose version v2.30.0' ;; 'info') exit 0 ;; *) exit 2 ;; esac
STUB
chmod 0755 "$BIN/docker"
printf 'inventory.example.com\ne\n' | bash "$FIXTURE/install-guided.sh" > "$CASE/out" 2>&1 || fail 'domain prompt flow failed.'
[[ "$(cat "$INSTALL_CAPTURE")" == inventory.example.com ]] || fail 'prompted domain not forwarded.'
pass 'prompted DNS name is forwarded'

new_case refuse-network
if printf 'H\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1; then fail 'declined network confirmation succeeded.'; fi
grep -q 'sistem değiştirilmedi' "$CASE/out" || fail 'network refusal not explained.'
no_apt; [[ ! -e "$INSTALL_CAPTURE" ]] || fail 'network refusal reached install.sh.'
pass 'network refusal stops before changes'

new_case refuse-docker
printf 'e\nH\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 && fail 'declined Docker setup succeeded.'
grep -q 'Docker kurulumu onaylanmadı' "$CASE/out" || fail 'Docker refusal not explained.'
no_apt; [[ ! -e "$INSTALL_CAPTURE" ]] || fail 'Docker refusal reached install.sh.'
pass 'Docker refusal causes no apt changes'

new_case conflict
export PACKAGE_CONFLICT=docker.io
printf 'e\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 && fail 'conflicting package accepted.'
grep -q 'Mevcut paket' "$CASE/out" || fail 'package conflict not explained.'
no_apt; unset PACKAGE_CONFLICT
pass 'conflicting package is preserved and rejected'

new_case existing
mkdir -p /etc/envantertr; echo existing > /etc/envantertr/server.env
bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 && fail 'existing installation accepted.'
grep -q 'kurulumu zaten var' "$CASE/out" || fail 'existing installation not explained.'
no_apt; rm -f /etc/envantertr/server.env; rmdir /etc/envantertr
pass 'existing installation is detected before changes'

new_case start-docker
cat > "$BIN/docker" <<'STUB'
#!/usr/bin/env bash
case "$1" in 'compose') [[ "${2:-}" == version ]] && echo 'Docker Compose version v2.30.0' ;; 'info') [[ -e "$DOCKER_STARTED" ]] ;; *) exit 2 ;; esac
STUB
chmod 0755 "$BIN/docker"
printf 'e\ne\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 || fail 'consented Docker start failed.'
grep -q '^enable --now docker$' "$SYSTEMCTL_LOG" || fail 'Docker not started after consent.'
no_apt
pass 'stopped Docker starts only after confirmation'

new_case install-docker
mkdir -p /etc/apt/keyrings
printf '%s\n' '-----BEGIN PGP PUBLIC KEY BLOCK-----' fixture '-----END PGP PUBLIC KEY BLOCK-----' > /etc/apt/keyrings/docker.asc
chmod 0644 /etc/apt/keyrings/docker.asc
printf 'e\ne\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1 || fail 'consented Docker install failed.'
grep -q 'install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin' "$APT_LOG" || fail 'official Docker packages not selected.'
[[ -s /etc/apt/keyrings/docker.asc && -s /etc/apt/sources.list.d/docker.sources ]] || fail 'Docker key/source missing.'
[[ "$(stat -c '%a' /etc/apt/keyrings/docker.asc)" == 644 && "$(stat -c '%a' /etc/apt/sources.list.d/docker.sources)" == 644 ]] || fail 'Docker source permissions are wrong.'
grep -q '^URIs: https://download.docker.com/linux/ubuntu$' /etc/apt/sources.list.d/docker.sources || fail 'unexpected Docker apt URI.'
[[ "$(cat "$INSTALL_CAPTURE")" == inventory.example.com ]] || fail 'Docker install did not continue.'
pass 'consented install uses the official Docker apt source'

new_case key-conflict
mkdir -p /etc/apt/keyrings
printf '%s\n' '-----BEGIN PGP PUBLIC KEY BLOCK-----' 'unrelated key' '-----END PGP PUBLIC KEY BLOCK-----' > /etc/apt/keyrings/docker.asc
if printf 'e\ne\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1; then fail 'unrelated PGP key was trusted.'; fi
grep -q 'resmi Docker anahtarıyla eşleşmiyor; üzerine yazmadım' "$CASE/out" || fail 'key conflict was not explained.'
grep -q 'install -y docker-ce docker-ce-cli' "$APT_LOG" && fail 'Docker packages installed with a mismatched key.'
[[ "$(cat /etc/apt/keyrings/docker.asc)" == $'-----BEGIN PGP PUBLIC KEY BLOCK-----\nunrelated key\n-----END PGP PUBLIC KEY BLOCK-----' ]] || fail 'existing PGP key was changed.'
pass 'mismatched existing PGP key is neither trusted nor overwritten'

new_case apt-conflict
mkdir -p /etc/apt/sources.list.d
printf 'Types: deb\nURIs: https://other.invalid/ubuntu\n' > /etc/apt/sources.list.d/docker.sources
if printf 'e\ne\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1; then fail 'unrelated apt source was overwritten.'; fi
grep -q 'başka bir kaynağa ait; üzerine yazmadım' "$CASE/out" || fail 'apt conflict not explained.'
no_apt; rm -f /etc/apt/sources.list.d/docker.sources; rmdir /etc/apt/sources.list.d 2>/dev/null || true
pass 'unrelated Docker apt source is preserved'

new_case dns-failure
export DNS_FAIL=1
if printf 'e\n' | bash "$FIXTURE/install-guided.sh" inventory.example.com > "$CASE/out" 2>&1; then fail 'unresolved DNS accepted.'; fi
grep -q 'DNS üzerinden çözümlenemedi' "$CASE/out" || fail 'DNS failure not explained.'
no_apt; unset DNS_FAIL
pass 'DNS failure stops before installation'

printf '\nGuided installer acceptance: %s checks passed in disposable Ubuntu 24.04.\n' "$PASS_COUNT"
